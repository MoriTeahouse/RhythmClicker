// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 MoriTeahouse (森之宿茶室)
using System.Runtime.InteropServices;
namespace ClickerGame.Systems;
/// <summary>Uses .NET's Unicode-aware Windows loader for installations containing Chinese paths.</summary>
internal sealed class UnicodeFFmpegResolver : FFmpeg.AutoGen.FunctionResolverBase
{
    protected override string GetNativeLibraryName(string name,int version)=>$"{name}-{version}.dll";
    protected override IntPtr LoadNativeLibrary(string path)=>NativeLibrary.TryLoad(path,out var handle)?handle:IntPtr.Zero;
    protected override IntPtr FindFunctionPointer(IntPtr handle,string name)=>handle!=IntPtr.Zero&&NativeLibrary.TryGetExport(handle,name,out var address)?address:IntPtr.Zero;
}
