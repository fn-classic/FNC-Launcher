using System;using System.Drawing;using System.Windows.Forms;
namespace FssLauncher {
 public sealed class StartupPreparation:Form {
  readonly LauncherForm launcher;readonly Timer timer=new Timer();readonly Label message=new Label();readonly ProgressBar progress=new ProgressBar();readonly FlatAction retry=new FlatAction();bool busy;DateTime began;public bool AllowClose;
  public StartupPreparation(LauncherForm controller,bool testing=false){launcher=controller;Text="Fortnite Classic — Preparing services";Icon=Branding.AppIcon();ClientSize=new Size(460,380);FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(32,32,32);ForeColor=Color.White;Font=new Font("Segoe UI",10);
   var logo=new PictureBox{Image=Branding.Logo,SizeMode=PictureBoxSizeMode.Zoom};logo.SetBounds(36,20,55,65);Controls.Add(logo);
   var title=new Label{Text="GETTING READY",Font=new Font("Segoe UI",16,FontStyle.Bold)};title.SetBounds(36,100,388,35);Controls.Add(title);
   message.Text="Preparing your local services...";message.SetBounds(36,151,388,66);Controls.Add(message);progress.SetBounds(36,232,388,8);Controls.Add(progress);
   retry.Text="Retry";retry.SetBounds(36,265,388,36);retry.Visible=false;retry.Click+=async delegate{await Prepare();};Controls.Add(retry);
   var settings=new FlatAction{Text="CONNECTION SETTINGS",BackColor=Color.FromArgb(61,65,68)};settings.SetBounds(36,318,388,36);settings.Click+=async delegate{await launcher.ConfigureConnection();began=DateTime.UtcNow;retry.Visible=false;timer.Start();};Controls.Add(settings);
   var close=new FlatAction{Text="×",BackColor=BackColor};close.SetBounds(420,5,35,30);close.Click+=delegate{Close();};Controls.Add(close);
   timer.Interval=500;timer.Tick+=delegate{progress.Value=launcher.PreparationProgress;if(!retry.Visible)message.Text=launcher.State==StudioState.Online?"Ready. Opening sign-in...":launcher.IsInstalling?"Installing local services...":"Starting Studio and preparing account, chat and game services..."+(String.IsNullOrWhiteSpace(launcher.ConnectionMessage)?"":"\n"+launcher.ConnectionMessage);if(!busy&&launcher.State!=StudioState.Online&&DateTime.UtcNow-began>TimeSpan.FromMinutes(5)){message.Text="Services could not start. "+launcher.ConnectionMessage;retry.Visible=true;timer.Stop();}};
   if(testing){ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Location=new Point(-20000,-20000);began=DateTime.UtcNow;timer.Start();}else Shown+=async delegate{await Prepare();};
  }
  async System.Threading.Tasks.Task Prepare(){if(busy)return;busy=true;retry.Visible=false;began=DateTime.UtcNow;timer.Start();try{await launcher.RefreshConnection();if(launcher.State!=StudioState.Online){if(launcher.InstallButton.Enabled)await launcher.InstallStudio();if(launcher.InstallButton.Enabled)throw new InvalidOperationException(launcher.ConnectionMessage);launcher.StartStudio();}}catch(Exception error){message.Text=error.Message;retry.Visible=true;timer.Stop();}finally{busy=false;}}
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
 }
}
