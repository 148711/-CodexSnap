using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

static class Native {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(Point p);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr h,uint f);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out Rect r);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint x,uint y,uint d,UIntPtr e);
    [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
    [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h,int id);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int v);
    [DllImport("user32.dll")] public static extern bool AllowSetForegroundWindow(int pid);
    delegate bool EnumWindowProc(IntPtr h,IntPtr l);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowProc callback,IntPtr l);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom; public int Width {get{return Right-Left;}} public int Height {get{return Bottom-Top;}}}
    public static Process GetProcess(IntPtr h){uint pid;GetWindowThreadProcessId(h,out pid);return Process.GetProcessById((int)pid);}
    public static bool SupportedName(string name){return name.Equals("ChatGPT",StringComparison.OrdinalIgnoreCase)||name.Equals("Codex",StringComparison.OrdinalIgnoreCase)||name.Equals("WorkBuddy",StringComparison.OrdinalIgnoreCase);}
    public static bool Allowed(IntPtr h){if(h==IntPtr.Zero)return false;try{using(var p=GetProcess(h)){return SupportedName(p.ProcessName);}}catch{return false;}}
    public static IntPtr FindTarget(){IntPtr result=IntPtr.Zero;EnumWindows(delegate(IntPtr h,IntPtr l){if(IsWindowVisible(h)&&Allowed(h)){result=h;return false;}return true;},IntPtr.Zero);return result;}
    public static TargetBinding Identity(IntPtr h){if(!Allowed(h))return null;Rect r;if(!GetWindowRect(h,out r))return null;using(var p=GetProcess(h))return new TargetBinding {Handle=h.ToInt64(),ProcessId=p.Id,Started=p.StartTime.ToUniversalTime().Ticks,Width=r.Width,Height=r.Height,X=.5,Y=.5,Name=p.ProcessName};}
    public static bool SameWindow(TargetBinding b){if(b==null)return false;try{var h=new IntPtr(b.Handle);if(!IsWindow(h)||!Allowed(h))return false;using(var p=GetProcess(h))return p.Id==b.ProcessId&&p.StartTime.ToUniversalTime().Ticks==b.Started;}catch{return false;}}
    public static TargetBinding Bind(Point point) {
        var h=GetAncestor(WindowFromPoint(point),2);if(!Allowed(h))throw new Exception(L10n.T("请把鼠标停在 Codex / ChatGPT / WorkBuddy 的聊天输入框内。"));
        Rect r;if(!GetWindowRect(h,out r)||r.Width<=0||r.Height<=0)throw new Exception(L10n.T("无法读取目标窗口。"));
        using(var p=GetProcess(h)) {var b=new TargetBinding {Handle=h.ToInt64(),ProcessId=p.Id,Started=p.StartTime.ToUniversalTime().Ticks,Width=r.Width,Height=r.Height,X=(double)(point.X-r.Left)/r.Width,Y=(double)(point.Y-r.Top)/r.Height,Name=p.ProcessName};if(!b.Valid)throw new Exception(L10n.T("请把鼠标放在输入框内部。"));return b;}
    }
    public static bool Check(TargetBinding b,out string reason) {
        reason=L10n.T("请先绑定聊天输入框。");if(b==null||!b.Valid)return false;
        try {
            var h=new IntPtr(b.Handle);if(!IsWindow(h)||!Allowed(h)){reason=L10n.T("目标窗口已关闭，请重新绑定。");return false;}
            using(var p=GetProcess(h))if(p.Id!=b.ProcessId||p.StartTime.ToUniversalTime().Ticks!=b.Started){reason=L10n.T("目标应用已重启，请重新绑定。");return false;}
            Rect r;if(!GetWindowRect(h,out r))return false;
            if(!IsIconic(h)&&(r.Width!=b.Width||r.Height!=b.Height)){reason=L10n.T("目标窗口尺寸已变化，请重新绑定。");return false;}
            reason=L10n.T("已绑定 ")+b.Name;return true;
        }catch{reason=L10n.T("目标窗口不可用，请重新绑定。");return false;}
    }
    public static uint Modifiers(string s){return 0x4000u|(s=="Alt + S"?1u:s=="Ctrl + Shift + S"?6u:3u);}
    public static bool ModifiersDown(){return (GetAsyncKeyState(0x12)&0x8000)!=0||(GetAsyncKeyState(0x11)&0x8000)!=0||(GetAsyncKeyState(0x10)&0x8000)!=0;}
}
