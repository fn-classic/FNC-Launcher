using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using CoreStudio;
namespace FssLauncher {
 public sealed class VersionInstallations {
  readonly string root;
  public string Selected="3.5";
  public readonly Dictionary<string,string> Paths=new Dictionary<string,string>();
  public VersionInstallations(string installation) {
   root=installation; string file=Path.Combine(root,"Launcher/versions.xml");
   if(File.Exists(file)) {
    var data=XElement.Load(file); string selected=(string)data.Attribute("selected");
    foreach(var entry in data.Elements("installation")) {
     string version=(string)entry.Attribute("version");
     if(Array.IndexOf(FortniteBuilds.Supported,version)>=0) Paths[version]=(string)entry.Attribute("executable")??"";
    }
    if(Array.IndexOf(FortniteBuilds.Supported,selected)>=0) Selected=selected;
   } else {
    string legacy=Path.Combine(root,"Launcher/game.xml");
    if(File.Exists(legacy)) {
     var game=XElement.Load(legacy); string version=(string)game.Element("version")??"3.5";
     if(Array.IndexOf(FortniteBuilds.Supported,version)>=0) { Selected=version; Paths[version]=(string)game.Element("executable")??""; }
    }
   }
  }
  public string Executable {get {string path;return Paths.TryGetValue(Selected,out path)?path:"";}}
  public void Register(string version,string path) {
   FortniteBuilds.ValidateExpected(path,version);
   Paths[version]=FortniteBuilds.ResolveExecutable(path);
  }
  public void Save() {
   if(Array.IndexOf(FortniteBuilds.Supported,Selected)<0) throw new InvalidOperationException("Unsupported version.");
   string directory=Path.Combine(root,"Launcher"); Directory.CreateDirectory(directory);
   var data=new XElement("versions",new XAttribute("selected",Selected));
   foreach(string version in FortniteBuilds.Supported) {string path;if(Paths.TryGetValue(version,out path)) data.Add(new XElement("installation",new XAttribute("version",version),new XAttribute("executable",path)));}
   Write(Path.Combine(directory,"versions.xml"),data);
   Write(Path.Combine(directory,"game.xml"),new XElement("game",new XElement("version",Selected),new XElement("executable",Executable)));
  }
  static void Write(string path,XElement data) {
   string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try { data.Save(temporary);if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path); }
   finally {if(File.Exists(temporary))File.Delete(temporary);}
  }
 }
}
