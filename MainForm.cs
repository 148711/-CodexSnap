using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

sealed class MainForm : Form {
    readonly string data=Program.DataDirectory;
    SnapSettings settings=new SnapSettings();TargetBinding binding,lastTarget;
    Icon appIcon;Bitmap logo,lastImage;
    NotifyIcon tray;ContextMenuStrip menu;
    Panel workspace,settingsPage,aboutPage;ActionButton navCapture,navSettings,navAbout,capture,bindButton,copyButton,saveAsButton,cancelBind;
    Label status,statusTitle,targetStatus,previewMeta,hotkeyStatus,heroHint;
    PreviewBox preview;TextBox directoryBox;CheckBox save,autoPaste,closeToTray;ComboBox hotkey;
    List<Control> locked=new List<Control>();Timer pulse,bindTimer;
    bool busy,exiting,loading,hotkeyRegistered;int seconds;string loadWarning;
    public MainForm() {
        AutoScaleMode=AutoScaleMode.None;Font=new Font("Microsoft YaHei UI",10);BackColor=Theme.Bg;ForeColor=Theme.Ink;
        Text="CodexSnap · 截图助手";ClientSize=new Size(900,654);MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
        using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon"))using(var icon=new Icon(stream))appIcon=(Icon)icon.Clone();Icon=appIcon;
        using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AppLogo"))using(var image=Image.FromStream(stream))logo=new Bitmap(image);
        try{settings=SnapSettings.Load(Path.Combine(data,"settings.txt"));}catch(Exception e){loadWarning=e.Message;Program.Log(e);}
        try{binding=TargetBinding.Load(Path.Combine(data,"binding.txt"));}catch(Exception e){Program.Log(e);}
        BuildSidebar();BuildWorkspace();BuildSettings();BuildAbout();
        menu=new ContextMenuStrip();menu.Items.Add("开始截图",null,delegate{CaptureNow();});menu.Items.Add("打开工作台",null,delegate{Reveal();});menu.Items.Add("打开保存文件夹",null,delegate{OpenFolder();});menu.Items.Add(new ToolStripSeparator());menu.Items.Add("退出 CodexSnap",null,delegate{ExitApp();});
        tray=new NotifyIcon {Icon=appIcon,Visible=true,Text="CodexSnap · "+settings.Hotkey,ContextMenuStrip=menu};tray.DoubleClick+=delegate{Reveal();};
        pulse=new Timer {Interval=500};pulse.Tick+=delegate{if(Program.WakeSignal.WaitOne(0))Reveal();if(!busy){var h=Native.GetForegroundWindow();if(Native.Allowed(h))lastTarget=Native.Identity(h);UpdateBinding();}};
        Shown+=delegate{RegisterShortcut();UpdateBinding();pulse.Start();if(loadWarning!=null)Status("设置需要检查",loadWarning,true);};
        FormClosing+=OnClosing;
        FormClosed+=delegate{pulse.Dispose();if(bindTimer!=null)bindTimer.Dispose();Native.UnregisterHotKey(Handle,1);tray.Dispose();menu.Dispose();appIcon.Dispose();logo.Dispose();if(lastImage!=null)lastImage.Dispose();};
        using(var g=Graphics.FromHwnd(IntPtr.Zero)){float factor=g.DpiX/96f;if(factor!=1f)Scale(new SizeF(factor,factor));}
        // Retain access to every control on smaller displays through scrolling.
        AutoScroll=true;AutoScrollMinSize=ClientSize;var work=Screen.FromPoint(Cursor.Position).WorkingArea;
        if(Width>work.Width||Height>work.Height)Size=new Size(Math.Min(Width,work.Width),Math.Min(Height,work.Height));
    }
    ActionButton Button(Control parent,string text,int x,int y,int w,int h,bool primary,EventHandler click) {
        var b=new ActionButton {Text=text,Location=new Point(x,y),Size=new Size(w,h),Primary=primary};b.Click+=click;parent.Controls.Add(b);return b;
    }
    Label TextAt(Control p,string text,int x,int y,int w,int h,float size,Color color,bool bold){var l=Theme.Label(text,x,y,w,h,size,color,bold);p.Controls.Add(l);return l;}
    Card NewCard(Control parent,int x,int y,int w,int h){var c=new Card {Location=new Point(x,y),Size=new Size(w,h)};parent.Controls.Add(c);return c;}
    void BuildSidebar(){
        var side=new Panel {BackColor=Color.FromArgb(234,241,236),Location=Point.Empty,Size=new Size(180,654)};Controls.Add(side);
        side.Controls.Add(new PictureBox {Image=logo,SizeMode=PictureBoxSizeMode.Zoom,Location=new Point(24,30),Size=new Size(38,38)});
        TextAt(side,"CodexSnap",24,82,150,30,15,Theme.Ink,true);TextAt(side,"让画面进入对话",24,118,140,24,9,Theme.Muted,false);
        navCapture=Button(side,"截图工作台",16,186,148,46,true,delegate{ShowPage(0);});navCapture.Quiet=true;
        navSettings=Button(side,"偏好设置",16,242,148,46,false,delegate{ShowPage(1);});navSettings.Quiet=true;
        navAbout=Button(side,"使用指南",16,298,148,46,false,delegate{ShowPage(2);});navAbout.Quiet=true;
        TextAt(side,"READY WHEN YOU ARE",24,550,140,24,7,Theme.Muted,true);
        TextAt(side,"v2.1.0  ·  Windows",24,580,140,24,9,Theme.Muted,false);
    }
    Panel Page(){var p=new Panel {Location=new Point(204,0),Size=new Size(672,654),BackColor=Theme.Bg};Controls.Add(p);return p;}
    void BuildWorkspace(){
        workspace=Page();
        TextAt(workspace,"截图工作台",0,28,400,40,23,Theme.Ink,true);TextAt(workspace,"捕捉画面，让沟通更直接。",1,72,500,25,10,Theme.Muted,false);
        var hero=NewCard(workspace,0,114,672,116);
        TextAt(hero,"下一张截图，随时开始",24,22,350,32,16,Theme.Ink,true);
        heroHint=TextAt(hero,settings.Hotkey+"  框选截图 · 松开即粘贴",24,65,360,25,10,Theme.Muted,false);
        capture=Button(hero,"＋  开始截图",454,34,194,48,true,delegate{CaptureNow();});locked.Add(capture);
        var target=NewCard(workspace,0,246,672,108);
        TextAt(target,"粘贴目标",24,17,130,25,11,Theme.Ink,true);
        targetStatus=TextAt(target,"尚未绑定聊天输入框",24,51,390,38,10,Theme.Muted,false);
        bindButton=Button(target,"绑定输入框",484,32,164,40,false,delegate{BeginBind();});locked.Add(bindButton);
        cancelBind=Button(target,"取消绑定",484,32,164,40,false,delegate{CancelBinding();});cancelBind.Visible=false;
        var recent=NewCard(workspace,0,370,672,182);
        TextAt(recent,"最近截图",24,16,300,27,11,Theme.Ink,true);
        preview=new PreviewBox {Location=new Point(24,54),Size=new Size(338,108)};recent.Controls.Add(preview);
        previewMeta=TextAt(recent,"本次运行还没有截图",386,58,260,36,10,Theme.Muted,false);
        copyButton=Button(recent,"再次复制",386,114,124,36,false,delegate{CopyLatest();});copyButton.Enabled=false;
        saveAsButton=Button(recent,"另存为…",524,114,124,36,false,delegate{SaveAs();});saveAsButton.Enabled=false;
        statusTitle=TextAt(workspace,"准备就绪",0,573,672,24,10,Theme.Green,true);
        status=TextAt(workspace,"Alt + S 唤起，框选后松开鼠标，自动放入正在使用的聊天窗口。",0,602,672,42,9,Theme.Muted,false);
    }
    void BuildSettings(){
        settingsPage=Page();settingsPage.Visible=false;
        TextAt(settingsPage,"偏好设置",0,28,500,40,23,Theme.Ink,true);TextAt(settingsPage,"适合你的操作习惯，设置自动保存。",1,72,600,25,10,Theme.Muted,false);
        var storage=NewCard(settingsPage,0,114,672,192);
        TextAt(storage,"文件保存",24,18,300,28,13,Theme.Ink,true);
        save=new CheckBox {Text="截图后自动保存 PNG 原图",Location=new Point(24,57),Size=new Size(470,28),BackColor=Color.White,Checked=settings.SaveEnabled};storage.Controls.Add(save);
        directoryBox=new TextBox {Text=settings.SaveDirectory,ReadOnly=true,BorderStyle=BorderStyle.FixedSingle,Location=new Point(24,99),Size=new Size(468,28),BackColor=Color.White,AccessibleName="截图保存文件夹"};storage.Controls.Add(directoryBox);
        locked.Add(Button(storage,"选择文件夹",508,94,140,36,false,delegate{ChooseDirectory();}));
        locked.Add(Button(storage,"打开文件夹",24,145,132,30,false,delegate{OpenFolder();}));
        TextAt(storage,"按时间命名，保留原始像素。",176,151,450,24,9,Theme.Muted,false);
        var actions=NewCard(settingsPage,0,322,672,168);
        TextAt(actions,"快捷键与粘贴",24,17,350,28,13,Theme.Ink,true);
        TextAt(actions,"全局截图快捷键",24,60,190,28,10,Theme.Ink,false);
        hotkey=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(234,56),Size=new Size(220,30)};hotkey.Items.AddRange(new object[]{"Alt + S","Ctrl + Shift + S","Ctrl + Alt + S"});hotkey.SelectedItem=settings.Hotkey;actions.Controls.Add(hotkey);
        hotkeyStatus=TextAt(actions,"等待注册",470,60,180,24,9,Theme.Muted,false);
        autoPaste=new CheckBox {Text="截图后自动粘贴到 Codex / WorkBuddy",Location=new Point(24,108),Size=new Size(620,32),BackColor=Color.White,Checked=settings.AutoPaste};actions.Controls.Add(autoPaste);
        var app=NewCard(settingsPage,0,506,672,120);
        TextAt(app,"后台运行",24,16,400,28,13,Theme.Ink,true);
        closeToTray=new CheckBox {Text="关闭窗口时驻留托盘，保留截图快捷键",Location=new Point(24,60),Size=new Size(620,32),BackColor=Color.White,Checked=settings.CloseToTray};app.Controls.Add(closeToTray);
        save.CheckedChanged+=delegate{if(!loading){settings.SaveEnabled=save.Checked;PersistSettings();}};
        autoPaste.CheckedChanged+=delegate{settings.AutoPaste=autoPaste.Checked;PersistSettings();UpdateBinding();};
        closeToTray.CheckedChanged+=delegate{settings.CloseToTray=closeToTray.Checked;PersistSettings();};
        hotkey.SelectedIndexChanged+=delegate{settings.Hotkey=(string)hotkey.SelectedItem;RegisterShortcut();PersistSettings();};
        locked.Add(save);locked.Add(autoPaste);locked.Add(closeToTray);locked.Add(hotkey);
    }
    void BuildAbout(){
        aboutPage=Page();aboutPage.Visible=false;
        TextAt(aboutPage,"一划即贴，画面进入对话",0,28,672,44,22,Theme.Ink,true);TextAt(aboutPage,"CodexSnap 2.1.0 · Windows 桌面截图助手",1,80,650,25,10,Theme.Muted,false);
        var card=NewCard(aboutPage,0,130,672,320);
        TextAt(card,"01   自动跟随聊天窗口",26,26,610,30,14,Theme.Green,true);
        TextAt(card,"优先使用按下快捷键时的 Codex / WorkBuddy 窗口。\n从其他软件截图时，回到最近使用的聊天窗口。",26,66,610,55,10,Theme.Muted,false);
        TextAt(card,"02   框选后松开鼠标",26,140,610,30,14,Theme.Green,true);
        TextAt(card,"按 Alt + S，拖动鼠标选择区域，松开立即粘贴。\nEsc / 右键取消；无需 Enter 或确认按钮。",26,180,610,55,10,Theme.Muted,false);
        TextAt(card,"03   图片直接进入输入框",26,252,610,32,14,Theme.Green,true);
        var details=NewCard(aboutPage,0,466,672,160);
        TextAt(details,"关于你的截图",24,20,600,27,12,Theme.Ink,true);
        TextAt(details,"默认仅保留最近一张在内存中，退出后清除预览。\n启用保存后才写入所选目录。粘贴会将图片交给目标聊天应用。\n截图助手本身不连接网络，不自动发送消息。",24,62,620,80,10,Theme.Muted,false);
    }
    void ShowPage(int page){workspace.Visible=page==0;settingsPage.Visible=page==1;aboutPage.Visible=page==2;navCapture.Primary=page==0;navSettings.Primary=page==1;navAbout.Primary=page==2;navCapture.Invalidate();navSettings.Invalidate();navAbout.Invalidate();}
    public void Reveal(){if(IsDisposed)return;Show();if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;Activate();}
    void Status(string title,string message,bool error){statusTitle.Text=title;statusTitle.ForeColor=error?Color.FromArgb(169,95,34):Theme.Green;status.Text=message;}
    void SetBusy(bool value){busy=value;foreach(var c in locked)c.Enabled=!value;copyButton.Enabled=saveAsButton.Enabled=!value&&lastImage!=null;}
    void RegisterShortcut(){Native.UnregisterHotKey(Handle,1);hotkeyRegistered=Native.RegisterHotKey(Handle,1,Native.Modifiers(settings.Hotkey),(uint)Keys.S);hotkeyStatus.Text=hotkeyRegistered?"● 已启用":"快捷键被占用";hotkeyStatus.ForeColor=hotkeyRegistered?Theme.Green:Color.FromArgb(169,95,34);heroHint.Text=hotkeyRegistered?settings.Hotkey+"  框选截图 · 松开即粘贴":"快捷键被占用，请在设置中更换";tray.Text="CodexSnap · "+settings.Hotkey;if(!hotkeyRegistered)Status("快捷键冲突","请在偏好设置中选择其他快捷键，或使用开始截图按钮。",true);}
    protected override void WndProc(ref Message m){if(m.Msg==0x312&&m.WParam.ToInt32()==1)CaptureNow();base.WndProc(ref m);}
    void PersistSettings(){try{settings.Save(Path.Combine(data,"settings.txt"));Status("设置已保存","你的偏好将在下次启动时自动恢复。",false);}catch(Exception e){Program.Log(e);Status("未能保存设置",e.Message,true);tray.ShowBalloonTip(3000,"未能记住设置",e.Message,ToolTipIcon.Warning);}}
    void UpdateBinding(){var target=ResolveTarget();targetStatus.Text=!settings.AutoPaste?"仅复制到剪贴板 · 自动粘贴已关闭":target==null?"自动跟随 Codex / WorkBuddy": "自动跟随 · "+target.Name;targetStatus.ForeColor=target!=null&&settings.AutoPaste?Theme.Green:Theme.Muted;bindButton.Text="备用绑定";}
    TargetBinding ResolveTarget(){var h=Native.GetForegroundWindow();if(Native.Allowed(h))return Native.Identity(h);if(Native.SameWindow(lastTarget))return lastTarget;if(Native.SameWindow(binding))return binding;return Native.Identity(Native.FindTarget());}
    void ChooseDirectory(){if(busy)return;SetBusy(true);try{using(var dialog=new FolderBrowserDialog {Description="选择截图保存文件夹",SelectedPath=settings.SaveDirectory,ShowNewFolderButton=true})if(dialog.ShowDialog(this)==DialogResult.OK){settings.SaveDirectory=dialog.SelectedPath;directoryBox.Text=settings.SaveDirectory;settings.SaveEnabled=true;loading=true;save.Checked=true;loading=false;PersistSettings();}}catch(Exception e){Program.Log(e);Status("无法选择目录",e.Message,true);}finally{SetBusy(false);}}
    void OpenFolder(){try{Directory.CreateDirectory(settings.SaveDirectory);Process.Start(new ProcessStartInfo(settings.SaveDirectory){UseShellExecute=true});}catch(Exception e){Status("无法打开文件夹",e.Message,true);}}
    void BeginBind(){if(busy)return;ShowPage(0);SetBusy(true);seconds=5;bindButton.Visible=false;cancelBind.Visible=true;Status("把鼠标移到聊天输入框内","5 秒后记住鼠标位置；无需点击，可按“取消绑定”放弃。",false);bindTimer=new Timer {Interval=1000};bindTimer.Tick+=delegate{seconds--;Status("正在绑定 · "+seconds+" 秒","切到目标聊天，将鼠标停在输入框中央。",false);if(seconds>0)return;bindTimer.Stop();bindTimer.Dispose();bindTimer=null;try{var next=Native.Bind(Cursor.Position);next.Save(Path.Combine(data,"binding.txt"));binding=next;Status("绑定完成","切换聊天、改变窗口尺寸或侧栏布局后，请重新绑定。",false);}catch(Exception e){Program.Log(e);Status("绑定未完成",e.Message,true);}finally{bindButton.Visible=true;cancelBind.Visible=false;SetBusy(false);UpdateBinding();}};bindTimer.Start();}
    void CancelBinding(){if(bindTimer!=null){bindTimer.Stop();bindTimer.Dispose();bindTimer=null;}bindButton.Visible=true;cancelBind.Visible=false;SetBusy(false);Status("已取消绑定","原有绑定保持不变。",false);}
    void CopyLatest(){if(lastImage==null||busy)return;try{Clipboard.SetDataObject(lastImage,true,8,100);Status("图片已复制","可在聊天输入框按 Ctrl + V 粘贴。",false);}catch(Exception e){Status("剪贴板暂不可用",e.Message+" 可先另存为图片。",true);}}
    void SaveAs(){if(lastImage==null||busy)return;SetBusy(true);try{using(var dialog=new SaveFileDialog {Title="保存截图",Filter="PNG 图片 (*.png)|*.png",DefaultExt="png",AddExtension=true,FileName="截图-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".png",InitialDirectory=Directory.Exists(settings.SaveDirectory)?settings.SaveDirectory:Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),OverwritePrompt=true})if(dialog.ShowDialog(this)==DialogResult.OK){SnapSettings.SavePng(lastImage,dialog.FileName,true);Status("截图已保存",dialog.FileName,false);}}catch(Exception e){Status("保存失败",e.Message,true);Program.Log(e);}finally{SetBusy(false);}}
    async void CaptureNow(){
        if(busy)return;var destination=ResolveTarget();SetBusy(true);bool wasVisible=Visible&&WindowState!=FormWindowState.Minimized,restore=wasVisible;var messages=new List<string>();bool problem=false;
        try {
            Hide();await Task.Delay(180);
            var area=SystemInformation.VirtualScreen;
            using(var full=new Bitmap(area.Width,area.Height,System.Drawing.Imaging.PixelFormat.Format32bppArgb)){
                using(var g=Graphics.FromImage(full))g.CopyFromScreen(area.Location,Point.Empty,area.Size);
                using(var selection=new Selection(full,area,appIcon)){
                    if(selection.ShowDialog()!=DialogResult.OK){Status("已取消截图","没有修改剪贴板，也没有保存图片。",false);return;}
                    var next=full.Clone(selection.Selected,System.Drawing.Imaging.PixelFormat.Format32bppArgb);preview.Preview=next;var old=lastImage;lastImage=next;if(old!=null)old.Dispose();preview.Invalidate();previewMeta.Text=next.Width+" × "+next.Height+" px\n"+DateTime.Now.ToString("HH:mm:ss")+" · PNG";
                }
            }
            bool copied=false;
            try{Clipboard.SetDataObject(lastImage,true,8,100);copied=true;messages.Add("已复制到剪贴板");}catch(Exception e){problem=true;messages.Add("剪贴板繁忙，可稍后再次复制");Program.Log(e);}
            if(settings.AutoPaste&&copied){try{await PasteToTarget(destination);messages.Add("已向 "+destination.Name+" 粘贴");restore=false;}catch(Exception e){problem=true;messages.Add(e.Message);}}
            if(settings.SaveEnabled){try{await Task.Run(()=>settings.SaveImage(lastImage));messages.Add("已保存 PNG");}catch(Exception e){problem=true;messages.Add("保存失败："+e.Message);Program.Log(e);}}
            Status(problem?"截图已保留 · 部分操作未完成":"截图完成",string.Join("；",messages.ToArray()),problem);
            if(problem)restore=true;
        }catch(Exception e){Program.Log(e);Status("截图未完成",e.Message,true);restore=true;}
        finally{SetBusy(false);if(restore){ShowPage(0);Reveal();}}
    }
    async Task PasteToTarget(TargetBinding destination){
        if(!Native.SameWindow(destination))throw new Exception("请打开 Codex 或 WorkBuddy 聊天窗口。图片已复制。");
        var h=new IntPtr(destination.Handle);if(Native.IsIconic(h))Native.ShowWindow(h,9);Native.SetForegroundWindow(h);await Task.Delay(150);
        if(!Native.SameWindow(destination))throw new Exception("目标窗口已关闭，图片已保留。");
        if(Native.GetForegroundWindow()!=h)throw new Exception("未能切到目标窗口，请手动粘贴。");
        for(int i=0;i<30&&Native.ModifiersDown();i++)await Task.Delay(30);
        if(Native.ModifiersDown())throw new Exception("请松开修饰键后手动粘贴。");
        Point? location=await ChatInput.Locate(h);string reason;
        if(!location.HasValue&&binding!=null&&binding.Handle==destination.Handle&&Native.Check(binding,out reason)){
            Native.Rect r;if(!Native.GetWindowRect(h,out r))throw new Exception("无法读取目标窗口。");
            location=new Point(r.Left+(int)(r.Width*binding.X),r.Top+(int)(r.Height*binding.Y));
        }
        if(location.HasValue){
            Point point=location.Value;
            if(Native.GetForegroundWindow()!=h||Native.GetAncestor(Native.WindowFromPoint(point),2)!=h)throw new Exception("输入位置被遮挡，请手动粘贴。");
            Point old=Cursor.Position;try{Cursor.Position=point;Native.mouse_event(2,0,0,0,UIntPtr.Zero);Native.mouse_event(4,0,0,0,UIntPtr.Zero);}finally{Cursor.Position=old;}
        }
        // Electron apps may not expose a textbox. Activation restores their existing input focus.
        await Task.Delay(100);if(Native.GetForegroundWindow()!=h)throw new Exception("窗口焦点已变化，请手动粘贴。");SendKeys.SendWait("^v");
    }
    void ExitApp(){if(busy){Status("请先完成当前操作","完成或取消截图 / 绑定后，再退出程序。",true);Reveal();return;}exiting=true;Close();}
    void OnClosing(object sender,FormClosingEventArgs e){if(e.CloseReason==CloseReason.WindowsShutDown||e.CloseReason==CloseReason.TaskManagerClosing)return;if(busy){e.Cancel=true;if(bindTimer!=null)CancelBinding();return;}if(!exiting&&settings.CloseToTray){e.Cancel=true;Hide();tray.ShowBalloonTip(2000,"CodexSnap 已在后台运行",settings.Hotkey+" 截图；双击托盘图标打开工作台。",ToolTipIcon.Info);}}
}
