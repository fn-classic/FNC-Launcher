using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FssLauncher {
 public static class BackgroundUpdates {
  static bool ForegroundLauncher(string root){string path=Path.Combine(root,"Launcher/FNClassicLauncher.exe");foreach(var p in Process.GetProcessesByName("FNClassicLauncher")){using(p){try{if(p.Id!=Process.GetCurrentProcess().Id&&Path.GetFullPath(p.MainModule.FileName).Equals(path,StringComparison.OrdinalIgnoreCase))return true;}catch{}}}return false;}
  public static int Run(string root){try{return Loop(root).GetAwaiter().GetResult();}catch{return 1;}}
  static async Task<int> Loop(string root){string cache=Path.Combine(root,"Launcher/updates");Directory.CreateDirectory(cache);using(var watcherLock=new FileStream(Path.Combine(cache,"watcher.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
   var updater=new UpdateClient(root);await Task.Delay(15000);while(File.Exists(Path.Combine(root,"Gameserver/Studio/studio-control.json"))||File.Exists(Path.Combine(root,"Backend/runtime/native/manager.json"))){
    if(UpdateClient.InstallerRunning(root))return 0;
    if(!ForegroundLauncher(root)&&!UpdateClient.InstallerRunning(root)){await updater.Check();if(updater.ReadyFile!=null&&UpdateSettings.Load(root).autoInstall&&await UpdateInstaller.CanUpdate(root)&&updater.StartInstaller())return 0;}
    for(int second=0;second<60;second++){await Task.Delay(1000);if(UpdateClient.InstallerRunning(root))return 0;}
   }if(!ForegroundLauncher(root)&&updater.ReadyFile!=null&&UpdateSettings.Load(root).autoInstall&&await UpdateInstaller.CanUpdate(root)&&updater.StartInstaller())return 0;return 0;
  }}
 }
}
