using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        void UpdatePlaying(GameTime gameTime)
        {
            float time = (float)stopwatch.Elapsed.TotalSeconds;
            float offset = (settingsManager?.Settings.OffsetMs ?? 0) / 1000f;
            float adjTime = time + offset;
            Keys[] keys = GetLaneKeys();

            // Update video background
            videoPlayer?.UpdateTime(time);

            // Break period detection
            inBreak = false; currentBreak = null;
            if (beatmap?.Breaks != null)
            {
                foreach (var bp in beatmap.Breaks)
                {
                    if (time >= bp.StartTime && time <= bp.EndTime)
                    { inBreak = true; currentBreak = bp; break; }
                }
            }

            for (int c = 0; c < 4; c++)
            {
                if (kb.IsKeyDown(keys[c]) && !prevKb.IsKeyDown(keys[c]))
                {
                    if (editorMode)
                    {
                        notes.AddLast(new Note { Time = time, Column = c });
                    }
                    else
                    {
                        Note? nearest = null;
                        LinkedListNode<Note>? nearestNode = null;
                        float best = float.MaxValue;
                        for (var node = notes.First; node != null; node = node.Next)
                        {
                            var n = node.Value;
                            if (n.Column != c) continue;
                            float dt = Math.Abs(n.Time - adjTime);
                            if (dt <= GameConfig.GoodWindow && dt < best) { best = dt; nearest = n; nearestNode = node; }
                        }
                        if (nearestNode != null)
                        {
                            notes.Remove(nearestNode);
                            int pts; string jText; Color jColor;
                            if (best <= GameConfig.PerfectWindow)
                            {
                                pts = GameConfig.PerfectScore; jText = "PERFECT"; jColor = new Color(255, 220, 50);
                                hp = Math.Min(GameConfig.MaxHP, hp + GameConfig.HPGainPerfect); perfectCount++;
                            }
                            else if (best <= GameConfig.GreatWindow)
                            {
                                pts = GameConfig.GreatScore; jText = "GREAT"; jColor = new Color(80, 255, 120);
                                hp = Math.Min(GameConfig.MaxHP, hp + GameConfig.HPGainGreat); greatCount++;
                            }
                            else
                            {
                                pts = GameConfig.GoodScore; jText = "GOOD"; jColor = new Color(0, 200, 255);
                                hp = Math.Min(GameConfig.MaxHP, hp + GameConfig.HPGainGood); goodCount++;
                            }

                            score += pts; combo++; hitCount++;
                            if (combo > maxCombo) maxCombo = combo;
                            float sfxVol = (settingsManager?.Settings.SfxVolume ?? 0.8f);
                            sfxHit?.Play(Math.Clamp(0.5f + combo * 0.005f, 0.5f, 0.9f) * sfxVol, Math.Clamp(combo * 0.015f, 0f, 0.8f), 0f);
                            shakeTimer = 0.06f; shakeIntensity = Math.Clamp(1f + combo * 0.05f, 1f, 4f);
                            judgmentPopups.Add(new JudgmentPopup { Text = jText, Color = jColor, Timer = 0.6f,
                                Position = new Vector2(LaneLeft + c * LaneWidth + LaneWidth / 2, HitZoneY - 30) });
                            SpawnHitParticles(c);
                            replayManager?.RecordEvent(adjTime, c, jText, pts, combo);
                        }
                    }
                    int lx = LaneLeft + c * LaneWidth + 4;
                    if (keyFlashPool != null)
                    {
                        var k = keyFlashPool.Rent();
                        k.Reset(new Rectangle(lx, HitZoneY, LaneWidth - 8, HitZoneHeight),
                            TierNoteColors[ComboTier][c], GameConfig.KeyFlashDuration);
                        keyFlashes.Add(k);
                    }
                }
            }

            // Save in editor mode
            if (editorMode && kb.IsKeyDown(Keys.S) && !prevKb.IsKeyDown(Keys.S))
            {
                string songId = songs.Count > 0 ? songs[currentSongIndex].Id : "song";
                RcFileManager.WriteBeatmap(Path.Combine("Assets", songId + "_" + currentDifficulty + ".rcm"),
                    new Beatmap { Notes = new List<Note>(notes) });
            }

            // End detection (HP depleted or song finished)
            bool hpFail = hp <= 0 && !hpDepleted;
            if (hpFail) hpDepleted = true;

            if (!summaryShown && (hpDepleted || notes.Count == 0 || stopwatch.Elapsed.TotalSeconds >= songDurationSeconds + 0.1))
            {
                summaryShown = true; state = GameState.Result; songInstance?.Stop();
                videoPlayer?.Stop();
                var pct = maxScore > 0 ? (double)score / maxScore : 0.0;
                resultGrade = hpDepleted ? "F" : pct >= 0.95 ? "SS" : pct >= 0.85 ? "S" : pct >= 0.75 ? "A"
                            : pct >= 0.60 ? "B" : pct >= 0.40 ? "C" : "D";
                resultMenuIndex = 0;
                discordRpc?.SetResult(resultGrade, score);

                // Record stats
                int total = hitCount + missCount;
                double acc = total > 0 ? (double)hitCount / total * 100 : 0;
                string songId = songs.Count > 0 ? songs[currentSongIndex].Id : "unknown";
                statsDb?.RecordPlay(accountsManager?.LoggedInUser ?? "guest",
                    songId, currentDifficulty, score, maxCombo, hitCount, missCount, acc, resultGrade);

                // Save replay
                replayManager?.StopRecording(songId, currentDifficulty,
                    accountsManager?.LoggedInUser ?? "guest",
                    score, maxCombo, hitCount, missCount, acc, resultGrade);

                // Check achievements
                bool isFC = missCount == 0 && hitCount > 0;
                var summary = statsDb?.GetSummary(accountsManager?.LoggedInUser);
                int totalPlays = summary?.TotalPlays ?? 1;
                int uniqueSongs = GetUniqueSongsPlayed();
                achievementManager?.CheckAfterPlay(totalPlays, maxCombo, resultGrade, acc, isFC, uniqueSongs, songs.Count);

                // Cloud sync: upload play + achievements
                if (cloudSync != null && accountsManager?.LoggedInUser != null)
                {
                    var syncUser = accountsManager.LoggedInUser;
                    var playedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await cloudSync.UploadPlayAsync(syncUser, songId, currentDifficulty,
                                score, maxCombo, hitCount, missCount, acc, resultGrade, playedAt);
                            if (achievementManager != null)
                                await cloudSync.UploadAchievementsAsync(syncUser, achievementManager.GetAll());
                        }
                        catch { }
                    });
                }

                // Show achievement popups
                if (achievementManager != null && achievementManager.PendingPopups.Count > 0)
                {
                    var ach = achievementManager.PendingPopups.Dequeue();
                    achievementPopupText = Localization.Get(ach.NameKey);
                    achievementPopupTimer = 4f;
                }
            }

            // Shake, pulse, particles, judgments, miss detection
            if (shakeTimer > 0) shakeTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            { float bi = 0.5f; float ct = (float)stopwatch.Elapsed.TotalSeconds; float bp = (ct % bi) / bi;
              float tgt = bp < 0.1f ? (1f - bp / 0.1f) : 0f; tgt *= Math.Clamp(combo / 10f, 0.15f, 1f);
              beatPulseAlpha = MathHelper.Lerp(beatPulseAlpha, tgt, 0.3f); }

            float dt2 = (float)gameTime.ElapsedGameTime.TotalSeconds;
            for (int i = particles.Count - 1; i >= 0; i--)
            { var p = particles[i]; p.Pos += p.Vel * dt2; p.Vel.Y += 500f * dt2; p.Life -= dt2; if (p.Life <= 0) particles.RemoveAt(i); }
            for (int i = judgmentPopups.Count - 1; i >= 0; i--)
            { var j = judgmentPopups[i]; j.Timer -= dt2; j.Position = new Vector2(j.Position.X, j.Position.Y - 45f * dt2); if (j.Timer <= 0) judgmentPopups.RemoveAt(i); }

            // Fast miss detection - notes are missed shortly after passing the hit zone
            float pTime = (float)stopwatch.Elapsed.TotalSeconds + offset;
            for (var mN = notes.First; mN != null;)
            {
                var nxt = mN.Next;
                if (pTime - mN.Value.Time > GameConfig.MissWindow)
                {
                    int col = mN.Value.Column; notes.Remove(mN); combo = 0; missCount++;
                    hp = Math.Max(0, hp - GameConfig.HPDrainMiss);
                    float sfxVol = (settingsManager?.Settings.SfxVolume ?? 0.8f);
                    sfxMiss?.Play(0.35f * sfxVol, 0f, 0f);
                    replayManager?.RecordEvent(pTime, col, "MISS", 0, 0);
                    judgmentPopups.Add(new JudgmentPopup { Text = "MISS", Color = new Color(255, 80, 80), Timer = 0.5f,
                        Position = new Vector2(LaneLeft + col * LaneWidth + LaneWidth / 2, HitZoneY - 30) });
                    if (keyFlashPool != null)
                    { var k = keyFlashPool.Rent(); k.Reset(new Rectangle(LaneLeft + col * LaneWidth + 4, HitZoneY, LaneWidth - 8, HitZoneHeight), Color.Red, GameConfig.MissFlashDuration); keyFlashes.Add(k); }
                }
                mN = nxt;
            }
        }


        void StartPlaying(bool editor)
        {
            editorMode = editor;
            state = GameState.Playing;
            score = 0; combo = 0; maxCombo = 0; hitCount = 0; missCount = 0;
            perfectCount = 0; greatCount = 0; goodCount = 0;
            hp = GameConfig.InitialHP; hpDepleted = false;
            inBreak = false; currentBreak = null;
            summaryShown = false; keyFlashes.Clear(); particles.Clear(); judgmentPopups.Clear();
            shakeTimer = 0; beatPulseAlpha = 0;
            LoadCurrentSong();

            // Load video/background for current beatmap
            LoadBeatmapMedia();

            EnterBorderlessFullscreen();
            menuMusicInstance?.Stop();
            replayManager?.StartRecording();
            stopwatch.Restart(); songInstance?.Stop();
            if (songInstance != null)
            {
                songInstance.Volume = settingsManager?.Settings.MusicVolume ?? 0.7f;
                songInstance.Play();
            }

            // Start video playback
            if (videoPlayer != null && videoPlayer.HasVideo)
                videoPlayer.Play();

            string title = songs.Count > 0 ? songs[currentSongIndex].Title : "Unknown";
            discordRpc?.SetPlaying(title, DiffShort(currentDifficulty));
        }

        void LoadBeatmapMedia()
        {
            // Stop previous video
            videoPlayer?.Stop();
            videoPlayer?.Dispose();
            videoPlayer = new VideoBackgroundPlayer(GraphicsDevice);

            bgImageTexture?.Dispose();
            bgImageTexture = null;

            if (beatmap == null) return;

            // Try loading video
            if (!string.IsNullOrEmpty(beatmap.VideoFile))
            {
                string videoPath = Path.Combine("Assets", beatmap.VideoFile);
                if (File.Exists(videoPath))
                    videoPlayer.Open(videoPath);
            }

            // Try loading background image
            if (!string.IsNullOrEmpty(beatmap.BackgroundImage))
            {
                string bgPath = Path.Combine("Assets", beatmap.BackgroundImage);
                if (File.Exists(bgPath))
                {
                    try
                    {
                        using var fs = File.OpenRead(bgPath);
                        bgImageTexture = Texture2D.FromStream(GraphicsDevice, fs);
                    }
                    catch { bgImageTexture = null; }
                }
            }
        }

        void EnterBorderlessFullscreen()
        {
            windowedWidth = graphics!.PreferredBackBufferWidth;
            windowedHeight = graphics.PreferredBackBufferHeight;
            var d = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            graphics.PreferredBackBufferWidth = d.Width;
            graphics.PreferredBackBufferHeight = d.Height;
            graphics.IsFullScreen = false; graphics.ApplyChanges();
            Window.IsBorderless = true; Window.Position = Point.Zero;
            width = d.Width; height = d.Height;
            isFullscreen = true;
            renderCache?.Dispose(); renderCache = new RenderCache(GraphicsDevice);
        }

        void ExitBorderlessFullscreen()
        {
            Window.IsBorderless = false;
            graphics!.PreferredBackBufferWidth = windowedWidth;
            graphics.PreferredBackBufferHeight = windowedHeight;
            graphics.IsFullScreen = false; graphics.ApplyChanges();
            width = windowedWidth; height = windowedHeight;
            isFullscreen = false;
            renderCache?.Dispose(); renderCache = new RenderCache(GraphicsDevice);
        }


        // ═══════════ Gameplay ═══════════

        void DrawGameplay(GameTime gameTime)
        {
            float time = (float)stopwatch.Elapsed.TotalSeconds;
            int ll = LaneLeft, hz = HitZoneY;

            // Video / background image behind lanes
            var videoFrame = videoPlayer?.CurrentFrame;
            if (videoFrame != null)
            {
                // Draw video frame scaled to fill screen, semi-transparent
                spriteBatch!.Draw(videoFrame, new Rectangle(0, 0, width, height), Color.White * 0.35f);
            }
            else if (bgImageTexture != null)
            {
                spriteBatch!.Draw(bgImageTexture, new Rectangle(0, 0, width, height), Color.White * 0.25f);
            }

            for (int i = 0; i < LaneCount; i++)
                spriteBatch!.Draw(pixel!, new Rectangle(ll + i * LaneWidth, 0, LaneWidth, height), Color.White * (i % 2 == 0 ? 0.02f : 0.04f));
            for (int i = 0; i <= LaneCount; i++)
                spriteBatch!.Draw(pixel!, new Rectangle(ll + i * LaneWidth, 0, 1, height), Color.White * 0.08f);

            // Hit zone
            Color glow = TierGlowColor[ComboTier];
            spriteBatch!.Draw(pixel!, new Rectangle(ll, hz, TotalLaneWidth, 2), glow * 0.8f);
            spriteBatch.Draw(pixel!, new Rectangle(ll, hz, TotalLaneWidth, HitZoneHeight), Color.White * 0.02f);

            for (int i = 0; i < LaneCount; i++)
            {
                var lkLabels = GetLaneKeyLabels();
                var label = textRenderer!.GetTexture(lkLabels[i], "Segoe UI", 18, new Color(180, 180, 200));
                spriteBatch.Draw(label, new Vector2(ll + i * LaneWidth + (LaneWidth - label.Width) / 2, hz + HitZoneHeight + 6), Color.White);
            }

            // Notes
            for (var n = notes.Last; n != null; n = n.Previous)
            {
                float dt = n.Value.Time - time;
                if (time - n.Value.Time > GameConfig.MissWindow) continue;
                float prog = (GameConfig.ApproachTime - dt) / (GameConfig.ApproachTime + 0.01f);
                int nx = ll + n.Value.Column * LaneWidth + 6;
                int ny = (int)(MathHelper.Clamp(prog, 0f, 1f) * (hz - NoteHeight));
                var nr = new Rectangle(nx, ny, LaneWidth - 12, NoteHeight);
                Color nc = editorMode ? Color.Yellow : TierNoteColors[ComboTier][n.Value.Column % 4];
                spriteBatch.Draw(pixel!, new Rectangle(nr.X - 1, nr.Y - 1, nr.Width + 2, nr.Height + 2), nc * 0.2f);
                spriteBatch.Draw(pixel!, nr, nc * 0.9f);
                spriteBatch.Draw(pixel!, new Rectangle(nr.X, nr.Y, nr.Width, 2), Color.White * 0.35f);
            }

            // Flashes, particles, judgments
            for (int i = keyFlashes.Count - 1; i >= 0; i--)
            {
                var k = keyFlashes[i];
                spriteBatch!.Draw(pixel!, k.Rect, k.Color * (Math.Clamp(k.TimeToLive / GameConfig.KeyFlashDuration, 0, 1) * 0.3f));
                k.TimeToLive -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (k.TimeToLive <= 0) { keyFlashes.RemoveAt(i); keyFlashPool?.Return(k); }
            }
            foreach (var p in particles)
            {
                float pa = p.Life / p.MaxLife; float ps = p.Size * (0.5f + pa * 0.5f);
                spriteBatch!.Draw(pixel!, new Rectangle((int)(p.Pos.X - ps / 2), (int)(p.Pos.Y - ps / 2), (int)ps + 1, (int)ps + 1), p.Color * pa);
            }
            foreach (var j in judgmentPopups)
            {
                float ja = Math.Clamp(j.Timer / 0.3f, 0, 1);
                var jt = textRenderer!.GetTexture(j.Text, "Segoe UI", 22, j.Color);
                spriteBatch!.Draw(jt, new Vector2(j.Position.X - jt.Width / 2, j.Position.Y), Color.White * ja);
            }

            // HUD - top
            string st = songs.Count > 0 ? songs[currentSongIndex].Title : Localization.Get("unknown");
            var tt = textRenderer!.GetTexture(st, "Segoe UI", 18, Color.White);
            spriteBatch.Draw(tt, new Vector2(16, 14), Color.White);
            var dft = textRenderer!.GetTexture(DiffShort(currentDifficulty), "Segoe UI", 14, glow);
            spriteBatch.Draw(dft, new Vector2(16, 38), Color.White);

            var sct = textRenderer!.GetTexture($"{Localization.Get("score")}: {score}", "Segoe UI", 18, Color.White);
            spriteBatch.Draw(sct, new Vector2(width - sct.Width - 16, 14), Color.White);

            if (combo > 1)
            {
                int cs = Math.Min(36 + ComboTier * 4, 52);
                var ct = textRenderer!.GetTexture($"{combo}x", "Segoe UI", cs, TierGlowColor[ComboTier]);
                spriteBatch.Draw(ct, new Vector2((width - ct.Width) / 2, hz - 56 - ComboTier * 4), Color.White);
            }

            if (songDurationSeconds > 0)
            {
                float pr = Math.Clamp((float)(stopwatch.Elapsed.TotalSeconds / songDurationSeconds), 0, 1);
                spriteBatch.Draw(pixel!, new Rectangle(0, 0, width, 3), Color.White * 0.06f);
                spriteBatch.Draw(pixel!, new Rectangle(0, 0, (int)(width * pr), 3), glow);
            }

            // ═══ HP bar (right side) ═══
            DrawHPBar();

            // ═══ Judgment counter (left side) ═══
            DrawJudgmentCounter();

            // ═══ Break overlay ═══
            if (inBreak && currentBreak != null)
                DrawBreakOverlay();

            if (editorMode)
            {
                var et = textRenderer!.GetTexture(Localization.Get("editor_hint"), "Segoe UI", 14, Color.Yellow);
                spriteBatch.Draw(et, new Vector2((width - et.Width) / 2, height - 22), Color.White);
            }
        }

        void DrawBreakOverlay()
        {
            float time = (float)stopwatch.Elapsed.TotalSeconds;
            float remaining = currentBreak!.EndTime - time;
            float breakDuration = currentBreak.EndTime - currentBreak.StartTime;
            float breakProgress = Math.Clamp((time - currentBreak.StartTime) / breakDuration, 0, 1);

            // Fade in/out
            float fadeIn = Math.Clamp((time - currentBreak.StartTime) / 0.5f, 0, 1);
            float fadeOut = Math.Clamp(remaining / 0.5f, 0, 1);
            float alpha = Math.Min(fadeIn, fadeOut);

            // Dim overlay
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * (0.5f * alpha));

            int cx = width / 2;
            int cy = height / 2;

            // "Break" title
            var breakTitle = textRenderer!.GetTexture("Break", "Segoe UI", 36, Color.White);
            spriteBatch.Draw(breakTitle, new Vector2(cx - breakTitle.Width / 2, cy - 80), Color.White * alpha);

            // Countdown timer
            int secs = Math.Max(0, (int)Math.Ceiling(remaining));
            var countdownTex = textRenderer!.GetTexture($"{secs}s", "Segoe UI", 48, new Color(0, 200, 255));
            spriteBatch.Draw(countdownTex, new Vector2(cx - countdownTex.Width / 2, cy - 30), Color.White * alpha);

            // Current grade
            int total = hitCount + missCount;
            double acc = total > 0 ? (double)hitCount / total * 100 : 100;
            double pct = maxScore > 0 ? (double)score / maxScore : 1.0;
            string grade = pct >= 0.95 ? "SS" : pct >= 0.85 ? "S" : pct >= 0.75 ? "A"
                         : pct >= 0.60 ? "B" : pct >= 0.40 ? "C" : "D";

            Color gradeColor = grade switch
            {
                "SS" => new Color(255, 220, 50),
                "S" => new Color(255, 200, 50),
                "A" => new Color(80, 255, 120),
                "B" => new Color(0, 200, 255),
                "C" => new Color(200, 120, 255),
                _ => new Color(255, 80, 80)
            };
            var gradeTex = textRenderer!.GetTexture(grade, "Segoe UI", 42, gradeColor);
            spriteBatch.Draw(gradeTex, new Vector2(cx - gradeTex.Width / 2, cy + 30), Color.White * alpha);

            // Accuracy
            var accTex = textRenderer!.GetTexture($"{acc:F1}%", "Segoe UI", 22, new Color(200, 200, 220));
            spriteBatch.Draw(accTex, new Vector2(cx - accTex.Width / 2, cy + 80), Color.White * alpha);

            // Progress bar for break
            int barW = 200, barH = 4;
            int barX = cx - barW / 2, barY2 = cy + 115;
            spriteBatch.Draw(pixel!, new Rectangle(barX, barY2, barW, barH), Color.White * (0.15f * alpha));
            spriteBatch.Draw(pixel!, new Rectangle(barX, barY2, (int)(barW * breakProgress), barH), new Color(0, 200, 255) * (0.7f * alpha));
        }

        void DrawHPBar()
        {
            // SAO-style HP bar: vertical bar on the right with gradient coloring
            int barW = 12, barH = height - 160;
            int barX = width - barW - 20, barY = 80;

            // Background
            spriteBatch!.Draw(pixel!, new Rectangle(barX - 1, barY - 1, barW + 2, barH + 2), new Color(20, 20, 40) * 0.8f);
            DrawRectBorder(new Rectangle(barX - 1, barY - 1, barW + 2, barH + 2), Color.White * 0.1f);

            // HP fill from bottom
            float hpRatio = Math.Clamp(hp / GameConfig.MaxHP, 0, 1);
            int fillH = (int)(barH * hpRatio);
            int fillY = barY + barH - fillH;

            // Color gradient: green→yellow→red based on HP
            Color hpColor;
            if (hpRatio > 0.6f)
                hpColor = Color.Lerp(new Color(80, 255, 120), new Color(255, 220, 50), (1f - hpRatio) / 0.4f * 0.5f);
            else if (hpRatio > 0.3f)
                hpColor = Color.Lerp(new Color(255, 220, 50), new Color(255, 120, 0), (0.6f - hpRatio) / 0.3f);
            else
                hpColor = Color.Lerp(new Color(255, 120, 0), new Color(255, 40, 40), (0.3f - hpRatio) / 0.3f);

            // Glow effect
            spriteBatch.Draw(pixel!, new Rectangle(barX - 2, fillY - 1, barW + 4, fillH + 2), hpColor * 0.15f);
            spriteBatch.Draw(pixel!, new Rectangle(barX, fillY, barW, fillH), hpColor * 0.85f);
            // Bright top edge
            if (fillH > 2)
                spriteBatch.Draw(pixel!, new Rectangle(barX, fillY, barW, 2), Color.White * 0.4f);

            // HP text label
            var hpLabel = textRenderer!.GetTexture("HP", "Segoe UI", 11, hpColor);
            spriteBatch.Draw(hpLabel, new Vector2(barX + (barW - hpLabel.Width) / 2, barY - 18), Color.White);

            // Critical HP warning flash
            if (hpRatio < 0.2f && hpRatio > 0)
            {
                float flash = (float)(0.5 + 0.5 * Math.Sin(stopwatch.Elapsed.TotalSeconds * 8));
                spriteBatch.Draw(pixel!, new Rectangle(0, 0, width, height), new Color(255, 0, 0) * (flash * 0.04f));
            }
        }

        void DrawJudgmentCounter()
        {
            // Judgment counter on the left side
            int cx = 16, cy = height / 2 - 60;
            int w = 100, lh = 22;

            // Semi-transparent panel
            spriteBatch!.Draw(pixel!, new Rectangle(cx - 4, cy - 4, w + 8, lh * 4 + 12), new Color(10, 10, 25) * 0.6f);
            DrawRectBorder(new Rectangle(cx - 4, cy - 4, w + 8, lh * 4 + 12), Color.White * 0.06f);

            var pT = textRenderer!.GetTexture($"P  {perfectCount}", "Segoe UI", 13, new Color(255, 220, 50));
            spriteBatch.Draw(pT, new Vector2(cx, cy), Color.White);

            var grT = textRenderer!.GetTexture($"G  {greatCount}", "Segoe UI", 13, new Color(80, 255, 120));
            spriteBatch.Draw(grT, new Vector2(cx, cy + lh), Color.White);

            var gdT = textRenderer!.GetTexture($"OK {goodCount}", "Segoe UI", 13, new Color(0, 200, 255));
            spriteBatch.Draw(gdT, new Vector2(cx, cy + lh * 2), Color.White);

            var mT = textRenderer!.GetTexture($"X  {missCount}", "Segoe UI", 13, new Color(255, 80, 80));
            spriteBatch.Draw(mT, new Vector2(cx, cy + lh * 3), Color.White);
        }

}
