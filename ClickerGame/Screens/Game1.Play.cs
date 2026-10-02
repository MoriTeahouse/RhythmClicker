// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
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
            if (playRun == null) return;
            double time = SongTime;
            UpdateRoundEffects(Math.Min(0.05f, (float)gameTime.ElapsedGameTime.TotalSeconds));
            if (!_audioStarted && time >= 0) { _audioStarted = true; songTimeline.Start(HostNow); audioPlayer.Volume = (settingsManager?.Settings.MusicVolume ?? 0.7f) * (settingsManager?.Settings.MasterVolume ?? 0.8f); audioPlayer.Play(); videoPlayer?.Play(); time = 0; }
            videoPlayer?.UpdateTime((float)Math.Max(0, time));
            double judgementTime = MatrixTea.Engine.Core.Rhythm.PlaybackTimeline.JudgementTime(time, settingsManager?.Settings.OffsetMs ?? 0);
            var keys = GetLaneKeys();
            for (int lane = 0; lane < 4; lane++) capturedLanes[lane] = kb.IsKeyDown(keys[lane]);
            laneInput.Capture(capturedLanes, HostNow);
            // Judge timestamped presses before overdue notes, then resolve every miss before results.
            while (laneInput.TryReadPress(out var press))
            {
                if (time < 0) continue;
                double at = judgementTime - (HostNow - press.TimestampSeconds);
                var result = playRun.HitAt(at, press.Action);
                if (result != null) ApplyJudgement(result, time);
                else if (keyFlashPool != null) { var flash = keyFlashPool.Rent(); flash.Reset(new(LaneLeft + press.Action * LaneWidth, HitZoneY - 20, LaneWidth, 70), LanePalette[press.Action], 0.1f); keyFlashes.Add(flash); }
            }
            foreach (var miss in playRun.MissesAt(judgementTime)) ApplyJudgement(miss, time);
            if (IsSmoke) DriveSmokeHits(time);
            bool failed = hp <= 0 && settingsManager?.Settings.PracticeMode != true;
            if (!summaryShown && (failed || playRun.Complete || time >= songDurationSeconds + 0.25))
            {
                hpDepleted = failed;
                foreach (var miss in playRun.Finish()) ApplyJudgement(miss, time);
                FinishRound();
            }
        }

        void FinishRound()
        {
            summaryShown = true; state = GameState.Result; audioPlayer.Stop(); songInstance?.Stop(); stopwatch.Stop(); videoPlayer?.Stop();
            double acc = playRun?.Accuracy ?? 0;
            resultGrade = hpDepleted ? "F" : acc >= 95 ? "SS" : acc >= 85 ? "S" : acc >= 75 ? "A" : acc >= 60 ? "B" : acc >= 40 ? "C" : "D";
            resultMenuIndex = 0; discordRpc?.SetResult(resultGrade, score);
            string songId = songs.Count > 0 ? songs[currentSongIndex].Id : "unknown";
            string player = accountsManager?.LoggedInUser ?? "guest";
            replayManager?.StopRecording(songId, currentDifficulty, player, score, maxCombo, hitCount, missCount, acc, resultGrade);
            if (settingsManager?.Settings.PracticeMode == true || IsSmoke) return;
            statsDb?.RecordPlay(player, songId, currentDifficulty, score, maxCombo, hitCount, missCount, acc, resultGrade);
            achievementManager?.CheckAfterPlay(statsDb?.GetSummary(player)?.TotalPlays ?? 1, maxCombo, resultGrade, acc, missCount == 0 && hitCount > 0, GetUniqueSongsPlayed(), songs.Count);
            if (cloudSync != null && accountsManager?.LoggedInUser != null)
            {
                // Freeze values before the next run mutates fields used by this background task.
                int finalScore = score, finalCombo = maxCombo, finalHit = hitCount, finalMiss = missCount;
                string difficulty = currentDifficulty, grade = resultGrade, date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                _ = Task.Run(async () => { try { await cloudSync.UploadPlayAsync(player, songId, difficulty, finalScore, finalCombo, finalHit, finalMiss, acc, grade, date); } catch { } });
            }
        }

        void StartPlaying(bool editor)
        {
            try { LoadCurrentSong(); }
            catch (Exception ex) { syncStatusText = "譜面載入失敗：" + ex.Message; syncStatusTimer = 6; return; }
            state = GameState.Playing; isReplayRun = false;
            score = combo = maxCombo = hitCount = missCount = perfectCount = greatCount = goodCount = 0;
            hp = GameConfig.InitialHP; hpDepleted = false; summaryShown = false;
            foreach (var flash in keyFlashes) keyFlashPool?.Return(flash);
            keyFlashes.Clear(); particles.Clear(); judgmentPopups.Clear();
            LoadBeatmapMedia(); menuMusicInstance?.Stop(); replayManager?.StartRecording(beatmap!);
            stopwatch.Restart(); BeginRound();
            discordRpc?.SetPlaying(songs.Count > 0 ? songs[currentSongIndex].Title : "Practice", DiffShort(currentDifficulty));
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

        void DrawGameplay(GameTime gameTime) => DrawModernGameplay(gameTime);

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
