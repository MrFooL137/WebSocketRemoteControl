WebSocketRemoteControl-Server
=======
基于workerman的GatewayWorker框架开发的一款高性能支持分布式部署的远程控制系统控制端。

GatewayWorker框架文档：http://www.workerman.net/gatewaydoc/

配置
======
默认端口:1234 可在此文件修改[Applications\Chat\start_web.php]
管理员密码:admin 可在此文件修改[Applications\Chat\Events.php]

下载安装
=====
1、git clone https://github.com/walkor/workerman-chat

2、composer install

启动停止(Linux系统)
=====
以debug方式启动  
```php start.php start  ```

以daemon方式启动  
```php start.php start -d ```

启动(windows系统)
======
双击start_for_win.bat  

注意：  
windows系统下无法使用 stop reload status 等命令  
如果无法打开页面请尝试关闭服务器防火墙  

测试
=======
浏览器访问 http://服务器ip或域:1234,例如http://127.0.0.1:1234