using System;using System.Drawing;using System.Drawing.Drawing2D;
namespace FssLauncher {
 static class HeaderIcons {
  public static void Draw(Graphics g,string kind,Rectangle bounds,Color color,bool online,string label){
   g.SmoothingMode=SmoothingMode.AntiAlias;
   float cx=bounds.Width/2f,cy=bounds.Height/2f;
   using(var brush=new SolidBrush(color))using(var pen=new Pen(color,2f)){
    if(kind=="account"){
     Person(g,brush,16,cy-2,5);
     using(var status=new SolidBrush(online?Color.FromArgb(117,171,67):Color.FromArgb(94,94,94)))g.FillEllipse(status,19,cy+2,9,9);
     TextRendererDraw(g,label,new Rectangle(35,0,bounds.Width-52,bounds.Height),color);
     g.DrawLines(pen,new[]{new PointF(bounds.Width-15,cy-2),new PointF(bounds.Width-11,cy+2),new PointF(bounds.Width-7,cy-2)});
    }else if(kind=="friends"){
     Person(g,brush,cx-13,cy-1,5);Person(g,brush,cx-3,cy+1,4);
     if(!String.IsNullOrEmpty(label))TextRendererDraw(g,label,new Rectangle(bounds.Width-18,0,18,bounds.Height),color);
    }else if(kind=="download"){
     using(var arrowPen=new Pen(color,4))g.DrawLine(arrowPen,cx,cy-9,cx,cy+2);
     g.FillPolygon(brush,new[]{new PointF(cx-6,cy-1),new PointF(cx+6,cy-1),new PointF(cx,cy+6)});
     g.DrawLines(pen,new[]{new PointF(cx-8,cy+3),new PointF(cx-8,cy+9),new PointF(cx+8,cy+9),new PointF(cx+8,cy+3)});
    }else if(kind=="settings"){
     var points=new PointF[32];for(int n=0;n<32;n++){double angle=n*Math.PI/16;float radius=n%4==0||n%4==3?9:6.7f;points[n]=new PointF(cx+(float)Math.Cos(angle)*radius,cy+(float)Math.Sin(angle)*radius);}
     using(var path=new GraphicsPath(FillMode.Alternate)){path.AddPolygon(points);path.AddEllipse(cx-3,cy-3,6,6);g.FillPath(brush,path);}
    }
   }
  }
  static void Person(Graphics g,Brush brush,float x,float y,float radius){g.FillEllipse(brush,x-radius,y-8-radius,radius*2,radius*2);g.FillEllipse(brush,x-radius-3,y-radius+2,radius*2+6,radius*2+5);}
  static void TextRendererDraw(Graphics g,string text,Rectangle box,Color color){using(var font=new Font("Segoe UI",9,FontStyle.Bold))System.Windows.Forms.TextRenderer.DrawText(g,text,font,box,color,System.Windows.Forms.TextFormatFlags.VerticalCenter|System.Windows.Forms.TextFormatFlags.EndEllipsis|System.Windows.Forms.TextFormatFlags.NoPadding|System.Windows.Forms.TextFormatFlags.SingleLine);}
 }
}
