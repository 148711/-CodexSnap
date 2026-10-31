using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

static class L10n {
    public static bool English;
    static readonly Dictionary<string,string> translations=Load();
    static Dictionary<string,string> Load(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("EnglishStrings"))using(var reader=new StreamReader(stream))return new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(reader.ReadToEnd());}
    public static string T(string source){string result;return English&&translations.TryGetValue(source,out result)?result:source;}
    public static string Existing(string text){foreach(var item in translations)if(item.Value==text)return T(item.Key);return T(text);}
    public static void Apply(Control root){root.Text=Existing(root.Text);if(!string.IsNullOrEmpty(root.AccessibleName))root.AccessibleName=Existing(root.AccessibleName);foreach(Control child in root.Controls)Apply(child);root.Invalidate();}
    public static void ApplyMenu(ToolStripItemCollection items){foreach(ToolStripItem item in items)item.Text=Existing(item.Text);}
}
