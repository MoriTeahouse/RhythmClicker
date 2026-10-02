// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using MatrixTea.Engine.Core.Input;
using MatrixTea.Engine.Core.Rhythm;
using MatrixTea.Engine.Desktop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ClickerGame;
public partial class Game1
{
    private readonly string[] runArguments;
    private readonly Stopwatch hostClock = Stopwatch.StartNew();
    private readonly DeviceAudioPlayer audioPlayer = new();
    private readonly PlaybackTimeline songTimeline = new();
    private readonly ActionInputBuffer laneInput = new(4);
    private readonly bool[] capturedLanes = new bool[4];
    private readonly ConcurrentQueue<Action> uiActions = new();
    private Core.PlayRun? playRun;
    private GlyphTextRenderer? glyphText;
    private bool _paused, _audioStarted, _audioFinished;
    private double lastSongTime, lastHitDelta;
    private float hitFeedbackTimer;
    private double HostNow => hostClock.Elapsed.TotalSeconds;
    private bool Pressed(Keys key) => kb.IsKeyDown(key) && !prevKb.IsKeyDown(key);
    private void RememberInput() { prevKb = kb; prevMouseState = mouseState; prevScrollValue = mouseState.ScrollWheelValue; }
    private double SongTime
    {
        get
        {
            double predicted = songTimeline.Position(HostNow);
            if (!_audioStarted || !audioPlayer.IsAvailable || _audioFinished) return predicted;
            if (!audioPlayer.IsPaused && !audioPlayer.IsPlaying)
            { _audioFinished = true; songTimeline.Seek(Math.Max(lastSongTime, audioPlayer.DurationSeconds), HostNow); return songTimeline.Position(HostNow); }
            double device = audioPlayer.PositionSeconds;
            lastSongTime = Math.Max(lastSongTime, device);
            return lastSongTime;
        }
    }
    private void BeginRound()
    {
        _paused = _audioStarted = _audioFinished = false; lastSongTime = 0;
        songTimeline.Start(HostNow, -1.8);
        laneInput.Synchronize(capturedLanes.AsSpan().FillAndReturn(false), HostNow);
    }
    private void PauseRound()
    {
        if (_paused) return;
        double position = SongTime; songTimeline.Seek(position, HostNow); songTimeline.Pause(HostNow);
        audioPlayer.Pause(); stopwatch.Stop(); videoPlayer?.Stop(); _paused = true;
    }
    private void ResumeRound()
    {
        _paused = false; songTimeline.Resume(HostNow); stopwatch.Start();
        if (_audioStarted) audioPlayer.Resume();
        var keys = GetLaneKeys(); for (int i = 0; i < 4; i++) capturedLanes[i] = kb.IsKeyDown(keys[i]);
        laneInput.Synchronize(capturedLanes, HostNow);
        if (videoPlayer?.HasVideo == true) videoPlayer.Play();
    }
    private void ReturnToMenu()
    {
        audioPlayer.Stop(); songInstance?.Stop(); stopwatch.Stop(); videoPlayer?.Stop();
        state = GameState.Menu; _paused = false; menuMusicInstance?.Play(); discordRpc?.SetMenu();
    }
    private void UpdateRoundEffects(float delta)
    {
        hitFeedbackTimer = Math.Max(0, hitFeedbackTimer - delta);
        for (int i = keyFlashes.Count - 1; i >= 0; i--) { var k = keyFlashes[i]; k.TimeToLive -= delta; if (k.TimeToLive <= 0) { keyFlashes.RemoveAt(i); keyFlashPool?.Return(k); } }
        for (int i = particles.Count - 1; i >= 0; i--) { var p = particles[i]; p.Pos += p.Vel * delta; p.Life -= delta; if (p.Life <= 0) particles.RemoveAt(i); }
        for (int i = judgmentPopups.Count - 1; i >= 0; i--) { var j = judgmentPopups[i]; j.Timer -= delta; if (j.Timer <= 0) judgmentPopups.RemoveAt(i); }
    }
    private void ApplyJudgement(RhythmPlayEvent<Note> result, double eventTime)
    {
        bool miss = !result.Judgment.CountsAsHit;
        string label = result.Judgment.Kind.ToString().ToUpperInvariant();
        int lane = result.Column;
        Color color = miss ? new(234, 128, 143) : result.Judgment.Kind == JudgementKind.Perfect ? new(240, 209, 143) : new(114, 211, 192);
        if (miss) hp = Math.Max(0, hp - GameConfig.HPDrainMiss);
        else
        {
            switch (result.Judgment.Kind) { case JudgementKind.Perfect: perfectCount++; hp = Math.Min(100, hp + 3); break; case JudgementKind.Great: greatCount++; hp = Math.Min(100, hp + 1.5f); break; default: goodCount++; hp = Math.Min(100, hp + 0.5f); break; }
            lastHitDelta = result.Judgment.DeltaSeconds; hitFeedbackTimer = 0.7f;
            if (settingsManager?.Settings.ReducedEffects != true) SpawnHitParticles(lane);
            sfxHit?.Play(0.45f * SfxVolume, 0, 0);
        }
        if (keyFlashPool != null) { var flash = keyFlashPool.Rent(); flash.Reset(new(LaneLeft + lane * LaneWidth, HitZoneY - 20, LaneWidth, 70), color, 0.15f); keyFlashes.Add(flash); }
        judgmentPopups.Add(new() { Text = label, Color = color, Timer = 0.55f, Position = new(LaneLeft + lane * LaneWidth + LaneWidth / 2, HitZoneY - 88) });
        replayManager?.RecordJudgement(eventTime, lane, label, result.Judgment.ScoreAwarded, playRun!.Combo, result.NoteTimeSeconds, result.Judgment.DeltaSeconds);
        score = playRun!.Score; combo = playRun.Combo; maxCombo = playRun.MaxCombo; hitCount = playRun.Hit; missCount = playRun.Miss;
    }
    private float SfxVolume => (settingsManager?.Settings.SfxVolume ?? 0.8f) * (settingsManager?.Settings.MasterVolume ?? 0.8f);
}

internal static class InputSpanExtensions
{
    public static Span<bool> FillAndReturn(this Span<bool> span, bool value) { span.Fill(value); return span; }
}
