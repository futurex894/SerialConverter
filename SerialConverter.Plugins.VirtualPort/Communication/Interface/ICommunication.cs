namespace SerialConverter.Plugins.VirtualSerial.Communication.Interface
{
    public interface ICommunication
    {
        public RunStatus Status { get; }
        public ICommunication? Communication { get; set; }
        public string? Description {  get; set; }
        public Task WriteData(byte[] bytes);
        public void Binding(ICommunication Communication);
        public Task<bool> OpenDevice();
        public Task StartAsync();
        public Task StopAsync();
        public DataMointor? IDataMonitor { get; set; }
        public StatusChange? StatusChanged {  get; set; }

    }

    public delegate void DataMointor(string info,byte[] bytes);
    public delegate void StatusChange(bool Connected);
    public enum RunStatus
    {
        Stopping = 0,
        Running = 1,
        Other = 2
    }
}
