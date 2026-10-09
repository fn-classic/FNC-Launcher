using System;using System.Drawing;using System.Windows.Forms;using CoreStudio;
namespace FssLauncher {
 public sealed class MatchSettingsDialog:Form {
  bool savePending;public bool VerifyRequested{get;private set;}
  public MatchSettingsDialog(string root,bool gameRunning=false){Text="Game options";Icon=Branding.AppIcon();ClientSize=new Size(420,635);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(32,32,32);ForeColor=Color.White;Font=new Font("Segoe UI",10);
   var versions=new VersionInstallations(root);
   var versionLabel=new Label{Text="FORTNITE VERSION"};versionLabel.SetBounds(28,28,364,25);
   var version=new ComboBox{Name="gameVersion",DropDownStyle=ComboBoxStyle.DropDownList,Enabled=!gameRunning,BackColor=Color.FromArgb(49,49,49),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};version.Items.AddRange(FortniteBuilds.Supported);version.SelectedItem=versions.Selected;version.SetBounds(28,58,364,30);
   var locate=new FlatAction{Text="LOCATE INSTALLATION",Enabled=!gameRunning};locate.SetBounds(28,98,364,34);
   var pathLabel=new Label{AutoEllipsis=true,ForeColor=Color.Silver};pathLabel.SetBounds(28,140,364,32);
   Action updatePath=delegate{string path;pathLabel.Text=versions.Paths.TryGetValue(Convert.ToString(version.SelectedItem),out path)?path:"No installation selected. Locate this build before launching.";};version.SelectedIndexChanged+=delegate{updatePath();};updatePath();
   locate.Click+=async delegate{using(var browse=new OpenFileDialog{Title="Locate Fortnite "+version.SelectedItem,Filter="Fortnite client|FortniteClient-Win64-Shipping*.exe"}){if(browse.ShowDialog(this)!=DialogResult.OK)return;locate.Enabled=false;savePending=true;try{string selected=Convert.ToString(version.SelectedItem);await System.Threading.Tasks.Task.Run(delegate{versions.Register(selected,browse.FileName);});updatePath();}catch(Exception error){MessageBox.Show(this,error.Message,"Fortnite installation",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{savePending=false;locate.Enabled=!gameRunning;}}};
   Controls.AddRange(new Control[]{versionLabel,version,locate,pathLabel});
   LauncherMatchSettings settings;try{settings=LauncherMatchSettings.Load(root);}catch(Exception){settings=new LauncherMatchSettings();}
   var bots=new NumericUpDown{Minimum=0,Maximum=85,Value=settings.Bots,BackColor=Color.FromArgb(49,49,49),ForeColor=Color.White};bots.SetBounds(28,215,364,30);
   var playlist=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Color.FromArgb(49,49,49),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};playlist.Items.AddRange(new object[]{"Solo","Duos","Squads"});playlist.SelectedIndex=settings.Playlist=="Playlist_DefaultDuo"?1:settings.Playlist=="Playlist_DefaultSquad"?2:0;playlist.SetBounds(28,285,364,30);
   var botLabel=new Label{Text="BOT OPPONENTS (0–85)"};botLabel.SetBounds(28,183,364,25);var playlistLabel=new Label{Text="PLAYLIST"};playlistLabel.SetBounds(28,255,364,25);
   var backendLabel=new Label{Text="BACKEND IP / ADDRESS"};backendLabel.SetBounds(28,331,364,25);var backend=new TextBox{Name="backendAddress",Text=settings.BackendUrl,Enabled=!gameRunning,BackColor=Color.FromArgb(49,49,49),ForeColor=Color.White};backend.SetBounds(28,361,364,30);
   var hint=new Label{Text=(gameRunning?"Close Fortnite to change backend. ":"")+"Leave blank for local play, or enter your friend's address (e.g. 26.10.20.30:8080). Changing backend requires signing in there. Bot and playlist settings apply to your own next match; your friend controls their server.",ForeColor=Color.Silver};hint.SetBounds(28,408,364,100);
   var save=new FlatAction{Text="SAVE SETTINGS"};save.SetBounds(28,525,364,40);save.Click+=delegate{if(savePending)return;try{var next=new LauncherMatchSettings{Bots=(int)bots.Value,Playlist=new[]{"Playlist_DefaultSolo","Playlist_DefaultDuo","Playlist_DefaultSquad"}[playlist.SelectedIndex],BackendUrl=backend.Text};next.Validate();if(!gameRunning){versions.Selected=Convert.ToString(version.SelectedItem);versions.Save();}next.Save(root);DialogResult=DialogResult.OK;Close();}catch(Exception error){MessageBox.Show(this,error.Message,"Match settings",MessageBoxButtons.OK,MessageBoxIcon.Error);}};
   var verify=new FlatAction{Text="VERIFY GAME FILES",BackColor=Color.FromArgb(61,65,68),Enabled=!gameRunning};verify.SetBounds(28,580,364,34);verify.Click+=delegate{if(savePending)return;VerifyRequested=true;save.PerformClick();if(DialogResult!=DialogResult.OK)VerifyRequested=false;};Controls.Add(verify);
   Controls.AddRange(new Control[]{botLabel,bots,playlistLabel,playlist,backendLabel,backend,hint,save});AcceptButton=save;
  }
 }
}
