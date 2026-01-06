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
        public static async Task DownloadFrpc(CancellationToken cancellationToken = default)
        {
            string downloadDir = Setting.programDir;

            var downloadUrls = new Dictionary<Architecture, string>
    {
        { Architecture.X64, "https://nya.globalslb.net/natfrp/client/frpc/0.51.0-sakura-11.1/frpc_windows_amd64.exe" },
        { Architecture.X86, "https://nya.globalslb.net/natfrp/client/frpc/0.51.0-sakura-11.1/frpc_windows_386.exe" },
        { Architecture.Arm64, "https://nya.globalslb.net/natfrp/client/frpc/0.51.0-sakura-11.1/frpc_windows_arm64.exe" }
    };

            Architecture currentArchitecture = RuntimeInformation.ProcessArchitecture;

            if (!downloadUrls.TryGetValue(currentArchitecture, out string downloadUrl))
            {
                Console.WriteLine($"不支持当前的系统架构: {currentArchitecture}. 无法下载文件.");
                return;
            }

            Console.WriteLine($"检测到系统架构: {currentArchitecture}");
            Console.WriteLine($"开始下载文件: {downloadUrl}");

            if (!Directory.Exists(downloadDir))
            {
                Directory.CreateDirectory(downloadDir);
            }

            string filePath = Path.Combine(downloadDir, "Frpc.exe");

            // 指数退避重试参数
            int retryCount = 0;
            const int maxDelaySeconds = 256; // 最大退避时间

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using (var client = new HttpClient())
                    {
                        // 可选：设置超时（避免卡死）
                        client.Timeout = TimeSpan.FromSeconds(60);

                        byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl, cancellationToken);
                        await File.WriteAllBytesAsync(filePath, fileBytes, cancellationToken);

                        Console.WriteLine($"✅ 文件下载成功并保存到: {filePath}");
                        return; // 成功则退出
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("⚠️ 下载被取消。");
                    throw;
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is IOException)
                {
                    retryCount++;
                    int delaySeconds = Math.Min(1 << (retryCount - 1), maxDelaySeconds); // 1, 2, 4, 8, ..., 256

                    Console.WriteLine($"❌ 下载失败 (尝试 #{retryCount}): {ex.Message}");
                    Console.WriteLine($"⏳ 等待 {delaySeconds} 秒后重试...");

                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            Console.WriteLine("⚠️ 重试被取消。");
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        // 非网络异常（如磁盘满、权限问题）—— 不重试
                        Console.WriteLine($"💥 发生不可恢复错误，停止重试: {ex.Message}");
                        throw;
                    }
                }
            }


        }

}