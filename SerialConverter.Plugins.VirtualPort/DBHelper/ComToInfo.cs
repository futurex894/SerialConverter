using CommunityToolkit.Mvvm.ComponentModel;

namespace SerialConverter.Plugins.VirtualSerial.DBHelper
{
    public partial class ComToInfo : ObservableObject
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
    }

    public enum WorkMode
    {
        PortToPort,
        PortToTcpClient,
        PortToTcpServer,
        PortToUdp
    }
}
