using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        // ═══════════ Achievements ═══════════

        void UpdateAchievements()
        {
            var all = achievementManager?.GetAll();
            if (all == null) return;
            int count = all.Count;
            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up))
                achievementsScrollIndex = Math.Max(0, achievementsScrollIndex - 1);
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down))
                achievementsScrollIndex = Math.Min(count - 1, achievementsScrollIndex + 1);
        }

        void DrawAchievements()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);
            int cx = width / 2;
            int cardW = 440, cardH = 420;
            int cardX = cx - cardW / 2, cardY = (height - cardH) / 2;

            spriteBatch.Draw(pixel!, new Rectangle(cardX, cardY, cardW, cardH), new Color(14, 14, 32) * 0.96f);
            DrawRectBorder(new Rectangle(cardX, cardY, cardW, cardH), new Color(255, 220, 50) * 0.15f);

            var title = textRenderer!.GetTexture(Localization.Get("achievements_title"), "Segoe UI", 22, Color.White);
            spriteBatch.Draw(title, new Vector2(cx - title.Width / 2, cardY + 14), Color.White);

            // Progress
            int unlocked = achievementManager?.UnlockedCount ?? 0;
            int total = achievementManager?.TotalCount ?? 0;
            var prog = textRenderer!.GetTexture($"{unlocked}/{total}", "Segoe UI", 14, new Color(255, 220, 50));
            spriteBatch.Draw(prog, new Vector2(cx - prog.Width / 2, cardY + 42), Color.White);

            spriteBatch.Draw(pixel!, new Rectangle(cardX + 20, cardY + 62, cardW - 40, 1), Color.White * 0.08f);

            var all = achievementManager?.GetAll() ?? new List<Achievement>();
            int sy = cardY + 72, sx = cardX + 24, sw = cardW - 48;
            int maxVisible = 8;
            int start = Math.Max(0, achievementsScrollIndex - maxVisible / 2);
            start = Math.Min(start, Math.Max(0, all.Count - maxVisible));

            for (int i = 0; i < maxVisible && start + i < all.Count; i++)
            {
                var ach = all[start + i];
                int ay = sy + i * 40;
                bool sel = start + i == achievementsScrollIndex;
                bool done = ach.Unlocked;

                Color bg = done ? new Color(255, 220, 50) * 0.06f : Color.White * 0.02f;
                Color border = done ? new Color(255, 220, 50) * 0.15f : Color.White * 0.04f;
                var rect = new Rectangle(sx, ay, sw, 34);
                spriteBatch.Draw(pixel!, rect, bg);
                DrawRectBorder(rect, sel ? new Color(0, 200, 255) * 0.3f : border);

                string icon = done ? "✓" : "✗";
                Color iconColor = done ? new Color(80, 255, 120) : new Color(120, 120, 150);
                var iconTex = textRenderer!.GetTexture(icon, "Segoe UI", 16, iconColor);
                spriteBatch.Draw(iconTex, new Vector2(sx + 8, ay + 4), Color.White);

                var nameTex = textRenderer!.GetTexture(Localization.Get(ach.NameKey), "Segoe UI", 13,
                    done ? Color.White : new Color(140, 140, 160));
                spriteBatch.Draw(nameTex, new Vector2(sx + 30, ay + 4), Color.White);

                var descTex = textRenderer!.GetTexture(Localization.Get(ach.DescKey), "Segoe UI", 10,
                    new Color(100, 100, 130));
                spriteBatch.Draw(descTex, new Vector2(sx + 30, ay + 20), Color.White);
            }

            var hint = textRenderer!.GetTexture(Localization.Get("hint_stats"), "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(hint, new Vector2(cx - hint.Width / 2, cardY + cardH - 22), Color.White);
        }

}
