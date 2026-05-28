using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        // ═══════════ Beatmap Editor Update ═══════════

        void InitEditor()
        {
            edSongName = ""; edAuthor = ""; edAudioPath = ""; edBpm = "120";
            edNotes = new(); edScrollTime = 0; edTotalTime = 10;
            edFieldFocus = 0; edMessage = ""; edMessageTimer = 0;
            edDragging = null; edPreviewing = false;
        }

        void UpdateEditor(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            if (edMessageTimer > 0) edMessageTimer -= dt;

            // Preview playback
            if (edPreviewing)
            {
                edScrollTime = (float)edPreviewWatch.Elapsed.TotalSeconds;
                if (edScrollTime >= edTotalTime) { edPreviewing = false; edPreviewInstance?.Stop(); }
            }

            // Scroll
            int scrollDelta = mouseState.ScrollWheelValue - prevScrollValue;
            if (scrollDelta != 0 && !edPreviewing)
            {
                edScrollTime -= scrollDelta / 120f * 0.5f;
                edScrollTime = Math.Clamp(edScrollTime, 0, Math.Max(edTotalTime - EdVisibleSeconds, 0));
            }

            // Tab / field switching
            if (kb.IsKeyDown(Keys.Tab) && !prevKb.IsKeyDown(Keys.Tab))
            {
                edFieldFocus = (edFieldFocus + 1) % 5; // -1→0→1→2→3→back to timeline
                if (edFieldFocus == 4) edFieldFocus = -1;
            }

            // Text input for focused field
            if (edFieldFocus >= 0)
            {
                bool shift = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
                if (kb.IsKeyDown(Keys.Back) && !prevKb.IsKeyDown(Keys.Back))
                {
                    ref string field = ref GetEdField(edFieldFocus);
                    if (field.Length > 0) field = field[..^1];
                }
                else
                {
                    foreach (Keys k in Enum.GetValues(typeof(Keys)))
                    {
                        if (k == Keys.None || k == Keys.Tab || k == Keys.Escape || k == Keys.Enter) continue;
                        if (kb.IsKeyDown(k) && !prevKb.IsKeyDown(k))
                        {
                            char ch = KeyToChar(k, shift);
                            if (ch != '\0') { ref string field = ref GetEdField(edFieldFocus); field += ch; }
                        }
                    }
                }
            }

            // Space = preview toggle
            if (kb.IsKeyDown(Keys.Space) && !prevKb.IsKeyDown(Keys.Space) && edFieldFocus < 0)
            {
                if (edPreviewing) { edPreviewing = false; edPreviewInstance?.Stop(); }
                else if (!string.IsNullOrEmpty(edAudioPath) && File.Exists(edAudioPath))
                {
                    try
                    {
                        edPreviewInstance?.Stop(); edPreviewEffect?.Dispose();
                        using var fs = File.OpenRead(edAudioPath);
                        edPreviewEffect = SoundEffect.FromStream(fs);
                        edPreviewInstance = edPreviewEffect.CreateInstance();
                        edPreviewInstance.Play();
                        edPreviewWatch.Restart(); edPreviewing = true; edScrollTime = 0;
                    }
                    catch { }
                }
            }

            // Ctrl+S = save
            bool ctrl = kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl);
            if (ctrl && kb.IsKeyDown(Keys.S) && !prevKb.IsKeyDown(Keys.S))
                SaveEditorBeatmap();

            // Timeline mouse interaction
            int tlLeft = 180; int tlRight = width - 20;
            int tlTop = 60; int tlBottom = height - 60;
            int tlW = tlRight - tlLeft;
            int laneW = tlW / 4;
            int mx = mouseState.X, my = mouseState.Y;

            bool inTimeline = mx >= tlLeft && mx < tlRight && my >= tlTop && my < tlBottom;

            // Left click = place or start drag
            if (mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released && inTimeline && edFieldFocus < 0)
            {
                int col = (mx - tlLeft) / laneW;
                col = Math.Clamp(col, 0, 3);
                float t = edScrollTime + (my - tlTop) / EdPixelsPerSecond;

                // Check if clicking existing note (for drag)
                Note? hit = null;
                foreach (var n in edNotes)
                {
                    float ny = tlTop + (n.Time - edScrollTime) * EdPixelsPerSecond;
                    if (Math.Abs(ny - my) < 12 && n.Column == col) { hit = n; break; }
                }

                if (hit != null) { edDragging = hit; }
                else
                {
                    edNotes.Add(new Note { Time = (float)Math.Round(t, 3), Column = col });
                    edNotes.Sort((a, b) => a.Time.CompareTo(b.Time));
                }
            }

            // Dragging
            if (edDragging != null && mouseState.LeftButton == ButtonState.Pressed)
            {
                int col = Math.Clamp((mx - tlLeft) / laneW, 0, 3);
                float t = edScrollTime + (my - tlTop) / EdPixelsPerSecond;
                edDragging.Column = col;
                edDragging.Time = (float)Math.Round(Math.Max(0, t), 3);
            }
            if (mouseState.LeftButton == ButtonState.Released) edDragging = null;

            // Right click = delete
            if (mouseState.RightButton == ButtonState.Pressed && prevMouseState.RightButton == ButtonState.Released && inTimeline)
            {
                int col = Math.Clamp((mx - tlLeft) / laneW, 0, 3);
                edNotes.RemoveAll(n =>
                {
                    float ny = tlTop + (n.Time - edScrollTime) * EdPixelsPerSecond;
                    return Math.Abs(ny - my) < 12 && n.Column == col;
                });
            }

            // Click on left panel fields
            if (mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released)
            {
                if (mx < 170 && my >= 90 && my < 250)
                {
                    int fi = (my - 90) / 50;
                    if (fi >= 0 && fi <= 3) edFieldFocus = fi;
                }
                else if (inTimeline) edFieldFocus = -1;
            }
        }

        ref string GetEdField(int idx)
        {
            switch (idx)
            {
                case 0: return ref edSongName;
                case 1: return ref edAuthor;
                case 2: return ref edAudioPath;
                default: return ref edBpm;
            }
        }

        void SaveEditorBeatmap()
        {
            // Validate
            if (string.IsNullOrWhiteSpace(edAudioPath) || !File.Exists(edAudioPath))
            { edMessage = Localization.Get("editor_err_audio"); edMessageTimer = 3; return; }
            if (edNotes.Count == 0)
            { edMessage = Localization.Get("editor_err_notes"); edMessageTimer = 3; return; }
            if (string.IsNullOrWhiteSpace(edSongName))
            { edMessage = Localization.Get("editor_err_name"); edMessageTimer = 3; return; }
            if (string.IsNullOrWhiteSpace(edAuthor))
            { edMessage = Localization.Get("editor_err_author"); edMessageTimer = 3; return; }

            float.TryParse(edBpm, out float bpm);
            if (bpm <= 0) bpm = 120;

            // Copy audio to Assets
            string audioName = Path.GetFileName(edAudioPath);
            string destAudio = Path.Combine("Assets", audioName);
            if (!File.Exists(destAudio) && File.Exists(edAudioPath))
                File.Copy(edAudioPath, destAudio, true);

            var bm = new Beatmap
            {
                Name = edSongName.Trim(),
                Author = edAuthor.Trim(),
                AudioFile = audioName,
                Bpm = bpm,
                Notes = new List<Note>(edNotes),
            };

            // Save as .rcm
            string safeId = edSongName.Trim().ToLowerInvariant().Replace(' ', '_');
            string rcmPath = Path.Combine("Assets", safeId + "_easy.rcm");
            RcFileManager.WriteBeatmap(rcmPath, bm);

            // Add to songs.json if not exists
            if (!songs.Any(s => s.Id == safeId))
            {
                songs.Add(new SongInfo { Id = safeId, Title = edSongName.Trim(), File = audioName,
                    Difficulties = new List<string> { "easy" } });
                var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText("Assets/songs.json", System.Text.Json.JsonSerializer.Serialize(songs, opts));
            }

            edMessage = Localization.Get("editor_saved"); edMessageTimer = 3;
        }


        // ═══════════ Beatmap Editor ═══════════

        void DrawEditor()
        {
            int cx = width / 2;

            // Title bar
            var title = textRenderer!.GetTexture(Localization.Get("editor_title"), "Segoe UI", 20, Color.White);
            spriteBatch!.Draw(title, new Vector2(cx - title.Width / 2, 10), Color.White);
            spriteBatch.Draw(pixel!, new Rectangle(0, 42, width, 1), Color.White * 0.08f);

            // Left panel - metadata fields
            int px = 12, py = 56;
            string[] labels = { "editor_name", "editor_author", "editor_audio", "editor_bpm" };
            string[] vals = { edSongName, edAuthor, edAudioPath, edBpm };
            for (int i = 0; i < 4; i++)
            {
                bool focused = edFieldFocus == i;
                var lb = textRenderer!.GetTexture(Localization.Get(labels[i]), "Segoe UI", 11, new Color(100, 100, 130));
                spriteBatch.Draw(lb, new Vector2(px, py + i * 50), Color.White);
                var box = new Rectangle(px, py + i * 50 + 16, 156, 28);
                spriteBatch.Draw(pixel!, box, focused ? Color.White * 0.06f : Color.White * 0.025f);
                DrawRectBorder(box, focused ? new Color(0, 200, 255) * 0.3f : Color.White * 0.04f);
                string v = vals[i] + (focused ? "|" : "");
                if (v.Length > 20) v = ".." + v[^18..];
                var vt = textRenderer!.GetTexture(v, "Segoe UI", 12, Color.White);
                spriteBatch.Draw(vt, new Vector2(box.X + 6, box.Y + 6), Color.White);
            }

            // Note count
            var nc = textRenderer!.GetTexture($"{Localization.Get("editor_notes")}: {edNotes.Count}", "Segoe UI", 13, new Color(0, 200, 255));
            spriteBatch.Draw(nc, new Vector2(px, py + 210), Color.White);

            // Save button
            int btnY = py + 240;
            var saveTex = textRenderer!.GetTexture(Localization.Get("editor_save"), "Segoe UI", 13, new Color(80, 255, 120));
            spriteBatch.Draw(saveTex, new Vector2(px, btnY), Color.White);

            // Preview button
            var prevTex = textRenderer!.GetTexture(Localization.Get("editor_play"), "Segoe UI", 13,
                edPreviewing ? new Color(255, 220, 50) : new Color(160, 160, 180));
            spriteBatch.Draw(prevTex, new Vector2(px, btnY + 22), Color.White);

            // Message
            if (edMessageTimer > 0 && !string.IsNullOrEmpty(edMessage))
            {
                bool isErr = edMessage.Contains("required") || edMessage.Contains("\u9700\u8981") || edMessage.Contains("\u81f3\u5c11");
                var mt = textRenderer!.GetTexture(edMessage, "Segoe UI", 13, isErr ? new Color(255, 100, 100) : new Color(80, 255, 120));
                spriteBatch.Draw(mt, new Vector2(px, btnY + 48), Color.White);
            }

            // Timeline area
            int tlLeft = 180, tlRight = width - 20;
            int tlTop = 56, tlBottom = height - 56;
            int tlW = tlRight - tlLeft;
            int laneW = tlW / 4;

            // Timeline background
            spriteBatch.Draw(pixel!, new Rectangle(tlLeft, tlTop, tlW, tlBottom - tlTop), new Color(8, 8, 20) * 0.8f);

            // Lane dividers
            for (int i = 0; i <= 4; i++)
                spriteBatch.Draw(pixel!, new Rectangle(tlLeft + i * laneW, tlTop, 1, tlBottom - tlTop), Color.White * 0.06f);

            // Lane labels
            for (int i = 0; i < 4; i++)
            {
                var ll = textRenderer!.GetTexture(LaneKeys[i], "Segoe UI", 14, new Color(80, 80, 110));
                spriteBatch.Draw(ll, new Vector2(tlLeft + i * laneW + (laneW - ll.Width) / 2, tlTop - 16), Color.White);
            }

            // Beat lines
            float bpm; float.TryParse(edBpm, out bpm); if (bpm <= 0) bpm = 120;
            float beatSec = 60f / bpm;
            float startBeat = (float)Math.Floor(edScrollTime / beatSec) * beatSec;
            for (float bt = startBeat; bt < edScrollTime + EdVisibleSeconds + beatSec; bt += beatSec)
            {
                float yp = tlTop + (bt - edScrollTime) * EdPixelsPerSecond;
                if (yp < tlTop || yp > tlBottom) continue;
                bool major = Math.Abs(bt % (beatSec * 4)) < 0.001f;
                spriteBatch.Draw(pixel!, new Rectangle(tlLeft, (int)yp, tlW, 1), Color.White * (major ? 0.12f : 0.04f));

                // Time label
                var tl2 = textRenderer!.GetTexture($"{bt:F1}s", "Segoe UI", 9, new Color(60, 60, 80));
                spriteBatch.Draw(tl2, new Vector2(tlLeft - tl2.Width - 4, (int)yp - 5), Color.White);
            }

            // Notes
            foreach (var n in edNotes)
            {
                float yp = tlTop + (n.Time - edScrollTime) * EdPixelsPerSecond;
                if (yp < tlTop - 20 || yp > tlBottom + 20) continue;

                int nx = tlLeft + n.Column * laneW + 4;
                int nw = laneW - 8;
                var nr = new Rectangle(nx, (int)yp - 8, nw, 16);
                Color nc2 = n == edDragging ? Color.Yellow : NoteColors[n.Column % 4];
                spriteBatch.Draw(pixel!, nr, nc2 * 0.8f);
                spriteBatch.Draw(pixel!, new Rectangle(nr.X, nr.Y, nr.Width, 2), Color.White * 0.4f);
                DrawRectBorder(nr, nc2 * 0.4f);
            }

            // Preview playhead
            if (edPreviewing)
            {
                float pyp = tlTop + (edScrollTime - edScrollTime) * EdPixelsPerSecond; // always at top since scroll follows
                // Actually the playhead is at current preview time
                float phY = tlTop; // scroll follows playhead
                spriteBatch.Draw(pixel!, new Rectangle(tlLeft, (int)phY, tlW, 2), new Color(255, 50, 50));
            }

            // Bottom hint
            spriteBatch.Draw(pixel!, new Rectangle(0, height - 46, width, 1), Color.White * 0.08f);
            var hint = textRenderer!.GetTexture(Localization.Get("editor_hint_main"), "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(hint, new Vector2(cx - hint.Width / 2, height - 36), Color.White);

            var escHint = textRenderer!.GetTexture("Esc \u2190", "Segoe UI", 11, new Color(80, 80, 110));
            spriteBatch.Draw(escHint, new Vector2(width - escHint.Width - 12, height - 36), Color.White);
        }

}
