using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace ClickerLauncher
{
    /// <summary>
    /// RhythmClicker Launcher – lets the player choose an install drive/folder,
    /// downloads the game package, extracts it, and launches the game.
    ///
    /// On subsequent runs it simply checks for updates before launching.
    /// </summary>
    public partial class LauncherForm : Form
    {
        // ── Configuration ─────────────────────────────────────────────────────
        // Update this URL to point at the publicly hosted game ZIP.
        // NOTE: Set to localhost for local testing; switch back before release.
        private const string GameDownloadUrl =
#if DEBUG
            "http://localhost:9090/rc_test.zip";
#else
            "https://github.com/MoriTeahouse/RhythmClicker/releases/latest/download/RhythmClicker.zip";
#endif

        // Name of the game executable inside the extracted ZIP.
        private const string GameExeName = "ClickerGame.exe";

        // Relative marker file written by the launcher so the game knows its root.
        private const string InstallPathMarker = "install_path.txt";

        // ── UI Controls ───────────────────────────────────────────────────────
        private readonly Label       _lblTitle        = new();
        private readonly Label       _lblDrive        = new();
        private readonly ComboBox    _cbDrive         = new();
        private readonly Label       _lblFolder       = new();
        private readonly TextBox     _txtFolder       = new();
        private readonly Button      _btnBrowse       = new();
        private readonly Button      _btnInstall      = new();
        private readonly Button      _btnLaunch       = new();
        private readonly ProgressBar _progress        = new();
        private readonly Label       _lblStatus       = new();
        private readonly Panel       _panelTop        = new();

        // ── State ─────────────────────────────────────────────────────────────
        private string _installPath = "";

        // ── Constructor ───────────────────────────────────────────────────────
        public LauncherForm()
        {
            BuildUI();
            RefreshDrives();
            DetectExistingInstall();
        }

        // ── UI Layout ─────────────────────────────────────────────────────────
        private void BuildUI()
        {
            Text = "RhythmClicker Launcher";
            ClientSize = new System.Drawing.Size(500, 300);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = System.Drawing.Color.FromArgb(14, 14, 32);
            ForeColor = System.Drawing.Color.White;
            Font = new System.Drawing.Font("Segoe UI", 9f);

            // Title panel
            _panelTop.Dock = DockStyle.Top;
            _panelTop.Height = 50;
            _panelTop.BackColor = System.Drawing.Color.FromArgb(20, 20, 50);
            _lblTitle.Text = "RhythmClicker Launcher";
            _lblTitle.Font = new System.Drawing.Font("Segoe UI", 15f, System.Drawing.FontStyle.Bold);
            _lblTitle.ForeColor = System.Drawing.Color.FromArgb(0, 200, 255);
            _lblTitle.AutoSize = true;
            _lblTitle.Location = new System.Drawing.Point(14, 12);
            _panelTop.Controls.Add(_lblTitle);
            Controls.Add(_panelTop);

            // Drive label
            _lblDrive.Text = "Install drive:";
            _lblDrive.ForeColor = System.Drawing.Color.FromArgb(160, 160, 200);
            _lblDrive.Location = new System.Drawing.Point(20, 70);
            _lblDrive.AutoSize = true;

            _cbDrive.Location = new System.Drawing.Point(120, 66);
            _cbDrive.Width = 80;
            _cbDrive.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbDrive.BackColor = System.Drawing.Color.FromArgb(25, 25, 55);
            _cbDrive.ForeColor = System.Drawing.Color.White;
            _cbDrive.SelectedIndexChanged += OnDriveChanged;

            // Folder label
            _lblFolder.Text = "Install folder:";
            _lblFolder.ForeColor = System.Drawing.Color.FromArgb(160, 160, 200);
            _lblFolder.Location = new System.Drawing.Point(20, 108);
            _lblFolder.AutoSize = true;

            _txtFolder.Location = new System.Drawing.Point(120, 104);
            _txtFolder.Width = 280;
            _txtFolder.BackColor = System.Drawing.Color.FromArgb(25, 25, 55);
            _txtFolder.ForeColor = System.Drawing.Color.White;
            _txtFolder.BorderStyle = BorderStyle.FixedSingle;
            _txtFolder.TextChanged += (_, _) => _installPath = _txtFolder.Text.Trim();

            _btnBrowse.Text = "Browse…";
            _btnBrowse.Location = new System.Drawing.Point(410, 103);
            _btnBrowse.Size = new System.Drawing.Size(70, 26);
            _btnBrowse.FlatStyle = FlatStyle.Flat;
            _btnBrowse.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(0, 150, 200);
            _btnBrowse.BackColor = System.Drawing.Color.FromArgb(20, 50, 80);
            _btnBrowse.ForeColor = System.Drawing.Color.White;
            _btnBrowse.Click += OnBrowse;

            // Progress bar
            _progress.Location = new System.Drawing.Point(20, 146);
            _progress.Size = new System.Drawing.Size(460, 18);
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Minimum = 0;
            _progress.Maximum = 100;
            _progress.Value = 0;
            _progress.ForeColor = System.Drawing.Color.FromArgb(0, 200, 255);

            // Status label
            _lblStatus.Text = "Ready.";
            _lblStatus.ForeColor = System.Drawing.Color.FromArgb(120, 120, 160);
            _lblStatus.Location = new System.Drawing.Point(20, 170);
            _lblStatus.Size = new System.Drawing.Size(460, 20);

            // Install button
            _btnInstall.Text = "Download && Install";
            _btnInstall.Location = new System.Drawing.Point(20, 205);
            _btnInstall.Size = new System.Drawing.Size(150, 36);
            StyleButton(_btnInstall, System.Drawing.Color.FromArgb(0, 100, 160));
            _btnInstall.Click += OnInstall;

            // Launch button
            _btnLaunch.Text = "Launch Game ▶";
            _btnLaunch.Location = new System.Drawing.Point(185, 205);
            _btnLaunch.Size = new System.Drawing.Size(140, 36);
            StyleButton(_btnLaunch, System.Drawing.Color.FromArgb(0, 130, 80));
            _btnLaunch.Enabled = false;
            _btnLaunch.Click += OnLaunch;

            Controls.AddRange(new Control[]
            {
                _lblDrive, _cbDrive, _lblFolder, _txtFolder, _btnBrowse,
                _progress, _lblStatus, _btnInstall, _btnLaunch
            });
        }

        private static void StyleButton(Button btn, System.Drawing.Color bg)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(0, 180, 255);
            btn.BackColor = bg;
            btn.ForeColor = System.Drawing.Color.White;
            btn.Cursor = Cursors.Hand;
        }

        // ── Drive helpers ─────────────────────────────────────────────────────
        private void RefreshDrives()
        {
            _cbDrive.Items.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                    _cbDrive.Items.Add(drive.Name.TrimEnd('\\'));
            }
            if (_cbDrive.Items.Count > 0) _cbDrive.SelectedIndex = 0;
        }

        private void OnDriveChanged(object? sender, EventArgs e)
        {
            string drive = _cbDrive.SelectedItem?.ToString() ?? "C:";
            string folder = Path.Combine(drive + "\\", "RhythmClicker");
            _txtFolder.Text = folder;
        }

        private void DetectExistingInstall()
        {
            // Check local AppData default
            string localData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RhythmClicker");
            if (Directory.Exists(localData) && File.Exists(Path.Combine(localData, GameExeName)))
            {
                _txtFolder.Text = localData;
                _installPath = localData;
                _btnLaunch.Enabled = true;
                SetStatus("Existing install found.");
                return;
            }
            // Try default drive folder
            string defaultFolder = "C:\\RhythmClicker";
            _txtFolder.Text = defaultFolder;
        }

        // ── Browse ────────────────────────────────────────────────────────────
        private void OnBrowse(object? sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            fbd.Description = "Select install folder for RhythmClicker";
            fbd.SelectedPath = _txtFolder.Text;
            if (fbd.ShowDialog(this) == DialogResult.OK)
                _txtFolder.Text = fbd.SelectedPath;
        }

        // ── Install ───────────────────────────────────────────────────────────
        private async void OnInstall(object? sender, EventArgs e)
        {
            string target = _txtFolder.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                MessageBox.Show("Please choose an install folder.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnInstall.Enabled = false;
            _btnLaunch.Enabled = false;
            _installPath = target;

            try
            {
                Directory.CreateDirectory(target);
                await DownloadAndExtractAsync(target);
                WriteInstallMarker(target);
                _btnLaunch.Enabled = true;
                SetStatus("Installation complete!");
                MessageBox.Show("RhythmClicker has been installed successfully!\nClick 'Launch Game' to start.",
                    "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                MessageBox.Show($"Installation failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _btnInstall.Enabled = true;
            }
        }

        private async Task DownloadAndExtractAsync(string targetDir)
        {
            string tempZip = Path.Combine(Path.GetTempPath(), "rc_install.zip");

            // Download
            SetStatus("Downloading game package…");
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("RhythmClicker-Launcher/1.0");

            using (var response = await http.GetAsync(GameDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                long total = response.Content.Headers.ContentLength ?? -1;
                long received = 0;

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var file = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None);
                byte[] buffer = new byte[81920];
                int read;
                while ((read = await stream.ReadAsync(buffer)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read));
                    received += read;
                    if (total > 0)
                    {
                        int pct = (int)((double)received / total * 80);
                        UpdateProgress(pct, $"Downloading…  {received / 1024 / 1024:F1} MB");
                    }
                }
            }

            // Extract
            SetStatus("Extracting…");
            UpdateProgress(85, "Extracting…");
            string tempExtract = Path.Combine(Path.GetTempPath(), "rc_install_extract");
            if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);
            ZipFile.ExtractToDirectory(tempZip, tempExtract, overwriteFiles: true);

            // Copy to target
            SetStatus("Copying files…");
            UpdateProgress(92, "Copying files…");
            CopyDirectory(tempExtract, targetDir);

            // Cleanup
            try { File.Delete(tempZip); } catch { }
            try { Directory.Delete(tempExtract, true); } catch { }

            UpdateProgress(100, "Done!");
        }

        // ── Launch ────────────────────────────────────────────────────────────
        private void OnLaunch(object? sender, EventArgs e)
        {
            string exePath = Path.Combine(_installPath, GameExeName);
            if (!File.Exists(exePath))
            {
                MessageBox.Show($"Game executable not found:\n{exePath}\nPlease install first.",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Make sure the marker file is up to date so the game knows its root
            WriteInstallMarker(_installPath);

            try
            {
                Process.Start(new ProcessStartInfo(exePath)
                {
                    WorkingDirectory = _installPath,
                    UseShellExecute = true,
                });
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch game:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>Writes install_path.txt next to the game exe so it can locate its data root.</summary>
        private static void WriteInstallMarker(string installDir)
        {
            string marker = Path.Combine(installDir, InstallPathMarker);
            File.WriteAllText(marker, installDir);
        }

        private static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(source, file);
                string target = Path.Combine(dest, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }

        private void SetStatus(string msg)
        {
            if (InvokeRequired)
                Invoke(() => _lblStatus.Text = msg);
            else
                _lblStatus.Text = msg;
        }

        private void UpdateProgress(int pct, string statusMsg)
        {
            if (InvokeRequired)
                Invoke(() => { _progress.Value = Math.Clamp(pct, 0, 100); _lblStatus.Text = statusMsg; });
            else
            {
                _progress.Value = Math.Clamp(pct, 0, 100);
                _lblStatus.Text = statusMsg;
            }
        }
    }
}
