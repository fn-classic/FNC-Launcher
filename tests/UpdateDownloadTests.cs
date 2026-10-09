using System;using System.IO;using System.Net;using System.Security.Cryptography.X509Certificates;using FssLauncher;
[assembly:System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8")]

class UpdateDownloadTests {
 static int Main(string[] args){string root=Path.Combine(Path.GetTempPath(),"ClassicFN-DownloadTest-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path.Combine(root,"Launcher"));try{
  // Pin only the ephemeral local fixture certificate in this test process.
  // Production updater certificate validation is never changed.
  ServicePointManager.ServerCertificateValidationCallback=(sender,certificate,chain,errors)=>certificate.GetCertHashString()==args[1];
  File.WriteAllText(Path.Combine(root,"Launcher/update-settings.json"),"{\"feedUrl\":\""+args[0]+"/manifest\",\"autoDownload\":true,\"autoInstall\":false}");
  var updater=new UpdateClient(root);updater.Check(true).GetAwaiter().GetResult();if(updater.ReadyFile==null||!File.Exists(updater.ReadyFile)||updater.Percent!=100)throw new Exception("Valid HTTPS update was not downloaded: "+updater.Status);Console.WriteLine("PASS: real HTTPS manifest check and background download with exact checksum and size");
  string good=updater.ReadyFile;updater.Available.sha256=new string('b',64);updater.Available.installerUrl=args[0]+"/corrupt";bool rejected=false;try{updater.Download().GetAwaiter().GetResult();}catch{rejected=true;}if(!rejected||File.Exists(Path.Combine(root,"Launcher/updates",new string('b',64)+".exe")))throw new Exception("Corrupt download was accepted");Console.WriteLine("PASS: corrupt HTTPS download rejected without installing");
  updater.Available.sha256=new string('c',64);updater.Available.installerUrl=args[0]+"/truncated";rejected=false;try{updater.Download().GetAwaiter().GetResult();}catch{rejected=true;}if(!rejected||Directory.GetFiles(Path.Combine(root,"Launcher/updates"),"*.partial").Length!=0)throw new Exception("Partial download was accepted or retained");Console.WriteLine("PASS: interrupted download rejected and incomplete file removed");
  if(!File.Exists(good))throw new Exception("Previous verified download was deleted");
  return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{ServicePointManager.ServerCertificateValidationCallback=null;Directory.Delete(root,true);}}
}
