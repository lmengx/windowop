using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
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

            // 生成配置文件 (TOML格式)
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

            string zipPath = Path.Combine(downloadDir, "frp.zip");
            string extractPath = Path.Combine(downloadDir, "extract");

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(120);
                    byte[] fileBytes = await client.GetByteArrayAsync(downloadUrl, cancellationToken);
                    await File.WriteAllBytesAsync(zipPath, fileBytes, cancellationToken);
                    Console.WriteLine($"✅ 压缩文件下载成功");

                    // 解压zip文件
                    if (Directory.Exists(extractPath))
                    {
                        Directory.Delete(extractPath, true);
                    }
                    ZipFile.ExtractToDirectory(zipPath, extractPath);
                    Console.WriteLine($"✅ 解压完成");

                    // 查找frpc.exe
                    string[] frpcFiles = Directory.GetFiles(extractPath, "frpc.exe", SearchOption.AllDirectories);
                    if (frpcFiles.Length > 0)
                    {
                        File.Copy(frpcFiles[0], FrpcFile, true);
                        Console.WriteLine($"✅ frpc.exe已复制到: {FrpcFile}");
                    }
                    else
                    {
                        throw new FileNotFoundException("未在压缩文件中找到frpc.exe");
                    }

                    // 清理临时文件
                    File.Delete(zipPath);
                    Directory.Delete(extractPath, true);

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 下载失败: {ex.Message}");
                throw;
            }
        }
    }
}
