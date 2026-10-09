param([Parameter(Mandatory=$true)][string]$Installer,[Parameter(Mandatory=$true)][string]$Version,[Parameter(Mandatory=$true)][string]$Tag,[string]$Notes='Launcher, backend and gameserver update.',[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$taskFile=Get-Item -LiteralPath $Installer
$taskVersion=[Version]$Version
if(!$OutputDirectory){$OutputDirectory=$taskFile.DirectoryName}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$taskManifest=[ordered]@{schema=1;version=$taskVersion.ToString();launcherVersion=$taskVersion.ToString();backendVersion=$taskVersion.ToString();gameserverVersion=$taskVersion.ToString();installerUrl=('https://github.com/fn-classic/FNC-Launcher/releases/download/'+[Uri]::EscapeDataString($Tag)+'/'+[Uri]::EscapeDataString($taskFile.Name));sha256=(Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256).Hash.ToLowerInvariant();size=$taskFile.Length;notes=$Notes}
$taskJson=$taskManifest|ConvertTo-Json
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'update.json'),$taskJson,(New-Object Text.UTF8Encoding($false)))
'Upload this installer and update.json as assets on the same published FNC-Launcher GitHub release. The bundled release includes all three components.'
