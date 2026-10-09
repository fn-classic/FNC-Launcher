using System;using System.Drawing;using System.Windows.Forms;
namespace FssLauncher {
 public sealed partial class LauncherForm {
  readonly ToolTip headerTips=new ToolTip();
  void InitializeHeader(){
   foreach(var button in new[]{DownloadButton,FriendsButton,SettingsButton}){button.BackColor=Color.FromArgb(44,44,44);button.ForeColor=Color.FromArgb(153,153,153);}
   AccountButton.ForeColor=Color.FromArgb(158,158,158);
   StudioTabButton.Font=FortniteTabButton.Font=new Font("Segoe UI",9,FontStyle.Bold);
   headerTips.SetToolTip(DownloadButton,"Downloads and installation");headerTips.SetToolTip(FriendsButton,"Friends");headerTips.SetToolTip(SettingsButton,"Settings");headerTips.SetToolTip(AccountButton,"Account");
   SettingsButton.Click+=async delegate{await ConfigureConnection();};
   DownloadButton.Click+=delegate{ShowDownloadsMenu();};
  }
  ContextMenuStrip HeaderMenu(){return new ContextMenuStrip{BackColor=Color.FromArgb(38,38,38),ForeColor=Color.WhiteSmoke,ShowImageMargin=false,Font=new Font("Segoe UI",9)};}
  void ShowAccountMenu(){
   var menu=HeaderMenu();menu.Items.Add(profile.displayName).Enabled=false;
   menu.Items.Add(new ToolStripSeparator());var signOut=menu.Items.Add("Sign out");signOut.Click+=async delegate{await SignOut();ShowLogin();};
   menu.Closed+=delegate{menu.Dispose();};menu.Show(AccountButton,new Point(0,AccountButton.Height));
  }
  void ShowDownloadsMenu(){
   var menu=HeaderMenu();menu.Items.Add("Fortnite "+selectedVersion+" — "+LaunchButton.Text).Enabled=false;
   var verify=menu.Items.Add("Verify game files");verify.Enabled=!verifying&&!installing&&!launchPending&&connection.GameProcessId==0;verify.Click+=async delegate{await VerifyGameFiles();};
   var options=menu.Items.Add("Game installation options…");options.Click+=async delegate{await ConfigureConnection();};
   if(!installation.Valid||installation.NeedsUpdate){var install=menu.Items.Add(InstallButton.Text);install.Enabled=!installing;install.Click+=async delegate{await InstallStudio();};}
   menu.Closed+=delegate{menu.Dispose();};menu.Show(DownloadButton,new Point(0,DownloadButton.Height));
  }
 }
}
