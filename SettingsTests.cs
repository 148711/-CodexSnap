using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
static class SettingsTests {
    static void Check(bool condition,string message) {if(!condition)throw new Exception(message);}
    [STAThread] static void Main() {
        string root=Path.Combine(Path.GetTempPath(),"CodexSnap-test-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            string config=Path.Combine(root,"config","settings.txt");
            Check(!SnapSettings.Load(config).SaveEnabled,"Default saving should be off");
            var s=new SnapSettings {SaveEnabled=true,SaveDirectory=Path.Combine(root,"截图 文件夹"),AutoPaste=false,CloseToTray=false,Hotkey="Ctrl + Alt + S",Language="en"};s.Save(config);
            var loaded=SnapSettings.Load(config);Check(loaded.SaveEnabled&&loaded.SaveDirectory==s.SaveDirectory&&!loaded.AutoPaste&&!loaded.CloseToTray&&loaded.Hotkey=="Ctrl + Alt + S","Settings roundtrip");
            Check(loaded.Language=="en","Language persists");
            L10n.English=true;Check(L10n.T("开始截图")=="Capture screenshot","English UI");
            L10n.English=false;Check(L10n.Existing("Capture screenshot")=="开始截图","Switch back to Chinese");
            File.WriteAllLines(config,new[]{"False",s.SaveDirectory,"True","True","Alt + S"});Check(SnapSettings.Load(config).Language=="zh-CN","Existing Chinese profiles migrate");
            s.Save(config);
            using(var bitmap=new Bitmap(17,11)) {bitmap.SetPixel(1,1,Color.Red);string a=loaded.SaveImage(bitmap),b=loaded.SaveImage(bitmap);Check(a!=b,"No filename collision");using(var read=new Bitmap(a)){Check(read.Width==17&&read.Height==11&&read.GetPixel(1,1).ToArgb()==Color.Red.ToArgb(),"PNG pixel roundtrip");}}
            loaded.SaveEnabled=false;loaded.Save(config);Check(!SnapSettings.Load(config).SaveEnabled,"Toggle persisted on replacement");
            File.WriteAllText(config,"broken");bool rejected=false;try{SnapSettings.Load(config);}catch(InvalidDataException){rejected=true;}Check(rejected,"Corrupt config detected");
            string blocked=Path.Combine(root,"file-not-folder");File.WriteAllText(blocked,"x");loaded.SaveDirectory=blocked;
            rejected=false;try{using(var bitmap=new Bitmap(1,1))loaded.SaveImage(bitmap);}catch(IOException){rejected=true;}Check(rejected,"Invalid directory reported");
            File.WriteAllLines(config,new[]{"True",s.SaveDirectory});var legacy=SnapSettings.Load(config);Check(legacy.AutoPaste&&legacy.CloseToTray&&legacy.Hotkey=="Alt + S","Legacy settings migration");
            using(var bitmap=new Bitmap(8,9)){string file=Path.Combine(root,"replace.png");SnapSettings.SavePng(bitmap,file,false);using(var updated=new Bitmap(12,13))SnapSettings.SavePng(updated,file,true);using(var read=new Bitmap(file))Check(read.Width==12&&read.Height==13,"Atomic replacement");}
            Check(Directory.GetFiles(root,"*.tmp",SearchOption.AllDirectories).Length==0,"No temporary files left");
            string bindFile=Path.Combine(root,"binding.txt");var binding=new TargetBinding{Handle=123,ProcessId=456,Started=789,Width=1280,Height=720,X=0.3,Y=0.8,Name="Codex"};binding.Save(bindFile);var restored=TargetBinding.Load(bindFile);Check(restored.Valid&&restored.Started==789&&restored.X==.3,"Binding identity roundtrip");
            binding.X=double.NaN;Check(!binding.Valid,"NaN binding rejected");binding.X=double.PositiveInfinity;Check(!binding.Valid,"Infinite binding rejected");
            File.WriteAllLines(bindFile,new[]{"123","0.3","0.8"});Check(TargetBinding.Load(bindFile)==null,"Unsafe legacy binding requires rebinding");
            Check(CaptureGeometry.Normalize(new Point(40,90),new Point(10,20))==new Rectangle(10,20,30,70),"Reverse selection");
            Check(CaptureGeometry.Move(new Rectangle(5,5,40,30),-100,100,new Size(100,100))==new Rectangle(0,70,40,30),"Movement clamped to screen");
            Check(Native.SupportedName("WorkBuddy")&&Native.SupportedName("workbuddy")&&Native.SupportedName("ChatGPT")&&Native.SupportedName("Codex"),"Supported targets");
            Check(!Native.SupportedName("CodexSnap")&&!Native.SupportedName("WorkBuddy-Setup")&&!Native.SupportedName("notepad"),"Reject unrelated processes");
            Check(ChatInput.Score("Search","Edit",500,100,true)<0,"Search box rejected");
            Check(ChatInput.Score("输入任务","Edit",500,100,true)>0,"Chat composer recognized");
            Check(ChatInput.Score("输入任务","Document",1000,900,true)<0,"Full page document rejected");
            using(var bitmap=new Bitmap(200,150)) {
                using(var selection=new Selection(bitmap,new Rectangle(0,0,200,150),SystemIcons.Application)) {
                    typeof(Selection).GetField("start",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(selection,new Point(100,100));
                    typeof(Selection).GetField("dragging",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(selection,true);
                    typeof(Control).GetMethod("OnMouseUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(selection,new object[]{new MouseEventArgs(MouseButtons.Left,1,20,30,0)});
                    Check(selection.DialogResult==DialogResult.OK&&selection.Selected==new Rectangle(20,30,80,70),"Mouse release finishes immediately without Enter; final release coordinates used");
                }
                using(var selection=new Selection(bitmap,new Rectangle(0,0,200,150),SystemIcons.Application)) {
                    typeof(Selection).GetField("start",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(selection,new Point(20,20));
                    typeof(Selection).GetField("dragging",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(selection,true);
                    typeof(Control).GetMethod("OnMouseUp",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(selection,new object[]{new MouseEventArgs(MouseButtons.Left,1,21,21,0)});
                    Check(selection.DialogResult!=DialogResult.OK&&selection.Selected.IsEmpty,"Accidental click does not paste");
                    typeof(Control).GetMethod("OnMouseDown",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(selection,new object[]{new MouseEventArgs(MouseButtons.Right,1,30,30,0)});
                    Check(selection.DialogResult==DialogResult.Cancel,"Right click cancels");
                }
            }
            Console.WriteLine("PASS: mouse-release immediate completion, tiny-click guard, cancel, WorkBuddy/Codex target detection, composer scoring, settings migration/persistence, Unicode directories, PNG roundtrip, atomic overwrite, binding identity, selection geometry.");
        } finally {Directory.Delete(root,true);}
    }
}
