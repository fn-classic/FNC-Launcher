using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Threading.Tasks;using System.Windows.Forms;
namespace FssLauncher {
 sealed class FriendsPanel:Form {
  readonly Func<Task<FriendSnapshot>> load;readonly Func<string,Task<List<FriendEntry>>> search;readonly Func<string,string,Task> action;
  readonly ListBox rows=new ListBox();readonly TextBox query=new TextBox();readonly Label status=new Label();readonly Timer timer=new Timer();readonly ToolTip tips=new ToolTip();
  readonly FlatAction friendsTab=new FlatAction(),requestsTab=new FlatAction(),find=new FlatAction();
  FriendSnapshot snapshot=new FriendSnapshot();List<FriendEntry> results=new List<FriendEntry>();int section;bool busy,menuOpen;string ownName;
  sealed class Row {public FriendEntry Person;public string Heading;public int Kind;}
  public FriendsPanel(Func<Task<FriendSnapshot>> loadData,Func<string,Task<List<FriendEntry>>> searchData,Func<string,string,Task> execute,string username=""){
   load=loadData;search=searchData;action=execute;ownName=username;Text="Friends";Icon=Branding.AppIcon();ClientSize=new Size(340,510);BackColor=Color.FromArgb(35,35,35);ForeColor=Color.White;Font=new Font("Segoe UI",9);FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;DoubleBuffered=true;
   var title=new Label{Text="FRIENDS",Font=new Font("Segoe UI",11,FontStyle.Bold),AutoSize=true,Location=new Point(18,14)};Controls.Add(title);
   var close=new FlatAction{Text="×",BackColor=BackColor,ForeColor=Color.Silver};close.SetBounds(302,4,32,30);close.Click+=delegate{Close();};Controls.Add(close);tips.SetToolTip(close,"Close friends");
   var self=new Label{Text=String.IsNullOrEmpty(username)?"Your friends":username,ForeColor=Color.WhiteSmoke,Font=new Font("Segoe UI",10,FontStyle.Bold)};self.SetBounds(18,47,270,22);Controls.Add(self);
   var online=new Label{Text="●  Online",ForeColor=Color.FromArgb(135,192,87)};online.SetBounds(18,69,270,18);Controls.Add(online);
   friendsTab.Text="FRIENDS";requestsTab.Text="REQUESTS";foreach(var tab in new[]{friendsTab,requestsTab}){tab.BackColor=BackColor;tab.Font=new Font("Segoe UI",8,FontStyle.Bold);Controls.Add(tab);}friendsTab.SetBounds(8,99,156,33);requestsTab.SetBounds(172,99,156,33);friendsTab.Click+=delegate{section=0;query.Clear();Fill();};requestsTab.Click+=delegate{section=1;query.Clear();Fill();};
   var searchBox=new Panel{BackColor=Color.FromArgb(47,47,47)};searchBox.SetBounds(16,150,308,30);Controls.Add(searchBox);query.BorderStyle=BorderStyle.None;query.BackColor=searchBox.BackColor;query.ForeColor=Color.WhiteSmoke;query.MaxLength=32;query.SetBounds(9,7,254,20);query.AccessibleName="Search or add friends";searchBox.Controls.Add(query);
   var placeholder=new Label{Text="Search or add players",ForeColor=Color.FromArgb(145,145,145),BackColor=searchBox.BackColor};placeholder.SetBounds(9,7,254,20);searchBox.Controls.Add(placeholder);placeholder.Click+=delegate{query.Focus();};query.GotFocus+=delegate{placeholder.Visible=false;};query.LostFocus+=delegate{placeholder.Visible=String.IsNullOrEmpty(query.Text);};query.TextChanged+=delegate{placeholder.Visible=!query.Focused&&String.IsNullOrEmpty(query.Text);};
   find.Text="+";find.Font=new Font("Segoe UI",16);find.BackColor=searchBox.BackColor;find.ForeColor=Color.Silver;find.SetBounds(272,0,36,30);find.Click+=async delegate{await Search();};searchBox.Controls.Add(find);tips.SetToolTip(find,"Search accounts to add a friend");query.KeyDown+=async delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;await Search();}};query.TextChanged+=delegate{if(section==0)Fill();};
   var hint=new Label{Text="Search friends • Enter to add a player",ForeColor=Color.FromArgb(140,140,140),Font=new Font("Segoe UI",8)};hint.SetBounds(18,185,304,18);Controls.Add(hint);
   rows.SetBounds(8,211,324,255);rows.BackColor=BackColor;rows.ForeColor=Color.WhiteSmoke;rows.BorderStyle=BorderStyle.None;rows.DrawMode=DrawMode.OwnerDrawVariable;rows.IntegralHeight=false;rows.AccessibleName="Friends and requests";
   rows.MeasureItem+=delegate(object sender,MeasureItemEventArgs e){e.ItemHeight=e.Index>=0&&e.Index<rows.Items.Count&&((Row)rows.Items[e.Index]).Person==null?28:50;};rows.DrawItem+=DrawRow;rows.MouseUp+=RowClick;rows.DoubleClick+=delegate{OpenActions();};rows.KeyDown+=delegate(object sender,KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Apps){OpenActions();e.Handled=true;}};Controls.Add(rows);
   status.SetBounds(18,478,304,28);status.ForeColor=Color.FromArgb(160,160,160);status.Font=new Font("Segoe UI",8);Controls.Add(status);
   Shown+=async delegate{PositionAtOwner();await RefreshRows();if(!IsDisposed)timer.Start();};timer.Interval=10000;timer.Tick+=async delegate{await RefreshRows();};
   Deactivate+=delegate{if(!menuOpen&&!IsDisposed)BeginInvoke(new Action(delegate{if(!IsDisposed&&!ContainsFocus&&!menuOpen)Close();}));};FormClosed+=delegate{timer.Stop();timer.Dispose();tips.Dispose();};Fill();
  }
  void PositionAtOwner(){if(Owner==null)return;var area=Screen.FromControl(Owner).WorkingArea;int x=Owner.Right-Width-105,y=Owner.Top+LauncherLayout.HeaderHeight;Location=new Point(Math.Max(area.Left,Math.Min(area.Right-Width,x)),Math.Max(area.Top,Math.Min(area.Bottom-Height,y)));}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var pen=new Pen(Color.FromArgb(65,65,65)))e.Graphics.DrawRectangle(pen,0,0,Width-1,Height-1);using(var pen=new Pen(Color.FromArgb(148,196,139),2))e.Graphics.DrawLine(pen,section==1?172:8,133,section==1?328:164,133);}
  void TextLine(Graphics g,string text,Rectangle box,Color color,Font font){TextRenderer.DrawText(g,text??"",font,box,color,TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.VerticalCenter);}
  void DrawRow(object sender,DrawItemEventArgs e){if(e.Index<0)return;var row=(Row)rows.Items[e.Index];bool selected=(e.State&DrawItemState.Selected)!=0;using(var brush=new SolidBrush(selected&&row.Person!=null?Color.FromArgb(57,57,57):BackColor))e.Graphics.FillRectangle(brush,e.Bounds);int y=e.Bounds.Top;
   if(row.Person==null){using(var font=new Font("Segoe UI",8,FontStyle.Bold))TextLine(e.Graphics,row.Heading,new Rectangle(10,y,302,28),Color.FromArgb(145,145,145),font);return;}
   var person=row.Person;var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   using(var background=new SolidBrush(Color.FromArgb(55,55,55)))g.FillRectangle(background,10,y+9,30,30);
   using(var avatar=new SolidBrush(Color.FromArgb(145,145,145))){g.FillEllipse(avatar,20,y+13,10,10);g.FillEllipse(avatar,15,y+23,20,13);}
   using(var dot=new SolidBrush(person.online?Color.FromArgb(128,190,75):Color.FromArgb(106,106,106)))g.FillEllipse(dot,32,y+31,8,8);
   TextLine(g,person.displayName,new Rectangle(52,y+5,225,22),Color.WhiteSmoke,Font);
   string activity=row.Kind==1?"Friend request":row.Kind==2?"Request sent":row.Kind==3?"Add friend":person.online?(person.activity??"Online"):"Offline";
   using(var font=new Font("Segoe UI",8))TextLine(g,activity,new Rectangle(52,y+25,225,18),person.online?Color.FromArgb(137,183,96):Color.FromArgb(149,149,149),font);
   TextLine(g,row.Kind==3?"+":"⋮",new Rectangle(291,y+7,24,36),Color.Silver,Font);
  }
  void Group(string heading,IEnumerable<FriendEntry> people,int kind){var list=people.ToList();if(list.Count==0)return;rows.Items.Add(new Row{Heading=heading+" ("+list.Count+")"});foreach(var person in list)rows.Items.Add(new Row{Person=person,Kind=kind});}
  void Fill(){if(IsDisposed)return;string selected=rows.SelectedItem is Row&&((Row)rows.SelectedItem).Person!=null?((Row)rows.SelectedItem).Person.userId:null;rows.BeginUpdate();rows.Items.Clear();
   requestsTab.Text="REQUESTS"+(snapshot.incoming.Count==0?"":"  "+snapshot.incoming.Count);friendsTab.ForeColor=section==1?Color.Gray:Color.WhiteSmoke;requestsTab.ForeColor=section==1?Color.WhiteSmoke:Color.Gray;
   if(section==1){Group("INCOMING",snapshot.incoming,1);Group("OUTGOING",snapshot.outgoing,2);}else if(section==2){Group("SEARCH RESULTS",results,3);}else{var people=snapshot.friends.Where(p=>String.IsNullOrWhiteSpace(query.Text)||(p.displayName??"").IndexOf(query.Text,StringComparison.OrdinalIgnoreCase)>=0).OrderBy(p=>p.displayName);Group("ONLINE",people.Where(p=>p.online),0);Group("OFFLINE",people.Where(p=>!p.online),0);}
   for(int i=0;i<rows.Items.Count;i++){var row=(Row)rows.Items[i];if(row.Person!=null&&row.Person.userId==selected)rows.SelectedIndex=i;}rows.EndUpdate();status.Text=rows.Items.Count==0?(section==1?"No friend requests.":section==2?"No accounts found.":"Add a friend to get started."):"";Invalidate();}
  void RowClick(object sender,MouseEventArgs e){int index=rows.IndexFromPoint(e.Location);if(index<0)return;rows.SelectedIndex=index;var row=(Row)rows.Items[index];if(row.Person==null)return;tips.SetToolTip(rows,row.Person.displayName+" — "+(row.Person.activity??"Offline")+(String.IsNullOrEmpty(row.Person.avatar)?"":"\nOutfit: "+row.Person.avatar));if(e.Button==MouseButtons.Right||e.X>280)OpenActions();}
  void OpenActions(){var row=rows.SelectedItem as Row;if(row==null||row.Person==null||busy)return;var menu=new ContextMenuStrip{BackColor=Color.FromArgb(42,42,42),ForeColor=Color.WhiteSmoke,ShowImageMargin=false};
   Action<string,string> add=(label,operation)=>{var item=menu.Items.Add(label);item.Click+=async delegate{await Execute(row,operation);};};
   if(row.Kind==1){add("Accept friend request","accept");add("Decline friend request","decline");}else if(row.Kind==2)add("Cancel request","cancel");else if(row.Kind==3)add("Add friend","add");else{add("Invite to party","invite");menu.Items[0].Enabled=snapshot.canInvite&&row.Person.online;menu.Items.Add(new ToolStripSeparator());add("Remove friend","remove");}
   menuOpen=true;menu.Closed+=delegate{menuOpen=false;menu.Dispose();};menu.Show(rows,new Point(Math.Max(8,rows.Width-175),rows.GetItemRectangle(rows.SelectedIndex).Bottom));
  }
  async Task RefreshRows(){if(busy||IsDisposed)return;busy=true;try{var data=await load();if(!IsDisposed){snapshot=data;Fill();}}catch(Exception error){if(!IsDisposed)status.Text=error.Message;}finally{busy=false;}}
  async Task Search(){if(busy||String.IsNullOrWhiteSpace(query.Text))return;busy=true;status.Text="Searching…";try{var data=await search(query.Text);if(!IsDisposed){results=data;section=2;Fill();}}catch(Exception error){if(!IsDisposed)status.Text=error.Message;}finally{busy=false;}}
  async Task Execute(Row row,string operation){if(busy||IsDisposed)return;busy=true;try{await action(operation,row.Person.userId);if(!IsDisposed){section=operation=="add"?1:0;query.Clear();}}catch(Exception error){if(!IsDisposed)status.Text=error.Message;}finally{busy=false;}if(!IsDisposed)await RefreshRows();}
 }
}
