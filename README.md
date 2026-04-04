# RhythmClicker — 節奏遊戲

> **這是第一個測試版（v0.1-alpha）。** 遊戲仍在開發中，歡迎回報問題與建議。

一款使用 MonoGame (DesktopGL) 建置的節奏遊戲，具備現代化暗色 UI、多語言支援（English / 繁中-台灣 / 簡中-中國大陸 / 繁中-香港 / English-US）以及帳號系統。

## 下載

👉 **[下載最新版本 (Windows x64)](https://github.com/keeiv/RhythmClicker/releases/latest)** — 解壓縮後直接執行 `ClickerGame.exe`，不需安裝 .NET。

請參閱 `DEVELOPMENT.md` 與 `CONTRIBUTING.md` 取得建置、執行與協作說明。

快速開始（Windows）
1. 取得此 repository：`git clone https://github.com/keeiv/RhythmClicker.git`
2. 建置：
```
dotnet build RhythmClicker/ClickerGame.csproj -c Debug
```
3. 執行：
```
dotnet run --project RhythmClicker/ClickerGame.csproj -c Debug
```

跨平台注意事項
- `TextRenderer.cs` 使用 `System.Drawing.Common`（目前主要在 Windows 測試）；若要跨平台建議改用 MonoGame `SpriteFont`。

回報問題或貢獻
- 想貢獻請先閱讀 `CONTRIBUTING.md`，有任何錯誤或改善建議請開 issue。

