using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Text;


namespace windowOP
{
    public class Protect
    {

        public class Bat
        {
            public static bool Running = false;
            public static int BatPid = -1;

            public static void Start()
            {
                Running = true;
                SetProcessProtect();
                DatabaseOP.Log("双进程保护开启");
                ExitHook.Register(() => { Stop(); });
            }

            public static void Stop()
            {
                Running = false;
                if (BatPid != -1)
                {
                    Actions.RunBatchAsAdmin($"taskkill /pid {Protect.Bat.BatPid} /f");
                }
                DatabaseOP.Log("双进程保护关闭");
            }

            public static void ContinueProtect(int pid)
            {
                Running = true;
                BatPid = pid;
                Task.Run(() => ProcessHolder(BatPid));
                DatabaseOP.Log($"以旧进程pid--{BatPid}进行双进程保护");
            }

            public static void ProcessHolder(int pid)
            {
                while (Running)
                {
                    if (!IsProcessRunning(pid))
                    {
                        Console.WriteLine($"pid为{pid}的保护进程已结束，开始新进程");
                        SetProcessProtect();
                        return;
                    }
                    Thread.Sleep(300);
                }
                
            }

            public static void SetProcessProtect()
            {
                string tempPSFilePath = Path.Combine(Path.GetTempPath(), Actions.Random() + ".ps1");

                string folderPath = AppDomain.CurrentDomain.BaseDirectory;

                string command = $@"
$pidToCheck = {Setting.pid}
$folderPath = '{folderPath}'
$currentPID = $PID

while ($true) {{
    Write-Output ""PID: $pidToCheck""
    
    $processIsRunning = Get-Process -Id $pidToCheck -ErrorAction SilentlyContinue

    if (-not $processIsRunning) {{
        
        $executablePath = Join-Path -Path $folderPath -ChildPath ""windowOP.exe""
        $newProcess = Start-Process -FilePath $executablePath -ArgumentList ""--pspid"", $currentPID -PassThru

        if ($newProcess) {{
            $pidToCheck = $newProcess.Id
            Write-Output "" new PID: $pidToCheck""
        }} else {{
            Write-Output ""fail start""
        }}
    }}

    Start-Sleep -Milliseconds 30
}}
";

                try
                {
                    // 将命令写入临时 PowerShell 脚本文件
                    File.WriteAllText(tempPSFilePath, command);

                    Process process = new Process();
                    process.StartInfo.FileName = "powershell";
                    process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempPSFilePath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();

                    BatPid = process.Id;
                    Task.Run(() => ProcessHolder(BatPid));
                }
                catch (Exception ex)
                {
                    // 处理异常
                    Console.WriteLine(ex.Message);
                }
            }

            public static bool IsProcessRunning(int pid)
            {
                try
                {
                    // 尝试获取进程
                    Process process = Process.GetProcessById(pid);
                    return !process.HasExited; // 进程未退出则返回 true
                }
                catch (ArgumentException)
                {
                    // 进程不存在的异常处理
                    return false;
                }
                catch (Exception ex)
                {
                    // 处理其他可能的异常
                    Console.WriteLine($"检查进程时出错: {ex.Message}");
                    return false;
                }
            }
        }

        public class StartTask
        {
            public static bool Running = true;

            public static void Start()
            {
                Running = true;
                if (!Setting.AutoRun.VerifyVbs())
                {
                    Setting.AutoRun.WriteVbs();
                }
                Task.Run(() => StartTaskHolder());
                DatabaseOP.Log("正在保护开机启动项");
            }

            public static void Stop()
            {
                Running = false;
                DatabaseOP.Log("停止保护开机启动项");
            }

            public static void StartTaskHolder()
            {
                Files.LockFile(Setting.AutoRun.vbsPath);
                while (Running)
                {
                    if (!Setting.AutoRun.AutoRun_Query())
                    {
                        Setting.AutoRun.AutoRun_Create();
                        DatabaseOP.Log("开机启动计划任务被删除，正在还原");
                    }
                    Thread.Sleep(200);
                }
                Files.UnlockFile(Setting.AutoRun.vbsPath);
            }
        }

        public class Files
        {

            static string[] dirToProtect = new string[]
            {
                Path.Combine(Setting.programDir, "runtimes"),
                Path.Combine(Setting.programDir, "webFile")
            };

            static List<string> filesToProtect = new List<string>
            {
                Path.Combine(Setting.programDir, "windowOP.deps.json"),
                Path.Combine(Setting.programDir, "windowOP.runtimeconfig.json"),
            };

            public static Dictionary<string, FileStream> LockedFiles = new Dictionary<string, FileStream> {};

            public static void AddFilesFromDirs()
            {
                // 遍历 dirToProtect 中的每个目录
                foreach (string dir in dirToProtect)
                {
                    // 检查目录是否存在
                    if (Directory.Exists(dir))
                    {
                        // 获取该目录下所有文件的相对路径并添加到 filesToProtect 列表中
                        foreach (string file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                        {
                            // 相对路径
                            string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file);
                            filesToProtect.Add(relativePath);
                        }
                    }
                    else
                    {
                        DatabaseOP.LogErr($"需保护的目录 '{dir}' 不存在");
                    }
                }
            }

            public static void Start() 
            {
                if(filesToProtect.Count  == 2) AddFilesFromDirs();
                foreach (string file in filesToProtect)
                {
                    string TrueFile = Path.Combine(Setting.programDir, file);
                    LockFile(TrueFile);
                }
                DatabaseOP.Log("文件保护完成");
            }

            public static void Stop()
            {
                foreach (string file in filesToProtect)
                {
                    string TrueFile = Path.Combine(Setting.programDir, file);
                    UnlockFile(TrueFile);
                }
                DatabaseOP.Log("文件保护关闭");

            }


            public static void LockFile(string filePath)
            {
                try
                {
                    FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    LockedFiles.Add(filePath, stream);
                }
                catch (Exception e) {
                    DatabaseOP.LogErr($"锁定文件时发生错误：{e}");
                }
            }

            public static void UnlockFile(string filePath)
            {
                try
                {
                    if (LockedFiles.TryGetValue(filePath, out FileStream stream))
                    {
                        // 释放 FileStream
                        stream.Close(); // 关闭流
                        stream.Dispose();
                        LockedFiles.Remove(filePath); // 从字典中移除
                    }
                }
                catch (Exception ex)
                {
                    DatabaseOP.LogErr($"关闭文件流时发生错误: {ex.Message}");
                }
            }


        }



        public static void StartService()
        {
            if (DatabaseOP.Setting_Read("Protect_Process") == "1" && !Bat.Running)
            {
                Bat.Start();
            }

            if (DatabaseOP.Setting_Read("Protect_StartTask") == "1")
                Task.Run(() => StartTask.Start());

            if (DatabaseOP.Setting_Read("Protect_Files") == "1")
                Task.Run(() => Files.Start());

        }



    }
}
