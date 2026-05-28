using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        // ═══════════ Account ═══════════

        void DrawAccount()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);
            int cx = width / 2;
            int cardW = 360, cardH = 320;
            int cardX = cx - cardW / 2, cardY = (height - cardH) / 2;

            spriteBatch.Draw(pixel!, new Rectangle(cardX, cardY, cardW, cardH), new Color(14, 14, 32) * 0.96f);
            DrawRectBorder(new Rectangle(cardX, cardY, cardW, cardH), new Color(160, 80, 255) * 0.15f);

            string titleKey = accountIsLoginMode ? "account_login" : "account_register";
            var title = textRenderer!.GetTexture(Localization.Get(titleKey), "Segoe UI", 20, Color.White);
            spriteBatch.Draw(title, new Vector2(cx - title.Width / 2, cardY + 16), Color.White);
            spriteBatch.Draw(pixel!, new Rectangle(cardX + 20, cardY + 46, cardW - 40, 1), Color.White * 0.08f);

            int fy = cardY + 56;
            if (accountsManager?.LoggedInUser != null)
            {
                var lt = textRenderer!.GetTexture($"{Localization.Get("logged_in_as")}: {accountsManager.LoggedInUser}", "Segoe UI", 12, new Color(80, 255, 120));
                spriteBatch.Draw(lt, new Vector2(cx - lt.Width / 2, fy), Color.White);
                fy += 24;
            }

            int fx = cardX + 24, fw = cardW - 48;
            DrawTextField(Localization.Get("username"), accountUsername, fx, fy, fw, accountFieldIndex == 0, false);
            DrawTextField(Localization.Get("password"), accountPassword, fx, fy + 64, fw, accountFieldIndex == 1, true);

            var hint = textRenderer!.GetTexture(Localization.Get("hint_account"), "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(hint, new Vector2(cx - hint.Width / 2, cardY + cardH - 44), Color.White);

            if (accountShowMessage)
            {
                bool ok = accountMessage == Localization.Get("register_success") || accountMessage == Localization.Get("login_success");
                var mc = ok ? new Color(80, 255, 120) : new Color(255, 100, 100);
                var m = textRenderer!.GetTexture(accountMessage, "Segoe UI", 13, mc);
                spriteBatch.Draw(m, new Vector2(cx - m.Width / 2, cardY + cardH - 24), Color.White);
            }
        }

        void DrawTextField(string label, string value, int x, int y, int w, bool focused, bool masked)
        {
            var lb = textRenderer!.GetTexture(label, "Segoe UI", 12, new Color(120, 120, 150));
            spriteBatch!.Draw(lb, new Vector2(x, y), Color.White);
            var box = new Rectangle(x, y + 18, w, 32);
            spriteBatch.Draw(pixel!, box, focused ? Color.White * 0.06f : Color.White * 0.025f);
            DrawRectBorder(box, focused ? new Color(0, 200, 255) * 0.3f : Color.White * 0.06f);
            string disp = masked ? new string('\u2022', value.Length) : value;
            if (focused) disp += "|";
            var vt = textRenderer!.GetTexture(disp, "Segoe UI", 14, Color.White);
            spriteBatch.Draw(vt, new Vector2(box.X + 8, box.Y + 7), Color.White);
        }

}
