using CommunityToolkit.Mvvm.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;

namespace SerialConverter.Plugins.About
{
    public partial class MainUIViewModel:ObservableObject
    {
        [ObservableProperty]
        string systemInfo;
        public MainUIViewModel()
        {
            List<string> Info = new List<string>();
            Info.Add($"系统版本\r\n{Environment.OSVersion}");
            Info.Add($" ");
            Info.Add($"系统位数\r\n{(Environment.Is64BitOperatingSystem ? "64位" : "32位")}");
            Info.Add($" ");
            Info.Add($"系统描述\r\n{GetOSDescription()}");
            Info.Add($" ");
            Info.Add($"计算机名\r\n{Environment.MachineName}");
            Info.Add($" ");
            Info.Add($"处理器\r\n{GetProcessorInfo()}");
            Info.Add($" ");
            Info.Add($"总内存\r\n{FormatBytes(GetTotalMemory())}");
            Info.Add($" ");
            Info.Add($"可用内存\r\n{FormatBytes(GetAvailableMemory())}");
            Info.Add($" ");
            Info.Add($"DotNET版本\r\n{Environment.Version}");
            Info.Add($" ");
            Info.Add($"运行时\r\n{RuntimeInformation.FrameworkDescription}");
            Info.Add($" ");
            Info.Add($"软件版本\r\nV0.0.1");
            Info.Add($" ");
            Info.Add($"本软件基于com0com虚拟串口,SukiUI以及Avalonia开发");
            SystemInfo = string.Join("\r\n", Info);
        }

        private string GetOSDescription()
        {
            #pragma warning disable CA1416 // 验证平台兼容性
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
            {
                foreach (var os in searcher.Get())
                {
                    return (os["Caption"].ToString()??"").Trim();
                }
            }
            #pragma warning restore CA1416 // 验证平台兼容性
            return "Unknown";
        }

        private string GetProcessorInfo()
        {
            #pragma warning disable CA1416 // 验证平台兼容性
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
            {
                foreach (var processor in searcher.Get())
                {
                    return (processor["Name"].ToString()??"").Trim();
                }
            }
            #pragma warning restore CA1416 // 验证平台兼容性
            return "Unknown";
        }

        private ulong GetTotalMemory()
        {
            #pragma warning disable CA1416 // 验证平台兼容性
            using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
            {
                foreach (var os in searcher.Get())
                {
                    return Convert.ToUInt64(os["TotalVisibleMemorySize"]) * 1024; // KB to bytes
                }
            }
            #pragma warning restore CA1416 // 验证平台兼容性
            return 0;
        }

        private ulong GetAvailableMemory()
        {
            #pragma warning disable CA1416 // 验证平台兼容性
            using (var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem"))
            {
                foreach (var os in searcher.Get())
                {
                    return Convert.ToUInt64(os["FreePhysicalMemory"]) * 1024; // KB to bytes
                }
            }
            #pragma warning restore CA1416 // 验证平台兼容性
            return 0;
        }

        private string FormatBytes(ulong bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {suffixes[order]}";
        }
    }
}
