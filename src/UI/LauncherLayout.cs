using System;using System.Drawing;
namespace FssLauncher {
 sealed class LauncherLayout {
  public const int HeaderHeight=55,FooterHeight=25,Inset=36;
  public readonly float Scale;public readonly int Padding;
  public readonly Rectangle Hero,Sidebar,Launch,Notice,Mode,ServerStatus,Footer;
  public readonly Rectangle[] Cards=new Rectangle[3];
  public LauncherLayout(Size size){
   Scale=size.Width/1789f;
   int sidebar=Math.Max(size.Width>=1408?471:330,(int)Math.Round(487*Scale));
   Padding=Math.Max(24,(sidebar-401)/2);
   int heroHeight=Math.Min((int)Math.Round(600*Scale),size.Height-HeaderHeight-FooterHeight-(int)Math.Round(285*Scale));
   heroHeight=Math.Max(330,heroHeight);
   Sidebar=new Rectangle(0,HeaderHeight,sidebar,heroHeight);
   Hero=new Rectangle(sidebar,HeaderHeight,Math.Max(1,size.Width-sidebar),heroHeight);
   int launchHeight=size.Width>=1408?90:Math.Max(60,(int)Math.Round(90*size.Width/1408f));
   Launch=new Rectangle(Padding,Sidebar.Top+(int)Math.Round(heroHeight*373f/600),Math.Min(401,sidebar-Padding*2),launchHeight);
   ServerStatus=new Rectangle(Padding,Launch.Top-(int)Math.Round(36*Scale),Launch.Width,24);
   Mode=new Rectangle(Padding,Launch.Bottom+(int)Math.Round(34*Scale),Launch.Width,18);
   Notice=new Rectangle(Padding,Sidebar.Bottom-30,Launch.Width,28);
   Footer=new Rectangle(0,size.Height-FooterHeight,size.Width,FooterHeight);
   int gap=(int)Math.Round(42*Scale),width=(int)Math.Round(389*Scale);
   int height=Math.Min((int)Math.Round(242*Scale),Footer.Top-Sidebar.Bottom-40);
   int left=(size.Width-width*3-gap*2)/2,top=Sidebar.Bottom+(int)Math.Round(27*Scale);
   for(int i=0;i<3;i++)Cards[i]=new Rectangle(left+i*(width+gap),top,width,Math.Max(1,height));
  }
 }
}
