// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System;
using System.IO;

namespace ClickerGame.Core
{
    /// <summary>
    /// Centralises all file-system paths used by the game.
    /// The data root defaults to UserData next to the game and can be
    /// overridden by the launcher writing "install_path.txt" next to the exe.
    /// </summary>
    public static class AppPaths
    {
        public const string AppName = "RhythmClicker";
        public const string CurrentVersion = "0.6.1";

        private static string? _installRoot;

        public static string InstallRoot
        {
            get
            {
                if (_installRoot != null) return _installRoot;

                string storage = Path.Combine(AppContext.BaseDirectory, "rhythmclicker-storage.json");
                if (File.Exists(storage))
                {
                    if (new FileInfo(storage).Length > 4096) throw new InvalidDataException("Storage marker is too large.");
                    using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(storage));
                    string? relative = json.RootElement.GetProperty("dataDirectory").GetString();
                    if (relative != "../../UserData") throw new InvalidDataException("Unsupported storage marker.");
                    _installRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relative));
                    return _installRoot;
                }

                // Launcher writes install_path.txt next to the executable.
                string marker = Path.Combine(AppContext.BaseDirectory, "install_path.txt");
                if (File.Exists(marker))
                {
                    string candidate = File.ReadAllText(marker).Trim();
                    if (Path.IsPathFullyQualified(candidate))
                    {
                        _installRoot = candidate;
                        return _installRoot;
                    }
                }

                // Portable installs keep persistent data on their chosen drive.
                _installRoot = Path.Combine(AppContext.BaseDirectory, "UserData");
                return _installRoot;
            }
            set => _installRoot = Path.GetFullPath(value);
        }

        // ── Sub-directories ────────────────────────────────────────────
        public static string AssetsPath => Path.Combine(InstallRoot, "Assets");
        public static string AccountsPath => Path.Combine(InstallRoot, "Accounts");
        public static string ReplaysPath => Path.Combine(InstallRoot, "Replays");
        public static string IconsPath => Path.Combine(AppContext.BaseDirectory, "Assets", "Icons");

        // ── Key files ─────────────────────────────────────────────────
        public static string SettingsFilePath => Path.Combine(InstallRoot, "settings.rc");
        public static string AchievementsFilePath => Path.Combine(InstallRoot, "achievements.rc");
        public static string StatsDbPath => Path.Combine(InstallRoot, "stats.db");
        public static string SongsJsonPath => Path.Combine(AssetsPath, "songs.json");
        public static string LocalProfilePath => Path.Combine(InstallRoot, "profile.json");
        public static string VersionFilePath => Path.Combine(AppContext.BaseDirectory, "version.txt");

        // ── Helpers ───────────────────────────────────────────────────
        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(InstallRoot);
            Directory.CreateDirectory(AssetsPath);
            Directory.CreateDirectory(AccountsPath);
            Directory.CreateDirectory(ReplaysPath);
        }

        public static string SongPath(string songFile) => Path.Combine(AssetsPath, songFile);
        public static string BeatmapPath(string songId, string difficulty) =>
            Path.Combine(AssetsPath, $"{songId}_{difficulty}.rcm");
        public static string ReplayPath(string filename) => Path.Combine(ReplaysPath, filename);
        public static string AccountsFilePath => Path.Combine(AccountsPath, "accounts.rc");

        public static string ReadCurrentVersion()
        {
            if (File.Exists(VersionFilePath))
                return File.ReadAllText(VersionFilePath).Trim();
            return CurrentVersion;
        }
    }
}
