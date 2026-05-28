using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        // ═══════════ Settings ═══════════

        Keys[] GetLaneKeys()
        {
            if (settingsManager == null) return new[] { Keys.D, Keys.F, Keys.J, Keys.K };
            var s = settingsManager.Settings;
            return new[]
            {
                Enum.TryParse<Keys>(s.Lane0Key, true, out var k0) ? k0 : Keys.D,
                Enum.TryParse<Keys>(s.Lane1Key, true, out var k1) ? k1 : Keys.F,
                Enum.TryParse<Keys>(s.Lane2Key, true, out var k2) ? k2 : Keys.J,
                Enum.TryParse<Keys>(s.Lane3Key, true, out var k3) ? k3 : Keys.K,
            };
        }

        string[] GetLaneKeyLabels()
        {
            var keys = GetLaneKeys();
            return keys.Select(k => k.ToString()).ToArray();
        }

        void UpdateSettings()
        {
            var s = settingsManager!.Settings;
            int itemCount = 8; // master, music, sfx, offset, lane0-3

            if (settingsBindingMode)
            {
                // Waiting for key press
                foreach (Keys k in Enum.GetValues(typeof(Keys)))
                {
                    if (k == Keys.None || k == Keys.Escape) continue;
                    if (kb.IsKeyDown(k) && !prevKb.IsKeyDown(k))
                    {
                        string kn = k.ToString();
                        switch (settingsBindingLane)
                        {
                            case 0: s.Lane0Key = kn; break;
                            case 1: s.Lane1Key = kn; break;
                            case 2: s.Lane2Key = kn; break;
                            case 3: s.Lane3Key = kn; break;
                        }
                        settingsBindingMode = false;
                        settingsManager.Save();
                        SyncSettingsToCloud();
                        return;
                    }
                }
                return;
            }

            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up))
                settingsMenuIndex = (settingsMenuIndex - 1 + itemCount) % itemCount;
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down))
                settingsMenuIndex = (settingsMenuIndex + 1) % itemCount;

            if (kb.IsKeyDown(Keys.Left) && !prevKb.IsKeyDown(Keys.Left))
            {
                switch (settingsMenuIndex)
                {
                    case 0: s.MasterVolume = Math.Max(0, s.MasterVolume - 0.05f); break;
                    case 1: s.MusicVolume = Math.Max(0, s.MusicVolume - 0.05f); break;
                    case 2: s.SfxVolume = Math.Max(0, s.SfxVolume - 0.05f); break;
                    case 3: s.OffsetMs -= 5; break;
                }
                ApplyVolume();
                settingsManager.Save();
                SyncSettingsToCloud();
            }
            if (kb.IsKeyDown(Keys.Right) && !prevKb.IsKeyDown(Keys.Right))
            {
                switch (settingsMenuIndex)
                {
                    case 0: s.MasterVolume = Math.Min(1, s.MasterVolume + 0.05f); break;
                    case 1: s.MusicVolume = Math.Min(1, s.MusicVolume + 0.05f); break;
                    case 2: s.SfxVolume = Math.Min(1, s.SfxVolume + 0.05f); break;
                    case 3: s.OffsetMs += 5; break;
                }
                ApplyVolume();
                settingsManager.Save();
                SyncSettingsToCloud();
            }
            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
            {
                if (settingsMenuIndex >= 4 && settingsMenuIndex <= 7)
                {
                    settingsBindingMode = true;
                    settingsBindingLane = settingsMenuIndex - 4;
                }
            }
        }

        void ApplyVolume()
        {
            if (menuMusicInstance != null && settingsManager != null)
                menuMusicInstance.Volume = settingsManager.Settings.MusicVolume * settingsManager.Settings.MasterVolume;
        }

        void DrawSettings()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);
            int cx = width / 2;
            int cardW = 420, cardH = 400;
            int cardX = cx - cardW / 2, cardY = (height - cardH) / 2;

            spriteBatch.Draw(pixel!, new Rectangle(cardX, cardY, cardW, cardH), new Color(14, 14, 32) * 0.96f);
            DrawRectBorder(new Rectangle(cardX, cardY, cardW, cardH), new Color(0, 200, 255) * 0.15f);

            var title = textRenderer!.GetTexture(Localization.Get("settings_title"), "Segoe UI", 22, Color.White);
            spriteBatch.Draw(title, new Vector2(cx - title.Width / 2, cardY + 14), Color.White);
            spriteBatch.Draw(pixel!, new Rectangle(cardX + 20, cardY + 46, cardW - 40, 1), Color.White * 0.08f);

            var s = settingsManager!.Settings;
            int sy = cardY + 56, sx = cardX + 24, sw = cardW - 48;
            int lh = 36;

            // Volume sliders
            DrawSettingsSlider(Localization.Get("master_volume"), s.MasterVolume, sy, sx, sw, settingsMenuIndex == 0);
            DrawSettingsSlider(Localization.Get("music_volume"), s.MusicVolume, sy + lh, sx, sw, settingsMenuIndex == 1);
            DrawSettingsSlider(Localization.Get("sfx_volume"), s.SfxVolume, sy + lh * 2, sx, sw, settingsMenuIndex == 2);

            // Offset
            bool selOffset = settingsMenuIndex == 3;
            var offLabel = textRenderer!.GetTexture(Localization.Get("offset_ms"), "Segoe UI", 13,
                selOffset ? Color.White : new Color(120, 120, 150));
            spriteBatch.Draw(offLabel, new Vector2(sx, sy + lh * 3), Color.White);
            var offVal = textRenderer!.GetTexture($"{s.OffsetMs}ms", "Segoe UI", 13,
                selOffset ? new Color(0, 200, 255) : Color.White);
            spriteBatch.Draw(offVal, new Vector2(sx + sw - offVal.Width, sy + lh * 3), Color.White);
            if (selOffset)
            {
                spriteBatch.Draw(pixel!, new Rectangle(sx, sy + lh * 3 - 2, sw, 20), new Color(0, 200, 255) * 0.05f);
                spriteBatch.Draw(pixel!, new Rectangle(sx, sy + lh * 3 - 2, 3, 20), new Color(0, 200, 255));
            }

            spriteBatch.Draw(pixel!, new Rectangle(cardX + 20, sy + lh * 4 - 4, cardW - 40, 1), Color.White * 0.08f);

            // Key bindings
            var bindTitle = textRenderer!.GetTexture(Localization.Get("key_bindings"), "Segoe UI", 14, new Color(0, 200, 255));
            spriteBatch.Draw(bindTitle, new Vector2(sx, sy + lh * 4 + 4), Color.White);

            string[] laneLabels = { "Lane 1", "Lane 2", "Lane 3", "Lane 4" };
            string[] laneKeys = { s.Lane0Key, s.Lane1Key, s.Lane2Key, s.Lane3Key };
            for (int i = 0; i < 4; i++)
            {
                int ky = sy + lh * 4 + 30 + i * 28;
                bool sel = settingsMenuIndex == 4 + i;
                bool binding = settingsBindingMode && settingsBindingLane == i;

                var ll = textRenderer!.GetTexture(laneLabels[i], "Segoe UI", 13,
                    sel ? Color.White : new Color(120, 120, 150));
                spriteBatch.Draw(ll, new Vector2(sx, ky), Color.White);

                string keyText = binding ? Localization.Get("press_key") : laneKeys[i];
                Color keyColor = binding ? new Color(255, 220, 50) : sel ? new Color(0, 200, 255) : Color.White;
                var kt = textRenderer!.GetTexture(keyText, "Segoe UI", 13, keyColor);
                spriteBatch.Draw(kt, new Vector2(sx + sw - kt.Width, ky), Color.White);

                if (sel)
                {
                    spriteBatch.Draw(pixel!, new Rectangle(sx, ky - 2, sw, 20), new Color(0, 200, 255) * 0.05f);
                    spriteBatch.Draw(pixel!, new Rectangle(sx, ky - 2, 3, 20), new Color(0, 200, 255));
                }
            }

            // Hint
            var hint = textRenderer!.GetTexture(Localization.Get("hint_settings"), "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(hint, new Vector2(cx - hint.Width / 2, cardY + cardH - 22), Color.White);
        }

        void DrawSettingsSlider(string label, float value, int y, int x, int w, bool selected)
        {
            var lt = textRenderer!.GetTexture(label, "Segoe UI", 13,
                selected ? Color.White : new Color(120, 120, 150));
            spriteBatch!.Draw(lt, new Vector2(x, y), Color.White);

            int barX = x + 160, barW = w - 200, barY = y + 5, barH = 8;
            spriteBatch.Draw(pixel!, new Rectangle(barX, barY, barW, barH), Color.White * 0.08f);
            int fillW = (int)(barW * Math.Clamp(value, 0, 1));
            spriteBatch.Draw(pixel!, new Rectangle(barX, barY, fillW, barH),
                selected ? new Color(0, 200, 255) : new Color(80, 180, 220));

            var vt = textRenderer!.GetTexture($"{(int)(value * 100)}%", "Segoe UI", 12,
                selected ? new Color(0, 200, 255) : Color.White);
            spriteBatch.Draw(vt, new Vector2(x + w - vt.Width, y), Color.White);

            if (selected)
            {
                spriteBatch.Draw(pixel!, new Rectangle(x, y - 2, w, 20), new Color(0, 200, 255) * 0.05f);
                spriteBatch.Draw(pixel!, new Rectangle(x, y - 2, 3, 20), new Color(0, 200, 255));
            }
        }

}
