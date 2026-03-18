Development guide
=================

This export contains only the core project needed to build the prototype.

Files included:
- `Game1.cs` — main loop, scenes and gameplay
- `Beatmap.cs` — beatmap model
- `AccountsManager.cs` — simple local account registration
- `TextRenderer.cs` — runtime text generator (Windows-only)
- `ClickerGame.csproj` — project file
- `Assets/*.json` — minimal beatmap and songs metadata

Build & run (Windows):

1. Open PowerShell in the workspace root.
2. Build:
   dotnet build RhythmClicker/ClickerGame.csproj -c Debug
3. Run:
   dotnet run --project RhythmClicker/ClickerGame.csproj -c Debug

Notes:
- This is intentionally a core-only export. Large assets and compiled binaries are excluded.
- The game is incomplete — score/result screens and UI are basic. Contributions welcome.
