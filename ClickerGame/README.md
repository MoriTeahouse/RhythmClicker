# RhythmClicker

RhythmClicker 是一款使用 MonoGame 建置的 4 軌落鍵節奏遊戲，現已接入 MatrixTea Engine 的核心節奏模型與回放橋接層。

## 特色

- 4 軌落鍵玩法，使用 `D` `F` `J` `K` 對應四欄位。
- 完整判定與連擊系統，支援 PERFECT / GREAT / GOOD / MISS。
- 內建歌曲、譜面編輯器、統計、回放與 Discord Rich Presence。
- 可透過 `.rcm` 與 `.osz` 匯入內容。
- 已與 MatrixTea Engine 共用 beatmap、judgement 與 replay 模型。

## 系統需求

- Windows 10 / 11（64-bit）
- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) 或以上

## 建置與執行

```bash
dotnet build ClickerGame.csproj
dotnet run --project ClickerGame.csproj
```

首次啟動會自動生成示例歌曲與譜面，請等待資源初始化完成。

## 操作

| 按鍵 | 功能 |
|---|---|
| `D` `F` `J` `K` | 打擊四欄位 |
| `←` `→` | 切換難度 |
| `Tab` | 切換歌曲 |
| `↑` `↓` | 選單導航 |
| `Enter` | 確認 |
| `Esc` | 返回 / 離開 |

### 譜面編輯器

| 操作 | 功能 |
|---|---|
| 左鍵 | 放置音符 |
| 拖曳 | 移動音符 |
| 右鍵 | 刪除音符 |
| 滾輪 | 捲動時間軸 |
| `Tab` | 切換欄位 |
| `Space` | 預覽播放 |
| `Ctrl+S` | 儲存譜面 |

## 專案結構

```
ClickerGame/
├── Game1.cs            # 主遊戲狀態、輸入與渲染
├── Beatmap.cs          # 譜面資料模型
├── MatrixTeaIntegration.cs
├── AccountsManager.cs
├── StatsDatabase.cs
├── DiscordRpcManager.cs
├── RcFileManager.cs
├── Localization.cs
├── TextRenderer.cs
├── RenderCache.cs
├── ObjectPool.cs
├── GameConfig.cs
└── Assets/
```

## MatrixTea 整合

MatrixTea Engine 目前已提供共用的 beatmap、判定、計分與 replay 模型。
RhythmClicker 會逐步把更多更新邏輯移入引擎核心，讓 `Game1` 保留在畫面與遊戲流程協調的角色。

## 授權

MIT License