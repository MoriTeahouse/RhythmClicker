// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MatrixTea.Engine.Core.Audio;
using MatrixTea.Engine.Desktop;
using System.Diagnostics;

namespace ClickerGame;

/// <summary>Local-only real device probe. Never loads accounts, cloud sync, RPC or user charts.</summary>
internal sealed class EnvironmentProbe : Game
{
    private readonly GraphicsDeviceManager graphics;
    private readonly DeviceAudioPlayer audio = new();
    private readonly Stopwatch clock = new();
    private SoundEffect? effect;
    private SoundEffectInstance? instance;
    private string? wave;
    private int frames;
    public EnvironmentProbe()
    {
        graphics = new(this) { PreferredBackBufferWidth = 320, PreferredBackBufferHeight = 180, SynchronizeWithVerticalRetrace = false };
        Window.Title = "RhythmClicker · 執行環境檢查"; IsFixedTimeStep = true; TargetElapsedTime = TimeSpan.FromMilliseconds(16);
    }
    protected override void LoadContent()
    {
        wave = Path.Combine(Core.AppPaths.InstallRoot, "environment-probe.wav");
        ProceduralScore.WriteWave(wave, 1.2, 120, 60, 0);
        using (var input = File.OpenRead(wave)) effect = SoundEffect.FromStream(input);
        instance = effect.CreateInstance(); instance.Volume = 0; instance.Play();
        if (!audio.Load(wave)) throw new IOException("WaveOut environment check failed: " + audio.Error);
        audio.Volume = 0; audio.Play(); clock.Start();
    }
    protected override void Update(GameTime gameTime)
    {
        if (clock.Elapsed.TotalSeconds > 4) throw new IOException("Audio device did not advance.");
        if (++frames >= 8 && audio.PositionSeconds > .08) Exit(); base.Update(gameTime);
    }
    protected override void Draw(GameTime gameTime) { GraphicsDevice.Clear(new Color(16, 44, 42)); base.Draw(gameTime); }
    protected override void UnloadContent() { audio.Dispose(); instance?.Dispose(); effect?.Dispose(); if (wave != null && File.Exists(wave)) File.Delete(wave); base.UnloadContent(); }
}
