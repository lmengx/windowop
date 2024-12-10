using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Security.Principal;
using System.Threading;
using static System.Formats.Asn1.AsnWriter;


namespace windowOP
{
    public class Setting
    {
        public static int pid = Process.GetCurrentProcess().Id;
        public static string programPath = Process.GetCurrentProcess().MainModule.FileName;
        public static string programDir = AppDomain.CurrentDomain.BaseDirectory;
        public static string system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
        public static int OperatingSYstemBit = (Environment.Is64BitOperatingSystem) ? 64 : 32;

        public static string GiteeStorageUrl = "https://gitee.com/lmx12330/window-op/raw/master/";

        public class RegKey_WorkFile
        {
            static string subKey = @"Software\WindowOP";
            static string valueName = "WorkFile";

            public static void WriteKey()
            {
                if (programDir == system32Path) return;
                string valueData = programDir; // 要保存的值数据
                try
                {
                    // 打开或创建子键
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(subKey))
                    {
                        if (key != null)
                        {
                            // 设置字符串值
                            key.SetValue(valueName, valueData);
                            DatabaseOP.Log("成功保存到注册表.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    DatabaseOP.LogErr("保存到注册表时发生错误: " + ex.Message);
                }
            }

            public static string ReadKey() 
            {
                try
                {
                    // 打开子键
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(subKey))
                    {
                        if (key == null) return "";

                            // 读取字符串值
                            object valueData = key.GetValue(valueName);
                        if (valueData == null) return "";
                            
                       // Console.WriteLine($"读取的值: {valueData}");
                       return valueData.ToString();
                    }
                }
                catch (Exception ex)
                {
                    DatabaseOP.LogErr("读取注册表时发生错误: " + ex.Message);
                    return "";
                }
            }

            public static void RemoveKey()
            {
                try
                {
                    // 打开子键
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(subKey, true)) // 需要以可写模式打开
                    {
                        if (key != null)
                        {
                            // 删除字符串值
                            key.DeleteValue(valueName, throwOnMissingValue: false);
                            DatabaseOP.Log("成功删除注册表值.");
                        }
                        else
                        {
                            DatabaseOP.Log("未找到指定的子键.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    DatabaseOP.LogErr("删除注册表值时发生错误: " + ex.Message);
                }
            }

        }

        public static string? GetStringFromUrl(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = client.GetAsync(url).Result;
                    response.EnsureSuccessStatusCode();
                    string content = response.Content.ReadAsStringAsync().Result;
                    return content;
                }
                catch (Exception ex)
                {
                    DatabaseOP.LogErr("从url获取文本失败: " + ex.Message);
                    return null; // 或者抛出异常，或者返回默认值
                }
            }
        }

        public static class Version
        {
            public const string version = "0.0.0.1";

            public static string GetLatestVersion()
            {
                string url = "https://gitee.com/lmx12330/window-op/raw/master/resource/version.txt";
                string? result = GetStringFromUrl(url);
                if (result != null) return result;
                return version;
            }

            public static string TransferDirectory(string filePath)
            {
                try
                {                  
                    filePath = filePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    Directory.CreateDirectory(filePath);
                    DirectoryInfo directoryInfo = new DirectoryInfo(filePath);
                    string lastFolderName = directoryInfo.Name;
                    if (!string.Equals(lastFolderName, "windowop", StringComparison.OrdinalIgnoreCase))// 比较最后一个文件夹名称 (不区分大小写)
                    {
                        filePath = Path.Combine(filePath, "windowOP");
                        Directory.CreateDirectory(filePath);

                    }

                    File.Copy(programDir, filePath, true);
                    string psContent = @$"
$fileToDelete = '{Setting.programDir}'
$filePath = '{filePath}'
$pidToCheck = {Setting.pid}

while ($true) {{   
    $processIsRunning = Get-Process -Id $pidToCheck -ErrorAction SilentlyContinue

    if (-not $processIsRunning) {{
        $executablePath = Join-Path -Path $folderPath -ChildPath ""windowOP.exe""
        $newProcess = Start-Process -FilePath $executablePath -PassThru

        Remove-Item -Path $tempFolderPath -Recurse -Force
        pause
        exit
    }}

    Start-Sleep -Milliseconds 1000
}}
";

                    Task.Run(() => { Actions.RunPs(psContent, "1"); });
                    return "脚本部署完成，正在转移";
                }
                catch (IOException ioEx)
                {
                    string message = $"更改目录失败：文件操作异常: {ioEx.Message}";
                    DatabaseOP.LogErr(message);
                    return message;
                }
                catch (Exception ex)
                {
                    string message = $"更改目录失败：出现异常: {ex.Message}";
                    DatabaseOP.LogErr(message);
                    return message;
                }
            }


            public static void Update()
            {
                string url = "https://gitee.com/lmx12330/window-op/raw/master/resource/windowOP.zip";
                string tempPath = Path.Combine(Path.GetTempPath(), Actions.Random().ToString());
                string zipPath = Path.Combine(tempPath, "windowOP.zip");


                Actions.download(url, zipPath);
                Actions.ExtractZipFile(zipPath, tempPath);
                if (File.Exists(zipPath)) File.Delete(zipPath);

                //导出targetlist与setting
                string Path_SettingJson = Path.Combine(tempPath, "Setting.json");
                Actions.WriteToFile(tempPath, DatabaseOP.Setting_Read_Batch());

                string Path_TargetListJson = Path.Combine(tempPath, "TargetList.json");
                Actions.WriteToFile(Path_TargetListJson, DatabaseOP.GetTargetList());

                string folderPath = AppDomain.CurrentDomain.BaseDirectory;

                // 创建批处理文件内容
                string batContent = $@"
@echo off
setlocal enabledelayedexpansion
set pidToCheck={pid}
set folderToDelete={folderPath}
set sourceFolder={tempPath}

:check_process
tasklist /FI ""PID eq !pidToCheck!"" | findstr /I ""!pidToCheck!"" >nul
if errorlevel 1 (
    rem 如果进程不存在，先清空目标文件夹
    call :DeleteFolderContents ""!folderToDelete!""

    rem 然后剪切文件
    xcopy /s /e /q /y ""!sourceFolder!\*"" ""!folderToDelete!""
    rmdir /s /q ""!sourceFolder!""

    rem 启动 windowOP.exe
    start """" ""!folderToDelete!\windowOP.exe""
    exit
)

timeout /t 1 >nul
goto check_process

:DeleteFolderContents
set folderPath=%1

rem 删除文件
for %%f in (""%folderPath%\*"") do (
    del /q ""%%f""
)

rem 删除子目录
for /d %%d in (""%folderPath%\*"") do (
    rmdir /s /q ""%%d""
)

exit
";
                Task.Run(() => Actions.cmd(batContent));
                Thread.Sleep(3000);
                Actions.exit();
            }


            public static void Delete()
            {
                //移除保护服务（如RTCore64.sys）
                Protect.Bat.Stop();
                AutoRun.AutoRun_Remove();
                if(RegKey_WorkFile.ReadKey() != "") RegKey_WorkFile.RemoveKey();

                string folderPath = AppDomain.CurrentDomain.BaseDirectory;
                string batContent = $@"
@echo off
setlocal enabledelayedexpansion
set pidToCheck={pid}
set folderToDelete={folderPath}

:check_process
tasklist /FI ""PID eq !pidToCheck!"" | findstr /I ""!pidToCheck!"" >nul
if errorlevel 1 (
    rem 如果进程不存在，清空目标文件夹
    goto :DeleteFolderContents
)

timeout /t 1 >nul
goto check_process

:DeleteFolderContents

rem 删除文件
for %%f in (""%folderToDelete%\*"") do (
    del /q ""%%f""
)

rem 删除子目录
for /d %%d in (""%folderToDelete%\*"") do (
    rmdir /s /q ""%%d""
)

exit
";
                Task.Run(() => Actions.RunBatchAsAdmin(batContent));
                Thread.Sleep(3000);
                Actions.exit();
            }


        }

        public static class AutoRun
        {
            static string StartupTaskName = "windowOP";
            public static string vbsPath = Path.Combine(programDir, "StartTask.vbs");
            static string vbsContent = $@"
Set objShell = CreateObject(""WScript.Shell"")
objShell.Run ""{programPath}"", 0, False
".Trim();

            public static void WriteVbs()
            {
                File.WriteAllText(vbsPath, vbsContent);
            }

            public static bool VerifyVbs()
            {
                if (!File.Exists(vbsPath)) return false;   
                
                    string vbsNowContent = File.ReadAllText(vbsPath);
                if (vbsNowContent == vbsContent)
                {
                    return true;
                }
                return false;
            }

            public static void AutoRun_Remove()
            {
                string cmdContent = @$"schtasks /delete /tn {StartupTaskName} /f";
                Actions.RunBatchAsAdmin(cmdContent);
            }

            public static void AutoRun_Create()
            {
                AutoRun_Remove();

                WriteVbs();

                string RunCommand = $"{vbsPath}";
                string cmdContent = @$"schtasks /create /tn {StartupTaskName} /tr ""{RunCommand}"" /sc onlogon";

                Actions.RunBatchAsAdmin(cmdContent);
            }

            public static bool AutoRun_Query()
            {
                string cmdContent = $"schtasks /query /tn {StartupTaskName} /fo LIST /v";

                string output = Actions.cmd(cmdContent);
                //Console.WriteLine(output);
                if (output.Contains(StartupTaskName))
                {
                    string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    string RunCommand = "";
                    bool taskStatus = false;
                    bool isAtLogon = false;

                    foreach (var line in lines)
                    {
                        // 提取任务路径
                        if (line.StartsWith("Task To Run:") || line.StartsWith("要运行的任务:"))
                        {
                            RunCommand = line.Substring(line.IndexOf(':') + 1).Trim();
                        }

                        // 提取任务状态
                        if (line.StartsWith("Status:") || line.StartsWith("计划任务状态:"))
                        {

                            var taskStatusValue = line.Substring(line.IndexOf(':') + 1).Trim();
                            taskStatus = taskStatusValue.Equals("Ready", StringComparison.OrdinalIgnoreCase) || taskStatusValue.Equals("已启用", StringComparison.OrdinalIgnoreCase);
                        }

                        if (line.StartsWith("Trigger:") || line.StartsWith("计划类型:"))
                        {
                            var trigger = line.Substring(line.IndexOf(':') + 1).Trim();
                            isAtLogon = trigger.Equals("At logon", StringComparison.OrdinalIgnoreCase) || trigger.Equals("登陆时", StringComparison.OrdinalIgnoreCase);

                        }
                    }

                    string ExceptRunCommand = @$"{vbsPath}";
                    // 检查路径和状态
                    if (RunCommand == ExceptRunCommand && taskStatus && isAtLogon) // 检查是否启用
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static void AccessFireWall()
        {

            string batContent = @$"
@echo off
set programPath=""{programPath}""
netsh advfirewall firewall add rule name=""windowOP"" dir=in action=allow program=""%programPath%"" enable=yes
";

            // 运行批处理文件以管理员身份
            Actions.RunBatchAsAdmin(batContent);
        }



        public static bool IsUserAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static string GetLocalIPAddress() 
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                //过滤出IPv4地址并且排除回环地址（127.0.0.1）
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        
            return "";
        }
    }
}

