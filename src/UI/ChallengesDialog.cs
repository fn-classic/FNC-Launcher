using System;using System.Collections;using System.Collections.Generic;using System.Drawing;using System.Windows.Forms;
namespace FssLauncher {
 public sealed class ChallengesDialog:Form {
  public ChallengesDialog(Dictionary<string,object> data){Text="Challenges";Icon=Branding.AppIcon();ClientSize=new Size(520,570);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;BackColor=Color.FromArgb(32,32,32);ForeColor=Color.White;Font=new Font("Segoe UI",10);
   var title=new Label{Text="CHALLENGES",Font=new Font("Segoe UI",18,FontStyle.Bold)};title.SetBounds(28,24,464,38);Controls.Add(title);
   var reset=DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(data["dailyResetAt"])).UtcDateTime;
   var subtitle=new Label{Text="Daily reset: "+reset.ToString("MMM d, HH:mm")+" UTC",ForeColor=Color.Silver};subtitle.SetBounds(28,68,464,24);Controls.Add(subtitle);
   var filter=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(43,47,49),ForeColor=Color.White};filter.Items.AddRange(new object[]{"Daily / milestones","Battle Pass"});filter.SetBounds(288,30,204,28);Controls.Add(filter);
   var list=new Panel{AutoScroll=true};list.SetBounds(0,108,520,462);Controls.Add(list);
   Action refresh=delegate{while(list.Controls.Count>0)list.Controls[0].Dispose();list.AutoScrollPosition=Point.Empty;int y=0;foreach(Dictionary<string,object> item in (IEnumerable)data["challenges"]){object flag;bool pass=item.TryGetValue("battlePass",out flag)&&Convert.ToBoolean(flag);if(pass!=(filter.SelectedIndex==1))continue;bool locked=item.TryGetValue("locked",out flag)&&Convert.ToBoolean(flag);bool done=Convert.ToBoolean(item["complete"]);int count=Convert.ToInt32(item["count"]),target=Convert.ToInt32(item["target"]);var panel=new Panel{BackColor=Color.FromArgb(43,47,49)};panel.SetBounds(28,y,450,108);y+=120;
    var caption=new Label{Text=(pass?"BATTLE PASS"+(item.TryGetValue("week",out flag)&&flag!=null?" · WEEK "+flag:"")+"  ·  ":Convert.ToBoolean(item["daily"])?"DAILY  ·  ":"SEASON 3  ·  ")+item["title"],Font=new Font("Segoe UI",10,FontStyle.Bold)};caption.SetBounds(14,10,422,40);
    var detail=new Label{Text=(locked?"LOCKED — PASS / TIER REQUIRED":done?"COMPLETE — REWARD GRANTED":count+" / "+target)+"   |   "+item["xp"]+" XP + "+item["stars"]+" Battle Stars",ForeColor=done?Color.FromArgb(148,196,139):Color.Silver};detail.SetBounds(14,56,422,33);
    var track=new Panel{BackColor=Color.FromArgb(63,67,69)};track.SetBounds(14,93,422,5);var fill=new Panel{BackColor=Color.FromArgb(148,196,139),Width=(int)(422*(count/(double)target)),Height=5};track.Controls.Add(fill);panel.Controls.AddRange(new Control[]{caption,detail,track});list.Controls.Add(panel);
   }};filter.SelectedIndexChanged+=delegate{refresh();};filter.SelectedIndex=0;
  }
 }
}
