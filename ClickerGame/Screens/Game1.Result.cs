using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        void UpdateResult()
        {
            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up)) resultMenuIndex = (resultMenuIndex - 1 + 3) % 3;
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down)) resultMenuIndex = (resultMenuIndex + 1) % 3;
            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
            {
                if (resultMenuIndex == 0) StartPlaying(false);
                else if (resultMenuIndex == 1)
                {
                    // Watch replay
                    string songId = songs.Count > 0 ? songs[currentSongIndex].Id : "unknown";
                    var replay = replayManager?.GetBestReplay(songId, currentDifficulty);
                    if (replay != null) StartReplayView(replay);
                }
                else { state = GameState.Menu; ExitBorderlessFullscreen(); menuMusicInstance?.Play(); discordRpc?.SetMenu(); }
            }
        }


        // ═══════════ Result ═══════════

        void DrawResult()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);
            int cx = width / 2;
            int cardW = 320, cardH = 340;
            int cardX = cx - cardW / 2, cardY = (height - cardH) / 2;

            var cr = new Rectangle(cardX, cardY, cardW, cardH);
            spriteBatch.Draw(pixel!, cr, new Color(14, 14, 32) * 0.96f);
            DrawRectBorder(cr, new Color(0, 200, 255) * 0.15f);

            var title = textRenderer!.GetTexture(Localization.Get("result"), "Segoe UI", 24, Color.White);
            spriteBatch.Draw(title, new Vector2(cx - title.Width / 2, cardY + 14), Color.White);
            spriteBatch.Draw(pixel!, new Rectangle(cardX + 20, cardY + 48, cardW - 40, 1), Color.White * 0.08f);

            Color gc = resultGrade switch
            { "SS" => new Color(255, 220, 50), "S" => new Color(255, 180, 0), "A" => new Color(80, 255, 120),
              "B" => new Color(0, 200, 255), "C" => new Color(180, 140, 255), _ => new Color(255, 80, 80) };
            var gt = textRenderer!.GetTexture(resultGrade, "Segoe UI", 48, gc);
            spriteBatch.Draw(gt, new Vector2(cx - gt.Width / 2, cardY + 56), Color.White);

            int sy = cardY + 120, sx = cardX + 24, sw = cardW - 48;
            int lh = 22;
            DrawStatLine(Localization.Get("score"), $"{score}/{maxScore}", sy, sx, sw);
            DrawStatLine(Localization.Get("max_combo"), $"{maxCombo}x", sy + lh, sx, sw);
            DrawStatLine(Localization.Get("hit"), $"{hitCount}", sy + lh * 2, sx, sw);
            DrawStatLine(Localization.Get("miss"), $"{missCount}", sy + lh * 3, sx, sw);
            int tn = hitCount + missCount;
            DrawStatLine(Localization.Get("accuracy"), tn > 0 ? $"{(double)hitCount / tn * 100:F1}%" : "--", sy + lh * 4, sx, sw);

            spriteBatch.Draw(pixel!, new Rectangle(cardX + 20, sy + lh * 5 + 6, cardW - 40, 1), Color.White * 0.08f);

            string[] opts = { Localization.Get("retry"), Localization.Get("watch_replay"), Localization.Get("menu") };
            int oy = sy + lh * 5 + 16;
            for (int i = 0; i < 3; i++)
            {
                bool sel = i == resultMenuIndex;
                int bx = cx - 70, by = oy + i * 32;
                if (sel)
                {
                    spriteBatch.Draw(pixel!, new Rectangle(bx, by, 140, 26), new Color(0, 200, 255) * 0.1f);
                    spriteBatch.Draw(pixel!, new Rectangle(bx, by, 3, 26), new Color(0, 200, 255));
                }
                var t = textRenderer!.GetTexture(opts[i], "Segoe UI", 15, sel ? Color.White : new Color(150, 150, 170));
                spriteBatch.Draw(t, new Vector2(bx + 12, by + (26 - t.Height) / 2), Color.White);
            }
        }

}
