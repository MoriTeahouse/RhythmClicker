// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using MatrixTea.Engine.Core.Rhythm;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
namespace ClickerGame;
public partial class Game1
{
    private bool _calibrating;
    private LatencyCalibration? calibration;
    private double calibrationStart;
    private int lastCalibrationBeat = -1, lastTappedBeat = -1;
    private string calibrationMessage = "";
    private void StartCalibration()
    {
        calibration = new(); _calibrating = true; calibrationStart = HostNow + 1; lastCalibrationBeat = lastTappedBeat = -1;
        calibrationMessage = "跟著聲音按 SPACE，前兩拍暖身。"; menuMusicInstance?.Pause();
    }
    private void UpdateCalibration()
    {
        double elapsed = HostNow - calibrationStart;
        int beat = (int)Math.Floor(elapsed / 0.6);
        if (beat >= 0 && beat != lastCalibrationBeat) { lastCalibrationBeat = beat; sfxHit?.Play(0.65f * SfxVolume, 0, 0); }
        if (Pressed(Keys.Escape)) { _calibrating = false; menuMusicInstance?.Resume(); return; }
        if (Pressed(Keys.Space) && elapsed >= 0)
        {
            int nearest = (int)Math.Round(elapsed / 0.6);
            if (nearest != lastTappedBeat)
            {
                lastTappedBeat = nearest; calibration!.AddTap(elapsed, nearest * 0.6);
                if (calibration.IsReady) calibrationMessage = $"建議輸入偏移：{calibration.RecommendedOffsetMs} ms。ENTER 套用。";
            }
        }
        if (Pressed(Keys.Enter) && calibration!.IsReady)
        {
            settingsManager!.Settings.OffsetMs = calibration.RecommendedOffsetMs; settingsManager.Save();
            _calibrating = false; menuMusicInstance?.Resume();
        }
    }
    private void DrawCalibrationOverlay()
    {
        Rect(new(0, 0, width, height), Ink * 0.95f);
        CenterLabel("延遲校正", width / 2, height / 2 - 118, 32, Jade);
        CenterLabel(calibrationMessage, width / 2, height / 2 - 51, 18);
        CenterLabel($"有效樣本 {calibration?.Samples ?? 0} / 8", width / 2, height / 2 - 9, 18, Gold);
        float phase = (float)((HostNow - calibrationStart) / 0.6 % 1);
        if (phase >= 0) spriteBatch!.Draw(circleTexture!, new Rectangle(width / 2 - 20, height / 2 + 44, 40, 40), Jade * (1 - phase * 0.7f));
        CenterLabel("SPACE 跟拍    ENTER 套用    ESC 取消", width / 2, height / 2 + 117, 16, Muted);
    }
}
