// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using RhythmClicker.Launcher.Core;

namespace RhythmClicker.Launcher;
public partial class MainWindow : Window
{
    private readonly HttpClient http = ReleaseClient.CreateHttpClient();
    private readonly bool smoke;
    private AvailableRelease? available;
    private CancellationTokenSource? work;
    private bool closing;
    private static string Bootstrap => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RhythmClickerLauncher", "bootstrap.json");
    public MainWindow(bool isSmoke = false)
    {
        smoke = isSmoke; InitializeComponent();
        string root = DefaultRoot();
        if (!smoke)
            try { if (File.Exists(Bootstrap) && new FileInfo(Bootstrap).Length < 8192) { using var json = JsonDocument.Parse(File.ReadAllText(Bootstrap)); root = InstallPaths.Root(json.RootElement.GetProperty("root").GetString()!); IncludeTesting.IsChecked = json.RootElement.GetProperty("includeTesting").GetBoolean(); } }
            catch (Exception ex) when (ex is IOException or ArgumentException or JsonException or InvalidOperationException or KeyNotFoundException) { }
        RootInput.Text = root; RefreshState();
        Loaded += async (_, _) => { if (!smoke) await CheckAsync(); };
        Closing += OnClosing; Closed += (_, _) => http.Dispose();
    }
    private static string DefaultRoot()
    {
        var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed && !d.Name.Equals(Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase) && d.AvailableFreeSpace > 1024L * 1024 * 1024);
        return Path.Combine(drive?.Name ?? Path.GetPathRoot(Environment.SystemDirectory)!, "Games", "RhythmClicker");
    }
    private void SavePreference()
    {
        if (!smoke) InstallPaths.WriteJson(Bootstrap, new { root = InstallPaths.Root(RootInput.Text), includeTesting = IncludeTesting.IsChecked == true });
    }
    private void RefreshState()
    {
        if (!IsInitialized) return; bool busy = work != null; InstalledGame? game = null; string? root = null;
        try { root = InstallPaths.Root(RootInput.Text); game = InstallPaths.Read(root); } catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException) { }
        InstalledLabel.Text = game == null ? "尚未安裝 · 可下載或匯入本機封裝" : "已安裝 " + game.Version;
        DataLabel.Text = root == null ? "請選擇硬碟內的實際資料夾。" : "玩家資料：" + Path.Combine(root, "UserData");
        RootInput.IsEnabled = BrowseButton.IsEnabled = IncludeTesting.IsEnabled = CheckButton.IsEnabled = LocalButton.IsEnabled = !busy;
        InstallButton.IsEnabled = !busy && available != null && root != null && (game == null || ReleaseClient.CompareVersions(available.Manifest.Version, game.Version) > 0);
        InstallButton.Content = game == null ? "下載並安裝" : "下載並更新"; PlayButton.IsEnabled = !busy && game != null; DataButton.IsEnabled = root != null; CancelButton.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }
    private async Task RunWorkAsync(Func<CancellationToken, Task> action)
    {
        if (work != null) return; work = new(); Progress.Value = 0; SizeLabel.Text = ""; RefreshState();
        try { await action(work.Token); SavePreference(); }
        catch (OperationCanceledException) { PhaseLabel.Text = "工作已取消 · 原有版本與玩家資料仍保留。"; }
        catch (Exception ex) when (ex is IOException or InvalidDataException or HttpRequestException or ArgumentException or JsonException or UnauthorizedAccessException or InvalidOperationException or KeyNotFoundException or FormatException) { PhaseLabel.Text = "無法完成：" + ex.Message; }
        finally { work.Dispose(); work = null; RefreshState(); if (closing) Close(); }
    }
    private Task CheckAsync() => RunWorkAsync(async cancellation =>
    {
        available = null; PhaseLabel.Text = "正在查詢 GitHub 公開發布…";
        available = await new ReleaseClient(http).FindAsync(IncludeTesting.IsChecked == true, cancellation);
        AvailableLabel.Text = available.Manifest.DisplayName; ReleaseNotes.Text = string.Join(Environment.NewLine, available.Manifest.Notes.Take(5));
        SizeLabel.Text = $"封裝 {available.Manifest.Size / 1024d / 1024:0.0} MB · {available.Manifest.Channel}";
        var installed = InstallPaths.Read(RootInput.Text); PhaseLabel.Text = installed != null && ReleaseClient.CompareVersions(available.Manifest.Version, installed.Version) <= 0 ? "目前已安裝版本為最新，隨時可以開始遊戲。" : "已找到可下載版本 · 封裝含遊戲與執行環境。";
    });
    private async void Check_Click(object sender, RoutedEventArgs e) => await CheckAsync();
    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (available == null) return; var release = available; string root = RootInput.Text;
        await RunWorkAsync(async cancellation => { var installed = await new Installer(http).InstallAsync(release, root, Reporter(), cancellation); PhaseLabel.Text = "安裝完成 " + installed.Version + " · 可以開始遊戲。"; });
    }
    private IProgress<InstallProgress> Reporter() => new Progress<InstallProgress>(p => { PhaseLabel.Text = p.Phase; Progress.Value = p.Percent; if (p.Total > 0) SizeLabel.Text = $"{p.Downloaded / 1024d / 1024:0.0} / {p.Total / 1024d / 1024:0.0} MB · {p.Percent:0}%"; });
    private async void Local_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "RhythmClicker 遊戲封裝|*.zip;*.atr" }; if (dialog.ShowDialog(this) != true) return; string root = RootInput.Text;
        await RunWorkAsync(async cancellation => { var installed = await new Installer(http).InstallFileAsync(dialog.FileName, root, Reporter(), cancellation); PhaseLabel.Text = "本機封裝已安裝 " + installed.Version; });
    }
    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "選擇 RhythmClicker 的安裝資料夾（所有資料都留在這裡）", Multiselect = false }; if (dialog.ShowDialog(this) == true) RootInput.Text = dialog.FolderName;
    }
    private void Root_Changed(object sender, TextChangedEventArgs e) { if (IsInitialized) RefreshState(); }
    private void Channel_Changed(object sender, RoutedEventArgs e) { if (!IsInitialized) return; available = null; AvailableLabel.Text = "下載頻道已變更，請重新檢查更新。"; ReleaseNotes.Text = ""; RefreshState(); }
    private void Cancel_Click(object sender, RoutedEventArgs e) { work?.Cancel(); PhaseLabel.Text = "正在取消與清理暫存…"; }
    private void Play_Click(object sender, RoutedEventArgs e)
    {
        try { SavePreference(); Process.Start(GameLaunch.CreateStartInfo(RootInput.Text)); PhaseLabel.Text = "遊戲已啟動 · 玩家資料使用所選硬碟。"; }
        catch (Exception ex) when (ex is IOException or Win32Exception or ArgumentException or UnauthorizedAccessException) { PhaseLabel.Text = "啟動失敗：" + ex.Message; }
    }
    private void Data_Click(object sender, RoutedEventArgs e)
    {
        try { string data = InstallPaths.Within(InstallPaths.Root(RootInput.Text), "UserData"); Directory.CreateDirectory(data); Process.Start(new ProcessStartInfo(data) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is IOException or ArgumentException or Win32Exception or UnauthorizedAccessException) { PhaseLabel.Text = ex.Message; }
    }
    private void Releases_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("https://github.com/MoriTeahouse/RhythmClicker/releases") { UseShellExecute = true });
    private void OnClosing(object? sender, CancelEventArgs e) { if (work == null) return; e.Cancel = true; closing = true; work.Cancel(); PhaseLabel.Text = "正在取消與清理暫存，完成後關閉。"; }
    public async Task SmokeAsync(string output, string root)
    {
        Directory.CreateDirectory(output); RootInput.Text = InstallPaths.Root(root);
        await CheckAsync(); if (available == null) throw new Exception("Public release discovery failed: " + PhaseLabel.Text);
        RefreshState(); await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(this); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using (var stream = File.Create(Path.Combine(output, "launcher.png"))) encoder.Save(stream);
        InstallPaths.WriteJson(Path.Combine(output, "launcher-smoke.json"), new { passed = true, publicRelease = available.Manifest.Version, installRoot = RootInput.Text, playerData = InstallPaths.Within(RootInput.Text, "UserData"), installed = InstallPaths.Read(RootInput.Text)?.Version, icon = "ClickerGame/icon.svg" });
    }
}
