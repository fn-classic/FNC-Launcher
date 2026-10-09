using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using FssLauncher;

[assembly:System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8")]

static class UpdateTests {
 static int checks;
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
 static void Reject(Action action,string message){try{action();}catch{checks++;return;}throw new Exception(message);}
 static void Put(string root,string name,string content){string path=UpdateInstaller.Destination(root,name);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,content);}
 static void Manifest(string stage,params string[] paths){var lines=new List<string>();foreach(string name in paths)lines.Add(UpdateClient.Hash(UpdateInstaller.Destination(stage,name))+"  "+name);Put(stage,"payload.sha256",String.Join(Environment.NewLine,lines));}
 static UpdateManifest Release(){return new UpdateManifest{schema=1,version="2026.10.8.2",launcherVersion="2026.10.8.2",backendVersion="2026.10.8.2",gameserverVersion="2026.10.8.2",installerUrl="https://github.com/fn-classic/FNC-Launcher/releases/download/test/setup.exe",sha256=new string('a',64),size=123};}
 static async Task<bool> Probe(string root,bool allowed,bool wrongIdentity){
  var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();int port=((IPEndPoint)listener.LocalEndpoint).Port;
  Put(root,"Gameserver/Studio/studio-control.json",new JavaScriptSerializer().Serialize(new{protocol="FSS.STUDIO/1",instance="test-instance",port=port,pid=123}));
  var serve=Task.Run(async delegate{using(var client=await listener.AcceptTcpClientAsync())using(var stream=client.GetStream()){
   var text=new StringBuilder();while(!text.ToString().EndsWith("\r\n\r\n")){int b=stream.ReadByte();if(b<0)throw new IOException();text.Append((char)b);}
   string challenge=text.ToString().Split('?')[1].Split(' ')[0].Substring("challenge=".Length);
   string body=new JavaScriptSerializer().Serialize(new{protocol="FSS.STUDIO/1",instance=wrongIdentity?"wrong-instance":"test-instance",challenge=challenge,installation=root,game=new{updateAllowed=allowed}});
   byte[] payload=Encoding.UTF8.GetBytes(body),header=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: "+payload.Length+"\r\nConnection: close\r\n\r\n");stream.Write(header,0,header.Length);stream.Write(payload,0,payload.Length);
  }});
  try{bool result=await UpdateInstaller.CanUpdate(root);await serve;return result;}finally{listener.Stop();File.Delete(Path.Combine(root,"Gameserver/Studio/studio-control.json"));}
 }
 static int Main(){AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);string temp=Path.Combine(Path.GetTempPath(),"ClassicFN-UpdateTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);try{
  Release().Validate();checks++;
  Reject(delegate{var release=Release();release.installerUrl="http://example.com/setup.exe";release.Validate();},"HTTP update accepted");
  Reject(delegate{var release=Release();release.sha256="not a checksum";release.Validate();},"Invalid checksum accepted");
  Reject(delegate{var release=Release();release.gameserverVersion="2026.10.8.0";release.Validate();},"Mixed component release accepted");
  Reject(delegate{var release=Release();release.size=5L*1024*1024*1024;release.Validate();},"Oversized release accepted");
  Reject(delegate{UpdateInstaller.Destination(temp,"../escape.exe");},"Update path escaped installation");
  string root=Path.Combine(temp,"root"),stage=Path.Combine(temp,"stage"),transaction=Path.Combine(temp,"transaction");Directory.CreateDirectory(root);
  Put(root,"Launcher/FNClassicLauncher.exe","old launcher");Put(root,"Gameserver/Studio/settings.xml","personal settings");Put(root,"Backend/runtime/native/accounts/user.json","private account");Put(stage,"Launcher/FNClassicLauncher.exe","new launcher");Put(stage,"Gameserver/Studio/Core.dll","new core");Put(stage,"Backend/index.js","new backend");Put(stage,"Gameserver/Studio/settings.xml","defaults");Manifest(stage,"Launcher/FNClassicLauncher.exe","Gameserver/Studio/Core.dll","Backend/index.js","Gameserver/Studio/settings.xml");
  UpdateInstaller.ApplyFiles(root,stage,transaction,null);
  Check(File.ReadAllText(Path.Combine(root,"Launcher/FNClassicLauncher.exe"))=="new launcher","Launcher not updated");
  Check(File.ReadAllText(Path.Combine(root,"Gameserver/Studio/Core.dll"))=="new core","Core not updated");
  Check(File.ReadAllText(Path.Combine(root,"Backend/index.js"))=="new backend","Backend not updated");
  Check(File.ReadAllText(Path.Combine(root,"Gameserver/Studio/settings.xml"))=="personal settings","Studio settings overwritten");
  Check(File.ReadAllText(Path.Combine(root,"Backend/runtime/native/accounts/user.json"))=="private account","Private account overwritten");
  Put(stage,"Launcher/FNClassicLauncher.exe","next launcher");Put(stage,"blocked.dll","cannot install");Manifest(stage,"Launcher/FNClassicLauncher.exe","blocked.dll");Directory.CreateDirectory(Path.Combine(root,"blocked.dll"));
  Reject(delegate{UpdateInstaller.ApplyFiles(root,stage,transaction,null);},"Write failure not detected");
  Check(File.ReadAllText(Path.Combine(root,"Launcher/FNClassicLauncher.exe"))=="new launcher","Rollback did not restore previous launcher");
  Check(!File.Exists(Path.Combine(transaction,"journal.txt")),"Rollback journal not cleared");
  string longName="Backend/node_modules/"+new string('x',80)+"/node_modules/"+new string('y',80)+"/node_modules/"+new string('z',80)+"/common.d.ts";Put(stage,longName,"long path dependency");Manifest(stage,longName);UpdateInstaller.ApplyFiles(root,stage,transaction,null);Check(File.ReadAllText(UpdateInstaller.Destination(root,longName))=="long path dependency","Long-path dependency update failed");
  Manifest(stage,"Launcher/FNClassicLauncher.exe","blocked.dll");
  Put(stage,"blocked.dll","tampered after manifest");Reject(delegate{UpdateInstaller.ApplyFiles(root,stage,transaction,null);},"Corrupt payload accepted");
  Check(File.ReadAllText(Path.Combine(root,"Launcher/FNClassicLauncher.exe"))=="new launcher","Corrupt payload changed installation");
  string work=Path.Combine(root,"Launcher/updates/install/transaction");Put(work,"backup/Launcher/FNClassicLauncher.exe","recovered launcher");Put(work,"journal.txt","1 Launcher/FNClassicLauncher.exe\n0 Backend/new.js\n");Put(root,"Backend/new.js","partial");UpdateInstaller.Recover(root);
  Check(File.ReadAllText(Path.Combine(root,"Launcher/FNClassicLauncher.exe"))=="recovered launcher","Interrupted update not restored");Check(!File.Exists(Path.Combine(root,"Backend/new.js")),"Partial new file retained");
  Check(Probe(root,true,false).GetAwaiter().GetResult(),"Idle Studio rejected");Check(!Probe(root,false,false).GetAwaiter().GetResult(),"Active game accepted update");Check(!Probe(root,true,true).GetAwaiter().GetResult(),"Wrong Studio identity accepted");
  Put(root,"Backend/runtime/native/manager.json","{}");Check(!UpdateInstaller.CanUpdate(root).GetAwaiter().GetResult(),"Standalone backend interrupted");
  Console.WriteLine("PASS: "+checks+" checks — release validation, all component files, account/config preservation, rollback, interrupted-update recovery and Studio idle/identity gates.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{Directory.Delete(@"\\?\"+temp,true);}}
}
