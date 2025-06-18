using SerialConverter.Plugins.VirtualSerial.VirtualPortCore.Model;
using SerialConverter.Plugins.VirtualSerial.VirtualPortCore.RunCommand;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace SerialConverter.Plugins.VirtualSerial.VirtualPortCore
{
    public class VirtualPortS
    {
        private readonly string _regex_Index = @"(?<=CNC[A|B])\d";
        private readonly string _regex_PortName = @"(?<=PortName=)[\w-]+(?=,|$)";
        private readonly string _regex_Type = @"(?<=CNC)[A-Z]";
        public VirtualPortS()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) throw new Exception("仅支持Windows");
            if (!IsAdministrator()) throw new Exception("需要管理员权限");
        }

        /// <summary>
        /// 是否是管理员权限
        /// </summary>
        /// <returns></returns>
        private bool IsAdministrator()
        {
            bool result;
            try
            {
                #pragma warning disable CA1416 //验证平台兼容性
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                result = principal.IsInRole(WindowsBuiltInRole.Administrator);
                #pragma warning restore CA1416 //验证平台兼容性
            }
            catch { result = false; }
            return result;
        }

        /// <summary>
        /// 列出虚拟串口
        /// </summary>
        /// <returns></returns>
        public CrossoverPortPair[] List()
        {
            string[] result = CommandRuner.RunCommandWithProcess("list");
            return result
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p =>
                {
                    int index = Convert.ToInt32(Regex.Match(p, _regex_Index).Value);
                    string type = Regex.Match(p, _regex_Type).Value;
                    string portName = Regex.Match(p, _regex_PortName).Value;
                    return (index, type, portName);
                })
                .GroupBy(p => p.index)
                .Select(p =>
                {
                    int idx = p.Key;
                    return new CrossoverPortPair(idx, p.FirstOrDefault(x => x.type == "A").portName.ToUpper(), p.FirstOrDefault(x => x.type == "B").portName.ToUpper());
                }).ToArray();
        }

        /// <summary>
        /// 获取所有占用的串口
        /// </summary>
        /// <returns></returns>
        public string[] GetALLComName()
        {
            return CommandRuner.RunCommandWithProcess("busynames COM?*");
        }

        /// <summary>
        /// 端口是否被占用
        /// </summary>
        /// <param name="PortName"></param>
        /// <returns></returns>
        public bool CheckIsBusy(string PortName)
        {
            string[] BusyList = GetALLComName();
            if (BusyList.Where(x => x.ToUpper() == PortName.ToUpper()).Count() > 0) return true;
            return false;
        }

        /// <summary>
        /// 创建虚拟串口对
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public string[] CreateVirtualSerialPort(string first = "COM201", string second = "COM202")
        {
            return CommandRuner.RunCommandWithProcess($"install PortName={first},EmuBR=yes PortName={second},EmuBR=yes");
        }

        /// <summary>
        /// 创建虚拟串口对
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public string[] CreateVirtualSerialPort(string first = "COM203")
        {
            return CommandRuner.RunCommandWithProcess($"install PortName={first} PortName=-,HiddenMode=yes");
        }

        /// <summary>
        /// 移除串口对
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public string[] RemoveVirtualSerialPort(int index)
        {
            return CommandRuner.RunCommandWithProcess($"remove {index}");
        }

        /// <summary>
        /// 移除所有串口
        /// </summary>
        /// <returns></returns>
        public string[] RemoveAllVirtualSerialPortWithoutUninstall()
        {
            CrossoverPortPair[] portPairs = List();
            List<string> result = new List<string>();
            foreach (CrossoverPortPair pair in portPairs)
            {
                result.AddRange(CommandRuner.RunCommandWithProcess($"remove {pair.PairNumber}"));
            }
            return result.ToArray();
        }

        /// <summary>
        /// 清空串口对
        /// </summary>
        /// <returns></returns>
        public string[] RemoveAllVirtualSerialPort()
        {
            return CommandRuner.RunCommandWithProcess($"uninstall");
        }
    }
}