using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace FssLauncher {
 public sealed class UpdateManifest {
  public int schema; public string version, launcherVersion, backendVersion, gameserverVersion, installerUrl, sha256, notes;
  public long size;
  public void Validate() {
   Version parsed;
   if(schema!=1||!Version.TryParse(version,out parsed)||!Version.TryParse(launcherVersion,out parsed)||!Version.TryParse(backendVersion,out parsed)||!Version.TryParse(gameserverVersion,out parsed))throw new InvalidDataException("Unsupported update release.");
   if(launcherVersion!=version||backendVersion!=version||gameserverVersion!=version)throw new InvalidDataException("This update requires a matched launcher, backend and gameserver release.");
   UpdateClient.SecureUrl(installerUrl);
   if(size<1||size>4L*1024*1024*1024||sha256==null||!Regex.IsMatch(sha256,"^[a-fA-F0-9]{64}$"))throw new InvalidDataException("Invalid update checksum or size.");
  }
 }
 public sealed class UpdateSettings {
  public string feedUrl="https://github.com/fn-classic/FNC-Launcher/releases/latest/download/update.json";
  public bool autoDownload=true, autoInstall=true;
  public static UpdateSettings Load(string root) {
   string path=Path.Combine(root,"Launcher/update-settings.json");
   return File.Exists(path)?new JavaScriptSerializer().Deserialize<UpdateSettings>(File.ReadAllText(path)):new UpdateSettings();
  }
 }
 public sealed class UpdateClient {
  readonly string root; public string Status="Checking for updates…", ReadyFile; public UpdateManifest Available; public int Percent;
  public Action Changed; public bool Busy; public DateTime NextCheck=DateTime.MinValue;
  public UpdateClient(string installation){root=Path.GetFullPath(installation);}
  public static string Quote(string value){return "\""+value.Replace("\"", "\\\"").TrimEnd('\\')+"\"";}
  public static Uri SecureUrl(string value){Uri uri;if(!Uri.TryCreate(value,UriKind.Absolute,out uri)||uri.Scheme!="https"||uri.UserInfo!=""||uri.Fragment!="")throw new InvalidDataException("Updates require an HTTPS address.");return uri;}
  static HttpWebRequest Request(string url){ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var request=(HttpWebRequest)WebRequest.Create(SecureUrl(url));request.UserAgent="FNClassic-Updater/1";request.AllowAutoRedirect=true;request.Timeout=20000;request.ReadWriteTimeout=20000;return request;}
  void Notify(){if(Changed!=null)Changed();}
  public static string Hash(string file){using(var sha=SHA256.Create())using(var stream=File.OpenRead(file))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
  public async Task Check(bool force=false) {
   if(Busy||(!force&&DateTime.UtcNow<NextCheck))return; Busy=true;NextCheck=DateTime.UtcNow.AddMinutes(15);
   try {
    var settings=UpdateSettings.Load(root);
    if(String.IsNullOrWhiteSpace(settings.feedUrl)){Status="Update address is not configured.";return;}
    Status="Checking for updates…";Notify();
    var manifest=await Task.Run(delegate {
     using(var response=(HttpWebResponse)Request(settings.feedUrl).GetResponse())using(var stream=response.GetResponseStream()) {
      SecureUrl(response.ResponseUri.AbsoluteUri); using(var output=new MemoryStream()){var buffer=new byte[4096];int count;while((count=stream.Read(buffer,0,buffer.Length))>0){output.Write(buffer,0,count);if(output.Length>65536)throw new InvalidDataException("Update manifest is too large.");}return new JavaScriptSerializer().Deserialize<UpdateManifest>(System.Text.Encoding.UTF8.GetString(output.ToArray()));}
     }
    });
    manifest.Validate();
    if(new Version(manifest.version)<=Assembly.GetExecutingAssembly().GetName().Version){Available=null;ReadyFile=null;Status="You're up to date.";return;}
    if(Available==null||Available.sha256!=manifest.sha256)ReadyFile=null;
    Available=manifest;Status="Update "+manifest.version+" available";Notify();
    if(settings.autoDownload)await DownloadCore();
   }catch(Exception error){Status="Update check failed: "+error.Message;NextCheck=DateTime.UtcNow.AddMinutes(5);}
   finally{Busy=false;Notify();}
  }
  public async Task Download() {
   if(Busy)return;Busy=true;try{await DownloadCore();}finally{Busy=false;Notify();}
  }
  async Task DownloadCore() {
   if(Available==null)return;var release=Available;release.Validate();
   string cache=Path.Combine(root,"Launcher/updates");Directory.CreateDirectory(cache);string final=Path.Combine(cache,release.sha256.ToLowerInvariant()+".exe");
   Status="Downloading update…";Percent=0;Notify();
   await Task.Run(delegate {
    using(var downloadLock=new FileStream(Path.Combine(cache,"download.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) {
     if(File.Exists(final)&&new FileInfo(final).Length==release.size&&Hash(final).Equals(release.sha256,StringComparison.OrdinalIgnoreCase)){ReadyFile=final;return;}
     string part=final+".partial";
     try{using(var response=(HttpWebResponse)Request(release.installerUrl).GetResponse())using(var input=response.GetResponseStream())using(var output=new FileStream(part,FileMode.Create,FileAccess.Write,FileShare.None)) {
      SecureUrl(response.ResponseUri.AbsoluteUri);if(response.ContentLength>=0&&response.ContentLength!=release.size)throw new InvalidDataException("Update download has an unexpected size.");
      var bytes=new byte[131072];long total=0;int count,last=-1;while((count=input.Read(bytes,0,bytes.Length))>0){total+=count;if(total>release.size)throw new InvalidDataException("Update download exceeds expected size.");output.Write(bytes,0,count);int progress=(int)(total*100/release.size);if(progress!=last){Percent=last=progress;Notify();}}if(total!=release.size)throw new InvalidDataException("Update download was interrupted.");
     }
     if(!Hash(part).Equals(release.sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Update checksum failed. Nothing was installed.");
     if(File.Exists(final))File.Delete(final);File.Move(part,final);ReadyFile=final;
     }finally{if(File.Exists(part))File.Delete(part);}
    }
   });
   Status="Update ready to install";Percent=100;Notify();
  }
  public bool StartInstaller() {
   if(Available==null||ReadyFile==null||!File.Exists(ReadyFile))return false;
   Available.Validate();if(new FileInfo(ReadyFile).Length!=Available.size||!Hash(ReadyFile).Equals(Available.sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Downloaded update changed. Download it again.");
   string cache=Path.Combine(root,"Launcher/updates");using(var launchLock=new FileStream(Path.Combine(cache,"launch.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
    if(InstallerRunning(root))return false;
    var process=Process.Start(new ProcessStartInfo(ReadyFile,"--auto-update "+Quote(root)+" "+Process.GetCurrentProcess().Id){UseShellExecute=false});
    File.WriteAllText(Path.Combine(cache,"installing.json"),new JavaScriptSerializer().Serialize(new{pid=process.Id,executable=ReadyFile}));process.Dispose();return true;
   }
  }
  public static bool InstallerRunning(string root){try{string marker=Path.Combine(root,"Launcher/updates/installing.json");if(!File.Exists(marker))return false;var info=new JavaScriptSerializer().Deserialize<System.Collections.Generic.Dictionary<string,object>>(File.ReadAllText(marker));using(var process=Process.GetProcessById(Convert.ToInt32(info["pid"])))return !process.HasExited&&Path.GetFullPath(process.MainModule.FileName).Equals(Path.GetFullPath(Convert.ToString(info["executable"])),StringComparison.OrdinalIgnoreCase);}catch{return false;}}
 }
}
