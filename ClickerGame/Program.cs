// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System;
using System.IO;
using System.Linq;
namespace ClickerGame;
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        string? data = Value(args, "--data-root") ?? Environment.GetEnvironmentVariable("RHYTHMCLICKER_DATA_ROOT");
        if (data != null) Core.AppPaths.InstallRoot = Path.GetFullPath(data);
        Core.AppPaths.EnsureDirectories();
        if (args.Contains("--check-environment"))
        {
            try { using var probe = new EnvironmentProbe(); probe.Run(); Console.WriteLine("RHYTHMCLICKER_ENVIRONMENT_OK graphics=OpenGL audio=OpenAL+WaveOut runtime=app-local"); return 0; }
            catch (Exception ex) { Console.Error.WriteLine(ex); File.WriteAllText(Path.Combine(Core.AppPaths.InstallRoot, "environment-error.txt"), ex.ToString()); return 1; }
        }
        // Seed content only when missing; user charts and song catalog are never overwritten.
        string shipped = Path.Combine(AppContext.BaseDirectory, "Assets");
        if (Directory.Exists(shipped))
            foreach (string source in Directory.EnumerateFiles(shipped, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(Core.AppPaths.AssetsPath, Path.GetRelativePath(shipped, source));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                if (!File.Exists(target)) File.Copy(source, target);
            }
        Directory.SetCurrentDirectory(Core.AppPaths.InstallRoot);
        try { using var game = new Game1(args); game.Run(); return 0; }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(Core.AppPaths.InstallRoot, "crash.log"), ex.ToString());
            Console.Error.WriteLine(ex); return 1;
        }
    }
    private static string? Value(string[] args, string key) { int i = Array.IndexOf(args, key); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
}
