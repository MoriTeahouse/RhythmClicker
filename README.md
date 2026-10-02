# RhythmClicker
<img src="ClickerGame/icon.svg" alt="RhythmClicker" width="96" />

RhythmClicker 由 **MoriTeahouse（森之宿茶室）** 開發，是採 AGPL-3.0-only 的四軌落鍵音遊。0.6.1-test.2 是大更新測試版第二版：加入精美專屬啟動器、公開 GitHub 更新、ZIP／ATR 安裝與實際執行環境檢查；遊戲、視窗、工作列與啟動器使用本頁 SVG 直接轉換的圖標。延續茶室介面、原創三曲九張譜面、裝置位置判定、倒數、暫停、校正、練習與精確重播。

## 專屬啟動器

從 [公開發布](https://github.com/MoriTeahouse/RhythmClicker/releases) 下載並解壓縮整份 `RhythmClickerLauncher.zip`，執行 `RhythmClickerLauncher.exe`。選擇安裝硬碟／資料夾，再下載遊戲；免登入、免另裝 .NET。遊戲、執行環境、玩家資料與下載暫存使用所選硬碟。更新驗證完成才啟用，失敗或取消保留原版與玩家資料。

支援測試版與正式版頻道、本機 ZIP／ATR 安裝、啟動遊戲及開啟資料目錄。詳見 [啟動器指南](docs/launcher.md)。

## 執行

Windows x64 的自包含發布包可直接執行 `ClickerGame.exe`，已包含 .NET 執行環境與所需原生函式庫。需要相容 MonoGame DesktopGL 的圖形驅動；首次啟動會產生原創演示音訊。

獨立執行預設使用 EXE 旁 `UserData`；啟動器安裝則統一使用安裝根目錄的 `UserData`，曲庫、帳號、統計與重播不隨版本切換。優先使用 `--data-root <完整路徑>` 或 `RHYTHMCLICKER_DATA_ROOT`，其次為可隨整個資料夾搬移的相對 marker，既有 `install_path.txt` 仍相容。舊資料可用同一參數指定原有目錄，無須覆寫曲庫。

| 操作 | 功能 |
|---|---|
| D / F / J / K | 四軌打擊；設定可更換按鍵 |
| ↑ / ↓、Enter | 主選單；滑鼠可點選 |
| ← / →、Tab | 難度、下一首 |
| Space、Esc | 暫停／繼續、返回 |
| F11 | 全螢幕 |
| 設定中 C、Space、Enter | 啟動校正、跟拍、套用 |
| 主選單 U | 有更新時下載／啟動已驗證版本 |

視覺偏移與輸入偏移分開；練習模式不寫入排行統計。正確率使用 PERFECT 100、GREAT 75、GOOD 50 的加權分數，漏鍵與提早失敗的剩餘音符均計入結算。

## 開發

需要 .NET 8 SDK 與 Windows。
```sh
git clone --recurse-submodules https://github.com/MoriTeahouse/RhythmClicker.git
cd RhythmClicker
dotnet run --project ClickerGame/ClickerGame.csproj -c Release
dotnet run --project tests/RhythmClicker.Tests -c Release
```

正式入口位於 `ClickerGame/`，根目錄專案保留相容轉接；引擎以 `lib/MatrixTea-Engine` 子模組固定版本。MonoGame 與音訊／字形共用服務由引擎提供。

[開發指南](docs/development.md) · [玩家說明](docs/PLAYER.md) · [第三方元件](THIRD-PARTY-NOTICES.md) · [公開版本](https://github.com/MoriTeahouse/RhythmClicker/releases)

## 授權與內容

本版原創程式與演示曲譜採 [AGPL-3.0-only](LICENSE)。完整條文、第三方聲明與對應原始碼取得資訊隨包提供；舊版 MIT 授權不溯及撤銷。匯入歌曲、影片、圖片及譜面保留原作者權利。

歷史根目錄 `Assets/` 不納入新版預設建置；首次啟動只生成原創演示曲，不覆寫既有自訂檔案。發布工作流先建立草稿版本，發布後才更新公開下載描述。
