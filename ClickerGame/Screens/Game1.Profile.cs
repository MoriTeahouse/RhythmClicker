using System;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using WinForms = System.Windows.Forms;

namespace ClickerGame;

public partial class Game1
{
        // ═══════════ Account Input ═══════════
        // ═══════════ Account Input ═══════════

        void HandleAccountInput(KeyboardState kbState, KeyboardState prevKbState)
        {
            bool shift = kbState.IsKeyDown(Keys.LeftShift) || kbState.IsKeyDown(Keys.RightShift);
            if (kbState.IsKeyDown(Keys.F1) && !prevKbState.IsKeyDown(Keys.F1))
            { accountIsLoginMode = !accountIsLoginMode; accountShowMessage = false; return; }
            if (kbState.IsKeyDown(Keys.Tab) && !prevKbState.IsKeyDown(Keys.Tab))
            { accountFieldIndex = (accountFieldIndex + 1) % 2; return; }
            if (kbState.IsKeyDown(Keys.Back) && !prevKbState.IsKeyDown(Keys.Back))
            {
                if (accountFieldIndex == 0 && accountUsername.Length > 0) accountUsername = accountUsername[..^1];
                else if (accountFieldIndex == 1 && accountPassword.Length > 0) accountPassword = accountPassword[..^1];
                return;
            }
            if (kbState.IsKeyDown(Keys.Enter) && !prevKbState.IsKeyDown(Keys.Enter))
            {
                if (accountsManager != null)
                {
                    bool loginOk = false;
                    string pwHash = AccountsManager.HashPassword(accountPassword);
                    if (accountIsLoginMode)
                    {
                        if (accountsManager.Login(accountUsername, accountPassword, out _))
                        { accountShowMessage = true; accountMessage = Localization.Get("login_success"); loginOk = true; accountPassword = ""; }
                        else { accountShowMessage = true; accountMessage = "Invalid credentials"; }
                    }
                    else
                    {
                        if (accountsManager.Register(accountUsername, accountPassword, out _))
                        { accountShowMessage = true; accountMessage = Localization.Get("register_success"); loginOk = true; accountPassword = ""; }
                        else { accountShowMessage = true; accountMessage = "Username taken or invalid"; }
                    }
                    // Cloud sync after successful login/register
                    if (loginOk && cloudSync != null)
                    {
                        var user = accountsManager.LoggedInUser ?? accountUsername;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                if (!accountIsLoginMode)
                                    await cloudSync.RegisterAsync(user, pwHash);
                                else
                                    await cloudSync.LoginAsync(user, pwHash);

                                var result = await cloudSync.FullSyncAsync(user, statsDb, achievementManager, settingsManager);
                                syncStatusText = result.Success ? Localization.Get("sync_ok") : result.Message;
                                syncStatusTimer = 3f;
                            }
                            catch { syncStatusText = "Sync failed"; syncStatusTimer = 3f; }
                        });
                    }
                }
                return;
            }
            foreach (Keys k in Enum.GetValues(typeof(Keys)))
            {
                if (k == Keys.None) continue;
                if (kbState.IsKeyDown(k) && !prevKbState.IsKeyDown(k))
                {
                    char ch = KeyToChar(k, shift);
                    if (ch != '\0') { if (accountFieldIndex == 0) accountUsername += ch; else accountPassword += ch; }
                }
            }
        }

        // KeyToChar → Helpers/Game1.Helpers.cs

        void LoadLocalProfile()
        {
            var lp = LocalProfileData.Load();
            customAvatarPath = lp.CustomAvatarPath ?? "";
            // Pre-load custom avatar texture if exists
            LoadCustomAvatarTexture();
            // Apply to edit defaults
            editAvatarIndex = Math.Max(0, Array.FindIndex(AvatarPresets, a => a.id == lp.AvatarId));
            editBannerIndex = Math.Max(0, Array.FindIndex(BannerPresets, b => b.id == lp.BannerId));
            editBio = lp.Bio ?? "";
            editRegion = lp.Region ?? "";
        }

        void SaveLocalProfile(string avatarId, string bannerId, string bio, string region)
        {
            var lp = new LocalProfileData
            {
                AvatarId = avatarId,
                BannerId = bannerId,
                Bio = bio,
                Region = region,
                CustomAvatarPath = customAvatarPath
            };
            lp.Save();
        }

        void LoadCustomAvatarTexture()
        {
            customAvatarTexture?.Dispose();
            customAvatarTexture = null;
            if (!string.IsNullOrEmpty(customAvatarPath) && File.Exists(customAvatarPath))
            {
                try
                {
                    using var fs = File.OpenRead(customAvatarPath);
                    customAvatarTexture = Texture2D.FromStream(GraphicsDevice, fs);
                }
                catch { customAvatarTexture = null; }
            }
        }

        void OpenAvatarFilePicker()
        {
            // Run file dialog on STA thread (required for Windows Forms dialog)
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    Directory.CreateDirectory(AvatarsDir);
                    using var ofd = new System.Windows.Forms.OpenFileDialog();
                    ofd.Title = "Select Avatar Image";
                    ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp";
                    ofd.InitialDirectory = AvatarsDir;
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        // Copy to Avatars folder if not already there
                        string destPath = Path.Combine(AvatarsDir, Path.GetFileName(ofd.FileName));
                        if (!string.Equals(Path.GetFullPath(ofd.FileName), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
                            File.Copy(ofd.FileName, destPath, true);
                        customAvatarPath = destPath;
                        // Reload texture on next frame
                        _pendingAvatarReload = true;
                    }
                }
                catch { }
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
        }
        bool _pendingAvatarReload = false;

        void DrawAvatarAt(int x, int y, int size, string avatarId, string username)
        {
            if (avatarId == "custom" && customAvatarTexture != null)
            {
                // Draw custom image scaled to fit
                spriteBatch!.Draw(customAvatarTexture, new Rectangle(x, y, size, size), Color.White);
            }
            else
            {
                int avIdx = Array.FindIndex(AvatarPresets, a => a.id == avatarId);
                var avStyle = avIdx >= 0 ? AvatarPresets[avIdx] : AvatarPresets[0];
                spriteBatch!.Draw(pixel!, new Rectangle(x, y, size, size), avStyle.bg);
                string icon = avStyle.icon != "" ? avStyle.icon : (username.Length > 0 ? username[0].ToString().ToUpper() : "?");
                int fontSize = size > 40 ? 28 : 18;
                var charT = textRenderer!.GetTexture(icon, "Segoe UI", fontSize, avStyle.fg);
                spriteBatch.Draw(charT, new Vector2(x + (size - charT.Width) / 2, y + (size - charT.Height) / 2), Color.White);
            }
        }

        // SyncSettingsToCloud → Helpers/Game1.Helpers.cs

        /// <summary>Register .rcm / .rcp / .rc file associations with custom icons (HKCU, no admin).</summary>
        static void RegisterFileAssociations()
        {
            try
            {
                // For single-file publish, the actual exe is the host process
                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath)) exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath)) return;
                string baseDir = Path.GetDirectoryName(exePath)!;
                string iconsDir = Path.Combine(baseDir, "Icons");
                if (!Directory.Exists(iconsDir)) return;

                var associations = new (string ext, string progId, string desc, string icoFile)[]
                {
                    (".rcm", "RhythmClicker.Beatmap",  "RhythmClicker Beatmap",  "file_rcm.ico"),
                    (".rcp", "RhythmClicker.Replay",   "RhythmClicker Replay",   "file_rcp.ico"),
                    (".rc",  "RhythmClicker.Data",     "RhythmClicker Data",     "file_rc.ico"),
                };

                foreach (var (ext, progId, desc, icoFile) in associations)
                {
                    string icoPath = Path.Combine(iconsDir, icoFile);
                    if (!File.Exists(icoPath)) continue;

                    // HKCU\Software\Classes\.ext → ProgId
                    using var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ext}");
                    extKey?.SetValue("", progId);

                    // HKCU\Software\Classes\ProgId
                    using var progKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}");
                    progKey?.SetValue("", desc);

                    // DefaultIcon
                    using var iconKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\DefaultIcon");
                    iconKey?.SetValue("", $"\"{icoPath}\",0");
                }

                // Notify shell of changes
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch { /* non-critical */ }
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        // ═══════════ Profile ═══════════
}
