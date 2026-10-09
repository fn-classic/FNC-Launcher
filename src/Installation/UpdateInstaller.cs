using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Drawing;

namespace FssLauncher {
 public static class UpdateInstaller {
  static Dictionary<string,object> Descriptor(string root){string file=Path.Combine(root,"Gameserver/Studio/studio-control.json");return File.Exists(file)?new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(file)):null;}
  static async Task<Dictionary<string,object>> Health(string root,Dictionary<string,object> descriptor){return await Task.Run(delegate{
   string challenge=Guid.NewGuid().ToString("N");var request=LocalRequest(descriptor,"/health?challenge="+challenge);
   using(var response=request.GetResponse())using(var reader=new StreamReader(response.GetResponseStream())){
    var result=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(reader.ReadToEnd());
    if(Convert.ToString(result["instance"])!=Convert.ToString(descriptor["instance"])||Convert.ToString(result["challenge"])!=challenge||Convert.ToString(result["protocol"])!="FSS.STUDIO/1"||!Path.GetFullPath(Convert.ToString(result["installation"])).TrimEnd('\\').Equals(Path.GetFullPath(root).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase))throw new IOException("Studio update identity did not verify.");return result;
   }
  });}
  static HttpWebRequest LocalRequest(Dictionary<string,object> descriptor,string path){int port=Convert.ToInt32(descriptor["port"]);if(port<1024||port>65535||Convert.ToString(descriptor["protocol"])!="FSS.STUDIO/1")throw new IOException("Invalid Studio connection.");var request=(HttpWebRequest)WebRequest.Create("http://127.0.0.1:"+port+path);request.Proxy=null;request.Timeout=10000;request.ReadWriteTimeout=10000;return request;}
  public static async Task<bool> CanUpdate(string root){var descriptor=Descriptor(root);if(descriptor==null)return !File.Exists(Path.Combine(root,"Backend/runtime/native/manager.json"));try{var health=await Health(root,descriptor);object game;if(!health.TryGetValue("game",out game)||!(game is Dictionary<string,object>))return false;object allowed;return ((Dictionary<string,object>)game).TryGetValue("updateAllowed",out allowed)&&Convert.ToBoolean(allowed);}catch{return false;}}
  static async Task StopStudio(string root){var descriptor=Descriptor(root);if(descriptor==null){if(File.Exists(Path.Combine(root,"Backend/runtime/native/manager.json")))throw new IOException("The standalone backend is still running. Stop it before updating.");return;}
   await Health(root,descriptor);int pid=Convert.ToInt32(descriptor["pid"]);
   Process studio;try{studio=Process.GetProcessById(pid);}catch(ArgumentException){throw new IOException("Studio changed while updating. Retry the update.");}
   using(studio){if(!Path.GetFullPath(studio.MainModule.FileName).Equals(Path.Combine(root,"Gameserver/Studio/FortniteServerStudio.exe"),StringComparison.OrdinalIgnoreCase))throw new IOException("Studio process identity changed.");
    await Task.Run(delegate{var request=LocalRequest(descriptor,"/studio/update");request.Method="POST";request.ContentType="application/json";request.Headers["X-FSS-Instance"]=Convert.ToString(descriptor["instance"]);request.ContentLength=2;using(var stream=request.GetRequestStream()){stream.WriteByte(123);stream.WriteByte(125);}using(var response=request.GetResponse()){};});
    if(!await Task.Run(delegate{return studio.WaitForExit(60000);}))throw new IOException("Studio did not stop cleanly. No update files were installed.");
   }
  }
  static string LongPath(string path){path=Path.GetFullPath(path);return path.StartsWith(@"\\?\",StringComparison.Ordinal)?path:path.StartsWith(@"\\",StringComparison.Ordinal)?@"\\?\UNC\"+path.Substring(2):@"\\?\"+path;}
  public static string Destination(string root,string relative){string prefix=Path.GetFullPath(root).TrimEnd('\\')+"\\";string path=Path.GetFullPath(Path.Combine(prefix,relative.Replace('/','\\')));if(!path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid update path.");return LongPath(path);}
  public static bool Preserve(string name){return name=="Gameserver/Studio/settings.xml"||name.StartsWith("Backend/runtime/native/tools/openfire/conf/",StringComparison.OrdinalIgnoreCase)||name.StartsWith("Backend/runtime/native/tools/openfire/resources/security/",StringComparison.OrdinalIgnoreCase);}
  static void ReplaceFile(string source,string target){string temp=target+".fnupdate.tmp";try{File.Copy(source,temp,true);using(var file=new FileStream(temp,FileMode.Open,FileAccess.ReadWrite,FileShare.None))file.Flush(true);if(File.Exists(target))File.Replace(temp,target,null);else File.Move(temp,target);}finally{if(File.Exists(temp))File.Delete(temp);}}
  public static void Recover(string root){string work=Path.Combine(root,"Launcher/updates/install"),transaction=Path.Combine(work,"transaction");if(!File.Exists(Path.Combine(transaction,"journal.txt")))return;
   using(var updateLock=new FileStream(Path.Combine(work,"install.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){if(Descriptor(root)!=null||File.Exists(Path.Combine(root,"Backend/runtime/native/manager.json")))throw new IOException("An interrupted update needs recovery. Stop this installation's Studio services and reopen the launcher.");Rollback(root,transaction);}
  }
  public static void Rollback(string root,string transaction){string journal=Path.Combine(transaction,"journal.txt");if(!File.Exists(journal))return;var rows=File.ReadAllLines(journal);for(int i=rows.Length-1;i>=0;i--){string row=rows[i];string target=Destination(root,row.Substring(2));string backup=Destination(Path.Combine(transaction,"backup"),row.Substring(2));if(row[0]=='1'){if(!File.Exists(backup))throw new IOException("Update backup is missing: "+row.Substring(2));Directory.CreateDirectory(Path.GetDirectoryName(target));ReplaceFile(backup,target);}else if(File.Exists(target))File.Delete(target);}File.Delete(journal);}
  public static void ApplyFiles(string root,string stage,string transaction,Action<int,string> progress){
   Directory.CreateDirectory(transaction);string journal=Path.Combine(transaction,"journal.txt");if(File.Exists(journal))Rollback(root,transaction);
   var rows=File.ReadAllLines(Path.Combine(stage,"payload.sha256"));var paths=new List<string>();foreach(var row in rows){if(row.Length<67||row.Substring(64,2)!="  ")throw new InvalidDataException("Invalid update manifest.");string name=row.Substring(66);string input=Destination(stage,name);if(UpdateClient.Hash(input)!=row.Substring(0,64).ToLowerInvariant())throw new InvalidDataException("Invalid staged update: "+name);paths.Add(name);}paths.Add("payload.sha256");
   try{int done=0;foreach(string name in paths){string input=Destination(stage,name),target=Destination(root,name);if(File.Exists(target)&&(Preserve(name)||UpdateClient.Hash(target)==UpdateClient.Hash(input))){done++;continue;}
    bool existed=File.Exists(target);if(existed){string backup=Destination(Path.Combine(transaction,"backup"),name);Directory.CreateDirectory(Path.GetDirectoryName(backup));File.Copy(target,backup,true);}
    using(var log=new FileStream(journal,FileMode.Append,FileAccess.Write,FileShare.Read)){byte[] bytes=System.Text.Encoding.UTF8.GetBytes((existed?"1 ":"0 ")+name+Environment.NewLine);log.Write(bytes,0,bytes.Length);log.Flush(true);}
    Directory.CreateDirectory(Path.GetDirectoryName(target));ReplaceFile(input,target);if(progress!=null)progress(++done*100/paths.Count,"Updating "+name);
   }if(File.Exists(journal))File.Delete(journal);}catch{Rollback(root,transaction);throw;}try{Directory.Delete(LongPath(transaction),true);}catch(IOException){}
  }
  public static async Task Run(string root,int parent,Action<int,string> progress){root=Path.GetFullPath(root).TrimEnd('\\');if(!File.Exists(Path.Combine(root,"payload.sha256")))throw new IOException("Select an existing Fortnite Classic installation.");
   if(parent>0){try{using(var process=Process.GetProcessById(parent)){if(!Path.GetFullPath(process.MainModule.FileName).Equals(Path.Combine(root,"Launcher/FNClassicLauncher.exe"),StringComparison.OrdinalIgnoreCase))throw new IOException("Launcher identity changed.");if(!await Task.Run(delegate{return process.WaitForExit(30000);}))throw new IOException("Close the launcher before installing the update.");}}catch(ArgumentException){}}
   string work=Path.Combine(root,"Launcher/updates/install");Directory.CreateDirectory(work);
   using(var updateLock=new FileStream(Path.Combine(work,"install.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
    string stage=Path.Combine(work,"stage-"+Guid.NewGuid().ToString("N")),transaction=Path.Combine(work,"transaction");
    try{progress(0,"Verifying the complete update…");await Task.Run(delegate{FssSetup.Install.Extract(stage,progress);});
     progress(0,"Waiting for services to stop safely…");await StopStudio(root);
     await Task.Run(delegate{string launcher=Path.Combine(root,"Launcher/FNClassicLauncher.exe");foreach(var process in Process.GetProcessesByName("FNClassicLauncher")){using(process){if(process.Id==Process.GetCurrentProcess().Id)continue;try{if(!Path.GetFullPath(process.MainModule.FileName).Equals(launcher,StringComparison.OrdinalIgnoreCase))continue;}catch(InvalidOperationException){continue;}if(!process.WaitForExit(30000))throw new IOException("Another launcher is still running. Close it before updating.");}}});
     await Task.Run(delegate{ApplyFiles(root,stage,transaction,progress);});
     progress(100,"Update complete. Starting Fortnite Classic…");
    }finally{try{if(Directory.Exists(LongPath(stage)))Directory.Delete(LongPath(stage),true);}catch(IOException error){try{File.WriteAllText(Path.Combine(work,"cleanup-error.txt"),error.Message);}catch{}}}
   }
  }
  public static int Show(string root,int parent){int result=1;using(var form=new Form{Text="Fortnite Classic — Updating",ClientSize=new Size(480,160),StartPosition=FormStartPosition.CenterScreen,BackColor=Color.FromArgb(30,30,30),ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
   var label=new Label{Left=24,Top=24,Width=432,Height=60,Text="Preparing update…"};var bar=new ProgressBar{Left=24,Top=100,Width=432,Height=18};form.Controls.Add(label);form.Controls.Add(bar);bool busy=true;form.FormClosing+=delegate(object s,FormClosingEventArgs e){e.Cancel=busy;};
   form.Shown+=async delegate{try{await Run(root,parent,delegate(int percent,string message){if(!form.IsDisposed)form.BeginInvoke(new Action(delegate{bar.Value=Math.Max(0,Math.Min(100,percent));label.Text=message;}));});result=0;}catch(Exception error){try{Directory.CreateDirectory(Path.Combine(root,"Launcher/updates"));File.WriteAllText(Path.Combine(root,"Launcher/updates/last-error.txt"),error.ToString());}catch{}MessageBox.Show(form,error.Message,"Update could not finish",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{busy=false;form.Close();}};Application.Run(form);
  }return result;}
 }
}
