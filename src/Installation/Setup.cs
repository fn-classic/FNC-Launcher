using System;using System.IO;using System.IO.Compression;using System.Reflection;using System.Security.Cryptography;using System.Drawing;using System.Windows.Forms;using System.Threading.Tasks;using System.Diagnostics;using Microsoft.Win32;using System.Collections.Generic;
[assembly:System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8")]
namespace FssSetup {
 static class Program {
  [STAThread] static int Main(string[] args){
   try{if(args.Length==2&&args[0]=="--extract-only"){Install.Extract(Path.GetFullPath(args[1]),null);Console.WriteLine("PASS: payload extracted and every file checksum verified.");return 0;}
    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Wizard());return 0;
   }catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}
  }
 }
 sealed class Wizard:Form {
  TextBox folder=new TextBox();CheckBox migrate=new CheckBox(),launch=new CheckBox();Button install=new Button();ProgressBar progress=new ProgressBar();Label status=new Label();bool busy;
  public Wizard(){Text="Fortnite Classic — Complete Setup";ClientSize=new Size(620,370);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;BackColor=Color.FromArgb(38,38,38);ForeColor=Color.WhiteSmoke;Font=new Font("Segoe UI",9);
   Controls.Add(new Label{Text="Install Fortnite Classic",Font=new Font("Segoe UI",19),AutoSize=true,Location=new Point(24,22)});
   Controls.Add(new Label{Text="Launcher, Server Studio, local accounts/chat, database and bundled runtimes.\nOpen the launcher to sign in. Select your existing Fortnite build.",AutoSize=true,Location=new Point(26,72)});
   folder.SetBounds(26,130,485,25);folder.Text=Install.DefaultDirectory();Controls.Add(folder);var browse=new Button{Text="Browse…",Location=new Point(518,128),Size=new Size(78,28)};Controls.Add(browse);browse.Click+=delegate{using(var d=new FolderBrowserDialog()){d.SelectedPath=folder.Text;if(d.ShowDialog(this)==DialogResult.OK)folder.Text=d.SelectedPath;}};
   migrate.SetBounds(26,174,570,24);migrate.Text="Keep my existing workspace accounts, settings and saved sign-in";migrate.Checked=false;migrate.Visible=false;migrate.Enabled=false;Controls.Add(migrate);
   launch.SetBounds(26,202,570,24);launch.Text="Launch Fortnite Classic when setup finishes";launch.Checked=true;Controls.Add(launch);
   progress.SetBounds(26,242,570,18);Controls.Add(progress);status.SetBounds(26,268,570,42);status.Text="No separate program installation or manual backend startup required.";Controls.Add(status);
   install.Text="Install";install.SetBounds(472,326,124,30);Controls.Add(install);install.Click+=async delegate{if(busy)return;busy=true;install.Enabled=folder.Enabled=migrate.Enabled=launch.Enabled=browse.Enabled=false;string target=Path.GetFullPath(folder.Text);bool keep=migrate.Checked,run=launch.Checked;
    try{await Task.Run(delegate{Install.Extract(target,delegate(int percent,string message){BeginInvoke(new Action(delegate{progress.Value=percent;status.Text=message;}));});Install.Configure(target,keep);});progress.Value=100;status.Text="Installation complete. The launcher manages your local services.";if(run)Process.Start(new ProcessStartInfo(Path.Combine(target,"Launcher/FNClassicLauncher.exe")){WorkingDirectory=target,UseShellExecute=true});busy=false;Close();}
    catch(Exception e){MessageBox.Show(this,e.Message,"Setup could not finish",MessageBoxButtons.OK,MessageBoxIcon.Error);busy=false;install.Enabled=folder.Enabled=launch.Enabled=browse.Enabled=true;migrate.Enabled=Directory.Exists(Install.LegacyRoot);}
   };FormClosing+=delegate(object s,FormClosingEventArgs e){if(busy&&progress.Value<100)e.Cancel=true;};
  }
 }
 static class Install {
  static string FileSystemPath(string path){path=Path.GetFullPath(path);return path.StartsWith(@"\\?\",StringComparison.Ordinal)?path:path.StartsWith(@"\\",StringComparison.Ordinal)?@"\\?\UNC\"+path.Substring(2):@"\\?\"+path;}
  public static string LegacyRoot { get { return Environment.GetEnvironmentVariable("CLASSICFN_LEGACY_ROOT") ?? ""; } }
  public static string DefaultDirectory(){using(var key=Registry.CurrentUser.OpenSubKey(@"Software\FSS")){return (key==null?null:key.GetValue("InstallPath") as string)??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FortniteServerStudio");}}
  public static void Extract(string target,Action<int,string> update){
   if(!Environment.Is64BitOperatingSystem)throw new InvalidOperationException("A 64-bit Windows system is required.");
   target=Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar);if(target.Length<8||target==Path.GetPathRoot(target).TrimEnd('\\'))throw new InvalidOperationException("Choose an application folder.");
   foreach(string name in new[]{"Core.dll","StudioClientBridge.dll","FortniteServerStudio.exe"}){string p=Path.Combine(target,"Gameserver/Studio",name);if(File.Exists(p)){try{using(var check=new FileStream(p,FileMode.Open,FileAccess.ReadWrite,FileShare.None)){};}catch{throw new IOException("Close Studio and stop its games before upgrading.");}}}
   string managerFile=Path.Combine(target,"Backend/runtime/native/manager.json");
   if(File.Exists(managerFile)){
    var match=System.Text.RegularExpressions.Regex.Match(File.ReadAllText(managerFile),"\\\"pid\\\"\\s*:\\s*(\\d+)");
    int pid;if(match.Success&&Int32.TryParse(match.Groups[1].Value,out pid)){
     bool running=false;try{running=!Process.GetProcessById(pid).HasExited;}catch(ArgumentException){}
     if(running)throw new IOException("Close Server Studio and stop this installation's services before updating. No running backend was changed.");
    }
   }
   if(File.Exists(Path.Combine(target,"payload.sha256"))){
    foreach(string script in new[]{"Backend/Start.ps1","Backend/Start-LawinV1.ps1"}){string existing=Path.Combine(target,script);if(!File.Exists(existing))continue;var stop=new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -File \""+existing+"\" -Stop"){UseShellExecute=false,CreateNoWindow=true};using(var p=Process.Start(stop)){p.WaitForExit();if(p.ExitCode!=0)throw new IOException("Stop the installed backend before upgrading.");}}
   }
   Directory.CreateDirectory(target);string prefix=target+Path.DirectorySeparatorChar;var preserved=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   using(var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("FSS.Payload"))using(var zip=new ZipArchive(resource,ZipArchiveMode.Read)){
    int done=0;foreach(var entry in zip.Entries){if(entry.FullName.EndsWith("/"))continue;string path=Path.GetFullPath(Path.Combine(target,entry.FullName.Replace('/',Path.DirectorySeparatorChar)));if(!path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid installer path.");
     Directory.CreateDirectory(FileSystemPath(Path.GetDirectoryName(path)));bool mutable=entry.FullName=="Gameserver/Studio/settings.xml"||entry.FullName.StartsWith("Backend/runtime/native/tools/openfire/conf/")||entry.FullName.StartsWith("Backend/runtime/native/tools/openfire/resources/security/");if(mutable&&File.Exists(FileSystemPath(path))){preserved.Add(entry.FullName);continue;}
     using(var input=entry.Open())using(var output=new FileStream(FileSystemPath(path),FileMode.Create,FileAccess.Write,FileShare.None))input.CopyTo(output);
     if(update!=null)update(Math.Min(96,++done*96/zip.Entries.Count),"Installing "+entry.FullName);
    }
   }
   foreach(string line in File.ReadAllLines(Path.Combine(target,"payload.sha256"))){int split=line.IndexOf("  ");if(split!=64)throw new InvalidDataException("Invalid manifest.");string relative=line.Substring(split+2);if(preserved.Contains(relative))continue;string path=Path.GetFullPath(Path.Combine(target,relative.Replace('/',Path.DirectorySeparatorChar)));if(!path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid checksum path.");using(var sha=SHA256.Create())using(var f=File.OpenRead(FileSystemPath(path))){string hash=BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();if(hash!=line.Substring(0,64))throw new InvalidDataException("Checksum failed: "+relative);}}
   if(update!=null)update(98,"Configuring shortcuts and preserving settings…");
  }
  public static void Configure(string target,bool migrate,bool register=true){
   target=Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
   if(Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.FullName=="FssLauncher.Program"){
    string launcher=Path.Combine(target,"Launcher","FNClassicLauncher.exe");Directory.CreateDirectory(Path.GetDirectoryName(launcher));
    string source=Assembly.GetExecutingAssembly().Location;
    if(!Path.GetFullPath(source).Equals(Path.GetFullPath(launcher),StringComparison.OrdinalIgnoreCase))File.Copy(source,launcher,true);
   }
   string script=Path.Combine(target,"Configure-Install.ps1");var info=new ProcessStartInfo("powershell.exe","-NoProfile -ExecutionPolicy Bypass -File \""+script+"\""+(migrate?" -ImportExisting":"")+(register?"":" -IsolatedInstall")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
   using(var p=Process.Start(info)){var output=p.StandardOutput.ReadToEndAsync();var errors=p.StandardError.ReadToEndAsync();p.WaitForExit();if(p.ExitCode!=0)throw new IOException(output.Result+errors.Result);}
   if(!register)return;
   using(var key=Registry.CurrentUser.CreateSubKey(@"Software\FSS"))key.SetValue("InstallPath",target);
   using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\FortniteServerStudio")){key.SetValue("DisplayName","Fortnite Classic Launcher and Server Studio");key.SetValue("DisplayVersion","2026.10.08");key.SetValue("Publisher","FSS");key.SetValue("InstallLocation",target);key.SetValue("DisplayIcon",Path.Combine(target,"Gameserver/Studio/FortniteServerStudio.exe"));key.SetValue("UninstallString","powershell.exe -NoProfile -ExecutionPolicy Bypass -File \""+Path.Combine(target,"Uninstall.ps1")+"\"");key.SetValue("NoModify",1);key.SetValue("NoRepair",1);}
  }
 }
}
