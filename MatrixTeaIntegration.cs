using System;
using MatrixTea.Engine.Core.Rhythm;

namespace ClickerGame;

public static class MatrixTeaIntegration
{
    public static RhythmJudgementProfile ToMatrixTeaJudgementProfile()
    {
        return new RhythmJudgementProfile
        {
            PerfectWindow = TimeSpan.FromSeconds(GameConfig.PerfectWindow),
            GreatWindow = TimeSpan.FromSeconds(GameConfig.GreatWindow),
            GoodWindow = TimeSpan.FromSeconds(GameConfig.GoodWindow),
        };
    }

    public static RhythmScoringProfile ToMatrixTeaScoringProfile()
    {
        return new RhythmScoringProfile
        {
            PerfectScore = GameConfig.PerfectScore,
            GreatScore = GameConfig.GreatScore,
            GoodScore = GameConfig.GoodScore,
        };
    }

    public static RhythmReplay ToMatrixTea(this ReplayData replay)
    {
        return new RhythmReplay
        {
            SongId = replay.SongId,
            Difficulty = replay.Difficulty,
            Player = replay.Player,
            PlayedAt = replay.PlayedAt,
            FinalScore = replay.FinalScore,
            MaxCombo = replay.MaxCombo,
            Hit = replay.Hit,
            Miss = replay.Miss,
            Accuracy = replay.Accuracy,
            Grade = replay.Grade,
            Events = replay.Events.ConvertAll(eventItem => eventItem.ToMatrixTea()),
        };
    }

    public static ReplayData FromMatrixTea(this RhythmReplay replay)
    {
        return new ReplayData
        {
            SongId = replay.SongId,
            Difficulty = replay.Difficulty,
            Player = replay.Player,
            PlayedAt = replay.PlayedAt,
            FinalScore = replay.FinalScore,
            MaxCombo = replay.MaxCombo,
            Hit = replay.Hit,
            Miss = replay.Miss,
            Accuracy = replay.Accuracy,
            Grade = replay.Grade,
            Events = replay.Events.ConvertAll(eventItem => eventItem.FromMatrixTea()),
        };
    }

    public static RhythmReplayEvent ToMatrixTea(this ReplayEvent replayEvent)
    {
        return new RhythmReplayEvent
        {
            Time = replayEvent.Time,
            Column = replayEvent.Column,
            Judgement = replayEvent.Judgment,
            ScoreGained = replayEvent.ScoreGained,
            ComboAt = replayEvent.ComboAt,
        };
    }

    public static ReplayEvent FromMatrixTea(this RhythmReplayEvent replayEvent)
    {
        return new ReplayEvent
        {
            Time = (float)replayEvent.Time,
            Column = replayEvent.Column,
            Judgment = replayEvent.Judgement,
            ScoreGained = replayEvent.ScoreGained,
            ComboAt = replayEvent.ComboAt,
        };
    }
}