using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly:AssemblyTitle("CodexSnap")]
[assembly:AssemblyDescription("Windows screenshot companion for chat workflows")]
[assembly:AssemblyProduct("CodexSnap")]
[assembly:AssemblyVersion("2.1.0.0")]
[assembly:AssemblyFileVersion("2.1.0.0")]
static class Program {
    internal static readonly string DataDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CodexSnap");
    internal static EventWaitHandle WakeSignal;
    internal static void Log(Exception error){try{Directory.CreateDirectory(DataDirectory);string file=Path.Combine(DataDirectory,"errors.log");if(File.Exists(file)&&new FileInfo(file).Length>262144)File.WriteAllText(file,"");File.AppendAllText(file,DateTime.Now.ToString("s")+" "+error.GetType().Name+": "+error.Message+Environment.NewLine);}catch{}}
    [STAThread] static void Main(){
        Native.SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        Application.ThreadException+=delegate(object sender,ThreadExceptionEventArgs e){Log(e.Exception);MessageBox.Show("操作未完成："+e.Exception.Message,"CodexSnap",MessageBoxButtons.OK,MessageBoxIcon.Warning);};
        using(var mutex=new Mutex(false,"Local\\CodexSnap.SingleInstance"))using(WakeSignal=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\CodexSnap.ShowWindow")){
            bool owns=false;try{try{owns=mutex.WaitOne(0,false);}catch(AbandonedMutexException){owns=true;}
                if(!owns){Native.AllowSetForegroundWindow(-1);WakeSignal.Set();return;}
                Application.Run(new MainForm());
            }catch(Exception e){Log(e);MessageBox.Show("启动失败："+e.Message,"CodexSnap",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{if(owns)mutex.ReleaseMutex();}
        }
    }
}
