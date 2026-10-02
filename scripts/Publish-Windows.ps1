# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (c) 2026 MoriTeahouse (森之宿茶室)
param([string]$OutputPath,[string]$ArchivePath,[string]$ReleaseTag='v0.6.1-test.2',[string]$EngineRoot,[string]$AtrPath)
$ErrorActionPreference='Stop'
$taskRepo=Split-Path -Parent $PSScriptRoot
if(-not $OutputPath){$OutputPath=Join-Path $taskRepo 'artifacts/windows/RhythmClicker'}
if(-not $ArchivePath){$ArchivePath=Join-Path $taskRepo 'artifacts/RhythmClicker-0.6.1-test.2-win-x64.zip'}
$OutputPath=[IO.Path]::GetFullPath($OutputPath);$ArchivePath=[IO.Path]::GetFullPath($ArchivePath)
if(-not $EngineRoot){$EngineRoot=Join-Path $taskRepo 'lib/MatrixTea-Engine'}
$EngineRoot=[IO.Path]::GetFullPath($EngineRoot)
if((Test-Path -LiteralPath $OutputPath) -and (Get-ChildItem -LiteralPath $OutputPath -Force | Select-Object -First 1)){throw 'Output directory must be empty.'}
if(Test-Path -LiteralPath $ArchivePath){throw 'Archive already exists.'}
$taskVersionParts=$ReleaseTag.TrimStart('v').Split('-',2)
$taskVersion=$taskVersionParts[0]
if($ReleaseTag -notmatch '^v\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$'){throw 'Invalid release tag.'}
$taskRevision=(git -C $taskRepo rev-parse HEAD).Trim()
if($LASTEXITCODE -ne 0){throw 'Source revision is required.'}
$taskEngineRevision=(git -C $EngineRoot rev-parse HEAD).Trim()
if($LASTEXITCODE -ne 0){throw 'Engine revision is required.'}
dotnet publish (Join-Path $taskRepo 'ClickerGame/ClickerGame.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -p:CopyOutputSymbolsToPublishDirectory=false "-p:EngineRoot=$EngineRoot" -o $OutputPath --nologo
if($LASTEXITCODE -ne 0){throw 'Game publish failed.'}
$taskForbidden=Get-ChildItem -LiteralPath $OutputPath -Recurse -File | Where-Object { $_.Extension -in '.cs','.csproj','.pdb' } | Select-Object -First 1
if($taskForbidden){throw 'Player package contains source files or debug symbols.'}
Copy-Item -LiteralPath (Join-Path $OutputPath 'PLAYER.md') -Destination (Join-Path $OutputPath 'README.md')
$taskSources=@"
# RhythmClicker corresponding source
Copyright (c) 2026 MoriTeahouse (森之宿茶室). AGPL-3.0-only.
Game revision: $taskRevision
https://github.com/MoriTeahouse/RhythmClicker/tree/$taskRevision
https://github.com/MoriTeahouse/RhythmClicker/archive/$taskRevision.zip
Engine revision: $taskEngineRevision
https://github.com/MoriTeahouse/MatrixTea-Engine/tree/$taskEngineRevision
https://github.com/MoriTeahouse/MatrixTea-Engine/archive/$taskEngineRevision.zip
Build guide: docs/development.md in the game source.
Clone the game recursively, or obtain both source archives and set EngineRoot.
Third-party source and license records: LICENSES and THIRD-PARTY-NOTICES.md.
"@
[IO.File]::WriteAllText((Join-Path $OutputPath 'SOURCE.md'),$taskSources,[Text.UTF8Encoding]::new($false))
$taskAssets=Get-Content -LiteralPath (Join-Path $taskRepo 'ClickerGame/obj/project.assets.json') -Raw | ConvertFrom-Json -AsHashtable
$taskInventory=@()
foreach($taskLib in $taskAssets.libraries.GetEnumerator()){
 if($taskLib.Value.type -ne 'package'){continue}
 $taskParts=$taskLib.Key.Split('/')
 foreach($taskFolder in $taskAssets.packageFolders.Keys){
  $taskPackage=Join-Path $taskFolder $taskLib.Value.path
  $taskSpec=Get-ChildItem -LiteralPath $taskPackage -Filter '*.nuspec' -File -ErrorAction SilentlyContinue | Select-Object -First 1
  if($taskSpec){
   [xml]$taskMetadata=Get-Content -LiteralPath $taskSpec.FullName -Raw
   $taskMeta=$taskMetadata.package.metadata
   $taskInventory+=@{id=$taskParts[0];version=$taskParts[1];authors=[string]$taskMeta.authors;copyright=[string]$taskMeta.copyright;license=[string]$taskMeta.license.InnerText;source=[string]$taskMeta.projectUrl}
   break
  }
 }
}
$taskInventory | Sort-Object {$_['id']} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputPath 'LICENSES/dependencies.json') -Encoding utf8NoBOM
$taskRuntime=Get-Content -LiteralPath (Join-Path $OutputPath 'ClickerGame.runtimeconfig.json') -Raw | ConvertFrom-Json
$taskRuntimeVersion=($taskRuntime.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.NETCore.App').version
if(-not $taskRuntimeVersion){throw 'Package must include app-local .NET runtime.'}
foreach($taskFolder in $taskAssets.packageFolders.Keys){
 $taskRuntimeFolder=Join-Path $taskFolder "microsoft.netcore.app.runtime.win-x64/$taskRuntimeVersion"
 if(Test-Path -LiteralPath (Join-Path $taskRuntimeFolder 'LICENSE.TXT')){
  Copy-Item -LiteralPath (Join-Path $taskRuntimeFolder 'LICENSE.TXT') -Destination (Join-Path $OutputPath 'LICENSES/DotNet-Runtime.txt')
  Copy-Item -LiteralPath (Join-Path $taskRuntimeFolder 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $OutputPath 'LICENSES/DotNet-Third-Party.txt')
  break
 }
}
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
$taskSourceRecords=Get-Content -LiteralPath (Join-Path $OutputPath 'LICENSES/native-sources.md') -Raw
$taskSourceRecords=$taskSourceRecords -replace '\.NET runtime 8\.0\.24: https://github.com/dotnet/runtime/tree/v8\.0\.24',".NET runtime ${taskRuntimeVersion}: https://github.com/dotnet/runtime/tree/v$taskRuntimeVersion"
if($taskDesktopSource){$taskSourceRecords+="
.NET Windows Desktop $taskDesktopVersion source: $taskDesktopSource
"}
[IO.File]::WriteAllText((Join-Path $OutputPath 'LICENSES/native-sources.md'),$taskSourceRecords,[Text.UTF8Encoding]::new($false))
$taskVersion | Set-Content -LiteralPath (Join-Path $OutputPath 'version.txt') -Encoding utf8NoBOM
@{version=$taskVersion;release_tag=$ReleaseTag;source_revision=$taskRevision;engine_revision=$taskEngineRevision;self_contained=$true;runtime='win-x64';runtime_version=$taskRuntimeVersion} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputPath 'build-info.json') -Encoding utf8NoBOM
@{gameId='RhythmClicker';version=$ReleaseTag.TrimStart('v');schemaVersion=1} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputPath 'rhythmclicker-package.json') -Encoding utf8NoBOM
@{schemaVersion=1;platform='win-x64';deployment='app-local';runtimeVersion=$taskRuntimeVersion;probe='--check-environment'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputPath 'rhythmclicker-environment.json') -Encoding utf8NoBOM
New-Item -ItemType Directory -Path (Split-Path -Parent $ArchivePath) -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($OutputPath,$ArchivePath,[IO.Compression.CompressionLevel]::Optimal,$false)
$taskHash=(Get-FileHash -LiteralPath $ArchivePath -Algorithm SHA256).Hash.ToLowerInvariant()
"$taskHash  $([IO.Path]::GetFileName($ArchivePath))" | Set-Content -LiteralPath ($ArchivePath+'.sha256') -Encoding utf8NoBOM
@{version=$taskVersion;download_url="https://github.com/MoriTeahouse/RhythmClicker/releases/download/$ReleaseTag/RhythmClicker.zip";sha256=$taskHash} | ConvertTo-Json | Set-Content -LiteralPath ($ArchivePath+'.manifest.json') -Encoding utf8NoBOM
$taskAsset='RhythmClicker.zip';$taskPackagePath=$ArchivePath
if($AtrPath){
 $AtrPath=[IO.Path]::GetFullPath($AtrPath)
 if(Test-Path -LiteralPath $AtrPath){throw 'ATR archive already exists.'}
 dotnet run --project (Join-Path $EngineRoot 'src/MatrixTea.Packaging.Cli') -c Release -- pack $OutputPath $AtrPath
 if($LASTEXITCODE -ne 0){throw 'ATR packaging failed.'}
 $taskHash=(Get-FileHash -LiteralPath $AtrPath -Algorithm SHA256).Hash.ToLowerInvariant()
 "$taskHash  $([IO.Path]::GetFileName($AtrPath))" | Set-Content -LiteralPath ($AtrPath+'.sha256') -Encoding utf8NoBOM
 $taskAsset='RhythmClicker.atr';$taskPackagePath=$AtrPath
}
@{schemaVersion=2;version=$ReleaseTag.TrimStart('v');displayName="RhythmClicker $($ReleaseTag.TrimStart('v'))";channel=$(if($taskVersionParts.Count -gt 1){'testing'}else{'stable'});assetName=$taskAsset;sha256=$taskHash;size=(Get-Item -LiteralPath $taskPackagePath).Length;notes=@('README 原始 SVG 圖標 · 專屬啟動器','遊戲、玩家資料與下載暫存使用所選硬碟','自帶 .NET 8 / SDL2 / OpenAL · 安裝前實際環境檢查')} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path (Split-Path -Parent $ArchivePath) 'rhythmclicker-release.json') -Encoding utf8NoBOM
Write-Output "Published: $OutputPath"
Write-Output "Archive: $ArchivePath"
Write-Output "SHA256: $taskHash"
