// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace ClickerGame;
public partial class Game1
{
    private readonly Keys[] cachedLaneKeys = { Keys.D, Keys.F, Keys.J, Keys.K };
    private readonly string[] cachedLaneLabels = { "D", "F", "J", "K" };
    private readonly string[] cachedBindingNames = { "", "", "", "" };
    Keys[] GetLaneKeys()
    {
        var s = settingsManager?.Settings;
        if (s == null) return cachedLaneKeys;
        if (s.Lane0Key != cachedBindingNames[0] || s.Lane1Key != cachedBindingNames[1] || s.Lane2Key != cachedBindingNames[2] || s.Lane3Key != cachedBindingNames[3])
        {
            s.Normalize(); string[] names = { s.Lane0Key, s.Lane1Key, s.Lane2Key, s.Lane3Key };
            for (int i = 0; i < 4; i++) { cachedLaneKeys[i] = Enum.Parse<Keys>(names[i], true); cachedLaneLabels[i] = cachedLaneKeys[i].ToString(); cachedBindingNames[i] = names[i]; }
        }
        return cachedLaneKeys;
    }
    string[] GetLaneKeyLabels() { GetLaneKeys(); return cachedLaneLabels; }
    void UpdateSettings()
    {
        var s = settingsManager!.Settings;
        if (Pressed(Keys.C)) { StartCalibration(); return; }
        if (settingsBindingMode)
        {
            foreach (Keys key in kb.GetPressedKeys())
            {
                if (key is Keys.None or Keys.Escape or Keys.F11 or Keys.Space or Keys.Enter || prevKb.IsKeyDown(key)) continue;
                string[] names = { s.Lane0Key, s.Lane1Key, s.Lane2Key, s.Lane3Key };
                int existing = Array.FindIndex(names, n => Enum.TryParse<Keys>(n, true, out var k) && k == key);
                if (existing >= 0 && existing != settingsBindingLane) names[existing] = names[settingsBindingLane];
                names[settingsBindingLane] = key.ToString();
                (s.Lane0Key, s.Lane1Key, s.Lane2Key, s.Lane3Key) = (names[0], names[1], names[2], names[3]);
                settingsBindingMode = false; settingsManager.Save(); return;
            }
            return;
        }
        const int count = 12;
        if (Pressed(Keys.Up)) settingsMenuIndex = (settingsMenuIndex + count - 1) % count;
        if (Pressed(Keys.Down)) settingsMenuIndex = (settingsMenuIndex + 1) % count;
        int direction = Pressed(Keys.Right) ? 1 : Pressed(Keys.Left) ? -1 : 0;
        if (direction != 0)
        {
            switch (settingsMenuIndex)
            {
                case 0: s.MasterVolume += direction * 0.05f; break;
                case 1: s.MusicVolume += direction * 0.05f; break;
                case 2: s.SfxVolume += direction * 0.05f; break;
                case 3: s.OffsetMs += direction * 5; break;
                case 8: s.VisualOffsetMs += direction * 5; break;
                case 9: s.ApproachSeconds += direction * 0.1f; break;
                case 10: s.PracticeMode = !s.PracticeMode; break;
                case 11: s.ReducedEffects = !s.ReducedEffects; break;
            }
            settingsManager.Save(); ApplyVolume();
        }
        if (Pressed(Keys.Enter) && settingsMenuIndex is >= 4 and <= 7) { settingsBindingMode = true; settingsBindingLane = settingsMenuIndex - 4; }
        if (Pressed(Keys.Enter) && settingsMenuIndex >= 10) { if (settingsMenuIndex == 10) s.PracticeMode = !s.PracticeMode; else s.ReducedEffects = !s.ReducedEffects; settingsManager.Save(); }
    }
    void ApplyVolume()
    {
        if (settingsManager == null) return;
        var s = settingsManager.Settings;
        if (menuMusicInstance != null) menuMusicInstance.Volume = s.MasterVolume * s.MusicVolume;
        if (songInstance != null) songInstance.Volume = s.MasterVolume * s.MusicVolume;
        audioPlayer.Volume = s.MasterVolume * s.MusicVolume;
    }
    void DrawSettings()
    {
        int x = width / 2 - 330, y = 70; Panel(new(x, y, 660, 580));
        Label("演奏設定", x + 32, y + 18, 28, Jade);
        Label("C 開啟延遲校正", x + 408, y + 27, 17, Gold);
        var s = settingsManager!.Settings;
        string[] titles = { "主音量", "音樂音量", "效果音量", "輸入偏移", "第一軌按鍵", "第二軌按鍵", "第三軌按鍵", "第四軌按鍵", "視覺偏移", "接近時間 / 速度", "練習模式", "減少畫面效果" };
        string[] values = { $"{s.MasterVolume:P0}", $"{s.MusicVolume:P0}", $"{s.SfxVolume:P0}", $"{s.OffsetMs:+0;-0;0} ms", s.Lane0Key, s.Lane1Key, s.Lane2Key, s.Lane3Key, $"{s.VisualOffsetMs:+0;-0;0} ms", $"{s.ApproachSeconds:F1} s", s.PracticeMode ? "開" : "關", s.ReducedEffects ? "開" : "關" };
        for (int i = 0; i < titles.Length; i++)
        {
            int row = y + 82 + i * 36;
            if (settingsMenuIndex == i) { Rect(new(x + 22, row - 3, 616, 32), Jade * 0.08f); Rect(new(x + 22, row - 3, 3, 32), Jade); }
            Label(titles[i], x + 38, row, 18, settingsMenuIndex == i ? Cream : Muted);
            string value = settingsBindingMode && settingsBindingLane == i - 4 ? "按下新按鍵…" : values[i];
            Label(value, x + 451, row, 18, Gold);
        }
        Label("↑ ↓ 選擇    ← → 調整    ENTER 更改按鍵    ESC 返回", x + 32, y + 532, 15, Muted);
    }
}
