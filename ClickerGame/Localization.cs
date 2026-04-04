using System.Collections.Generic;

namespace ClickerGame
{
    public enum GameLanguage
    {
        English,
        EnglishUS,
        ZhTW,
        ZhCN,
        ZhHK,
    }

    public static class Localization
    {
        static GameLanguage _current = GameLanguage.English;

        public static GameLanguage Current
        {
            get => _current;
            set => _current = value;
        }

        public static readonly GameLanguage[] All = new[]
        {
            GameLanguage.English,
            GameLanguage.EnglishUS,
            GameLanguage.ZhTW,
            GameLanguage.ZhCN,
            GameLanguage.ZhHK,
        };

        public static string LanguageDisplayName(GameLanguage lang) => lang switch
        {
            GameLanguage.English   => "English",
            GameLanguage.EnglishUS => "English (US)",
            GameLanguage.ZhTW      => "繁體中文（台灣）",
            GameLanguage.ZhCN      => "简体中文（中国大陆）",
            GameLanguage.ZhHK      => "繁體中文（香港）",
            _ => "English",
        };

        static readonly Dictionary<string, Dictionary<GameLanguage, string>> _strings = new()
        {
            ["title"] = new()
            {
                [GameLanguage.English]   = "CLICK",
                [GameLanguage.EnglishUS] = "CLICK",
                [GameLanguage.ZhTW]      = "CLICK",
                [GameLanguage.ZhCN]      = "CLICK",
                [GameLanguage.ZhHK]      = "CLICK",
            },
            ["menu_start"] = new()
            {
                [GameLanguage.English]   = "Start Game",
                [GameLanguage.EnglishUS] = "Start Game",
                [GameLanguage.ZhTW]      = "開始遊戲",
                [GameLanguage.ZhCN]      = "开始游戏",
                [GameLanguage.ZhHK]      = "開始遊戲",
            },
            ["menu_editor"] = new()
            {
                [GameLanguage.English]   = "Editor",
                [GameLanguage.EnglishUS] = "Editor",
                [GameLanguage.ZhTW]      = "編輯器",
                [GameLanguage.ZhCN]      = "编辑器",
                [GameLanguage.ZhHK]      = "編輯器",
            },
            ["menu_account"] = new()
            {
                [GameLanguage.English]   = "Account",
                [GameLanguage.EnglishUS] = "Account",
                [GameLanguage.ZhTW]      = "帳號",
                [GameLanguage.ZhCN]      = "账号",
                [GameLanguage.ZhHK]      = "帳號",
            },
            ["menu_language"] = new()
            {
                [GameLanguage.English]   = "Language",
                [GameLanguage.EnglishUS] = "Language",
                [GameLanguage.ZhTW]      = "語言",
                [GameLanguage.ZhCN]      = "语言",
                [GameLanguage.ZhHK]      = "語言",
            },
            ["menu_exit"] = new()
            {
                [GameLanguage.English]   = "Exit",
                [GameLanguage.EnglishUS] = "Exit",
                [GameLanguage.ZhTW]      = "離開",
                [GameLanguage.ZhCN]      = "退出",
                [GameLanguage.ZhHK]      = "離開",
            },
            ["hint_menu"] = new()
            {
                [GameLanguage.English]   = "\u2191\u2193 Select   \u25C0\u25B6 Song   Tab Difficulty   Enter Confirm   Esc Exit",
                [GameLanguage.EnglishUS] = "\u2191\u2193 Select   \u25C0\u25B6 Song   Tab Difficulty   Enter Confirm   Esc Exit",
                [GameLanguage.ZhTW]      = "\u2191\u2193 選擇   \u25C0\u25B6 切歌   Tab 難度   Enter 確認   Esc 離開",
                [GameLanguage.ZhCN]      = "\u2191\u2193 选择   \u25C0\u25B6 切歌   Tab 难度   Enter 确认   Esc 退出",
                [GameLanguage.ZhHK]      = "\u2191\u2193 選擇   \u25C0\u25B6 切歌   Tab 難度   Enter 確認   Esc 離開",
            },
            ["difficulty"] = new()
            {
                [GameLanguage.English]   = "Difficulty",
                [GameLanguage.EnglishUS] = "Difficulty",
                [GameLanguage.ZhTW]      = "難度",
                [GameLanguage.ZhCN]      = "难度",
                [GameLanguage.ZhHK]      = "難度",
            },
            ["score"] = new()
            {
                [GameLanguage.English]   = "Score",
                [GameLanguage.EnglishUS] = "Score",
                [GameLanguage.ZhTW]      = "分數",
                [GameLanguage.ZhCN]      = "分数",
                [GameLanguage.ZhHK]      = "分數",
            },
            ["result"] = new()
            {
                [GameLanguage.English]   = "RESULT",
                [GameLanguage.EnglishUS] = "RESULT",
                [GameLanguage.ZhTW]      = "結算",
                [GameLanguage.ZhCN]      = "结算",
                [GameLanguage.ZhHK]      = "結算",
            },
            ["max_combo"] = new()
            {
                [GameLanguage.English]   = "Max Combo",
                [GameLanguage.EnglishUS] = "Max Combo",
                [GameLanguage.ZhTW]      = "最大連擊",
                [GameLanguage.ZhCN]      = "最大连击",
                [GameLanguage.ZhHK]      = "最大連擊",
            },
            ["hit"] = new()
            {
                [GameLanguage.English]   = "Hit",
                [GameLanguage.EnglishUS] = "Hit",
                [GameLanguage.ZhTW]      = "命中",
                [GameLanguage.ZhCN]      = "命中",
                [GameLanguage.ZhHK]      = "命中",
            },
            ["miss"] = new()
            {
                [GameLanguage.English]   = "Miss",
                [GameLanguage.EnglishUS] = "Miss",
                [GameLanguage.ZhTW]      = "失誤",
                [GameLanguage.ZhCN]      = "失误",
                [GameLanguage.ZhHK]      = "失誤",
            },
            ["accuracy"] = new()
            {
                [GameLanguage.English]   = "Accuracy",
                [GameLanguage.EnglishUS] = "Accuracy",
                [GameLanguage.ZhTW]      = "準確率",
                [GameLanguage.ZhCN]      = "准确率",
                [GameLanguage.ZhHK]      = "準確率",
            },
            ["retry"] = new()
            {
                [GameLanguage.English]   = "Retry",
                [GameLanguage.EnglishUS] = "Retry",
                [GameLanguage.ZhTW]      = "重試",
                [GameLanguage.ZhCN]      = "重试",
                [GameLanguage.ZhHK]      = "重試",
            },
            ["menu"] = new()
            {
                [GameLanguage.English]   = "Menu",
                [GameLanguage.EnglishUS] = "Menu",
                [GameLanguage.ZhTW]      = "選單",
                [GameLanguage.ZhCN]      = "菜单",
                [GameLanguage.ZhHK]      = "選單",
            },
            ["account_register"] = new()
            {
                [GameLanguage.English]   = "Account Register",
                [GameLanguage.EnglishUS] = "Account Register",
                [GameLanguage.ZhTW]      = "帳號註冊",
                [GameLanguage.ZhCN]      = "账号注册",
                [GameLanguage.ZhHK]      = "帳號註冊",
            },
            ["username"] = new()
            {
                [GameLanguage.English]   = "Username",
                [GameLanguage.EnglishUS] = "Username",
                [GameLanguage.ZhTW]      = "使用者名稱",
                [GameLanguage.ZhCN]      = "用户名",
                [GameLanguage.ZhHK]      = "用戶名稱",
            },
            ["password"] = new()
            {
                [GameLanguage.English]   = "Password",
                [GameLanguage.EnglishUS] = "Password",
                [GameLanguage.ZhTW]      = "密碼",
                [GameLanguage.ZhCN]      = "密码",
                [GameLanguage.ZhHK]      = "密碼",
            },
            ["hint_account"] = new()
            {
                [GameLanguage.English]   = "Enter Register  \u00B7  Tab Switch  \u00B7  Esc Cancel",
                [GameLanguage.EnglishUS] = "Enter Register  \u00B7  Tab Switch  \u00B7  Esc Cancel",
                [GameLanguage.ZhTW]      = "Enter 註冊  \u00B7  Tab 切換  \u00B7  Esc 取消",
                [GameLanguage.ZhCN]      = "Enter 注册  \u00B7  Tab 切换  \u00B7  Esc 取消",
                [GameLanguage.ZhHK]      = "Enter 註冊  \u00B7  Tab 切換  \u00B7  Esc 取消",
            },
            ["register_success"] = new()
            {
                [GameLanguage.English]   = "Registered successfully",
                [GameLanguage.EnglishUS] = "Registered successfully",
                [GameLanguage.ZhTW]      = "註冊成功",
                [GameLanguage.ZhCN]      = "注册成功",
                [GameLanguage.ZhHK]      = "註冊成功",
            },
            ["editor_hint"] = new()
            {
                [GameLanguage.English]   = "EDITOR  \u00B7  S to save",
                [GameLanguage.EnglishUS] = "EDITOR  \u00B7  S to save",
                [GameLanguage.ZhTW]      = "編輯模式  \u00B7  S 儲存",
                [GameLanguage.ZhCN]      = "编辑模式  \u00B7  S 保存",
                [GameLanguage.ZhHK]      = "編輯模式  \u00B7  S 儲存",
            },
            ["unknown"] = new()
            {
                [GameLanguage.English]   = "Unknown",
                [GameLanguage.EnglishUS] = "Unknown",
                [GameLanguage.ZhTW]      = "未知",
                [GameLanguage.ZhCN]      = "未知",
                [GameLanguage.ZhHK]      = "未知",
            },
            ["select_language"] = new()
            {
                [GameLanguage.English]   = "Select Language",
                [GameLanguage.EnglishUS] = "Select Language",
                [GameLanguage.ZhTW]      = "選擇語言",
                [GameLanguage.ZhCN]      = "选择语言",
                [GameLanguage.ZhHK]      = "選擇語言",
            },
            ["hint_language"] = new()
            {
                [GameLanguage.English]   = "\u2191\u2193 Select   Enter Confirm   Esc Back",
                [GameLanguage.EnglishUS] = "\u2191\u2193 Select   Enter Confirm   Esc Back",
                [GameLanguage.ZhTW]      = "\u2191\u2193 選擇   Enter 確認   Esc 返回",
                [GameLanguage.ZhCN]      = "\u2191\u2193 选择   Enter 确认   Esc 返回",
                [GameLanguage.ZhHK]      = "\u2191\u2193 選擇   Enter 確認   Esc 返回",
            },
        };

        public static string Get(string key)
        {
            if (_strings.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(_current, out var val)) return val;
                if (dict.TryGetValue(GameLanguage.English, out var fallback)) return fallback;
            }
            return key;
        }
    }
}
