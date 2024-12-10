using System;
using System.Net;
//using System.Net.Sockets;//web服务
using System.Threading;//线程
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using static windowOP.DatabaseOP;
using Microsoft.VisualBasic.ApplicationServices;
using System.Security.Policy;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.Xml;

namespace windowOP
{
    public class WebServer
    {
        public static string ListenData = $"http://127.0.0.1:{ListenPort}/";

        public static void webServer()
        {

            if (Setting_Read("ServeAllPrefix") == "1")
            {
                if (Setting.IsUserAdministrator())
                {
                    ListenData = $"http://*:{ListenPort}/";
                    Setting.AccessFireWall();
                    Console.WriteLine("具有管理员权限，全前缀监听");
                }
                else
                {
                    Console.WriteLine("权限不够,无法提供全前缀服务");
                }
            }

            HttpListener listener = new HttpListener();


            try
            {
                listener.Prefixes.Add(ListenData); // 创建HttpListener实例并指定监听的前缀


                    listener.Start();
                    Console.WriteLine($"网页服务启动，监听前缀:{ListenData}");

                    int maxThreads = Environment.ProcessorCount * 4;  // 设置线程池的最大线程数
                    ThreadPool.SetMaxThreads(maxThreads, maxThreads);
                    ThreadPool.SetMinThreads(2, 2); // 设置线程池的最小线程数

                    while (true)
                    {
                        // 获取HttpListenerContext对象并将处理请求的方法放入线程池进行处理
                        HttpListenerContext context = listener.GetContext();
                        if (context.Request.IsWebSocketRequest)
                            ThreadPool.QueueUserWorkItem(HandleWebSocket, context);
                        else
                            ThreadPool.QueueUserWorkItem(HandleRequest, context);
                    }
                }
            catch (Exception ex)
            {
                Console.WriteLine($"HttpListenerException: {ex.Message}");

                listener.Stop();
                listener.Close();

                Thread.Sleep(1000);

                Task.Run(() => webServer());
                    return;

            }
            
        }

        public static JsonDocument? TryParseJson(string jsonString)
        {
            try
            {
                // 尝试解析 JSON 字符串
                return JsonDocument.Parse(jsonString);
            }
            catch (JsonException)
            {
                // 如果发生解析异常，返回 null
                return null;
            }
        }







public static string? GetJsonProperty(JsonDocument jsonDoc, string propertyName)
    {
        try
        {
            if (jsonDoc.RootElement.TryGetProperty(propertyName, out JsonElement property))
            {
                // 检查属性的类型
                if (property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString(); // 如果是字符串类型，返回其值
                }
                else if (property.ValueKind == JsonValueKind.Array)
                {
                    // 如果是数组类型，可以将数组转换为 JSON 字符串返回
                    return property.ToString();
                }
                // 可以在这里添加对其他类型（例如数字、布尔值、对象等）的处理
            }
            // 如果属性未找到或不是字符串类型，返回 null
            return null;
        }
        catch (Exception exception)
        {
                DatabaseOP.Log(exception.ToString());
            return null; // 如果发生异常，返回 null
        }
    }

    static void SendMsg(string msg, WebSocket ws, string ip)
        {
            ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
            DatabaseOP.LogWebServer($"对{ip}发送WebSocket消息：{msg}");
        }

        static void SendCrypted(string msg, WebSocket ws, string key, string iv, string ip)
        {
            string? CryptedMsg = Crypto.AES_en(msg, key, iv);
            if(CryptedMsg == null) return;
            JObject sendJson = new JObject();
            sendJson["Operation"] = "CryptedMsg";
            sendJson["CryptedMsg"] = CryptedMsg;
            SendMsg(sendJson.ToString(), ws, ip);
        }

    static void SendErr(string msg, WebSocket ws, string ip)
        {
            JObject sendJson = new JObject();
            sendJson["Operation"] = "Error";
            sendJson["msg"] = msg;
            SendMsg(sendJson.ToString(), ws ,ip );
        }
    
    

        async private static void HandleWebSocket(object state)
        {
            try
            {
                HttpListenerContext context = (HttpListenerContext)state;

                // 进行 WebSocket 握手
                WebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
                string clientIp = context.Request.RemoteEndPoint.Address.ToString();
                DatabaseOP.LogWebServer($"来自{clientIp}的WebSocket已连接.");

                // 使用 WebSocket 进行通信
                var webSocket = webSocketContext.WebSocket;

                var buffer = new byte[1024];
                byte[] responseBuffer;

                var (RSA_sk, RSA_pk) = Crypto.GetRSAKey();

                string AES_key = Setting_Read("Password");
                string AES_iv = "";

                JObject AnsJson = new JObject();

                AnsJson["Operation"] = "IniKey";
                AnsJson["RSA_pk"] = RSA_pk;
                AnsJson["HmacKey"] = GetHmacKey();

                SendMsg(AnsJson.ToString(), webSocket, clientIp);
                AnsJson.RemoveAll();

                while (true)
                {
                    ArraySegment<byte> segment = new ArraySegment<byte>(buffer);
                    var result = webSocket.ReceiveAsync(segment, CancellationToken.None).Result;

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait();
                        DatabaseOP.LogWebServer($"来自{clientIp}的WebSocket连接断开");
                        break;
                    }//断连逻辑

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);


                    if(AES_iv == "" || AES_key == "")
                    {
                        string? oriText = Crypto.RSA_de(receivedMessage, RSA_sk);
                        if (oriText == null) 
                        {
                            SendErr("信息无法解密", webSocket, clientIp);
                            continue;
                        }
                        DatabaseOP.LogWebServer($"来自{clientIp}的WebSocket消息: " + oriText);
                        if (Setting_Read("Password") == "" || Setting_Read("HmacKey") == "")
                        {
                            JsonDocument? PwdData = TryParseJson(oriText);
                            if (PwdData == null)
                            {
                                SendErr("需要提供json格式的密码数据", webSocket, clientIp);
                                continue;
                            }
                            string? HashedPwd = GetJsonProperty(PwdData, "HashedPwd");
                            string? HmacKey = GetJsonProperty(PwdData, "HmacKey");
                            if (HashedPwd == null || HmacKey == null)
                            {
                                SendErr("需要提供参数HashedPwd和HmacKey", webSocket, clientIp);
                                continue;
                            }
                            SetPassword(HashedPwd, HmacKey);
                            AES_key = Setting_Read("Password");

                            AnsJson["Operation"] = "IniKey";
                            AnsJson["RSA_pk"] = RSA_pk;
                            AnsJson["HmacKey"] = GetHmacKey();

                            SendMsg(AnsJson.ToString(), webSocket, clientIp);
                            AnsJson.RemoveAll();
                        }
                        else
                        { 
                            if(oriText.Length != 16)
                            {
                                SendErr("iv需要长度为16的字符串", webSocket, clientIp);
                                continue;
                            }
                            AES_iv = oriText;
                            SendMsg("{\"Operation\": \"ivReceived\"}", webSocket, clientIp);
                        }
                        continue;
                    }


                    string? oriJson = Crypto.AES_de(receivedMessage, AES_key, AES_iv);
                    if (oriJson == null)
                    {
                        SendErr("信息无法解密", webSocket, clientIp);
                        continue;
                    }

                    DatabaseOP.LogWebServer($"来自{clientIp}的WebSocket消息: " + oriJson);

                    JsonDocument? jsonDoc = TryParseJson(oriJson);
                    if (jsonDoc == null)
                    {
                        webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("消息格式有误")), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                        continue;
                    }
                    string? operation = GetJsonProperty(jsonDoc, "Operation");
                    

                    if(operation == "Verify")
                    {
                        SendCrypted("Verified", webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "ReadToMemory")
                    {
                        string? item = GetJsonProperty(jsonDoc, "Item");
                        if (item == null)
                        {
                            webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("item不能设置为空")), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                            continue;
                        }
                        else if (item == "TargetList") ReadToMemory_TargetList();

                        else
                        {
                            string ans = ReadToMemory(item);
                            SendCrypted(ans, webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        SendCrypted("读取完成", webSocket, AES_key, AES_iv, clientIp);
                        continue;
                    }

                    else if (operation == "GetProgramPath")
                    {
                        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        bool isAppData = Directory.GetParent(Setting.programDir).FullName.Equals(appDataPath, StringComparison.OrdinalIgnoreCase);
                        string ansData = Setting.programDir;
                        if (isAppData) ansData = "%appdata%";
                        ansData = "ProgramPath:" + ansData;
                        SendCrypted(ansData, webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "RunAction")
                    {
                        string? ActionCalls = GetJsonProperty(jsonDoc, "Action");

                        Actions action = new Actions();
                        Task.Run(() => action.ExecuteActions(ActionCalls));

                        continue;
                    }

                    else if (operation == "Setting_Read_Batch")
                    {
                        string? item = GetJsonProperty(jsonDoc, "Item");
                        string query = $"SettingQueryResult:{Setting_Read_Batch()}";
                        SendCrypted(query, webSocket, AES_key, AES_iv, clientIp);
                        continue;
                    }

                    else if (operation == "Setting_Write")
                    {
                        string? item = GetJsonProperty(jsonDoc, "Item");
                        string? value = GetJsonProperty(jsonDoc, "Value");
                        Setting_Write(item, value);
                    }

                    else if(operation == "GetTextFronUrl")
                    {
                        string? Url = GetJsonProperty(jsonDoc, "Url");
                        
                        if (Url == null)
                        {
                            SendCrypted("url不能为空", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        string? Ans = Setting.GetStringFromUrl(Url);
                        if (Ans == null)
                        {
                            SendCrypted("获取内容失败", webSocket, AES_key, AES_iv, clientIp);
                            continue ;
                        }

                        var ansData = new
                        {
                            Url,
                            Ans
                        };

                        string ansJson = "GetTextFronUrl:" + JsonSerializer.Serialize(ansData);
                        SendCrypted(ansJson, webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if(operation == "TransferDirectory")
                    {
                        string? filePath = GetJsonProperty(jsonDoc, "filePath");
                        if(filePath == null)
                        {
                            SendCrypted("需要提供目标路径", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        string ans = Setting.Version.TransferDirectory(filePath);
                        SendCrypted(ans, webSocket, AES_key, AES_iv, clientIp);

                        if(ans == "脚本部署完成，正在转移")
                        {
                            Thread.Sleep(5000);
                            Actions.exit();
                        }

                    }

                    else if (operation == "ChangePassword")
                    {
                        string? OldPassword = GetJsonProperty(jsonDoc, "OldPassword");
                        string? NewPassword = GetJsonProperty(jsonDoc, "NewPassword");
                        string? HmacKey = GetJsonProperty(jsonDoc, "HmacKey");
                        
                        if (OldPassword == null || NewPassword == null || HmacKey == null)
                        {
                            SendCrypted("有参数为空", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        if (!DatabaseOP.VerifyPassword(OldPassword))
                        {
                            SendCrypted("ShowAlert:旧密码输入错误", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        DatabaseOP.SetPassword(NewPassword, HmacKey);
                        SendCrypted("ShowAlert:密码更改成功", webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "AddTarget")
                    {
                        string? TargetSign = GetJsonProperty(jsonDoc, "TargetSign");
                        string? PassingSign = GetJsonProperty(jsonDoc, "PassingSign");
                        string? Action = GetJsonProperty(jsonDoc, "Action");
                        string? ApplicationName = GetJsonProperty(jsonDoc, "ApplicationName");

                        if (TargetSign == null || PassingSign == null || Action == null || ApplicationName == null)
                        {
                            SendCrypted("有参数为空", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        DatabaseOP.InsertTarget(TargetSign, PassingSign, ApplicationName, Action);
                        SendCrypted("ShowAlert:插入成功", webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "DelTarget")
                    {
                        string? Id = GetJsonProperty(jsonDoc, "Id");
                        if (int.TryParse(Id, out int id))// input 是纯数字，可以转换为 int
                        {
                            DatabaseOP.DeleteTarget(id);
                            SendCrypted("ShowAlert:删除成功", webSocket, AES_key, AES_iv, clientIp);
                        }
                        else
                            SendCrypted("ShowAlert:需要数字ID作为参数", webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "UpdateTarget")
                    {
                        int id;
                        string? Id = GetJsonProperty(jsonDoc, "Id");
                        string? TargetSign = GetJsonProperty(jsonDoc, "TargetSign");
                        string? PassingSign = GetJsonProperty(jsonDoc, "PassingSign");
                        string? Action = GetJsonProperty(jsonDoc, "Action");
                        string? ApplicationName = GetJsonProperty(jsonDoc, "ApplicationName");


                        if (Id == null || !int.TryParse(Id, out id))// input 是纯数字，可以转换为 int
                        {
                            SendCrypted("参数Id需要为数字", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        if (TargetSign == null || Action == null || ApplicationName == null)
                        {
                            SendCrypted("有参数为空", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        DatabaseOP.UpdateTarget(id, TargetSign, PassingSign, ApplicationName, Action);
                        SendCrypted("ShowAlert:更改成功", webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "EnableTarget")
                    {
                        int id, enable;
                        string? Id = GetJsonProperty(jsonDoc, "Id");
                        string? Enable = GetJsonProperty(jsonDoc, "Enable");
                        if (int.TryParse(Id, out int Id_ToInt))// input 是纯数字，可以转换为 int
                            id = Id_ToInt;
                        else
                        {
                            SendCrypted("参数Id需要为数字", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        if (int.TryParse(Enable, out int Enable_ToInt))// input 是纯数字，可以转换为 int
                            enable = Enable_ToInt;
                        else
                        {
                            SendCrypted("参数Id需要为数字", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        DatabaseOP.EnableTarget(id, enable);
                        string msg = "ShowAlert:启用成功";
                        if (enable == 0) msg = "ShowAlert:禁用成功";
                        SendCrypted(msg, webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "QuiryTargetList")
                    {
                        SendCrypted("TargetList: " + GetTargetList(), webSocket, AES_key, AES_iv, clientIp);
                    }
                    else
                        SendCrypted("API不存在", webSocket, AES_key, AES_iv, clientIp);

                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"webSocket处理错误：{exception.ToString()}");
                return;
            }
        }

        static void HandleRequest(object state)
        {
            try
            {
                HttpListenerContext context = (HttpListenerContext)state;
                string requestUrl = context.Request.Url.LocalPath.Substring(1);
                if (requestUrl == "" || requestUrl == "debug") requestUrl = "index.html";
                string filePath = Path.Combine(Setting.programDir, "webFile", requestUrl);
                DatabaseOP.LogWebServer("http请求了目录" + filePath);
                if (File.Exists(filePath))
                {
                    // 根据文件类型设置ContentType
                    string contentType = "text/html";
                    if (filePath.EndsWith(".js"))
                    {
                        contentType = "application/javascript";
                    }
                    else if (filePath.EndsWith(".css"))
                    {
                        contentType = "text/css";
                    }
                    else if (filePath.EndsWith(".png") || filePath.EndsWith(".jpg") || filePath.EndsWith(".jpeg"))
                    {
                        contentType = "image/jpeg";
                    }
                    // 设置Response的ContentType和ContentLength
                    context.Response.ContentType = contentType;

                    byte[] buffer = File.ReadAllBytes(filePath);

                    context.Response.ContentLength64 = buffer.Length;

                    using (Stream output = context.Response.OutputStream)
                    {
                        output.Write(buffer, 0, buffer.Length);
                    }
                }
                else
                {
                    // 文件不存在时返回404错误页面
                    string html = @"<html><body><h1>404 Page Not Found</h1></body></html>";
                    byte[] buffer = Encoding.UTF8.GetBytes(html);

                    context.Response.ContentType = "text/html";
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;

                    using (Stream output = context.Response.OutputStream)
                    {
                        output.Write(buffer, 0, buffer.Length);
                    }
                }

                context.Response.Close();
            }
            catch (Exception ex)
            {
                DatabaseOP.LogErr($"处理HTTP请求时发生错误： {ex.Message}");
                return;
            }
            
        }



    }
}
