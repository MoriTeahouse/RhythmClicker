// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClickerGame
{
    /// <summary>
    /// Stores user settings (volume, key bindings, offset) in an encrypted .rc file.
    /// </summary>
    public class SettingsManager
    {
        public GameSettings Settings { get; set; } = new();
        private readonly string _path;

        public SettingsManager(string path = "settings.rc")
        {
            _path = path;
            Load();
        }

        public void Load()
        {
            if (!File.Exists(_path))
            {
                Settings = GameSettings.Default();
                return;
            }
            try
            {
                Settings = RcFileManager.ReadEncrypted<GameSettings>(_path);
                Settings ??= GameSettings.Default();
                Settings.Normalize();
            }
            catch
            {
                try { Settings = RcFileManager.ReadEncrypted<GameSettings>(_path + ".bak"); Settings.Normalize(); }
                catch { Settings = GameSettings.Default(); }
            }
        }

        public void Save()
        {
            Settings.Normalize();
            RcFileManager.WriteEncrypted(_path, Settings);
        }
    }

    public class GameSettings
    {
        public float MasterVolume { get; set; } = 0.8f;
        public float MusicVolume { get; set; } = 0.7f;
        public float SfxVolume { get; set; } = 0.8f;
        public int OffsetMs { get; set; } = 0;
        public int VisualOffsetMs { get; set; } = 0;
        public float ApproachSeconds { get; set; } = 1.6f;
        public bool PracticeMode { get; set; } = false;
        public bool ReducedEffects { get; set; } = false;

        public void Normalize()
        {
            MasterVolume = float.IsFinite(MasterVolume) ? Math.Clamp(MasterVolume, 0, 1) : 0.8f;
            MusicVolume = float.IsFinite(MusicVolume) ? Math.Clamp(MusicVolume, 0, 1) : 0.7f;
            SfxVolume = float.IsFinite(SfxVolume) ? Math.Clamp(SfxVolume, 0, 1) : 0.8f;
            OffsetMs = Math.Clamp(OffsetMs, -500, 500); VisualOffsetMs = Math.Clamp(VisualOffsetMs, -500, 500);
            ApproachSeconds = float.IsFinite(ApproachSeconds) ? Math.Clamp(ApproachSeconds, 0.6f, 3) : 1.6f;
            string[] names = { Lane0Key, Lane1Key, Lane2Key, Lane3Key };
            var used = new HashSet<Microsoft.Xna.Framework.Input.Keys>();
            foreach (string name in names)
                if (!Enum.TryParse<Microsoft.Xna.Framework.Input.Keys>(name, true, out var key) || !Enum.IsDefined(key) || key is Microsoft.Xna.Framework.Input.Keys.None or Microsoft.Xna.Framework.Input.Keys.Escape or Microsoft.Xna.Framework.Input.Keys.F11 or Microsoft.Xna.Framework.Input.Keys.Space or Microsoft.Xna.Framework.Input.Keys.Enter || !used.Add(key))
                { Lane0Key = "D"; Lane1Key = "F"; Lane2Key = "J"; Lane3Key = "K"; break; }
        }

        // Key bindings as string names (Keys enum)
        public string Lane0Key { get; set; } = "D";
        public string Lane1Key { get; set; } = "F";
        public string Lane2Key { get; set; } = "J";
        public string Lane3Key { get; set; } = "K";

        public static GameSettings Default() => new();
    }
}
