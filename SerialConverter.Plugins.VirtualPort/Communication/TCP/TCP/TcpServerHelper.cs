using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace SerialConverter.Plugins.VirtualSerial.Communication.TCP.TCP
{
    public class TcpServerHelper : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<TcpClient, bool> _connectedClients = new ConcurrentDictionary<TcpClient, bool>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private bool _isRunning;
        public int ConnectedClientCount => _connectedClients.Count;

        public event EventHandler<ConnectionEventArgs>? ClientConnected;

        public event EventHandler<ConnectionEventArgs>? ClientDisconnected;

        public event EventHandler<ReceiveInfo>? DataReceived;

        public TcpServerHelper(string ipAddress, int port)
        {
            _listener = new TcpListener(IPAddress.Parse(ipAddress), port);
        }

        public void Start()
        {
            if (_isRunning) return;
            _listener.Start();
            _isRunning = true;
            _ = AcceptClientsAsync();
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            //关闭所有连接的客户端
            foreach (var client in _connectedClients.Keys)
            {
                try
                {
                    client.Close();
                }
                catch
                {
                    /* 忽略关闭错误 */
                }
            }
            _connectedClients.Clear();
        }

        private async Task AcceptClientsAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _connectedClients.TryAdd(client, true);
                    ClientConnected?.Invoke(this, new ConnectionEventArgs(client));
                    _ = Task.Run(()=> { _ = HandleClientAsync(client); });
                }
                catch (ObjectDisposedException) when (_cancellationTokenSource.IsCancellationRequested)
                {
                    //正常关闭
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"接受客户端连接时发生错误: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                //获取客户端流并处理数据
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[1024];
                    //简单的循环读取，实际应用中可能需要更复杂的协议处理
                    while (client.Connected && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        //客户端断开连接
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        //处理接收到的数据
                        DataReceived?.Invoke(client, new ReceiveInfo("", buffer[0..bytesRead]));
                    }
                }

            }
            catch { }
            finally
            {
                //移除客户端并触发断开事件
                if (_connectedClients.TryRemove(client, out _))
                {
                    try
                    {
                        client.Close();
                        client.Dispose();
                    }
                    catch { /* 忽略清理错误 */ }
                    ClientDisconnected?.Invoke(this, new ConnectionEventArgs(client));
                }
            }
        }

        public void WriteData(byte[] bytes)
        {
            TcpClient[] tcpClients = _connectedClients.Keys.ToArray();
            Parallel.ForEach(tcpClients, (x) => { x.Client.Send(bytes); });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
                _cancellationTokenSource.Dispose();
            }
        }
    }

    public class ConnectionEventArgs : EventArgs
    {
        public TcpClient Client { get; }

        public ConnectionEventArgs(TcpClient client)
        {
            Client = client;
        }
    }
    public class ReceiveInfo : EventArgs
    {
        public string Info { get; set; }

        public byte[] bytes { get; set; }

        public ReceiveInfo(string Info, byte[] bytes)
        {
            this.Info = Info;
            this.bytes = bytes;
        }
    }

}
