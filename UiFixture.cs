using System;
using System.Drawing;
using System.Windows.Forms;
class UiFixture {
    [STAThread] static void Main(){Application.EnableVisualStyles();var f=new Form {Text="CodexSnap QA fixture",ClientSize=new Size(600,400),StartPosition=FormStartPosition.CenterScreen,BackColor=Color.White};f.Paint+=delegate(object sender,PaintEventArgs e){e.Graphics.Clear(Color.White);e.Graphics.FillRectangle(Brushes.SeaGreen,40,70,160,160);e.Graphics.FillRectangle(Brushes.Coral,240,70,160,160);using(var font=new Font("Segoe UI",16))e.Graphics.DrawString("CodexSnap capture test",font,Brushes.Black,40,280);};Application.Run(f);}
}
