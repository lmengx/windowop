using CommandLine;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using static windowOP.DatabaseOP;

namespace windowOP
{
    public class WindowOP
    {

    public class Options
        {
            [Option('u', "update", Default = false, HelpText = "Update the target list.")]
            public bool Update { get; set; }

            [Option("pspid", HelpText = "Specify the batpid value.")]
            public string PSpid { get; set; }

            public static void ProcessArgs(string[] args)
            {
                Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
            .WithNotParsed<Options>((errs) => Console.WriteLine($"参数读取错误：{errs}"));
            }

            static void RunOptionsAndReturnExitCode(Options opts)
            {
                if (opts.Update)
                {
                    HandleUpdate();
                }

                if (!string.IsNullOrEmpty(opts.PSpid)) // 检查 batpid 参数是否提供
                {
                    if(int.TryParse(opts.PSpid, out int PSpid))
                        Protect.Bat.ContinueProtect(PSpid);
                }

            }

            static void HandleUpdate()
            {
                FirstToUse = false;
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
        }


        public static bool FirstToUse = false;

        public static void ini(string[] args)
        {
            //Actions.HideConsole();

            if (!ExistDatabase())
            {
                FirstToUse = true;
                DatabaseOP.CreateDB();
            }

            try
            {
                Options.ProcessArgs(args);
            }
            catch (Exception) { }

            DatabaseOP.InsertSetting();
            Task.Run(() => Protect.StartService());

            if (DatabaseOP.Setting_Read("RunAsAdmin") == "1")
                {
                    Actions.TryRestartAsAdmin();
                }
            ReadToMemory("Log_Enable");
            ReadToMemory("Log_PrintDebugMsg");
            ReadToMemory("SleepTime");
            ReadToMemory("ListenPort");
            ReadToMemory("Log_WindowWatcher");
            ReadToMemory("Log_WebServer");
            ReadToMemory_TargetList();


            

            if (Setting_Read("Log_ShowConsole") == "1") Actions.ShowConsole();
                if(FirstToUse) Actions.OpenManagePage();

            if (Setting_Read("Frp_Enable") == "1") Task.Run(() => Frp.StartFrpc());



        }

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        public static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine(args[i]);
            }


            //FreeConsole();
            ini(args);
            Task.Run(() => WebServer.webServer());

            Thread windowWatcherThread = new Thread(WindowWatcher.windowWatcher);
            windowWatcherThread.Start();
            // 防止主线程退出

            Console.WriteLine("初始化完成.");

            Task.Delay(-1).Wait();
            /*
             * 下一步准备实现：

             * 2.Actions的完善，分类分页
             * 3.前端
             * 4.监护线程及其退出逻辑（ws连接）
             * 5.创建表
            */
        }
    }
}