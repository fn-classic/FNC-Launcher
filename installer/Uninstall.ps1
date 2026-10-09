$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\')
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $taskRoot 'Backend/Start.ps1') -Stop
if($LASTEXITCODE -ne 0){throw 'Stop Studio and its backend before uninstalling.'}
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $taskRoot 'Backend/Start-LawinV1.ps1') -Stop
foreach($line in Get-Content -LiteralPath (Join-Path $taskRoot 'payload.sha256')){
    $relative=$line.Substring(66)
    # Keep account/chat data, settings, credentials and certificates for reinstall.
    if($relative -match '^Backend/runtime/' -or $relative -eq 'Gameserver/Studio/settings.xml'){continue}
    $target=[IO.Path]::GetFullPath((Join-Path $taskRoot $relative))
    if(!$target.StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid installed manifest.'}
    if(Test-Path -LiteralPath $target -PathType Leaf){Remove-Item -LiteralPath $target}
}
$taskShell=New-Object -ComObject WScript.Shell
foreach($folder in @([Environment]::GetFolderPath('Desktop'),(Join-Path ([Environment]::GetFolderPath('Programs')) 'Fortnite Server Studio'),(Join-Path ([Environment]::GetFolderPath('Programs')) 'Fortnite Classic'))){
    foreach($name in @('Fortnite Server Studio.lnk','FN Classic.lnk','Fortnite Classic Studio.lnk','Fortnite Classic.lnk')){
        $shortcut=Join-Path $folder $name
        if(Test-Path -LiteralPath $shortcut -PathType Leaf){$target=$taskShell.CreateShortcut($shortcut).TargetPath;if($target -and [IO.Path]::GetFullPath($target).StartsWith($taskRoot+'\',[StringComparison]::OrdinalIgnoreCase)){Remove-Item -LiteralPath $shortcut}}
    }
}
$launcher=Join-Path $taskRoot 'Launcher/FNClassicLauncher.exe';if(Test-Path -LiteralPath $launcher){Remove-Item -LiteralPath $launcher}
Remove-Item -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\FortniteServerStudio' -ErrorAction SilentlyContinue
Write-Host 'Studio removed. Your accounts and saved settings remain in the installation folder.'
