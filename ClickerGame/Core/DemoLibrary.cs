// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.IO;
using System.Collections.Generic;
using System.Linq;
using MatrixTea.Engine.Core.Audio;

namespace ClickerGame.Core;
public static class DemoLibrary
{
    public static readonly string[] Ids = { "tea_prelude", "stream_walk", "lantern_echo" };
    public static readonly string[] Titles = { "茶霧序曲 · Tea Prelude", "溪光步行 · Stream Walk", "夜燈回聲 · Lantern Echo" };
    public static readonly double[] Bpms = { 108, 126, 144 };
    public static void Ensure(string assets)
    {
        Directory.CreateDirectory(assets);
        for (int song = 0; song < Ids.Length; song++)
        {
            string wave = Path.Combine(assets, Ids[song] + ".wav");
            if (!File.Exists(wave)) ProceduralScore.WriteWave(wave, 60 + song * 15, Bpms[song], 57 + song * 2, song);
            foreach (string difficulty in new[] { "easy", "hard", "difficulty" })
            {
                string chart = Path.Combine(assets, Ids[song] + "_" + difficulty + ".rcm");
                if (!File.Exists(chart)) RcFileManager.WriteBeatmap(chart, Chart(song, difficulty));
            }
        }
    }
    public static Beatmap Chart(int song, string difficulty)
    {
        double beat = 60 / Bpms[song]; int subdivision = difficulty == "easy" ? 1 : 2;
        var map = new Beatmap { Name = Titles[song], Author = "MoriTeahouse", AudioFile = Ids[song] + ".wav", Bpm = (float)Bpms[song] };
        int[] pattern = { 0, 1, 2, 3, 2, 1, 0, 2, 1, 3, 2, 0, 3, 1, 2, 3 };
        for (int step = 4 * subdivision; step * beat / subdivision < 58 + song * 15; step++)
        {
            // Two-beat breath at phrase boundaries, with a smooth difficulty ramp.
            if (step / subdivision % 32 is 30 or 31) continue;
            float time = (float)(step * beat / subdivision); int column = pattern[(step + song * 3) % pattern.Length];
            map.Notes.Add(new() { Time = time, Column = column });
            if (difficulty == "difficulty" && step % 4 == 0) map.Notes.Add(new() { Time = time, Column = (column + 2) % 4 });
        }
        return map;
    }
}
