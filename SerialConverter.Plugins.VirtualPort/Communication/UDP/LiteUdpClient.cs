namespace SerialConverter.Plugins.VirtualSerial.Communication.UDP
{
    public class UdpMessageQueue
    {
        //private readonly UdpClient _udpClient;
        //private CancellationTokenSource? CancellationToken;
        //private Object SendLock = new Object();
        //public IPEndPoint? UdpEndPoint;
        //public ReceiveHandle? DataReceived;

        //public bool IsStarted = false;

        //public void LockSend(byte[] buffer)
        //{
        //    lock (SendLock)
        //    {
        //        if (UdpEndPoint != null)
        //        {
        //            _udpClient.Send(buffer, UdpEndPoint);
        //        }
        //    }
        //}
        //public UdpMessageQueue(int port)
        //{
        //    _udpClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
        //}
        //public async Task StartListeningAsync(CancellationToken cancellationToken)
        //{
        //    IsStarted = true;
        //    _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        //    await Task.Run(() => ListenForMessages(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        //}

        //private async Task ListenForMessages(CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        while (!cancellationToken.IsCancellationRequested)
        //        {
        //            try
        //            {
        //                var receivedResults = await _udpClient.ReceiveAsync();
        //                UdpEndPoint = receivedResults.RemoteEndPoint;
        //                HardwareControl.RemoteEndPoint = receivedResults.RemoteEndPoint;
        //                // 将接收到的数据添加到队列中  
        //                _messageQueue.Enqueue(receivedResults.Buffer);
        //                // 在这里可以添加逻辑来处理接收到的数据（如果不需要排队）  

        //                // 例如，简单打印接收到的数据  
        //                //Console.WriteLine("Received message: " + BitConverter.ToString(receivedResults.Buffer));
        //                _ = ProcessMessageQueueAsync(cancellationToken);
        //            }
        //            catch (Exception ex)
        //            {
        //                LogHelper.ErrorLog("[Udp服务]" + ex.Message);
        //            }
        //        }
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        IsStarted = false;
        //        // 监听被取消，正常退出  
        //    }
        //    catch (Exception ex)
        //    {
        //        IsStarted = false;
        //        // 处理其他异常  
        //        LogHelper.ErrorLog("[Udp服务]" + ex.Message);
        //    }
        //}

        //// 示例方法：从队列中获取并处理消息  
        //public async Task ProcessMessageQueueAsync(CancellationToken cancellationToken)
        //{
        //    while (!cancellationToken.IsCancellationRequested && !_messageQueue.IsEmpty)
        //    {
        //        if (_messageQueue.TryDequeue(out byte[]? message))
        //        {
        //            if (message is not null && UdpEndPoint is not null)
        //            {
        //                Recived?.Invoke(UdpEndPoint, message);
        //            }
        //        }
        //    }
        //    await Task.CompletedTask;
        //}
        //public void StopListening()
        //{
        //    _cancellationTokenSource?.Cancel();
        //    _udpClient.Close();
        //    IsStarted = false;
        //}
    }
}
