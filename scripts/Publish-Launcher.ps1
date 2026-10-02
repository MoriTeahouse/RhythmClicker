# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (c) 2026 MoriTeahouse (森之宿茶室)
param([string]$OutputPath,[string]$ArchivePath)
$ErrorActionPreference='Stop'
$taskRepo=Split-Path -Parent $PSScriptRoot
if(-not $OutputPath){$OutputPath=Join-Path $taskRepo 'artifacts/windows/RhythmClickerLauncher'}
if(-not $ArchivePath){$ArchivePath=Join-Path $taskRepo 'artifacts/RhythmClickerLauncher-1.0.0-test.1-win-x64.zip'}
$OutputPath=[IO.Path]::GetFullPath($OutputPath);$ArchivePath=[IO.Path]::GetFullPath($ArchivePath)
if((Test-Path -LiteralPath $OutputPath) -and (Get-ChildItem -LiteralPath $OutputPath -Force | Select-Object -First 1)){throw 'Output directory must be empty.'}
if(Test-Path -LiteralPath $ArchivePath){throw 'Archive already exists.'}
dotnet publish (Join-Path $taskRepo 'src/RhythmClicker.Launcher') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -o $OutputPath --nologo
if($LASTEXITCODE -ne 0){throw 'Launcher publish failed.'}
$taskRevision=(git -C $taskRepo rev-parse HEAD).Trim();$taskEngineRevision=(git -C (Join-Path $taskRepo 'lib/MatrixTea-Engine') rev-parse HEAD).Trim()
$taskRuntime=Get-Content -LiteralPath (Join-Path $OutputPath 'RhythmClickerLauncher.runtimeconfig.json') -Raw | ConvertFrom-Json
$taskRuntimeVersion=($taskRuntime.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.NETCore.App').version
$taskAssets=Get-Content -LiteralPath (Join-Path $taskRepo 'src/RhythmClicker.Launcher/obj/project.assets.json') -Raw | ConvertFrom-Json -AsHashtable
$taskCopied=$false
foreach($taskFolder in $taskAssets.packageFolders.Keys){
 $taskRuntimeFolder=Join-Path $taskFolder "microsoft.netcore.app.runtime.win-x64/$taskRuntimeVersion"
 if(Test-Path -LiteralPath (Join-Path $taskRuntimeFolder 'LICENSE.TXT')){
  Copy-Item -LiteralPath (Join-Path $taskRuntimeFolder 'LICENSE.TXT') -Destination (Join-Path $OutputPath 'LICENSES/DotNet-Runtime.txt')
  Copy-Item -LiteralPath (Join-Path $taskRuntimeFolder 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $OutputPath 'LICENSES/DotNet-Third-Party.txt')
  $taskCopied=$true;break
 }
}
if(-not $taskCopied){throw 'Actual runtime license files not found.'}
$taskDesktopVersion=($taskRuntime.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.WindowsDesktop.App').version
$taskDesktopSource=''
if($taskDesktopVersion){
 foreach($taskFolder in $taskAssets.packageFolders.Keys){
  $taskDesktopFolder=Join-Path $taskFolder "microsoft.windowsdesktop.app.runtime.win-x64/$taskDesktopVersion"
  if(Test-Path -LiteralPath (Join-Path $taskDesktopFolder 'LICENSE')){
   Copy-Item -LiteralPath (Join-Path $taskDesktopFolder 'LICENSE') -Destination (Join-Path $OutputPath 'LICENSES/DotNet-WindowsDesktop.txt')
   [xml]$taskDesktopMetadata=Get-Content -LiteralPath (Join-Path $taskDesktopFolder 'microsoft.windowsdesktop.app.runtime.win-x64.nuspec') -Raw
   $taskDesktopSource="$($taskDesktopMetadata.package.metadata.repository.url)/tree/$($taskDesktopMetadata.package.metadata.repository.commit)"
   break
  }
 }
 if(-not $taskDesktopSource){throw 'Actual Windows Desktop runtime license files not found.'}
}
@"
# RhythmClicker Launcher corresponding source
MoriTeahouse (森之宿茶室). AGPL-3.0-only.
Game and launcher source: https://github.com/MoriTeahouse/RhythmClicker/tree/$taskRevision
Engine source: https://github.com/MoriTeahouse/MatrixTea-Engine/tree/$taskEngineRevision
.NET Windows Desktop $taskDesktopVersion source: $taskDesktopSource
.NET $taskRuntimeVersion source: https://github.com/dotnet/runtime/tree/v$taskRuntimeVersion
Build: dotnet publish src/RhythmClicker.Launcher -c Release -r win-x64 --self-contained true
Full source includes the fixed engine submodule. Third-party runtime notices: LICENSES.
"@ | Set-Content -LiteralPath (Join-Path $OutputPath 'SOURCE.md') -Encoding utf8NoBOM
Copy-Item -LiteralPath (Join-Path $taskRepo 'docs/launcher.md') -Destination (Join-Path $OutputPath 'README.md')
New-Item -ItemType Directory -Path (Split-Path -Parent $ArchivePath) -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($OutputPath,$ArchivePath,[IO.Compression.CompressionLevel]::Optimal,$false)
$taskHash=(Get-FileHash -LiteralPath $ArchivePath -Algorithm SHA256).Hash.ToLowerInvariant()
"$taskHash  $([IO.Path]::GetFileName($ArchivePath))" | Set-Content -LiteralPath ($ArchivePath+'.sha256') -Encoding utf8NoBOM
Write-Output "Launcher: $OutputPath"
Write-Output "Archive: $ArchivePath"
