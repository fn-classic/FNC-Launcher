$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& (Join-Path $taskRoot 'Build.ps1')
$taskOutput=Join-Path $taskRoot 'build/update-tests'
New-Item -ItemType Directory -Force -Path $taskOutput|Out-Null
Copy-Item -LiteralPath (Join-Path $taskRoot 'build/Launcher/FNClassicLauncher.exe') -Destination $taskOutput -Force
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:exe ('/out:'+(Join-Path $taskOutput 'UpdateTests.exe')) ('/reference:'+(Join-Path $taskOutput 'FNClassicLauncher.exe')) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'UpdateTests.cs')
if($LASTEXITCODE -ne 0){throw 'Updater test build failed.'}
& (Join-Path $taskOutput 'UpdateTests.exe')
if($LASTEXITCODE -ne 0){throw 'Updater tests failed.'}
