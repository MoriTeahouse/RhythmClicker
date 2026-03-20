# ClickerGame — MonoGame 範例節奏遊戲

這是一個極簡的節奏遊戲範例，使用 MonoGame (DesktopGL) 建置。專案包含：

- 一個最小可運行的 MonoGame 專案 (`ClickerGame.csproj`)。
- 簡單的落點節奏遊戲核心（在 `Game1.cs`）。
- 範例 beatmap：`Assets/beatmap.json`。
- 若 `Assets/song.wav` 不存在，遊戲會在第一次啟動時自動產生一段示例音訊（純正弦波，3 秒）。

快速上手

1. 安裝 MonoGame 桌面環境（建議使用 MonoGame 3.8+）。
2. 開啟解決方案：`ClickerGame.sln`（或開啟 `ClickerGame` 資料夾作為專案）。
3. 復原 NuGet 套件，並以 `dotnet build` 或 Visual Studio/VS Code 執行專案。
4. 遊戲操作：按 `D F J K` 對應四個欄位；Esc 離開。

專案結構

- `ClickerGame/` — 專案檔與原始碼
  - `Program.cs`, `Game1.cs`, `Beatmap.cs`
  - `Assets/beatmap.json`, `Assets/song.wav` (由程式在缺少時生成)
- `CODING_STYLE.md` — 程式碼風格
- `LICENSE` — MIT

設計說明（中文）

遊戲採用極簡架構：

- `Game1` 負責遊戲循環、載入 `beatmap.json`、在缺少音訊時產生示例 `.wav`、播放並用 `Stopwatch` 對齊時間軸。
- `Beatmap` 類別負責從 JSON 反序列化為 `Note` 列表，`Note` 含 `time`（秒）與 `column`（0..3）。
- 畫面以四欄呈現，音符會從上方下降到欄位底端，玩家按鍵命中最近的音符即可得分。

如何貢獻

- Fork 並建立分支
- 修正或新增功能後，提出 PR
- 請確保遵守 `CODING_STYLE.md` 中的規範

如果需要，我可以把遊戲擴充為使用 Content Pipeline、加入視覺反饋與音訊同步優化。