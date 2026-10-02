# SPDX-License-Identifier: AGPL-3.0-only
# Copyright (c) 2026 MoriTeahouse (森之宿茶室)
param([string]$OutputPath,[string]$ArchivePath,[string]$ReleaseTag='v0.6.0-test.1',[string]$EngineRoot)
$ErrorActionPreference='Stop'
$taskRepo=Split-Path -Parent $PSScriptRoot
if(-not $OutputPath){$OutputPath=Join-Path $taskRepo 'artifacts/windows/RhythmClicker'}
if(-not $ArchivePath){$ArchivePath=Join-Path $taskRepo 'artifacts/RhythmClicker-0.6.0-test.1-win-x64.zip'}
$OutputPath=[IO.Path]::GetFullPath($OutputPath);$ArchivePath=[IO.Path]::GetFullPath($ArchivePath)
if(-not $EngineRoot){$EngineRoot=Join-Path $taskRepo 'lib/MatrixTea-Engine'}
$EngineRoot=[IO.Path]::GetFullPath($EngineRoot)
if((Test-Path -LiteralPath $OutputPath) -and (Get-ChildItem -LiteralPath $OutputPath -Force | Select-Object -First 1)){throw 'Output directory must be empty.'}
if(Test-Path -LiteralPath $ArchivePath){throw 'Archive already exists.'}
$taskRevision=(git -C $taskRepo rev-parse HEAD).Trim()
if($LASTEXITCODE -ne 0){throw 'Source revision is required.'}
$taskEngineRevision=(git -C $EngineRoot rev-parse HEAD).Trim()
if($LASTEXITCODE -ne 0){throw 'Engine revision is required.'}
dotnet publish (Join-Path $taskRepo 'ClickerGame/ClickerGame.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false "-p:EngineRoot=$EngineRoot" -o $OutputPath --nologo
if($LASTEXITCODE -ne 0){throw 'Game publish failed.'}
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
'0.6.0' | Set-Content -LiteralPath (Join-Path $OutputPath 'version.txt') -Encoding utf8NoBOM
@{version='0.6.0';channel='test.1';source_revision=$taskRevision;engine_revision=$taskEngineRevision;self_contained=$true;runtime='win-x64'} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputPath 'build-info.json') -Encoding utf8NoBOM
New-Item -ItemType Directory -Path (Split-Path -Parent $ArchivePath) -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($OutputPath,$ArchivePath,[IO.Compression.CompressionLevel]::Optimal,$false)
$taskHash=(Get-FileHash -LiteralPath $ArchivePath -Algorithm SHA256).Hash.ToLowerInvariant()
"$taskHash  $([IO.Path]::GetFileName($ArchivePath))" | Set-Content -LiteralPath ($ArchivePath+'.sha256') -Encoding utf8NoBOM
@{version='0.6.0';download_url="https://github.com/MoriTeahouse/RhythmClicker/releases/download/$ReleaseTag/RhythmClicker.zip";sha256=$taskHash} | ConvertTo-Json | Set-Content -LiteralPath ($ArchivePath+'.manifest.json') -Encoding utf8NoBOM
Write-Output "Published: $OutputPath"
Write-Output "Archive: $ArchivePath"
Write-Output "SHA256: $taskHash"
