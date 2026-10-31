using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

static class Theme {
    public static readonly Color Bg=Color.FromArgb(244,247,246), Ink=Color.FromArgb(26,46,42), Muted=Color.FromArgb(112,127,123), Green=Color.FromArgb(22,119,101), Line=Color.FromArgb(225,232,228), Soft=Color.FromArgb(230,244,238);
    public static GraphicsPath Round(RectangleF r,float radius) {
        var p=new GraphicsPath();float d=radius*2;
        p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    public static Label Label(string text,int x,int y,int width,int height,float size,Color color,bool bold) {return new Label {Text=text,Location=new Point(x,y),Size=new Size(width,height),Font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular),ForeColor=color,BackColor=Color.Transparent,AutoEllipsis=true};}
}
sealed class Card : Panel {
    public Card(){DoubleBuffered=true;BackColor=Color.White;}
    protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?Theme.Bg:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;float scale=e.Graphics.DpiX/96f;using(var p=Theme.Round(new RectangleF(.5f,.5f,Width-1,Height-1),16*scale))using(var b=new SolidBrush(Color.White))using(var pen=new Pen(Theme.Line)){e.Graphics.FillPath(b,p);e.Graphics.DrawPath(pen,p);}}
}
sealed class ActionButton : Button {
    public bool Primary,Quiet; bool hover;
    public ActionButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;DoubleBuffered=true;BackColor=Color.White;ForeColor=Theme.Ink;Font=new Font("Microsoft YaHei UI",10);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(Parent==null?Theme.Bg:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        Color fill=Primary?(hover?Color.FromArgb(16,100,84):Theme.Green):(hover?Theme.Soft:Color.White);
        if(!Enabled)fill=Theme.Line;if(Quiet&&!hover&&!Primary)fill=Parent.BackColor;
        using(var p=Theme.Round(new RectangleF(1,1,Width-2,Height-2),10*e.Graphics.DpiX/96f))using(var b=new SolidBrush(fill))using(var pen=new Pen(Primary||Quiet?fill:Theme.Line)){e.Graphics.FillPath(b,p);e.Graphics.DrawPath(pen,p);}
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,!Enabled?Theme.Muted:Primary?Color.White:ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        if(Focused&&ShowFocusCues){var r=ClientRectangle;r.Inflate(-6,-6);ControlPaint.DrawFocusRectangle(e.Graphics,r);}
    }
}
sealed class PreviewBox : Control {
    public Image Preview;
    public PreviewBox(){DoubleBuffered=true;BackColor=Color.FromArgb(243,247,245);AccessibleName="最近截图预览";}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        if(Preview==null){
            int cx=Width/2,cy=Height/2;float s=e.Graphics.DpiX/96f;
            using(var p=new Pen(Color.FromArgb(164,193,179),2*s)){e.Graphics.DrawRectangle(p,cx-21*s,cy-32*s,42*s,32*s);e.Graphics.DrawLine(p,cx-14*s,cy-8*s,cx-2*s,cy-20*s);e.Graphics.DrawLine(p,cx-2*s,cy-20*s,cx+14*s,cy-8*s);}
            using(var f=new Font("Microsoft YaHei UI",10))TextRenderer.DrawText(e.Graphics,"你的下一张灵感，从这里开始",f,new Rectangle(0,cy+(int)(15*s),Width,(int)(32*s)),Theme.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        }else{
            float factor=Math.Min((float)(Width-24)/Preview.Width,(float)(Height-24)/Preview.Height);int w=(int)(Preview.Width*factor),h=(int)(Preview.Height*factor);
            e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;e.Graphics.DrawImage(Preview,new Rectangle((Width-w)/2,(Height-h)/2,w,h));
        }
    }
}
