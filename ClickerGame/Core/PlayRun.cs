// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System;
using System.Collections.Generic;
using System.Linq;
using MatrixTea.Engine.Core.Rhythm;

namespace ClickerGame.Core;

/// <summary>UI-independent round state. All pending notes count in final accuracy, including early failure.</summary>
public sealed class PlayRun
{
    private readonly RhythmPlaySession<Note> _session;
    public PlayRun(Beatmap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        map.Validate();
        _session = new(map.Notes.OrderBy(n => n.Time).Select(n => new Note { Time = n.Time, Column = n.Column }),
            n => n.Time, n => n.Column, true,
            new() { PerfectWindow = TimeSpan.FromSeconds(GameConfig.PerfectWindow), GreatWindow = TimeSpan.FromSeconds(GameConfig.GreatWindow), GoodWindow = TimeSpan.FromSeconds(GameConfig.GoodWindow) },
            new() { PerfectScore = GameConfig.PerfectScore, GreatScore = GameConfig.GreatScore, GoodScore = GameConfig.GoodScore }, GameConfig.MissWindow);
        LastNoteTime = map.Notes.Count == 0 ? 0 : map.Notes.Max(n => n.Time);
    }
    public IEnumerable<Note> RemainingNotes => _session.RemainingNotes;
    public int Score => _session.Score;
    public int Combo => _session.Combo;
    public int MaxCombo => _session.MaxCombo;
    public int Hit => _session.HitCount;
    public int Miss => _session.MissCount;
    public int Total => _session.TotalNotes;
    public int MaxScore => _session.MaxScore;
    public bool Complete => _session.IsComplete;
    public bool Indexed => _session.UsesIndexedLookup;
    public double LastNoteTime { get; }
    public double Accuracy => MaxScore == 0 ? 100 : 100d * Score / MaxScore;
    public double JudgedAccuracy => Hit + Miss == 0 ? 100 : 100d * Score / ((Hit + Miss) * GameConfig.PerfectScore);
    public RhythmPlayEvent<Note>? HitAt(double time, int column) => _session.TryHit(time, column);
    public IReadOnlyList<RhythmPlayEvent<Note>> MissesAt(double time) => _session.CollectMisses(time);
    public IReadOnlyList<RhythmPlayEvent<Note>> Finish() => _session.CollectMisses(LastNoteTime + GameConfig.MissWindow + 1);
}
