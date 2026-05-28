using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace ClickerGame.UI
{
    /// <summary>
    /// Loads and caches icon textures from Assets/Icons/ and maps menu-key
    /// strings to their respective Texture2D.
    ///
    /// Usage:
    ///   iconRegistry = new IconRegistry(GraphicsDevice);
    ///   iconRegistry.Load();
    ///   Texture2D? icon = iconRegistry.GetIcon("menu_start");
    /// </summary>
    public sealed class IconRegistry : IDisposable
    {
        // ── Icon file mapping (menu key → filename in Assets/Icons/) ──────────────
        private static readonly Dictionary<string, string> KeyToFile = new(StringComparer.OrdinalIgnoreCase)
        {
            ["menu_start"]        = "icon_play.png",
            ["menu_editor"]       = "icon_edit.png",
            ["menu_stats"]        = "icon_stats.png",
            ["menu_profile"]      = "icon_profile.png",
            ["menu_search"]       = "icon_search.png",
            ["menu_settings"]     = "icon_settings.png",
            ["menu_achievements"] = "icon_achievements.png",
            ["menu_account"]      = "icon_account.png",
            ["menu_language"]     = "icon_language.png",
            ["menu_exit"]         = "icon_exit.png",
            ["menu_update"]       = "icon_update.png",
            ["icon_download"]     = "icon_download.png",
        };

        private readonly GraphicsDevice _gd;
        private readonly Dictionary<string, Texture2D> _cache = new(StringComparer.OrdinalIgnoreCase);

        public IconRegistry(GraphicsDevice gd) => _gd = gd;

        // ── Load ─────────────────────────────────────────────────────────────────

        /// <summary>Loads all registered icons from disk.  Missing files are skipped.</summary>
        public void Load()
        {
            string iconsDir = Core.AppPaths.IconsPath;
            if (!Directory.Exists(iconsDir)) return;

            foreach (var (key, file) in KeyToFile)
            {
                string path = Path.Combine(iconsDir, file);
                if (!File.Exists(path)) continue;

                try
                {
                    using var fs = File.OpenRead(path);
                    var tex = Texture2D.FromStream(_gd, fs);
                    _cache[key] = tex;
                }
                catch
                {
                    // Non-fatal — icon simply won't show.
                }
            }
        }

        // ── Query ─────────────────────────────────────────────────────────────────

        /// <param name="key">A menu-key string like "menu_start" or a named icon key.</param>
        /// <returns>The loaded texture, or null if unavailable.</returns>
        public Texture2D? GetIcon(string key) =>
            _cache.TryGetValue(key, out var tex) ? tex : null;

        public bool HasIcon(string key) => _cache.ContainsKey(key);

        // ── IDisposable ───────────────────────────────────────────────────────────
        public void Dispose()
        {
            foreach (var tex in _cache.Values)
                tex.Dispose();
            _cache.Clear();
        }
    }
}
