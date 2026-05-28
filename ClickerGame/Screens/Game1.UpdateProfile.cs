using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        void UpdateProfile()
        {
            if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
            { state = GameState.Menu; return; }
            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up)) profileScrollIndex = Math.Max(0, profileScrollIndex - 1);
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down)) profileScrollIndex++;
            // E = Edit profile (only own profile)
            if (kb.IsKeyDown(Keys.E) && !prevKb.IsKeyDown(Keys.E))
            {
                var user = accountsManager?.LoggedInUser;
                if (user != null && viewingProfile != null && string.Equals(viewingProfile.User, user, StringComparison.OrdinalIgnoreCase))
                {
                    editProfileFieldIndex = 0;
                    editBio = viewingProfile.Bio ?? "";
                    editRegion = viewingProfile.Region ?? "";
                    editAvatarIndex = Math.Max(0, Array.FindIndex(AvatarPresets, a => a.id == viewingProfile.AvatarId));
                    editBannerIndex = Math.Max(0, Array.FindIndex(BannerPresets, b => b.id == viewingProfile.BannerId));
                    editProfileMessage = "";
                    editProfileMsgTimer = 0f;
                    editProfileSaving = false;
                    // Load custom avatar if applicable
                    var lp = LocalProfileData.Load();
                    if (!string.IsNullOrEmpty(lp.CustomAvatarPath))
                    {
                        customAvatarPath = lp.CustomAvatarPath;
                        LoadCustomAvatarTexture();
                    }
                    state = GameState.EditProfile;
                    return;
                }
            }
            if (kb.IsKeyDown(Keys.F5) && !prevKb.IsKeyDown(Keys.F5))
            {
                var user = accountsManager?.LoggedInUser;
                if (user != null && cloudSync != null)
                {
                    profileLoading = true;
                    _ = Task.Run(async () =>
                    {
                        try { viewingProfile = await cloudSync.GetProfileAsync(user); }
                        catch { }
                        finally { profileLoading = false; }
                    });
                }
            }
        }

        void DrawProfile()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);
            int cx = width / 2;
            int cardW = 520, cardH = height - 60;
            int cardX = cx - cardW / 2, cardY = 30;

            spriteBatch.Draw(pixel!, new Rectangle(cardX, cardY, cardW, cardH), new Color(14, 14, 32) * 0.96f);
            DrawRectBorder(new Rectangle(cardX, cardY, cardW, cardH), new Color(0, 200, 255) * 0.15f);

            // Banner area
            int bannerH = 80;
            Color bannerColor = new Color(30, 40, 80);
            // Apply banner preset if profile loaded
            if (viewingProfile != null)
            {
                int bnIdx = Array.FindIndex(BannerPresets, b => b.id == viewingProfile.BannerId);
                if (bnIdx >= 0) bannerColor = BannerPresets[bnIdx].color;
            }
            spriteBatch.Draw(pixel!, new Rectangle(cardX + 1, cardY + 1, cardW - 2, bannerH), bannerColor);

            if (profileLoading)
            {
                var loading = textRenderer!.GetTexture(Localization.Get("profile_loading"), "Segoe UI", 16, new Color(100, 100, 130));
                spriteBatch.Draw(loading, new Vector2(cx - loading.Width / 2, cardY + bannerH + 40), Color.White);
                return;
            }

            if (viewingProfile == null)
            {
                var noProf = textRenderer!.GetTexture(Localization.Get("profile_not_logged_in"), "Segoe UI", 16, new Color(100, 100, 130));
                spriteBatch.Draw(noProf, new Vector2(cx - noProf.Width / 2, cardY + bannerH + 40), Color.White);
                var hint = textRenderer!.GetTexture(Localization.Get("hint_profile"), "Segoe UI", 11, new Color(80, 80, 110));
                spriteBatch.Draw(hint, new Vector2(cx - hint.Width / 2, cardY + cardH - 22), Color.White);
                return;
            }

            var p = viewingProfile;
            int sy = cardY + bannerH + 12;
            int sx = cardX + 20;
            int sw = cardW - 40;

            // Avatar with preset style
            int avSize = 56;
            int avX = cardX + 24, avY = cardY + bannerH - avSize / 2;
            int avIdx = Array.FindIndex(AvatarPresets, a => a.id == p.AvatarId);
            var avStyle = avIdx >= 0 ? AvatarPresets[avIdx] : AvatarPresets[0];
            spriteBatch.Draw(pixel!, new Rectangle(avX - 1, avY - 1, avSize + 2, avSize + 2), avStyle.fg * 0.4f);
            DrawAvatarAt(avX, avY, avSize, p.AvatarId, p.User);

            // Username
            var nameT = textRenderer!.GetTexture(p.User, "Segoe UI", 22, Color.White);
            spriteBatch.Draw(nameT, new Vector2(avX + avSize + 12, avY + 4), Color.White);

            // Badges
            int badgeX = avX + avSize + 12;
            int badgeY = avY + 30;
            foreach (var badge in p.Badges)
            {
                Color bc = ParseHexColor(badge.BadgeColor);
                var bt = textRenderer!.GetTexture(badge.BadgeName, "Segoe UI", 11, Color.White);
                int bw = bt.Width + 12;
                spriteBatch.Draw(pixel!, new Rectangle(badgeX, badgeY, bw, 20), bc * 0.3f);
                DrawRectBorder(new Rectangle(badgeX, badgeY, bw, 20), bc * 0.6f);
                spriteBatch.Draw(bt, new Vector2(badgeX + 6, badgeY + 3), Color.White);
                badgeX += bw + 6;
            }

            // Region + join date
            sy = avY + avSize + 16;
            if (!string.IsNullOrEmpty(p.Region))
            {
                var regT = textRenderer!.GetTexture($"\ud83c\udf0d {p.Region}", "Segoe UI", 12, new Color(120, 120, 150));
                spriteBatch.Draw(regT, new Vector2(sx, sy), Color.White);
            }
            if (!string.IsNullOrEmpty(p.CreatedAt))
            {
                var joinT = textRenderer!.GetTexture($"{Localization.Get("profile_joined")}: {p.CreatedAt[..Math.Min(10, p.CreatedAt.Length)]}", "Segoe UI", 12, new Color(120, 120, 150));
                spriteBatch.Draw(joinT, new Vector2(sx + sw - 160, sy), Color.White);
            }
            sy += 22;

            // Bio
            if (!string.IsNullOrEmpty(p.Bio))
            {
                spriteBatch.Draw(pixel!, new Rectangle(sx, sy, sw, 1), Color.White * 0.06f);
                sy += 6;
                var bioT = textRenderer!.GetTexture(p.Bio.Length > 200 ? p.Bio[..200] + "..." : p.Bio, "Segoe UI", 12, new Color(180, 180, 200));
                spriteBatch.Draw(bioT, new Vector2(sx, sy), Color.White);
                sy += Math.Max(bioT.Height, 16) + 8;
            }

            // Stats section
            spriteBatch.Draw(pixel!, new Rectangle(sx, sy, sw, 1), Color.White * 0.08f);
            sy += 8;
            var statsLabel = textRenderer!.GetTexture(Localization.Get("profile_stats"), "Segoe UI", 14, new Color(0, 200, 255));
            spriteBatch.Draw(statsLabel, new Vector2(sx, sy), Color.White);
            sy += 22;

            DrawStatLine(Localization.Get("total_plays"), $"{p.TotalPlays}", sy, sx, sw); sy += 22;
            DrawStatLine(Localization.Get("best_combo"), $"{p.BestCombo}x", sy, sx, sw); sy += 22;
            DrawStatLine(Localization.Get("avg_accuracy"), $"{p.AvgAccuracy:F1}%", sy, sx, sw); sy += 22;

            // Best grades per map
            if (p.BestGrades.Count > 0)
            {
                sy += 4;
                spriteBatch.Draw(pixel!, new Rectangle(sx, sy, sw, 1), Color.White * 0.08f);
                sy += 8;
                var mapsLabel = textRenderer!.GetTexture(Localization.Get("profile_maps"), "Segoe UI", 14, new Color(0, 200, 255));
                spriteBatch.Draw(mapsLabel, new Vector2(sx, sy), Color.White);
                sy += 22;

                int maxVisible = Math.Min(p.BestGrades.Count, 12);
                int startIdx = Math.Min(profileScrollIndex, Math.Max(0, p.BestGrades.Count - maxVisible));
                for (int i = startIdx; i < Math.Min(startIdx + maxVisible, p.BestGrades.Count); i++)
                {
                    if (sy + 18 > cardY + cardH - 30) break;
                    var g = p.BestGrades[i];
                    Color gc = g.BestGrade switch
                    {
                        "SS" => new Color(255, 220, 50),
                        "S" => new Color(255, 180, 0),
                        "A" => new Color(80, 255, 120),
                        "B" => new Color(0, 200, 255),
                        "C" => new Color(180, 140, 255),
                        _ => new Color(255, 80, 80)
                    };
                    var songLine = textRenderer!.GetTexture($"{g.SongId}  {DiffShort(g.Difficulty)}", "Segoe UI", 11, new Color(160, 160, 180));
                    spriteBatch.Draw(songLine, new Vector2(sx, sy), Color.White);
                    var gradeT = textRenderer!.GetTexture(g.BestGrade, "Segoe UI", 12, gc);
                    spriteBatch.Draw(gradeT, new Vector2(sx + sw - 120, sy), Color.White);
                    var accT = textRenderer!.GetTexture($"{g.BestAccuracy:F1}%", "Segoe UI", 11, new Color(140, 140, 160));
                    spriteBatch.Draw(accT, new Vector2(sx + sw - accT.Width, sy), Color.White);
                    sy += 18;
                }
            }

            var hintP = textRenderer!.GetTexture(Localization.Get("hint_profile"), "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(hintP, new Vector2(cx - hintP.Width / 2, cardY + cardH - 22), Color.White);
        }

        // ═══════════ Edit Profile ═══════════
}
