# Native dependency source records

- MonoGame DesktopGL 3.8.1.303: https://github.com/MonoGame/MonoGame/tree/v3.8.1
- SDL2 (bundled DLL reports 2.0.20): https://github.com/libsdl-org/SDL/tree/release-2.0.20
- OpenAL Soft (bundled through MonoGame): https://github.com/kcat/openal-soft
- .NET runtime 8.0.24: https://github.com/dotnet/runtime/tree/v8.0.24
- SQLitePCLRaw 2.1.6: https://github.com/ericsink/SQLitePCL.raw/tree/v2.1.6
- FFmpeg.AutoGen 7.1.1: https://github.com/Ruslan-B/FFmpeg.AutoGen/tree/d90d69ba6287e040ea09e68e0fc66c641ec77a34
- Sdcb.FFmpeg.runtime.windows-x64 7.1.0: https://www.nuget.org/packages/Sdcb.FFmpeg.runtime.windows-x64/7.1.0
- FFmpeg upstream source: https://github.com/FFmpeg/FFmpeg/tree/n7.1
- FFmpeg native build recipe and third-party dependency recipes: https://github.com/BtbN/FFmpeg-Builds

The packaged avcodec DLL reports GPL version 3 or later and a 20241215 build suffix; the NuGet package declares GPL-3.0-only. Its configuration is preserved in ffmpeg-configuration.txt. This dependency license does not replace the game's AGPL-3.0-only notice. Component copyright notices remain with upstream sources. Package dependencies are listed by exact version in dependencies.json in the portable release.
