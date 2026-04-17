using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace windowOP
{
    public class GeneralFrp
    {
        static string FrpcFile = Path.Combine(Setting.programDir, "GeneralFrp", "frpc.exe");
        public static Process FrpcProcess = null;

        public static int FrpcPid = -1;
        public static async Task StartFrpc()
        {
            if (!File.Exists(FrpcFile)) await DownloadFrpc();

            bool enable = DatabaseOP.Setting_Read("GeneralFrp_Enable") == "1";
            if (!enable)
            {
                DatabaseOP.Log("GeneralFrp_Enable 为关闭状态，跳过启动 frpc.exe");
                return;
            }

            string serverAddr = DatabaseOP.Setting_Read("GeneralFrp_ServerAddr");
            string serverPort = DatabaseOP.Setting_Read("GeneralFrp_ServerPort");
            string token = DatabaseOP.Setting_Read("GeneralFrp_Token");
            string name = DatabaseOP.Setting_Read("GeneralFrp_Name");
            string localPort = DatabaseOP.ListenPort.ToString();

            if (string.IsNullOrEmpty(serverAddr) || string.IsNullOrEmpty(serverPort) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(name))
            {
                DatabaseOP.Log("通用Frpc设置不完整，跳过启动");
                return;
            }

            string configContent = $@"serverAddr = ""{serverAddr}""
serverPort = {serverPort}
auth.token = ""{token}""

[[proxies]]
name = ""{name}""
type = ""tcp""
localIP = ""127.0.0.1""
localPort = {localPort}";

            string configPath = Path.Combine(Setting.programDir, "GeneralFrp", "frpc.toml");
            File.WriteAllText(configPath, configContent);

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = FrpcFile,
                    Arguments = $"-c {configPath}",
                    WorkingDirectory = Path.Combine(Setting.programDir, "GeneralFrp"),
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                FrpcProcess = Process.Start(startInfo);

                if (FrpcProcess != null)
                {
                    DatabaseOP.Log($"通用Frpc已启动，PID: {FrpcProcess.Id}");
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
                DatabaseOP.LogErr($"启动通用Frpc时发生错误：{ex.Message}");
            }
        }

        public static async Task DownloadFrpc(CancellationToken cancellationToken = default)
        {
            string downloadDir = Path.Combine(Setting.programDir, "GeneralFrp");
            string downloadUrl = "https://gitee.com/lmx12330/window-op/raw/master/res/frpc.exe";

            Console.WriteLine($"开始下载文件: {downloadUrl}");

            if (!Directory.Exists(downloadDir))
            {
                Directory.CreateDirectory(downloadDir);
            }

            string filePath = Path.Combine(downloadDir, "frpc.exe");

            int retryCount = 0;
            const int maxDelaySeconds = 256;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(120);
                        byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl, cancellationToken);
                        await File.WriteAllBytesAsync(filePath, fileBytes, cancellationToken);
                        Console.WriteLine($"✅ 文件下载成功并保存到: {filePath}");
                        return;
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
                    int delaySeconds = Math.Min(1 << (retryCount - 1), maxDelaySeconds);

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
                    Console.WriteLine($"💥 发生不可恢复错误，停止重试: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
