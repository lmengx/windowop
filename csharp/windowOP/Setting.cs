using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
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

                    Task.Run(() => { Actions.RunPs(psContent); });
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
                //导出targetlist与setting
                JObject Props = JObject.Parse(DatabaseOP.Setting_Read_Batch());
                Props["Password"] = DatabaseOP.Setting_Read("Password");
                Props["HmacKey"] = DatabaseOP.Setting_Read("HmacKey");
                string Path_SettingJson = Path.Combine(programDir, "Setting.json");
                Actions.WriteToFile(Path_SettingJson, Props.ToString());

                string Path_TargetListJson = Path.Combine(programDir, "TargetList.json");
                Actions.WriteToFile(Path_TargetListJson, DatabaseOP.GetTargetList());

                string folderPath = AppDomain.CurrentDomain.BaseDirectory;

                // 创建批处理文件内容
                string psContent = $@"
$pidToCheck = {pid}
$targetPath = ""{programDir}""
while ($true) {{
    if (-not (Get-Process -Id $pidToCheck -ErrorAction SilentlyContinue)) {{

$filesToKeep = @(""Setting.json"", ""TargetList.json"")# 要保留的文件
$files = Get-ChildItem -Path $targetPath
foreach ($file in $files) {{
    if ($filesToKeep -notcontains $file.Name) {{
        Remove-Item -Path $file.FullName -Recurse -Force
    }}
}}

$tempFolderName = [guid]::NewGuid().ToString()
$tempFolderPath = Join-Path -Path $env:TEMP -ChildPath $tempFolderName
$appDataPath = $env:APPDATA
New-Item -Path $tempFolderPath -ItemType Directory
$downloadurl = ""https://gitee.com/lmx12330/window-op/raw/master/resource/windowOP.zip""
$zipPath = Join-Path -Path $tempFolderPath -ChildPath ""windowOP.zip""
$exePath = Join-Path -Path $targetPath -ChildPath ""windowOP.exe""
$webClient = New-Object System.Net.WebClient
$webClient.DownloadFileAsync($downloadurl , $zipPath)
while ($webClient.IsBusy) {{Start-Sleep -Milliseconds 100}}Expand-Archive -Path $zipPath -DestinationPath $targetPath -Force
$arguments = ""--update""
Start-Process $exePath -ArgumentList $arguments
Remove-Item -Path $tempFolderPath -Recurse -Force
pause
    }}

    # 等待1秒
    Start-Sleep -Seconds 1
}}

";
                Console.WriteLine(psContent);
                Task.Run(() => Actions.RunPs(psContent));
                Thread.Sleep(3000);
                Actions.exit();
            }


            public static void Delete()
            {
                //移除保护服务（如RTCore64.sys）
                Protect.Bat.Stop();
                AutoRun.AutoRun_Remove();
                RemoveFireWallSetting(DatabaseOP.ListenPort);

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
                Actions.TryRestartAsAdmin();

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

        public static void AccessFireWall(int port)
        {

            string batContent = @$"
netsh advfirewall firewall add rule name=""windowOP"" dir=IN action=ALLOW protocol=TCP localport={port}
";

            // 运行批处理文件以管理员身份
            Actions.RunBatchAsAdmin(batContent);
        }

        public static void RemoveFireWallSetting(int port)
        {
            string batContent = @$"
        netsh advfirewall firewall delete rule name = ""windowOP"" protocol=TCP localport = { port }
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
                // 过滤出IPv4地址，并且排除回环地址（127.0.0.1）和169.254.x.x地址
                if (ip.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip) &&
                    !ip.ToString().StartsWith("169.254"))
                {
                    return ip.ToString();
                }
            }

            return "";
        }
    }
}

