param([switch]$ImportExisting,[switch]$IsolatedInstall)
$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$taskLegacy=$env:CLASSICFN_LEGACY_ROOT
$taskBackend=Join-Path $taskRoot 'Backend'
$taskRuntime=Join-Path $taskBackend 'runtime/native'
if($ImportExisting -and $taskLegacy -and $taskRoot -ne $taskLegacy -and (Test-Path -LiteralPath (Join-Path $taskLegacy 'Backend/runtime/native/settings.json'))){
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $taskLegacy 'Backend/Start.ps1') -Stop
    if($LASTEXITCODE -ne 0){throw 'Could not safely stop the previous backend to preserve its account database.'}
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $taskLegacy 'Backend/Start-LawinV1.ps1') -Stop
    foreach($name in @('accounts','settings.json','config.json','shop-rotation.json')){
        $source=Join-Path $taskLegacy ('Backend/runtime/native/'+$name);$target=Join-Path $taskRuntime $name
        if((Test-Path -LiteralPath $source) -and !(Test-Path -LiteralPath $target)){Copy-Item -LiteralPath $source -Destination $target -Recurse}
    }
    $chatSource=Join-Path $taskLegacy 'Backend/runtime/native/tools/openfire/embedded-db'
    $chatTarget=Join-Path $taskRuntime 'tools/openfire/embedded-db'
    if((Test-Path -LiteralPath $chatSource) -and !(Test-Path -LiteralPath (Join-Path $chatTarget 'openfire.script'))){New-Item -ItemType Directory -Force -Path $chatTarget | Out-Null;Get-ChildItem -LiteralPath $chatSource -File | Where-Object {$_.Extension -ne '.lck'} | Copy-Item -Destination $chatTarget}
    $oldStudio=Join-Path $taskLegacy 'Gameserver/Studio-single-header-fix'
    foreach($name in @('settings.xml','credentials.xml','workspace-layout.xml')){if(Test-Path -LiteralPath (Join-Path $oldStudio $name)){Copy-Item -LiteralPath (Join-Path $oldStudio $name) -Destination (Join-Path $taskRoot ('Gameserver/Studio/'+$name)) -Force}}
}
$settingsFile=Join-Path $taskRuntime 'settings.json'
if(Test-Path -LiteralPath $settingsFile){
    $settings=Get-Content -LiteralPath $settingsFile -Raw | ConvertFrom-Json
    $settings.java=Join-Path $taskRuntime 'tools/jdk17/bin/java.exe';$settings.openfire=Join-Path $taskRuntime 'tools/openfire'
    [IO.File]::WriteAllText($settingsFile,($settings | ConvertTo-Json -Depth 20),(New-Object Text.UTF8Encoding($false)))
    # Retain the existing backend addresses, ports, domain and account configuration.
    # Start.ps1 supplies defaults only when creating a new installation.
}
$studio=Join-Path $taskRoot 'Gameserver/Studio/FortniteServerStudio.exe'
$launcher=Join-Path $taskRoot 'Launcher/FNClassicLauncher.exe'
if($IsolatedInstall){Write-Output 'Configured isolated installation without changing shortcuts or registration.';return}
$shell=New-Object -ComObject WScript.Shell
foreach($folder in @([Environment]::GetFolderPath('Desktop'),(Join-Path ([Environment]::GetFolderPath('Programs')) 'Fortnite Classic'))){
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $shortcut=$shell.CreateShortcut((Join-Path $folder 'Fortnite Classic Studio.lnk'));$shortcut.TargetPath=$studio;$shortcut.WorkingDirectory=Split-Path $studio;$shortcut.IconLocation=$studio;$shortcut.Save()
    if(Test-Path -LiteralPath $launcher){$shortcut=$shell.CreateShortcut((Join-Path $folder 'Fortnite Classic.lnk'));$shortcut.TargetPath=$launcher;$shortcut.WorkingDirectory=Split-Path $launcher;$shortcut.IconLocation=$launcher;$shortcut.Save()}
}
function Remove-OwnedLegacyShortcut([string]$Path,[string]$Target){
    if(!(Test-Path -LiteralPath $Path -PathType Leaf)){return}
    try{$existing=$shell.CreateShortcut($Path);if([String]::IsNullOrWhiteSpace($existing.TargetPath)){return};$resolved=[IO.Path]::GetFullPath($existing.TargetPath)}catch{return}
    if($resolved.Equals([IO.Path]::GetFullPath($Target),[StringComparison]::OrdinalIgnoreCase)){Remove-Item -LiteralPath $Path}
}
foreach($folder in @([Environment]::GetFolderPath('Desktop'),(Join-Path ([Environment]::GetFolderPath('Programs')) 'Fortnite Server Studio'),(Join-Path ([Environment]::GetFolderPath('Programs')) 'Fortnite Classic'))){
    Remove-OwnedLegacyShortcut (Join-Path $folder 'FN Classic.lnk') $launcher
    Remove-OwnedLegacyShortcut (Join-Path $folder 'Fortnite Server Studio.lnk') $studio
}
Write-Output 'Configured. Studio manages backend startup automatically. Existing accounts retained when imported.'
