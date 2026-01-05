using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

namespace windowOP
{
    public static class ExitHook
    {
        private static readonly List<Action> _hooks = new List<Action>();
        private static readonly object _lock = new object();
        private static bool _shuttingDown = false;

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? handler, bool add);

        private delegate bool ConsoleCtrlDelegate(CtrlType sig);

        private enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool ConsoleCtrlHandler(CtrlType type)
        {
            if (type == CtrlType.CTRL_CLOSE_EVENT ||
                type == CtrlType.CTRL_LOGOFF_EVENT ||
                type == CtrlType.CTRL_SHUTDOWN_EVENT)
            {
                RunAll();
                Thread.Sleep(100); // 短暂等待日志写入（可选）
            }
            return true;
        }

        public static void Register(Action hook)
        {
            if (hook == null) return;
            lock (_lock)
            {
                if (_shuttingDown)
                    InvokeSafely(hook);
                else
                    _hooks.Add(hook);
            }
        }

        public static void RunAll()
        {
            List<Action> hooksToRun;
            lock (_lock)
            {
                if (_shuttingDown) return;
                _shuttingDown = true;
                hooksToRun = new List<Action>(_hooks);
                _hooks.Clear();
            }

            foreach (var hook in hooksToRun)
                InvokeSafely(hook);
        }

        private static void InvokeSafely(Action hook)
        {
            try
            {
                hook?.Invoke();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ExitHook] Error: {ex}");
            }
        }

        public static void AutoRegisterSystemEvents()
        {
            AppDomain.CurrentDomain.ProcessExit += (_, _) => RunAll();
            SetConsoleCtrlHandler(ConsoleCtrlHandler, true); // ← 关键！
        }
    }
}