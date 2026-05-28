using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        // ═══════════ Edit Profile ═══════════
        void UpdateEditProfile(GameTime gameTime)
        {
            if (editProfileMsgTimer > 0)
                editProfileMsgTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Reload custom avatar texture if file picker finished
            if (_pendingAvatarReload)
            {
                _pendingAvatarReload = false;
                LoadCustomAvatarTexture();
            }

            if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
            { state = GameState.Profile; return; }

            bool shift = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);

            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up))
                editProfileFieldIndex = Math.Max(0, editProfileFieldIndex - 1);
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down))
                editProfileFieldIndex = Math.Min(4, editProfileFieldIndex + 1);
            if (kb.IsKeyDown(Keys.Tab) && !prevKb.IsKeyDown(Keys.Tab))
                editProfileFieldIndex = (editProfileFieldIndex + 1) % 5;

            // Avatar selection (Left/Right on field 0)
            if (editProfileFieldIndex == 0)
            {
                if (kb.IsKeyDown(Keys.Left) && !prevKb.IsKeyDown(Keys.Left))
                    editAvatarIndex = (editAvatarIndex - 1 + AvatarPresets.Length) % AvatarPresets.Length;
                if (kb.IsKeyDown(Keys.Right) && !prevKb.IsKeyDown(Keys.Right))
                    editAvatarIndex = (editAvatarIndex + 1) % AvatarPresets.Length;
                // Enter on Custom avatar → open file picker
                if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter) && AvatarPresets[editAvatarIndex].id == "custom")
                    OpenAvatarFilePicker();
            }
            // Banner selection (Left/Right on field 1)
            else if (editProfileFieldIndex == 1)
            {
                if (kb.IsKeyDown(Keys.Left) && !prevKb.IsKeyDown(Keys.Left))
                    editBannerIndex = (editBannerIndex - 1 + BannerPresets.Length) % BannerPresets.Length;
                if (kb.IsKeyDown(Keys.Right) && !prevKb.IsKeyDown(Keys.Right))
                    editBannerIndex = (editBannerIndex + 1) % BannerPresets.Length;
            }
            // Bio text input (field 2)
            else if (editProfileFieldIndex == 2)
            {
                if (kb.IsKeyDown(Keys.Back) && !prevKb.IsKeyDown(Keys.Back))
                { if (editBio.Length > 0) editBio = editBio[..^1]; }
                else
                {
                    foreach (var key in kb.GetPressedKeys())
                    {
                        if (prevKb.IsKeyDown(key)) continue;
                        char? c = KeyToChar(key, shift);
                        if (c.HasValue && editBio.Length < 120) editBio += c.Value;
                    }
                }
            }
            // Region text input (field 3)
            else if (editProfileFieldIndex == 3)
            {
                if (kb.IsKeyDown(Keys.Back) && !prevKb.IsKeyDown(Keys.Back))
                { if (editRegion.Length > 0) editRegion = editRegion[..^1]; }
                else
                {
                    foreach (var key in kb.GetPressedKeys())
                    {
                        if (prevKb.IsKeyDown(key)) continue;
                        char? c = KeyToChar(key, shift);
                        if (c.HasValue && editRegion.Length < 30) editRegion += c.Value;
                    }
                }
            }

            // Enter on Save (field 4) or Ctrl+S anywhere
            bool ctrlS = (kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl))
                         && kb.IsKeyDown(Keys.S) && !prevKb.IsKeyDown(Keys.S);
            if ((editProfileFieldIndex == 4 && kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter)) || ctrlS)
            {
                if (!editProfileSaving)
                {
                    var user = accountsManager?.LoggedInUser;
                    if (user != null)
                    {
                        var avatarId = AvatarPresets[editAvatarIndex].id;
                        var bannerId = BannerPresets[editBannerIndex].id;
                        var bio = editBio;
                        var region = editRegion;

                        // Update local viewingProfile immediately
                        if (viewingProfile != null)
                        {
                            viewingProfile.AvatarId = avatarId;
                            viewingProfile.BannerId = bannerId;
                            viewingProfile.Bio = bio;
                            viewingProfile.Region = region;
                        }

                        // Always save locally
                        SaveLocalProfile(avatarId, bannerId, bio, region);
                        editProfileMessage = Localization.Get("edit_profile_saved");
                        editProfileMsgTimer = 2.5f;

                        // Try cloud upload in background (non-blocking)
                        if (cloudSync != null)
                        {
                            _ = Task.Run(async () =>
                            {
                                try { await cloudSync.UploadProfileAsync(user, avatarId, bannerId, bio, region); }
                                catch { }
                            });
                        }
                    }
                }
            }
        }

        void DrawEditProfile()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.8f);
            int cx = width / 2;
            int cardW = 480, cardH = 420;
            int cardX = cx - cardW / 2, cardY = (height - cardH) / 2;

            spriteBatch.Draw(pixel!, new Rectangle(cardX, cardY, cardW, cardH), new Color(14, 14, 32) * 0.98f);
            DrawRectBorder(new Rectangle(cardX, cardY, cardW, cardH), new Color(0, 200, 255) * 0.2f);

            int sx = cardX + 24, sy = cardY + 16;
            int fieldW = cardW - 48;

            // Title
            var title = textRenderer!.GetTexture(Localization.Get("edit_profile_title"), "Segoe UI", 18, new Color(0, 200, 255));
            spriteBatch.Draw(title, new Vector2(cx - title.Width / 2, sy), Color.White);
            sy += 36;

            // ── Field 0: Avatar ──
            bool sel0 = editProfileFieldIndex == 0;
            var avLabel = textRenderer!.GetTexture(Localization.Get("edit_avatar"), "Segoe UI", 13, sel0 ? new Color(0, 200, 255) : new Color(140, 140, 160));
            spriteBatch.Draw(avLabel, new Vector2(sx, sy), Color.White);
            sy += 20;
            // Draw avatar preview
            var avPreset = AvatarPresets[editAvatarIndex];
            int avPrevSize = 48;
            int avPrevX = sx + 20;
            spriteBatch.Draw(pixel!, new Rectangle(avPrevX - 1, sy - 1, avPrevSize + 2, avPrevSize + 2), sel0 ? new Color(0, 200, 255) * 0.6f : Color.White * 0.15f);
            DrawAvatarAt(avPrevX, sy, avPrevSize, avPreset.id, accountsManager?.LoggedInUser ?? "?");
            // Name + arrows + browse button for custom
            string avLabelText = $"\u25C0  {avPreset.label}  \u25B6";
            var avNameT = textRenderer!.GetTexture(avLabelText, "Segoe UI", 14, sel0 ? Color.White : new Color(180, 180, 200));
            spriteBatch.Draw(avNameT, new Vector2(avPrevX + avPrevSize + 20, sy + 6), Color.White);
            if (avPreset.id == "custom")
            {
                string browseHint = customAvatarTexture != null ? Localization.Get("edit_avatar_change") : Localization.Get("edit_avatar_browse");
                var browseT = textRenderer!.GetTexture($"[Enter] {browseHint}", "Segoe UI", 11, sel0 ? new Color(0, 200, 255) : new Color(100, 100, 130));
                spriteBatch.Draw(browseT, new Vector2(avPrevX + avPrevSize + 20, sy + 28), Color.White);
            }
            sy += avPrevSize + 12;

            // ── Field 1: Banner ──
            bool sel1 = editProfileFieldIndex == 1;
            var bnLabel = textRenderer!.GetTexture(Localization.Get("edit_banner"), "Segoe UI", 13, sel1 ? new Color(0, 200, 255) : new Color(140, 140, 160));
            spriteBatch.Draw(bnLabel, new Vector2(sx, sy), Color.White);
            sy += 20;
            var bnPreset = BannerPresets[editBannerIndex];
            int bnW = fieldW - 40, bnH = 28;
            int bnX = sx + 20;
            spriteBatch.Draw(pixel!, new Rectangle(bnX - 1, sy - 1, bnW + 2, bnH + 2), sel1 ? new Color(0, 200, 255) * 0.6f : Color.White * 0.1f);
            spriteBatch.Draw(pixel!, new Rectangle(bnX, sy, bnW, bnH), bnPreset.color);
            var bnNameT = textRenderer!.GetTexture($"\u25C0  {bnPreset.label}  \u25B6", "Segoe UI", 12, Color.White);
            spriteBatch.Draw(bnNameT, new Vector2(bnX + (bnW - bnNameT.Width) / 2, sy + 6), Color.White);
            sy += bnH + 14;

            // ── Field 2: Bio ──
            bool sel2 = editProfileFieldIndex == 2;
            var bioLabel = textRenderer!.GetTexture(Localization.Get("edit_bio"), "Segoe UI", 13, sel2 ? new Color(0, 200, 255) : new Color(140, 140, 160));
            spriteBatch.Draw(bioLabel, new Vector2(sx, sy), Color.White);
            sy += 20;
            spriteBatch.Draw(pixel!, new Rectangle(sx + 20, sy, fieldW - 40, 26), sel2 ? new Color(30, 40, 70) : new Color(20, 24, 40));
            DrawRectBorder(new Rectangle(sx + 20, sy, fieldW - 40, 26), sel2 ? new Color(0, 200, 255) * 0.5f : Color.White * 0.08f);
            string bioDisplay = editBio + (sel2 ? "_" : "");
            var bioT = textRenderer!.GetTexture(bioDisplay.Length > 50 ? "..." + bioDisplay[^47..] : bioDisplay, "Segoe UI", 12, new Color(200, 200, 220));
            spriteBatch.Draw(bioT, new Vector2(sx + 26, sy + 5), Color.White);
            sy += 36;

            // ── Field 3: Region ──
            bool sel3 = editProfileFieldIndex == 3;
            var regLabel = textRenderer!.GetTexture(Localization.Get("edit_region"), "Segoe UI", 13, sel3 ? new Color(0, 200, 255) : new Color(140, 140, 160));
            spriteBatch.Draw(regLabel, new Vector2(sx, sy), Color.White);
            sy += 20;
            spriteBatch.Draw(pixel!, new Rectangle(sx + 20, sy, fieldW - 40, 26), sel3 ? new Color(30, 40, 70) : new Color(20, 24, 40));
            DrawRectBorder(new Rectangle(sx + 20, sy, fieldW - 40, 26), sel3 ? new Color(0, 200, 255) * 0.5f : Color.White * 0.08f);
            string regDisplay = editRegion + (sel3 ? "_" : "");
            var regT = textRenderer!.GetTexture(regDisplay.Length > 30 ? "..." + regDisplay[^27..] : regDisplay, "Segoe UI", 12, new Color(200, 200, 220));
            spriteBatch.Draw(regT, new Vector2(sx + 26, sy + 5), Color.White);
            sy += 38;

            // ── Field 4: Save button ──
            bool sel4 = editProfileFieldIndex == 4;
            string saveBtnText = editProfileSaving ? Localization.Get("edit_profile_saving") : Localization.Get("edit_profile_save");
            var saveT = textRenderer!.GetTexture(saveBtnText, "Segoe UI", 15, sel4 ? Color.White : new Color(140, 140, 160));
            int btnW = saveT.Width + 40, btnH = 32;
            int btnX = cx - btnW / 2;
            spriteBatch.Draw(pixel!, new Rectangle(btnX, sy, btnW, btnH), sel4 ? new Color(0, 140, 200) * 0.4f : new Color(30, 30, 50));
            DrawRectBorder(new Rectangle(btnX, sy, btnW, btnH), sel4 ? new Color(0, 200, 255) * 0.6f : Color.White * 0.1f);
            spriteBatch.Draw(saveT, new Vector2(cx - saveT.Width / 2, sy + 7), Color.White);
            sy += btnH + 10;

            // Status message
            if (editProfileMsgTimer > 0 && !string.IsNullOrEmpty(editProfileMessage))
            {
                Color msgColor = editProfileMessage.Contains("!") || editProfileMessage.Contains("失敗") || editProfileMessage.Contains("failed")
                    ? new Color(255, 80, 80)
                    : new Color(80, 255, 120);
                var msgT = textRenderer!.GetTexture(editProfileMessage, "Segoe UI", 12, msgColor);
                spriteBatch.Draw(msgT, new Vector2(cx - msgT.Width / 2, sy), Color.White);
            }

            // Hint
            var hint = textRenderer!.GetTexture(Localization.Get("hint_edit_profile"), "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(hint, new Vector2(cx - hint.Width / 2, cardY + cardH - 22), Color.White);
        }

        // ═══════════ Search Players ═══════════
}
