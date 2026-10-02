// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.IO;
using System.Windows;
namespace RhythmClicker.Launcher;
public partial class App : Application
{
    private async void OnStartup(object sender, StartupEventArgs e)
    {
        bool smoke = e.Args.Contains("--smoke"); var window = new MainWindow(smoke); MainWindow = window; window.Show();
        if (smoke)
        {
            string Arg(string key, string fallback) { int i = Array.IndexOf(e.Args, key); return i >= 0 && i + 1 < e.Args.Length ? e.Args[i + 1] : fallback; }
            string output = Arg("--output", Path.Combine(AppContext.BaseDirectory, "smoke"));
            try { await window.SmokeAsync(output, Arg("--root", Path.Combine(output, "install"))); Shutdown(0); }
            catch (Exception ex) { Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "error.txt"), ex.ToString()); Shutdown(1); }
        }
    }
}
