using SerialConverter.Plugins.VirtualSerial.Communication.Device;
using SerialConverter.Plugins.VirtualSerial.Communication.Interface;

namespace SerialConverter.Plugins.VirtualSerial.Communication.SerialPort
{
    public class VirtualSerialPort : ICommunication
    {
        private DeviceControl DeviceControls { get; set; } = new DeviceControl();
        public ICommunication? Communication { get; set; }
        private string PortName { get; set; }
        public DataMointor? IDataMonitor { get; set; }
        public RunStatus Status { get; set; }= RunStatus.Stopping;
        public string? Description { get; set; }
        public StatusChange? StatusChanged { get; set ; }

        private Object SendLock = new Object();

        public VirtualSerialPort(string PortName,string Description) 
        {
            this.PortName = PortName;
            this.Description = Description;
            Status=RunStatus.Stopping;
        }
        public Task<bool> OpenDevice()
        {
            return Task.FromResult(DeviceControls.OpenDevice(PortName));
        }
        public void Binding(ICommunication Communication)
        {
            this.Communication = Communication;
            Communication.Communication = this;
            Communication.StartAsync();
        }
        private CancellationTokenSource Cancellation=new CancellationTokenSource();
        public async Task StartAsync()
        {
            if (Status == RunStatus.Running) return;
            await OpenDevice();
            Cancellation = new CancellationTokenSource();
            Status = RunStatus.Running;
            StatusChanged?.Invoke(true);
            DeviceControls.DataReceived += (bytes) =>
            {
                Communication?.WriteData(bytes);
                IDataMonitor?.Invoke($"{Description}->{Communication?.Description}", bytes);
            };
        }
        public Task StopAsync()
        {
            if (Status == RunStatus.Stopping) return Task.CompletedTask;
            Status = RunStatus.Stopping;
            StatusChanged?.Invoke(false);
            Cancellation.Cancel();
            if (Communication != null)
            {
                this.Communication.Communication = null;
                Communication.StopAsync();
            }
            DeviceControls.CloseDevice();
            return Task.CompletedTask;
        }
        public Task WriteData(byte[] bytes)
        {
            lock (SendLock) DeviceControls.WriteData(bytes);
            IDataMonitor?.Invoke($"{Description}<-{Communication?.Description}", bytes);
            return Task.CompletedTask;
        }
    }
}
