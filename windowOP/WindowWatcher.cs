using System;
using System.Text;
using System.Runtime.InteropServices;  // 确保引入此命名空间
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.ApplicationServices;

namespace windowOP
{
    public class WindowWatcher
    {
        // 导入 User32.dll
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        // 导入 Psapi.dll
        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, int nSize);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        static Actions action = new Actions();

        public static void windowWatcher()
        {
            Console.WriteLine("窗口监视开始");

            while (true)
            {
                IntPtr hWnd = GetForegroundWindow();
                int length = GetWindowTextLength(hWnd);
                if (length == 0) continue;
                StringBuilder windowTitle = new StringBuilder(length + 1);
                GetWindowText(hWnd, windowTitle, windowTitle.Capacity);

                string exeName = "";
                // 获取前台窗口的进程ID
                GetWindowThreadProcessId(hWnd, out int processId);
                IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);

                if (hProcess != IntPtr.Zero)
                {
                    StringBuilder processName = new StringBuilder(1024);
                    if (GetModuleFileNameEx(hProcess, IntPtr.Zero, processName, processName.Capacity))
                    {
                        // 如果操作成功，processName 包含文件名
                        exeName = Path.GetFileName(processName.ToString());
                        //Console.WriteLine($"前台窗口的应用程序名：{exeName}");
                    }
                    //Marshal.Release(hProcess);
                }

                Task.Run(() => judge(windowTitle.ToString(), exeName, processId));

                Thread.Sleep(DatabaseOP.SleepTime);
            }
        }

        public static bool MatchRegularExpressions(string input, string pattern)
        {
            if (input == null || pattern == null)
            {
                return false;
            }

            return Regex.IsMatch(input, pattern);
        }

        public static void log(string msg)
        {
            if (DatabaseOP.Log_WindowWatcher) DatabaseOP.LogWindowWatcher(msg);
        }

        public static void judge(string windowTitle, string ApplicationName, int pid)
        {
            log($"窗口名--{windowTitle}");
            foreach (var target in DatabaseOP.TargetList)
            {
                log($"--规则：{target.TargetSign}");

                if (target.TargetSign.StartsWith("//") && target.TargetSign.EndsWith("//") && target.TargetSign.Substring(2, target.TargetSign.Length - 4).Replace("/", "").Length > 0)
                {
                    // 前后均为//，且不只有/的字符串，被视为正则表达式
                    string RegularExpression = target.TargetSign.Substring(2, target.TargetSign.Length - 4);
                    if (!MatchRegularExpressions(windowTitle, RegularExpression))
                    {
                        log("#######不通过正则表达式");
                        continue;
                    }
                }

                else if (target.TargetSign != "" && !windowTitle.Contains(target.TargetSign))
                {
                    log("#######不包含关键词");
                    continue;
                }
                if (target.PassingSign != "" && windowTitle.Contains(target.PassingSign))
                {
                    log($"#######包含忽略词‘{target.PassingSign}’");
                    continue;
                }
                if (target.ApplicationName != "" && !ApplicationName.Contains(target.ApplicationName))
                {
                    log("#######不包含指定程序名");
                    continue;
                }
                DatabaseOP.Log("#######触发规则" + target.TargetSign + "\n执行：" + target.Action);
                    Task.Run(() => action.ExecuteActions(target.Action));
                
            }

        }
    }
}