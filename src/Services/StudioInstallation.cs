using System;
using System.IO;
using Microsoft.Win32;
namespace FssLauncher {
 public enum StudioState {NotInstalled,Stopped,Starting,Online,Error}
 public sealed class StudioInstallation {
  public string Root,Executable,Error;public StudioState State;public bool NeedsUpdate;public bool InstallationVerified;
  public const int RequiredPipelineVersion=14;
  public bool Valid {get{return InstallationVerified;}}
  public static string DefaultRoot(){using(var key=Registry.CurrentUser.OpenSubKey(@"Software\FSS")){return (key==null?null:key.GetValue("InstallPath") as string)??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FortniteServerStudio");}}
  public static StudioInstallation Inspect(string root){
   var result=new StudioInstallation{Root=Path.GetFullPath(root),State=StudioState.NotInstalled};
   result.Executable=Path.Combine(result.Root,"Gameserver","Studio","FortniteServerStudio.exe");
   if(!Directory.Exists(result.Root))return result;
   string[] required={"Gameserver/Studio/FortniteServerStudio.exe","Gameserver/Studio/Core.dll","Gameserver/Studio/StudioClientBridge.dll","Backend/Start-TCP.ps1","Backend/Start-LawinV1.ps1","Backend/index.js"};
   foreach(string relative in required){string path=Path.Combine(result.Root,relative.Replace('/',Path.DirectorySeparatorChar));
    if(!File.Exists(path)||new FileInfo(path).Length==0){result.State=StudioState.Error;result.Error="Server Studio installation is incomplete: "+relative;return result;}}
   string integrity;if(!StudioInstaller.Validate(result.Root,out integrity)){result.State=StudioState.Error;result.Error=integrity;return result;}
   int version=0;string versionFile=Path.Combine(result.Root,"Gameserver/Studio/pipeline-version.txt");if(File.Exists(versionFile))Int32.TryParse(File.ReadAllText(versionFile).Trim(),out version);result.NeedsUpdate=version<RequiredPipelineVersion;
   result.InstallationVerified=true;result.State=StudioState.Stopped;return result;
  }
 }
 public sealed class OnlineGate {
  public StudioState Studio=StudioState.NotInstalled;public bool Authenticated;
  public bool Allowed {get{return Studio==StudioState.Online&&Authenticated;}}
  public bool Try(Action action){if(!Allowed)return false;action();return true;}
 }
}
