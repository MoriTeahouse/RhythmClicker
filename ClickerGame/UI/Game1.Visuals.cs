// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
namespace ClickerGame;
public partial class Game1
{
    private static readonly Color Ink = new(14, 24, 33), Jade = new(124, 221, 194), Cream = new(244, 238, 218), Muted = new(139, 163, 165), Gold = new(230, 189, 128);
    private static readonly Color[] LanePalette = { new(124, 221, 194), new(123, 178, 231), new(195, 157, 221), new(230, 189, 128) };
    private void Rect(Rectangle rectangle, Color color) => spriteBatch!.Draw(pixel!, rectangle, color);
    private void Label(string text, float x, float y, int size = 20, Color? color = null) => glyphText!.Draw(spriteBatch!, text, new(x, y), color ?? Cream, size);
    private void CenterLabel(string text, float x, float y, int size = 20, Color? color = null) { float w = glyphText!.Measure(text, size).X; Label(text, x - w / 2, y, size, color); }
    private void Panel(Rectangle rectangle, Color? color = null)
    {
        Rect(new(rectangle.X + 4, rectangle.Y + 8, rectangle.Width, rectangle.Height), Color.Black * 0.18f);
        Rect(rectangle, color ?? new Color(23, 40, 48)); DrawRectBorder(rectangle, new Color(53, 80, 85));
    }
    private Rectangle MenuButton(int i) => new(48 + i % 2 * 192, 318 + i / 2 * 57, 178, 45);
    private Rectangle SongPanel => new(470, 148, Math.Max(360, width - 524), height - 242);
    private Rectangle DifficultyButton(int i) => new(SongPanel.X + 30 + i * 104, SongPanel.Y + 119, 94, 34);
    private void DrawMark(int x, int y, int size)
    {
        if(brandMark!=null){spriteBatch!.Draw(brandMark,new Rectangle(x,y,size,size),Color.White);return;}
        spriteBatch!.Draw(circleTexture!, new Rectangle(x, y, size, size), Jade * 0.12f);
        Rect(new(x + size / 5, y + size * 2 / 5, size * 3 / 5, size / 3), Jade);
        Rect(new(x + size / 3, y + size / 4, size / 10, size / 8), Gold);
        Rect(new(x + size / 2, y + size / 5, size / 10, size / 6), Gold);
        Rect(new(x + size / 6, y + size * 3 / 4, size * 2 / 3, 2), Cream * 0.6f);
    }
    private Texture2D? brandMark;
    private void UpdateMenu(GameTime gt)
    {
        menuTimer += Math.Min(0.05f, (float)gt.ElapsedGameTime.TotalSeconds);
        if (Pressed(Keys.U) && Systems.UpdateManager.IsUpdateAvailable)
        {
            if (Systems.UpdateManager.ReadyExecutable != null) Systems.UpdateManager.RestartGame();
            else _ = Systems.UpdateManager.DownloadAndInstallAsync();
        }
        if (Pressed(Keys.Up)) currentMenuIndex = (currentMenuIndex + menuKeys.Length - 1) % menuKeys.Length;
        if (Pressed(Keys.Down)) currentMenuIndex = (currentMenuIndex + 1) % menuKeys.Length;
        if (Pressed(Keys.Left) || Pressed(Keys.Right))
        {
            if (songs.Count > 0) { var diffs = songs[currentSongIndex].Difficulties; if (diffs.Count > 0) { int i = diffs.IndexOf(currentDifficulty); currentDifficulty = diffs[(i + (Pressed(Keys.Right) ? 1 : diffs.Count - 1)) % diffs.Count]; } }
        }
        if (Pressed(Keys.Tab) && songs.Count > 0) SelectSong((currentSongIndex + 1) % songs.Count);
        if (Pressed(Keys.Enter)) ExecuteMenuAction(menuKeys[currentMenuIndex]);
        if (mouseState.Position != prevMouseState.Position)
            for (int i = 0; i < menuKeys.Length; i++) if (MenuButton(i).Contains(mouseState.Position)) currentMenuIndex = i;
        bool click = mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released;
        if (!click) return;
        for (int i = 0; i < menuKeys.Length; i++) if (MenuButton(i).Contains(mouseState.Position)) { currentMenuIndex = i; ExecuteMenuAction(menuKeys[i]); return; }
        if (songs.Count > 0)
        {
            for (int i = 0; i < Math.Min(3, songs[currentSongIndex].Difficulties.Count); i++) if (DifficultyButton(i).Contains(mouseState.Position)) currentDifficulty = songs[currentSongIndex].Difficulties[i];
            if (new Rectangle(SongPanel.Right - 100, SongPanel.Y + 29, 68, 34).Contains(mouseState.Position)) SelectSong((currentSongIndex + 1) % songs.Count);
            if (new Rectangle(SongPanel.X + 30, SongPanel.Bottom - 76, SongPanel.Width - 60, 48).Contains(mouseState.Position)) StartPlaying(false);
        }
    }
    private void SelectSong(int index) { currentSongIndex = index; var d = songs[index].Difficulties; if (!d.Contains(currentDifficulty)) currentDifficulty = d.FirstOrDefault() ?? "easy"; }
    private void DrawMenu()
    {
        DrawMark(48, 35, 44); Label("MORI TEAHOUSE  /  MATRIXTEA", 106, 48, 16, Jade);
        Label("Rhythm", 45, 108, 54); Label("Clicker", 45, 166, 54, Jade);
        Label("循著節拍，找到自己的節奏。", 48, 249, 18, Muted);
        Label("0.6.1  ·  大更新測試版 · 第二版", 48, 282, 14, Gold);
        if(Systems.UpdateManager.IsUpdateAvailable)
            Label(Systems.UpdateManager.IsDownloading ? $"更新下載 {Systems.UpdateManager.DownloadProgress:P0}" : "U 下載／啟動更新 "+Systems.UpdateManager.AvailableVersion, 470, 108, 16, Gold);
        for (int i = 0; i < menuKeys.Length; i++)
        {
            var rect = MenuButton(i); bool selected = currentMenuIndex == i;
            Rect(rect, selected ? new Color(45, 81, 80) : new Color(23, 39, 47)); DrawRectBorder(rect, selected ? Jade * 0.65f : new Color(40, 61, 66));
            Label(Localization.Get(menuKeys[i]), rect.X + 15, rect.Y + 9, 18, selected ? Cream : Muted);
        }
        var card = SongPanel; Panel(card);
        Label("現在播放 / ORIGINAL COLLECTION", card.X + 30, card.Y + 27, 14, Jade);
        Label("下一首 →", card.Right - 100, card.Y + 30, 14, Muted);
        if (songs.Count > 0)
        {
            var song = songs[currentSongIndex];
            Label(glyphText!.Wrap(song.Title, card.Width - 60, 27), card.X + 30, card.Y + 65, 27);
            for (int i = 0; i < Math.Min(3, song.Difficulties.Count); i++)
            {
                var diff = DifficultyButton(i); bool selected = currentDifficulty == song.Difficulties[i];
                Rect(diff, selected ? new Color(61, 85, 69) : new Color(31, 47, 54)); DrawRectBorder(diff, selected ? Gold : Muted * 0.3f);
                CenterLabel(DiffShort(song.Difficulties[i]), diff.Center.X, diff.Y + 7, 15, selected ? Gold : Muted);
            }
        }
        int waveY = card.Y + 260;
        for (int i = 0; i < 48; i++)
        {
            float pulse = (float)(0.25 + 0.75 * Math.Abs(Math.Sin(i * 0.47 + menuTimer * 1.4) * Math.Cos(i * 0.21 - menuTimer * 0.4)));
            int bar = (int)(pulse * Math.Max(40, card.Height - 315));
            int bx = card.X + 30 + i * (card.Width - 60) / 48;
            Rect(new(bx, waveY - bar / 2, Math.Max(3, (card.Width - 60) / 48 - 3), bar), Color.Lerp(Jade, Gold, i / 47f) * 0.55f);
        }
        var start = new Rectangle(card.X + 30, card.Bottom - 76, card.Width - 60, 48);
        Rect(start, Jade); CenterLabel("開始演奏  /  ENTER", start.Center.X, start.Y + 10, 20, Ink);
        Label("TAB 換曲   ← → 難度   D F J K 打擊   SPACE 暫停", card.X + 30, card.Bottom - 24, 13, Muted);
        Label("© 2026 MoriTeahouse（森之宿茶室） · AGPL-3.0-only", 48, height - 38, 13, Muted);
    }
    private void DrawModernGameplay(GameTime gameTime)
    {
        double time = SongTime;
        double visual = time + (settingsManager?.Settings.VisualOffsetMs ?? 0) / 1000d;
        float approach = settingsManager?.Settings.ApproachSeconds ?? 1.6f;
        int ll = LaneLeft, hz = HitZoneY, top = 105;
        if (bgImageTexture != null) spriteBatch!.Draw(bgImageTexture, new Rectangle(0, 0, width, height), Color.White * 0.12f);
        if (videoPlayer?.CurrentFrame is Texture2D video) spriteBatch!.Draw(video, new Rectangle(0, 0, width, height), Color.White * 0.12f);
        Rect(new(ll - 18, top - 15, TotalLaneWidth + 36, height - top - 12), new Color(11, 23, 30));
        for (int lane = 0; lane < 4; lane++)
        {
            Rect(new(ll + lane * LaneWidth, top, LaneWidth - 1, hz - top + 65), new Color(24 + lane % 2 * 3, 39, 47));
            Rect(new(ll + lane * LaneWidth + 7, hz - 18, LaneWidth - 14, 36), LanePalette[lane] * (kb.IsKeyDown(GetLaneKeys()[lane]) ? 0.65f : 0.13f));
            DrawRectBorder(new(ll + lane * LaneWidth + 7, hz - 18, LaneWidth - 14, 36), LanePalette[lane] * 0.65f);
            CenterLabel(GetLaneKeyLabels()[lane], ll + lane * LaneWidth + LaneWidth / 2, hz + 33, 18, LanePalette[lane]);
        }
        double beat = 60d / Math.Max(30, beatmap?.Bpm ?? 120);
        for (int i = (int)Math.Floor(visual / beat); i * beat <= visual + approach; i++)
        {
            double ahead = i * beat - visual; if (ahead < 0) continue;
            int y = (int)(hz - ahead / approach * (hz - top));
            Rect(new(ll, y, TotalLaneWidth, 1), Jade * (i % 4 == 0 ? 0.12f : 0.04f));
        }
        IEnumerable<Note> visible = state == GameState.ReplayView ? notes : playRun?.RemainingNotes ?? notes;
        foreach (var note in visible)
        {
            double ahead = note.Time - visual;
            if (ahead > approach) break;
            if (ahead < -GameConfig.MissWindow) continue;
            int x = ll + note.Column * LaneWidth + 7, y = (int)Math.Round(hz - ahead / approach * (hz - top)) - NoteHeight / 2;
            Color color = LanePalette[note.Column];
            Rect(new(x - 2, y - 3, LaneWidth - 10, NoteHeight + 6), color * 0.12f);
            Rect(new(x, y, LaneWidth - 14, NoteHeight), color);
            Rect(new(x + 3, y + 3, LaneWidth - 20, 3), Cream * 0.7f);
        }
        Rect(new(ll - 8, hz - 1, TotalLaneWidth + 16, 2), Cream * 0.8f);
        foreach (var flash in keyFlashes) Rect(flash.Rect, flash.Color * Math.Clamp(flash.TimeToLive / 0.15f * 0.28f, 0, 0.28f));
        foreach (var particle in particles) Rect(new((int)particle.Pos.X, (int)particle.Pos.Y, 3, 3), particle.Color * Math.Clamp(particle.Life / particle.MaxLife, 0, 1));
        if (judgmentPopups.Count > 0) { var j = judgmentPopups[^1]; CenterLabel(j.Text, width / 2, hz - 103, 25, j.Color * Math.Clamp(j.Timer / 0.15f, 0, 1)); }
        if (hitFeedbackTimer > 0) CenterLabel($"{(lastHitDelta < 0 ? "EARLY" : "LATE")}  {Math.Abs(lastHitDelta * 1000):F0} ms", width / 2, hz - 68, 14, Muted);
        DrawMark(35, 27, 36); Label("RHYTHMCLICKER", 87, 35, 18, Jade);
        string songTitle = songs.Count > 0 ? songs[currentSongIndex].Title : "Practice";
        Label(songTitle, 35, 100, 21); Label(DiffShort(currentDifficulty), 35, 137, 16, Gold);
        Label("SCORE", 35, 204, 13, Muted); Label(score.ToString("D7"), 35, 230, 38);
        Label("COMBO", 35, 304, 13, Muted); Label(combo.ToString(), 35, 332, 50, Jade);
        Label($"PERFECT   {perfectCount}", 35, 421, 16, Gold);
        Label($"GREAT       {greatCount}", 35, 452, 16, Jade);
        Label($"GOOD        {goodCount}", 35, 483, 16, new(123, 178, 231));
        Label($"MISS           {missCount}", 35, 514, 16, new(234, 128, 143));
        int side = ll + TotalLaneWidth + 55;
        Label("ACCURACY", side, 112, 13, Muted); Label($"{(isReplayRun ? (hitCount + missCount == 0 ? 100 : score * 100d / ((hitCount + missCount) * GameConfig.PerfectScore)) : playRun?.JudgedAccuracy ?? 100):F2}%", side, 141, 28, Gold);
        Label("ENERGY", side, 223, 13, Muted);
        Rect(new(side, 253, 180, 6), Muted * 0.15f); Rect(new(side, 253, (int)(180 * hp / 100), 6), Jade);
        Label(settingsManager?.Settings.PracticeMode == true ? "練習模式 · 不計排行榜" : "正式演奏", side, 290, 15, Muted);
        Label("SPACE 暫停\nESC 返回\nF11 全螢幕", side, height - 156, 15, Muted);
        float progress = Math.Clamp((float)(Math.Max(0, time) / Math.Max(1, songDurationSeconds)), 0, 1);
        Rect(new(0, 0, width, 3), Jade * 0.1f); Rect(new(0, 0, (int)(width * progress), 3), Jade);
        if (time < 0) { Panel(new(width / 2 - 90, 210, 180, 130)); CenterLabel("準備", width / 2, 224, 18, Muted); CenterLabel(Math.Ceiling(-time).ToString(), width / 2, 258, 48, Gold); }
        if (!audioPlayer.IsAvailable) Label("音訊裝置未就緒 · 靜音計時", side, 349, 14, Gold);
    }
    private void DrawPauseOverlay()
    {
        Rect(new(0, 0, width, height), Ink * 0.85f);
        CenterLabel("演奏已暫停", width / 2, height / 2 - 55, 32, Jade);
        CenterLabel("SPACE 繼續    ESC 返回選曲", width / 2, height / 2 + 8, 19);
    }
    private void DrawModernResult()
    {
        int x = width / 2 - 380, y = 130; Panel(new(x, y, 760, height - 230));
        Label("SESSION COMPLETE", x + 38, y + 25, 14, Jade);
        Label(settingsManager?.Settings.PracticeMode == true ? "練習完成" : hpDepleted ? "再試一次，每個節拍都在等你。" : "演奏完成，留下這次的節奏。", x + 38, y + 56, 22);
        Label(resultGrade, x + 40, y + 108, 64, Gold);
        Label(score.ToString("D7"), x + 210, y + 123, 42);
        Label($"準確率 {RoundAccuracy:F2}%   最大連擊 {maxCombo}", x + 210, y + 184, 20, Jade);
        Label($"PERFECT {perfectCount}   GREAT {greatCount}   GOOD {goodCount}   MISS {missCount}", x + 38, y + 251, 17, Muted);
        for (int i = 0; i < 3; i++)
        {
            string[] names = { "重新演奏", "觀看回放", "返回選曲" };
            var button = new Rectangle(x + 38 + i * 229, y + 319, 211, 48);
            Rect(button, resultMenuIndex == i ? new Color(45, 81, 80) : new Color(30, 46, 54));
            DrawRectBorder(button, resultMenuIndex == i ? Jade : Muted * 0.3f);
            CenterLabel(names[i], button.Center.X, button.Y + 10, 19);
        }
        Label("© MoriTeahouse（森之宿茶室）", x + 38, y + 386, 13, Muted);
    }
}
