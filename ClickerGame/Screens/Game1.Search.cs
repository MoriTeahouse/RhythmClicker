using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        // ═══════════ Search Players ═══════════
        void UpdateSearchPlayer()
        {
            bool shift = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
            if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
            { state = GameState.Menu; return; }
            if (kb.IsKeyDown(Keys.Back) && !prevKb.IsKeyDown(Keys.Back))
            { if (searchQuery.Length > 0) searchQuery = searchQuery[..^1]; return; }
            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up))
                searchSelectedIndex = Math.Max(0, searchSelectedIndex - 1);
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down))
                searchSelectedIndex = Math.Min(searchResults.Count - 1, searchSelectedIndex + 1);
            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
            {
                if (searchResults.Count > 0 && searchSelectedIndex >= 0 && searchSelectedIndex < searchResults.Count)
                {
                    // View selected player's profile
                    var sel = searchResults[searchSelectedIndex];
                    state = GameState.Profile;
                    profileScrollIndex = 0;
                    profileLoading = true;
                    viewingProfile = null;
                    if (cloudSync != null)
                    {
                        _ = Task.Run(async () =>
                        {
                            try { viewingProfile = await cloudSync.GetProfileAsync(sel.Username); }
                            catch { }
                            finally { profileLoading = false; }
                        });
                    }
                }
                else if (searchQuery.Length > 0 && cloudSync != null)
                {
                    // Search
                    searchLoading = true;
                    var q = searchQuery;
                    _ = Task.Run(async () =>
                    {
                        try { searchResults = await cloudSync.SearchPlayersAsync(q); searchSelectedIndex = 0; }
                        catch { searchResults.Clear(); }
                        finally { searchLoading = false; }
                    });
                }
                return;
            }
            foreach (Keys k in Enum.GetValues(typeof(Keys)))
            {
                if (k == Keys.None) continue;
                if (kb.IsKeyDown(k) && !prevKb.IsKeyDown(k))
                {
                    char ch = KeyToChar(k, shift);
                    if (ch != '\0') searchQuery += ch;
                }
            }
        }

        void DrawSearchPlayer()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);
            int cx = width / 2;
            int cardW = 520, cardH = height - 60;
            int cardX = cx - cardW / 2, cardY = 30;

            spriteBatch.Draw(pixel!, new Rectangle(cardX, cardY, cardW, cardH), new Color(14, 14, 32) * 0.96f);
            DrawRectBorder(new Rectangle(cardX, cardY, cardW, cardH), new Color(0, 200, 255) * 0.15f);

            var title = textRenderer!.GetTexture(Localization.Get("search_title"), "Segoe UI", 20, Color.White);
            spriteBatch.Draw(title, new Vector2(cx - title.Width / 2, cardY + 12), Color.White);

            // Search input
            int inputY = cardY + 48;
            int sx = cardX + 20, sw = cardW - 40;
            var inputBox = new Rectangle(sx, inputY, sw, 30);
            spriteBatch.Draw(pixel!, inputBox, Color.White * 0.04f);
            DrawRectBorder(inputBox, new Color(0, 200, 255) * 0.3f);
            var inputLabel = textRenderer!.GetTexture(Localization.Get("search_placeholder"), "Segoe UI", 11, new Color(80, 80, 110));
            if (searchQuery.Length == 0)
                spriteBatch.Draw(inputLabel, new Vector2(sx + 8, inputY + 7), Color.White);
            else
            {
                var qt = textRenderer!.GetTexture(searchQuery, "Segoe UI", 14, Color.White);
                spriteBatch.Draw(qt, new Vector2(sx + 8, inputY + 6), Color.White);
            }

            int ry = inputY + 40;

            if (searchLoading)
            {
                var loading = textRenderer!.GetTexture(Localization.Get("search_loading"), "Segoe UI", 14, new Color(100, 100, 130));
                spriteBatch.Draw(loading, new Vector2(cx - loading.Width / 2, ry), Color.White);
            }
            else if (searchResults.Count == 0 && searchQuery.Length > 0)
            {
                var noResult = textRenderer!.GetTexture(Localization.Get("search_no_results"), "Segoe UI", 14, new Color(100, 100, 130));
                spriteBatch.Draw(noResult, new Vector2(cx - noResult.Width / 2, ry), Color.White);
            }
            else
            {
                for (int i = 0; i < searchResults.Count; i++)
                {
                    if (ry + 60 > cardY + cardH - 30) break;
                    var r = searchResults[i];
                    bool sel = i == searchSelectedIndex;

                    var rowRect = new Rectangle(sx, ry, sw, 54);
                    if (sel)
                    {
                        spriteBatch.Draw(pixel!, rowRect, new Color(0, 200, 255) * 0.08f);
                        spriteBatch.Draw(pixel!, new Rectangle(sx, ry, 3, 54), new Color(0, 200, 255));
                    }
                    else
                        spriteBatch.Draw(pixel!, rowRect, Color.White * 0.02f);
                    DrawRectBorder(rowRect, sel ? new Color(0, 200, 255) * 0.15f : Color.White * 0.03f);

                    // Avatar with preset style
                    int avS = 36;
                    DrawAvatarAt(sx + 8, ry + 9, avS, r.AvatarId, r.Username);

                    // Name + badges
                    var nameT = textRenderer!.GetTexture(r.Username, "Segoe UI", 15, Color.White);
                    spriteBatch.Draw(nameT, new Vector2(sx + 52, ry + 6), Color.White);

                    int bx = sx + 52 + nameT.Width + 8;
                    foreach (var badge in r.Badges)
                    {
                        Color bc = ParseHexColor(badge.BadgeColor);
                        var bt = textRenderer!.GetTexture(badge.BadgeName, "Segoe UI", 9, Color.White);
                        int bw = bt.Width + 8;
                        spriteBatch.Draw(pixel!, new Rectangle(bx, ry + 9, bw, 16), bc * 0.3f);
                        DrawRectBorder(new Rectangle(bx, ry + 9, bw, 16), bc * 0.5f);
                        spriteBatch.Draw(bt, new Vector2(bx + 4, ry + 11), Color.White);
                        bx += bw + 4;
                    }

                    // Stats line
                    string statsLine = $"{Localization.Get("total_plays")}: {r.TotalPlays}   {Localization.Get("best_combo")}: {r.BestCombo}x   {Localization.Get("avg_accuracy")}: {r.AvgAccuracy:F1}%";
                    var statsT = textRenderer!.GetTexture(statsLine, "Segoe UI", 10, new Color(120, 120, 150));
                    spriteBatch.Draw(statsT, new Vector2(sx + 52, ry + 28), Color.White);

                    if (!string.IsNullOrEmpty(r.Region))
                    {
                        var regT = textRenderer!.GetTexture(r.Region, "Segoe UI", 10, new Color(100, 100, 130));
                        spriteBatch.Draw(regT, new Vector2(sx + sw - regT.Width - 8, ry + 8), Color.White);
                    }

                    ry += 58;
                }
            }

            var hint = textRenderer!.GetTexture(Localization.Get("hint_search"), "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(hint, new Vector2(cx - hint.Width / 2, cardY + cardH - 22), Color.White);
        }
}
