using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ClickerGame.Systems
{
    /// <summary>
    /// Checks for new game releases via a version-manifest URL and applies in-place
    /// updates.  After a successful download the caller should prompt the user to
    /// restart (use ShowRestartDialog).
    /// </summary>
    public static class UpdateManager
    {
        // ── Configuration ─────────────────────────────────────────────────────────
        // Point this at your own manifest JSON (GitHub raw, CDN, etc.).
        // The file must contain { "version": "1.2.3", "download_url": "https://..." }
        public const string ManifestUrl =
            "https://raw.githubusercontent.com/keeiv/RhythmClicker/main/version.json";

        // ── State ─────────────────────────────────────────────────────────────────
        public static bool IsUpdateAvailable { get; private set; }
        public static string? AvailableVersion { get; private set; }
        public static string? DownloadUrl { get; private set; }

        public static bool IsDownloading { get; private set; }
        public static float DownloadProgress { get; private set; }   // 0..1
        public static string StatusText { get; private set; } = "";

        // ── Events ────────────────────────────────────────────────────────────────
        public static event Action? UpdateAvailable;
        public static event Action? UpdateReady;     // called once install complete

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Checks the remote manifest in the background (fire-and-forget).</summary>
        public static void CheckAsync()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var http = new HttpClient();
                    http.Timeout = TimeSpan.FromSeconds(10);
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("RhythmClicker-Updater/1.0");

                    string json = await http.GetStringAsync(ManifestUrl);
                    var manifest = JsonSerializer.Deserialize<VersionManifest>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (manifest == null) return;

                    string current = Core.AppPaths.ReadCurrentVersion();
                    if (IsNewerVersion(manifest.Version, current))
                    {
                        IsUpdateAvailable = true;
                        AvailableVersion = manifest.Version;
                        DownloadUrl = manifest.DownloadUrl;
                        UpdateAvailable?.Invoke();
                    }
                }
                catch
                {
                    // Network errors are silently ignored – updates are non-critical.
                }
            });
        }

        /// <summary>
        /// Downloads and installs the update in the background.
        /// <para>Progress is reflected in <see cref="DownloadProgress"/>
        /// and <see cref="StatusText"/>.</para>
        /// </summary>
        public static async Task DownloadAndInstallAsync()
        {
            if (string.IsNullOrEmpty(DownloadUrl) || IsDownloading) return;

            IsDownloading = true;
            DownloadProgress = 0f;
            StatusText = "Downloading…";

            try
            {
                string tempZip = Path.Combine(Path.GetTempPath(), "rc_update.zip");
                string tempExtract = Path.Combine(Path.GetTempPath(), "rc_update_extract");

                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("RhythmClicker-Updater/1.0");

                using (var response = await http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long total = response.Content.Headers.ContentLength ?? -1;
                    long received = 0;

                    await using var stream = await response.Content.ReadAsStreamAsync();
                    await using var file = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None);
                    byte[] buffer = new byte[81920];
                    int read;
                    while ((read = await stream.ReadAsync(buffer)) > 0)
                    {
                        await file.WriteAsync(buffer.AsMemory(0, read));
                        received += read;
                        if (total > 0)
                            DownloadProgress = (float)received / total * 0.8f;
                    }
                }

                StatusText = "Installing…";
                DownloadProgress = 0.85f;

                if (Directory.Exists(tempExtract))
                    Directory.Delete(tempExtract, true);
                ZipFile.ExtractToDirectory(tempZip, tempExtract, overwriteFiles: true);

                // Copy all extracted files over the current exe directory
                string exeDir = AppContext.BaseDirectory;
                CopyDirectory(tempExtract, exeDir);

                // Write new version number
                if (AvailableVersion != null)
                    File.WriteAllText(Core.AppPaths.VersionFilePath, AvailableVersion);

                // Cleanup
                try { File.Delete(tempZip); } catch { }
                try { Directory.Delete(tempExtract, true); } catch { }

                DownloadProgress = 1f;
                StatusText = "Update complete!";
                IsDownloading = false;
                UpdateReady?.Invoke();
            }
            catch (Exception ex)
            {
                StatusText = $"Update failed: {ex.Message}";
                IsDownloading = false;
            }
        }

        /// <summary>
        /// Shows a WinForms MessageBox asking the player to restart the game.
        /// Returns true if the player clicked Yes.
        /// </summary>
        public static bool ShowRestartDialog(string localizedYes, string localizedNo, string localizedMessage, string localizedTitle)
        {
            var result = System.Windows.Forms.MessageBox.Show(
                localizedMessage,
                localizedTitle,
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Information);

            return result == System.Windows.Forms.DialogResult.Yes;
        }

        /// <summary>
        /// Restarts the current process (the game exe).
        /// Call after <see cref="ShowRestartDialog"/> returns true.
        /// </summary>
        public static void RestartGame()
        {
            string exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (!string.IsNullOrEmpty(exe))
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            Environment.Exit(0);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static bool IsNewerVersion(string? remote, string? current)
        {
            if (string.IsNullOrEmpty(remote) || string.IsNullOrEmpty(current)) return false;
            if (Version.TryParse(remote.TrimStart('v'), out var r) &&
                Version.TryParse(current.TrimStart('v'), out var c))
                return r > c;
            return false;
        }

        private static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(source, file);
                string target = Path.Combine(dest, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                // Skip the running executable to avoid file-lock issues.
                // The launcher handles exe replacement on next launch if needed.
                string fileName = Path.GetFileName(file);
                if (fileName.Equals("RhythmClicker.exe", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals("ClickerGame.exe", StringComparison.OrdinalIgnoreCase))
                    continue;
                File.Copy(file, target, overwrite: true);
            }
        }

        // ── DTO ───────────────────────────────────────────────────────────────────
        private sealed class VersionManifest
        {
            [JsonPropertyName("version")]
            public string? Version { get; set; }

            [JsonPropertyName("download_url")]
            public string? DownloadUrl { get; set; }
        }
    }
}
