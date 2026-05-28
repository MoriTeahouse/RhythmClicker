using System.Linq;
using Microsoft.Xna.Framework;

namespace ClickerGame
{
    public partial class Game1
    {
        // ═══════════ Stats Screen ═══════════

        void DrawStats()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);
            int cx = width / 2;
            int cardW = 460, cardH = 420;
            int cardX = cx - cardW / 2, cardY = (height - cardH) / 2;

            spriteBatch.Draw(pixel!, new Rectangle(cardX, cardY, cardW, cardH), new Color(14, 14, 32) * 0.96f);
            DrawRectBorder(new Rectangle(cardX, cardY, cardW, cardH), new Color(0, 200, 255) * 0.15f);

            var title = textRenderer!.GetTexture(Localization.Get("stats_title"), "Segoe UI", 22, Color.White);
            spriteBatch.Draw(title, new Vector2(cx - title.Width / 2, cardY + 14), Color.White);
            spriteBatch.Draw(pixel!, new Rectangle(cardX + 20, cardY + 46, cardW - 40, 1), Color.White * 0.08f);

            if (cachedStats == null || cachedStats.TotalPlays == 0)
            {
                var nd = textRenderer!.GetTexture(Localization.Get("no_data"), "Segoe UI", 16, new Color(100, 100, 130));
                spriteBatch.Draw(nd, new Vector2(cx - nd.Width / 2, cardY + 80), Color.White);
            }
            else
            {
                int sx = cardX + 24, sw = cardW - 48, lh = 22;
                int sy = cardY + 56;

                DrawStatLine(Localization.Get("total_plays"), $"{cachedStats.TotalPlays}", sy, sx, sw);
                DrawStatLine(Localization.Get("avg_accuracy"), $"{cachedStats.AvgAccuracy:F1}%", sy + lh, sx, sw);
                DrawStatLine(Localization.Get("best_score"), $"{cachedStats.BestScore}", sy + lh * 2, sx, sw);
                DrawStatLine(Localization.Get("best_combo"), $"{cachedStats.BestCombo}x", sy + lh * 3, sx, sw);
                DrawStatLine(Localization.Get("total_hit"), $"{cachedStats.TotalHit}", sy + lh * 4, sx, sw);
                DrawStatLine(Localization.Get("total_miss"), $"{cachedStats.TotalMiss}", sy + lh * 5, sx, sw);

                // Grade distribution bar
                int gy = sy + lh * 6 + 8;
                var gl = textRenderer!.GetTexture(Localization.Get("grade_dist"), "Segoe UI", 13, new Color(120, 120, 150));
                spriteBatch.Draw(gl, new Vector2(sx, gy), Color.White);
                gy += 20;

                string[] grades = { "SS", "S", "A", "B", "C", "D" };
                int[] counts = { cachedStats.CountSS, cachedStats.CountS, cachedStats.CountA, cachedStats.CountB, cachedStats.CountC, cachedStats.CountD };
                Color[] gColors = { new(255,220,50), new(255,180,0), new(80,255,120), new(0,200,255), new(180,140,255), new(255,80,80) };
                int maxC = counts.Max();
                int barMax = sw - 60;
                for (int i = 0; i < grades.Length; i++)
                {
                    var gn = textRenderer!.GetTexture(grades[i], "Segoe UI", 12, gColors[i]);
                    spriteBatch.Draw(gn, new Vector2(sx, gy + i * 18), Color.White);
                    int bw = maxC > 0 ? (int)((float)counts[i] / maxC * barMax) : 0;
                    if (bw < 2 && counts[i] > 0) bw = 2;
                    spriteBatch.Draw(pixel!, new Rectangle(sx + 36, gy + i * 18 + 2, bw, 12), gColors[i] * 0.5f);
                    var cv = textRenderer!.GetTexture($"{counts[i]}", "Segoe UI", 11, new Color(160, 160, 180));
                    spriteBatch.Draw(cv, new Vector2(sx + 40 + bw, gy + i * 18 + 1), Color.White);
                }

                // Recent plays
                int ry = gy + grades.Length * 18 + 12;
                spriteBatch.Draw(pixel!, new Rectangle(cardX + 20, ry - 4, cardW - 40, 1), Color.White * 0.08f);
                var rl = textRenderer!.GetTexture(Localization.Get("recent_plays"), "Segoe UI", 13, new Color(120, 120, 150));
                spriteBatch.Draw(rl, new Vector2(sx, ry), Color.White);
                ry += 20;

                if (cachedRecent != null)
                {
                    foreach (var rec in cachedRecent.Take(5))
                    {
                        string line = $"{rec.SongId}  {DiffShort(rec.Difficulty)}  {rec.Grade}  {rec.Score}";
                        var rt = textRenderer!.GetTexture(line, "Segoe UI", 11, new Color(140, 140, 160));
                        spriteBatch.Draw(rt, new Vector2(sx, ry), Color.White);
                        ry += 16;
                    }
                }
            }

            var hint = textRenderer!.GetTexture(Localization.Get("hint_stats"), "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(hint, new Vector2(cx - hint.Width / 2, cardY + cardH - 22), Color.White);
        }
    }
}
