using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Threading;
using System.IO;
using System.Net;
using System.Configuration;
using System.Collections.Generic;

namespace WebTimerTask
{
    /// <summary>
    /// 
    /// </summary>
    public class Global : HttpApplication
    {
        //使用静态保持对这处Timer实例的引用,以免GC 
        static System.Threading.Timer timer = null;

        //设置每日定时运行的时间
        public static string[] time = ConfigurationManager.AppSettings["runtime"].Split(':');

        protected void Application_Start(object sender, EventArgs e)
        {
            //计算现在到目标时间要过的时间段。 
            //DateTime LuckTime = DateTime.Now.Date.Add(new TimeSpan(1, 0, 0));
            DateTime LuckTime = DateTime.Now.Date.Add(new TimeSpan(int.Parse(time[0]), int.Parse(time[1]), int.Parse(time[2])));

            TimeSpan span = LuckTime - DateTime.Now;

            if (span < TimeSpan.Zero)
            {
                span = LuckTime.AddDays(1d) - DateTime.Now;
            }

            //按需传递的状态或者对象。 
            object state = new object();

            //定义计时器 
            timer = new System.Threading.Timer
            (
                new TimerCallback(CertainTask), state,
                span, TimeSpan.FromTicks(TimeSpan.TicksPerDay)
            );
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {
            //结束时记得释放 
            if (timer != null)
            {
                timer.Dispose();
            }
        }

        #region CertainTask
        /// <summary>
        /// 指定时间执行的代码,必须是静态的
        /// </summary>
        /// <param name="state"></param>

        static void CertainTask(object state)
        {
            //这里写你的任务逻辑 
            //SelfTask.TraceLog(DateTime.Now.ToString(), SelfTask.logPath);
            SelfTask.Run();
        }
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public class SelfTask
    {
        #region 日志路径
        public static string logPath = AppDomain.CurrentDomain.BaseDirectory + "log\\log.log";
        public static string urlsPath = AppDomain.CurrentDomain.BaseDirectory + "log\\urls.log";
        #endregion

        #region 站点列表
        /// <summary>
        /// 站点列表
        /// </summary>
        static List<string> siteUrls = new List<string>();
        #endregion

        #region 加载列表
        /// <summary>
        /// 加载列表
        /// </summary>
        static SelfTask()
        {
            siteUrls = LoadTextToList(urlsPath, true);
        }
        #endregion

        #region 单个站点
        public static void WebSiteTast(string siteUrl)
        {
            List<string> ls = new List<string>();

            ls.Add(string.Concat(siteUrl, "/ws/ws.asmx/WS_RebootWebSite"));
            ls.Add(siteUrl);
            ls.Add(string.Concat(siteUrl, "/ws/ws.asmx/WS_CreateSiteMap"));
            ls.Add(string.Concat(siteUrl, "/ws/ws.asmx/WS_UpdateNextAndPreID"));
            ls.Add(string.Concat(siteUrl, "/ws/ws.asmx/WS_CreateSiteHTML_Forcibly"));
            ls.Add(string.Concat(siteUrl, "/ws/ws.asmx/WS_RebootWebSite"));

            string[] data = new string[] 
            {
                "重启网站",
                "访问首页",
                "站点地图",
                "上下序号",
                "全站html",
                "重启网站"
            };

            string code = string.Empty;

            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].Equals(siteUrl))
                {
                    code = RequestGet(ls[i], null, 1);
                }
                else
                {
                    code = RequestGet(ls[i], null, 0);
                }
                TraceLog(string.Concat("站点：", siteUrl, "  ", data[i], " ", code), logPath);
            }
            TraceLog("======================================================", logPath);
        }
        #endregion

        #region 运行任务
        public static void Run()
        {
            foreach (string item in siteUrls)
            {
                WebSiteTast(item);
            }
        }
        #endregion

        #region 辅助函数

        #region 加载文本
        /// <summary>
        /// txt之将txt中的数据变为个List
        /// </summary>
        /// <param name="filePath">文本的路径</param>
        /// <param name="bl">是否去除掉文本中的空格，txt，js html文件</param>
        /// <returns></returns>
        public static List<string> LoadTextToList(string filePath, bool bl)
        {
            List<string> ls = new List<string>();
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.Default))
                {
                    String line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (bl)
                        {
                            ls.Add(line.Trim());
                        }
                        else
                        {
                            ls.Add(line);
                        }
                    }
                }
                return ls;
            }
        }
        #endregion

        #region HTTP GET
        /// <summary>
        /// HTTP GET方式提交数据
        /// </summary>
        /// <param name="TheURL">提交的URL</param>
        /// <param name="cookies">cookie</param>
        /// <param name="type">0 表示获取源代码 1表示获取状态码</param>
        /// <returns></returns>
        public static string RequestGet(string TheURL, CookieCollection cookies, int type)
        {
            System.Net.ServicePointManager.Expect100Continue = false;

            //1.定义URL地址
            Uri uri = new Uri(TheURL);

            //2.建立HttpWebRequest对象
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);

            string page = string.Empty;
            try
            {
                request.KeepAlive = false;
                //设置Htpp协议的版本
                request.ProtocolVersion = HttpVersion.Version11;

                request.Method = "GET";

                request.ContentType = "application/x-www-form-urlencoded";

                //allow auto redirects from redirect headers 允许请求跟随重定向相应
                request.AllowAutoRedirect = true;

                //maximum of 10 auto redirects 
                request.MaximumAutomaticRedirections = 10;

                //30 second timeout for request定义到服务器的超时时间 默认是100秒
                request.Timeout = (int)new TimeSpan(0, 0, 60).TotalMilliseconds;

                //give the crawler a name. 
                //request.UserAgent = "Mozilla/3.0 (compatible; My Browser/1.0)";
                //request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)";
                //request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 2.0.50727; .NET CLR 3.0.04506.648; .NET CLR 3.5.21022)";
                //request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/53.0.2785.89 Safari/537.36";
                request.ServicePoint.Expect100Continue = false;

                if (cookies != null)
                {
                    request.CookieContainer = new CookieContainer();
                    request.CookieContainer.Add(cookies);
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                switch (type)
                {
                    case 0:
                        Stream responseStream = response.GetResponseStream();
                        StreamReader readStream = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                        page = readStream.ReadToEnd();
                        readStream.Close();
                        responseStream.Close();
                        break;
                    case 1:
                        page = response.StatusCode.ToString();
                        response.Close();
                        break;
                    default:

                        break;
                }
            }
            catch (Exception ee)
            {
                page = "Fail message : " + ee.Message;
            }
            return page;
        }

        #endregion

        #region 追踪日志
        /// <summary>
        /// 追踪日志查看结果
        /// </summary>
        /// <param name="data"></param>
        /// <param name="filePath"></param>
        public static void TraceLog(string data, string filePath)
        {
            try
            {
                File.AppendAllText(filePath, string.Concat(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), " ", data, Environment.NewLine));
            }
            catch (Exception ex)
            {
                File.AppendAllText(filePath, string.Concat(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), " ", ex.Message, Environment.NewLine));
            }
        }
        #endregion

        #endregion
    }
}