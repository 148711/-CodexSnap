using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public static class CaptureGeometry {
    public static Rectangle Normalize(Point a,Point b){return Rectangle.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));}
    public static Rectangle Move(Rectangle r,int dx,int dy,Size bounds){r.X=Math.Max(0,Math.Min(bounds.Width-r.Width,r.X+dx));r.Y=Math.Max(0,Math.Min(bounds.Height-r.Height,r.Y+dy));return r;}
}
sealed class Selection : Form {
    readonly Bitmap screen;Point start,current;bool dragging;float uiScale;
    public Rectangle Selected;
    public Selection(Bitmap source,Rectangle bounds,Icon icon) {
        Text=L10n.T("框选截图 · CodexSnap");Icon=icon;AutoScaleMode=AutoScaleMode.None;screen=source;
        FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=bounds;
        TopMost=true;ShowInTaskbar=true;DoubleBuffered=true;Cursor=Cursors.Cross;KeyPreview=true;
        using(var g=Graphics.FromHwnd(IntPtr.Zero))uiScale=g.DpiX/96f;
        KeyDown+=delegate(object s,KeyEventArgs e){
            if(e.KeyCode==Keys.Escape){DialogResult=DialogResult.Cancel;Close();}
            e.Handled=true;
        };
        MouseDown+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Right){DialogResult=DialogResult.Cancel;Close();return;}if(e.Button!=MouseButtons.Left)return;start=current=e.Location;Selected=Rectangle.Empty;dragging=true;Capture=true;};
        MouseMove+=delegate(object s,MouseEventArgs e){current=e.Location;if(dragging)Selected=Rectangle.Intersect(new Rectangle(Point.Empty,screen.Size),CaptureGeometry.Normalize(start,current));Invalidate();};
        MouseUp+=delegate(object s,MouseEventArgs e){if(e.Button!=MouseButtons.Left||!dragging)return;dragging=false;current=e.Location;Selected=Rectangle.Intersect(new Rectangle(Point.Empty,screen.Size),CaptureGeometry.Normalize(start,current));Capture=false;if(Selected.Width<3||Selected.Height<3){Selected=Rectangle.Empty;Invalidate();return;}Confirm();};
    }
    void Confirm(){if(Selected.Width>=3&&Selected.Height>=3){DialogResult=DialogResult.OK;Close();}}
    protected override bool IsInputKey(Keys keyData){Keys k=keyData&Keys.KeyCode;if(k==Keys.Left||k==Keys.Right||k==Keys.Up||k==Keys.Down)return true;return base.IsInputKey(keyData);}
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.DrawImageUnscaled(screen,0,0);using(var b=new SolidBrush(Color.FromArgb(135,10,22,20)))g.FillRectangle(b,ClientRectangle);
        if(Selected.Width>0&&Selected.Height>0){g.DrawImage(screen,Selected,Selected,GraphicsUnit.Pixel);using(var p=new Pen(Color.FromArgb(91,239,185),2*uiScale))g.DrawRectangle(p,Selected);}
        // Keep the toolbar on the monitor where the pointer is located.
        Rectangle monitor=Screen.FromPoint(Cursor.Position).Bounds;int toolbarWidth=Math.Min((int)(670*uiScale),monitor.Width-24);
        var box=new Rectangle(monitor.Left-Left+(monitor.Width-toolbarWidth)/2,monitor.Top-Top+(int)(24*uiScale),toolbarWidth,(int)(52*uiScale));
        g.SmoothingMode=SmoothingMode.AntiAlias;using(var p=Theme.Round(box,12*uiScale))using(var b=new SolidBrush(Color.FromArgb(246,24,43,37)))g.FillPath(b,p);
        string hint=L10n.T("拖动框选 · 松开鼠标自动粘贴    |    Esc / 右键取消");
        using(var f=new Font("Microsoft YaHei UI",10))TextRenderer.DrawText(g,hint,f,box,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
        if(Selected.Width>0){var sizeBox=new Rectangle(Selected.Left,Math.Min(Height-(int)(34*uiScale),Selected.Bottom+(int)(6*uiScale)),(int)(170*uiScale),(int)(28*uiScale));using(var b=new SolidBrush(Color.FromArgb(240,24,43,37)))g.FillRectangle(b,sizeBox);using(var f=new Font("Segoe UI",10))TextRenderer.DrawText(g,Selected.Width+" × "+Selected.Height+" px",f,sizeBox,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
    }
}
