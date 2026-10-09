using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
namespace FssLauncher {
 sealed class VersionTheme {
  public string title {get;set;}
  public string artwork {get;set;}
  public bool Historical {get{return artwork=="2017";}}
  public static VersionTheme Load(string version) {
   using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("FSS.Theme."+version))
   using(var reader=new StreamReader(stream))
    return new JavaScriptSerializer().Deserialize<VersionTheme>(reader.ReadToEnd());
  }
 }
}
