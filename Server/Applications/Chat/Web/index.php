<!DOCTYPE html>
<html>
	<head>
		<meta charset="UTF8" />
		<title>Controler</title>
        <link href="/css/cmd.css" rel="stylesheet">
        <script type="text/javascript" src="js/base64.js"></script>
        <script type="text/javascript" src="js/jquery-3.2.1.js"></script>
		<script type="text/javascript">

                // 创建websocket

                WEB_SOCKET_DEBUG = true;
                var ws, name, client_list={};
                //var base = new Base64();
                // 连接服务端
                select_client_id = 'all';
                var base = new Base64();



                connect();
                function connect() {
                    // 创建websocket
                    ws = new WebSocket("ws://"+document.domain+":7272");
                    // 当socket连接打开时，输入用户名
                    ws.onopen = onopen;
                    // 当有消息时根据消息类型显示不同信息
                    ws.onmessage = onmessage;
                    ws.onclose = function() {
                        console.log("连接关闭，定时重连");
                        connect();
                    };
                    ws.onerror = function() {
                        console.log("出现错误");
                    };
                }
                function onmessage(e)
                {
                    console.log(e.data);
                    var data = JSON.parse(e.data);
                    switch(data['type']){
                        // 服务端ping客户端
                        case 'ping':
                            ws.send('{"type":"pong"}');
                            break;;
                        // 登录 更新用户列表
                        case 'login':
                           if(data['client_name'])notify(data['client_name']+' 上线了');

                            if(data['client_list'])
                            {
                                client_list = data['client_list'];
                            }
                            else
                            {
                                client_list[data['client_id']] = data['client_name'];
                            }
                            flush_client_list();

                            break;
                        // 发言
                        case 'say':
                            if(data['from_client_id']==select_client_id){
                             say(base.decode(data['content']));
                            }else{
                                notify(data['from_client_name']+'有命令回执');
                            }
                            break;
                        // 用户退出 更新用户列表
                        case 'logout':
                            //{"type":"logout","client_id":xxx,"time":"xxx"}
                            notify(data['from_client_name']+' 下线了');
                            delete client_list[data['from_client_id']];
                            flush_client_list();
                    }
                }


                function flush_client_list(){
                    var client_list_slelect = $("#client_list");
                    client_list_slelect.empty();
                    client_list_slelect.append('<option value="all" id="cli_all">未选择用户</option>');
                    for(var p in client_list){
                        client_list_slelect.append('<option value="'+p+'">'+client_list[p]+'</option>');
                    }
                    $("#client_list").val(select_client_id);
                }

                function onopen()
                             {
                            if(!name)
                            {
                                show_prompt();
                          }
                // 登录

                var login_data = '{"type":"admin_login","login":"admin"}';
                console.log("websocket握手成功，发送登录数据:"+login_data);
                ws.send(login_data);
            }
            function show_prompt(){
                name = prompt('输入你的密码：', '');
                if(!name || name=='null'){
                    show_prompt();
                }
            }

        function say(content){
            $("#now").remove();//移除
            $("#area").append(content+"<input type='text' id='now' class='now'>")
            $("#now").focus();

        }
                function notify(content) {
                    if (Notification && Notification.permission == "granted") {
                        var instance = new Notification(
                            "提醒", {
                                body: content,
                            })
                    }
                }

        $(function(){
            $("#client_list").change(function(){
               select_client_id = $("#client_list option:selected").attr("value");
                ws.send('{"type":"control","execute":"start","to_client_id":"'+select_client_id+'"}');
            });
            $('#area').on('keypress','.now',function(event){
                var abcd=$("#now").val();
                if(event.keyCode==13){
                    if(select_client_id=='all'){
                        alert("请选择用户！");
                        return;
                    }
                    ws.send('{"type":"admin_say","to_client_id":"'+select_client_id+'","content":"'+base.encode(abcd)+'"}');
                }
            });

           var Notification = window.Notification || window.mozNotification || window.webkitNotification;


                if (Notification) {
                    Notification.requestPermission(function (permission) {
                    });
                }

        });



      </script>
	</head>
	<body id="text">
    <div class="top">
            <div class="user_kehu">
                <select  id="client_list">
                    <option value="all">请选择用户</option>
                </select>
            </div>
    </div>
	<div id="area" class="cmd">

		<div id="line" class="cmd"><hr size=1></div>

        <input type="text" id="now" class="now">
	</div>
	</body>
</html>
