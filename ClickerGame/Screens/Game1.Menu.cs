using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        void UpdateMenu(GameTime gt)
        {
            menuTimer += (float)gt.ElapsedGameTime.TotalSeconds;

            // Mouse wheel scroll
            int scrollDeltaMenu = mouseState.ScrollWheelValue - prevScrollValue;
            if (scrollDeltaMenu != 0)
                menuScrollOffset = Math.Max(0, menuScrollOffset - scrollDeltaMenu / 40);

            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up))
                currentMenuIndex = (currentMenuIndex - 1 + menuKeys.Length) % menuKeys.Length;
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down))
                currentMenuIndex = (currentMenuIndex + 1) % menuKeys.Length;

            // Left/Right = difficulty
            if (kb.IsKeyDown(Keys.Left) && !prevKb.IsKeyDown(Keys.Left) && songs.Count > 0)
            {
                var s = songs[currentSongIndex];
                if (s.Difficulties.Count > 1)
                {
                    int idx = s.Difficulties.IndexOf(currentDifficulty);
                    idx = (idx - 1 + s.Difficulties.Count) % s.Difficulties.Count;
                    currentDifficulty = s.Difficulties[idx];
                    LoadCurrentSong();
                }
            }
            if (kb.IsKeyDown(Keys.Right) && !prevKb.IsKeyDown(Keys.Right) && songs.Count > 0)
            {
                var s = songs[currentSongIndex];
                if (s.Difficulties.Count > 1)
                {
                    int idx = s.Difficulties.IndexOf(currentDifficulty);
                    idx = (idx + 1) % s.Difficulties.Count;
                    currentDifficulty = s.Difficulties[idx];
                    LoadCurrentSong();
                }
            }

            // Tab = switch song
            if (kb.IsKeyDown(Keys.Tab) && !prevKb.IsKeyDown(Keys.Tab) && songs.Count > 1)
            {
                currentSongIndex = (currentSongIndex + 1) % songs.Count;
                var s = songs[currentSongIndex];
                if (!s.Difficulties.Contains(currentDifficulty))
                    currentDifficulty = s.Difficulties.FirstOrDefault() ?? "easy";
                LoadCurrentSong();
            }

            // ═══ Mouse click support for menu ═══
            bool mouseClicked = mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released;
            if (mouseClicked)
            {
                int mx = mouseState.X, my = mouseState.Y;
                int cx = width / 2;

                // Check menu button clicks
                int optW = 240, optH = 38, gap = 6;
                int optX = cx - optW / 2;
                // Calculate cardY based on DrawMenu layout
                var titleTexH = 48 + 8; // approximate title height
                int scroll = -menuScrollOffset;
                int titleY = 80 + scroll;
                int cardY = titleY + titleTexH + 24 + 98;

                for (int i = 0; i < menuKeys.Length; i++)
                {
                    var btn = new Rectangle(optX, cardY + i * (optH + gap), optW, optH);
                    if (mx >= btn.Left && mx <= btn.Right && my >= btn.Top && my <= btn.Bottom)
                    {
                        currentMenuIndex = i;
                        // Trigger the menu action
                        ExecuteMenuAction(menuKeys[i]);
                        return;
                    }
                }

                // Check difficulty pill clicks
                if (songs.Count > 0)
                {
                    var s = songs[currentSongIndex];
                    int pillY = titleY + titleTexH + 24 + 30;
                    int pillGap = 8;
                    int totalPW = 0;
                    var pillWidths = new int[s.Difficulties.Count];
                    for (int i = 0; i < s.Difficulties.Count; i++)
                        pillWidths[i] = DiffShort(s.Difficulties[i]).Length * 9 + 20; // approximate
                    for (int i = 0; i < s.Difficulties.Count; i++)
                        totalPW += pillWidths[i] + (i > 0 ? pillGap : 0);
                    int px = cx - totalPW / 2;
                    for (int i = 0; i < s.Difficulties.Count; i++)
                    {
                        var pillRect = new Rectangle(px, pillY, pillWidths[i], 26);
                        if (mx >= pillRect.Left && mx <= pillRect.Right && my >= pillRect.Top && my <= pillRect.Bottom)
                        {
                            currentDifficulty = s.Difficulties[i];
                            LoadCurrentSong();
                            return;
                        }
                        px += pillWidths[i] + pillGap;
                    }
                }
            }

            // Mouse hover for menu items
            {
                int mx2 = mouseState.X, my2 = mouseState.Y;
                int cx2 = width / 2;
                int optW2 = 240, optH2 = 38, gap2 = 6;
                int optX2 = cx2 - optW2 / 2;
                int scroll2 = -menuScrollOffset;
                int titleY2 = 80 + scroll2;
                int cardY2 = titleY2 + 48 + 8 + 24 + 98;
                for (int i = 0; i < menuKeys.Length; i++)
                {
                    var btn = new Rectangle(optX2, cardY2 + i * (optH2 + gap2), optW2, optH2);
                    if (mx2 >= btn.Left && mx2 <= btn.Right && my2 >= btn.Top && my2 <= btn.Bottom)
                    { currentMenuIndex = i; break; }
                }
            }

            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
                ExecuteMenuAction(menuKeys[currentMenuIndex]);
        }

        void ExecuteMenuAction(string key)
        {
                if (key == "menu_start") StartPlaying(false);
                else if (key == "menu_editor")
                {
                    state = GameState.BeatmapEditor;
                    menuMusicInstance?.Stop();
                    InitEditor();
                    discordRpc?.SetEditor("");
                }
                else if (key == "menu_stats")
                {
                    state = GameState.Stats;
                    string? user = accountsManager?.LoggedInUser;
                    cachedStats = statsDb?.GetSummary(user);
                    cachedRecent = statsDb?.GetRecentPlays(user, 8);
                    discordRpc?.SetStats();
                }
                else if (key == "menu_profile")
                {
                    state = GameState.Profile;
                    profileScrollIndex = 0;
                    var user = accountsManager?.LoggedInUser;
                    if (user != null)
                    {
                        // Build local fallback profile immediately
                        viewingProfile = new PlayerProfileDto { User = user };
                        // Apply local saved profile data
                        var lp = LocalProfileData.Load();
                        viewingProfile.AvatarId = lp.AvatarId;
                        viewingProfile.BannerId = lp.BannerId;
                        viewingProfile.Bio = lp.Bio;
                        viewingProfile.Region = lp.Region;
                        if (statsDb != null)
                        {
                            var summary = statsDb.GetSummary(user);
                            if (summary != null)
                            {
                                viewingProfile.TotalPlays = summary.TotalPlays;
                                viewingProfile.BestCombo = summary.BestCombo;
                                viewingProfile.AvgAccuracy = summary.AvgAccuracy;
                            }
                        }
                        // Try cloud fetch to enrich with badges/bio/region
                        if (cloudSync != null)
                        {
                            profileLoading = true;
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    var cloud = await cloudSync.GetProfileAsync(user);
                                    if (cloud != null) viewingProfile = cloud;
                                }
                                catch { }
                                finally { profileLoading = false; }
                            });
                        }
                    }
                    else
                    {
                        viewingProfile = null;
                    }
                }
                else if (key == "menu_search")
                {
                    state = GameState.SearchPlayer;
                    searchQuery = ""; searchResults.Clear(); searchSelectedIndex = 0; searchLoading = false;
                }
                else if (key == "menu_settings")
                {
                    state = GameState.Settings;
                    settingsMenuIndex = 0;
                    settingsBindingMode = false;
                }
                else if (key == "menu_achievements")
                {
                    state = GameState.Achievements;
                    achievementsScrollIndex = 0;
                }
                else if (key == "menu_account")
                {
                    state = GameState.Account;
                    accountUsername = ""; accountPassword = "";
                    accountShowMessage = false; accountFieldIndex = 0;
                    accountIsLoginMode = true;
                }
                else if (key == "menu_language")
                {
                    state = GameState.Language;
                    languageMenuIndex = Array.IndexOf(Localization.All, Localization.Current);
                    if (languageMenuIndex < 0) languageMenuIndex = 0;
                }
                else if (key == "menu_exit") Exit();
        }

        // ═══════════ Modern Menu ═══════════

        void DrawMenu()
        {
            int cx = width / 2;
            float pulse = (float)(0.8 + 0.2 * Math.Sin(menuTimer * 2.0));
            int scroll = -menuScrollOffset;

            // Title
            int titleY = 80 + scroll;
            var titleTex = textRenderer!.GetTexture("CLICK", "Segoe UI", 48, Color.White);
            spriteBatch!.Draw(titleTex, new Vector2(cx - titleTex.Width / 2, titleY), Color.White);

            // Thin accent line under title
            int lineW = 120;
            spriteBatch.Draw(pixel!, new Rectangle(cx - lineW / 2, titleY + titleTex.Height + 4, lineW, 2), new Color(0, 200, 255) * pulse);

            // Song selector card
            int cardY = titleY + titleTex.Height + 24;
            if (songs.Count > 0)
            {
                var s = songs[currentSongIndex];
                var songTex = textRenderer!.GetTexture(s.Title, "Segoe UI", 20, Color.White);
                spriteBatch.Draw(songTex, new Vector2(cx - songTex.Width / 2, cardY), Color.White);

                // Difficulty pills
                int pillY = cardY + 30;
                int pillGap = 8;
                var diffs = s.Difficulties;
                int totalW = 0;
                var pillWidths = new int[diffs.Count];
                for (int i = 0; i < diffs.Count; i++)
                {
                    var dt = textRenderer!.GetTexture(DiffShort(diffs[i]), "Segoe UI", 13, Color.White);
                    pillWidths[i] = dt.Width + 20;
                    totalW += pillWidths[i] + (i > 0 ? pillGap : 0);
                }
                int px = cx - totalW / 2;
                for (int i = 0; i < diffs.Count; i++)
                {
                    bool active = diffs[i] == currentDifficulty;
                    var pillRect = new Rectangle(px, pillY, pillWidths[i], 26);
                    Color pillBg = active ? new Color(0, 200, 255) * 0.2f : Color.White * 0.04f;
                    Color pillBorder = active ? new Color(0, 200, 255) * 0.5f : Color.White * 0.08f;
                    spriteBatch.Draw(pixel!, pillRect, pillBg);
                    DrawRectBorder(pillRect, pillBorder);
                    Color tc = active ? new Color(0, 200, 255) : new Color(140, 140, 160);
                    var dt = textRenderer!.GetTexture(DiffShort(diffs[i]), "Segoe UI", 13, tc);
                    spriteBatch.Draw(dt, new Vector2(pillRect.X + (pillRect.Width - dt.Width) / 2, pillRect.Y + 4), Color.White);
                    px += pillWidths[i] + pillGap;
                }

                // Song navigation hint
                var tabHint = textRenderer!.GetTexture("Tab \u25B6", "Segoe UI", 11, new Color(80, 80, 100));
                spriteBatch.Draw(tabHint, new Vector2(cx - tabHint.Width / 2, pillY + 32), Color.White);
            }

            // Menu buttons - modern flat style
            int optW = 240;
            int optH = 38;
            int gap = 6;
            int optX = cx - optW / 2;
            int optY = cardY + 98;

            // Auto-scroll to keep selected item visible
            int selBtnTop = optY + currentMenuIndex * (optH + gap);
            int selBtnBot = selBtnTop + optH;
            if (selBtnBot > height - 40)
                menuScrollOffset += selBtnBot - (height - 40);
            if (selBtnTop < 60)
                menuScrollOffset = Math.Max(0, menuScrollOffset + selBtnTop - 60);

            for (int i = 0; i < menuKeys.Length; i++)
            {
                bool sel = i == currentMenuIndex;
                var btn = new Rectangle(optX, optY + i * (optH + gap), optW, optH);

                // Skip drawing if completely off-screen
                if (btn.Bottom < 0 || btn.Top > height) continue;

                if (sel)
                {
                    spriteBatch.Draw(pixel!, btn, new Color(0, 200, 255) * 0.1f);
                    spriteBatch.Draw(pixel!, new Rectangle(btn.X, btn.Y, 3, btn.Height), new Color(0, 200, 255));
                }
                else
                    spriteBatch.Draw(pixel!, btn, Color.White * 0.03f);

                DrawRectBorder(btn, sel ? new Color(0, 200, 255) * 0.2f : Color.White * 0.04f);
                var tc2 = sel ? Color.White : new Color(160, 160, 180);

                // Icon (16×16, left-aligned with 10px margin)
                const int iconSize = 18;
                const int iconMargin = 12;
                const int textStartX = iconMargin + iconSize + 8;
                var icon = _iconRegistry?.GetIcon(menuKeys[i]);
                if (icon != null)
                {
                    int iconY = btn.Y + (btn.Height - iconSize) / 2;
                    spriteBatch.Draw(icon, new Rectangle(btn.X + iconMargin, iconY, iconSize, iconSize), tc2 * 0.9f);
                }

                var tex = textRenderer!.GetTexture(Localization.Get(menuKeys[i]), "Segoe UI", 17, tc2);
                int textX = icon != null ? btn.X + textStartX : btn.X + 18;
                spriteBatch.Draw(tex, new Vector2(textX, btn.Y + (btn.Height - tex.Height) / 2), Color.White);
            }

            // Scroll indicator
            int totalMenuH = menuKeys.Length * (optH + gap);
            int contentH = optY - scroll + totalMenuH;
            if (contentH > height)
            {
                float scrollRatio = (float)menuScrollOffset / Math.Max(1, contentH - height);
                int barH = Math.Max(20, height * height / contentH);
                int barY = (int)(scrollRatio * (height - barH));
                spriteBatch.Draw(pixel!, new Rectangle(width - 6, barY, 4, barH), Color.White * 0.15f);
            }

            // Hint bar
            var hint = textRenderer!.GetTexture(Localization.Get("hint_menu"), "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(hint, new Vector2(cx - hint.Width / 2, height - 28), Color.White);

            // Logged-in display
            if (accountsManager?.LoggedInUser != null)
            {
                var ut = textRenderer!.GetTexture($"{Localization.Get("logged_in_as")}: {accountsManager.LoggedInUser}", "Segoe UI", 12, new Color(80, 255, 120));
                spriteBatch.Draw(ut, new Vector2(width - ut.Width - 14, 12), Color.White);
            }

            // ── Update notification badge ───────────────────────────────
            if (Systems.UpdateManager.IsUpdateAvailable)
            {
                float pulse2 = (float)(0.7 + 0.3 * Math.Sin(menuTimer * 3.0));
                string badge = Systems.UpdateManager.IsDownloading
                    ? $"{Localization.Get("update_downloading")}  {Systems.UpdateManager.DownloadProgress * 100:F0}%"
                    : $"▲ {Localization.Get("menu_update")}  v{Systems.UpdateManager.AvailableVersion}";

                var badgeTex = textRenderer!.GetTexture(badge, "Segoe UI", 12, new Color(255, 220, 50));
                int bw = badgeTex.Width + 24;
                int bh = 24;
                int bx = width - bw - 14;
                int by = height - 48;
                spriteBatch.Draw(pixel!, new Rectangle(bx, by, bw, bh), new Color(80, 60, 0) * 0.8f);
                DrawRectBorder(new Rectangle(bx, by, bw, bh), new Color(255, 200, 0) * (0.5f + 0.5f * pulse2));

                // Update icon next to badge text
                var updIcon = _iconRegistry?.GetIcon("menu_update");
                if (updIcon != null)
                    spriteBatch.Draw(updIcon, new Rectangle(bx + 4, by + (bh - 14) / 2, 14, 14), new Color(255, 200, 0));
                spriteBatch.Draw(badgeTex, new Vector2(bx + (updIcon != null ? 22 : 8), by + (bh - badgeTex.Height) / 2), Color.White);
            }
        }

}
