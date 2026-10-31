using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Automation;

// Read-only discovery runs off the UI thread. Late results never generate input.
static class ChatInput {
    static Task<Point?> pending;
    internal static int Score(string name,string kind,double width,double height,bool focused) {
        string n=(name??"").ToLowerInvariant();
        foreach(string excluded in new[]{"搜索","查找","search","find","password","密码","邮箱","email","url","地址"})if(n.Contains(excluded))return -1;
        bool hint=false;foreach(string token in new[]{"消息","提问","描述任务","输入需求","输入任务","有什么","帮你","帮我","message","prompt","ask","follow up","follow-up","继续","想做","发送给","输入内容"})if(n.Contains(token)){hint=true;break;}
        if(width<120||height<25||(kind=="Document"&&height>350))return -1;
        if(!hint&&(kind!="Edit"||width<200||height<48))return -1;
        return (hint?100:20)+(focused?10:0);
    }
    public static async Task<Point?> Locate(IntPtr window) {
        if(pending!=null&&!pending.IsCompleted)return null;
        pending=Task.Run(()=>LocateCore(window));var task=pending;
        if(await Task.WhenAny(task,Task.Delay(900))!=task)return null;
        return await task;
    }
    static Point? LocateCore(IntPtr window) {
        try {
            var root=AutomationElement.FromHandle(window);
            var filter=new OrCondition(new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Edit),new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Document));
            var fields=root.FindAll(TreeScope.Descendants,filter);Point? result=null;int highest=-1;bool tied=false;
            for(int i=0;i<fields.Count;i++) {
                try {
                    var e=fields[i];var c=e.Current;
                    if(!c.IsEnabled||c.IsOffscreen||c.IsPassword||!c.IsKeyboardFocusable)continue;
                    object vp;if(e.TryGetCurrentPattern(ValuePattern.Pattern,out vp)&&((ValuePattern)vp).Current.IsReadOnly)continue;
                    var r=c.BoundingRectangle;if(r.IsEmpty)continue;
                    int score=Score(c.Name,c.ControlType==ControlType.Edit?"Edit":"Document",r.Width,r.Height,c.HasKeyboardFocus);
                    if(score<0)continue;
                    if(score>highest){highest=score;result=new Point((int)(r.Left+r.Width*.5),(int)(r.Top+r.Height*.5));tied=false;}else if(score==highest)tied=true;
                }catch(ElementNotAvailableException){}
            }
            return tied?null:result;
        }catch{return null;}
    }
}
