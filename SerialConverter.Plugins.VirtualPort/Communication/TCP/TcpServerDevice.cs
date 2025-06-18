using SerialConverter.Plugins.VirtualSerial.Communication.Interface;
using SerialConverter.Plugins.VirtualSerial.Communication.TCP.TCP;
using System.Net;

namespace SerialConverter.Plugins.VirtualSerial.Communication.TCP
{
    public class TcpServerDevice : ICommunication
    {
        TcpServerHelper TcpServers;
        public string LocalIP;
        public int LocalPort;
        public TcpServerDevice(string IP,int Port)
        {
            TcpServers = new TcpServerHelper(IP,Port);
            this.LocalIP = IP;
            this.LocalPort = Port;
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
                try
                {
                    TcpServers.Start();
                }
                catch
                {
                    StatusChanged?.Invoke(false);
                }
                return await Task.FromResult(true);
            }
            catch { return await Task.FromResult(false); }
        }
        private CancellationTokenSource Cancellation = new CancellationTokenSource();
        public async Task StartAsync()
        {
            
            if (Status == RunStatus.Running) return;
            await OpenDevice();
            Cancellation = new CancellationTokenSource();
            Status = RunStatus.Running;
            StatusChanged?.Invoke(true);
            TcpServers.DataReceived += (sender, buffer) => 
            {
                Communication?.WriteData(buffer.bytes);
                IDataMonitor?.Invoke($"{Description}->{Communication?.Description}", buffer.bytes);
            };
        }
        public async Task StopAsync()
        {
            if (Status == RunStatus.Stopping) return;
            Status = RunStatus.Stopping;
            StatusChanged?.Invoke(false);
            Cancellation.Cancel();
            if (Communication != null)
            {
                this.Communication.Communication = null;
                await Communication.StopAsync();
            }
            TcpServers.Stop();
            await Task.CompletedTask;
        }

        public async Task WriteData(byte[] bytes)
        {
            TcpServers.WriteData(bytes);
            await Task.CompletedTask;
        }
    }
}
