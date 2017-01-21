﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace wxBot.NET
{
    public class wxbot
    {
        string UNKONWN = "unkonwn";
        string SUCCESS = "200";
        string SCANED = "201";
        string TIMEOUT = "408";

        string uuid = "";
        string redirect_uri = "";
        string base_uri = "";
        string base_host = "";
        string uin = "";
        string sid = "";
        string skey = "";
        string pass_ticket ="";
        string device_id = "e1615250492";     // 'e' + repr(random.random())[2:17]
       
        string base_request="";
        private static Dictionary<string, string> sync_key = new Dictionary<string, string>();
        private static Dictionary<string, string> my_account = new Dictionary<string, string>();
        string sync_key_str = "";
        string sync_host="";
    
        /// <summary>
        /// 当前登录微信用户
        /// </summary>
        private wxUser _me;
        private List<Object> _contact_all = new List<object>();   //完整通讯录

        public  wxbot()
        {
        //    bool DEBUG = false;
        //string uuid = "";
        //string base_uri = "";
        //string base_host = "";
        //string redirect_uri = "";
        //string uin = "";
        //string sid = "";
        //string skey = "";
        //string pass_ticket ="";
        //string device_id = 'e' + repr(random.random())[2:17]
        //string base_request = {}
        //string sync_key_str = "";
        //string sync_key = []
        //string sync_host = "";


        //int batch_count = 50;    //一次拉取50个联系人的信息
        //self.full_user_name_list = []    #直接获取不到通讯录时，获取的username列表
        //self.wxid_list = []   #获取到的wxid的列表
        //self.cursor = 0   #拉取联系人信息的游标
        //self.is_big_contact = False  #通讯录人数过多，无法直接获取
        //#文件缓存目录
        //self.temp_pwd  =  os.path.join(os.getcwd(),'temp')
        //if os.path.exists(self.temp_pwd) == False:
        //    os.makedirs(self.temp_pwd)

        //self.session = SafeSession()
        //self.session.headers.update({'User-Agent': 'Mozilla/5.0 (X11; Linux i686; U;) Gecko/20070322 Kazehakase/0.4.5'})
        //self.conf = {'qr': 'png'}

        //self.my_account = {}  # 当前账户

        //# 所有相关账号: 联系人, 公众号, 群组, 特殊账号
        //self.member_list = []

        //# 所有群组的成员, {'group_id1': [member1, member2, ...], ...}
        //self.group_members = {}

        //# 所有账户, {'group_member':{'id':{'type':'group_member', 'info':{}}, ...}, 'normal_member':{'id':{}, ...}}
        //self.account_info = {'group_member': {}, 'normal_member': {}}

        //self.contact_list = []  # 联系人列表
        //self.public_list = []  # 公众账号列表
        //self.group_list = []  # 群聊列表
        //self.special_list = []  # 特殊账号列表
        //self.encry_chat_room_id_list = []  # 存储群聊的EncryChatRoomId，获取群内成员头像时需要用到

        //self.file_index = 0
          
        }

        public  virtual void handle_msg()
        {
        }

        public void get_contact()
        {
            List<object> contact_all = new List<object>();
            string contact_str=WebUrlRule.WebGet(base_uri + "/webwxgetcontact?pass_ticket=" + pass_ticket + "&skey=" + skey + "&r=" + CommonRule.ConvertDateTimeToInt(DateTime.Now));
           JObject contact_result=JsonConvert.DeserializeObject(contact_str) as JObject;
           if (contact_result != null)
           {               

               foreach (JObject contact in contact_result["MemberList"])  //完整好友名单
               {
                   wxUser user = new wxUser();
                   user.UserName = contact["UserName"].ToString();
                   user.City = contact["City"].ToString();
                   user.HeadImgUrl = contact["HeadImgUrl"].ToString();
                   user.NickName = contact["NickName"].ToString();
                   user.Province = contact["Province"].ToString();
                   user.PYQuanPin = contact["PYQuanPin"].ToString();
                   user.RemarkName = contact["RemarkName"].ToString();
                   user.RemarkPYQuanPin = contact["RemarkPYQuanPin"].ToString();
                   user.Sex = contact["Sex"].ToString();
                   user.Signature = contact["Signature"].ToString();

                   contact_all.Add(user);
               }
           }
           IOrderedEnumerable<object> list_all = contact_all.OrderBy(e => (e as wxUser).ShowPinYin);

           wxUser wx; string start_char;
           foreach (object o in list_all)
           {
               wx = o as wxUser;
               start_char = wx.ShowPinYin == "" ? "" : wx.ShowPinYin.Substring(0, 1);
               if (!_contact_all.Contains(start_char.ToUpper()))
               {
                   _contact_all.Add(start_char.ToUpper());
               }
               _contact_all.Add(o);
           }
        }


        /// <summary>
        /// 主逻辑
        /// </summary>
        public  void run()
        {
            if (!get_uuid())
            {
                Console.WriteLine("登录失败：uuid获取失败");
            }

            gen_qr_code();
            Console.WriteLine("[INFO] Please use WeChat to scan the QR code .");

            string result=wait4login();
            if (result != SUCCESS)
            {
                Console.WriteLine("[ERROR] Web WeChat login failed. failed code="+result);
                return;
            }

            if (login())
            {
                Console.WriteLine("[INFO] Web WeChat login succeed .");
            }
            else
            {
                Console.WriteLine("[ERROR] Web WeChat login failed .");
                return;
            }

            if (init())
            {
                Console.WriteLine("[INFO] Web WeChat init succeed .");
            }
            else
            {
                Console.WriteLine("[INFO] Web WeChat init failed .");
                return;
            }
            get_contact();
            proc_msg();



            Console.ReadKey();
        }
        /// <summary>
        /// 获取本次登录会话ID->uuid
        /// </summary>
        /// <returns></returns>
        public bool get_uuid()
        {
            string url = "https://login.weixin.qq.com/jslogin?appid=wx782c26e4c19acffb&fun=new&lang=zh_CN&_=" + CommonRule.ConvertDateTimeToInt(DateTime.Now);
            string ReturnValue = WebUrlRule.WebGet(url);
            Match match = Regex.Match(ReturnValue, "window.QRLogin.code = (\\d+); window.QRLogin.uuid = \"(\\S+?)\"");
            if (match.Success)
            {
                string code = match.Groups[1].Value;
                uuid = match.Groups[2].Value;
                return code == "200";
            }
            else
                return false;
        }
        /// <summary>
        /// 获取登录二维码
        /// </summary>
        /// <returns></returns>
        public void gen_qr_code()
        {
            string url = "https://login.weixin.qq.com/l/" + uuid;
            Image QRCode=CommonRule.GenerateQRCode(url, Color.Black, Color.White);
            if (QRCode != null)
            {
                QRCode.Save("img\\QRcode.png", System.Drawing.Imaging.ImageFormat.Png);
            }
            System.Diagnostics.Process.Start("img\\QRcode.png", "rundll32.exe C://WINDOWS//system32//shimgvw.dll");            
        }
        /// <summary>
        /// 登录扫描检测
        /// </summary>
        /// <returns></returns>
        public string wait4login()
        {
            //     http comet:
            //tip=1, 等待用户扫描二维码,
            //       201: scaned
            //       408: timeout
            //tip=0, 等待用户确认登录,
            //       200: confirmed
            string tip = "1";
            int try_later_secs = 1;
            int MAX_RETRY_TIMES = 10;
            string code = UNKONWN;
            int retry_time = MAX_RETRY_TIMES;
            string status_code = null;
            string status_data = null;
            while (retry_time > 0)
            {

                string login_result = WebUrlRule.WebGet("https://login.weixin.qq.com/cgi-bin/mmwebwx-bin/login?" + "tip=" + tip + "&uuid=" + uuid + "&_=" + CommonRule.ConvertDateTimeToInt(DateTime.Now));
                Match match = Regex.Match(login_result, "window.code=(\\d+)");
                if (match.Success)
                {
                    status_data = login_result;
                    status_code = match.Groups[1].Value;
                }

                if (status_code == SCANED) //已扫描 未登录
                {
                    Console.WriteLine("[INFO] Please confirm to login .");
                    tip = "0";
                }
                else if (status_code == SUCCESS)  //已扫描 已登录
                {
                    match = Regex.Match(status_data, "window.redirect_uri=\"(\\S+?)\"");
                    if (match.Success)
                    {
                        string _redirect_uri = match.Groups[1].Value + "&fun=new";
                        redirect_uri = _redirect_uri;
                        base_uri = _redirect_uri.Substring(0, _redirect_uri.LastIndexOf('/'));
                        string temp_host = base_uri.Substring(8);
                        base_host = temp_host.Substring(0, temp_host.IndexOf('/'));
                        return status_code;
                    }
                }
                else if (status_code == TIMEOUT)
                {
                    Console.WriteLine("[ERROR] WeChat login exception return_code=" + status_code + ". retry in" + try_later_secs + "secs later...");
                    tip = "1";
                    retry_time -= 1;
                    Thread.Sleep(try_later_secs * 1000);
                }
                else
                {
                    return null;
                }
                Thread.Sleep(800);
            }
            return status_code;
        }
        /// <summary>
        /// 获取skey sid uid pass_ticket  结果存放在cookies中
        /// </summary>
        public bool login()
        {
            if(redirect_uri.Length<4) 
            {
                Console.WriteLine("[ERROR] Login failed due to network problem, please try again.");
                return false;
            }
            string SessionInfo=WebUrlRule.WebGet(redirect_uri);
            pass_ticket = SessionInfo.Split(new string[] { "pass_ticket" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');
            skey = SessionInfo.Split(new string[] { "skey" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');
            sid = SessionInfo.Split(new string[] { "wxsid" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');
            uin = SessionInfo.Split(new string[] { "wxuin" }, StringSplitOptions.None)[1].TrimStart('>').TrimEnd('<', '/');
            if(pass_ticket==""||skey==""|sid==""|uin=="")
            {
                return false;
            }
            base_request="{{\"BaseRequest\":{{\"Uin\":\"{0}\",\"Sid\":\"{1}\",\"Skey\":\"{2}\",\"DeviceID\":\"{3}\"}}}}";
            base_request = string.Format(base_request, uin, sid, skey,device_id);
            return true;
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public bool init()
        {
            string ReturnValue = WebUrlRule.WebPost(base_uri + "/webwxinit?r=" + CommonRule.ConvertDateTimeToInt(DateTime.Now) + "&lang=en_US" + "&pass_ticket=" + pass_ticket, base_request);
            JObject init_result = JsonConvert.DeserializeObject(ReturnValue) as JObject;
            _me = new wxUser();
            _me.UserName = init_result["User"]["UserName"].ToString();
            _me.City = "";
            _me.HeadImgUrl = init_result["User"]["HeadImgUrl"].ToString();
            _me.NickName = init_result["User"]["NickName"].ToString();
            _me.Province = "";
            _me.PYQuanPin = init_result["User"]["PYQuanPin"].ToString();
            _me.RemarkName = init_result["User"]["RemarkName"].ToString();
            _me.RemarkPYQuanPin = init_result["User"]["RemarkPYQuanPin"].ToString();
            _me.Sex = init_result["User"]["Sex"].ToString();
            _me.Signature = init_result["User"]["Signature"].ToString();

            foreach (JObject synckey in init_result["SyncKey"]["List"])  //同步键值
            {
                sync_key.Add(synckey["Key"].ToString(), synckey["Val"].ToString());
            }
            //foreach (JObject _user in init_result["User"])  //同步键值
            //{
            //    //sync_key.Add(_user["Key"].ToString(), _user["Val"].ToString());
            //}
            foreach (KeyValuePair<string, string> p in sync_key)
            {
                sync_key_str += p.Key + "_" + p.Value + "%7C";
            }
            sync_key_str = sync_key_str.TrimEnd('%', '7', 'C');

            return init_result["BaseResponse"]["Ret"].ToString() =="0";
        }
        /// <summary>
        /// 状态通知
        /// </summary>
        /// <returns></returns>
        public void status_notify()
        {
            string ReturnValue = WebUrlRule.WebGet(base_uri + "/webwxstatusnotify?lang=zh_CN&pass_ticket=" + pass_ticket);
           
            //return init_result["BaseResponse"]["Ret"].ToString() == "0";
        }
        /// <summary>
        /// 测试同步检查sync_host
        /// </summary>
        /// <returns></returns>
        public bool test_sync_check()
        {
            string retcode = "";
            sync_host = "webpush." + base_host;
            try
            {
                retcode = sync_check()[0];
            }
            catch
            {
                retcode = "-1";
            }
            if (retcode == "0") return true;
            sync_host = "webpush2." + base_host;
            try
            {
                retcode = sync_check()[0];
            }
            catch
            {
                retcode = "-1";
            }
            if (retcode == "0") return true;
            return false;
        }

        /// <summary>
        /// 同步检查
        /// </summary>
        /// <returns></returns>
       public string[] sync_check()
       {
           string retcode = "";
           string selector = "";

           string _synccheck_url = "https://{0}/cgi-bin/mmwebwx-bin/synccheck?sid={1}&uin={2}&synckey={3}&r={4}&skey={5}&deviceid={6}&_={7}";
           _synccheck_url = string.Format(_synccheck_url, sync_host, sid, uin, sync_key_str, (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds, skey.Replace("@", "%40"), device_id, CommonRule.ConvertDateTimeToInt(DateTime.Now));
           try
           {
               string ReturnValue = WebUrlRule.WebGet(_synccheck_url);
               Match match = Regex.Match(ReturnValue, "window.synccheck=\\{retcode:\"(\\d+)\",selector:\"(\\d+)\"\\}");
               if (match.Success)
               {
                   retcode = match.Groups[1].Value;
                   selector = match.Groups[2].Value;
               }
               return new string[2] { retcode, selector };

           }
           catch
           {
               return new string[2] { "-1", "-1" };
           }
       }

       public JObject sync()
       {
           string sync_json = "{{\"BaseRequest\" : {{\"DeviceID\":\"e1615250492\",\"Sid\":\"{1}\", \"Skey\":\"{5}\", \"Uin\":\"{0}\"}},\"SyncKey\" : {{\"Count\":{2},\"List\":[{3}]}},\"rr\" :{4}}}";
          

           string sync_keys = "";
           foreach (KeyValuePair<string, string> p in sync_key)
           {
               sync_keys += "{\"Key\":" + p.Key + ",\"Val\":" + p.Value + "},";
           }
           sync_keys = sync_keys.TrimEnd(',');
           sync_json = string.Format(sync_json, uin, sid, sync_key.Count, sync_keys, (long)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds, skey);

           if (sid != null && uin != null)
           {
               string sync_str = WebUrlRule.WebPost(base_uri + "/webwxsync?sid=" + sid + "&lang=zh_CN&skey=" + skey + "&pass_ticket=" + pass_ticket, sync_json);
               

               JObject sync_resul = JsonConvert.DeserializeObject(sync_str) as JObject;

               if (sync_resul["SyncKey"]["Count"].ToString() != "0")
               {
                   sync_key.Clear();
                   foreach (JObject key in sync_resul["SyncKey"]["List"])
                   {
                       sync_key.Add(key["Key"].ToString(), key["Val"].ToString());
                   }
                   sync_key_str = "";
                   foreach (KeyValuePair<string, string> p in sync_key)
                   {
                       sync_key_str += p.Key + "_" + p.Value + "%7C";
                   }
                   sync_key_str = sync_key_str.TrimEnd('%', '7', 'C');
               }
               return sync_resul;
           }
           else
           {
               return null;
           }
       }

       public void handle_msg(JObject r)
       {
           //处理原始微信消息的内部函数
           //msg_type_id:
           //    0 -> Init
           //    1 -> Self
           //    2 -> FileHelper
           //    3 -> Group
           //    4 -> Contact
           //    5 -> Public
           //    6 -> Special
           //    99 -> Unknown
           //:param r: 原始微信消息
           foreach (JObject m in r["AddMsgList"])
           {
               string from = m["FromUserName"].ToString();
               string to = m["ToUserName"].ToString();
               string content = m["Content"].ToString();
               string type = m["MsgType"].ToString();

               wxMsg msg = new wxMsg();
               msg.From = from;
               msg.Msg = type == "1" ? content : "请在其他设备上查看消息";  //只接受文本消息
               msg.Readed = false;
               msg.Time = DateTime.Now;
               msg.To = to;
               msg.Type = int.Parse(type);

               if (msg.Type == 51)  //屏蔽一些系统数据
               {
                   continue;
               }
               foreach (Object u in _contact_all)
               {
                   wxUser user = u as wxUser;
                   if (user != null)
                   {
                       if (user.UserName == msg.From && msg.To == _me.UserName)  //接收别人消息
                       {
                           
                           //user.ReceiveMsg(msg);
                           //break;
                       }
                       else if (user.UserName == msg.To && msg.From == _me.UserName)  //同步自己在其他设备上发送的消息
                       {
                       
                           //SendMsg(msg, true);
                           //break;
                       }
                   }
               }
               handle_msg_all(msg);
           }
       }

       public virtual void handle_msg_all(wxMsg msg)
       {
           //处理所有消息，请子类化后覆盖此函数
           //msg:
           //    msg_id  ->  消息id
           //    msg_type_id  ->  消息类型id
           //    user  ->  发送消息的账号id
           //    content  ->  消息内容
           //:param msg: 收到的消息
         
           send_msg_by_uid("test,do not reply");
       }
       public class csMSG
       {
           public int Type { get; set; }
           public string Content { get; set; }
           public string FromUserName { get; set; }
           public string ToUserName { get; set; }
           public string LocalID { get; set; }
           public string ClientMsgId { get; set; }
       }

       public class csBaseRequest
       {
           public string Uin;
           public string Sid;
           public string Skey;
           public string DeviceID;           
       }

        public class message
        {
            public csMSG Msg { get; set; }
            public csBaseRequest BaseRequest { get; set; }           
        }

        
         public void  send_msg_by_uid(string word,string  dst="Teano")
         {
             foreach (Object u in _contact_all)
             {
                 wxUser user = u as wxUser;
                 if (user != null)
                 {
                     if (user.RemarkName == dst || user.NickName==dst )  //接收别人消息
                     {
                         dst = user.UserName;                         
                     }
                 }
             }



           string url = base_uri + "/webwxsendmsg?pass_ticket="+pass_ticket;

           message _message = new message();
           csMSG MSG = new csMSG();
           MSG.Type = 1;
           MSG.FromUserName = _me.UserName;
           MSG.ToUserName = dst;
            Random rd = new Random();
            double a = rd.NextDouble();            
            string para2=a.ToString("f3").Replace(".",string.Empty);
            string para1 = (DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds.ToString("f0");
            string msg_id = para1 + para2;
            word = CommonRule.ConvertGB2312ToUTF8(word);
            MSG.Content = word;
            MSG.LocalID = msg_id;
            MSG.ClientMsgId = msg_id;
            csBaseRequest BaseRequest = new csBaseRequest();
            BaseRequest.Uin = uin;
            BaseRequest.Sid = sid;
            BaseRequest.Skey = skey;
            BaseRequest.DeviceID = device_id;

            _message.Msg = MSG;
            _message.BaseRequest = BaseRequest;

            string jsonStr = JsonConvert.SerializeObject(_message);
            string ReturnVal=WebUrlRule.WebPost2(url, jsonStr);
            ReturnVal = "";
        //msg_id = str(int(time.time() * 1000)) + str(random.random())[:5].replace('.', '')
        //word = self.to_unicode(word)
        //    JObject sync_resul = JsonConvert.SerializeObject(sync_str) as JObject;
        //params = {
        //    'BaseRequest': self.base_request,
        //    'Msg': {
        //        "Type": 1,
        //        "Content": word,
        //        "FromUserName": self.my_account['UserName'],
        //        "ToUserName": dst,
        //        "LocalID": msg_id,
        //        "ClientMsgId": msg_id
        //    }
        //}
        //headers = {'content-type': 'application/json; charset=UTF-8'}
        //data = json.dumps(params, ensure_ascii=False).encode('utf8')
        //try:
        //    r = self.session.post(url, data=data, headers=headers)
        //except (ConnectionError, ReadTimeout):
        //    return False
        //dic = r.json()
        //return dic['BaseResponse']['Ret'] == 0

        }




        public  virtual void schedule()
        {
        //做任务型事情的函数，如果需要，可以在子类中覆盖此函数
        //此函数在处理消息的间隙被调用，请不要长时间阻塞此函数
        } 
       

        /// <summary>
        /// 处理消息
        /// </summary>
       public void proc_msg()
       {
           test_sync_check();
           while (true)
           {
               float check_time = (float)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds;
               try
               {
                 string[] ReturnArray= sync_check();//[retcode, selector] 
                 string retcode=ReturnArray[0];
                 string selector=ReturnArray[1];

                if (retcode == "1100")  //从微信客户端上登出
                    break;
                else if (retcode == "1101") // 从其它设备上登了网页微信
                    break;
                else if (retcode == "0")
                {
                    if(selector == "2")  // 有新消息
                    {
                        JObject r = sync();
                        if (r != null)
                        {
                            handle_msg(r);
                        }
                      
                    }
                    //else if ( selector == "3")  // 未知
                    //{
                    //    r = self.sync()
                    //    if r is not None:
                    //        self.handle_msg(r)
                    //}
                    //elif selector == '4':  # 通讯录更新
                    //    r = self.sync()
                    //    if r is not None:
                    //        self.get_contact()
                    //elif selector == '6':  # 可能是红包
                    //    r = self.sync()
                    //    if r is not None:
                    //        self.handle_msg(r)
                    //elif selector == '7':  # 在手机上操作了微信
                    //    r = self.sync()
                    //    if r is not None:
                    //        self.handle_msg(r)
                    //elif selector == '0':  # 无事件
                    //    pass
                    //else:
                    //    print '[DEBUG] sync_check:', retcode, selector
                    //    r = self.sync()
                    //    if r is not None:
                    //        self.handle_msg(r)
                }
                else
                {
                    //print '[DEBUG] sync_check:', retcode, selector
                    //Thread.Slee
                }
                //self.schedule()
               }
            catch
                   {
                //print '[ERROR] Except in proc_msg'
                //print format_exc()
            }
            check_time = (float)(DateTime.Now.ToUniversalTime() - new System.DateTime(1970, 1, 1)).TotalMilliseconds - check_time;
            if (check_time < 0.8)
                Thread.Sleep((int)(1.0 - check_time)*1000);

           }
       }
       /// <summary>
       /// 发送消息
       /// </summary>
       /// <param name="msg"></param>
       /// <param name="from"></param>
       /// <param name="to"></param>
       /// <param name="type"></param>
       public void SendMsg(string msg, string from, string to, int type)
       {
           string msg_json = "{{" +
           "\"BaseRequest\":{{" +
               "\"DeviceID\" : \"e441551176\"," +
               "\"Sid\" : \"{0}\"," +
               "\"Skey\" : \"{6}\"," +
               "\"Uin\" : \"{1}\"" +
           "}}," +
           "\"Msg\" : {{" +
               "\"ClientMsgId\" : {8}," +
               "\"Content\" : \"{2}\"," +
               "\"FromUserName\" : \"{3}\"," +
               "\"LocalID\" : {9}," +
               "\"ToUserName\" : \"{4}\"," +
               "\"Type\" : {5}" +
           "}}," +
           "\"rr\" : {7}" +
           "}}";

           string _sendmsg_url = "https://wx.qq.com/cgi-bin/mmwebwx-bin/webwxsendmsg?sid=";

           if (sid != null && uin != null)
           {
               msg_json = string.Format(msg_json, sid, uin, msg, from, to, type, skey, DateTime.Now.Millisecond, DateTime.Now.Millisecond, DateTime.Now.Millisecond);

               string send_result = WebUrlRule.WebPost(_sendmsg_url + sid + "&lang=zh_CN&pass_ticket=" + pass_ticket, msg_json);
              
           }
       }   
          
        
    }
}
