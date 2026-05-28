using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        // ═══════════ Replay View ═══════════

        void StartReplayView(ReplayData replay)
        {
            replayData = replay;
            replayEventIndex = 0;
            state = GameState.ReplayView;
            replayJudgments.Clear();
            score = 0; combo = 0; maxCombo = 0; hitCount = 0; missCount = 0;
            keyFlashes.Clear(); particles.Clear(); judgmentPopups.Clear();
            shakeTimer = 0; beatPulseAlpha = 0;

            // Load song/beatmap for visual display
            LoadCurrentSong();
            EnterBorderlessFullscreen();
            menuMusicInstance?.Stop();
            stopwatch.Restart();
            if (songInstance != null)
            {
                songInstance.Volume = settingsManager?.Settings.MusicVolume ?? 0.7f;
                songInstance.Play();
            }
        }

        void UpdateReplayView(GameTime gameTime)
        {
            if (replayData == null) return;
            float time = (float)stopwatch.Elapsed.TotalSeconds;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Process replay events
            while (replayEventIndex < replayData.Events.Count)
            {
                var ev = replayData.Events[replayEventIndex];
                if (ev.Time > time) break;
                replayEventIndex++;

                // Simulate the event visually
                Color jColor = ev.Judgment switch
                {
                    "PERFECT" => new Color(255, 220, 50),
                    "GREAT" => new Color(80, 255, 120),
                    "GOOD" => new Color(0, 200, 255),
                    _ => new Color(255, 80, 80)
                };

                if (ev.Judgment != "MISS")
                {
                    score += ev.ScoreGained;
                    combo = ev.ComboAt;
                    if (combo > maxCombo) maxCombo = combo;
                    hitCount++;
                    SpawnHitParticles(ev.Column);
                    sfxHit?.Play(0.6f * (settingsManager?.Settings.SfxVolume ?? 0.8f), 0, 0);

                    int lx = LaneLeft + ev.Column * LaneWidth + 4;
                    if (keyFlashPool != null)
                    {
                        var k = keyFlashPool.Rent();
                        k.Reset(new Rectangle(lx, HitZoneY, LaneWidth - 8, HitZoneHeight),
                            jColor, GameConfig.KeyFlashDuration);
                        keyFlashes.Add(k);
                    }
                }
                else
                {
                    combo = 0; missCount++;
                }

                judgmentPopups.Add(new JudgmentPopup { Text = ev.Judgment, Color = jColor, Timer = 0.6f,
                    Position = new Vector2(LaneLeft + ev.Column * LaneWidth + LaneWidth / 2, HitZoneY - 30) });
            }

            // Remove played notes
            for (var n = notes.First; n != null;)
            {
                var next = n.Next;
                if (time - n.Value.Time > GameConfig.ApproachTime + 0.5f) notes.Remove(n);
                n = next;
            }

            // Update effects
            if (shakeTimer > 0) shakeTimer -= dt;
            for (int i = particles.Count - 1; i >= 0; i--)
            { var p = particles[i]; p.Pos += p.Vel * dt; p.Vel.Y += 500f * dt; p.Life -= dt; if (p.Life <= 0) particles.RemoveAt(i); }
            for (int i = judgmentPopups.Count - 1; i >= 0; i--)
            { var j = judgmentPopups[i]; j.Timer -= dt; j.Position = new Vector2(j.Position.X, j.Position.Y - 45f * dt); if (j.Timer <= 0) judgmentPopups.RemoveAt(i); }
            for (int i = keyFlashes.Count - 1; i >= 0; i--)
            { var k = keyFlashes[i]; k.TimeToLive -= dt; if (k.TimeToLive <= 0) { keyFlashes.RemoveAt(i); keyFlashPool?.Return(k); } }

            // End of replay
            if (replayEventIndex >= replayData.Events.Count && notes.Count == 0)
            {
                songInstance?.Stop(); stopwatch.Stop();
                state = GameState.Result;
                score = replayData.FinalScore;
                maxCombo = replayData.MaxCombo;
                hitCount = replayData.Hit;
                missCount = replayData.Miss;
                resultGrade = replayData.Grade;
                maxScore = (hitCount + missCount) * 100;
                resultMenuIndex = 0;
            }
        }

        void DrawReplayView()
        {
            // Draw the same gameplay view
            float time = (float)stopwatch.Elapsed.TotalSeconds;
            int ll = LaneLeft, hz = HitZoneY;

            for (int i = 0; i < LaneCount; i++)
                spriteBatch!.Draw(pixel!, new Rectangle(ll + i * LaneWidth, 0, LaneWidth, height), Color.White * (i % 2 == 0 ? 0.02f : 0.04f));
            for (int i = 0; i <= LaneCount; i++)
                spriteBatch!.Draw(pixel!, new Rectangle(ll + i * LaneWidth, 0, 1, height), Color.White * 0.08f);

            int tier = combo >= GameConfig.ComboTier4 ? 4
                     : combo >= GameConfig.ComboTier3 ? 3
                     : combo >= GameConfig.ComboTier2 ? 2
                     : combo >= GameConfig.ComboTier1 ? 1 : 0;
            Color glow = TierGlowColor[tier];
            spriteBatch!.Draw(pixel!, new Rectangle(ll, hz, TotalLaneWidth, 2), glow * 0.8f);
            spriteBatch.Draw(pixel!, new Rectangle(ll, hz, TotalLaneWidth, HitZoneHeight), Color.White * 0.02f);

            // Notes
            for (var n = notes.Last; n != null; n = n.Previous)
            {
                float dt = n.Value.Time - time;
                float prog = (GameConfig.ApproachTime - dt) / (GameConfig.ApproachTime + 0.01f);
                if (prog < 0 || prog > 1.2f) continue;
                int nx = ll + n.Value.Column * LaneWidth + 6;
                int ny = (int)(MathHelper.Clamp(prog, 0f, 1f) * (hz - NoteHeight));
                var nr = new Rectangle(nx, ny, LaneWidth - 12, NoteHeight);
                Color nc = TierNoteColors[tier][n.Value.Column % 4];
                spriteBatch.Draw(pixel!, new Rectangle(nr.X - 1, nr.Y - 1, nr.Width + 2, nr.Height + 2), nc * 0.2f);
                spriteBatch.Draw(pixel!, nr, nc * 0.9f);
            }

            // Effects
            foreach (var k in keyFlashes)
                spriteBatch!.Draw(pixel!, k.Rect, k.Color * (Math.Clamp(k.TimeToLive / GameConfig.KeyFlashDuration, 0, 1) * 0.3f));
            foreach (var p in particles)
            { float pa = p.Life / p.MaxLife; float ps = p.Size * (0.5f + pa * 0.5f);
              spriteBatch!.Draw(pixel!, new Rectangle((int)(p.Pos.X - ps / 2), (int)(p.Pos.Y - ps / 2), (int)ps + 1, (int)ps + 1), p.Color * pa); }
            foreach (var j in judgmentPopups)
            { float ja = Math.Clamp(j.Timer / 0.3f, 0, 1);
              var jt = textRenderer!.GetTexture(j.Text, "Segoe UI", 22, j.Color);
              spriteBatch!.Draw(jt, new Vector2(j.Position.X - jt.Width / 2, j.Position.Y), Color.White * ja); }

            // HUD
            var sct = textRenderer!.GetTexture($"{Localization.Get("score")}: {score}", "Segoe UI", 18, Color.White);
            spriteBatch.Draw(sct, new Vector2(width - sct.Width - 16, 14), Color.White);

            if (combo > 1)
            {
                var ct = textRenderer!.GetTexture($"{combo}x", "Segoe UI", 36, glow);
                spriteBatch.Draw(ct, new Vector2((width - ct.Width) / 2, hz - 56), Color.White);
            }

            // REPLAY label
            var replayLabel = textRenderer!.GetTexture("▶ REPLAY", "Segoe UI", 20, new Color(255, 220, 50));
            spriteBatch.Draw(replayLabel, new Vector2(16, 14), Color.White);

            if (replayData != null)
            {
                var playerTex = textRenderer!.GetTexture(replayData.Player, "Segoe UI", 14, new Color(160, 160, 180));
                spriteBatch.Draw(playerTex, new Vector2(16, 40), Color.White);
            }
        }

        // GenerateHitSfx, GenerateMissSfx → Audio/Game1.Audio.cs
        // GenerateMusicalWav, GenerateMenuMusicWav, GenerateBaStyleWav, NormalizeAndWriteWav → Audio/Game1.Audio.cs
        // GenerateBeatmapObject → Audio/Game1.Audio.cs

}
