using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SerialConverter.Plugins.VirtualSerial.Communication.TCP.TCP
{
    public class ReconnectableTcpClient : IDisposable
    {
        //连接状态事件
        public event EventHandler<bool>? ConnectionStatusChanged;
        public event EventHandler<Exception>? ErrorOccurred;
        public event EventHandler<byte[]>? DataReceived;

        //配置参数
        private string? _serverIp;
        private int _serverPort;
        //默认5秒重连间隔
        private int _reconnectInterval = 3000;
        //默认无限次重连
        private int _maxReconnectAttempts = -1;
        private bool _autoReconnect = true;
        private int _bufferSize = 2048;

        private EndPoint? _endPoint;

        //连接相关对象
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private bool _isConnected;
        private bool _isDisposed;
        private int _reconnectAttempts;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _lockObject = new object();

        public bool IsConnected => _isConnected;
        public int ReconnectAttempts => _reconnectAttempts;

        public ReconnectableTcpClient(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
            _tcpClient = new TcpClient();
        }

        public void Bind(EndPoint endPoint)
        {
            _endPoint = endPoint;
            _tcpClient?.Client.Bind(endPoint);
        }

        //配置重连参数
        public void ConfigureReconnection(int reconnectInterval = 3000, int maxReconnectAttempts = -1, bool autoReconnect = true)
        {
            _reconnectInterval = reconnectInterval;
            _maxReconnectAttempts = maxReconnectAttempts;
            _autoReconnect = autoReconnect;
        }

        //配置缓冲区大小
        public void ConfigureBuffer(int bufferSize = 4096)
        {
            _bufferSize = bufferSize;
        }

        //异步连接到服务器
        public async Task ConnectAsync()
        {
            if (_isConnected) return;
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _reconnectAttempts = 0;

                await ConnectInternalAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                await HandleDisconnectAsync();
            }
        }

        //内部连接方法
        private async Task ConnectInternalAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && (_maxReconnectAttempts < 0 || _reconnectAttempts < _maxReconnectAttempts))
            {
                try
                {
                    if (_tcpClient == null || _tcpClient.Connected == false)
                    {
                        if (_tcpClient != null) { _tcpClient.Dispose(); }
                        _tcpClient = new TcpClient();
                        if (_endPoint != null) _tcpClient.Client.Bind(new IPEndPoint(((IPEndPoint)_endPoint).Address, ((IPEndPoint)_endPoint).Port));
                        await _tcpClient.ConnectAsync(_serverIp??"", _serverPort);
                        _stream = _tcpClient.GetStream();
                        SetConnected(true);
                        _reconnectAttempts = 0;
                        // 启动数据接收
                        _ = StartReceivingAsync(cancellationToken);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _reconnectAttempts++;
                    ErrorOccurred?.Invoke(this, new Exception($"连接尝试 {_reconnectAttempts} 失败: {ex.Message}", ex));
                    if (_maxReconnectAttempts > 0 && _reconnectAttempts >= _maxReconnectAttempts)
                    {
                        ErrorOccurred?.Invoke(this, new Exception("已达到最大重连尝试次数")); break;
                    }
                    await Task.Delay(_reconnectInterval, cancellationToken);
                }
            }
        }
        //启动接收数据
        private async Task StartReceivingAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[_bufferSize];
            try
            {
                while (!cancellationToken.IsCancellationRequested && _isConnected)
                {
                    if (_stream == null || (_tcpClient != null && !_tcpClient.Connected))
                    {
                        await HandleDisconnectAsync(); break;
                    }
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0)
                    {
                        //连接已关闭
                        await HandleDisconnectAsync();
                        break;
                    }
                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, data, bytesRead);
                    DataReceived?.Invoke(this, data);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                ErrorOccurred?.Invoke(this, ex);
                await HandleDisconnectAsync();
            }
        }
        //发送数据
        public async Task SendAsync(byte[] data)
        {
            if (!_isConnected || _stream == null) throw new InvalidOperationException("未连接到服务器");
            try
            {
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                await HandleDisconnectAsync();
                throw;
            }
        }

        //发送字符串数据
        public async Task SendStringAsync(string message, Encoding? encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            await SendAsync(encoding.GetBytes(message));
        }

        //处理断开连接
        private async Task HandleDisconnectAsync()
        {
            if (!_isConnected) return;
            SetConnected(false);
            _tcpClient?.Close();
            if (_autoReconnect && (_maxReconnectAttempts < 0 || _reconnectAttempts < _maxReconnectAttempts))
            {
                await ConnectInternalAsync(_cancellationTokenSource?.Token ?? CancellationToken.None);
            }
        }

        //设置连接状态
        private void SetConnected(bool connected)
        {
            lock (_lockObject)
            {
                if (_isConnected != connected)
                {
                    _isConnected = connected;
                    ConnectionStatusChanged?.Invoke(this, connected);
                }
            }
        }

        //断开连接
        public async Task DisconnectAsync()
        {
            _autoReconnect = false;
            _cancellationTokenSource?.Cancel();
            await CloseConnectionAsync();
        }

        //关闭连接
        private async Task CloseConnectionAsync()
        {
            try
            {
                _stream?.Close();
                _stream = null;
                _tcpClient?.Close();
                _tcpClient = null;
                SetConnected(false);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }

        //实现IDisposable接口
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _autoReconnect = false;
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                    _ = CloseConnectionAsync();
                }
                _isDisposed = true;
            }
        }
    }

}
