# 基于WebSocket的远程控制管理软件

>源码及思路并非原创，属于踩在大佬肩膀二次开发之作，仅限技术交流使用。

**前情提要**

在t00ls看到@xiaoniu大佬发布的[《web远控 C#客户端 过全球杀毒。》](https://www.t00ls.net/thread-56617-1-1.html)[《WEB远控,基于workerman多客户端即时传输》](https://www.t00ls.net/thread-56422-1-1.html)，觉得这是一个不错的思路，可以为自己所用，于是乎便有了这篇帖子。由于只是写为己用，所以直接之前的源码上做的改动及添加，当然整个项目如果我有空还是打算重构的，以提高自身代码水平，如有不恰当处请与我联系。
开源地址[WebSocketRemoteControl](https://github.com/MrFooL137/WebSocketRemoteControl)，有任何意见或者建议都可以提出，有时间我就改改。


## 服务端的使用及功能演示

以Windows示例，安装php并配置好环境变量，运行服务端目录下**start_for_win.bat**，Linux运行命令即为bat文件中所运行的命令。

使用浏览器访问地址http://127.0.0.1:1234 ，输入管理员密码admin，进入web服务端。**默认端口:1234 可在此文件中修改[Applications\Chat\start_web.php] 管理员密码:admin 可在此文件中修改[Applications\Chat\Events.php]**

当客户端上线时，在下拉列表中选择要操控的客户端，并在下方文本框中输入要执行的cmd命令，按回车执行。


## 客户端的生成及功能演示

使用Visual Studio打开websocket.sln，找到Example.cs，修改RootPath、ServiceName、startupCode、ip这四项参数，然后生成客户端，会在**[websocket\bin\Debug]**目录下生成WSS.exe，将此程序在目标机器中打开，程序将释放在指定位置并删除自身尝试自启动（可能会被360拦截，虚拟机测试添加计划任务未拦截，本机测试前几次未拦截之后均拦截），随后客户端上线。
