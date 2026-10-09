using System;using System.IO;using FssLauncher;
[assembly:System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8")]

class UpdateApplyTests {
 static int Main(string[] args){AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);try{string root=Path.GetFullPath(args[0]);string accounts=Path.Combine(root,"Backend/runtime/native/accounts/update-test.json"),settings=Path.Combine(root,"Gameserver/Studio/settings.xml");Directory.CreateDirectory(Path.GetDirectoryName(accounts));File.WriteAllText(accounts,"{\"testAccount\":\"preserve\"}");File.WriteAllText(settings,"<settings><test>preserve</test></settings>");
  UpdateInstaller.Run(root,0,delegate(int progress,string message){}).GetAwaiter().GetResult();int count=0;
  foreach(string row in File.ReadAllLines(Path.Combine(root,"payload.sha256"))){string name=row.Substring(66);if(UpdateInstaller.Preserve(name))continue;string path=UpdateInstaller.Destination(root,name);if(UpdateClient.Hash(path)!=row.Substring(0,64))throw new Exception("Updated file checksum failed: "+name);count++;}
  if(File.ReadAllText(accounts)!="{\"testAccount\":\"preserve\"}"||File.ReadAllText(settings)!="<settings><test>preserve</test></settings>")throw new Exception("Account or settings not preserved");
  Console.WriteLine("PASS: complete previous-release upgrade; "+count+" installed file checksums verified; private account and Studio settings preserved.");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}}
}
