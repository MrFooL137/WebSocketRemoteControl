<?php
/**
 * This file is part of workerman.
 *
 * Licensed under The MIT License
 * For full copyright and license information, please see the MIT-LICENSE.txt
 * Redistributions of files must retain the above copyright notice.
 *
 * @author walkor<walkor@workerman.net>
 * @copyright walkor<walkor@workerman.net>
 * @link http://www.workerman.net/
 * @license http://www.opensource.org/licenses/mit-license.php MIT License
 */

/**
 * https://www.t00ls.net/space-uid-11943.html
 * BY:小牛
 */
//declare(ticks=1);

/**
 * 聊天主逻辑
 * 主要是处理 onMessage onClose 
 */
use \GatewayWorker\Lib\Gateway;

class Events
{

   /**
    * 有消息时
    * @param int $client_id
    * @param mixed $message
    */
   public static function onMessage($client_id, $message)
   {

        // debug
        echo "client:{$_SERVER['REMOTE_ADDR']}:{$_SERVER['REMOTE_PORT']} gateway:{$_SERVER['GATEWAY_ADDR']}:{$_SERVER['GATEWAY_PORT']}  client_id:$client_id session:".json_encode($_SESSION)." onMessage:".$message."\n";
        
        // 客户端传递的是json数据
        $message_data = json_decode($message, true);

        if(!$message_data)
        {
            return ;
        }
        
        // 根据类型执行不同的业务
        switch($message_data['type'])
        {
            // 客户端回应服务端的心跳
            case 'pong':
                return;
            // 客户端登录 message格式: {type:login, name:xx, room_id:1} ，添加到客户端，广播给所有客户端xx进入聊天室


            case 'control'://像客户端发送命令
                if(!isset($message_data['to_client_id']))
                {
                    return;
                }
                if($message_data['execute']=='start'){
                $new_message = array(
                    'type'=>'control',
                    'execute'=>'start');
                }elseif ($message_data['execute']=='ctrlc'){
                    $new_message = array(
                        'type'=>'control',
                        'execute'=>'ctrlc');
                }elseif ($message_data['execute']=='exit'){
                    $new_message = array(
                        'type'=>'control',
                        'execute'=>'exit');
                }

                return Gateway::sendToClient($message_data['to_client_id'], json_encode($new_message));
               // $new_message['content'] = nl2br(htmlspecialchars($message_data['content']));
               // return Gateway::sendToCurrentClient(json_encode($new_message));

            case 'admin_login'://管理员登录
            if($message_data['login']!='admin')   return;
                // 获取客户组
                $_SESSION['client_name'] = 'admin'; //定义管理员名字
                $clients_list = Gateway::getClientSessionsByGroup('kehu');
                foreach($clients_list as $tmp_client_id=>$item)
                {
                    $clients_list[$tmp_client_id] = $item['client_name'];
                }
                Gateway::joinGroup($client_id, 'admin'); //吧管理员加入管理员组

                // 给当前用户发送用户列表
                $new_message = array('type'=>'login');
                $new_message['client_list'] = $clients_list;


                Gateway::sendToCurrentClient(json_encode($new_message));
                return;
            case 'login':  //客户端登录

                $_SESSION['client_name'] = htmlspecialchars($message_data['client_name']).'['.$_SERVER['REMOTE_ADDR'].']'; //定义客户名字
                //获取管理员列表
                $new_message = array('type'=>$message_data['type'], 'client_id'=>$client_id, 'client_name'=>htmlspecialchars($_SESSION['client_name']), 'time'=>date('Y-m-d H:i:s'));
                Gateway::sendToGroup('admin', json_encode($new_message));//像管理员组发送新加入用户



                 Gateway::joinGroup($client_id, 'kehu');
               
                // 给当前用户发送用户列表 
               // $new_message['client_list'] = $clients_list;
               // Gateway::sendToCurrentClient(json_encode($new_message));
                return;
                
            // 客户端发言 message: {type:say, to_client_id:xx, content:xx}
            case 'say': //客户发送数据,只能通知给管理员
                // 非法请求

               // echo '客户发言了';
                $new_message = array(
                    'type'=>'say', 
                    'from_client_id'=>$client_id,
                   // 'from_client_name' =>$_SESSION['client_name'],
                    //'to_client_id'=>'all',
                    'content'=>nl2br(htmlspecialchars($message_data['content'])),
                    'time'=>date('Y-m-d H:i:s'),
                );
                return Gateway::sendToGroup('admin' ,json_encode($new_message));

            case 'admin_say':
                // 像客户发送
              if(!isset($message_data['to_client_id']))
                {
                    return;
                }

                $new_message = array(
                    'type'=>'say',
                    'content'=>nl2br(htmlspecialchars($message_data['content'])),
                );
                return Gateway::sendToClient($message_data['to_client_id'], json_encode($new_message));

               // $new_message['content'] = nl2br(htmlspecialchars(base64_encode($message_data['content'])));
                //return Gateway::sendToCurrentClient(json_encode($new_message));


        }
   }
   
   /**
    * 当客户端断开连接时
    * @param integer $client_id 客户端id
    */
   public static function onClose($client_id)
   {

       // debug
      // echo $_SESSION['client_name']. "client_id:$client_id client_name: onClose:''\n";
       
       // 从房间的客户端列表中删除

       if($_SESSION['client_name'] != 'admin')
       {
           $new_message = array('type'=>'logout', 'from_client_id'=>$client_id, 'from_client_name'=>$_SESSION['client_name'], 'time'=>date('Y-m-d H:i:s'));
           Gateway::sendToGroup('admin', json_encode($new_message));
       }
   }
  
}
