// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClickerGame
{
    /// <summary>
    /// Represents a single input event in a replay.
    /// </summary>
    public class ReplayEvent
    {
        public float Time { get; set; }       // Seconds from song start
        public int Column { get; set; }       // Lane 0-3
        public string Judgment { get; set; } = ""; // PERFECT/GREAT/GOOD/MISS
        public int ScoreGained { get; set; }
        public int ComboAt { get; set; }
        public double? NoteTime { get; set; }
        public double DeltaSeconds { get; set; }
    }

    /// <summary>
    /// Full replay data for a single play.
    /// </summary>
    public class ReplayData
    {
        public string BeatmapHash { get; set; } = "";
        public string SongId { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string Player { get; set; } = "guest";
        public string PlayedAt { get; set; } = "";
        public int FinalScore { get; set; }
        public int MaxCombo { get; set; }
        public int Hit { get; set; }
        public int Miss { get; set; }
        public double Accuracy { get; set; }
        public string Grade { get; set; } = "";
        public List<ReplayEvent> Events { get; set; } = new();
        public void Validate()
        {
            if (SongId == null || Difficulty == null || Player == null || Grade == null || BeatmapHash == null || string.IsNullOrWhiteSpace(SongId) || SongId.Length > 256 || Difficulty.Length > 64 || Player.Length > 256 || Grade.Length > 16 || BeatmapHash.Length is not (0 or 64) || Events == null || Events.Count > 500000 || FinalScore < 0 || MaxCombo < 0 || Hit < 0 || Miss < 0 || !double.IsFinite(Accuracy) || Accuracy < 0 || Accuracy > 100)
                throw new InvalidDataException("Invalid replay summary.");
            foreach (var item in Events)
                if (item == null || !float.IsFinite(item.Time) || item.Time < 0 || item.Time > 3602 || item.Column is < 0 or > 3 || item.ScoreGained < 0 || item.ComboAt < 0 || !double.IsFinite(item.DeltaSeconds) || item.NoteTime is double time && (!double.IsFinite(time) || time < 0 || time > 3600) || item.Judgment is not ("PERFECT" or "GREAT" or "GOOD" or "MISS"))
                    throw new InvalidDataException("Invalid replay event.");
            if (!Events.Select(e => e.Time).SequenceEqual(Events.Select(e => e.Time).OrderBy(t => t))) throw new InvalidDataException("Replay events are not chronological.");
        }
    }

    /// <summary>
    /// Records and plays back replays using .rcp encrypted format.
    /// </summary>
    public class ReplayManager
    {
        private readonly string _replayDir;
        private List<ReplayEvent> _recording = new();
        private bool _isRecording;
        private string _chartHash = "";

        public ReplayManager(string replayDir = "Replays")
        {
            _replayDir = replayDir;
            Directory.CreateDirectory(_replayDir);
        }

        public void StartRecording()
        {
            _recording = new List<ReplayEvent>();
            _chartHash = "";
            _isRecording = true;
        }
        public void StartRecording(Beatmap chart) { StartRecording(); _chartHash = ChartHash(chart); }
        public static string ChartHash(Beatmap chart) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(chart.Notes.OrderBy(n => n.Time).ThenBy(n => n.Column)))));
        public void RecordJudgement(double time, int column, string judgment, int scoreGained, int comboAt, double noteTime, double delta)
        {
            if (!_isRecording) return;
            _recording.Add(new() { Time = (float)Math.Max(0, time), Column = column, Judgment = judgment, ScoreGained = scoreGained, ComboAt = comboAt, NoteTime = noteTime, DeltaSeconds = delta });
        }

        public void RecordEvent(float time, int column, string judgment, int scoreGained, int comboAt)
        {
            if (!_isRecording) return;
            _recording.Add(new ReplayEvent
            {
                Time = (float)Math.Round(time, 3),
                Column = column,
                Judgment = judgment,
                ScoreGained = scoreGained,
                ComboAt = comboAt
            });
        }

        public ReplayData StopRecording(string songId, string difficulty, string player,
            int finalScore, int maxCombo, int hit, int miss, double accuracy, string grade)
        {
            _isRecording = false;
            var data = new ReplayData
            {
                BeatmapHash = _chartHash,
                SongId = songId,
                Difficulty = difficulty,
                Player = player,
                PlayedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                FinalScore = finalScore,
                MaxCombo = maxCombo,
                Hit = hit,
                Miss = miss,
                Accuracy = accuracy,
                Grade = grade,
                Events = new List<ReplayEvent>(_recording)
            };

            // Save to .rcp file
            data.Validate();
            string prefix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(songId + "/" + difficulty)))[..16];
            string fileName = $"{prefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fffffff}_{Guid.NewGuid():N}.rcp";
            string path = Path.Combine(_replayDir, fileName);
            RcFileManager.WriteEncrypted(path, data);

            return data;
        }

        /// <summary>Get the best replay for a song+difficulty combo.</summary>
        public ReplayData? GetBestReplay(string songId, string difficulty)
        {
            ReplayData? best = null;
            string pattern = "*.rcp";

            if (!Directory.Exists(_replayDir)) return null;

            foreach (var file in Directory.GetFiles(_replayDir, pattern))
            {
                try
                {
                    var data = RcFileManager.ReadEncrypted<ReplayData>(file);
                    data.Validate();
                    if (data.SongId != songId || data.Difficulty != difficulty) continue;
                    if (best == null || data.FinalScore > best.FinalScore)
                        best = data;
                }
                catch { }
            }
            return best;
        }

        /// <summary>Get all replays, sorted by score descending.</summary>
        public List<(string file, ReplayData data)> GetAllReplays()
        {
            var list = new List<(string file, ReplayData data)>();
            if (!Directory.Exists(_replayDir)) return list;

            foreach (var file in Directory.GetFiles(_replayDir, "*.rcp"))
            {
                try
                {
                    var data = RcFileManager.ReadEncrypted<ReplayData>(file);
                    data.Validate();
                    list.Add((Path.GetFileName(file), data));
                }
                catch { }
            }
            list.Sort((a, b) => b.data.FinalScore.CompareTo(a.data.FinalScore));
            return list;
        }
    }
}
