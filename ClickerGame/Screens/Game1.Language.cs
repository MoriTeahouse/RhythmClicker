using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame
{
    public partial class Game1
    {
        // ═══════════ Language Screen ═══════════

        void UpdateLanguage()
        {
            int c = Localization.All.Length;
            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up)) languageMenuIndex = (languageMenuIndex - 1 + c) % c;
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down)) languageMenuIndex = (languageMenuIndex + 1) % c;
            if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
            { Localization.Current = Localization.All[languageMenuIndex]; state = GameState.Menu; }
        }

        void DrawLanguage()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);
            int cx = width / 2, lc = Localization.All.Length;
            int cardW = 300, cardH = 60 + lc * 38;
            int cardX = cx - cardW / 2, cardY = (height - cardH) / 2;

            spriteBatch.Draw(pixel!, new Rectangle(cardX, cardY, cardW, cardH), new Color(14, 14, 32) * 0.96f);
            DrawRectBorder(new Rectangle(cardX, cardY, cardW, cardH), new Color(0, 200, 255) * 0.15f);

            var title = textRenderer!.GetTexture(Localization.Get("select_language"), "Segoe UI", 20, Color.White);
            spriteBatch.Draw(title, new Vector2(cx - title.Width / 2, cardY + 14), Color.White);
            spriteBatch.Draw(pixel!, new Rectangle(cardX + 20, cardY + 44, cardW - 40, 1), Color.White * 0.08f);

            for (int i = 0; i < lc; i++)
            {
                bool sel = i == languageMenuIndex;
                bool act = Localization.All[i] == Localization.Current;
                int by = cardY + 52 + i * 38;
                if (sel)
                {
                    spriteBatch.Draw(pixel!, new Rectangle(cardX + 16, by, cardW - 32, 30), new Color(0, 200, 255) * 0.1f);
                    spriteBatch.Draw(pixel!, new Rectangle(cardX + 16, by, 3, 30), new Color(0, 200, 255));
                }
                string dn = Localization.LanguageDisplayName(Localization.All[i]) + (act ? "  \u2713" : "");
                var t = textRenderer!.GetTexture(dn, "Segoe UI", 15, sel ? Color.White : new Color(160, 160, 180));
                spriteBatch.Draw(t, new Vector2(cardX + 30, by + (30 - t.Height) / 2), Color.White);
            }

            var hint = textRenderer!.GetTexture(Localization.Get("hint_language"), "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(hint, new Vector2(cx - hint.Width / 2, cardY + cardH - 22), Color.White);
        }
    }
}
