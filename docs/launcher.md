# RhythmClicker 專屬啟動器

MoriTeahouse（森之宿茶室）開發。啟動器 1.0.0-test.1；遊戲 0.6.1-test.2 為大更新測試版第二版。原創程式採 AGPL-3.0-only。圖標與 README 的 `ClickerGame/icon.svg` 相同，直接轉為七種尺寸 ICO；MonoGame 視窗嵌入同源 BMP。

## 安裝與更新

解壓縮整份啟動器 ZIP，執行 `RhythmClickerLauncher.exe`。啟動器自帶 .NET 8 Windows Desktop 環境，首次執行不需要另裝 .NET。選擇硬碟內的安裝資料夾，再按「檢查更新」與「下載並安裝」。預設包含測試版本，版本取自公開的 MoriTeahouse/RhythmClicker GitHub Releases；下載不需登入或 token。正式版頻道不會安裝 prerelease。

支援本機 ZIP／ATR 安裝。應使用本專案的完整 Windows x64 封裝，ATR 必須含 RhythmClicker 產品中繼資料。內容專案 ATR 與其他遊戲 ATR 不視為可安裝音遊。離線封裝的雜湊僅驗證複製完整性；發布來源信任仍由取得檔案的管道決定。

| 選定安裝目錄內 | 用途 |
|---|---|
| `Game/<版本-雜湊-識別碼>/` | 遊戲、.NET、SDL2、OpenAL 與所需原生函式庫 |
| `UserData/` | 曲庫、譜面、設定、帳號、重播及統計 |
| `.launcher/jobs/` | 安裝期間的下載與解封裝暫存；完成、失敗及取消後清理 |
| `.launcher/install.json` | 目前啟用版本指標，更新採原子替換 |

更新核對 GitHub 描述、檔案大小、SHA-256、路徑與必要環境；新版本完整部署後才更換指標。取消或失敗不切換版本，不覆寫 `UserData`。舊遊戲目錄保留供手動回退；首版不自動刪除舊版本。兩個啟動器同時安裝會由檔案鎖拒絕第二項工作。

遊戲使用 app-local 執行環境，不要求系統管理員權限；新版安裝前會實際啟動 `--check-environment`，驗證 OpenGL／OpenAL／WaveOut，探針資料放在當次安裝暫存中。系統顯示卡驅動與 Windows 音訊裝置仍由作業系統提供。舊 0.6.0 ZIP 尚無探針旗標，只檢查自帶環境檔案，不會以未知參數啟動遊戲。

啟動器傳入 `RHYTHMCLICKER_DATA_ROOT`。新版遊戲直接執行 EXE 時也讀取相對 `rhythmclicker-storage.json`，所以整個安裝資料夾搬到其他硬碟後，仍使用同一份 `UserData`。資料路徑優先順序：`--data-root`、環境變數、相對 marker、既有絕對 `install_path.txt`、EXE 旁 `UserData`。舊 0.6.0 直接啟動使用絕對 marker，搬移後應透過啟動器執行。

啟動器只在 `%LOCALAPPDATA%/RhythmClickerLauncher/bootstrap.json` 保存所選資料夾與頻道；遊戲、玩家資料及下載暫存均在所選硬碟。既有玩家資料可完整複製至 `UserData`，或直接執行遊戲並以 `--data-root` 指定原資料夾；啟動器不自動搜尋或合併帳號資料。

## 開發與發布

```powershell
dotnet build src/RhythmClicker.Launcher -c Release -warnaserror
dotnet run --project tests/RhythmClicker.Launcher.Tests -c Release
pwsh -File scripts/Publish-Launcher.ps1
pwsh -File scripts/Publish-Windows.ps1 -ReleaseTag v0.6.1-test.2 -AtrPath ./artifacts/RhythmClicker.atr
```

標籤工作流產出遊戲 ZIP、ATR、SHA-256、`rhythmclicker-release.json` 與啟動器 ZIP，先建立草稿。發布草稿後，啟動器即可看到新版本；其版本查詢含 prerelease，不依賴僅供穩定版的 `version.json`。描述中的 `version` 必須等於去除 v 的 GitHub 標籤，大小／雜湊必須來自實際資產。

啟動器更新遊戲；首版啟動器自身的升級使用發布頁上的新版 ZIP。下載後更新整份啟動器資料夾即可，安裝路徑偏好與玩家資料不受影響。

安裝回歸涵蓋版本排序、頻道、雜湊失敗、取消、ZIP 越界、檔案鎖、原子指標、資料保留、ATR 安裝及來源限制。可在實際 Windows 桌面以 `--smoke --output <輸出目錄> --root <安裝資料夾>` 執行啟動器介面與匿名發布查詢探針。真實下載／裝置測試另使用隔離資料夾，與純資料回歸分開。
