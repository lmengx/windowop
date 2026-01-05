using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace windowOP
{
    public class Frp
    {
        static string FrpcFile = Path.Combine(Setting.programDir, "Frpc.exe");
        public static Process FrpcProcess = null; // 替代原来的 FrpcPid

        public static int FrpcPid = -1;
        public static async Task StartFrpc()
            {         
                if(!File.Exists(FrpcFile)) await DownloadFrpc();
                string parameters = DatabaseOP.Setting_Read("Frp_parameters");

            if (string.IsNullOrEmpty(parameters))
            {
                DatabaseOP.Log("Frp_parameters 为空，跳过启动 frpc.exe");
                return;
            }
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = FrpcFile,
                        Arguments = parameters,
                        WorkingDirectory = Setting.programDir, // 设置工作目录为frpc.exe所在的目录
                        CreateNoWindow = true,           // 隐藏窗口
                        UseShellExecute = false          // 不使用操作系统外壳启动
                    };

                    FrpcProcess = Process.Start(startInfo);

                    if (FrpcProcess != null)
                    {
                        DatabaseOP.Log($"frpc.exe 已启动，PID: {FrpcProcess.Id}");
                        ExitHook.Register(() =>
                        {
                            if (!FrpcProcess.HasExited)
                            {
                                FrpcProcess.Kill();
                                FrpcProcess.Dispose();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    DatabaseOP.LogErr($"启动 frpc.exe 时发生错误：{ex.Message}");
                }
            
            }
        public static async Task DownloadFrpc()
        {
            // 获取程序下载目录，假设 Setting.programDir 已经定义并可用
            string downloadDir = Setting.programDir;

            // 定义不同架构对应的下载 URL
            var downloadUrls = new System.Collections.Generic.Dictionary<Architecture, string>
            {
                { Architecture.X64, "https://nya.globalslb.net/natfrp/client/frpc/0.51.0-sakura-11.1/frpc_windows_amd64.exe" },
                { Architecture.X86, "https://nya.globalslb.net/natfrp/client/frpc/0.51.0-sakura-11.1/frpc_windows_386.exe" },
                { Architecture.Arm64, "https://nya.globalslb.net/natfrp/client/frpc/0.51.0-sakura-11.1/frpc_windows_arm64.exe" }
            };

            // 获取当前系统架构
            Architecture currentArchitecture = RuntimeInformation.ProcessArchitecture;

            // 根据架构获取对应的下载 URL
            if (downloadUrls.TryGetValue(currentArchitecture, out string downloadUrl))
            {
                Console.WriteLine($"检测到系统架构: {currentArchitecture}");
                Console.WriteLine($"开始下载文件: {downloadUrl}");

                try
                {
                    // 确保下载目录存在
                    if (!Directory.Exists(downloadDir))
                    {
                        Directory.CreateDirectory(downloadDir);
                    }

                    // 从 URL 中提取文件名
                    string fileName = "Frpc.exe";
                    string filePath = Path.Combine(downloadDir, fileName);

                    using (HttpClient client = new HttpClient())
                    {
                        // 发送 GET 请求并下载文件
                        byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl);

                        // 将文件保存到指定路径
                        await File.WriteAllBytesAsync(filePath, fileBytes);

                        Console.WriteLine($"文件下载成功并保存到: {filePath}");
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"下载文件时发生网络错误: {e.Message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"下载或保存文件时发生错误: {e.Message}");
                }
            }
            else
            {
                Console.WriteLine($"不支持当前的系统架构: {currentArchitecture}. 无法下载文件.");
            }
        }


    }

}