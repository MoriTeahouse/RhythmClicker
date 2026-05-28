# RhythmClicker — 節奏遊戲

一款使用 MonoGame (DesktopGL) 建置的 4 軌落鍵節奏遊戲。具備現代化暗色 UI、5 語言支援、帳號系統與 Discord Rich Presence。

## 下載

**[下載最新版本 (Windows x64)](https://github.com/MoriTeahouse/RhythmClicker/releases/latest)** — 解壓縮後直接執行 `ClickerGame.exe`，不需安裝 .NET。

## 快速開始

```bash
git clone https://github.com/MoriTeahouse/RhythmClicker.git
cd RhythmClicker
dotnet restore ClickerGame/ClickerGame.csproj
dotnet run --project ClickerGame/ClickerGame.csproj
```

> 首次啟動會自動產生音訊與譜面，需等待數秒。

## 操作方式

| 按鍵 | 功能 |
|------|------|
| `D` `F` `J` `K` | 打擊四欄位 |
| `◀` `▶` | 切換難度 |
| `Tab` | 切換歌曲 |
| `↑` `↓` `Enter` | 選單導航 |
| `Esc` | 返回 / 離開 |

詳細操作與譜面編輯器使用說明請參閱 [`ClickerGame/README.md`](ClickerGame/README.md)。

## 回報問題或貢獻

想貢獻請先閱讀 `CONTRIBUTING.md`，有任何錯誤或改善建議請開 issue。

