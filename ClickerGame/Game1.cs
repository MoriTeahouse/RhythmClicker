using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace ClickerGame
{
    /// <summary>
    /// Core game class. This is a partial class — screen logic is split into
    /// Screens/, audio generation into Audio/, and helper utilities into Helpers/.
    /// </summary>
    public partial class Game1 : Game
    {
        GraphicsDeviceManager? graphics;
        SpriteBatch? spriteBatch;
        Texture2D? pixel;
        int width = 800, height = 600;
        Beatmap? beatmap;
        LinkedList<Note> notes = new();
        SoundEffect? songEffect;
        SoundEffectInstance? songInstance;

        // Menu background music
        SoundEffect? menuMusicEffect;
        SoundEffectInstance? menuMusicInstance;

        Stopwatch stopwatch = new Stopwatch();
        int score = 0;
        KeyboardState kb, prevKb;
        MouseState mouseState, prevMouseState;
        int prevScrollValue;

        // Song selection
        List<SongInfo> songs = new();
        int currentSongIndex = 0;
        string currentDifficulty = "easy";

        // Menu / scene
        enum GameState { Menu, Playing, Result, Account, Language, Stats, BeatmapEditor, Settings, Achievements, ReplayView, Profile, SearchPlayer, EditProfile }
        GameState state = GameState.Menu;
        string[] menuKeys = new[] { "menu_start", "menu_editor", "menu_stats", "menu_profile", "menu_search", "menu_settings", "menu_achievements", "menu_account", "menu_language", "menu_exit" };
        int currentMenuIndex = 0;

        // Language selection
        int languageMenuIndex = 0;

        // Saved windowed dimensions
        int windowedWidth = 800;
        int windowedHeight = 600;

        TextRenderer? textRenderer;
        Texture2D? circleTexture;

        // input feedback
        public class KeyFlash
        {
            public Rectangle Rect;
            public Color Color;
            public float TimeToLive;
            public void Reset(Rectangle rect, Color color, float ttl)
            { Rect = rect; Color = color; TimeToLive = ttl; }
        }
        List<KeyFlash> keyFlashes = new();
        ObjectPool<KeyFlash>? keyFlashPool;

        // result
        int maxScore = 0;
        bool summaryShown = false;
        int resultMenuIndex = 0;
        double songDurationSeconds = 0.0;
        RenderCache? renderCache;

        // account
        AccountsManager? accountsManager;
        string accountUsername = string.Empty;
        string accountPassword = string.Empty;
        bool accountShowMessage = false;
        string accountMessage = string.Empty;
        int accountFieldIndex = 0;
        bool accountIsLoginMode = true;

        string resultGrade = "";

        // Combo/stats
        int combo = 0;
        int maxCombo = 0;
        int hitCount = 0;
        int missCount = 0;

        // Systems
        StatsDatabase? statsDb;
        DiscordRpcManager? discordRpc;
        SettingsManager? settingsManager;
        AchievementManager? achievementManager;
        ReplayManager? replayManager;
        CloudSyncManager? cloudSync;
        string syncStatusText = "";
        float syncStatusTimer = 0f;

        // Profile
        PlayerProfileDto? viewingProfile;
        bool profileLoading = false;
        int profileScrollIndex = 0;

        // Edit Profile
        int editProfileFieldIndex = 0; // 0=avatar, 1=banner, 2=bio, 3=region, 4=save
        string editBio = "";
        string editRegion = "";
        int editAvatarIndex = 0;
        int editBannerIndex = 0;
        bool editProfileSaving = false;
        string editProfileMessage = "";
        float editProfileMsgTimer = 0f;

        // Custom avatar
        Texture2D? customAvatarTexture;
        string customAvatarPath = "";
        static readonly string AvatarsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars");

        // Avatar/Banner presets
        static readonly (string id, string label, Color bg, Color fg, string icon)[] AvatarPresets = new[]
        {
            ("default",  "Default",   new Color(20, 20, 40),    new Color(0, 200, 255),   ""),
            ("blue",     "Ocean",     new Color(20, 60, 120),   new Color(100, 200, 255), "~"),
            ("red",      "Flame",     new Color(120, 20, 20),   new Color(255, 120, 80),  "*"),
            ("green",    "Forest",    new Color(20, 80, 40),    new Color(80, 255, 120),   "T"),
            ("purple",   "Nebula",    new Color(60, 20, 100),   new Color(200, 140, 255), "."),
            ("gold",     "Crown",     new Color(80, 60, 10),    new Color(255, 220, 50),  "W"),
            ("pink",     "Sakura",    new Color(100, 30, 60),   new Color(255, 160, 200), "*"),
            ("cyan",     "Ice",       new Color(10, 60, 80),    new Color(80, 240, 255),  "#"),
            ("orange",   "Sunset",    new Color(100, 50, 10),   new Color(255, 180, 60),  "~"),
            ("white",    "Ghost",     new Color(60, 60, 60),    new Color(220, 220, 230), "?"),
            ("custom",   "Custom",    new Color(40, 40, 40),    new Color(200, 200, 200), "+"),
        };
        static readonly (string id, string label, Color color)[] BannerPresets = new[]
        {
            ("default",  "Default",   new Color(30, 40, 80)),
            ("crimson",  "Crimson",   new Color(100, 20, 30)),
            ("navy",     "Navy",      new Color(15, 25, 80)),
            ("emerald",  "Emerald",   new Color(15, 70, 40)),
            ("violet",   "Violet",    new Color(60, 20, 90)),
            ("midnight", "Midnight",  new Color(10, 10, 30)),
            ("sunset",   "Sunset",    new Color(100, 50, 20)),
            ("rose",     "Rose",      new Color(90, 30, 50)),
        };

        // Search
        string searchQuery = "";
        List<PlayerSearchResult> searchResults = new();
        int searchSelectedIndex = 0;
        bool searchLoading = false;

        // Settings UI state
        int settingsMenuIndex = 0;
        bool settingsBindingMode = false;
        int settingsBindingLane = -1;

        // Achievement popup
        float achievementPopupTimer = 0f;
        string achievementPopupText = "";

        // Replay playback state
        ReplayData? replayData;
        int replayEventIndex;
        List<JudgmentPopup> replayJudgments = new();

        // Achievements screen scroll
        int achievementsScrollIndex = 0;

        // Difficulty abbreviation mapping
        static readonly Dictionary<string, string> DiffAbbrev = new()
        {
            ["easy"] = "EZ",
            ["hard"] = "HD",
            ["difficulty"] = "DIFF",
            ["very_difficulty"] = "VDIFF",
        };

        static string DiffShort(string d) => DiffAbbrev.TryGetValue(d, out var s) ? s : d.ToUpper();

        // Note colors
        static readonly Color[] NoteColors = { new(0, 200, 255), new(255, 60, 140), new(255, 220, 50), new(80, 255, 120) };
        static readonly string[] LaneKeys = { "D", "F", "J", "K" };

        // Judgment popup
        class JudgmentPopup { public string Text = ""; public Color Color; public float Timer; public Vector2 Position; }
        List<JudgmentPopup> judgmentPopups = new();

        // Particles
        class HitParticle { public Vector2 Pos, Vel; public Color Color; public float Life, MaxLife, Size; }
        List<HitParticle> particles = new();
        Random rng = new();

        float shakeTimer;
        float shakeIntensity;
        float beatPulseAlpha;
        SoundEffect? sfxHit;

        // ── Icon Registry ────────────────────────────────────────────
        UI.IconRegistry? _iconRegistry;

        // ── Update system ────────────────────────────────────────────
        float _updateCheckDelay = 5f;   // seconds after launch before first check
        bool  _updatePromptShown = false;

        SoundEffect? sfxMiss;

        // HP system (SAO-style)
        float hp = GameConfig.InitialHP;
        bool hpDepleted = false;

        // Judgment counters
        int perfectCount = 0;
        int greatCount = 0;
        int goodCount = 0;

        // Video background
        VideoBackgroundPlayer? videoPlayer;
        Texture2D? bgImageTexture;
        string currentVideoPath = "";
        string currentBgImagePath = "";

        // Break period tracking
        bool inBreak = false;
        BreakPeriod? currentBreak = null;

        // Combo tier
        int ComboTier => combo >= GameConfig.ComboTier4 ? 4
                       : combo >= GameConfig.ComboTier3 ? 3
                       : combo >= GameConfig.ComboTier2 ? 2
                       : combo >= GameConfig.ComboTier1 ? 1 : 0;

        static readonly Color[][] TierNoteColors = new[]
        {
            new[] { new Color(0, 200, 255), new Color(255, 60, 140), new Color(255, 220, 50), new Color(80, 255, 120) },
            new[] { new Color(0, 255, 200), new Color(100, 255, 150), new Color(50, 255, 255), new Color(150, 255, 100) },
            new[] { new Color(200, 255, 0), new Color(255, 255, 50), new Color(150, 255, 0), new Color(255, 200, 0) },
            new[] { new Color(255, 180, 0), new Color(255, 140, 0), new Color(255, 220, 50), new Color(255, 120, 0) },
            new[] { new Color(255, 50, 100), new Color(255, 0, 180), new Color(255, 100, 50), new Color(255, 30, 220) },
        };

        static readonly Color[] TierGlowColor = { new(0,200,255), new(0,255,180), new(200,255,0), new(255,180,0), new(255,50,100) };

        // Lane layout
        const int LaneCount = 4;
        const int LaneWidth = 90;
        const int TotalLaneWidth = LaneCount * LaneWidth;
        const int NoteHeight = 22;
        const int HitZoneHeight = 70;
        int LaneLeft => (width - TotalLaneWidth) / 2;
        int HitZoneY => height - HitZoneHeight - 40;

        float menuTimer = 0f;
        int menuScrollOffset = 0;
        bool isFullscreen = false;
        bool editorMode = false; // legacy playing-editor flag

        // ═══════════ Beatmap Editor State ═══════════
        string edSongName = "";
        string edAuthor = "";
        string edAudioPath = "";
        string edBpm = "120";
        List<Note> edNotes = new();
        float edScrollTime = 0f; // current scroll position in seconds
        float edTotalTime = 10f;
        int edFieldFocus = -1; // -1=timeline, 0=name, 1=author, 2=audio, 3=bpm
        string edMessage = "";
        float edMessageTimer = 0f;
        Note? edDragging = null;
        bool edPreviewing = false;
        SoundEffect? edPreviewEffect;
        SoundEffectInstance? edPreviewInstance;
        Stopwatch edPreviewWatch = new();

        float EdPixelsPerSecond => (height - 120) / 4f; // 4 seconds visible at once
        float EdVisibleSeconds => (height - 120) / EdPixelsPerSecond;

        // Stats cached
        PlayerSummary? cachedStats;
        List<PlayRecord>? cachedRecent;

        class SongInfo
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public string File { get; set; } = "";
            public List<string> Difficulties { get; set; } = new();
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
        }

        protected override void Initialize()
        {
            IsMouseVisible = true;
            Window.Title = "RhythmClicker";
            Window.AllowUserResizing = false;

            // Low-latency settings for better hit responsiveness
            IsFixedTimeStep = false;
            graphics!.SynchronizeWithVerticalRetrace = false;
            graphics.ApplyChanges();

            // File drop for editor audio import
            Window.FileDrop += OnFileDrop;

            // Register custom file type icons
            RegisterFileAssociations();

            base.Initialize();
        }

        void OnFileDrop(object? sender, FileDropEventArgs e)
        {
            if (e.Files == null || e.Files.Length == 0) return;
            string f = e.Files[0];
            string ext = Path.GetExtension(f).ToLowerInvariant();

            // osu! .osz package import (ZIP containing .osu + audio)
            if (ext == ".osz")
            {
                try
                {
                    // Pre-compute songId for unique audio naming
                    string tempDir = Path.Combine(Path.GetTempPath(), "rc_osz_peek_" + Path.GetFileNameWithoutExtension(f));
                    string previewId = Path.GetFileNameWithoutExtension(f).Trim().ToLowerInvariant()
                        .Replace(' ', '_').Replace("'", "").Replace("\"", "");
                    if (string.IsNullOrEmpty(previewId)) previewId = "osu_import_" + DateTime.Now.Ticks;

                    var imported = OsuImporter.ImportOsz(f, "Assets", previewId);
                    if (imported.Count == 0) return;

                    // Use first beatmap for metadata
                    var first = imported[0].beatmap;
                    string safeId = (first.Name ?? previewId).Trim().ToLowerInvariant()
                        .Replace(' ', '_').Replace("'", "").Replace("\"", "");
                    if (string.IsNullOrEmpty(safeId)) safeId = previewId;

                    // Sanitize difficulty labels and save each as .rcm
                    var difficulties = new List<string>();
                    foreach (var (bm, diffLabel) in imported)
                    {
                        string safeDiff = diffLabel.Trim().ToLowerInvariant()
                            .Replace(' ', '_').Replace("'", "").Replace("\"", "");
                        if (string.IsNullOrEmpty(safeDiff)) safeDiff = "easy";

                        // Avoid duplicate difficulty names
                        string finalDiff = safeDiff;
                        int dup = 1;
                        while (difficulties.Contains(finalDiff))
                            finalDiff = safeDiff + "_" + (++dup);

                        string rcmPath = Path.Combine("Assets", $"{safeId}_{finalDiff}.rcm");
                        RcFileManager.WriteBeatmap(rcmPath, bm);
                        difficulties.Add(finalDiff);
                    }

                    // Add to songs.json
                    string audioFile = first.AudioFile ?? "song1.wav";
                    if (!File.Exists(Path.Combine("Assets", audioFile))) audioFile = "song1.wav";

                    if (!songs.Any(s => s.Id == safeId))
                    {
                        songs.Add(new SongInfo { Id = safeId, Title = first.Name ?? safeId,
                            File = audioFile, Difficulties = difficulties });
                        var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                        File.WriteAllText("Assets/songs.json", System.Text.Json.JsonSerializer.Serialize(songs, opts));
                    }
                    // Always switch to the imported song and reload
                    currentSongIndex = songs.FindIndex(s => s.Id == safeId);
                    if (currentSongIndex < 0) currentSongIndex = songs.Count - 1;
                    currentDifficulty = difficulties[0];
                    LoadCurrentSong();
                }
                catch { }
                return;
            }

            // osu! single .osu file import
            if (ext == ".osu")
            {
                try
                {
                    var imported = OsuImporter.Import(f);
                    string safeId = (imported.Name ?? "osu_import").Trim().ToLowerInvariant().Replace(' ', '_');
                    if (string.IsNullOrEmpty(safeId)) safeId = "osu_import_" + DateTime.Now.Ticks;

                    // Copy audio file if exists alongside .osu (convert to WAV if needed)
                    string osuDir = Path.GetDirectoryName(f) ?? ".";
                    string audioSrc = Path.Combine(osuDir, imported.AudioFile ?? "");
                    string wavName = "";
                    if (!string.IsNullOrEmpty(imported.AudioFile) && File.Exists(audioSrc))
                    {
                        // Use safeId prefix for unique audio filename
                        wavName = safeId + "_" + Path.GetFileNameWithoutExtension(imported.AudioFile) + ".wav";
                        string audioDest = Path.Combine("Assets", wavName);
                        if (!File.Exists(audioDest))
                        {
                            string ext2 = Path.GetExtension(audioSrc).ToLowerInvariant();
                            if (ext2 == ".wav") File.Copy(audioSrc, audioDest, false);
                            else
                            {
                                try { OsuImporter.ConvertToWavPublic(audioSrc, audioDest); }
                                catch { wavName = ""; }
                            }
                        }
                    }

                    // Save as .rcm
                    string rcmPath = Path.Combine("Assets", safeId + "_easy.rcm");
                    RcFileManager.WriteBeatmap(rcmPath, imported);

                    // Add to songs.json
                    string audioFile = !string.IsNullOrEmpty(wavName) ? wavName : "song1.wav";
                    if (!songs.Any(s => s.Id == safeId))
                    {
                        songs.Add(new SongInfo { Id = safeId, Title = imported.Name ?? safeId,
                            File = audioFile, Difficulties = new List<string> { "easy" } });
                        var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                        File.WriteAllText("Assets/songs.json", System.Text.Json.JsonSerializer.Serialize(songs, opts));
                        currentSongIndex = songs.Count - 1;
                        currentDifficulty = "easy";
                        LoadCurrentSong();
                    }
                }
                catch { }
                return;
            }

            if (state != GameState.BeatmapEditor) return;
            if (ext == ".wav" || ext == ".ogg" || ext == ".mp3")
            {
                edAudioPath = f;
                // Try to determine duration
                try
                {
                    using var fs = File.OpenRead(f);
                    using var se = SoundEffect.FromStream(fs);
                    edTotalTime = (float)se.Duration.TotalSeconds + 1f;
                }
                catch { }
            }
            else if (ext == ".rcm")
            {
                // Import existing beatmap
                try
                {
                    var bm = RcFileManager.ReadBeatmap(f);
                    edNotes = bm.Notes ?? new List<Note>();
                    if (!string.IsNullOrEmpty(bm.Name)) edSongName = bm.Name;
                    if (!string.IsNullOrEmpty(bm.Author)) edAuthor = bm.Author;
                    if (bm.Bpm > 0) edBpm = bm.Bpm.ToString("F0");
                }
                catch { }
            }
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            textRenderer = new TextRenderer(GraphicsDevice);
            circleTexture = CreateCircleTexture(256, Color.White);
            renderCache = new RenderCache(GraphicsDevice);
            keyFlashPool = new ObjectPool<KeyFlash>(() => new KeyFlash(), 16);

            sfxHit = GenerateHitSfx();
            sfxMiss = GenerateMissSfx();

            // Initialize video background player
            videoPlayer = new VideoBackgroundPlayer(GraphicsDevice);

            textRenderer.Precache("CLICK", "Segoe UI", 56, Color.White);
            foreach (var lk in LaneKeys) textRenderer.Precache(lk, "Segoe UI", 18, new Color(180, 180, 200));

            Directory.CreateDirectory("Assets");
            EnsureExampleSongs();

            string songsMeta = "Assets/songs.json";
            if (!File.Exists(songsMeta) || !File.Exists("Assets/.audio_v3"))
                File.WriteAllText(songsMeta, DefaultSongsJson());
            var metaJson = File.ReadAllText(songsMeta);
            songs = System.Text.Json.JsonSerializer.Deserialize<List<SongInfo>>(metaJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            LoadCurrentSong();
            accountsManager = new AccountsManager(Core.AppPaths.AccountsFilePath);
            statsDb = new StatsDatabase(Core.AppPaths.StatsDbPath);
            settingsManager = new SettingsManager(Core.AppPaths.SettingsFilePath);
            achievementManager = new AchievementManager(Core.AppPaths.AchievementsFilePath);
            replayManager = new ReplayManager(Core.AppPaths.ReplaysPath);
            cloudSync = new CloudSyncManager();

            // Load local profile data
            LoadLocalProfile();

            // Discord RPC
            try { discordRpc = new DiscordRpcManager(); }
            catch { discordRpc = null; }

            // Menu music
            string menuBgmPath = "Assets/menu_bgm.wav";
            if (!File.Exists(menuBgmPath)) GenerateMenuMusicWav(menuBgmPath, 20.0f, 95f, 82.4);
            using (var fs = File.OpenRead(menuBgmPath))
            {
                menuMusicEffect = SoundEffect.FromStream(fs);
                menuMusicInstance = menuMusicEffect.CreateInstance();
                menuMusicInstance.IsLooped = true;
                menuMusicInstance.Volume = settingsManager?.Settings.MusicVolume ?? 0.35f;
            }
            menuMusicInstance.Play();

            // Load button icons
            _iconRegistry = new UI.IconRegistry(GraphicsDevice);
            _iconRegistry.Load();

            // Kick off background update check
            Systems.UpdateManager.UpdateAvailable += () => { /* update badge handled in Draw */ };
            Systems.UpdateManager.UpdateReady += OnUpdateReady;
            Systems.UpdateManager.CheckAsync();
        }

        protected override void UnloadContent()
        {
            textRenderer?.Dispose();
            statsDb?.Dispose();
            discordRpc?.Dispose();
            videoPlayer?.Dispose();
            bgImageTexture?.Dispose();
            _iconRegistry?.Dispose();
            base.UnloadContent();
        }

        /// <summary>Called by UpdateManager on the thread-pool when install finishes.</summary>
        void OnUpdateReady()
        {
            if (_updatePromptShown) return;
            _updatePromptShown = true;
            bool restart = Systems.UpdateManager.ShowRestartDialog(
                Localization.Get("update_restart_title"),
                Localization.Get("update_restart_title"),
                Localization.Get("update_restart_msg"),
                Localization.Get("update_restart_title"));
            if (restart)
                Systems.UpdateManager.RestartGame();
        }

        string DefaultSongsJson() => @"[
  { ""Id"": ""song1"", ""Title"": ""Example A"", ""File"": ""song1.wav"", ""Difficulties"": [""easy"", ""hard"", ""difficulty"", ""very_difficulty""] },
  { ""Id"": ""song2"", ""Title"": ""Example B"", ""File"": ""song2.wav"", ""Difficulties"": [""easy"", ""hard"", ""difficulty""] },
  { ""Id"": ""song3"", ""Title"": ""Example C"", ""File"": ""song3.wav"", ""Difficulties"": [""easy"", ""hard"", ""difficulty"", ""very_difficulty""] },
  { ""Id"": ""ba_unwelcome"", ""Title"": ""Unwelcome School"", ""File"": ""ba_unwelcome.wav"", ""Difficulties"": [""easy"", ""hard"", ""difficulty"", ""very_difficulty""] },
  { ""Id"": ""ba_constant"", ""Title"": ""Constant Moderato"", ""File"": ""ba_constant.wav"", ""Difficulties"": [""easy"", ""hard"", ""difficulty"", ""very_difficulty""] },
  { ""Id"": ""ba_midsummer"", ""Title"": ""Midsummer Daydream"", ""File"": ""ba_midsummer.wav"", ""Difficulties"": [""easy"", ""hard"", ""difficulty"", ""very_difficulty""] }
]";

        void EnsureExampleSongs()
        {
            string marker = "Assets/.audio_v4";
            if (File.Exists(marker)) return;

            GenerateMusicalWav("Assets/song1.wav", 8.0f, 120f, 110.0);
            GenerateMusicalWav("Assets/song2.wav", 6.0f, 130f, 130.8);
            GenerateMusicalWav("Assets/song3.wav", 10.0f, 100f, 82.4);
            GenerateMenuMusicWav("Assets/menu_bgm.wav", 20.0f, 95f, 82.4);

            // Blue Archive style songs
            GenerateBaStyleWav("Assets/ba_unwelcome.wav", 12.0f, 170f, 164.8, 0);  // Bright uptempo pop-rock
            GenerateBaStyleWav("Assets/ba_constant.wav", 14.0f, 132f, 146.8, 1);   // Smooth piano pop
            GenerateBaStyleWav("Assets/ba_midsummer.wav", 10.0f, 155f, 196.0, 2);  // Energetic electronic

            string[][] allSongDiffs = {
                new[] { "easy", "hard", "difficulty", "very_difficulty" },
                new[] { "easy", "hard", "difficulty" },
                new[] { "easy", "hard", "difficulty", "very_difficulty" },
                new[] { "easy", "hard", "difficulty", "very_difficulty" },
                new[] { "easy", "hard", "difficulty", "very_difficulty" },
                new[] { "easy", "hard", "difficulty", "very_difficulty" },
            };
            float[][] songParams = {
                new[] { 8.0f, 120f }, new[] { 6.0f, 130f }, new[] { 10.0f, 100f },
                new[] { 12.0f, 170f }, new[] { 14.0f, 132f }, new[] { 10.0f, 155f },
            };
            string[] songIds = { "song1", "song2", "song3", "ba_unwelcome", "ba_constant", "ba_midsummer" };

            for (int si = 0; si < songIds.Length; si++)
            {
                foreach (var d in allSongDiffs[si])
                    WriteBeatmapRcm($"Assets/{songIds[si]}_{d}.rcm", songParams[si][0], songParams[si][1], d);
            }

            File.WriteAllText(marker, "v4");
        }

        void WriteBeatmapRcm(string path, float dur, float bpm, string diff)
        {
            var bm = GenerateBeatmapObject(dur, bpm, diff);
            RcFileManager.WriteBeatmap(path, bm);
        }

        void LoadCurrentSong()
        {
            if (songs.Count == 0)
            {
                if (!File.Exists("Assets/song.wav")) GenerateMusicalWav("Assets/song.wav", 3.0f);
                LoadSong("Assets/song.wav", "Assets/beatmap.rcm");
                return;
            }
            var s = songs[Math.Clamp(currentSongIndex, 0, songs.Count - 1)];
            string songPath = Path.Combine("Assets", s.File);
            if (!File.Exists(songPath)) GenerateMusicalWav(songPath, 6.0f);

            string rcmPath = Path.Combine("Assets", s.Id + "_" + currentDifficulty + ".rcm");
            string jsonPath = Path.Combine("Assets", s.Id + "_" + currentDifficulty + ".json");

            if (File.Exists(rcmPath)) { LoadSongRcm(songPath, rcmPath); return; }
            if (File.Exists(jsonPath))
            {
                RcFileManager.MigrateJsonToRcm(jsonPath, rcmPath);
                if (File.Exists(rcmPath)) { LoadSongRcm(songPath, rcmPath); return; }
                LoadSong(songPath, jsonPath); return;
            }
            foreach (var d in s.Difficulties)
            {
                var rp = Path.Combine("Assets", s.Id + "_" + d + ".rcm");
                if (File.Exists(rp)) { currentDifficulty = d; LoadSongRcm(songPath, rp); return; }
            }
            var defBm = GenerateBeatmapObject(6.0f, 120f, currentDifficulty);
            RcFileManager.WriteBeatmap(rcmPath, defBm);
            LoadSongRcm(songPath, rcmPath);
        }

        void LoadSongRcm(string songFilePath, string rcmPath)
        {
            beatmap = RcFileManager.ReadBeatmap(rcmPath);
            notes = new LinkedList<Note>(beatmap.Notes ?? new List<Note>());
            maxScore = (beatmap?.Notes?.Count ?? 0) * 100;
            songInstance?.Stop(); songInstance?.Dispose(); songEffect = null;
            try
            {
                using (var fs = File.OpenRead(songFilePath))
                { songEffect = SoundEffect.FromStream(fs); songInstance = songEffect.CreateInstance(); }
            }
            catch
            {
                // Audio file is not WAV or corrupted — generate fallback
                float dur = (beatmap?.Notes?.Count > 0) ? beatmap.Notes.Max(n => n.Time) + 2f : 6f;
                GenerateMusicalWav(songFilePath, dur, beatmap?.Bpm ?? 120f);
                using (var fs = File.OpenRead(songFilePath))
                { songEffect = SoundEffect.FromStream(fs); songInstance = songEffect.CreateInstance(); }
            }
            songDurationSeconds = songEffect?.Duration.TotalSeconds ?? 0.0;
        }

        void LoadSong(string songFilePath, string beatmapPath)
        {
            var json = File.ReadAllText(beatmapPath);
            beatmap = Beatmap.LoadFromString(json);
            notes = new LinkedList<Note>(beatmap.Notes ?? new List<Note>());
            maxScore = (beatmap?.Notes?.Count ?? 0) * 100;
            songInstance?.Stop(); songInstance?.Dispose(); songEffect = null;
            try
            {
                using (var fs = File.OpenRead(songFilePath))
                { songEffect = SoundEffect.FromStream(fs); songInstance = songEffect.CreateInstance(); }
            }
            catch
            {
                float dur = (beatmap?.Notes?.Count > 0) ? beatmap.Notes.Max(n => n.Time) + 2f : 6f;
                GenerateMusicalWav(songFilePath, dur, beatmap?.Bpm ?? 120f);
                using (var fs = File.OpenRead(songFilePath))
                { songEffect = SoundEffect.FromStream(fs); songInstance = songEffect.CreateInstance(); }
            }
            songDurationSeconds = songEffect?.Duration.TotalSeconds ?? 0.0;
        }

        // CreateCircleTexture → Helpers/Game1.Helpers.cs

        // ═══════════════════════════════════════════════════════════════
        // UPDATE
        // ═══════════════════════════════════════════════════════════════

        protected override void Update(GameTime gameTime)
        {
            kb = Keyboard.GetState();
            mouseState = Mouse.GetState();

            // F11 fullscreen toggle
            if (kb.IsKeyDown(Keys.F11) && !prevKb.IsKeyDown(Keys.F11))
            {
                if (isFullscreen) { ExitBorderlessFullscreen(); isFullscreen = false; }
                else { EnterBorderlessFullscreen(); isFullscreen = true; }
            }

            // Escape handling
            if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
            {
                if (state == GameState.Playing)
                {
                    songInstance?.Stop(); stopwatch.Stop(); videoPlayer?.Stop();
                    state = GameState.Menu; ExitBorderlessFullscreen();
                    menuMusicInstance?.Play(); discordRpc?.SetMenu();
                }
                else if (state == GameState.Result)
                {
                    state = GameState.Menu; ExitBorderlessFullscreen();
                    menuMusicInstance?.Play(); discordRpc?.SetMenu();
                }
                else if (state == GameState.Account || state == GameState.Language
                      || state == GameState.Stats || state == GameState.BeatmapEditor
                      || state == GameState.Settings || state == GameState.Achievements
                      || state == GameState.ReplayView || state == GameState.Profile
                      || state == GameState.SearchPlayer)
                {
                    if (state == GameState.BeatmapEditor)
                    { edPreviewInstance?.Stop(); edPreviewing = false; }
                    state = GameState.Menu; discordRpc?.SetMenu();
                }
                else { Exit(); }

                prevKb = kb; prevMouseState = mouseState; prevScrollValue = mouseState.ScrollWheelValue;
                base.Update(gameTime); return;
            }

            switch (state)
            {
                case GameState.Menu: UpdateMenu(gameTime); break;
                case GameState.Language: UpdateLanguage(); break;
                case GameState.Result: UpdateResult(); break;
                case GameState.Account: HandleAccountInput(kb, prevKb); break;
                case GameState.Stats: break; // stats is just display
                case GameState.BeatmapEditor: UpdateEditor(gameTime); break;
                case GameState.Playing: UpdatePlaying(gameTime); break;
                case GameState.Settings: UpdateSettings(); break;
                case GameState.Achievements: UpdateAchievements(); break;
                case GameState.ReplayView: UpdateReplayView(gameTime); break;
                case GameState.Profile: UpdateProfile(); break;
                case GameState.SearchPlayer: UpdateSearchPlayer(); break;
                case GameState.EditProfile: UpdateEditProfile(gameTime); break;
            }

            // Achievement popup timer
            if (achievementPopupTimer > 0)
                achievementPopupTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Sync status timer
            if (syncStatusTimer > 0)
                syncStatusTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            prevKb = kb;
            prevMouseState = mouseState;
            prevScrollValue = mouseState.ScrollWheelValue;
            base.Update(gameTime);
        }

        // UpdateMenu, ExecuteMenuAction → Screens/Game1.Menu.cs

        // UpdateLanguage → Screens/Game1.Language.cs

        // UpdateResult → Screens/Game1.Result.cs
        // UpdatePlaying → Screens/Game1.Play.cs
        // InitEditor, UpdateEditor, SaveEditorBeatmap → Screens/Game1.Editor.cs
        // StartPlaying, LoadBeatmapMedia, EnterBorderlessFullscreen, ExitBorderlessFullscreen → Screens/Game1.Play.cs
        // ═══════════════════════════════════════════════════════════════
        // DRAW
        // ═══════════════════════════════════════════════════════════════

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(10, 10, 25));

            spriteBatch!.Begin();
            DrawBackground();
            if (state == GameState.Playing)
            {
                if (ComboTier > 0)
                    spriteBatch.Draw(pixel!, new Rectangle(0, 0, width, height), TierGlowColor[ComboTier] * (0.03f + ComboTier * 0.018f));
                if (beatPulseAlpha > 0.01f)
                    spriteBatch.Draw(pixel!, new Rectangle(0, 0, width, height), TierGlowColor[ComboTier] * (beatPulseAlpha * 0.08f));
            }
            spriteBatch.End();

            Matrix xform = Matrix.Identity;
            if (state == GameState.Playing && shakeTimer > 0)
                xform = Matrix.CreateTranslation((float)(rng.NextDouble() - 0.5) * shakeIntensity * 4f,
                    (float)(rng.NextDouble() - 0.5) * shakeIntensity * 4f, 0f);

            spriteBatch.Begin(transformMatrix: state == GameState.Playing ? xform : Matrix.Identity);
            switch (state)
            {
                case GameState.Playing: DrawGameplay(gameTime); break;
                case GameState.Menu: DrawMenu(); break;
                case GameState.Result: DrawResult(); break;
                case GameState.Account: DrawAccount(); break;
                case GameState.Language: DrawLanguage(); break;
                case GameState.Stats: DrawStats(); break;
                case GameState.BeatmapEditor: DrawEditor(); break;
                case GameState.Settings: DrawSettings(); break;
                case GameState.Achievements: DrawAchievements(); break;
                case GameState.ReplayView: DrawReplayView(); break;
                case GameState.Profile: DrawProfile(); break;
                case GameState.SearchPlayer: DrawSearchPlayer(); break;
                case GameState.EditProfile: DrawEditProfile(); break;
            }

            // Achievement popup overlay
            if (achievementPopupTimer > 0 && !string.IsNullOrEmpty(achievementPopupText))
            {
                float alpha = Math.Min(achievementPopupTimer, 1f);
                int popW = 320, popH = 50;
                int popX = (width - popW) / 2, popY = 20;
                spriteBatch.Draw(pixel!, new Rectangle(popX, popY, popW, popH), new Color(255, 220, 50) * (0.15f * alpha));
                DrawRectBorder(new Rectangle(popX, popY, popW, popH), new Color(255, 220, 50) * (0.5f * alpha));
                var achLabel = textRenderer!.GetTexture("🏆 " + Localization.Get("achievement_unlocked"), "Segoe UI", 11, new Color(255, 220, 50));
                spriteBatch.Draw(achLabel, new Vector2(popX + (popW - achLabel.Width) / 2, popY + 6), Color.White * alpha);
                var achName = textRenderer!.GetTexture(achievementPopupText, "Segoe UI", 15, Color.White);
                spriteBatch.Draw(achName, new Vector2(popX + (popW - achName.Width) / 2, popY + 24), Color.White * alpha);
            }

            // Cloud sync status overlay
            if (syncStatusTimer > 0 && !string.IsNullOrEmpty(syncStatusText))
            {
                float alpha = Math.Min(syncStatusTimer, 1f);
                var syncTex = textRenderer!.GetTexture("☁ " + syncStatusText, "Segoe UI", 11, new Color(120, 200, 255));
                spriteBatch.Draw(syncTex, new Vector2(width - syncTex.Width - 12, height - 30), Color.White * alpha);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        // DrawBackground → Helpers/Game1.Helpers.cs

        // DrawGameplay, DrawBreakOverlay, DrawHPBar, DrawJudgmentCounter → Screens/Game1.Play.cs
        // DrawMenu → Screens/Game1.Menu.cs
        // DrawResult → Screens/Game1.Result.cs
        // DrawAccount, DrawTextField → Screens/Game1.Account.cs
        // ═══════════ Language ═══════════

        // DrawLanguage → Screens/Game1.Language.cs
        // DrawStats → Screens/Game1.Stats.cs

        // DrawEditor → Screens/Game1.Editor.cs (see also InitEditor/UpdateEditor/SaveEditorBeatmap)
        // GetLaneKeys, GetLaneKeyLabels, UpdateSettings, ApplyVolume, DrawSettings, DrawSettingsSlider → Screens/Game1.Settings.cs
        // UpdateAchievements, DrawAchievements → Screens/Game1.Achievements.cs
        // StartReplayView, UpdateReplayView, DrawReplayView → Screens/Game1.Replay.cs
        // UpdateProfile, DrawProfile → Screens/Game1.UpdateProfile.cs
        // UpdateEditProfile, DrawEditProfile → Screens/Game1.EditProfile.cs
        // UpdateSearchPlayer, DrawSearchPlayer → Screens/Game1.Search.cs
    }
}
