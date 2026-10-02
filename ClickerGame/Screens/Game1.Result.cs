// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ClickerGame;

public partial class Game1
{
        void UpdateResult()
        {
            if (Pressed(Keys.Left)) resultMenuIndex = (resultMenuIndex + 2) % 3;
            if (Pressed(Keys.Right)) resultMenuIndex = (resultMenuIndex + 1) % 3;
            if (mouseState.LeftButton == ButtonState.Pressed && prevMouseState.LeftButton == ButtonState.Released)
                for (int i = 0; i < 3; i++) if (new Rectangle(width / 2 - 342 + i * 229, 449, 211, 48).Contains(mouseState.Position)) { resultMenuIndex = i; ExecuteResult(); return; }
            if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up)) resultMenuIndex = (resultMenuIndex - 1 + 3) % 3;
            if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down)) resultMenuIndex = (resultMenuIndex + 1) % 3;
            if (Pressed(Keys.Enter)) ExecuteResult();
        }
        void ExecuteResult()
        {
                if (resultMenuIndex == 0) StartPlaying(false);
                else if (resultMenuIndex == 1)
                {
                    // Watch replay
                    string songId = songs.Count > 0 ? songs[currentSongIndex].Id : "unknown";
                    var replay = replayManager?.GetBestReplay(songId, currentDifficulty);
                    if (replay != null) StartReplayView(replay);
                }
                else ReturnToMenu();
        }

        // ═══════════ Result ═══════════

        void DrawResult() => DrawModernResult();
}
