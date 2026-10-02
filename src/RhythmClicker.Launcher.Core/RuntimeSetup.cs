// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.Diagnostics;
using System.Text.Json;

namespace RhythmClicker.Launcher.Core;
public static class RuntimeSetup
{
    public static async Task VerifyAsync(string directory, string probeData, CancellationToken cancellation)
    {
        string descriptor = Path.Combine(directory, "rhythmclicker-environment.json");
        if (!File.Exists(descriptor)) return; // Old 0.6.0 self-contained ZIP has no probe flag; required files are checked by Installer.
        if (!OperatingSystem.IsWindows() || !Environment.Is64BitOperatingSystem) throw new IOException("此遊戲封裝需要 Windows x64。");
        using var metadata = JsonDocument.Parse(await File.ReadAllTextAsync(descriptor, cancellation));
        if (metadata.RootElement.GetProperty("schemaVersion").GetInt32() != 1 || metadata.RootElement.GetProperty("platform").GetString() != "win-x64") throw new InvalidDataException("不支援的執行環境描述。");
        var info = new ProcessStartInfo(Path.Combine(directory, "ClickerGame.exe")) { WorkingDirectory = directory, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        info.ArgumentList.Add("--check-environment"); info.Environment["RHYTHMCLICKER_DATA_ROOT"] = probeData;
        using var process = Process.Start(info) ?? throw new IOException("無法啟動環境檢查。");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation); timeout.CancelAfter(TimeSpan.FromSeconds(45));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token); var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token); string text = await output, diagnostic = await error;
            if (process.ExitCode != 0 || !text.Contains("RHYTHMCLICKER_ENVIRONMENT_OK", StringComparison.Ordinal)) throw new IOException("遊戲環境檢查未通過。請確認顯示卡驅動、音訊裝置與 Windows x64 支援。" + diagnostic[..Math.Min(diagnostic.Length, 1500)]);
            InstallPaths.WriteJson(Path.Combine(directory, "rhythmclicker-environment-check.json"), new { checkedAt = DateTimeOffset.UtcNow, appLocal = true, result = text.Trim() });
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(true); try { await Task.WhenAll(output, error); } catch (OperationCanceledException) { }
            if (!cancellation.IsCancellationRequested) throw new IOException("執行環境檢查逾時；舊版本仍保留。"); throw;
        }
    }
}
