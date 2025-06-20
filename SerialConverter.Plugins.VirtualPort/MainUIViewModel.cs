using Avalonia.Controls.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SerialConverter.Core;
using SerialConverter.Plugins.VirtualSerial.Communication.Interface;
using SerialConverter.Plugins.VirtualSerial.Communication.SerialPort;
using SerialConverter.Plugins.VirtualSerial.Communication.TCP;
using SerialConverter.Plugins.VirtualSerial.DBHelper;
using SerialConverter.Plugins.VirtualSerial.VirtualPortCore;
using SerialConverter.Plugins.VirtualSerial.VirtualPortCore.Model;
using SukiUI.Dialogs;
using SukiUI.Toasts;
using System.Collections.ObjectModel;
using System.Net;
using System.Text.RegularExpressions;

namespace SerialConverter.Plugins.VirtualSerial
{
    public partial class MainUIViewModel : ObservableObject
    {
        VirtualPortS? VirtualPorter;
        string _regex_Index = @"(?<=CNC[A|B])\d";
        string _regex_PortName = @"(?<=PortName=)[\w-]+(?=,|$)";
        string _regex_Type = @"(?<=CNC)[A-Z]";
        public MainUIViewModel()
        {
            try { VirtualPorter = new VirtualPortS(); }
            catch (Exception Ex) { LogHelper.Error(Ex.Message); }
            MainComName = "COM42";
            SlaveComName = "COM48";

            AddPair = new AsyncRelayCommand(addPair);
            ListPair = new AsyncRelayCommand(listPair);
            ComMode = true;
            TcpMode = false;
            UdpMode = false;

            RemoteIP = "127.0.0.1";
            RemotePort = 8899;

            LocalIP = "127.0.0.1";
            LocalPort = 8898;
            TCPModeModeList = new List<string>() { "TcpClient", "TcpServer" };
            TcpWorkMode = "TcpClient";

            _ = Fresh();
            _ = FreshIPList();
        }
        public ObservableCollection<DataInfo> DataList { get; set; } = new ObservableCollection<DataInfo>();

        [ObservableProperty]
        private List<string>? iPList;

        /// <summary>
        /// 主串口名称
        /// </summary>
        [ObservableProperty]
        private string mainComName;

        /// <summary>
        /// 副串口名称
        /// </summary>
        [ObservableProperty]
        private string slaveComName;

        /// <summary>
        /// 主串口名称
        /// </summary>
        [ObservableProperty]
        private string? ipInfo;

        [ObservableProperty]
        private bool comMode;

        [ObservableProperty]
        private bool tcpMode;

        [ObservableProperty]
        private bool udpMode;

        [ObservableProperty]
        private string localIP;

        [ObservableProperty]
        private int localPort;

        [ObservableProperty]
        private string remoteIP;

        [ObservableProperty]
        private int remotePort;

        [ObservableProperty]
        private string tcpWorkMode;

        public List<string> TCPModeModeList { get; set; }

        /// <summary>
        /// 添加串口对
        /// </summary>
        public IAsyncRelayCommand AddPair { get; set; }
        public async Task addPair()
        {
            try
            {
                if (VirtualPorter == null) return;
                if (ComMode)
                {
                    bool IsMain = VirtualPorter.CheckIsBusy(MainComName);
                    bool IsSlave = VirtualPorter.CheckIsBusy(SlaveComName);
                    if (IsMain || IsSlave)
                    {
                        if (IsMain && IsSlave) { OpenMessageBoxStyleDialog("Error Info", $"{MainComName},{SlaveComName}已被占用"); return; }
                        if (IsMain) { OpenMessageBoxStyleDialog("Error Info", $"{MainComName}已被占用"); return; }
                        if (IsSlave) { OpenMessageBoxStyleDialog("Error Info", $"{SlaveComName}已被占用"); return; }
                    }
                    string[] Info = Array.Empty<string>();
                    await Task.Run(() =>
                    {
                        Info = VirtualPorter.CreateVirtualSerialPort(MainComName,SlaveComName);
                        Info = Info.Select(x => x.Replace("       ", "")).ToArray();
                    });

                    string[] InstallInfo = Info.Where(x => (!x.Contains("ComDB")) && (!x.Contains("Reboot"))).ToArray();
                    CrossoverPortPair? crossover = InstallInfo.Select(p =>
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
                            }).FirstOrDefault();
                    if (crossover != null)
                    {
                        DataInfo dataInfo = new DataInfo()
                        {
                            Status=true,
                            Index = crossover.PairNumber,
                            PortNameA=crossover.PortNameA,
                            PortNameB=crossover.PortNameB,
                            WorkModes = WorkMode.PortToPort,
                            RemoveAction = removePair
                        };
                        DataList.Add(dataInfo);
                        DB.DataBase.Insert<ComToInfo>(dataInfo.ToComToInfo()).ExecuteAffrows();
                    }
                    LogHelper.Success(string.Join("\r\n", Info));
                }
                else if (TcpMode)
                {
                    bool IsMain = VirtualPorter.CheckIsBusy(MainComName);
                    if (IsMain) { OpenMessageBoxStyleDialog("Error Info", $"{MainComName}已被占用"); return; }
                    string[] Info = Array.Empty<string>();
                    await Task.Run(() =>
                    {
                        Info = VirtualPorter.CreateVirtualSerialPort(MainComName);
                        Info = Info.Select(x => x.Replace("       ", "")).ToArray();
                    });
                    string[] InstallInfo = Info.Where(x => (!x.Contains("ComDB"))&&(!x.Contains("Reboot"))).ToArray();
                    CrossoverPortPair? crossover = InstallInfo.Select(p =>
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
                            }).FirstOrDefault();
                    if (crossover != null)
                    {
                        ICommunication? Communication=null;
                        WorkMode mode = WorkMode.PortToTcpClient;
                        DataInfo dataInfo = new DataInfo()
                        {
                            Index = crossover.PairNumber,
                            PortNameA = crossover.PortNameA,
                            PortNameB = crossover.PortNameB,
                            LocalIP = this.LocalIP,
                            LocalPort = this.LocalPort,
                            RemoteIP = this.RemoteIP,
                            RemotePort = this.RemotePort,
                            RemoveAction = removePair,
                        };
                        await Task.Delay(2000);
                        if (TcpWorkMode == "TcpClient")
                        {
                            Communication = new TcpClientDevice(LocalIP, LocalPort, RemoteIP, RemotePort);
                            Communication.Binding(new VirtualSerialPort(@$"\\.\CNCB{crossover.PairNumber}", MainComName));
                            Communication.StatusChanged += (status) => { dataInfo.Status = status; };
                            _ = Communication.StartAsync();
                        }
                        else
                        {
                            Communication = new TcpServerDevice(LocalIP, LocalPort);
                            Communication.Binding(new VirtualSerialPort(@$"\\.\CNCB{crossover.PairNumber}", MainComName));
                            Communication.StatusChanged += (status) => { dataInfo.Status = status; };
                            _ = Communication.StartAsync();
                            mode = WorkMode.PortToTcpServer;
                        }
                        dataInfo.WorkModes = mode;
                        dataInfo.Communication = Communication;
                        DataList.Add(dataInfo);
                        DB.DataBase.Insert<ComToInfo>(dataInfo.ToComToInfo()).ExecuteAffrows();
                    }
                    LogHelper.Success(string.Join("\r\n", Info));
                }
                await Fresh();
            }
            catch (Exception Ex)
            {
                LogHelper.Error(Ex.Message);
            }
        }

        private void OpenMessageBoxStyleDialog(string Title, string Content)
        {
            DialogHost.Manager.CreateDialog()
                .OfType(NotificationType.Error)
                .WithTitle(Title)
                .WithContent(Content)
                .WithActionButton("关闭", _ => { }, true, "Flat")
                .TryShow();
        }
        /// <summary>
        /// 移除串口对
        /// </summary>
        public async Task removePair(int Index)
        {
            try
            {
                if (VirtualPorter == null) return;
                DataInfo data = DataList.Where(x => x.Index == Index).First();
                if (data.Communication != null) await data.Communication.StopAsync();
                string[] Info = Array.Empty<string>();
                DB.DataBase.Delete<ComToInfo>().Where(x=>x.Index==Index).ExecuteAffrows();
                DataList.Remove(data);
                await Task.Run(() =>
                {
                    Info = VirtualPorter.RemoveVirtualSerialPort(Index);
                });
                LogHelper.Success(string.Join("\r\n", Info));
            }
            catch (Exception Ex)
            {
                LogHelper.Error(Ex.Message);
            }
        }

        /// <summary>
        /// 移除串口对
        /// </summary>
        public IAsyncRelayCommand ListPair { get; set; }
        public async Task listPair()
        {
            try
            {
                await Fresh();
                LogHelper.Success("刷新成功!");
            }
            catch (Exception Ex)
            {
                LogHelper.Error(Ex.Message);
            }
        }

        public async Task Fresh()
        {
            try
            {
                CrossoverPortPair[] Info = Array.Empty<CrossoverPortPair>();
                List<DataInfo> infos = new List<DataInfo>();
                List<ComToInfo> comToInfos = DB.DataBase.Select<ComToInfo>().ToList();
                await Task.Run(() =>
                {
                 
                    if(VirtualPorter != null) Info = VirtualPorter.List();
                    #region 移除缺少对
                    List<ComToInfo> Wait = comToInfos.Where(x => Info.Where(y => y.PairNumber == x.Index).Count() <= 0).ToList();

                    foreach(ComToInfo info in Wait)  DB.DataBase.Delete<ComToInfo>().Where(x=>x.Index==info.Index).ExecuteAffrows();
                    #endregion
                    #region 移除多余的对
                    int[] WaitRemoveList = Info.Where(x => comToInfos.Where(y => y.Index == x.PairNumber).Count() <= 0).Select(x => x.PairNumber).ToArray();
                    foreach (int i in WaitRemoveList)
                    {
                        VirtualPorter?.RemoveVirtualSerialPort(i);
                    }
                    #endregion
                   
                });
                comToInfos = DB.DataBase.Select<ComToInfo>().ToList();
                List<ComToInfo> LastInfos = comToInfos.Where(x => DataList.Where(y => y.Index == x.Index).Count() <= 0).ToList();
                await Task.Run(() =>
                {
                    foreach (ComToInfo info in LastInfos)
                    {
                        DataInfo dataInfo = new DataInfo(info);
                        dataInfo.RemoveAction += removePair;
                        ICommunication? Communication = null;
                        if (dataInfo.WorkModes == WorkMode.PortToPort)
                        {
                            dataInfo.Status = true;
                        }
                        else if (dataInfo.WorkModes == WorkMode.PortToTcpClient)
                        {
                            Communication = new TcpClientDevice(dataInfo.LocalIP??"", dataInfo.LocalPort, dataInfo.RemoteIP ?? "", dataInfo.RemotePort);
                            Communication.Binding(new VirtualSerialPort(@$"\\.\CNCB{dataInfo.Index}", dataInfo.PortNameA ?? ""));
                            Communication.StatusChanged += (status) =>{ dataInfo.Status = status; };
                            _ = Communication.StartAsync();
                        }
                        else if (dataInfo.WorkModes == WorkMode.PortToTcpServer)
                        {
                            Communication = new TcpServerDevice(dataInfo.LocalIP ?? "", dataInfo.LocalPort);
                            Communication.Binding(new VirtualSerialPort(@$"\\.\CNCB{dataInfo.Index}", dataInfo.PortNameA??""));
                            Communication.StatusChanged += (status) => { dataInfo.Status = status; };
                            _ = Communication.StartAsync();
                        }
                        dataInfo.Communication = Communication;
                        infos.Add(dataInfo);
                    }
                });
                foreach (DataInfo info in infos) { DataList.Add(info); }
            }
            catch (Exception Ex)
            {
                LogHelper.Error(Ex.Message);
            }
        }
        public async Task FreshIPList()
        {
            try
            {
                await Task.Run(() =>
                {
                    IPAddress[] iPAddresses = Dns.GetHostAddresses(Dns.GetHostName());
                    List<string> IPAddres = new List<string>();
                    foreach (IPAddress ip in iPAddresses)
                    {
                        IPAddres.Add(ip.ToString());
                    }
                    this.IPList = IPAddres;
                });
            }
            catch (Exception Ex)
            {
                LogHelper.Error(Ex.Message);
            }
        }
    }

    public class LogHelper
    {
        public static void Error(string Info)
        {
            ToastHost.Manager.CreateToast().WithTitle($"Error").WithContent(Info).OfType(NotificationType.Error).Dismiss().After(TimeSpan.FromSeconds(3)).Dismiss().ByClicking().Queue();
        }
        public static void Warning(string Info)
        {
            ToastHost.Manager.CreateToast().WithTitle($"Warn").WithContent(Info).OfType(NotificationType.Warning).Dismiss().After(TimeSpan.FromSeconds(3)).Dismiss().ByClicking().Queue();
        }
        public static void Success(string Info)
        {
            ToastHost.Manager.CreateToast().WithTitle($"Success").WithContent(Info).OfType(NotificationType.Success).Dismiss().After(TimeSpan.FromSeconds(3)).Dismiss().ByClicking().Queue();
        }
        public static void Information(string Info)
        {
            ToastHost.Manager.CreateToast().WithTitle($"Information").WithContent(Info).OfType(NotificationType.Information).Dismiss().After(TimeSpan.FromSeconds(3)).Dismiss().ByClicking().Queue();
        }
    }

    public delegate Task ManagerPort(int Index);

    public partial class DataInfo : ObservableObject
    {
        /// <summary>
        /// 索引
        /// </summary>
        [ObservableProperty]
        private int index;
        /// <summary>
        /// COMA口
        /// </summary>
        [ObservableProperty]
        private string? portNameA;
        /// <summary>
        /// COMB口
        /// </summary>
        [ObservableProperty]
        private string? portNameB;
        /// <summary>
        /// 描述信息
        /// </summary>
        public string? Info
        {
            get
            {
                return WorkModes switch
                {
                    WorkMode.PortToPort => $"{PortNameA}<->{PortNameB}",
                    WorkMode.PortToTcpClient => $"{PortNameA}<->{RemoteIP}:{RemotePort}\r\n本地[{LocalIP}:{LocalPort}]",
                    WorkMode.PortToTcpServer => $"{PortNameA}<->{LocalIP}:{LocalPort}",
                    WorkMode.PortToUdp => $"{PortNameA}<->{RemoteIP}:{RemotePort}",
                    _ => "未知模式"
                };
            }
        }
        /// <summary>
        /// 工作模式
        /// </summary>
        [ObservableProperty]
        private WorkMode workModes;

        /// <summary>
        /// 远程IP
        /// </summary>
        [ObservableProperty]
        private string? remoteIP;

        /// <summary>
        /// 远程Port
        /// </summary>
        [ObservableProperty]
        private int remotePort;

        /// <summary>
        /// 本地IP
        /// </summary>
        [ObservableProperty]
        private string? localIP;

        /// <summary>
        /// 本地Port
        /// </summary>
        [ObservableProperty]
        private int localPort;

        public ManagerPort? RemoveAction;

        public IAsyncRelayCommand RemoveComPair { get; set; }


        private async Task removeAction()
        {
            if (RemoveAction == null) return;
            else await (RemoveAction?.Invoke(Index) ?? Task.CompletedTask);
        }

        /// <summary>
        /// 本地Port
        /// </summary>
        [ObservableProperty]
        private bool status;


        /// <summary>
        /// 通信类
        /// </summary>
        public ICommunication? Communication;

        public DataInfo()
        {
            RemoveComPair = new AsyncRelayCommand(removeAction);
        }

        public DataInfo(ComToInfo info)
        {
            RemoveComPair = new AsyncRelayCommand(removeAction);
            this.Index = info.Index;
            this.PortNameA = info.PortNameA;
            this.PortNameB = info.PortNameB;
            this.WorkModes = info.WorkModes;
            this.RemoteIP = info.RemoteIP;
            this.RemotePort = info.RemotePort;
            this.LocalIP = info.LocalIP;
            this.LocalPort = info.LocalPort;
        }

        public ComToInfo ToComToInfo()
        {
            return new ComToInfo()
            {
                Index = this.Index,
                PortNameA = this.PortNameA,
                PortNameB = this.PortNameB,
                WorkModes = this.WorkModes,
                RemoteIP = this.RemoteIP,
                RemotePort = this.RemotePort,
                LocalIP = this.LocalIP,
                LocalPort = this.LocalPort,
            };
        }
    }
}
