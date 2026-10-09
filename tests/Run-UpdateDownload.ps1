param([string]$Python='python')
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& (Join-Path $taskRoot 'Build.ps1')
$taskOutput=Join-Path $taskRoot 'build/update-tests'
New-Item -ItemType Directory -Force -Path $taskOutput|Out-Null
Copy-Item -LiteralPath (Join-Path $taskRoot 'build/Launcher/FNClassicLauncher.exe') -Destination $taskOutput -Force
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:exe ('/out:'+(Join-Path $taskOutput 'UpdateDownloadTests.exe')) ('/reference:'+(Join-Path $taskOutput 'FNClassicLauncher.exe')) (Join-Path $PSScriptRoot 'UpdateDownloadTests.cs')
if($LASTEXITCODE -ne 0){throw 'Updater download test build failed.'}
& $Python (Join-Path $PSScriptRoot 'UpdateDownloadFixture.py') (Join-Path $taskOutput 'UpdateDownloadTests.exe')
if($LASTEXITCODE -ne 0){throw 'Updater HTTPS tests failed.'}
