using System;using System.Drawing;using System.Windows.Forms;
namespace FssLauncher {
 public sealed class LauncherSessionContext:ApplicationContext {
  readonly LauncherForm launcher;StartupSignIn startup;StartupPreparation preparation;bool transitioning,exiting;readonly bool testMode;
  public bool AuthenticationGateVisible{get{return startup!=null&&startup.Visible;}}public Form SignInWindow{get{return startup;}}
  public LauncherSessionContext(LauncherForm form,bool testing=false){testMode=testing;launcher=form;var handle=launcher.Handle;launcher.IdentityChanged+=Changed;launcher.FormClosed+=delegate{if(!exiting){exiting=true;ExitThread();}};ShowPreparation();}
  void Changed(object sender,EventArgs args){if(exiting||transitioning||(startup!=null&&startup.RequestPending))return;transitioning=true;launcher.BeginInvoke(new Action(delegate{transitioning=false;if(exiting)return;
   if(launcher.Profile!=null){CloseGate();if(!launcher.Visible)launcher.Show();}
   else{launcher.Hide();if(launcher.State==StudioState.Online){if(preparation!=null){preparation.AllowClose=true;preparation.Close();preparation.Dispose();preparation=null;}if(startup==null)ShowGate();}else if(startup==null&&preparation==null)ShowPreparation();}
  }));}
  void CloseGate(){if(startup!=null){startup.AllowClose=true;startup.Close();startup.Dispose();startup=null;}if(preparation!=null){preparation.AllowClose=true;preparation.Close();preparation.Dispose();preparation=null;}}
  void GateClosed(bool allow){if(!allow&&!exiting){exiting=true;launcher.Close();ExitThread();}}
  void ShowPreparation(){preparation=new StartupPreparation(launcher,testMode);var window=preparation;window.FormClosed+=delegate{GateClosed(window.AllowClose);};window.Show();}
  void ShowGate(){startup=new StartupSignIn(launcher,true);if(!testMode){startup.ShowInTaskbar=true;startup.StartPosition=FormStartPosition.CenterScreen;}startup.AuthenticationCompleted+=Changed;var window=startup;window.FormClosed+=delegate{GateClosed(window.AllowClose);};window.Show();}
  protected override void Dispose(bool disposing){if(disposing){launcher.IdentityChanged-=Changed;exiting=true;CloseGate();launcher.Dispose();}base.Dispose(disposing);}
 }
 public sealed class StartupSignIn:AccountDialog {
  readonly LauncherForm launcher;readonly Timer timer=new Timer();readonly Label status=new Label();readonly FlatAction setup=new FlatAction(),start=new FlatAction();public bool AllowClose;bool autoStarted;
  public StartupSignIn(LauncherForm controller,bool testing=false):base(controller,false){launcher=controller;Text="Fortnite Classic — Sign in";Icon=Branding.AppIcon();BackColor=Color.FromArgb(32,32,32);ForeColor=Color.White;ClientSize=new Size(460,570);StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.None;if(testing){ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;Location=new Point(-20000,-20000);autoStarted=true;}
   var settings=new FlatAction{Text="CONNECTION SETTINGS",BackColor=Color.FromArgb(61,65,68)};settings.SetBounds(35,510,390,36);settings.Click+=async delegate{await launcher.ConfigureConnection();};Controls.Add(settings);
   FormClosing+=delegate{if(launcher.Profile!=null&&launcher.CanUseOnline)AllowClose=true;};
   status.SetBounds(35,577,390,42);status.ForeColor=Color.Silver;Controls.Add(status);setup.SetBounds(35,632,190,36);start.SetBounds(235,632,190,36);setup.BackColor=start.BackColor=Color.FromArgb(61,65,68);setup.Text="Install / update Studio";start.Text="Start Server Studio";Controls.Add(setup);Controls.Add(start);setup.Visible=start.Visible=false;status.BringToFront();setup.Click+=async delegate{await launcher.InstallStudio();};start.Click+=delegate{autoStarted=true;launcher.StartStudio();};timer.Interval=500;timer.Tick+=delegate{if(IsDisposed)return;setup.Enabled=launcher.InstallButton.Enabled;start.Enabled=launcher.StudioButton.Enabled&&!launcher.IsInstalling&&!setup.Enabled&&launcher.State!=StudioState.Online&&launcher.State!=StudioState.Starting;if(!testing&&!autoStarted&&!launcher.IsInstalling&&setup.Enabled){autoStarted=true;Prepare();}status.Text=launcher.State==StudioState.Online?"Services online. Sign in to enter the launcher.":"Preparing your local services...\n"+(String.IsNullOrEmpty(launcher.ConnectionMessage)?launcher.State.ToString():launcher.ConnectionMessage);if(!autoStarted&&launcher.State==StudioState.Stopped&&start.Enabled&&!setup.Enabled){autoStarted=true;launcher.StartStudio();}};timer.Start();
  }
  async void Prepare(){try{await launcher.InstallStudio();launcher.StartStudio();}catch(Exception error){status.Text=error.Message;autoStarted=false;}}
  protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
 }
}
