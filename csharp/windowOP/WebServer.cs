using Microsoft.VisualBasic.ApplicationServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RemoteDesktopServer;
using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Forms;
using static windowOP.DatabaseOP;

namespace windowOP
{
    public class WebServer
    {
        public static string ListenData = $"http://127.0.0.1:{ListenPort}/";

        public static async Task webServer()
        {

            if (Setting_Read("ServeAllPrefix") == "1")
            {
                if (Setting.IsUserAdministrator())
                {
                    ListenData = $"http://*:{ListenPort}/";
                    Setting.AccessFireWall(ListenPort);
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

                while (true)
                {
                    try
                    {
                        // ⚡ 非阻塞等待新连接
                        HttpListenerContext context = await listener.GetContextAsync();

                        // 立即派发到线程池（或直接 async 处理）
                        _ = Task.Run(() => ProcessRequest(context));
                    }
                    catch (HttpListenerException ex) when (ex.ErrorCode == 995) // 监听器已关闭
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"接受请求时出错: {ex}");
                        // 不应重启整个服务器！记录日志即可
                    }
                }
             }

            catch (Exception ex)
            {
                Console.WriteLine($"HttpListenerException: {ex}");

                listener.Stop();
                listener.Close();

                Thread.Sleep(1000);

                Task.Run(() => webServer());
                    return;

            }
            
        }

        private static void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                if (context.Request.IsWebSocketRequest)
                    HandleWebSocket(context);
                else
                    HandleRequest(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理请求失败: {ex}");
                // 尝试返回 500
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch { /* 忽略 */ }
            }
        }


        public static JsonDocument? TryParseJson(string jsonString)
        {
            try
            {
                // 尝试解析 JSON 字符串
                return JsonDocument.Parse(jsonString);
            }
            catch (Exception)
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
        }

        static void SendCrypted(string msg, WebSocket ws, string key, string iv, string ip)
        {
            string? CryptedMsg = Crypto.AES_en(msg, key, iv);
            if(CryptedMsg == null) return;
            JObject sendJson = new JObject();
            sendJson["Operation"] = "CryptedMsg";
            sendJson["CryptedMsg"] = CryptedMsg;
            SendMsg(sendJson.ToString(), ws, ip);
            DatabaseOP.LogWebServer($"对{ip}发送WebSocket消息：{msg}");
        }

        //用于正常流程不会出现的错误，在web控制台显示
        static void SendErr(string message, WebSocket ws, string key, string iv, string ip)
        {
            JObject sendJson = new JObject();
            sendJson["Operation"] = "Error";
            sendJson["message"] = message;
            SendCrypted(sendJson.ToString(), ws, key, iv, ip);
        }

        //给普通用户显示的提示
        static void SendNotification(string title, string message, string type, WebSocket ws, string key, string iv, string ip)
        {
            JObject sendJson = new JObject();
            sendJson["Operation"] = "Notification";
            sendJson["title"] = title;
            sendJson["message"] = message;
            sendJson["type"] = type;
            SendCrypted(sendJson.ToString(), ws, key, iv, ip);
        }

        public static Dictionary<string, JArray> tokenPathMap_download = new Dictionary<string, JArray>();
        public static Dictionary<string, string> tokenPathMap_upload = new Dictionary<string, string>();


        private const int allowedSec = 30;
        private static bool CheckTimestamp(JObject JsonData)
        {
            var token = JsonData["Timestamp"];
            if (token == null) return false;

            try
            {
                long clientStamp = token.Value<long>();
                long serverNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                long diff = Math.Abs(serverNow - clientStamp);
                return diff <= allowedSec * 1000;
            }
            catch
            {
                return false; // 转换失败
            }
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

                if (context.Request.Url.AbsolutePath.Equals("/remotedesk", StringComparison.OrdinalIgnoreCase))
                {
                    using var handler = new RemoteDesktopHandler(webSocketContext.WebSocket, Setting_Read("Password"));
                    await handler.StartHandlingAsync();
                    return;
                }

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
                            SendMsg("{\"Operation\": \"RSACouldNotDecrypt\"}", webSocket, clientIp);
                            continue;
                        }
                       // DatabaseOP.LogWebServer($"来自{clientIp}的WebSocket消息: " + oriText);
                        if (Setting_Read("Password") == "" || Setting_Read("HmacKey") == "")
                        {
                            JsonDocument? PwdData = TryParseJson(oriText);
                            if (PwdData == null)
                            {
                                SendMsg("需要提供json格式的密码数据", webSocket, clientIp);
                                continue;
                            }
                            string? HashedPwd = GetJsonProperty(PwdData, "HashedPwd");
                            string? HmacKey = GetJsonProperty(PwdData, "HmacKey");
                            if (HashedPwd == null || HmacKey == null)
                            {
                                SendMsg("需要提供参数HashedPwd和HmacKey", webSocket, clientIp);
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
                                SendMsg("iv需要长度为16的字符串", webSocket, clientIp);
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
                        SendMsg("{\"Operation\": \"AESCouldNotDecrypt\"}", webSocket, clientIp);
                        continue;
                    }

                    DatabaseOP.LogWebServer($"来自{clientIp}的WebSocket消息: " + oriJson);

                    //JsonDocument? jsonDoc = TryParseJson(oriJson);
                    var jsonObject = JsonConvert.DeserializeObject<JObject>(oriJson);

                    try
                    {
                        jsonObject = JObject.Parse(oriJson);
                    }
                    catch (Exception)
                    {
                        SendMsg("消息格式有误", webSocket, clientIp);
                        continue;
                    }

                    string? operation = jsonObject["Operation"]?.ToString();

                    if (!CheckTimestamp(jsonObject)) continue;



                    if (operation == "Verify")
                    {
                        SendCrypted("{\"Operation\": \"Verified\"}", webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "MachineStatus")
                    {
                        string json = MachineStatus.GetMachineStatusJson();
                        SendCrypted(json, webSocket, AES_key, AES_iv, clientIp);
                        continue;
                    }

                    else if (operation == "ReadToMemory")
                    {
                        string? item = jsonObject["Item"]?.ToString();
                        if (item == null)
                        {
                            SendErr("item不能设置为空", webSocket, AES_key, AES_iv, clientIp);
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

                    else if (operation == "FileManager")
                    {
                        string? op = jsonObject["op"]?.ToString();
                        if (op == null)
                        {
                            SendCrypted("参数不能为空", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        else if (op == "GetContent")
                        {
                            string? path = jsonObject["path"]?.ToString();
                            string? info;
                            if (path == null)
                            {
                                SendCrypted("路径不能为空", webSocket, AES_key, AES_iv, clientIp);
                                continue;
                            }

                            if (path == "" || path == "\\") info = FileManager.GetIndex();
                            else info = FileManager.GetInfo(path);

                            if (info == null && File.Exists(path))
                            {
                                path = Path.GetDirectoryName(path);
                                if (path != null) info = FileManager.GetInfo(path);
                            }
                            if (info != null)
                            {
                                string ans = "FileContent:" + info;
                                SendCrypted(ans, webSocket, AES_key, AES_iv, clientIp);
                            }
                            else
                                SendNotification("获取文件夹内容失败", $"文件夹路径：{path}，具体原因可以查看错误日志", "error", webSocket, AES_key, AES_iv, clientIp);
                        }
                        else if (op == "Delete")
                        {
                            string? DeleteList = jsonObject["DeleteList"]?.ToString();
                            if (DeleteList == null)
                            {
                                SendCrypted("路径不能为空", webSocket, AES_key, AES_iv, clientIp);
                                continue;
                            }

                            var (SucNum, FailInfo) = FileManager.Delete(DeleteList);
                            SendNotification("删除完成", $"成功删除{SucNum}个内容", "success", webSocket, AES_key, AES_iv, clientIp);
                            if (FailInfo != null && FailInfo != "")
                                SendNotification("删除这些文件时出现错误", FailInfo, "error", webSocket, AES_key, AES_iv, clientIp);
                            SendCrypted("RefreshFileContent:", webSocket, AES_key, AES_iv, clientIp);
                        }
                        else if (op == "Rename")
                        {
                            string? path = jsonObject["path"]?.ToString();
                            string? newName = jsonObject["newName"]?.ToString();
                            if (path == null || newName == null)
                            {
                                SendCrypted("路径和新名称不能为空", webSocket, AES_key, AES_iv, clientIp);
                                continue;
                            }
                            string msg = FileManager.Rename(path, newName);
                            if (msg == "文件重命名成功" || msg == "文件夹重命名成功") SendNotification("重命名成功", "", "success", webSocket, AES_key, AES_iv, clientIp);
                            else SendNotification("重命名失败", msg, "error", webSocket, AES_key, AES_iv, clientIp);
                            SendCrypted("RefreshFileContent:", webSocket, AES_key, AES_iv, clientIp);

                        }
                        else if (op == "Paste")
                        {
                            string? resPath = jsonObject["resPath"]?.ToString();
                            string? targetPath = jsonObject["targetPath"]?.ToString();
                            bool? cut = jsonObject["cut"]?.Value<bool>();

                            if (resPath == null || targetPath == null || cut == null)
                            {
                                SendCrypted("文件路径、目标路径、剪切选项均不能为空", webSocket, AES_key, AES_iv, clientIp);
                                continue;
                            }
                            var (SucNum, FailMsg) = FileManager.Copy(resPath, targetPath, cut == true);
                            if (FailMsg == "") SendNotification($"{(cut == true ? "剪切" : "复制")}成功", $"成功复制{SucNum}个文件/文件夹", "success", webSocket, AES_key, AES_iv, clientIp);
                            else SendNotification($"{(cut == true ? "剪切" : "复制")}失败", $"出现了这些错误：{FailMsg}", "error", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }

                        else if (op == "Download")
                        {
                            JArray? path = jsonObject["path"] as JArray;

                            if (path == null)
                            {
                                SendCrypted("路径不能为空", webSocket, AES_key, AES_iv, clientIp);
                                continue;
                            }
                            string token = Actions.GetRandomString(16);
                            tokenPathMap_download[token] = path;
                            SendCrypted("FileDownloadToken:" + token, webSocket, AES_key, AES_iv, clientIp);
                        }

                        else if (op == "Upload")
                        {
                            string? path = jsonObject["path"]?.ToString();

                            if (path == null)
                            {
                                SendCrypted("路径不能为空", webSocket, AES_key, AES_iv, clientIp);
                                continue;
                            }
                            string token = Actions.GetRandomString(16);
                            tokenPathMap_upload[token] = path;
                            SendCrypted("FileUploadToken:" + token, webSocket, AES_key, AES_iv, clientIp);
                        }

                        else if (op == "ZipFile")
                        {
                            JArray? ZipList = jsonObject["ZipList"] as JArray;
                            string? targetPath = jsonObject["targetPath"]?.ToString();

                            if (ZipList == null || targetPath == null)
                            {
                                SendCrypted("原文件和目标路径不能为空", webSocket, AES_key, AES_iv, clientIp);
                                continue;
                            }

                            string? ans = FileManager.ZipFiles(ZipList, targetPath);

                            if (ans == null)
                            {
                                SendNotification("压缩完成", $"已压缩{ZipList.Count}个文件/文件夹", "success", webSocket, AES_key, AES_iv, clientIp);
                                SendCrypted("RefreshFileContent:", webSocket, AES_key, AES_iv, clientIp);
                            }
                            else
                            {
                                SendNotification("压缩失败", $"出现错误：{ans}", "error", webSocket, AES_key, AES_iv, clientIp);

                            }
                        }

                        else if (op == "NewItem")
                        {
                            string? type = jsonObject["type"]?.ToString();
                            string? path = jsonObject["path"]?.ToString();
                            if (type == null || path == null)
                            {
                                SendNotification("需要提供新文件(夹）的路径与类型", "", "error", webSocket, AES_key, AES_iv, clientIp);
                                continue;
                            }
                            try
                            {
                                if (type == "dir") Directory.CreateDirectory(path);
                                if (type == "file") File.Create(path);
                            }
                            catch
                            {
                                SendNotification("创建失败", "", "error", webSocket, AES_key, AES_iv, clientIp);
                                continue;
                            }

                            SendNotification("创建成功", "", "success", webSocket, AES_key, AES_iv, clientIp);
                            SendCrypted("RefreshFileContent:", webSocket, AES_key, AES_iv, clientIp);

                        }




                    }

                    else if (operation == "RunAction")
                    {
                        string? ActionCalls = jsonObject["Action"]?.ToString();

                        Actions action = new Actions();
                        Task.Run(() => action.ExecuteActions(ActionCalls));

                        continue;
                    }

                    else if (operation == "Setting_Read_Batch")
                    {
                        string? item = jsonObject["Item"]?.ToString();
                        string query = $"SettingQueryResult:{Setting_Read_Batch()}";
                        SendCrypted(query, webSocket, AES_key, AES_iv, clientIp);
                        continue;
                    }

                    else if (operation == "Setting_Write")
                    {
                        string? item = jsonObject["Item"]?.ToString();
                        string? value = jsonObject["Value"]?.ToString();
                        if (item == null || value == null)
                        {
                            SendCrypted("读取的参数不能为空", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        Setting_Write(item, value);
                    }

                    else if (operation == "GetTextFronUrl")
                    {
                        string? Url = jsonObject["Url"]?.ToString();

                        if (Url == null)
                        {
                            SendCrypted("url不能为空", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        string? Ans = Setting.GetStringFromUrl(Url);
                        if (Ans == null)
                        {
                            SendCrypted("获取url内容失败", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }

                        var ansData = new
                        {
                            Url,
                            Ans
                        };

                        string ansJson = "GetTextFronUrl:" + System.Text.Json.JsonSerializer.Serialize(ansData);
                        SendCrypted(ansJson, webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "TransferDirectory")
                    {
                        string? filePath = jsonObject["filePath"]?.ToString();
                        if (filePath == null)
                        {
                            SendCrypted("需要提供目标路径", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        string ans = Setting.Version.TransferDirectory(filePath);
                        SendCrypted(ans, webSocket, AES_key, AES_iv, clientIp);

                        if (ans == "脚本部署完成，正在转移")
                        {
                            Thread.Sleep(5000);
                            Actions.exit();
                        }

                    }

                    else if (operation == "ChangePassword")
                    {
                        string? OldPassword = jsonObject["OldPassword"]?.ToString();
                        string? NewPassword = jsonObject["NewPassword"]?.ToString();
                        string? HmacKey = jsonObject["HmacKey"]?.ToString();

                        if (OldPassword == null || NewPassword == null || HmacKey == null)
                        {
                            SendCrypted("有参数为空", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        if (!DatabaseOP.VerifyPassword(OldPassword))
                        {
                            SendNotification("", "旧密码输入错误", "error", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        DatabaseOP.SetPassword(NewPassword, HmacKey);
                        SendCrypted("PwdChangeSuc:", webSocket, AES_key, AES_iv, clientIp);
                        SendNotification("", "密码更改成功", "success", webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "AddTarget")
                    {
                        string? TargetSign = jsonObject["TargetSign"]?.ToString();
                        string? PassingSign = jsonObject["PassingSign"]?.ToString();
                        string? Action = jsonObject["Action"]?.ToString();
                        string? ApplicationName = jsonObject["ApplicationName"]?.ToString();

                        if (TargetSign == null || PassingSign == null || Action == null || ApplicationName == null)
                        {
                            SendCrypted("有参数为空", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        DatabaseOP.InsertTarget(TargetSign, PassingSign, ApplicationName, Action);
                        SendNotification("", "添加成功", "success", webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "DelTarget")
                    {
                        string? Id = jsonObject["Id"]?.ToString();
                        if (int.TryParse(Id, out int id))// input 是纯数字，可以转换为 int
                        {
                            DatabaseOP.DeleteTarget(id);
                            SendNotification("", "删除成功", "success", webSocket, AES_key, AES_iv, clientIp);
                        }
                        else
                            SendErr("需要数字ID作为参数", webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "UpdateTarget")
                    {
                        int id;
                        string? Id = jsonObject["Id"]?.ToString();
                        string? TargetSign = jsonObject["TargetSign"]?.ToString();
                        string? PassingSign = jsonObject["PassingSign"]?.ToString();
                        string? Action = jsonObject["Action"]?.ToString();
                        string? ApplicationName = jsonObject["ApplicationName"]?.ToString();


                        if (Id == null || !int.TryParse(Id, out id))// input 是纯数字，可以转换为 int
                        {
                            SendErr("参数Id需要为数字", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        if (TargetSign == null || Action == null || ApplicationName == null)
                        {
                            SendCrypted("有参数为空", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        DatabaseOP.UpdateTarget(id, TargetSign, PassingSign, ApplicationName, Action);
                        SendNotification("", "更新成功", "success", webSocket, AES_key, AES_iv, clientIp);
                    }

                    else if (operation == "EnableTarget")
                    {
                        int id, enable;
                        string? Id = jsonObject["Id"]?.ToString();
                        string? Enable = jsonObject["Enable"]?.ToString();
                        if (int.TryParse(Id, out int Id_ToInt))// input 是纯数字，可以转换为 int
                            id = Id_ToInt;
                        else
                        {
                            SendErr("参数Id需要为数字", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        if (int.TryParse(Enable, out int Enable_ToInt))// input 是纯数字，可以转换为 int
                            enable = Enable_ToInt;
                        else
                        {
                            SendErr("参数Id需要为数字", webSocket, AES_key, AES_iv, clientIp);
                            continue;
                        }
                        DatabaseOP.EnableTarget(id, enable);
                        if (enable == 0) SendNotification("", "已禁用规则", "success", webSocket, AES_key, AES_iv, clientIp);
                        else SendNotification("", "已启用规则", "success", webSocket, AES_key, AES_iv, clientIp);

                    }

                    else if (operation == "QuiryTargetList")
                    {
                        SendCrypted("TargetList: " + GetTargetList(), webSocket, AES_key, AES_iv, clientIp);
                    }
                    else if (operation == "heartbeat")
                    {
                        SendCrypted("heatbeat", webSocket, AES_key, AES_iv, clientIp);
                        return;
                    }

                    else
                        SendErr("API不存在", webSocket, AES_key, AES_iv, clientIp);

                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"webSocket处理错误：{exception.ToString()}");
                return;
            }
        }





        static void SendErr_http(HttpListenerContext context, string msg)
        {
            msg = @$"<html><head><meta charset=""utf-8""</head><body><h1>{msg}</h1></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(msg);

            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;

            using (Stream output = context.Response.OutputStream)
            {
                output.Write(buffer, 0, buffer.Length);
            }
        }

static void SendFile_http(HttpListenerContext context, string targetPath, string name)
    {
        if (File.Exists(targetPath))
        {
            // 设置为文件的 MIME 类型
            context.Response.ContentType = "application/octet-stream";

            // 处理中文文件名，使用 RFC 5987 编码
            string encodedName = WebUtility.UrlEncode(name);
            string headerValue = $"attachment; filename=\"{encodedName}\"; filename*=UTF-8''{encodedName}";

            context.Response.AddHeader("Content-Disposition", headerValue);
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            // 发送文件
            using (FileStream fs = new FileStream(targetPath, FileMode.Open, FileAccess.Read))
            {
                fs.CopyTo(context.Response.OutputStream);
            }
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.StatusDescription = "File not found.";
            context.Response.Close();
        }
    }

        private static (string FileName, byte[] Content)[] ExtractFileContents(string formData, string boundary)
        {
            // 将表单数据拆分为各个部分
            string[] parts = formData.Split(new string[] { "--" + boundary }, StringSplitOptions.RemoveEmptyEntries);
            var files = new System.Collections.Generic.List<(string FileName, byte[] Content)>();

            foreach (var part in parts)
            {
                if (part.Contains("Content-Disposition:"))
                {
                    var lines = part.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    string fileName = null;

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Content-Disposition:"))
                        {
                            // 从头部提取文件名
                            int fileNameIndex = line.IndexOf("filename=\"") + "filename=\"".Length;
                            int fileNameEndIndex = line.IndexOf("\"", fileNameIndex);
                            fileName = line.Substring(fileNameIndex, fileNameEndIndex - fileNameIndex);
                        }
                    }

                    // 提取文件内容
                    int contentIndex = part.IndexOf("\r\n\r\n") + 4; // 跳过头部
                    byte[] contentData = System.Text.Encoding.UTF8.GetBytes(part.Substring(contentIndex, part.Length - contentIndex - 2)); // 去掉结尾的标记
                    files.Add((fileName, contentData));
                }
            }

            return files.ToArray();
        }


        static void HandleRequest(object state)
        {
            try
            {
                HttpListenerContext context = (HttpListenerContext)state;
                string requestUrl = context.Request.Url.LocalPath.Substring(1); // 去掉开头的斜杠

                string[]? pathSegments = requestUrl.Split('/');
                HttpListenerRequest request = context.Request;

                // 如果路径数组的长度大于0，检查第一个路径段
                if (pathSegments != null && pathSegments.Length > 0 &&
                    string.Equals(pathSegments[0], "download", StringComparison.OrdinalIgnoreCase))
                {
                    var query = context.Request.QueryString;

                    string? token = query["token"]; // 获取token参数

                    if (token != null && tokenPathMap_download.TryGetValue(token, out JArray? downloadPathList) && downloadPathList != null)
                    {
                        if (downloadPathList.Count == 0) SendErr_http(context, "下载失败：不能下载空内容");
                        if (downloadPathList.Count == 1)
                        {
                            string? downloadPath = downloadPathList[0].ToString();
                            if (downloadPath == null) SendErr_http(context, "下载失败：需要提供目标路径");
                            if (File.Exists(downloadPath))
                            {
                                SendFile_http(context, downloadPath, Path.GetFileName(downloadPath));
                            }
                            else if (Directory.Exists(downloadPath))
                            {
                                string tempZipPath = Path.GetTempFileName() + ".zip";

                                FileManager.ZipFiles(downloadPathList, tempZipPath);
                                SendFile_http(context, tempZipPath, Path.GetFileName(downloadPath) + ".zip");
                                File.Delete(tempZipPath);
                            }
                        }
                        else
                        {
                            string tempZipPath = Path.GetTempFileName() + ".zip";

                            FileManager.ZipFiles(downloadPathList, tempZipPath);
                            DateTime now = DateTime.Now;
                            string formattedTime = now.ToString("yyyy-MM-dd-HH-mm");
                            SendFile_http(context, tempZipPath, $"{downloadPathList.Count}个文件-{formattedTime}.zip");
                            File.Delete(tempZipPath);
                        }
                    }
                    else
                    {
                        SendErr_http(context, "下载失败：token无效");
                    }
                    if (token != null && tokenPathMap_download.ContainsKey(token)) // 检查是否存在键
                    {
                        tokenPathMap_download.Remove(token);
                    }
                }

                else if (request.HttpMethod == "POST" && request.ContentType.StartsWith("multipart/form-data"))
                {
                    var query = context.Request.QueryString;

                    string? token = query["token"]; // 获取token参数

                    if (token != null && tokenPathMap_upload.TryGetValue(token, out string? targetPath) && targetPath != null)
                    {
                        string boundary = request.ContentType.Split(';')[1].Trim().Substring("boundary=".Length);
                        using (var reader = new StreamReader(request.InputStream))
                        {
                            string formData = reader.ReadToEnd();
                            var files = ExtractFileContents(formData, boundary);

                            // 保存文件
                            foreach (var file in files)
                            {
                                string filePath = Path.Combine(targetPath, file.FileName);
                                File.WriteAllBytes(filePath, file.Content);
                                Console.WriteLine($"File uploaded to {filePath}");
                            }
                            HttpListenerResponse response = context.Response;
                            string responseString = "<html><body>Files Uploaded Successfully</body></html>";
                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                            response.ContentLength64 = buffer.Length;
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                        }
                    }
                    else
                    {
                        SendErr_http(context, "上传失败：token无效");
                    }
                    if (token != null && tokenPathMap_upload.ContainsKey(token)) // 检查是否存在键
                    {
                        tokenPathMap_upload.Remove(token);
                    }
                }


                else
                {
                    if (requestUrl == "" || requestUrl == "debug") requestUrl = "index.html";

                    string filePath = Path.Combine(Setting.programDir, "webFile", requestUrl);

                    if (!File.Exists(filePath)) filePath = Path.Combine(Setting.programDir, "webFile", "index.html");

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
                        else if (filePath.EndsWith(".png") || filePath.EndsWith(".jpg") || filePath.EndsWith(".jpeg") || filePath.EndsWith(".svg"))
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
