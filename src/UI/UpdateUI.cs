using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace FssLauncher {
 public sealed partial class LauncherForm {
  UpdateClient updater; bool updateInstalling;DateTime nextInstallAttempt=DateTime.MinValue;
  void InitializeUpdates(){
   if(preview)return;updater=new UpdateClient(studioRoot);
   updater.Changed=delegate{if(IsDisposed||!IsHandleCreated)return;BeginInvoke(new Action(delegate{
    DownloadButton.ForeColor=updater.Available==null?Color.FromArgb(153,153,153):Color.FromArgb(148,196,139);
    DownloadButton.Online=updater.Available!=null;
    headerTips.SetToolTip(DownloadButton,updater.Status+(updater.Busy&&updater.Available!=null?" "+updater.Percent+"%":""));DownloadButton.Invalidate();
   }));};
   Shown+=async delegate{await RefreshUpdates();};
   timer.Tick+=async delegate{await RefreshUpdates();};
  }
  async Task RefreshUpdates(){
   if(updater==null||installing||updateInstalling||updater.Busy)return;
   await updater.Check();
   try{if(updater.ReadyFile!=null&&DateTime.UtcNow>=nextInstallAttempt&&UpdateSettings.Load(studioRoot).autoInstall&&connection.GameProcessId==0&&!launchPending&&!verifying&&!authPending&&profile!=null){nextInstallAttempt=DateTime.UtcNow.AddSeconds(30);await InstallAvailableUpdate();}}catch(Exception error){notice="Update settings: "+error.Message;Invalidate();}
  }
  async Task InstallAvailableUpdate(){
   if(updateInstalling||updater==null||updater.ReadyFile==null)return;
   if(connection.GameProcessId!=0||launchPending||installing||verifying){notice="Update downloaded. It will install after you close Fortnite.";Invalidate();return;}
   updateInstalling=true;
   try{if(!await UpdateInstaller.CanUpdate(studioRoot)){notice="Update downloaded. Waiting for hosted players and games to finish.";Invalidate();return;}if(await Task.Run(delegate{return updater.StartInstaller();})){timer.Stop();Close();}}
   catch(Exception error){notice="Update could not start: "+error.Message;Invalidate();}
   finally{updateInstalling=false;}
  }
  void AddUpdateMenu(ContextMenuStrip menu){
   if(updater==null)return;menu.Items.Add(new ToolStripSeparator());menu.Items.Add(updater.Status).Enabled=false;
   if(updater.Busy&&updater.Available!=null)menu.Items.Add("Downloading — "+updater.Percent+"%").Enabled=false;
   var check=menu.Items.Add("Check for updates");check.Enabled=!updater.Busy;check.Click+=async delegate{await updater.Check(true);};
   if(updater.Available!=null&&!updater.Busy){var action=menu.Items.Add(updater.ReadyFile==null?"Download update":"Install update");action.Click+=async delegate{if(updater.ReadyFile==null){try{await updater.Download();}catch(Exception error){MessageBox.Show(this,error.Message,"Update",MessageBoxButtons.OK,MessageBoxIcon.Error);}}else await InstallAvailableUpdate();};}
  }
 }
}
