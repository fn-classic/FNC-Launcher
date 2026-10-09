$ErrorActionPreference='Stop'
$taskRepo=Split-Path -Parent $PSScriptRoot
$taskLauncher=[IO.Path]::GetFullPath((Join-Path $taskRepo 'build/Launcher/FNClassicLauncher.exe'))
$taskTemp=Join-Path $env:TEMP ("ClassicFN-Friends-"+[Guid]::NewGuid().ToString("N"));New-Item -ItemType Directory $taskTemp | Out-Null;Copy-Item -LiteralPath $taskLauncher -Destination (Join-Path $taskTemp "FNClassicLauncher.exe");$taskLauncher=Join-Path $taskTemp "FNClassicLauncher.exe"
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
[Reflection.Assembly]::LoadFrom($taskLauncher) | Out-Null
$taskSource=@'
using System;using System.IO;using System.Linq;using System.Reflection;using System.Drawing;using System.Threading.Tasks;using System.Windows.Forms;using FssLauncher;
public static class FriendsPanelFixture {
 public static void Run(string output){
  Directory.CreateDirectory(output);var data=new FriendSnapshot{canInvite=true};
  data.friends.Add(new FriendEntry{userId="test-online",displayName="QA Player",online=true,activity="In lobby"});
  data.friends.Add(new FriendEntry{userId="test-offline",displayName="QA Offline",online=false});
  data.incoming.Add(new FriendEntry{userId="test-request",displayName="QA Request"});
  var type=typeof(LauncherForm).Assembly.GetType("FssLauncher.FriendsPanel");
  Func<Task<FriendSnapshot>> load=()=>Task.FromResult(data);
  Func<string,Task<System.Collections.Generic.List<FriendEntry>>> search=term=>Task.FromResult(data.friends);
  string operation=null,user=null;Func<string,string,Task> action=(op,id)=>{operation=op;user=id;return Task.FromResult(0);};
  using(var form=(Form)Activator.CreateInstance(type,new object[]{load,search,action,"QA Account"})){
   form.Location=new Point(-20000,-20000);form.Show();Application.DoEvents();
   if(form.FormBorderStyle!=FormBorderStyle.None||form.ShowInTaskbar)throw new Exception("Friends panel retained a native dialog/taskbar frame");
   var rows=(ListBox)type.GetField("rows",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
   if(rows.Items.Count!=4)throw new Exception("Online/offline grouping missing");
   using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,form.ClientRectangle);image.Save(Path.Combine(output,"friends.png"));}
   type.GetField("section",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,1);
   type.GetMethod("Fill",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,null);
   if(rows.Items.Count!=2)throw new Exception("Incoming request missing");
   var row=rows.Items[1];var task=(Task)type.GetMethod("Execute",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{row,"accept"});
   task.GetAwaiter().GetResult();Application.DoEvents();
   if(operation!="accept"||user!="test-request")throw new Exception("Request action used the wrong account or operation");
   form.Close();
  }
 }
}
'@
$taskSource+='
class FixtureEntry { [System.STAThread] static void Main(string[] args){FriendsPanelFixture.Run(args[0]);} }'
$taskFile=Join-Path $taskTemp 'Fixture.cs';[IO.File]::WriteAllText($taskFile,$taskSource)
$taskExe=Join-Path $taskTemp 'Fixture.exe'
& (Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe') /nologo /target:exe /main:FixtureEntry "/out:$taskExe" "/reference:$taskLauncher" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll $taskFile
if($LASTEXITCODE -ne 0){throw 'Fixture compilation failed'}
& $taskExe (Join-Path $taskRepo 'build/friends-reference')
if($LASTEXITCODE -ne 0){throw 'Friends fixture failed'}
'PASS: compact friends rendering, online/offline groups, requests and account-bound actions.'
