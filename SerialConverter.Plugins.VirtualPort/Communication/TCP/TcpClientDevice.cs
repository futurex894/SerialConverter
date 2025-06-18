using SerialConverter.Plugins.VirtualSerial.Communication.Interface;
using SerialConverter.Plugins.VirtualSerial.Communication.TCP.TCP;
using System.Net;

namespace SerialConverter.Plugins.VirtualSerial.Communication.TCP
{
    public class TcpClientDevice : ICommunication
    {
        ReconnectableTcpClient TcpClients;
        public string RemoteIP;
        public int RemotePort;

        public TcpClientDevice(string IP,int Port,string RemoteIP,int RemotePort)
        {
            TcpClients = new ReconnectableTcpClient(RemoteIP,RemotePort);
            IPEndPoint localIP = new IPEndPoint(IPAddress.Parse(IP), Port);
            TcpClients.Bind(localIP);
            TcpClients.ConfigureReconnection();
            this.RemoteIP = RemoteIP;
            this.RemotePort = RemotePort;
        }

        public RunStatus Status { get; set; } = RunStatus.Stopping;
        public ICommunication? Communication { get; set; }
        public string? Description { get; set; }
        public DataMointor? IDataMonitor { get; set; }
        public StatusChange? StatusChanged { get; set; }

        public void Binding(ICommunication Communication)
        {
            this.Communication = Communication;
            Communication.Communication = this;
            Communication.StartAsync();
        }
        public async Task<bool> OpenDevice()
        {
            try
            {
                await TcpClients.ConnectAsync();
                return true;
            }
            catch { return false; }
        }
        private CancellationTokenSource Cancellation = new CancellationTokenSource();
        public async Task StartAsync()
        {
            _ = Task.Run(() =>
            {
                if (Status == RunStatus.Running) return;
                TcpClients.DataReceived += (sender, buffer) =>
                {
                    Communication?.WriteData(buffer);
                    IDataMonitor?.Invoke($"{Description}->{Communication?.Description}", buffer);
                };
                TcpClients.ConnectionStatusChanged += (sender, status) =>
                {
                    StatusChanged?.Invoke(status);
                };
                _ = OpenDevice();
                Cancellation = new CancellationTokenSource();
                Status = RunStatus.Running;
            });
            await Task.CompletedTask;
        }
        public async Task StopAsync()
        {
            if (Status == RunStatus.Stopping) return;
            Status = RunStatus.Stopping;
            Cancellation.Cancel();
            if (Communication != null)
            {
                this.Communication.Communication = null;
                await Communication.StopAsync();
            }
            await TcpClients.DisconnectAsync();
        }

        public async Task WriteData(byte[] bytes)
        {
            await TcpClients.SendAsync(bytes);
        }
    }
}
