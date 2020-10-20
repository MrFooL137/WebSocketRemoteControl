using System;
using System.IO;
using System.Text;
using System.Threading;

/*******************************************************************************
* https://www.cnblogs.com/lulianqi/p/6180479.html
* Copyright (c) 2016 lulianqi
* All rights reserved.
*******************************************************************************/

namespace websocket
{
    class StreamAsynRead : IDisposable
    {
        public delegate void delegateGetStreamAsynReadEventHandler(object sender, string outData);
        public event delegateGetStreamAsynReadEventHandler OnGetAsynReadData;

        private Stream baseStream;
        private Thread readStreamThread;
        private Encoding baseEncode;
        private bool isDropAscStyle;
        private bool willKill;

        /// <summary>
        /// 异步读取指定IO流并即时返回直到该流结束（初始化完成后即开始读取）
        /// </summary>
        /// <param name="yourBaseStream">目标IO流</param>
        /// <param name="yourEncode">编码方式</param>
        /// <param name="dropAscStyle">是否丢弃ASC样式</param>
        /// <param name="yourGetAsynReadData">数据返回委托</param>
        public StreamAsynRead(Stream yourBaseStream, Encoding yourEncode, bool dropAscStyle, delegateGetStreamAsynReadEventHandler yourGetAsynReadData)
        {
            if (yourBaseStream == null)
            {
                throw new Exception("yourBaseStream is null");
            }
            else
            {
                isDropAscStyle = dropAscStyle;
                baseStream = yourBaseStream;
                baseEncode = yourEncode;
                OnGetAsynReadData += yourGetAsynReadData;
                StartRead();
                willKill = false;
            }
        }

        public StreamAsynRead(Stream yourBaseStream, Encoding yourEncode, delegateGetStreamAsynReadEventHandler yourGetAsynReadData)
            : this(yourBaseStream, yourEncode, false, yourGetAsynReadData) { }

        public StreamAsynRead(Stream yourBaseStream, delegateGetStreamAsynReadEventHandler yourGetAsynReadData)
            : this(yourBaseStream, ASCIIEncoding.UTF8, false, yourGetAsynReadData) { }

        public bool IsdropAscStyle
        {
            get { return isDropAscStyle; }
            set { isDropAscStyle = value; }
        }


        private void PutOutData(string yourData)
        {
            if (OnGetAsynReadData != null)
            {
                this.OnGetAsynReadData(this, yourData);
            }
        }

        private bool StartRead()
        {
            if (baseStream == null)
            {
                return false;
            }
            if (readStreamThread != null)
            {
                if (readStreamThread.IsAlive)
                {
                    readStreamThread.Abort();
                }
            }
            readStreamThread = new Thread(new ParameterizedThreadStart(GetDataThread));
            readStreamThread.IsBackground = true;
            readStreamThread.Start(baseStream);
            return true;
        }

        private void GetDataThread(object ReceiveStream)
        {
            /*
            try
            {
            }
            catch (ThreadAbortException abortException)
            {
                Console.WriteLine((string)abortException.ExceptionState);
            }
             * */

            Byte[] read = new Byte[1024];
            Stream receiveStream = (Stream)ReceiveStream;
            int bytes = receiveStream.Read(read, 0, 1024);
            string esc = baseEncode.GetString(new byte[] { 27, 91 });
            //string bs = baseEncode.GetString(new byte[] { 8 });    //  \b
            string re = "";
            while (bytes > 0 && !willKill)
            {
                re = baseEncode.GetString(read, 0, bytes);
                if (isDropAscStyle)
                {
                    while (re.Contains(esc))
                    {
                        int starEsc = re.IndexOf(esc);
                        int endEsc = re.IndexOf('m', starEsc);
                        if (endEsc > 0)
                        {
                            re = re.Remove(starEsc, (endEsc - starEsc + 1));
                        }
                        else
                        {
                            re = re.Remove(starEsc, 2);
                        }
                    }
                }
                PutOutData(re);
                bytes = receiveStream.Read(read, 0, 1024);
            }
        }

        public void Dispose()
        {
            willKill = true;
        }
    }

    class MyWindowsCmd : IDisposable
    {
        public enum RedirectOutputType
        {
            RedirectStandardInput,
            RedirectStandardError
        }

        public delegate void delegateGetCmdMessageEventHandler(object sender, string InfoMessage, RedirectOutputType redirectOutputType);
        /// <summary>
        /// 订阅CMD返回数据
        /// </summary>
        public event delegateGetCmdMessageEventHandler OnGetCmdMessage;

        private System.Diagnostics.Process p = new System.Diagnostics.Process();
        StreamAsynRead standardOutputRead = null;
        StreamAsynRead standardErrorRead = null;
        private string errorMes = null;
        private string cmdName = null;
        private bool isStart = false;
        private bool isDropAscStyle = false;


        public MyWindowsCmd()
        {
            p.StartInfo.FileName = "cmd.exe";
            cmdName = "CMD";
            p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            p.StartInfo.ErrorDialog = true;
        }

        /// <summary>
        /// 含名称字段的构造函数
        /// </summary>
        /// <param name="yourNmae">CMD名称（方便区分多份CMD实例）</param>
        public MyWindowsCmd(string yourNmae) : this()
        {
            cmdName = yourNmae;
        }

        private void ShowMessage(string mes, RedirectOutputType redirectOutputType)
        {
            if (OnGetCmdMessage != null)
            {
                this.OnGetCmdMessage(this, mes, redirectOutputType);
            }
        }

        /// <summary>
        /// 获取CMD名称
        /// </summary>
        public string CmdName
        {
            get { return cmdName; }
        }

        /// <summary>
        /// 获取最近的错误
        /// </summary>
        public string ErrorMes
        {
            get { return errorMes; }
        }

        /// <summary>
        /// 获取一个值，盖值指示该CMD是否启动
        /// </summary>
        public bool IsStart
        {
            get { return isStart; }
        }

        /// <summary>
        /// 获取或设置获取内容回调时是否丢弃ASK颜色等样式方案（如果您的应用不具备处理这种样式的功能，请选择放弃该样式）
        /// </summary>
        public bool IsDropAscStyle
        {
            get { return isDropAscStyle; }
            set { isDropAscStyle = value; }
        }

        /// <summary>
        /// 启动CMD
        /// </summary>
        /// <returns>是否成功启动</returns>
        public bool StartCmd()
        {
            if (isStart)
            {
                errorMes = "[StartCmd]" + "is Already Started";
                return false;
            }
            try
            {
                p.Start();//启动程序
                //System.Text.Encoding.GetEncoding("gb1232");
                if (standardOutputRead != null)
                {
                    standardOutputRead.Dispose();
                }
                if (standardErrorRead != null)
                {
                    standardErrorRead.Dispose();
                }

                if (OnGetCmdMessage != null)
                {
                    standardOutputRead = new StreamAsynRead(p.StandardOutput.BaseStream, System.Text.Encoding.Default, true, new StreamAsynRead.delegateGetStreamAsynReadEventHandler((obj, str) => { this.OnGetCmdMessage(this, str, RedirectOutputType.RedirectStandardInput); }));
                    standardErrorRead = new StreamAsynRead(p.StandardError.BaseStream, System.Text.Encoding.Default, true, new StreamAsynRead.delegateGetStreamAsynReadEventHandler((obj, str) => { this.OnGetCmdMessage(this, str, RedirectOutputType.RedirectStandardError); }));
                }
                isStart = true;
                return true;
            }
            catch (Exception ex)
            {
                errorMes = "[StartCmd]" + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 执行CMD命令
        /// </summary>
        /// <param name="yourCmd">cmd命令内容</param>
        /// <returns>是否成功</returns>
        public bool RunCmd(string yourCmd)
        {
            if (yourCmd == null || !isStart)
            {
                return false;
            }
            try
            {
                p.StandardInput.WriteLine(yourCmd);
                return true;
            }
            catch (Exception ex)
            {
                errorMes = "[RunCmd]" + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 等待执行完成（同步方法，请勿在主线程中调用）
        /// </summary>
        public void WaitForExit()
        {
            if (RunCmd("exit"))
            {
                p.WaitForExit();
            }
        }

        /// <summary>
        /// 停止该CMD，如果不准备再次启动，请直接调用Dispose
        /// </summary>
        public void StopCmd()
        {
            if (isStart)
            {
                p.Close();
                isStart = false;
            }
        }

        public void Dispose()
        {
            StopCmd();
            standardOutputRead.Dispose();
            standardErrorRead.Dispose();
        }
    }
}