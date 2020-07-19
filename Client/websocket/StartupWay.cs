using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TaskScheduler;

namespace websocket
{
    class StartupWay
    {
        /*******************************************************************************
         * Copy From https://blog.csdn.net/arrowzz/article/details/69808761
         * Modify By MrFooL
         * 引用 COM: TaskScheduler & Windows Script Host Object Model
         *******************************************************************************/
        public bool doStartUpWay(int startupCode, string RootPath, string ServiceName)
        {
            string FullServicePath = RootPath + ServiceName + ".exe";
            switch (startupCode)
            {
                case 0:
                    // 获取当前登录用户的 开始 文件夹位置
                    string nowUserPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    FileStartUp(nowUserPath, ServiceName, FullServicePath);
                    // 获取全局开始文件夹位置 [全局开始文件夹存在权限问题可能导致添加启动项失败 后期可增加提权]
                    //string allUserPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
                    //FileStartUp(allUserPath, ServiceName, FullServicePath);
                    break;
                case 1:
                    // 添加到 当前登陆用户的 注册表启动项
                    RegistryKey RKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                    RKey.SetValue(ServiceName, @"""" + FullServicePath + @"""");//防止生成路径带有空格导致的启动失败

                    // 添加到 所有用户的 注册表启动项 [权限问题]
                    //RegistryKey RKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                    //RKey.SetValue(ServiceName, @""""+ FullServicePath + @"""");//防止生成路径带有空格导致的启动失败
                    break;
                case 2:
                    string author = ServiceName;
                    string desc = ServiceName;
                    string name = ServiceName;
                    string file = FullServicePath;
                    //新建任务
                    TaskSchedulerClass scheduler = new TaskSchedulerClass();
                    //连接
                    scheduler.Connect(null, null, null, null);
                    //获取创建任务的目录
                    ITaskFolder folder = scheduler.GetFolder("\\");
                    //设置参数
                    ITaskDefinition task = scheduler.NewTask(0);
                    task.RegistrationInfo.Author = author;//创建者
                    task.RegistrationInfo.Description = desc;//描述
                    //设置触发机制（此处是 登陆后）
                    task.Triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON);
                    //设置动作（此处为运行exe程序）
                    IExecAction action = (IExecAction)task.Actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
                    action.Path = file;//设置文件目录
                    task.Settings.ExecutionTimeLimit = "PT0S"; //运行任务时间超时停止任务吗? PT0S 不开启超时
                    task.Settings.DisallowStartIfOnBatteries = false;//只有在交流电源下才执行
                    task.Settings.RunOnlyIfIdle = false;//仅当计算机空闲下才执行

                    IRegisteredTask regTask =
                        folder.RegisterTaskDefinition(name, task,//此处需要设置任务的名称（name）
                        (int)_TASK_CREATION.TASK_CREATE, null, //user
                        null, // password
                        _TASK_LOGON_TYPE.TASK_LOGON_INTERACTIVE_TOKEN,
                        "");
                    IRunningTask runTask = regTask.Run(null);
                    //注意：任务计划需要添加引用，在 Com 中搜索 TaskScheduler，添加即可，并且要在“嵌入互操作类型”设置为false，使程序编译时，能从互操作程序集中获取 COM 类型的类型信息。
                    break;
            }
            return true;
        }

        public static bool FileStartUp(string directory, string shortcutName, string targetPath, string description = null, string iconLocation = null)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                //添加引用 Com 中搜索 Windows Script Host Object Model
                string shortcutPath = Path.Combine(directory, string.Format("{0}.lnk", shortcutName));
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);//创建快捷方式对象
                shortcut.TargetPath = targetPath;//指定目标路径
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);//设置起始位置
                shortcut.WindowStyle = 1;//设置运行方式，默认为常规窗口
                shortcut.Description = description;//设置备注
                shortcut.IconLocation = string.IsNullOrWhiteSpace(iconLocation) ? targetPath : iconLocation;//设置图标路径
                shortcut.Save();//保存快捷方式

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
