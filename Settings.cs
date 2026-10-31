using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Globalization;

public sealed class SnapSettings {
    public bool SaveEnabled;
    public bool AutoPaste=true;
    public bool CloseToTray=true;
    public string Hotkey="Alt + S";
    public string Language=CultureInfo.CurrentUICulture.TwoLetterISOLanguageName=="zh"?"zh-CN":"en";
    public string SaveDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),"CodexSnap");
    public static SnapSettings Load(string file) {
        var r=new SnapSettings(); if(!File.Exists(file))return r;
        var lines=File.ReadAllLines(file); bool enabled;
        if((lines.Length!=2&&lines.Length!=5&&lines.Length!=6) || !bool.TryParse(lines[0],out enabled) || !Path.IsPathRooted(lines[1]))throw new InvalidDataException(L10n.T("保存设置损坏，已使用默认设置。原文件仍保留，可在设置页重新保存。"));
        r.SaveEnabled=enabled; r.SaveDirectory=Path.GetFullPath(lines[1]);
        if(lines.Length>=5) {
            bool paste,close;
            if(!bool.TryParse(lines[2],out paste)||!bool.TryParse(lines[3],out close)||!ValidHotkey(lines[4]))throw new InvalidDataException(L10n.T("设置格式无效，已使用默认设置。"));
            r.AutoPaste=paste;r.CloseToTray=close;r.Hotkey=lines[4];
        }
        if(lines.Length==6){if(lines[5]!="en"&&lines[5]!="zh-CN")throw new InvalidDataException("Invalid language setting.");r.Language=lines[5];}
        else r.Language="zh-CN";
        return r;
    }
    public static bool ValidHotkey(string s) {return s=="Alt + S"||s=="Ctrl + Shift + S"||s=="Ctrl + Alt + S";}
    public void Save(string file) {
        if(!Path.IsPathRooted(SaveDirectory))throw new ArgumentException(L10n.T("请选择完整的文件夹路径。"));
        if(!ValidHotkey(Hotkey))throw new ArgumentException(L10n.T("快捷键设置无效。"));
        if(Language!="en"&&Language!="zh-CN")throw new ArgumentException("Invalid language setting.");
        AtomicWrite(file,new[]{SaveEnabled.ToString(),Path.GetFullPath(SaveDirectory),AutoPaste.ToString(),CloseToTray.ToString(),Hotkey,Language});
    }
    public static void AtomicWrite(string file,string[] lines) {
        Directory.CreateDirectory(Path.GetDirectoryName(file));string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {File.WriteAllLines(temp,lines);if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);}
        finally {if(File.Exists(temp))File.Delete(temp);}
    }
    public string SaveImage(Bitmap image) {
        Directory.CreateDirectory(SaveDirectory);
        string file=Path.Combine(SaveDirectory,DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8)+".png");
        SavePng(image,file,false);return file;
    }
    public static void SavePng(Bitmap image,string file,bool overwrite) {
        string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write))image.Save(stream,ImageFormat.Png);
            if(File.Exists(file)&&overwrite)File.Replace(temp,file,null);else File.Move(temp,file);
        }finally{if(File.Exists(temp))File.Delete(temp);}
    }
}

public sealed class TargetBinding {
    public long Handle,Started; public int ProcessId,Width,Height; public double X,Y;
    public string Name;
    public bool Valid {get{return Handle!=0&&ProcessId>0&&Width>0&&Height>0&&!double.IsNaN(X)&&!double.IsNaN(Y)&&X>0&&X<1&&Y>0&&Y<1;}}
    public void Save(string file) {SnapSettings.AtomicWrite(file,new[]{"v2",Handle.ToString(),ProcessId.ToString(),Started.ToString(),Width.ToString(),Height.ToString(),X.ToString("R",CultureInfo.InvariantCulture),Y.ToString("R",CultureInfo.InvariantCulture),Name});}
    public static TargetBinding Load(string file) {
        if(!File.Exists(file))return null;var a=File.ReadAllLines(file);if(a.Length!=9||a[0]!="v2")return null;
        var b=new TargetBinding {Handle=long.Parse(a[1]),ProcessId=int.Parse(a[2]),Started=long.Parse(a[3]),Width=int.Parse(a[4]),Height=int.Parse(a[5]),X=double.Parse(a[6],CultureInfo.InvariantCulture),Y=double.Parse(a[7],CultureInfo.InvariantCulture),Name=a[8]};
        return b.Valid?b:null;
    }
}
