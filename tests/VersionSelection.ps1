param(
 [Parameter(Mandatory=$true)][string]$Build172,
 [Parameter(Mandatory=$true)][string]$Build18,
 [Parameter(Mandatory=$true)][string]$Build182,
 [Parameter(Mandatory=$true)][string]$Build35
)
$ErrorActionPreference='Stop'
if($PSEdition -eq 'Core') {
 # UI assemblies target the installed Windows .NET Framework, not PowerShell's
 # newer .NET runtime. Run the rendering fixture in the same runtime as the app.
 & (Join-Path $env:WINDIR 'System32/WindowsPowerShell/v1.0/powershell.exe') -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath @PSBoundParameters
 exit $LASTEXITCODE
}
$taskRepo=Split-Path -Parent $PSScriptRoot
& (Join-Path $taskRepo 'Build.ps1')
[Reflection.Assembly]::LoadFrom((Join-Path $taskRepo 'build/Launcher/FNClassicLauncher.exe')) | Out-Null
Add-Type -AssemblyName System.Windows.Forms
$taskRoot=Join-Path $taskRepo ('build/version-tests/'+[Guid]::NewGuid().ToString('N'))
$taskPaths=@{'1.7.2'=$Build172;'1.8'=$Build18;'1.8.2'=$Build182;'3.5'=$Build35}
$taskVersions=New-Object FssLauncher.VersionInstallations($taskRoot)
foreach($taskVersion in @('1.7.2','1.8','1.8.2','3.5')) {
 $taskVersions.Register($taskVersion,$taskPaths[$taskVersion])
 $taskVersions.Selected=$taskVersion;$taskVersions.Save()
 $taskReload=New-Object FssLauncher.VersionInstallations($taskRoot)
 if($taskReload.Selected -ne $taskVersion -or [CoreStudio.FortniteBuilds]::Detect($taskReload.Executable) -ne $taskVersion){throw 'Version identity/persistence failed.'}
 Write-Output ('PASS: '+$taskVersion+' identity, installation and selection persist.')
}
try {$taskVersions.Register('1.8',$Build35);throw 'Wrong build was accepted.'}
catch {if($_.Exception.Message -eq 'Wrong build was accepted.'){throw};Write-Output 'PASS: mismatched installation rejected.'}
$taskDialog=New-Object FssLauncher.MatchSettingsDialog($taskRoot,$true)
try {if($taskDialog.Controls.Find('gameVersion',$true)[0].Enabled){throw 'Version switching remained enabled during gameplay.'}}
finally {$taskDialog.Dispose()}
Write-Output 'PASS: active game blocks version changes.'
foreach($taskVersion in @('1.7.2','3.5')) {
 $taskVersions.Selected=$taskVersion;$taskVersions.Save()
 $taskForm=New-Object FssLauncher.LauncherForm($taskRoot,$true)
 try {
  $taskForm.ShowInTaskbar=$false;$taskForm.StartPosition='Manual';$taskForm.Location=New-Object Drawing.Point(-20000,-20000);$taskForm.Show()
  foreach($taskSize in @(@(1024,720),@(1408,898),@(1920,1080))) {
   $taskForm.ClientSize=New-Object Drawing.Size($taskSize[0],$taskSize[1]);[Windows.Forms.Application]::DoEvents()
   $taskImage=New-Object Drawing.Bitmap($taskForm.Width,$taskForm.Height)
   try {$taskForm.DrawToBitmap($taskImage,(New-Object Drawing.Rectangle(0,0,$taskImage.Width,$taskImage.Height)));$taskImage.Save((Join-Path $taskRoot ($taskVersion+'-'+$taskSize[0]+'.png')))}
   finally {$taskImage.Dispose()}
  }
 } finally {$taskForm.Close();$taskForm.Dispose()}
}
Write-Output ('Historical/current UI captures saved: '+$taskRoot)
