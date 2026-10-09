param([Parameter(Mandatory=$true)][string]$PayloadLauncher,[Parameter(Mandatory=$true)][string]$PreviousTestInstallation)
$ErrorActionPreference='Stop'
$taskPayload=[IO.Path]::GetFullPath($PayloadLauncher)
$taskOutput=Split-Path -Parent $taskPayload
$taskTarget=[IO.Path]::GetFullPath($PreviousTestInstallation)
if($taskTarget -notmatch '[\\/]build[\\/]'){throw 'Use an isolated build/test installation, never a production installation.'}
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:exe ('/out:'+(Join-Path $taskOutput 'UpdateApplyTests.exe')) ('/reference:'+$taskPayload) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll (Join-Path $PSScriptRoot 'UpdateApplyTests.cs')
if($LASTEXITCODE -ne 0){throw 'Full update test build failed.'}
& (Join-Path $taskOutput 'UpdateApplyTests.exe') $taskTarget
if($LASTEXITCODE -ne 0){throw 'Full previous-release update test failed.'}
