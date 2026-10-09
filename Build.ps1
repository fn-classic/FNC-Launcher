param([string]$Payload,[string]$OutputDirectory=(Join-Path $PSScriptRoot 'build/Launcher'))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$taskLock=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'dependency-lock.json') -Raw | ConvertFrom-Json
$taskPackage=Join-Path $PSScriptRoot $taskLock.package
if((Get-FileHash -LiteralPath $taskPackage).Hash.ToLowerInvariant() -ne $taskLock.sha256){throw 'Contract dependency checksum failed.'}
$taskSdk=Join-Path $PSScriptRoot ('build/contracts-'+$taskLock.sha256.Substring(0,12))
if(!(Test-Path -LiteralPath $taskSdk)){[IO.Compression.ZipFile]::ExtractToDirectory($taskPackage,$taskSdk)}
$taskContractSources=Get-ChildItem -LiteralPath (Join-Path $taskSdk 'contentFiles/cs/any') -Filter '*.cs' | Select-Object -ExpandProperty FullName
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$taskResources=@(('/resource:'+(Join-Path $PSScriptRoot 'assets/fn-classic-logo.png')+',FSS.LauncherLogo'),('/resource:'+(Join-Path $PSScriptRoot 'assets/launcher-reference.png')+',FSS.LauncherArtwork'))
$taskResources+=('/resource:'+(Join-Path $PSScriptRoot 'assets/launch-gear.png')+',FSS.LaunchGear')
$taskResources+=('/resource:'+(Join-Path $PSScriptRoot 'assets/versions/2017/reference.png')+',FSS.Artwork.2017')
foreach($taskVersion in @('1.7.2','1.8','1.8.2','3.5')){$taskResources+=('/resource:'+(Join-Path $PSScriptRoot ('assets/versions/'+$taskVersion+'/theme.json'))+',FSS.Theme.'+$taskVersion)}
if($Payload){$taskResources+=('/resource:'+([IO.Path]::GetFullPath($Payload))+',FSS.Payload')}
$taskCompiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $taskCompiler /nologo /target:winexe /main:FssLauncher.Program /platform:x64 /optimize+ "/out:$OutputDirectory/FNClassicLauncher.exe" ('/win32icon:'+(Join-Path $PSScriptRoot 'assets/fn-classic.ico')) /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.Linq.dll /reference:System.Security.dll /reference:System.Web.Extensions.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll $taskResources (Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Recurse -Filter '*.cs' | Select-Object -ExpandProperty FullName) $taskContractSources
if($LASTEXITCODE -ne 0){throw 'Launcher build failed.'}
'Launcher built independently. Supply -Payload only when composing a complete installer.'
