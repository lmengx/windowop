using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using static windowOP.DatabaseOP;
using static System.Collections.Specialized.BitVector32;
//using System.Data.Entity.Core.Common;
using Microsoft.Win32;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Windows.Forms;
using Newtonsoft.Json;
using Microsoft.VisualBasic.ApplicationServices;
using System.Net.WebSockets;
//using System.Data.SQLite;

namespace windowOP
{
    public class DatabaseOP
    {
        public struct Target
        {
            public string ID { get; set; }
            public string TargetSign { get; set; }
            public string PassingSign { get; set; }
            public string ApplicationName { get; set; }
            public string Action { get; set; }
            public string Enable { get; set; }

            public Target(string id, string targetSign, string passingSign, string applicationName, string action, string enable)
            {
                ID = id;
                TargetSign = targetSign;
                PassingSign = passingSign;
                ApplicationName = applicationName;
                Action = action;
                Enable = enable;
            }
        }



        public static string DbPath = Path.Combine(Setting.programDir, "database.db"); // 数据库文件路径

        public static List<Target> TargetList = new List<Target>();

        public static int ListenPort = 7799;
        public static bool Log_Enable;
        public static bool Log_PrintDebugMsg;
        public static bool Log_WebServer;
        public static bool Log_WindowWatcher;

        public static bool SusStatus;
        public static int SusTime;
        public static int SleepTime = 1000;




        public static void LogErr(string msg)
        {
            DateTime now = DateTime.Now;
            string filePath = Path.Combine(Setting.programDir, "Logs", "Err", now.ToString("yyyyMMdd") + ".txt");
            msg = now.ToString("[HH:mm:ss] ") + msg;
            Log(msg, filePath);
        }

        public static void Log(string msg) 
        {
            DateTime now = DateTime.Now;
            string filePath = Path.Combine(Setting.programDir, "Logs", now.ToString("yyyyMMdd") + ".txt");
            msg = now.ToString("[HH:mm:ss] ") + msg;
            Log(msg, filePath);
        }

        public static void LogWindowWatcher(string msg)
        {
            if (!Log_WindowWatcher) return;
            DateTime now = DateTime.Now;
            string filePath = Path.Combine(Setting.programDir, "Logs","WindowWatcher", now.ToString("yyyyMMdd") + ".txt");
            msg = now.ToString("[HH:mm:ss] ") + msg;
            Log(msg, filePath);
        }

        public static void LogWebServer(string msg)
        {
            if(!Log_WebServer) return;
            DateTime now = DateTime.Now;
            string filePath = Path.Combine(Setting.programDir, "Logs", "WebServer", now.ToString("yyyyMMdd") + ".txt");
            msg = now.ToString("[HH:mm:ss] ") + msg;
            Log(msg, filePath);
        }
        public static void Log(string msg, string filePath)
        {
            if (!Log_Enable) return;
            const int MaxRetries = 300; // Retry count
            const int DelayBetweenRetries = 1000; // Delay in milliseconds

            if (Log_PrintDebugMsg) Console.WriteLine(msg);
                int retryCount = 0;
                while (retryCount < MaxRetries)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        File.AppendAllText(filePath, msg + Environment.NewLine);
                        return;
                    }
                    catch
                    {
                        Thread.Sleep(DelayBetweenRetries);
                        retryCount++;
                    }
                }
                    Console.WriteLine("无法写入日志文件，已达到最大重试次数。");
                         
        }

        public static bool ExistDatabase()
        {
            if (File.Exists(DbPath)) return true;
            return false;
        }

        public static void CreateDB()
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    @"
                    CREATE TABLE IF NOT EXISTS TargetList (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TargetSign TEXT DEFAULT '',
                        PassingSign TEXT DEFAULT '',                       
                        ApplicationName TEXT DEFAULT '',
                        Action TEXT DEFAULT '',
                        Enable INTEGER NOT NULL DEFAULT 0
                    );
                                   
                    CREATE TABLE IF NOT EXISTS Setting (
                        Password TEXT,
                        HmacKey TEXT,
                        SleepTime INTEGER DEFAULT 1000,
                        ListenPort INTEGER DEFAULT 7799,
                        ServeAllPrefix INTEGER DEFAULT 0,
                        RunAsAdmin INTEGER DEFAULT 0,
                        Log_Enable INTEGER DEFAULT 0,                     
                        Log_PrintDebugMsg INTEGER DEFAULT 0,
                        Log_ShowConsole INTEGER DEFAULT 0,
                        Log_WebServer INTEGER DEFAULT 0,
                        Log_WindowWatcher INTEGER DEFAULT 0,
                        Protect_Process INTEGER DEFAULT 0,
                        Protect_Files INTEGER DEFAULT 0,
                        Protect_StartTask INTEGER DEFAULT 0,
                        Frp_Enable INTEGER DEFAULT 0,
                        Frp_parameters TEXT DEFAULT '',
                        HOSTS TEXT DEFAULT '',
                        DebugMode INTEGER DEFAULT 0
                    );
                ";
                command.ExecuteNonQuery();
            }
        }

        public static bool InsertSetting()
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM Setting;";
                int count = Convert.ToInt32(command.ExecuteScalar());
                if (count == 0)
                {
                    command.CommandText =
                    "INSERT INTO Setting (Password) VALUES (@Password);";
                    command.Parameters.AddWithValue("@Password", "");
                    command.ExecuteNonQuery();
                    return true;
                }
                return false;
            }
        }
        public static HashSet<string> SetList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "SleepTime",
        "ListenPort",
        "ServeAllPrefix",
        "Log_Enable",
        "Log_PrintDebugMsg",
        "Log_ShowConsole",
        "Log_WebServer",
        "Log_WindowWatcher",
        "RunAsAdmin",
        "Protect_Process",
        "Protect_Files",
        "Protect_StartTask",
        "Frp_Enable",
        "Frp_parameters",
        "HOSTS",
        "DebugMode"
    };

        public static bool IsSettingTableInitialized()
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // 查询是否存在至少一条记录
                    command.CommandText = "SELECT COUNT(1) FROM Setting;";
                    var count = (long)command.ExecuteScalar();
                    return count > 0;
                }
            }
        }

        public static void SetFrpc(string parameters)
        {
            if(!IsSettingTableInitialized()) InsertSetting();
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // 注意：SQLite 中布尔值通常用 0/1 表示
                    command.CommandText = @"
                UPDATE Setting 
                SET Frp_Enable = 1, 
                    Frp_parameters = @parameters;";

                    command.Parameters.AddWithValue("@parameters", parameters ?? string.Empty);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void AddHost(string host)
        {
            string Hosts = DatabaseOP.Setting_Read("HOSTS");
            if (Hosts != "") Hosts += " ";
            Hosts += host;
            DatabaseOP.WriteHost(Hosts);//在host后添加内容
        }

        public static void DelHost(string hostToRemove)
        {
            if (string.IsNullOrEmpty(hostToRemove))
                return;

            string currentHosts = DatabaseOP.Setting_Read("HOSTS");
            if (string.IsNullOrEmpty(currentHosts))
                return;

            // 按空格分割成列表
            var hostsList = new List<string>(currentHosts.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            // 移除匹配的项（精确匹配）
            bool removed = hostsList.RemoveAll(h => h.Equals(hostToRemove, StringComparison.OrdinalIgnoreCase)) > 0;

            if (removed)
            {
                // 重新拼接
                string newHosts = string.Join(" ", hostsList);
                DatabaseOP.WriteHost(newHosts); // 覆盖写回
            }
        }

        public static void WriteHost(string host)
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "UPDATE Setting SET HOSTS = @host;";
                    command.Parameters.AddWithValue("@host", host);
                    command.ExecuteNonQuery();
                }
            }
        }


        public static void Setting_Write(string item, string value)
        {
            // 检查 item 是否在允许的列名之内
            if (!SetList.Contains(item))
            {
                LogErr($"请求写入的列 '{item}' 不合法. 允许的列如下: {string.Join(", ", SetList)}.");
                return;
            }

            try
            {
                using (var connection = new SqliteConnection($"Data Source={DbPath}"))
                {
                    connection.Open();

                    // 获取列的数据类型
                    string? columnType = GetColumnType(connection, item);
                    if (columnType == null) return;

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"UPDATE Setting SET {item} = @value;";

                        // 判断数据类型并添加参数
                        if (columnType.Equals("INTEGER", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!int.TryParse(value, out int numericValue))
                            {
                                LogErr($"列 '{item}' 期望数字类型, 但接收到的值是: {value}");
                                return;
                            }
                            command.Parameters.AddWithValue("@value", numericValue);
                        }
                        else if (columnType.Equals("TEXT", StringComparison.OrdinalIgnoreCase))
                        {
                            command.Parameters.AddWithValue("@value", value);
                        }
                        else
                        {
                            LogErr($"不支持的数据类型: {columnType}.");
                            return;
                        }

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                LogErr(ex.ToString());
            }
        }

        // 辅助函数：获取列的数据类型
        private static string? GetColumnType(SqliteConnection connection, string columnName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA table_info(Setting);";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // column 0: cid, column 1: name, column 2: type, column 3: notnull, column 4: dflt_value, column 5: pk
                        if (reader.GetString(1) == columnName)
                        {
                            return reader.GetString(2); // 返回列的类型
                        }
                    }
                }
            }
            return null; // 如果未找到列
        }


        public static string Setting_Read(string item)//用于程序内部读取
        {
            if (!SetList.Contains(item) && item != "Password" && item != "HmacKey")
            {
                string msg = $"请求读取的列 '{item}' 不合法. 允许的列如下: {string.Join(", ", SetList)}.";
                Log(msg);
                return msg;
            }
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT {item} FROM Setting LIMIT 1;";
                        var result = command.ExecuteScalar();
                        if (result == null || result.ToString() == null) return "";
                        return result.ToString();
                    }
                }
            }
            catch(Exception e) 
            {
                LogErr(e.ToString());
                return "";
            }
        }

        public static string Setting_Read_Batch()//用于web查询
        {
            // 结果存储字典
            var resultObject = new Dictionary<string, object>();

            try
            {
                using (var connection = new SqliteConnection($"Data Source={DbPath}"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        // 使用 HashSet 中的所有列构建查询字符串
                        var query = string.Join(", ", SetList);
                        command.CommandText = $"SELECT {query} FROM Setting LIMIT 1;";

                        var result = command.ExecuteReader();

                        // 读取结果
                        if (result.Read())
                        {
                            foreach (var column in SetList)
                            {
                                // 如果该列存在于结果集中，则加入字典
                                resultObject[column] = result[column];
                            }
                        }
                        resultObject["LANAddress"] = $"http://{Setting.GetLocalIPAddress()}:{ListenPort}";
                        resultObject["LatestVersion"] = Setting.Version.GetLatestVersion();
                        // 返回结果格式
                        string jsonResult = JsonConvert.SerializeObject(resultObject);
                        return jsonResult;
                    }
                }
            }
            catch (Exception e)
            {
                LogErr(e.ToString());
                return "Error: An exception occurred while reading settings.";
            }
        }

        public static string ReadToMemory(string item)
        {
            try
            {
                int value = Convert.ToInt32(Setting_Read(item));
                string result;
                if (item == "SleepTime")
                {
                    if (value > 10) SleepTime = value;
                    else LogErr("SleepTime必须大于10毫秒");
                }
                else if (item == "ListenPort")
                {
                    if (value > 1023 && value <= 65535) ListenPort = value;
                    else LogErr("端口必须设置在1024--65535之间");
                }
                else if (item == "Log_Enable") Log_Enable = (value == 1);
                else if (item == "Log_PrintDebugMsg") Log_PrintDebugMsg = (value == 1);
                else if (item == "Log_WebServer") Log_WebServer = (value == 1);
                else if (item == "Log_WindowWatcher") Log_WindowWatcher = (value == 1);

                else if (item == "Protect_Process")
                {
                    if (value == 1) Protect.Bat.Start();
                    else Protect.Bat.Stop();
                }
                else if (item == "Protect_StartTask")
                {
                    if (value == 1) Protect.StartTask.Start();
                    else Protect.StartTask.Stop();
                }
                else if (item == "Protect_Files")
                {
                    if (value == 1) Protect.Files.Start();
                    else Protect.Files.Stop();
                }

                else
                {
                    result = $"此参数不可读取:'{item}'";
                    return result;
                }

                result = $"已完成读取:'{item}'--{value}";
                DatabaseOP.Log(result);
                return result;
            }
            catch { return ""; }
        }

        public static string GetHmacKey()
        {
            if (Setting_Read("Password") == "" || Setting_Read("HmacKey") == "")
                return "PwdNotSet";
            else
            {
                string HmacKey = Setting_Read("HmacKey");
                return HmacKey;
            }
        }

        public static void InsertTarget(string TargetSign, string PassingSign, string ApplicationName, string Action)
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "INSERT INTO TargetList (TargetSign, PassingSign, ApplicationName, Action) VALUES (@TargetSign, @PassingSign, @ApplicationName, @Action);";
                    command.Parameters.AddWithValue("@TargetSign", TargetSign);
                    command.Parameters.AddWithValue("@PassingSign", PassingSign);
                    command.Parameters.AddWithValue("@ApplicationName", ApplicationName);                    
                    command.Parameters.AddWithValue("@Action", Action);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteTarget(int id)
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM TargetList WHERE Id = @id;";
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateTarget(int id, string targetSign, string passingSign, string ApplicationName, string action)
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "UPDATE TargetList SET TargetSign = @targetSign, PassingSign = @passingSign, ApplicationName = @applicationName, Action = @action WHERE Id = @id;";
                    command.Parameters.AddWithValue("@targetSign", targetSign);
                    command.Parameters.AddWithValue("@passingSign", passingSign);
                    command.Parameters.AddWithValue("@applicationName", ApplicationName);
                    
                    command.Parameters.AddWithValue("@action", action);
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void EnableTarget(int id, int enable)
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "UPDATE TargetList SET Enable = @enable WHERE Id = @id;";
                    command.Parameters.AddWithValue("@enable", enable);
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static string GetTargetList()
        {
            var targets = GetTargets(false);
            return System.Text.Json.JsonSerializer.Serialize(targets);
        }

        public static void WriteTargetsFromJson(string json)
        {
            Console.WriteLine(json);

            try
            {
                var targets = System.Text.Json.JsonSerializer.Deserialize<List<Target>>(json);

                using (var connection = new SqliteConnection($"Data Source={DbPath}"))
                {
                    connection.Open();
                    {
                        int Targer_Id = 1;
                        foreach (var target in targets)
                        {
                            // Console.WriteLine($"{target.TargetSign}\n{target.PassingSign}\n{target.ApplicationName}");
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText =
                                "INSERT INTO TargetList (TargetSign, PassingSign, ApplicationName, Action) VALUES (@TargetSign, @PassingSign, @ApplicationName, @Action);";

                                command.Parameters.AddWithValue("@TargetSign", string.IsNullOrEmpty(target.TargetSign) ? "" : target.TargetSign);
                                command.Parameters.AddWithValue("@PassingSign", string.IsNullOrEmpty(target.PassingSign) ? "" : target.PassingSign);
                                command.Parameters.AddWithValue("@ApplicationName", string.IsNullOrEmpty(target.ApplicationName) ? "" : target.ApplicationName);
                                command.Parameters.AddWithValue("@Action", string.IsNullOrEmpty(target.Action) ? "" : target.Action);
                                command.ExecuteNonQuery();

                                if (int.TryParse(target.Enable, out int enableValue))
                                {
                                    if (enableValue == 1)
                                    {
                                        EnableTarget(Targer_Id, enableValue);
                                    }
                                }
                            }
                            Targer_Id++;
                        }
                    }
                }
                Console.WriteLine("TargetList 成功继承.");
            }
            catch (Exception e) {
                LogErr(e.ToString());
                Console.WriteLine("继承TargetList时出现错误." + e);
            }

        }

        public static bool WriteSettingFromJson(string jsonSettings)
        {
            // 解析 JSON 字符串为字典
            try
            {
                Console.WriteLine(jsonSettings);
                var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonSettings);

                if (!InsertSetting())
                {
                    Console.WriteLine("试图在有设置项时使用继承覆盖");
                    return false;
                }

                string? Password = "", HmacKey = "";

                        foreach (var (key, value) in settings)
                        {
                            if (value.ToString() == null) continue;
                            if (SetList.Contains(key))
                            {
                                Setting_Write(key, value.ToString());
                            }
                            else if ( key == "Password" ) Password = value.ToString();
                            else if ( key == "HmacKey" ) HmacKey = value.ToString();

                        }

                        if( Password != null && HmacKey != null) SetPassword(Password, HmacKey);
                    
                
                Console.WriteLine( "Settings 成功继承.");
                return true;
            }
            catch (Exception e)
            {
                LogErr(e.ToString());
                Console.WriteLine( "继承Setting时出现错误." + e);
                return false;
            }
        }

        public static void HandleUpdate()
        {
            string TargetListJson = Path.Combine(Setting.programDir, "TargetList.json");
            string SettingJson = Path.Combine(Setting.programDir, "Setting.json");

            if (File.Exists(TargetListJson) && File.Exists(SettingJson))
            {
                try
                {
                    string content = File.ReadAllText(SettingJson);
                    if (DatabaseOP.WriteSettingFromJson(content))
                    {
                        content = File.ReadAllText(TargetListJson);
                        DatabaseOP.WriteTargetsFromJson(content);
                        File.Delete(TargetListJson);
                    }
                    File.Delete(SettingJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"继承更新前的数据出错: {ex}");
                    return;
                }
            }
        }

        static List<Target> GetTargets(bool enable)//待优化
        {
            var targets = new List<Target>();

            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Id, TargetSign, PassingSign, ApplicationName, Action, Enable FROM TargetList;";
                    if (enable)
                        command.CommandText = "SELECT Id, TargetSign, PassingSign, ApplicationName, Action, Enable FROM TargetList WHERE Enable = 1;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var target = new Target(
                                reader.GetString(0),
                                reader.GetString(1),
                                reader.GetString(2),
                                reader.GetString(3),
                                reader.GetString(4),
                                reader.GetString(5)
                            );
                            targets.Add(target);
                        }
                    }
                }
            }
            return targets;
        }

        



        public static void ReadToMemory_TargetList()
        {
            var newTargetList = GetTargets(true);
            TargetList.Clear();
            TargetList.AddRange(newTargetList);
        }


        public static bool VerifyPassword(string password)
        {
            Console.WriteLine(password);
            if (password == null) return false;
            string hashedPassword = Setting_Read("Password");
            string HmacKey = Setting_Read("HmacKey");
            try
            {
                if (Crypto.HmacSha256(password, HmacKey).Substring(0, 16) == hashedPassword) return true;
                return false;
            }
            catch
            {
                LogErr("密码验证出错，请检查数据库文件正确性");
                return false;
            }            
        }

        public static void SetPassword(string password, string HmacKey)
        {
            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                        command.CommandText = "UPDATE Setting SET Password = @password, HmacKey = @hmackey;";
                    command.Parameters.AddWithValue("@password", password);
                    command.Parameters.AddWithValue("@hmackey", HmacKey);
                    command.ExecuteNonQuery();

                }
            }
        }

    }
}