using System;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.VisualBasic.Logging;
using System.Threading;
using System.IO.Compression;
using System.Text;

namespace windowOP
{
    public class Actions
    {

        public static string cmd(string command)
        {
            string output = string.Empty;
            string error = string.Empty;

            try
            {
                // 配置启动信息
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C " + command, // /C 表示执行指定命令并关闭
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true // 不创建窗口
                };

                using (Process process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();

                    // 读取输出
                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();

                    process.WaitForExit(); // 等待命令执行完成
                }
            }
            catch (Exception ex)
            {
                output = $"发生异常: {ex.Message}";
            }

            // 合并输出和错误信息
            if (!string.IsNullOrEmpty(error))
            {
                output += $"\n错误: {error.Trim()}";
            }

            return output.Trim(); // 返回执行结果
        }






        public static string RunBatchAsAdmin(string command)
        {
            // 创建一个临时批处理文件
            string tempBatchFilePath = Path.GetTempFileName() + ".bat";
            string output = string.Empty; // 用于存储cmd输出的信息

            try
            {
                // 将命令写入临时批处理文件
                File.WriteAllText(tempBatchFilePath, command);

                // 配置启动信息
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",  // 使用 cmd.exe
                    Arguments = "/c \"" + tempBatchFilePath + "\"",  // 运行批处理文件
                    Verb = "runas",  // 请求以管理员权限运行
                    UseShellExecute = false,  // 必须设置为false以便重定向输出
                    RedirectStandardOutput = true, // 重定向标准输出
                    RedirectStandardError = true, // 重定向标准错误
                    CreateNoWindow = true // 不创建窗口
                };

                // 启动进程并获取输出
                using (Process process = Process.Start(processStartInfo))
                {
                    // 读取输出
                    output = process.StandardOutput.ReadToEnd();
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit(); // 等待进程结束

                    // 如果有错误输出，附加到输出信息
                    if (!string.IsNullOrEmpty(errorOutput))
                    {
                        output += "\n错误: " + errorOutput;
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // 处理没有管理员权限的情况
                if (ex.NativeErrorCode == 1223) // 用户取消了授权提示
                {
                    output = "用户取消了管理员权限请求。";
                    DatabaseOP.LogErr("用户取消了管理员权限请求");
                }
                else
                {
                    output = "发生错误: " + ex.Message;
                    DatabaseOP.LogErr("发生错误: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                DatabaseOP.LogErr("发生错误: " + ex.Message);
            }
            finally
            {
                // 删除临时批处理文件
                if (File.Exists(tempBatchFilePath))
                {
                    try
                    {
                        File.Delete(tempBatchFilePath);
                    }
                    catch (Exception ex)
                    {
                        output += "\n无法删除临时文件: " + ex.Message;
                        DatabaseOP.LogErr("无法删除临时文件: " + ex.Message);
                    }
                }
            }

            return output; // 返回命令的输出内容
        }


        public static void ExecuteVbs(string text)
        {
            string tempPath = Path.GetTempFileName() + ".vbs";

            File.WriteAllText(tempPath, text);            // 将文本写入 VBS 文件
            try
            {
                // 执行 VBS 文件
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "wscript.exe";
                process.StartInfo.Arguments = $"\"{tempPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true; // 如果不想显示窗口
                process.Start();
                process.WaitForExit();
            }
            catch(Exception e)
            {
                DatabaseOP.LogErr("执行vbs时出错：" + e.Message);
                return;
            }

                // 删除临时文件
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }           
        }

        public static void RunPs(string psContent)
        {
            string tempPSFilePath = Path.Combine(Path.GetTempPath(), Actions.Random() + ".ps1");
            try
            {
                // 将命令写入临时 PowerShell 脚本文件
                File.WriteAllText(tempPSFilePath, psContent);

                Process process = new Process();
                process.StartInfo.FileName = "powershell";
                process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempPSFilePath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                process.WaitForExit();
                File.Delete(tempPSFilePath);
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine(ex.Message);
            }
        }



        public static void WriteToFile(string path, string content)
        {
            try
            {
                // 使用StreamWriter写入文件
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.Write(content);
                }
                Console.WriteLine("写入成功！");
            }
            catch (Exception ex)
            {
                Console.WriteLine("写入文件时发生错误: " + ex.Message);
            }
        }

        public static void ExtractZipFile(string zipPath, string extractPath)
        {
            try
            {
                // 确保目标目录存在
                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }

                // 解压缩 ZIP 文件
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                DatabaseOP.Log("解压成功！--" + extractPath);
            }
            catch (Exception ex)
            {
                DatabaseOP.Log($"解压失败: {ex.Message}");
            }
        }

        public static void OpenManagePage()
        {
            OpenWebPage(WebServer.ListenData);
        }

        public static void OpenManagePage_LANAddress()
        {
            Console.WriteLine(Setting.GetLocalIPAddress());
            OpenWebPage( $"http://{Setting.GetLocalIPAddress()}:{DatabaseOP.ListenPort}" );
        }

        public static void UAC(string op)
        {
            if (op != "0" && op != "1")
            {
                DatabaseOP.Log("UAC设置的参数需要为0或1");
                return;
            }
            string command = $@"
            reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v EnableLUA /t REG_DWORD /d {op} /f
            reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v UserAccountControlPolicy /t REG_DWORD /d {op} /f
            ";
            Actions.RunBatchAsAdmin(command);
        }

        public static void CreateTask_AutoRun()
        {
            Setting.AutoRun.AutoRun_Create();
        }
        public static void DeleteTask_AutoRun()
        {
            Setting.AutoRun.AutoRun_Remove();
        }

        public static bool ExitCode = false;
        public static void exit()
        {
            ExitCode = true;
            Protect.Bat.Stop();
            Environment.Exit(0);
        }

        public static void RestartAs(bool RunAs)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName, // 获取当前程序的路径
                UseShellExecute = true,
            };

            if (RunAs) startInfo.Verb = "runas";

            try
            {
                Process.Start(startInfo); // 启动新进程
                exit();
            }
            catch
            {
                Console.WriteLine("重启失败");
            }
        }

        public static void restart()
        {
            RestartAs(false);
        }

        public static void TryRestartAsAdmin()
        {
            if(!Setting.IsUserAdministrator())
            RestartAs(true);
        }

        public static string download(string url, string path)
        {
            try
            {
                // 获取文件的目录
                string directoryPath = Path.GetDirectoryName(path);

                // 检查目录是否存在，如果不存在，则创建目录
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    DatabaseOP.Log("创建目录: " + directoryPath);
                }

                using (HttpClient client = new HttpClient())
                {
                    // 发送请求并获取响应内容
                    HttpResponseMessage response = client.GetAsync(url).Result; // 使用同步方法
                    response.EnsureSuccessStatusCode(); // 确保请求成功

                    // 读取文件内容
                    byte[] fileBytes = response.Content.ReadAsByteArrayAsync().Result; // 使用同步方法

                    // 将内容写入指定的文件
                    File.WriteAllBytes(path, fileBytes); // 使用同步方法

                    return "文件下载成功: " + path;
                }
            }
            catch (Exception ex)
            {
                return "下载文件时出错: " + ex.Message;
            }
        }


            public static void downloadAndExecute(string url, string fileType = "exe")
            {
            // 获取文件名并添加指定的文件扩展名
            string FileName = Guid.NewGuid().ToString() + $".{fileType}"; 
            string tempPath = Path.Combine(Path.GetTempPath(), FileName); // 使用系统临时文件夹

                // 下载可执行文件
                download(url, tempPath);

                // 检查下载的文件是否存在，然后执行
                if (File.Exists(tempPath))
                {
                    string result = cmd($"\"{tempPath}\""); // 使用引号处理可能的空格

                    // 执行后删除文件
                    File.Delete(tempPath);
                }

            }


        public static void ctrl_w()
        {
            // 生成按下 Ctrl + W 的 VBS 脚本
            string vbsScript = @"
            Set WshShell = CreateObject(""WScript.Shell"")
            WshShell.SendKeys ""^w""
        ";
            ExecuteVbs(vbsScript);
        }
        public static void alt_f4()
        {
            // 生成按下 Ctrl + W 的 VBS 脚本
            string vbsScript = @"
        Set WshShell = WScript.Createobject(""WScript.Shell"")
WshShell.SendKeys ""%{F4}""
        ";
            ExecuteVbs(vbsScript);
        }

        public static void msgbox(string text, string title)
        {
            // 创建一个 VBScript 代码片段
            string vbsScript = $@"Set objArgs = WScript.Arguments
        MsgBox objArgs(0), 64, objArgs(1)";

            // 临时文件用于存储 VBS 脚本
            string tempFile = System.IO.Path.GetTempFileName() + ".vbs";

            // 将 VBScript 写入文件
            System.IO.File.WriteAllText(tempFile, vbsScript);

            // 使用 Process 启动 wscript.exe 运行 VBS 文件
            var process = new Process();
            process.StartInfo.FileName = "wscript.exe";
            process.StartInfo.Arguments = $"\"{tempFile}\" \"{text}\" \"{title}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            process.WaitForExit(); // 可选：等待 VBS 脚本执行完成

            // 可选：删除临时 VBS 文件
            System.IO.File.Delete(tempFile);
        }

        public static void shutdown(string time)
        {
            if (int.TryParse(time, out int timeValue))
                Thread.Sleep(timeValue);
            cmd("shutdown /s /t 0");
        }

        public static void reboot(string time)
        {
            if (int.TryParse(time, out int timeValue))
                Thread.Sleep(timeValue);
            cmd("shutdown /r /t 0");
        }
        public static void sleep(string time)
        {
            if (int.TryParse(time, out int timeValue))
                Thread.Sleep(timeValue);
            cmd("rundll32.exe powrprof.dll, SetSuspendState 0,1,0");
        }

        public static void wait(string time)
        {
            if(int.TryParse(time, out int timeValue))
            Thread.Sleep(timeValue);

        }

        public static void OpenWebPage(string url)
        {
            try
            {
                // 确保输入的 URL 是有效的
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    throw new ArgumentException("Invalid URL format");
                }

                // 使用默认的浏览器打开网页
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // 在默认的浏览器中打开
                });
            }
            catch (Exception ex)
            {
                DatabaseOP.Log($"打开网页时出错: {ex.Message}");
            }
        }

        public static void Update()
        {
            Setting.Version.Update();
        }

        public static void Delete()
        {
            Setting.Version.Delete();
        }

        // 使用高质量随机数生成器
        public static int Random()
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                byte[] randomNumber = new byte[4]; // 4 bytes for an int
                rng.GetBytes(randomNumber);
                int outNumber = BitConverter.ToInt32(randomNumber, 0);
                return outNumber & 0x7fffffff; // 保留正数部分
            }
        }

        private const string AvailableChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string GetRandomString(int length)
        {
            if (length <= 0)
            {
                throw new ArgumentException("Length must be greater than zero.", nameof(length));
            }

            using (var rng = new RNGCryptoServiceProvider())
            {
                // 创建一个字节数组以存储随机数
                byte[] randomNumber = new byte[length];

                // 填充字节数组，用随机数填充
                rng.GetBytes(randomNumber);

                // 将随机字节映射到可用字符集
                var result = new StringBuilder(length);
                foreach (var num in randomNumber)
                {
                    result.Append(AvailableChars[num % AvailableChars.Length]);
                }

                return result.ToString();
            }
        }




        // 方法来读取和执行对应的 JSON 操作，支持按参数名匹配
        public void ExecuteActions(string json)
        {
            // 将 JSON 字符串解析为 JArray
            var actions = JArray.Parse(json);

            foreach (var action in actions)
            {
                var actionName = action["ActionName"].ToString();
                var parameters = action["Paras"].ToObject<List<Parameter>>();

                // 寻找与 ActionName 匹配的方法
                MethodInfo method = GetType().GetMethod(actionName);
                if (method != null)
                {
                    // 获取方法的参数
                    ParameterInfo[] methodParams = method.GetParameters();

                    // 准备参数值
                    var paramValues = new object[methodParams.Length];
                    bool allParamsMatched = true;

                    // 按照参数名称匹配来填充 paramValues
                    for (int i = 0; i < methodParams.Length; i++)
                    {
                        // 找到与当前参数名对应的参数值
                        var matchingParameter = parameters.FirstOrDefault(p => p.ParaName == methodParams[i].Name);
                        if (matchingParameter != null)
                        {
                            paramValues[i] = matchingParameter.value; // 假设所有值都是字符串
                        }
                        else
                        {
                            allParamsMatched = false;
                            Console.WriteLine($"参数 '{methodParams[i].Name}' 与 '{actionName}'操作不匹配.");
                        }
                    }

                    if (allParamsMatched)
                    {
                        // 调用找到的方法
                        DatabaseOP.Log($"触发了Action：'{actionName}' .");
                        method.Invoke(this, paramValues);
                    }
                }
                else
                {
                    DatabaseOP.Log($"Action '{actionName}' 不存在.");
                }
            }
        }


        public class Parameter
        {
            public string ParaName { get; set; }
            public string value { get; set; }
        }

    }
}


