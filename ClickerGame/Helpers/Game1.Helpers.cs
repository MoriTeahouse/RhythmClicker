using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame
{
    // Utility and rendering helper methods extracted from Game1.cs.
    public partial class Game1
    {
        Texture2D CreateCircleTexture(int size, Color fill)
        {
            var tex = new Texture2D(GraphicsDevice, size, size);
            var data = new Color[size * size];
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    float a = d < r - 1 ? 1f : d < r ? r - d : 0f;
                    data[y * size + x] = new Color(fill.R, fill.G, fill.B, (byte)(fill.A * a));
                }
            tex.SetData(data);
            return tex;
        }

        void DrawBackground()
        {
            if (renderCache != null)
                spriteBatch!.Draw(renderCache.GetBackground(width, height), new Rectangle(0, 0, width, height), Color.White);
        }

        void DrawStatLine(string label, string value, int y, int x, int w)
        {
            var lt = textRenderer!.GetTexture(label, "Segoe UI", 13, new Color(120, 120, 150));
            var vt = textRenderer!.GetTexture(value, "Segoe UI", 13, Color.White);
            spriteBatch!.Draw(lt, new Vector2(x, y), Color.White);
            spriteBatch.Draw(vt, new Vector2(x + w - vt.Width, y), Color.White);
        }

        void DrawRectBorder(Rectangle r, Color c)
        {
            spriteBatch!.Draw(pixel!, new Rectangle(r.Left, r.Top, r.Width, 1), c);
            spriteBatch.Draw(pixel!, new Rectangle(r.Left, r.Bottom - 1, r.Width, 1), c);
            spriteBatch.Draw(pixel!, new Rectangle(r.Left, r.Top, 1, r.Height), c);
            spriteBatch.Draw(pixel!, new Rectangle(r.Right - 1, r.Top, 1, r.Height), c);
        }

        void SpawnHitParticles(int col)
        {
            Color[] c = TierNoteColors[ComboTier];
            Color bc = c[col % 4];
            int cnt = 8 + ComboTier * 5;
            int pcx = LaneLeft + col * LaneWidth + LaneWidth / 2;
            for (int i = 0; i < cnt; i++)
            {
                float a = (float)(rng.NextDouble() * Math.PI * 2);
                float spd = 120f + (float)rng.NextDouble() * 220f + ComboTier * 35f;
                particles.Add(new HitParticle
                {
                    Pos = new Vector2(pcx + (float)(rng.NextDouble() - 0.5) * 18, HitZoneY),
                    Vel = new Vector2((float)Math.Cos(a) * spd, (float)Math.Sin(a) * spd - 120f),
                    Color = Color.Lerp(bc, Color.White, (float)rng.NextDouble() * 0.4f),
                    Life = 0.3f + (float)rng.NextDouble() * 0.35f, MaxLife = 0.65f,
                    Size = 2f + (float)rng.NextDouble() * 4f,
                });
            }
        }

        int GetUniqueSongsPlayed()
        {
            if (statsDb == null) return 0;
            var recent = statsDb.GetRecentPlays(accountsManager?.LoggedInUser, 1000);
            return recent.Select(r => r.SongId).Distinct().Count();
        }

        void SyncSettingsToCloud()
        {
            if (cloudSync == null || settingsManager == null || accountsManager?.LoggedInUser == null) return;
            var user = accountsManager.LoggedInUser;
            var settings = settingsManager.Settings;
            _ = Task.Run(async () =>
            {
                try { await cloudSync.UploadSettingsAsync(user, settings); }
                catch { }
            });
        }

        static char KeyToChar(Keys k, bool shift)
        {
            if (k >= Keys.A && k <= Keys.Z) { char c = (char)('a' + (k - Keys.A)); return shift ? char.ToUpper(c) : c; }
            if (k >= Keys.D0 && k <= Keys.D9) return (char)('0' + (k - Keys.D0));
            if (k >= Keys.NumPad0 && k <= Keys.NumPad9) return (char)('0' + (k - Keys.NumPad0));
            if (k == Keys.OemMinus) return '-';
            if (k == Keys.OemPeriod) return '.';
            if (k == Keys.Space) return ' ';
            return '\0';
        }

        static Color ParseHexColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex[0] != '#' || hex.Length < 7) return new Color(255, 215, 0);
            try
            {
                int r = Convert.ToInt32(hex.Substring(1, 2), 16);
                int g = Convert.ToInt32(hex.Substring(3, 2), 16);
                int b = Convert.ToInt32(hex.Substring(5, 2), 16);
                return new Color(r, g, b);
            }
            catch { return new Color(255, 215, 0); }
        }

        // DrawAvatarAt is in Screens/Game1.Profile.cs
        // RegisterFileAssociations is in Screens/Game1.Profile.cs
    }
}
