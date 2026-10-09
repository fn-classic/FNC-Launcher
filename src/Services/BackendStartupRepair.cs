using System;using System.IO;using System.Collections.Generic;using System.Linq;using System.Net;using System.Net.NetworkInformation;using System.Diagnostics;using System.Text;using System.Web.Script.Serialization;
namespace FssLauncher {
 public static class BackendStartupRepair {
  public static bool Apply(string root,Func<string[]> addresses=null,Func<bool> running=null){
   string folder=Path.Combine(Path.GetFullPath(root),"Backend/runtime/native"),settingsFile=Path.Combine(folder,"settings.json"),configFile=Path.Combine(folder,"config.json");
   if(!File.Exists(settingsFile)||!File.Exists(configFile))return false;
   if((running??delegate{return ServicesRunning(folder);})())return false;
   var json=new JavaScriptSerializer();var settings=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(settingsFile));var config=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(configFile));
   string beforeSettings=json.Serialize(settings),beforeConfig=json.Serialize(config);object value;bool managed=settings.TryGetValue("managedDatabase",out value)&&Convert.ToBoolean(value);
   if(managed){int port=Convert.ToInt32(settings["databasePort"]);Uri uri;if(port<1024||port>65535||!Uri.TryCreate(Convert.ToString(settings["databaseUrl"]),UriKind.Absolute,out uri)||uri.Scheme!="mongodb")throw new IOException("The saved managed database settings are invalid. Account data was not changed.");var address=new UriBuilder(uri){Host="127.0.0.1",Port=port};settings["databaseUrl"]=address.Uri.AbsoluteUri;config["database_url"]=settings["databaseUrl"];}
   else if(config.TryGetValue("database_url",out value))settings["databaseUrl"]=value;
   var backend=Child(config,"backend");var game=Child(backend,"gameserver");var match=Child(backend,"matchmaking");var chat=Child(config,"openfire");
   var candidates=(addresses??LocalVpnAddresses)().Where(IsVpnAddress).Distinct().ToArray();
   if(candidates.Length==1){string actual=candidates[0];bool hosted=VpnField(game,"address")||VpnField(match,"address")||VpnField(chat,"clientAddress");if(hosted){if(VpnField(game,"address"))game["address"]=actual;if(VpnField(match,"address"))match["address"]=actual;if(VpnField(chat,"clientAddress"))chat["clientAddress"]=actual;config["bind_address"]="0.0.0.0";}}
   string afterSettings=json.Serialize(settings),afterConfig=json.Serialize(config);if(beforeSettings==afterSettings&&beforeConfig==afterConfig)return false;
   string backup=Path.Combine(folder,"startup-repair-backup-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(backup);File.Copy(settingsFile,Path.Combine(backup,"settings.json"));File.Copy(configFile,Path.Combine(backup,"config.json"));
   // Write backend settings first; the supervisor reconciles both again on startup.
   if(beforeSettings!=afterSettings)Write(settingsFile,afterSettings);if(beforeConfig!=afterConfig)Write(configFile,afterConfig);return true;
  }
  static Dictionary<string,object> Child(Dictionary<string,object> parent,string key){object item;if(parent!=null&&parent.TryGetValue(key,out item))return item as Dictionary<string,object>;return null;}
  static bool VpnField(Dictionary<string,object> parent,string key){object value;return parent!=null&&parent.TryGetValue(key,out value)&&IsVpnAddress(Convert.ToString(value));}
  static bool IsVpnAddress(string value){IPAddress address;return IPAddress.TryParse(value,out address)&&address.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork&&address.GetAddressBytes()[0]==26;}
  static string[] LocalVpnAddresses(){return NetworkInterface.GetAllNetworkInterfaces().Where(adapter=>adapter.OperationalStatus==OperationalStatus.Up).SelectMany(adapter=>adapter.GetIPProperties().UnicastAddresses).Select(address=>address.Address.ToString()).Where(IsVpnAddress).ToArray();}
  static bool ServicesRunning(string folder){string file=Path.Combine(folder,"manager.json");if(!File.Exists(file))return false;try{var data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(file));using(var process=Process.GetProcessById(Convert.ToInt32(data["pid"])))return !process.HasExited;}catch(ArgumentException){return false;}catch{return true;}}
  static void Write(string file,string text){string temporary=file+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllText(temporary,text,new UTF8Encoding(false));if(File.Exists(file))File.Replace(temporary,file,null);else File.Move(temporary,file);}
 }
}
