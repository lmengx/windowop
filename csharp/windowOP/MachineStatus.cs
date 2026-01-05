using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;

public static class MachineStatus
{
    // 性能计数器（仅使用 .NET 内置）
    private static readonly PerformanceCounter cpuCounter = new("Processor", "% Processor Time", "_Total");
    private static readonly PerformanceCounter diskReadCounter = new("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
    private static readonly PerformanceCounter diskWriteCounter = new("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
    private static readonly PerformanceCounter networkSentCounter;
    private static readonly PerformanceCounter networkRecvCounter;

    static MachineStatus()
    {
        // 预热性能计数器
        _ = cpuCounter.NextValue();
        _ = diskReadCounter.NextValue();
        _ = diskWriteCounter.NextValue();

        // 初始化网络计数器（选活跃网卡）
        var netInterface = GetActiveNetworkInterfaceName();
        if (!string.IsNullOrEmpty(netInterface))
        {
            try
            {
                networkSentCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", netInterface);
                networkRecvCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", netInterface);
                _ = networkSentCounter.NextValue();
                _ = networkRecvCounter.NextValue();
            }
            catch { /* 忽略失败 */ }
        }

        Thread.Sleep(100); // 等待首次采样
    }

    public static string GetMachineStatusJson()
    {
        try
        {
            var status = new
            {
                Operation = "MachineStatus",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                CPU = GetCpuInfo(),
                Memory = GetMemoryInfo(),
                Disks = GetLogicalDisksInfo(),
               // GPUs = GetGpuInfoFallback(), // 无 WMI 时的降级方案
                //Network = GetNetworkIo()
            };
            return JsonConvert.SerializeObject(status, Formatting.None);
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { error = "获取机器状态失败: " + ex.Message });
        }
    }

    // ====== CPU 使用率 + 核心数 ======
    private static object GetCpuInfo()
    {
        float usage = cpuCounter.NextValue(); // 第二次读取才有效
        return new
        {
            CpuModel = GetCpuModel(),
            CoreCount = Environment.ProcessorCount,
            UsagePercent = Math.Round(Math.Max(0, Math.Min(100, usage)), 1) // 限制在 0~100
        };
    }

    private static string GetCpuModel()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("ProcessorNameString")?.ToString()?.Trim()
                       ?? "Unknown CPU Model";
            }
            catch
            {
                return "Failed to read CPU model from registry";
            }
        }
        else
        {
            // Linux: 可读 /proc/cpuinfo（此处省略）
            return "CPU model not available on this platform";
        }
    }

    // ====== 内存（Windows P/Invoke）======
    private static object GetMemoryInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var mem = GetPhysicalMemoryWindows();
            long totalMB = mem.Total / (1024 * 1024);
            long usedMB = (mem.Total - mem.Available) / (1024 * 1024);
            float percent = totalMB > 0 ? (float)(usedMB * 100.0 / totalMB) : 0;
            return new
            {
                TotalMB = totalMB,
                UsedMB = usedMB,
                UsagePercent = Math.Round(percent, 1)
            };
        }
        else
        {
            // Linux/macOS 可扩展（此处简化）
            return new { TotalMB = 0L, UsedMB = 0L, UsagePercent = 0.0f };
        }
    }

    // ====== 逻辑磁盘（C:, D:...）======
    private static List<object> GetLogicalDisksInfo()
    {
        var disks = new List<object>();
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    long total = drive.TotalSize;
                    long free = drive.AvailableFreeSpace;
                    long used = total - free;
                    float usagePercent = total > 0 ? (float)(used * 100.0 / total) : 0;

                    disks.Add(new
                    {
                        DriveLetter = drive.Name.TrimEnd('\\'),
                        TotalGB = Math.Round(total / (1024.0 * 1024 * 1024), 1),
                        FreeGB = Math.Round(free / (1024.0 * 1024 * 1024), 1),
                        UsagePercent = Math.Round(usagePercent, 1),
                        FileSystem = drive.DriveFormat ?? "Unknown"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            disks.Add(new { Error = "Failed to read disk info: " + ex.Message });
        }
        return disks;
    }

    // ====== GPU（无 WMI 时的占位）======
    //private static List<object> GetGpuInfoFallback()
    //{
    //    // 纯 .NET 无法获取 GPU 信息
    //    return new List<object>
    //    {
    //        new { Name = "GPU details require WMI or external tools (e.g., nvidia-smi)" }
    //    };
    //}

    // ====== 网络 I/O ======
    //private static object GetNetworkIo()
    //{
    //    try
    //    {
    //        float sent = networkSentCounter?.NextValue() ?? 0;
    //        float recv = networkRecvCounter?.NextValue() ?? 0;
    //        return new
    //        {
    //            BytesSentPerSec = (long)Math.Max(0, sent),
    //            BytesReceivedPerSec = (long)Math.Max(0, recv)
    //        };
    //    }
    //    catch
    //    {
    //        return new { BytesSentPerSec = 0L, BytesReceivedPerSec = 0L };
    //    }
    //}

    // ====== 辅助：Windows 内存（P/Invoke）======
    private static (long Total, long Available) GetPhysicalMemoryWindows()
    {
        MEMORYSTATUSEX memStatus = new();
        memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        GlobalMemoryStatusEx(memStatus);
        return ((long)memStatus.ullTotalPhys, (long)memStatus.ullAvailPhys);
    }

    private static string GetActiveNetworkInterfaceName()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(nic => nic.GetIPv4Statistics().BytesReceived)
                .FirstOrDefault()?.Name;
        }
        catch { return null; }
    }

    // ====== P/Invoke for Windows Memory ======
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
}