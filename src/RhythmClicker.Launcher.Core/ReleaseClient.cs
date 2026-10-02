// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.Net.Http.Headers;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RhythmClicker.Launcher.Core;

public sealed record ReleaseManifest(int SchemaVersion, string Version, string DisplayName, string Channel, string AssetName, string Sha256, long Size, string[] Notes);
public sealed record AvailableRelease(ReleaseManifest Manifest, Uri DownloadUrl, string PageUrl);
public sealed class ReleaseClient(HttpClient http)
{
    public const string Repository = "MoriTeahouse/RhythmClicker";
    public static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, WriteIndented = true };
    private static readonly Regex SemVer = new(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-([A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*))?$", RegexOptions.CultureInvariant);
    public static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("RhythmClickerLauncher/1.0.0"); client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28"); return client;
    }
    public async Task<AvailableRelease> FindAsync(bool includeTesting, CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases?per_page=30"); request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"GitHub 版本查詢失敗（{(int)response.StatusCode}）；已安裝遊戲仍可啟動。");
        using var releases = JsonDocument.Parse(await ReadBoundedAsync(response.Content, 2 * 1024 * 1024, cancellation));
        var candidates = releases.RootElement.EnumerateArray().Where(r => !r.GetProperty("draft").GetBoolean() && (includeTesting || !r.GetProperty("prerelease").GetBoolean()) && SemVer.IsMatch((r.GetProperty("tag_name").GetString() ?? "").TrimStart('v'))).Select(r => r.Clone()).ToList();
        candidates.Sort((a, b) => -CompareVersions(a.GetProperty("tag_name").GetString()!.TrimStart('v'), b.GetProperty("tag_name").GetString()!.TrimStart('v')));
        foreach (var release in candidates)
        {
            var assets = release.GetProperty("assets").EnumerateArray().ToArray();
            var descriptor = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == "rhythmclicker-release.json");
            bool legacy = descriptor.ValueKind == JsonValueKind.Undefined;
            if (legacy) descriptor = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == "RhythmClicker.zip.manifest.json");
            if (descriptor.ValueKind == JsonValueKind.Undefined) continue;
            var uri = new Uri(descriptor.GetProperty("browser_download_url").GetString()!); ValidateDownloadUri(uri);
            using var manifestResponse = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellation); manifestResponse.EnsureSuccessStatusCode(); ValidateRedirect(manifestResponse);
            string contents = await ReadBoundedAsync(manifestResponse.Content, 128_000, cancellation);
            string version = release.GetProperty("tag_name").GetString()!.TrimStart('v');
            ReleaseManifest manifest;
            if (legacy)
            {
                using var old = JsonDocument.Parse(contents); string hash = old.RootElement.GetProperty("sha256").GetString() ?? "";
                var package = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == "RhythmClicker.zip"); if (package.ValueKind == JsonValueKind.Undefined) continue;
                manifest = new(1, version, "RhythmClicker " + version, release.GetProperty("prerelease").GetBoolean() ? "testing" : "stable", "RhythmClicker.zip", hash, package.GetProperty("size").GetInt64(), ["MoriTeahouse 公開發布 · 包含遊戲與所需執行環境"]);
            }
            else manifest = JsonSerializer.Deserialize<ReleaseManifest>(contents, Json) ?? throw new InvalidDataException("版本資訊損壞。");
            Validate(manifest); if (manifest.Version != version) throw new InvalidDataException("版本描述與 GitHub 標籤不符。");
            var asset = assets.FirstOrDefault(a => a.GetProperty("name").GetString() == manifest.AssetName); if (asset.ValueKind == JsonValueKind.Undefined) continue;
            if (asset.GetProperty("size").GetInt64() != manifest.Size) throw new InvalidDataException("發布檔案大小不符。");
            uri = new Uri(asset.GetProperty("browser_download_url").GetString()!); ValidateDownloadUri(uri);
            return new(manifest, uri, release.GetProperty("html_url").GetString()!);
        }
        throw new InvalidDataException(includeTesting ? "發布庫尚無支援的下載版本。" : "目前只有測試版本；可勾選包含測試版。");
    }
    public static void Validate(ReleaseManifest manifest)
    {
        if (manifest.SchemaVersion is not (1 or 2) || !SemVer.IsMatch(manifest.Version) || manifest.Version.Length > 100 || manifest.DisplayName == null || manifest.DisplayName.Length > 300 || manifest.Notes == null || manifest.Notes.Length > 40 || manifest.Notes.Any(n => n == null || n.Length > 4000)) throw new InvalidDataException("版本資訊格式錯誤。");
        if (manifest.Size is <= 0 or > 2L * 1024 * 1024 * 1024 || !Regex.IsMatch(manifest.Sha256, @"^[a-fA-F0-9]{64}$")) throw new InvalidDataException("檔案大小或 SHA-256 錯誤。");
        if (manifest.AssetName != Path.GetFileName(manifest.AssetName) || manifest.AssetName.Contains('\\') || manifest.AssetName.Contains(':') || Path.GetExtension(manifest.AssetName).ToLowerInvariant() is not (".zip" or ".atr")) throw new InvalidDataException("封裝檔名錯誤。");
    }
    public static void ValidateDownloadUri(Uri uri)
    {
        if (uri.Scheme != "https" || uri.Host != "github.com" || !uri.AbsolutePath.StartsWith("/" + Repository + "/releases/download/", StringComparison.Ordinal) || !string.IsNullOrEmpty(uri.UserInfo)) throw new InvalidDataException("下載來源必須是 RhythmClicker 的 GitHub 發布。");
    }
    public static void ValidateRedirect(HttpResponseMessage response)
    {
        var uri = response.RequestMessage?.RequestUri; if (uri == null) throw new InvalidDataException("下載缺少來源。");
        if (uri.Scheme != "https" || !(uri.Host == "github.com" || uri.Host == "release-assets.githubusercontent.com" || uri.Host == "objects.githubusercontent.com") || uri.UserInfo.Length > 0) throw new InvalidDataException("下載重新導向來源不受支援。");
    }
    public static async Task<string> ReadBoundedAsync(HttpContent content, int maximum, CancellationToken cancellation)
    {
        if (content.Headers.ContentLength > maximum) throw new InvalidDataException("版本資訊過大。");
        await using var stream = await content.ReadAsStreamAsync(cancellation); using var output = new MemoryStream(); byte[] buffer = new byte[8192]; int read;
        while ((read = await stream.ReadAsync(buffer, cancellation)) != 0) { if (output.Length + read > maximum) throw new InvalidDataException("版本資訊過大。"); output.Write(buffer, 0, read); }
        return System.Text.Encoding.UTF8.GetString(output.ToArray());
    }
    public static int CompareVersions(string left, string right)
    {
        if (!SemVer.IsMatch(left) || !SemVer.IsMatch(right)) throw new InvalidDataException("版本格式錯誤。");
        string[] a = left.Split('-', 2), b = right.Split('-', 2); var x = a[0].Split('.'); var y = b[0].Split('.');
        for (int i = 0; i < 3; i++) { int result = BigInteger.Parse(x[i]).CompareTo(BigInteger.Parse(y[i])); if (result != 0) return result; }
        if (a.Length == 1 || b.Length == 1) return a.Length == b.Length ? 0 : a.Length == 1 ? 1 : -1;
        x = a[1].Split('.'); y = b[1].Split('.');
        for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
        {
            bool nx = BigInteger.TryParse(x[i], out var vx), ny = BigInteger.TryParse(y[i], out var vy); int result = nx && ny ? vx.CompareTo(vy) : nx != ny ? nx ? -1 : 1 : StringComparer.Ordinal.Compare(x[i], y[i]); if (result != 0) return result;
        }
        return x.Length.CompareTo(y.Length);
    }
}
