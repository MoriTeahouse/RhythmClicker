using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace ClickerGame
{
    public class Game1 : Game
    {
        GraphicsDeviceManager? graphics;
        SpriteBatch? spriteBatch;
        Texture2D? pixel;
        int width = 800, height = 600;
        Beatmap? beatmap;
        LinkedList<Note> notes = new();
        SoundEffect? songEffect;
        SoundEffectInstance? songInstance;
        Stopwatch stopwatch = new Stopwatch();
        int score = 0;
        KeyboardState kb, prevKb;

        // Song selection and editor
        List<SongInfo> songs = new();
        int currentSongIndex = 0;
        string currentDifficulty = "easy";
        bool editorMode = false;

        // Menu / scene
        enum GameState { Menu, Playing, Result, Account, Language }
        GameState state = GameState.Menu;
        // Menu keys used for logic branching; display text comes from Localization
        string[] menuKeys = new[] { "menu_start", "menu_editor", "menu_account", "menu_language", "menu_exit" };
        int currentMenuIndex = 0;

        // Language selection
        int languageMenuIndex = 0;

        // Saved windowed dimensions for restoring from borderless fullscreen
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
            {
                Rect = rect;
                Color = color;
                TimeToLive = ttl;
            }
        }
        List<KeyFlash> keyFlashes = new();
        ObjectPool<KeyFlash>? keyFlashPool;

        // result / scoring
        int maxScore = 0;
        bool summaryShown = false;
        int resultMenuIndex = 0;

        double songDurationSeconds = 0.0;
        RenderCache? renderCache;

        // account manager
        AccountsManager? accountsManager;
        string accountUsername = string.Empty;
        string accountPassword = string.Empty;
        bool accountShowMessage = false;
        string accountMessage = string.Empty;
        int accountFieldIndex = 0;

        // result grade
        string resultGrade = "";

        // Combo and match stats
        int combo = 0;
        int maxCombo = 0;
        int hitCount = 0;
        int missCount = 0;

        // Note column colors (neon palette)
        static readonly Color[] NoteColors = new[]
        {
            new Color(0, 200, 255),
            new Color(255, 60, 140),
            new Color(255, 220, 50),
            new Color(80, 255, 120),
        };
        static readonly string[] LaneKeys = { "D", "F", "J", "K" };

        // Lane layout constants
        const int LaneCount = 4;
        const int LaneWidth = 90;
        const int TotalLaneWidth = LaneCount * LaneWidth;
        const int NoteHeight = 22;
        const int HitZoneHeight = 70;
        int LaneLeft => (width - TotalLaneWidth) / 2;
        int HitZoneY => height - HitZoneHeight - 40;

        // Menu animation
        float menuTimer = 0f;

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
            Window.Title = "CLICK - Rhythm Game";
            base.Initialize();
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

            // Precache common UI text (English defaults; others cached on demand)
            textRenderer.Precache("CLICK", "Segoe UI", 56, Color.White);
            foreach (var lk in LaneKeys) textRenderer.Precache(lk, "Segoe UI", 18, new Color(180, 180, 200));

            Directory.CreateDirectory("Assets");

            EnsureExampleSongs();

            string songsMeta = "Assets/songs.json";
            if (!File.Exists(songsMeta))
            {
                File.WriteAllText(songsMeta, DefaultSongsJson());
            }
            var metaJson = File.ReadAllText(songsMeta);
            songs = System.Text.Json.JsonSerializer.Deserialize<List<SongInfo>>(metaJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<SongInfo>();

            LoadCurrentSong();
            accountsManager = new AccountsManager();
        }

        protected override void UnloadContent()
        {
            textRenderer?.Dispose();
            base.UnloadContent();
        }

        string DefaultBeatmapJson()
        {
            return @"{
    ""notes"": [
        { ""time"": 0.5, ""column"": 0 },
        { ""time"": 0.9, ""column"": 1 },
        { ""time"": 1.3, ""column"": 2 },
        { ""time"": 1.7, ""column"": 3 }
    ]
}";
        }

        string DefaultSongsJson()
        {
            return @"[
      { ""Id"": ""song1"", ""Title"": ""Example A"", ""File"": ""song1.wav"", ""Difficulties"": [""easy"", ""hard""] },
      { ""Id"": ""song2"", ""Title"": ""Example B"", ""File"": ""song2.wav"", ""Difficulties"": [""easy""] },
      { ""Id"": ""song3"", ""Title"": ""Example C"", ""File"": ""song3.wav"", ""Difficulties"": [""easy""] }
    ]";
        }

        void EnsureExampleSongs()
        {
            if (!File.Exists("Assets/song1.wav")) GenerateExampleWav("Assets/song1.wav", 8.0f, 440.0);
            if (!File.Exists("Assets/song2.wav")) GenerateExampleWav("Assets/song2.wav", 6.0f, 523.25);
            if (!File.Exists("Assets/song3.wav")) GenerateExampleWav("Assets/song3.wav", 10.0f, 349.23);

            if (!File.Exists("Assets/song1_easy.json")) File.WriteAllText("Assets/song1_easy.json", DefaultBeatmapJson());
            if (!File.Exists("Assets/song1_hard.json")) File.WriteAllText("Assets/song1_hard.json", @"{
    ""notes"": [
        { ""time"": 0.4, ""column"": 0 },{ ""time"": 0.6, ""column"": 1 },{ ""time"": 0.8, ""column"": 2 },{ ""time"": 1.0, ""column"": 3 },{ ""time"": 1.2, ""column"": 0 },{ ""time"": 1.4, ""column"": 1 }
    ]
}");
            if (!File.Exists("Assets/song2_easy.json")) File.WriteAllText("Assets/song2_easy.json", DefaultBeatmapJson());
            if (!File.Exists("Assets/song3_easy.json")) File.WriteAllText("Assets/song3_easy.json", DefaultBeatmapJson());
        }

        void LoadCurrentSong()
        {
            if (songs.Count == 0)
            {
                if (!File.Exists("Assets/song.wav")) GenerateExampleWav("Assets/song.wav", 3.0f);
                LoadSong("Assets/song.wav", "Assets/beatmap.json");
                return;
            }
            var s = songs[Math.Clamp(currentSongIndex, 0, songs.Count - 1)];
            string songPath = Path.Combine("Assets", s.File);
            if (!File.Exists(songPath)) GenerateExampleWav(songPath, 6.0f);

            string beatmapPath = Path.Combine("Assets", s.Id + "_" + currentDifficulty + ".json");
            if (!File.Exists(beatmapPath))
            {
                foreach (var d in s.Difficulties)
                {
                    var p = Path.Combine("Assets", s.Id + "_" + d + ".json");
                    if (File.Exists(p)) { beatmapPath = p; currentDifficulty = d; break; }
                }
                if (!File.Exists(beatmapPath)) File.WriteAllText(beatmapPath, DefaultBeatmapJson());
            }

            LoadSong(songPath, beatmapPath);
        }

        Texture2D CreateCircleTexture(int size, Color fill)
        {
            var tex = new Texture2D(GraphicsDevice, size, size);
            var data = new Color[size * size];
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    float alpha = d < r - 1 ? 1f : d < r ? r - d : 0f;
                    data[y * size + x] = new Color(fill.R, fill.G, fill.B, (byte)(fill.A * alpha));
                }
            }
            tex.SetData(data);
            return tex;
        }

        void LoadSong(string songFilePath, string beatmapPath)
        {
            var beatmapJson = File.ReadAllText(beatmapPath);
            beatmap = Beatmap.LoadFromString(beatmapJson);
            notes = new LinkedList<Note>(beatmap.Notes ?? new List<Note>());
            maxScore = (beatmap?.Notes?.Count ?? 0) * 100;

            songInstance?.Stop();
            songInstance?.Dispose();
            songEffect = null;
            using (var fs = File.OpenRead(songFilePath))
            {
                songEffect = SoundEffect.FromStream(fs);
                songInstance = songEffect.CreateInstance();
            }
            songDurationSeconds = songEffect?.Duration.TotalSeconds ?? 0.0;
        }

        void GenerateExampleWav(string path, float durationSeconds, double freq = 440.0)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * durationSeconds);
            using (var fs = new FileStream(path, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                int byteRate = sampleRate * 2;
                int subchunk2 = samples * 2;
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(36 + subchunk2);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)1);
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)2);
                bw.Write((short)16);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(subchunk2);
                for (int i = 0; i < samples; i++)
                {
                    short sample = (short)(Math.Sin(2 * Math.PI * freq * i / sampleRate) * short.MaxValue * 0.2);
                    bw.Write(sample);
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            kb = Keyboard.GetState();

            // Global escape handling for all states
            if (kb.IsKeyDown(Keys.Escape) && !prevKb.IsKeyDown(Keys.Escape))
            {
                if (state == GameState.Playing)
                {
                    songInstance?.Stop();
                    stopwatch.Stop();
                    state = GameState.Menu;
                    ExitBorderlessFullscreen();
                    prevKb = Keyboard.GetState();
                    base.Update(gameTime);
                    return;
                }
                else if (state == GameState.Result)
                {
                    state = GameState.Menu;
                    ExitBorderlessFullscreen();
                    prevKb = Keyboard.GetState();
                    base.Update(gameTime);
                    return;
                }
                else if (state == GameState.Account || state == GameState.Language)
                {
                    state = GameState.Menu;
                    prevKb = Keyboard.GetState();
                    base.Update(gameTime);
                    return;
                }
                else
                {
                    Exit();
                }
            }

            if (state == GameState.Menu)
            {
                menuTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up))
                    currentMenuIndex = (currentMenuIndex - 1 + menuKeys.Length) % menuKeys.Length;
                if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down))
                    currentMenuIndex = (currentMenuIndex + 1) % menuKeys.Length;

                // Song selection with left/right
                if (kb.IsKeyDown(Keys.Left) && !prevKb.IsKeyDown(Keys.Left) && songs.Count > 0)
                {
                    currentSongIndex = Math.Max(0, currentSongIndex - 1);
                    LoadCurrentSong();
                }
                if (kb.IsKeyDown(Keys.Right) && !prevKb.IsKeyDown(Keys.Right) && songs.Count > 0)
                {
                    currentSongIndex = Math.Min(songs.Count - 1, currentSongIndex + 1);
                    LoadCurrentSong();
                }

                // Difficulty selection with Tab
                if (kb.IsKeyDown(Keys.Tab) && !prevKb.IsKeyDown(Keys.Tab) && songs.Count > 0)
                {
                    var s = songs[currentSongIndex];
                    if (s.Difficulties.Count > 1)
                    {
                        int idx = s.Difficulties.IndexOf(currentDifficulty);
                        idx = (idx + 1) % s.Difficulties.Count;
                        currentDifficulty = s.Difficulties[idx];
                        LoadCurrentSong();
                    }
                }

                if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
                {
                    var choiceKey = menuKeys[currentMenuIndex];
                    if (choiceKey == "menu_start")
                    {
                        StartPlaying(false);
                    }
                    else if (choiceKey == "menu_editor")
                    {
                        StartPlaying(true);
                    }
                    else if (choiceKey == "menu_account")
                    {
                        state = GameState.Account;
                        accountUsername = string.Empty;
                        accountPassword = string.Empty;
                        accountShowMessage = false;
                        accountFieldIndex = 0;
                    }
                    else if (choiceKey == "menu_language")
                    {
                        state = GameState.Language;
                        languageMenuIndex = System.Array.IndexOf(Localization.All, Localization.Current);
                        if (languageMenuIndex < 0) languageMenuIndex = 0;
                    }
                    else if (choiceKey == "menu_exit")
                    {
                        Exit();
                    }
                }

                prevKb = kb;
                base.Update(gameTime);
                return;
            }

            // Language selection screen
            if (state == GameState.Language)
            {
                int langCount = Localization.All.Length;
                if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up))
                    languageMenuIndex = (languageMenuIndex - 1 + langCount) % langCount;
                if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down))
                    languageMenuIndex = (languageMenuIndex + 1) % langCount;
                if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
                {
                    Localization.Current = Localization.All[languageMenuIndex];
                    state = GameState.Menu;
                }
                prevKb = kb;
                base.Update(gameTime);
                return;
            }

            float time = (float)stopwatch.Elapsed.TotalSeconds;

            // Result screen
            if (state == GameState.Result)
            {
                if (kb.IsKeyDown(Keys.Up) && !prevKb.IsKeyDown(Keys.Up)) resultMenuIndex = (resultMenuIndex - 1 + 2) % 2;
                if (kb.IsKeyDown(Keys.Down) && !prevKb.IsKeyDown(Keys.Down)) resultMenuIndex = (resultMenuIndex + 1) % 2;
                if (kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter))
                {
                    if (resultMenuIndex == 0)
                    {
                        StartPlaying(editorMode);
                    }
                    else if (resultMenuIndex == 1)
                    {
                        state = GameState.Menu;
                        ExitBorderlessFullscreen();
                    }
                }
                prevKb = kb;
                base.Update(gameTime);
                return;
            }

            // Account screen
            if (state == GameState.Account)
            {
                HandleAccountInput(kb, prevKb);
                prevKb = kb;
                base.Update(gameTime);
                return;
            }

            // === Playing state ===
            Keys[] keys = new[] { Keys.D, Keys.F, Keys.J, Keys.K };
            for (int c = 0; c < 4; c++)
            {
                if (kb.IsKeyDown(keys[c]) && !prevKb.IsKeyDown(keys[c]))
                {
                    if (editorMode)
                    {
                        var n = new Note { Time = time, Column = c };
                        notes.AddLast(n);
                    }
                    else
                    {
                        Note? nearest = null;
                        LinkedListNode<Note>? nearestNode = null;
                        float best = float.MaxValue;
                        const float hitWindow = 0.30f;
                        for (var node = notes.First; node != null; node = node.Next)
                        {
                            var n = node.Value;
                            if (n.Column != c) continue;
                            float dt = Math.Abs(n.Time - time);
                            if (dt <= hitWindow && dt < best)
                            {
                                best = dt;
                                nearest = n;
                                nearestNode = node;
                            }
                        }
                        if (nearestNode != null)
                        {
                            notes.Remove(nearestNode);
                            score += 100;
                            combo++;
                            hitCount++;
                            if (combo > maxCombo) maxCombo = combo;
                        }
                    }

                    // Flash feedback using lane layout
                    int lx = LaneLeft + c * LaneWidth + 4;
                    int ly = HitZoneY;
                    var rect = new Rectangle(lx, ly, LaneWidth - 8, HitZoneHeight);
                    if (keyFlashPool != null)
                    {
                        var k = keyFlashPool.Rent();
                        k.Reset(rect, NoteColors[c], GameConfig.KeyFlashDuration);
                        keyFlashes.Add(k);
                    }
                }
            }

            // Save beatmap in editor: S
            if (editorMode && kb.IsKeyDown(Keys.S) && !prevKb.IsKeyDown(Keys.S))
            {
                var bm = new Beatmap { Notes = new List<Note>(notes) };
                string songId = songs.Count > 0 ? songs[currentSongIndex].Id : "song";
                string outPath = Path.Combine("Assets", songId + "_" + currentDifficulty + ".json");
                var opts = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(outPath, System.Text.Json.JsonSerializer.Serialize(bm, opts));
            }

            // End-of-song detection
            if (state == GameState.Playing && !summaryShown)
            {
                if (notes.Count == 0 || stopwatch.Elapsed.TotalSeconds >= songDurationSeconds + 0.1)
                {
                    summaryShown = true;
                    state = GameState.Result;
                    songInstance?.Stop();
                    var pct = maxScore > 0 ? (double)score / maxScore : 0.0;
                    if (pct >= 0.95) resultGrade = "SS";
                    else if (pct >= 0.85) resultGrade = "S";
                    else if (pct >= 0.75) resultGrade = "A";
                    else if (pct >= 0.60) resultGrade = "B";
                    else if (pct >= 0.40) resultGrade = "C";
                    else resultGrade = "D";
                    resultMenuIndex = 0;
                }
            }

            prevKb = kb;
            base.Update(gameTime);
        }

        void StartPlaying(bool editor)
        {
            editorMode = editor;
            state = GameState.Playing;
            score = 0;
            combo = 0;
            maxCombo = 0;
            hitCount = 0;
            missCount = 0;
            summaryShown = false;
            keyFlashes.Clear();
            LoadCurrentSong();
            EnterBorderlessFullscreen();
            stopwatch.Restart();
            songInstance?.Stop();
            songInstance?.Play();
        }

        void EnterBorderlessFullscreen()
        {
            windowedWidth = graphics!.PreferredBackBufferWidth;
            windowedHeight = graphics.PreferredBackBufferHeight;
            var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            graphics.PreferredBackBufferWidth = display.Width;
            graphics.PreferredBackBufferHeight = display.Height;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            Window.IsBorderless = true;
            Window.Position = Point.Zero;
            width = display.Width;
            height = display.Height;
            renderCache?.Dispose();
            renderCache = new RenderCache(GraphicsDevice);
        }

        void ExitBorderlessFullscreen()
        {
            Window.IsBorderless = false;
            graphics!.PreferredBackBufferWidth = windowedWidth;
            graphics.PreferredBackBufferHeight = windowedHeight;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            width = windowedWidth;
            height = windowedHeight;
            renderCache?.Dispose();
            renderCache = new RenderCache(GraphicsDevice);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(10, 10, 25));

            spriteBatch!.Begin();

            DrawBackground();

            if (state == GameState.Playing)
            {
                DrawGameplay(gameTime);
            }

            if (state == GameState.Menu)
            {
                DrawMenu();
            }

            if (state == GameState.Result)
            {
                DrawResult();
            }

            if (state == GameState.Account)
            {
                DrawAccount();
            }

            if (state == GameState.Language)
            {
                DrawLanguage();
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        void DrawBackground()
        {
            if (renderCache != null)
            {
                var bg = renderCache.GetBackground(width, height);
                spriteBatch!.Draw(bg, new Rectangle(0, 0, width, height), Color.White);
            }
        }

        void DrawGameplay(GameTime gameTime)
        {
            float time = (float)stopwatch.Elapsed.TotalSeconds;
            int laneLeft = LaneLeft;
            int hitZoneY = HitZoneY;

            // Lane backgrounds (alternating subtle shading)
            for (int i = 0; i < LaneCount; i++)
            {
                int lx = laneLeft + i * LaneWidth;
                spriteBatch!.Draw(pixel!, new Rectangle(lx, 0, LaneWidth, height),
                    Color.White * (i % 2 == 0 ? 0.03f : 0.05f));
            }

            // Lane dividers
            for (int i = 0; i <= LaneCount; i++)
            {
                int lx = laneLeft + i * LaneWidth;
                spriteBatch!.Draw(pixel!, new Rectangle(lx, 0, 1, height), Color.White * 0.1f);
            }

            // Hit zone glow
            for (int g = 1; g <= 6; g++)
            {
                float a = 0.05f * (7 - g);
                spriteBatch!.Draw(pixel!,
                    new Rectangle(laneLeft, hitZoneY - g * 2, TotalLaneWidth, 2 + g * 4),
                    new Color(0, 200, 255) * a);
            }

            // Hit zone line
            spriteBatch!.Draw(pixel!,
                new Rectangle(laneLeft, hitZoneY, TotalLaneWidth, 2), Color.White * 0.7f);

            // Hit zone area
            spriteBatch.Draw(pixel!,
                new Rectangle(laneLeft, hitZoneY, TotalLaneWidth, HitZoneHeight),
                Color.White * 0.03f);

            // Key labels below hit zone
            for (int i = 0; i < LaneCount; i++)
            {
                int lx = laneLeft + i * LaneWidth;
                var label = textRenderer!.GetTexture(LaneKeys[i], "Segoe UI", 18, new Color(180, 180, 200));
                spriteBatch.Draw(label,
                    new Vector2(lx + (LaneWidth - label.Width) / 2, hitZoneY + HitZoneHeight + 6),
                    Color.White);
            }

            // Falling notes
            for (var node = notes.Last; node != null;)
            {
                var prev = node.Previous;
                var n = node.Value;
                float dt = n.Time - time;

                // Miss: note passed the hit zone
                if (time - n.Time > GameConfig.ApproachTime + 0.75f)
                {
                    notes.Remove(node);
                    combo = 0;
                    missCount++;
                    int mx = laneLeft + n.Column * LaneWidth + 4;
                    if (keyFlashPool != null)
                    {
                        var k = keyFlashPool.Rent();
                        k.Reset(new Rectangle(mx, hitZoneY, LaneWidth - 8, HitZoneHeight),
                            Color.Red, GameConfig.MissFlashDuration);
                        keyFlashes.Add(k);
                    }
                    node = prev;
                    continue;
                }

                float progress = (GameConfig.ApproachTime - dt) / (GameConfig.ApproachTime + 0.01f);
                int nx = laneLeft + n.Column * LaneWidth + 6;
                int ny = (int)(MathHelper.Clamp(progress, 0f, 1f) * (hitZoneY - NoteHeight));
                var noteRect = new Rectangle(nx, ny, LaneWidth - 12, NoteHeight);
                Color noteColor = editorMode ? Color.Yellow : NoteColors[n.Column % NoteColors.Length];

                // Note glow
                spriteBatch.Draw(pixel!,
                    new Rectangle(noteRect.X - 2, noteRect.Y - 2, noteRect.Width + 4, noteRect.Height + 4),
                    noteColor * 0.25f);

                // Note body
                spriteBatch.Draw(pixel!, noteRect, noteColor * 0.9f);

                // Note highlight strip
                spriteBatch.Draw(pixel!,
                    new Rectangle(noteRect.X, noteRect.Y, noteRect.Width, 3),
                    Color.White * 0.4f);

                node = prev;
            }

            // Key flashes
            for (int i = keyFlashes.Count - 1; i >= 0; i--)
            {
                var k = keyFlashes[i];
                float alpha = Math.Clamp(k.TimeToLive / GameConfig.KeyFlashDuration, 0f, 1f);
                spriteBatch!.Draw(pixel!, k.Rect, k.Color * (alpha * 0.35f));
                k.TimeToLive -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (k.TimeToLive <= 0f)
                {
                    keyFlashes.RemoveAt(i);
                    keyFlashPool?.Return(k);
                }
            }

            // HUD: Song info (top-left)
            string songTitle = songs.Count > 0 ? songs[currentSongIndex].Title : Localization.Get("unknown");
            var titleTex = textRenderer!.GetTexture(songTitle, "Segoe UI", 18, Color.White);
            spriteBatch.Draw(titleTex, new Vector2(16, 14), Color.White);
            var diffTex = textRenderer!.GetTexture(currentDifficulty.ToUpper(), "Segoe UI", 13,
                new Color(160, 160, 180));
            spriteBatch.Draw(diffTex, new Vector2(16, 38), Color.White);

            // HUD: Score (top-right)
            var scoreTex = textRenderer!.GetTexture($"{Localization.Get("score")}: {score}", "Segoe UI", 18, Color.White);
            spriteBatch.Draw(scoreTex, new Vector2(width - scoreTex.Width - 16, 14), Color.White);

            // HUD: Combo (centered above hit zone)
            if (combo > 1)
            {
                var comboTex = textRenderer!.GetTexture($"{combo}x", "Segoe UI", 36, Color.White);
                spriteBatch.Draw(comboTex,
                    new Vector2((width - comboTex.Width) / 2, hitZoneY - 56),
                    Color.White * 0.9f);
            }

            // Progress bar at top
            if (songDurationSeconds > 0)
            {
                float prog = Math.Clamp((float)(stopwatch.Elapsed.TotalSeconds / songDurationSeconds), 0f, 1f);
                spriteBatch.Draw(pixel!, new Rectangle(0, 0, width, 3), Color.White * 0.08f);
                spriteBatch.Draw(pixel!, new Rectangle(0, 0, (int)(width * prog), 3), new Color(0, 200, 255));
            }

            // Editor mode indicator
            if (editorMode)
            {
                var edTex = textRenderer!.GetTexture(Localization.Get("editor_hint"), "Segoe UI", 14, Color.Yellow);
                spriteBatch.Draw(edTex, new Vector2((width - edTex.Width) / 2, height - 22), Color.White);
            }
        }

        void DrawMenu()
        {
            int centerX = width / 2;

            // Title glow animation
            float pulse = (float)(0.8 + 0.2 * Math.Sin(menuTimer * 2.0));
            int circleSize = 160;
            int titleCY = 110;

            for (int glow = 1; glow <= 5; glow++)
            {
                float a = 0.05f * (6 - glow) * pulse;
                int size = circleSize + glow * 22;
                spriteBatch!.Draw(circleTexture!,
                    new Rectangle(centerX - size / 2, titleCY - size / 2, size, size),
                    new Color(0, 200, 255) * a);
            }

            spriteBatch!.Draw(circleTexture!,
                new Rectangle(centerX - circleSize / 2, titleCY - circleSize / 2, circleSize, circleSize),
                new Color(0, 100, 180) * 0.25f);

            var titleTex = textRenderer!.GetTexture("CLICK", "Segoe UI", 56, Color.White);
            spriteBatch.Draw(titleTex,
                new Vector2(centerX - titleTex.Width / 2, titleCY - titleTex.Height / 2),
                Color.White);

            // Song selection
            int cardY = titleCY + circleSize / 2 + 16;
            if (songs.Count > 0)
            {
                var s = songs[currentSongIndex];
                string songLabel = $"\u25C0  {s.Title}  \u25B6";
                var songTex = textRenderer!.GetTexture(songLabel, "Segoe UI", 18, Color.White);
                spriteBatch.Draw(songTex,
                    new Vector2(centerX - songTex.Width / 2, cardY), Color.White);

                string diffStr = $"{Localization.Get("difficulty")}: {currentDifficulty.ToUpper()}";
                if (s.Difficulties.Count > 1) diffStr += "  (Tab)";
                var diffTex = textRenderer!.GetTexture(diffStr, "Segoe UI", 13, new Color(140, 140, 170));
                spriteBatch.Draw(diffTex,
                    new Vector2(centerX - diffTex.Width / 2, cardY + 28), Color.White);
            }

            // Menu buttons
            int optW = 260;
            int optH = 42;
            int gap = 8;
            int optX = centerX - optW / 2;
            int optY = cardY + 64;

            for (int i = 0; i < menuKeys.Length; i++)
            {
                bool sel = i == currentMenuIndex;
                var btnRect = new Rectangle(optX, optY + i * (optH + gap), optW, optH);

                if (sel)
                {
                    spriteBatch.Draw(pixel!, btnRect, new Color(0, 200, 255) * 0.12f);
                    spriteBatch.Draw(pixel!,
                        new Rectangle(btnRect.X, btnRect.Y, 3, btnRect.Height),
                        new Color(0, 200, 255));
                }
                else
                {
                    spriteBatch.Draw(pixel!, btnRect, Color.White * 0.04f);
                }

                DrawRectBorder(btnRect, sel ? new Color(0, 200, 255) * 0.25f : Color.White * 0.06f);

                var textCol = sel ? Color.White : new Color(180, 180, 200);
                var tex = textRenderer!.GetTexture(Localization.Get(menuKeys[i]), "Segoe UI", 20, textCol);
                spriteBatch.Draw(tex,
                    new Vector2(btnRect.X + 20, btnRect.Y + (btnRect.Height - tex.Height) / 2),
                    Color.White);
            }

            // Hint bar
            var hint = textRenderer!.GetTexture(
                Localization.Get("hint_menu"),
                "Segoe UI", 12, new Color(100, 100, 130));
            spriteBatch.Draw(hint,
                new Vector2(centerX - hint.Width / 2, height - 32), Color.White);
        }

        void DrawResult()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);

            int centerX = width / 2;
            int cardW = 340;
            int cardH = 360;
            int cardX = centerX - cardW / 2;
            int cardY = (height - cardH) / 2;

            // Card
            var cardRect = new Rectangle(cardX, cardY, cardW, cardH);
            spriteBatch.Draw(pixel!, cardRect, new Color(16, 16, 36) * 0.95f);
            DrawRectBorder(cardRect, new Color(0, 200, 255) * 0.25f);

            // Title
            var title = textRenderer!.GetTexture(Localization.Get("result"), "Segoe UI", 28, Color.White);
            spriteBatch.Draw(title,
                new Vector2(centerX - title.Width / 2, cardY + 18), Color.White);

            // Separator
            spriteBatch.Draw(pixel!,
                new Rectangle(cardX + 20, cardY + 56, cardW - 40, 1), Color.White * 0.12f);

            // Grade
            Color gradeColor = resultGrade switch
            {
                "SS" => new Color(255, 220, 50),
                "S" => new Color(255, 180, 0),
                "A" => new Color(80, 255, 120),
                "B" => new Color(0, 200, 255),
                "C" => new Color(180, 140, 255),
                _ => new Color(255, 80, 80),
            };
            var gradeTex = textRenderer!.GetTexture(resultGrade, "Segoe UI", 52, gradeColor);
            spriteBatch.Draw(gradeTex,
                new Vector2(centerX - gradeTex.Width / 2, cardY + 68), Color.White);

            // Stats
            int sy = cardY + 140;
            int sx = cardX + 28;
            int sw = cardW - 56;
            int lineH = 24;

            DrawStatLine(Localization.Get("score"), $"{score} / {maxScore}", sy, sx, sw);
            DrawStatLine(Localization.Get("max_combo"), $"{maxCombo}x", sy + lineH, sx, sw);
            DrawStatLine(Localization.Get("hit"), $"{hitCount}", sy + lineH * 2, sx, sw);
            DrawStatLine(Localization.Get("miss"), $"{missCount}", sy + lineH * 3, sx, sw);
            int totalNotes = hitCount + missCount;
            string accStr = totalNotes > 0 ? $"{(double)hitCount / totalNotes * 100:F1}%" : "--";
            DrawStatLine(Localization.Get("accuracy"), accStr, sy + lineH * 4, sx, sw);

            // Separator
            spriteBatch.Draw(pixel!,
                new Rectangle(cardX + 20, sy + lineH * 5 + 4, cardW - 40, 1), Color.White * 0.12f);

            // Buttons
            string[] opts = { Localization.Get("retry"), Localization.Get("menu") };
            int resultOptY = sy + lineH * 5 + 16;
            for (int i = 0; i < opts.Length; i++)
            {
                bool sel = i == resultMenuIndex;
                int bx = centerX - 80;
                int by = resultOptY + i * 34;

                if (sel)
                {
                    spriteBatch.Draw(pixel!, new Rectangle(bx, by, 160, 28),
                        new Color(0, 200, 255) * 0.12f);
                    spriteBatch.Draw(pixel!, new Rectangle(bx, by, 3, 28),
                        new Color(0, 200, 255));
                }

                var col = sel ? Color.White : new Color(160, 160, 180);
                var t = textRenderer!.GetTexture(opts[i], "Segoe UI", 16, col);
                spriteBatch.Draw(t, new Vector2(bx + 14, by + (28 - t.Height) / 2), Color.White);
            }
        }

        void DrawAccount()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);

            int centerX = width / 2;
            int cardW = 380;
            int cardH = 300;
            int cardX = centerX - cardW / 2;
            int cardY = (height - cardH) / 2;

            var cardRect = new Rectangle(cardX, cardY, cardW, cardH);
            spriteBatch.Draw(pixel!, cardRect, new Color(16, 16, 36) * 0.95f);
            DrawRectBorder(cardRect, new Color(160, 80, 255) * 0.25f);

            var title = textRenderer!.GetTexture(Localization.Get("account_register"), "Segoe UI", 22, Color.White);
            spriteBatch.Draw(title,
                new Vector2(centerX - title.Width / 2, cardY + 18), Color.White);

            spriteBatch.Draw(pixel!,
                new Rectangle(cardX + 20, cardY + 52, cardW - 40, 1), Color.White * 0.12f);

            int fx = cardX + 28;
            int fw = cardW - 56;

            // Username
            var uLabel = textRenderer!.GetTexture(Localization.Get("username"), "Segoe UI", 13, new Color(160, 160, 180));
            spriteBatch.Draw(uLabel, new Vector2(fx, cardY + 66), Color.White);
            var uBox = new Rectangle(fx, cardY + 86, fw, 34);
            spriteBatch.Draw(pixel!, uBox, accountFieldIndex == 0 ? Color.White * 0.07f : Color.White * 0.03f);
            DrawRectBorder(uBox, accountFieldIndex == 0 ? new Color(0, 200, 255) * 0.4f : Color.White * 0.08f);
            var uText = textRenderer!.GetTexture(accountUsername + (accountFieldIndex == 0 ? "|" : ""),
                "Segoe UI", 15, Color.White);
            spriteBatch.Draw(uText, new Vector2(uBox.X + 8, uBox.Y + 7), Color.White);

            // Password
            var pLabel = textRenderer!.GetTexture(Localization.Get("password"), "Segoe UI", 13, new Color(160, 160, 180));
            spriteBatch.Draw(pLabel, new Vector2(fx, cardY + 132), Color.White);
            var pBox = new Rectangle(fx, cardY + 152, fw, 34);
            spriteBatch.Draw(pixel!, pBox, accountFieldIndex == 1 ? Color.White * 0.07f : Color.White * 0.03f);
            DrawRectBorder(pBox, accountFieldIndex == 1 ? new Color(0, 200, 255) * 0.4f : Color.White * 0.08f);
            var masked = new string('*', accountPassword.Length);
            var pText = textRenderer!.GetTexture(masked + (accountFieldIndex == 1 ? "|" : ""),
                "Segoe UI", 15, Color.White);
            spriteBatch.Draw(pText, new Vector2(pBox.X + 8, pBox.Y + 7), Color.White);

            // Hint
            var hint = textRenderer!.GetTexture(Localization.Get("hint_account"),
                "Segoe UI", 12, new Color(100, 100, 130));
            spriteBatch.Draw(hint,
                new Vector2(centerX - hint.Width / 2, cardY + 202), Color.White);

            // Message
            if (accountShowMessage)
            {
                bool success = accountMessage == Localization.Get("register_success");
                var msgColor = success ? new Color(80, 255, 120) : new Color(255, 100, 100);
                var m = textRenderer!.GetTexture(accountMessage, "Segoe UI", 14, msgColor);
                spriteBatch.Draw(m, new Vector2(centerX - m.Width / 2, cardY + 230), Color.White);
            }
        }

        void DrawLanguage()
        {
            spriteBatch!.Draw(pixel!, new Rectangle(0, 0, width, height), Color.Black * 0.7f);

            int centerX = width / 2;
            int langCount = Localization.All.Length;
            int cardW = 340;
            int cardH = 80 + langCount * 42;
            int cardX = centerX - cardW / 2;
            int cardY = (height - cardH) / 2;

            var cardRect = new Rectangle(cardX, cardY, cardW, cardH);
            spriteBatch.Draw(pixel!, cardRect, new Color(16, 16, 36) * 0.95f);
            DrawRectBorder(cardRect, new Color(0, 200, 255) * 0.25f);

            var title = textRenderer!.GetTexture(Localization.Get("select_language"), "Segoe UI", 22, Color.White);
            spriteBatch.Draw(title,
                new Vector2(centerX - title.Width / 2, cardY + 16), Color.White);

            spriteBatch.Draw(pixel!,
                new Rectangle(cardX + 20, cardY + 50, cardW - 40, 1), Color.White * 0.12f);

            int optY = cardY + 60;
            for (int i = 0; i < langCount; i++)
            {
                bool sel = i == languageMenuIndex;
                bool active = Localization.All[i] == Localization.Current;
                int bx = cardX + 20;
                int by = optY + i * 42;
                int bw = cardW - 40;
                int bh = 34;

                if (sel)
                {
                    spriteBatch.Draw(pixel!, new Rectangle(bx, by, bw, bh),
                        new Color(0, 200, 255) * 0.12f);
                    spriteBatch.Draw(pixel!, new Rectangle(bx, by, 3, bh),
                        new Color(0, 200, 255));
                }

                var textCol = sel ? Color.White : new Color(180, 180, 200);
                string displayName = Localization.LanguageDisplayName(Localization.All[i]);
                if (active) displayName += "  \u2713";
                var tex = textRenderer!.GetTexture(displayName, "Segoe UI", 16, textCol);
                spriteBatch.Draw(tex,
                    new Vector2(bx + 14, by + (bh - tex.Height) / 2), Color.White);
            }

            var hint = textRenderer!.GetTexture(Localization.Get("hint_language"),
                "Segoe UI", 12, new Color(100, 100, 130));
            spriteBatch.Draw(hint,
                new Vector2(centerX - hint.Width / 2, cardY + cardH - 24), Color.White);
        }

        void DrawStatLine(string label, string value, int y, int x, int w)
        {
            var labelTex = textRenderer!.GetTexture(label, "Segoe UI", 14, new Color(140, 140, 170));
            var valueTex = textRenderer!.GetTexture(value, "Segoe UI", 14, Color.White);
            spriteBatch!.Draw(labelTex, new Vector2(x, y), Color.White);
            spriteBatch.Draw(valueTex, new Vector2(x + w - valueTex.Width, y), Color.White);
        }

        void DrawRectBorder(Rectangle rect, Color color)
        {
            spriteBatch!.Draw(pixel!, new Rectangle(rect.Left, rect.Top, rect.Width, 1), color);
            spriteBatch.Draw(pixel!, new Rectangle(rect.Left, rect.Bottom - 1, rect.Width, 1), color);
            spriteBatch.Draw(pixel!, new Rectangle(rect.Left, rect.Top, 1, rect.Height), color);
            spriteBatch.Draw(pixel!, new Rectangle(rect.Right - 1, rect.Top, 1, rect.Height), color);
        }

        void HandleAccountInput(KeyboardState kbState, KeyboardState prevKbState)
        {
            bool shift = kbState.IsKeyDown(Keys.LeftShift) || kbState.IsKeyDown(Keys.RightShift);

            if (kbState.IsKeyDown(Keys.Tab) && !prevKbState.IsKeyDown(Keys.Tab))
            {
                accountFieldIndex = (accountFieldIndex + 1) % 2;
                return;
            }

            if (kbState.IsKeyDown(Keys.Back) && !prevKbState.IsKeyDown(Keys.Back))
            {
                if (accountFieldIndex == 0 && accountUsername.Length > 0) accountUsername = accountUsername[..^1];
                else if (accountFieldIndex == 1 && accountPassword.Length > 0) accountPassword = accountPassword[..^1];
                return;
            }

            if (kbState.IsKeyDown(Keys.Enter) && !prevKbState.IsKeyDown(Keys.Enter))
            {
                if (accountsManager != null)
                {
                    if (accountsManager.Register(accountUsername, accountPassword, out var msg))
                    {
                        accountShowMessage = true;
                        accountMessage = Localization.Get("register_success");
                        accountPassword = string.Empty;
                    }
                    else
                    {
                        accountShowMessage = true;
                        accountMessage = msg;
                    }
                }
                return;
            }

            foreach (Keys k in Enum.GetValues(typeof(Keys)))
            {
                if (k == Keys.None) continue;
                if (kbState.IsKeyDown(k) && !prevKbState.IsKeyDown(k))
                {
                    char ch = KeyToChar(k, shift);
                    if (ch != '\0')
                    {
                        if (accountFieldIndex == 0) accountUsername += ch;
                        else accountPassword += ch;
                    }
                }
            }
        }

        static char KeyToChar(Keys k, bool shift)
        {
            if (k >= Keys.A && k <= Keys.Z)
            {
                char c = (char)('a' + (k - Keys.A));
                return shift ? char.ToUpper(c) : c;
            }
            if (k >= Keys.D0 && k <= Keys.D9) return (char)('0' + (k - Keys.D0));
            if (k >= Keys.NumPad0 && k <= Keys.NumPad9) return (char)('0' + (k - Keys.NumPad0));
            if (k == Keys.OemMinus) return '-';
            if (k == Keys.OemPeriod) return '.';
            if (k == Keys.Space) return ' ';
            return '\0';
        }
    }
}
