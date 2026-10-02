# 開發指南

RhythmClicker 由 MoriTeahouse（森之宿茶室）開發。公開原創程式、曲譜及程式繪製的美術採 AGPL-3.0-only；第三方元件及外部曲庫依各自授權管理。

## 結構

| 目錄 | 責任 |
|---|---|
| ClickerGame/Core | 安裝資料路徑、原創曲庫、獨立演奏狀態 |
| ClickerGame/Screens | 選曲、演奏、結算、設定、重播與既有工具畫面 |
| ClickerGame/UI | 茶室視覺語彙及字形介面 |
| ClickerGame/Systems | 驗證更新、Unicode 原生函式庫載入 |
| lib/MatrixTea-Engine | 固定版本的共用引擎 |
| tests/RhythmClicker.Tests | 不建立圖形視窗的回歸 |
| ClickerServer / ClickerLauncher | 既有選用服務與下載助手 |

根目錄的舊重複遊戲程式已移除，原始修訂仍在 Git 歷史；根專案只轉呼叫正式入口。bin／obj 與執行資料不納入版本控制。個別遊戲專案建置不要求啟動伺服器。

## 計時與判定

歌曲時間以輸出裝置位置為準，解碼預讀時間不供判定。開始前有 1.8 秒倒數。失去焦點會暫停歌曲與重播，玩家明確恢復後同步按住狀態；恢復時不產生幽靈打擊。輸入先判定，逾期漏鍵隨後蒐集，最後一批事件結束後才結算。

PlayRun 使用引擎欄位索引；不得在演奏中改寫音符時間／欄位。完整正確率包含整張譜面，演奏 HUD 使用已判定音符的加權比例。練習模式不寫入排行統計或成就。校正使用暖身後敲擊中位數；人工與幀節拍校正不是取樣級硬體延遲測試。

重播保留譜面 SHA-256、音符身分與實際事件時間，變更譜面後拒絕不相符的新格式重播。既有無雜湊紀錄保留相容選取。重播不再次寫入統計。

## 檔案與資源

所有持久資料由 AppPaths 或 --data-root 決定。預設執行檔旁 UserData；完整路徑 marker、命令列與環境變數可選擇安裝磁碟。首次啟動只補足缺少的示範曲／譜面；不覆寫使用者編輯。

RC／RCM／RCP 原格式維持相容，寫入採同目錄原子替換並保留 .bak。固定金鑰 AES-CBC 只提供格式混淆，不能當成帳號資訊的強安全邊界。設定損壞可讀上一份備份。OSZ 匯入限制路徑、檔案數與展開大小，以選定資料目錄中的唯一 staging 避免同名匯入互相刪除。

字元快取重用動態分數；舊畫面整行紋理快取受 384 張／32 MiB 上限約束。影片背景由工作執行緒解碼像素，GPU 建立、上傳及釋放在遊戲執行緒執行。Windows 原生函式庫載入支援中文路徑。

## 驗證

```sh
dotnet build ClickerGame/ClickerGame.csproj -c Release -warnaserror
dotnet build ClickerGame.csproj -c Release -warnaserror
dotnet run --project tests/RhythmClicker.Tests -c Release
dotnet run --project ClickerGame/ClickerGame.csproj -c Release -- --smoke --data-root ./artifacts/probe --capture ./artifacts/captures
```

圖形探針可額外使用 --smoke-video <影片完整路徑>。探針建立自己的短曲與譜面，檢查倒數、裝置位置、暫停、結算、儲存與讀回重播；使用程式驅動命中，不能取代人工鍵盤手感與長時間遊玩測試。2026-10-02 本機回歸 1,144 項通過，實際 GPU、音訊與影片探針通過。

## 發布

scripts/Publish-Windows.ps1 產生自包含 win-x64 目錄、ZIP、SHA-256 與 SOURCE.md。原生 LGPL 依賴保持可替換的獨立 DLL；不合併單檔，不打包使用者資料。版本發布含遊戲與引擎對應修訂來源連結、完整授權及依賴資料。

GitHub tag 工作流建立草稿；對外發布需依專案維護流程進行。穩定版本正式發布後，發布事件工作流下載實際 ZIP 計算 SHA-256 再更新 version.json。更新器只接受所屬 GitHub 庫的 HTTPS release URL，先核對雜湊及 ZIP 路徑，再準備於 UserData/Updates；由玩家切換至新版本，沿用原資料目錄。

帳號／線上服務保留既有介面。本次驗證以離線遊戲及本機資料為範圍，未宣稱驗證公開伺服器登入、權限或可用性。
