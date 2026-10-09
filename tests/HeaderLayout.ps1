param([string]$Version="1.8")
$ErrorActionPreference='Stop'
$taskRepo=Split-Path -Parent $PSScriptRoot
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
[Reflection.Assembly]::LoadFrom((Join-Path $taskRepo 'build/Launcher/FNClassicLauncher.exe')) | Out-Null
$taskOutput=Join-Path $taskRepo ('build/header-reference/'+$Version)
New-Item -ItemType Directory -Force $taskOutput | Out-Null
$taskVersions=New-Object FssLauncher.VersionInstallations($taskOutput)
$taskVersions.Selected=$Version;$taskVersions.Save()
$taskForm=New-Object FssLauncher.LauncherForm($taskOutput,$true)
try{
 $taskForm.ShowInTaskbar=$false;$taskForm.StartPosition='Manual';$taskForm.Location=New-Object Drawing.Point(-20000,-20000);$taskForm.Show()
 $taskForm.LaunchButton.Enabled=$true;$taskForm.ModeButton.Enabled=$true;$taskForm.AccountButton.Text='erwinbantilan';$taskForm.AccountButton.Online=$true;$taskForm.AccountButton.Enabled=$true;$taskForm.FriendsButton.Enabled=$true
 foreach($taskSize in @(@(1024,720),@(1600,900),@(1789,965),@(1920,1080))){
  $taskForm.ClientSize=New-Object Drawing.Size($taskSize[0],$taskSize[1]);[Windows.Forms.Application]::DoEvents();$taskForm.GetType().GetMethod("PlaceControls",[Reflection.BindingFlags]"Instance,NonPublic").Invoke($taskForm,$null)
  if($taskSize[0] -ge 1408 -and ($taskForm.LaunchButton.Width+$taskForm.ModeButton.Width -ne 401 -or $taskForm.LaunchButton.Height -ne 90 -or $taskForm.ModeButton.Width -ne 50)){throw "Reference launch button dimensions differ"}
  $taskButtons=@($taskForm.AccountButton,$taskForm.DownloadButton,$taskForm.FriendsButton,$taskForm.SettingsButton)
  for($taskIndex=0;$taskIndex -lt $taskButtons.Count-1;$taskIndex++){if($taskButtons[$taskIndex].Bounds.IntersectsWith($taskButtons[$taskIndex+1].Bounds)){throw 'Header controls overlap'}}
  foreach($taskButton in $taskButtons){if(!$taskForm.ClientRectangle.Contains($taskButton.Bounds)){throw 'Header control outside window'}}
  $taskImage=New-Object Drawing.Bitmap($taskSize[0],$taskSize[1]);try{$taskGraphics=[Drawing.Graphics]::FromImage($taskImage);try{$taskPaint=New-Object Windows.Forms.PaintEventArgs($taskGraphics,$taskForm.ClientRectangle);$taskForm.GetType().GetMethod("OnPaint",[Reflection.BindingFlags]"Instance,NonPublic").Invoke($taskForm,[object[]]@($taskPaint.PSObject.BaseObject));foreach($taskControl in $taskForm.Controls){if($taskControl.Visible){$taskControl.DrawToBitmap($taskImage,$taskControl.Bounds)}}}finally{$taskGraphics.Dispose()};$taskImage.Save((Join-Path $taskOutput ('launcher-'+$taskSize[0]+'.png')))}finally{$taskImage.Dispose()}
 }
 Write-Output ('PASS: header bounds and captures at four window sizes: '+$taskOutput)
}finally{$taskForm.Close();$taskForm.Dispose()}
