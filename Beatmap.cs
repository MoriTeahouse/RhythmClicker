using System;
using System.Collections.Generic;
using System.Text.Json;
using MatrixTea.Engine.Core.Rhythm;

namespace ClickerGame
{
    public class Beatmap
    {
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public string AudioFile { get; set; } = "";
        public float Bpm { get; set; }
        public List<Note> Notes { get; set; } = new();

        public static Beatmap LoadFromString(string s)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            RhythmBeatmap? engineBeatmap = JsonSerializer.Deserialize<RhythmBeatmap>(s, options);
            return engineBeatmap is null ? new Beatmap() : FromMatrixTea(engineBeatmap);
        }

        public RhythmBeatmap ToMatrixTea() => new()
        {
            Name = Name,
            Author = Author,
            AudioFile = AudioFile,
            Bpm = Bpm,
            Notes = Notes.ConvertAll(note => new BeatmapNote
            {
                Time = note.Time,
                Column = note.Column,
            }),
        };

        public static Beatmap FromMatrixTea(RhythmBeatmap beatmap) => new()
        {
            Name = beatmap.Name,
            Author = beatmap.Author,
            AudioFile = beatmap.AudioFile,
            Bpm = beatmap.Bpm,
            Notes = beatmap.Notes.ConvertAll(note => new Note
            {
                Time = (float)note.Time,
                Column = note.Column,
            }),
        };
    }

    public class Note
    {
        public float Time { get; set; }
        public int Column { get; set; }
    }
}
