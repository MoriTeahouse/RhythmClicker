// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;

namespace ClickerGame
{
    public class BreakPeriod
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
    }

    public class Beatmap
    {
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public string AudioFile { get; set; } = "";
        public string VideoFile { get; set; } = "";
        public string BackgroundImage { get; set; } = "";
        public float Bpm { get; set; }
        public List<Note> Notes { get; set; } = new();
        public List<BreakPeriod> Breaks { get; set; } = new();
        public void NormalizeLegacyMetadata() { if (Bpm == 0) Bpm = 120; }

        public void Validate()
        {
            if (!float.IsFinite(Bpm) || Bpm <= 0 || Bpm > 1000 || Notes == null || Notes.Count > 200000)
                throw new InvalidDataException("Invalid BPM or chart size.");
            foreach (var note in Notes)
                if (note == null || !float.IsFinite(note.Time) || note.Time < 0 || note.Time > 3600 || note.Column is < 0 or > 3)
                    throw new InvalidDataException("Notes require finite times (0–3600s) and lanes 0–3.");
            foreach (var period in Breaks ?? new())
                if (period == null || !float.IsFinite(period.StartTime) || !float.IsFinite(period.EndTime) || period.StartTime < 0 || period.EndTime <= period.StartTime)
                    throw new InvalidDataException("Invalid break period.");
        }

        public static Beatmap LoadFromString(string s)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<Beatmap>(s, options) ?? new Beatmap();
        }
    }

    public class Note
    {
        public float Time { get; set; }
        public int Column { get; set; }
    }
}
