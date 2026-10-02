// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using MatrixTea.Engine.Packaging;

namespace RhythmClicker.Launcher.Core;

public sealed record InstalledGame(string Version, string DirectoryName, string DisplayName, string Sha256);
public sealed record InstallProgress(string Phase, double Percent, long Downloaded = 0, long Total = 0);
public static class InstallPaths
{
    public static string Root(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)) throw new IOException("請選擇完整的安裝資料夾路徑。");
        string root = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        if (root == Path.GetPathRoot(root)?.TrimEnd(Path.DirectorySeparatorChar)) throw new IOException("請選擇硬碟內的資料夾，例如 D:\\Games\\RhythmClicker。");
        CheckLinks(root); return root;
    }
    private static void CheckLinks(string path)
    {
        for (var node = new FileInfo(path) as FileSystemInfo; node != null; node = new DirectoryInfo(Path.GetDirectoryName(node.FullName)!))
        {
            if ((File.Exists(node.FullName) || Directory.Exists(node.FullName)) && File.GetAttributes(node.FullName).HasFlag(FileAttributes.ReparsePoint)) throw new IOException("請使用實際資料夾；路徑連結可能將資料存到另一顆硬碟。");
            if (Path.GetDirectoryName(node.FullName) == null) break;
        }
    }
    public static string Within(string root, string relative)
    {
        string normalized = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; string result = Path.GetFullPath(Path.Combine(normalized, relative));
        if (!result.StartsWith(normalized, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("封裝路徑離開安裝資料夾。"); CheckLinks(result); return result;
    }
    public static InstalledGame? Read(string root)
    {
        try
        {
            string path = Within(Root(root), ".launcher/install.json"); if (new FileInfo(path).Length > 16000) return null;
            var game = JsonSerializer.Deserialize<InstalledGame>(File.ReadAllText(path), ReleaseClient.Json);
            if (game == null || string.IsNullOrWhiteSpace(game.DirectoryName) || game.DirectoryName != Path.GetFileName(game.DirectoryName) || game.DirectoryName.Contains('\\') || game.DirectoryName.Contains(':')) return null;
            ReleaseClient.Validate(new(1, game.Version, game.DisplayName, "installed", "game.zip", game.Sha256, 1, []));
            return File.Exists(Within(root, "Game/" + game.DirectoryName + "/ClickerGame.exe")) ? game : null;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException or ArgumentException) { return null; }
    }
    public static void WriteJson<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { using (var output = new FileStream(temp, FileMode.CreateNew)) { JsonSerializer.Serialize(output, value, ReleaseClient.Json); output.Flush(true); } if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
public sealed class Installer(HttpClient http, bool allowLocalTestServer = false)
{
    public Task<InstalledGame> InstallAsync(AvailableRelease release, string root, IProgress<InstallProgress>? progress, CancellationToken cancellation) => InstallCoreAsync(release, root, progress, cancellation, null);
    public async Task<InstalledGame> InstallFileAsync(string path, string root, IProgress<InstallProgress>? progress, CancellationToken cancellation)
    {
        path = Path.GetFullPath(path); long size = new FileInfo(path).Length;
        if (size is <= 0 or > 2L * 1024 * 1024 * 1024 || Path.GetExtension(path).ToLowerInvariant() is not (".zip" or ".atr")) throw new InvalidDataException("請選擇 ZIP／ATR 遊戲封裝。");
        await using var input = File.OpenRead(path); string hash = Convert.ToHexString(await SHA256.HashDataAsync(input, cancellation));
        var manifest = new ReleaseManifest(2, "0.0.0-local", "RhythmClicker 本機封裝", "offline", Path.GetFileName(path), hash, size, []);
        return await InstallCoreAsync(new(manifest, new Uri(path), ""), root, progress, cancellation, path);
    }
    private async Task<InstalledGame> InstallCoreAsync(AvailableRelease release, string chosenRoot, IProgress<InstallProgress>? progress, CancellationToken cancellation, string? localFile)
    {
        string root = InstallPaths.Root(chosenRoot); ReleaseClient.Validate(release.Manifest);
        bool test = allowLocalTestServer && release.DownloadUrl.IsLoopback;
        if (localFile == null && !test) ReleaseClient.ValidateDownloadUri(release.DownloadUrl);
        Directory.CreateDirectory(root); string work = InstallPaths.Within(root, ".launcher"); Directory.CreateDirectory(work);
        using var gate = new FileStream(Path.Combine(work, "install.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (new DriveInfo(Path.GetPathRoot(root)!).AvailableFreeSpace < Math.Max(release.Manifest.Size * 8 + 64 * 1024 * 1024, 512L * 1024 * 1024)) throw new IOException("所選硬碟空間不足（需保留下載與解封裝空間）。");
        string job = InstallPaths.Within(root, ".launcher/jobs/" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(job);
        string package = Path.Combine(job, "package.partial"), staging = Path.Combine(job, "extracted");
        try
        {
            progress?.Report(new(localFile == null ? "連線 GitHub" : "讀取本機封裝", 0));
            using (var response = localFile == null ? await http.GetAsync(release.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellation) : new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StreamContent(File.OpenRead(localFile)) })
            {
                response.EnsureSuccessStatusCode(); if (localFile == null && !test) ReleaseClient.ValidateRedirect(response);
                if (response.Content.Headers.ContentLength is long length && length != release.Manifest.Size) throw new InvalidDataException("下載大小不符。");
                await using var input = await response.Content.ReadAsStreamAsync(cancellation); await using var output = new FileStream(package, FileMode.CreateNew, FileAccess.Write, FileShare.None, 131072, true);
                byte[] buffer = new byte[131072]; long count = 0; int read;
                while ((read = await input.ReadAsync(buffer, cancellation)) != 0) { count += read; if (count > release.Manifest.Size) throw new InvalidDataException("下載超過預期大小。"); await output.WriteAsync(buffer.AsMemory(0, read), cancellation); progress?.Report(new(localFile == null ? "下載遊戲與執行環境" : "複製本機封裝", count * 78d / release.Manifest.Size, count, release.Manifest.Size)); }
                if (count != release.Manifest.Size) throw new InvalidDataException("下載未完成；已安裝版本仍保留。");
            }
            progress?.Report(new("SHA-256 驗證", 80));
            await using (var input = File.OpenRead(package)) if (!Convert.ToHexString(await SHA256.HashDataAsync(input, cancellation)).Equals(release.Manifest.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("SHA-256 不符；更新未啟用。");
            await Task.Run(() => { if (release.Manifest.AssetName.EndsWith(".atr", StringComparison.OrdinalIgnoreCase)) AtrArchive.Extract(package, staging, p => progress?.Report(new("矩陣茶 ATR 解封裝", 82 + p * 12)), cancellation); else ExtractZip(package, staging, progress, cancellation); }, cancellation);
            foreach (string name in new[] { "ClickerGame.exe", "ClickerGame.dll", "ClickerGame.runtimeconfig.json", "coreclr.dll", "hostfxr.dll", "hostpolicy.dll", "System.Private.CoreLib.dll", "SDL2.dll", "soft_oal.dll" }) if (!File.Exists(Path.Combine(staging, name))) throw new InvalidDataException("遊戲／自帶環境缺少 " + name + "；請使用完整 Windows 發布封裝。");
            string metadata = Path.Combine(staging, "rhythmclicker-package.json");
            if (File.Exists(metadata))
            {
                using var data = JsonDocument.Parse(await File.ReadAllTextAsync(metadata, cancellation)); if (data.RootElement.GetProperty("gameId").GetString() != "RhythmClicker") throw new InvalidDataException("此封裝不屬於 RhythmClicker。");
                string version = data.RootElement.GetProperty("version").GetString() ?? "";
                if (localFile == null && version != release.Manifest.Version) throw new InvalidDataException("封裝版本不符。");
                release = release with { Manifest = release.Manifest with { Version = version } }; ReleaseClient.Validate(release.Manifest);
            }
            else if (release.Manifest.AssetName.EndsWith(".atr", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("ATR 缺少 RhythmClicker 封裝描述。");
            else if (localFile != null && File.Exists(Path.Combine(staging, "version.txt"))) { release = release with { Manifest = release.Manifest with { Version = (await File.ReadAllTextAsync(Path.Combine(staging, "version.txt"), cancellation)).Trim() } }; ReleaseClient.Validate(release.Manifest); }
            progress?.Report(new("部署 .NET／圖形／音訊環境", 96));
            await RuntimeSetup.VerifyAsync(staging, Path.Combine(job, "probe-data"), cancellation);
            // Legacy 0.6.0 reads install_path.txt; new games prefer the relocatable JSON marker.
            InstallPaths.WriteJson(Path.Combine(staging, "rhythmclicker-storage.json"), new { dataDirectory = "../../UserData" });
            File.WriteAllText(Path.Combine(staging, "install_path.txt"), InstallPaths.Within(root, "UserData"));
            Directory.CreateDirectory(InstallPaths.Within(root, "UserData")); cancellation.ThrowIfCancellationRequested();
            string namePart = release.Manifest.Version + "-" + release.Manifest.Sha256[..12] + "-" + Guid.NewGuid().ToString("N")[..8]; string destination = InstallPaths.Within(root, "Game/" + namePart);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); Directory.Move(staging, destination);
            var installed = new InstalledGame(release.Manifest.Version, namePart, release.Manifest.DisplayName, release.Manifest.Sha256);
            InstallPaths.WriteJson(InstallPaths.Within(root, ".launcher/install.json"), installed); progress?.Report(new("安裝完成 · 玩家資料保留於所選硬碟", 100)); return installed;
        }
        finally
        {
            string verified = InstallPaths.Within(root, Path.GetRelativePath(root, job)); if (Directory.Exists(verified)) Directory.Delete(verified, true);
        }
    }
    private static void ExtractZip(string source, string destination, IProgress<InstallProgress>? progress, CancellationToken cancellation)
    {
        Directory.CreateDirectory(destination); using var zip = ZipFile.OpenRead(source); long expanded = 0; int index = 0; var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (zip.Entries.Count is 0 or > 20000) throw new InvalidDataException("ZIP 檔案數量異常。");
        foreach (var entry in zip.Entries)
        {
            cancellation.ThrowIfCancellationRequested(); string relative = entry.FullName.Replace('\\', '/');
            if (relative.StartsWith('/') || relative.Split('/').Any(p => p is "." or ".." || p.Contains(':')) || ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000 || !paths.Add(relative.TrimEnd('/'))) throw new InvalidDataException("ZIP 含有路徑越界、連結或重複檔案。");
            string target = InstallPaths.Within(destination, relative); expanded = checked(expanded + entry.Length); if (expanded > 4L * 1024 * 1024 * 1024) throw new InvalidDataException("ZIP 展開大小異常。");
            if (relative.EndsWith('/')) Directory.CreateDirectory(target);
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!); using var input = entry.Open(); using var output = new FileStream(target, FileMode.CreateNew); byte[] buffer = new byte[131072]; long actual = 0; int read;
                while ((read = input.Read(buffer)) != 0) { cancellation.ThrowIfCancellationRequested(); actual += read; if (actual > entry.Length) throw new InvalidDataException("ZIP 長度超出預期。"); output.Write(buffer, 0, read); } if (actual != entry.Length) throw new InvalidDataException("ZIP 長度不符。");
            }
            progress?.Report(new("安裝遊戲與自帶環境", 82 + ++index * 12d / zip.Entries.Count));
        }
    }
}
public static class GameLaunch
{
    public static ProcessStartInfo CreateStartInfo(string chosenRoot)
    {
        string root = InstallPaths.Root(chosenRoot); var game = InstallPaths.Read(root) ?? throw new IOException("此資料夾尚未安裝遊戲。"); string directory = InstallPaths.Within(root, "Game/" + game.DirectoryName);
        var info = new ProcessStartInfo(Path.Combine(directory, "ClickerGame.exe")) { WorkingDirectory = directory, UseShellExecute = false };
        info.Environment["RHYTHMCLICKER_DATA_ROOT"] = InstallPaths.Within(root, "UserData"); return info;
    }
}
