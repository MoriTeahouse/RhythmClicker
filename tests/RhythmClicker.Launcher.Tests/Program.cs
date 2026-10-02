// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.Net;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Text.Json;
using RhythmClicker.Launcher.Core;
using MatrixTea.Engine.Packaging;

if (args.Length > 0 && args[0] == "--public-install")
{
    using var client = ReleaseClient.CreateHttpClient(); var release = await new ReleaseClient(client).FindAsync(true, default);
    var result = await new Installer(client).InstallAsync(release, args[1], new InlineProgress(p => { if (p.Percent >= 95) Console.WriteLine(p.Phase); }), default);
    Console.WriteLine(JsonSerializer.Serialize(result, ReleaseClient.Json)); return;
}
if (args.Length > 0 && args[0] == "--offline-install")
{
    using var client = ReleaseClient.CreateHttpClient(); var result = await new Installer(client).InstallFileAsync(args[1], args[2], new InlineProgress(p => { if (p.Percent >= 95) Console.WriteLine(p.Phase); }), default);
    Console.WriteLine(JsonSerializer.Serialize(result, ReleaseClient.Json)); return;
}
int checks = 0;
void Check(bool valid, string label) { checks++; if (!valid) throw new Exception(label); }
async Task RejectAsync(Func<Task> action, string label)
{
    try { await action(); } catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException or OperationCanceledException) { checks++; return; } throw new Exception(label);
}
string root = Path.Combine(Path.GetTempPath(), "RhythmClickerLauncherTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
try
{
    Check(ReleaseClient.CompareVersions("0.6.1-test.10", "0.6.1-test.2") > 0, "numeric prerelease");
    Check(ReleaseClient.CompareVersions("0.6.1", "0.6.1-test.10") > 0, "stable precedence");
    Check(ReleaseClient.CompareVersions("0.6.2-test.1", "0.6.1") > 0, "newer core testing precedence");
    await RejectAsync(() => Task.Run(() => InstallPaths.Root(Path.GetPathRoot(root)!)), "disk root rejected");
    await RejectAsync(() => Task.Run(() => InstallPaths.Within(root, "../outside")), "path traversal rejected");
    byte[] MakeZip(bool unsafePath = false)
    {
        using var output = new MemoryStream(); using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach (string file in new[] { "ClickerGame.exe", "ClickerGame.dll", "ClickerGame.runtimeconfig.json", "coreclr.dll", "hostfxr.dll", "hostpolicy.dll", "System.Private.CoreLib.dll", "SDL2.dll", "soft_oal.dll" }) { using var stream = zip.CreateEntry(file).Open(); stream.WriteByte(1); }
            if (unsafePath) { using var stream = zip.CreateEntry("../escape.txt").Open(); stream.WriteByte(1); }
        }
        return output.ToArray();
    }
    byte[] bytes = MakeZip(); string hash = Convert.ToHexString(SHA256.HashData(bytes)); string install = Path.Combine(root, "install");
    var handler = new FakeHttp { Payload = bytes }; using var http = new HttpClient(handler); var installer = new Installer(http, true);
    AvailableRelease Release(string version = "0.6.0-test.1", string? sha = null) => new(new(1, version, "RhythmClicker test", "testing", "game.zip", sha ?? hash, bytes.Length, []), new("http://localhost/game.zip"), "");
    var installed = await installer.InstallAsync(Release(), install, null, default);
    Check(InstallPaths.Read(install) == installed, "install pointer");
    string data = InstallPaths.Within(install, "UserData"); File.WriteAllText(Path.Combine(data, "settings.rc"), "player-owned");
    string pointer = File.ReadAllText(InstallPaths.Within(install, ".launcher/install.json"));
    await RejectAsync(() => installer.InstallAsync(Release("0.6.1-test.2", new string('0', 64)), install, null, default), "hash failure");
    Check(File.ReadAllText(InstallPaths.Within(install, ".launcher/install.json")) == pointer, "hash failure preserves pointer");
    using (var cancellation = new CancellationTokenSource())
    {
        var progress = new InlineProgress(p => { if (p.Percent >= 80) cancellation.Cancel(); });
        await RejectAsync(() => installer.InstallAsync(Release("0.6.1-test.2"), install, progress, cancellation.Token), "cancel");
    }
    Check(File.ReadAllText(InstallPaths.Within(install, ".launcher/install.json")) == pointer && File.ReadAllText(Path.Combine(data, "settings.rc")) == "player-owned", "cancel preserves player data and version");
    Check(!Directory.EnumerateDirectories(InstallPaths.Within(install, ".launcher/jobs")).Any(), "owned temp cleanup");
    handler.Payload = MakeZip(true); var bad = Release() with { Manifest = Release().Manifest with { Sha256 = Convert.ToHexString(SHA256.HashData(handler.Payload)), Size = handler.Payload.Length } };
    await RejectAsync(() => installer.InstallAsync(bad, install, null, default), "zip slip"); Check(!File.Exists(Path.Combine(install, ".launcher", "escape.txt")), "no escaped file");
    handler.Payload = bytes;
    using (var gate = new FileStream(InstallPaths.Within(install, ".launcher/install.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None)) await RejectAsync(() => installer.InstallAsync(Release(), install, null, default), "concurrent install rejected");
    var second = await installer.InstallAsync(Release("0.6.1-test.2"), install, null, default);
    Check(InstallPaths.Read(install) == second && Directory.Exists(InstallPaths.Within(install, "Game/" + installed.DirectoryName)), "atomic new pointer retains old version");
    Check(File.ReadAllText(Path.Combine(data, "settings.rc")) == "player-owned", "update preserves data");
    var start = GameLaunch.CreateStartInfo(install); Check(start.Environment["RHYTHMCLICKER_DATA_ROOT"] == data && start.WorkingDirectory.Contains(second.DirectoryName), "launch follows disk and version");
    string oldDirectory = InstallPaths.Within(install, "Game/" + installed.DirectoryName);
    File.WriteAllText(Path.Combine(oldDirectory, "rhythmclicker-package.json"), "{\"gameId\":\"RhythmClicker\",\"version\":\"0.6.1-test.2\"}");
    string atr = Path.Combine(root, "offline.atr"); AtrArchive.Pack(oldDirectory, atr);
    var offline = await installer.InstallFileAsync(atr, install, null, default); Check(offline.Version == "0.6.1-test.2", "offline ATR version");
    Check(File.ReadAllText(Path.Combine(data, "settings.rc")) == "player-owned", "ATR preserves data");
    handler.Api = JsonSerializer.Serialize(new[] { new { draft = false, prerelease = true, tag_name = "v0.6.1-test.2", html_url = "https://github.com/MoriTeahouse/RhythmClicker/releases/tag/v0.6.1-test.2", assets = new[] { new { name = "RhythmClicker.zip", size = bytes.Length, browser_download_url = "https://github.com/MoriTeahouse/RhythmClicker/releases/download/v0.6.1-test.2/RhythmClicker.zip" }, new { name = "RhythmClicker.zip.manifest.json", size = 200, browser_download_url = "https://github.com/MoriTeahouse/RhythmClicker/releases/download/v0.6.1-test.2/RhythmClicker.zip.manifest.json" } } } });
    handler.Manifest = JsonSerializer.Serialize(new { version = "0.6.1", sha256 = hash });
    var discovered = await new ReleaseClient(http).FindAsync(true, default); Check(discovered.Manifest.Version == "0.6.1-test.2" && discovered.Manifest.Size == bytes.Length, "legacy manifest adopts release tag and actual size");
    await RejectAsync(() => new ReleaseClient(http).FindAsync(false, default), "testing channel filter");
    await RejectAsync(() => Task.Run(() => ReleaseClient.ValidateDownloadUri(new("https://github.com/elsewhere/game/releases/download/x/game.zip"))), "foreign repo rejected");
    using (var oversized = new StringContent(new string('x', 100))) await RejectAsync(() => ReleaseClient.ReadBoundedAsync(oversized, 10, default), "metadata budget");
    Console.WriteLine($"LAUNCHER_TESTS_OK {checks} checks");
}
finally
{
    string expected = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "RhythmClickerLauncherTests")) + Path.DirectorySeparatorChar;
    if (Path.GetFullPath(root).StartsWith(expected, StringComparison.OrdinalIgnoreCase) && Directory.Exists(root)) Directory.Delete(root, true);
}
sealed class InlineProgress(Action<InstallProgress> action) : IProgress<InstallProgress> { public void Report(InstallProgress value) => action(value); }
sealed class FakeHttp : HttpMessageHandler
{
    public byte[] Payload { get; set; } = [];
    public string Api { get; set; } = "[]";
    public string Manifest { get; set; } = "{}";
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested(); HttpContent content = request.RequestUri!.Host == "api.github.com" ? new StringContent(Api) : request.RequestUri.AbsolutePath.EndsWith("manifest.json") ? new StringContent(Manifest) : new ByteArrayContent(Payload);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content, RequestMessage = request });
    }
}
