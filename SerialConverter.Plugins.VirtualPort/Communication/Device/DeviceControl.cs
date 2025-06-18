using Avalonia.Media;
using System.Runtime.InteropServices;
using System.Text;

namespace SerialConverter.Plugins.VirtualSerial.Communication.Device
{
    public class DeviceControl
    {
        //Windows API 常量定义
        const uint GENERIC_READ = 0x80000000;
        const uint GENERIC_WRITE = 0x40000000;
        const uint OPEN_EXISTING = 3;
        const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        const uint FILE_FLAG_OVERLAPPED = 0x40000000;

        //WaitForSingleObject 相关常量
        const uint INFINITE = 0xFFFFFFFF;
        const uint WAIT_OBJECT_0 = 0;
        const uint WAIT_TIMEOUT = 0x00000102;

        [StructLayout(LayoutKind.Sequential)]
        public struct DCB
        {
            public uint DCBlength;
            public uint BaudRate;
            public uint Flags;
            public ushort wReserved;
            public ushort XonLim;
            public ushort XoffLim;
            public byte ByteSize;
            public byte Parity;
            public byte StopBits;
            public char XonChar;
            public char XoffChar;
            public char ErrorChar;
            public char EofChar;
            public char EvtChar;
            public ushort wReserved1;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COMMTIMEOUTS
        {
            public uint ReadIntervalTimeout;
            public uint ReadTotalTimeoutMultiplier;
            public uint ReadTotalTimeoutConstant;
            public uint WriteTotalTimeoutMultiplier;
            public uint WriteTotalTimeoutConstant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct OVERLAPPED
        {
            public IntPtr Internal;
            public IntPtr InternalHigh;
            public uint Offset;
            public uint OffsetHigh;
            public IntPtr hEvent;
        }

        // 导入Windows API函数
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetCommState(IntPtr hFile, ref DCB lpDCB);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetCommState(IntPtr hFile, ref DCB lpDCB);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetCommTimeouts(IntPtr hFile, ref COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetCommTimeouts(IntPtr hFile, ref COMMTIMEOUTS lpCommTimeouts);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, ref OVERLAPPED lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, ref OVERLAPPED lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetOverlappedResult(IntPtr hFile, ref OVERLAPPED lpOverlapped, out uint lpNumberOfBytesTransferred, bool bWait);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ResetEvent(IntPtr hEvent);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CancelIo(IntPtr hFile);

        public IntPtr hComm;
        public IntPtr readEvent;
        public IntPtr writeEvent;
        Task? ReadTask;
        public bool IsOpen = false;

        CancellationTokenSource? CancellationToken;

        public ReceiveHandle? DataReceived;

        private void ConfigureSerialPort()
        {
            //获取当前串口配置
            DCB dcb = new DCB();
            if (!GetCommState(hComm, ref dcb)) throw new Exception("无法获取串口配置,错误码:" + Marshal.GetLastWin32Error());

            //配置串口参数
            dcb.BaudRate = 9600;
            dcb.Flags = 0;

            if (!SetCommState(hComm, ref dcb)) throw new Exception("无法获取串口配置,错误码:" + Marshal.GetLastWin32Error());

            //设置超时
            COMMTIMEOUTS timeouts = new COMMTIMEOUTS();
            timeouts.ReadIntervalTimeout = 20;
            timeouts.ReadTotalTimeoutConstant = 0;
            timeouts.ReadTotalTimeoutMultiplier = 0;
            timeouts.WriteTotalTimeoutConstant = 0;
            timeouts.WriteTotalTimeoutMultiplier = 0;

            if (!SetCommTimeouts(hComm, ref timeouts)) throw new Exception("无法设置超时,错误码:" + Marshal.GetLastWin32Error());
        }


        public bool OpenDevice(string PortName)
        {
            try
            {
                //打开串口，使用重叠I/O模式
                hComm = CreateFile(PortName, GENERIC_READ | GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, IntPtr.Zero);
                if (hComm == new IntPtr(-1)) throw new Exception("无法打开串口,错误码:" + Marshal.GetLastWin32Error());

                //创建事件对象
                readEvent = CreateEvent(IntPtr.Zero, true, false, null);
                writeEvent = CreateEvent(IntPtr.Zero, true, false, null);

                if (readEvent == IntPtr.Zero || writeEvent == IntPtr.Zero) throw new Exception("创建事件对象失败");

                // 配置串口
                ConfigureSerialPort();

                //启动读取线程
                ReadTask = Task.Run(ReadDataThread);
                return true;
            }
            catch { return false; }
        }

        public void CloseDevice()
        {
            IsOpen = false;
            CancellationToken?.Cancel();
            //清理资源
            if (readEvent != IntPtr.Zero)
            {
                CloseHandle(readEvent);
                readEvent = IntPtr.Zero;
            }

            if (writeEvent != IntPtr.Zero)
            {
                CloseHandle(writeEvent);
                writeEvent = IntPtr.Zero;
            }
            if (hComm != IntPtr.Zero)
            {
                CloseHandle(hComm);
                hComm = IntPtr.Zero;
            }
            if (ReadTask != null) ReadTask=null;
        }

        public void ReadDataThread()
        {
            byte[] buffer = new byte[1024];
            OVERLAPPED overlapped = new OVERLAPPED();
            overlapped.hEvent = readEvent;
            CancellationToken=new CancellationTokenSource();
            try
            {
                while (!CancellationToken.IsCancellationRequested)
                {
                    IsOpen = true;
                    //在开始新的读取操作前,确保事件已重置
                    ResetEvent(readEvent);
                    uint bytesRead = 0;
                    //异步读取数据
                    bool result = ReadFile(hComm, buffer, (uint)buffer.Length, out bytesRead, ref overlapped);
                    //如果立即完成，直接处理结果
                    if (result)
                    {
                        if (bytesRead > 0) DataReceived?.Invoke(buffer[0..(int)bytesRead]);
                        continue;
                    }
                    //检查错误码
                    int errorCode = Marshal.GetLastWin32Error();
                    //如果是I/O挂起，等待操作完成
                    if (errorCode == 997) // ERROR_IO_PENDING
                    {
                        //使用WaitForSingleObject等待事件
                        uint waitResult = WaitForSingleObject(readEvent, INFINITE);
                        if (waitResult == WAIT_OBJECT_0)
                        {
                            //事件已触发,获取操作结果
                            if (GetOverlappedResult(hComm, ref overlapped, out bytesRead, false))
                            {
                                if (bytesRead > 0) DataReceived?.Invoke(buffer[0..(int)bytesRead]);
                            }
                            else
                            {
                                throw new Exception("获取读取结果失败,错误码:" + Marshal.GetLastWin32Error());
                            }
                        }
                        else if (waitResult == WAIT_TIMEOUT)
                        {
                            throw new Exception("读取操作超时");
                        }
                        else
                        {
                            throw new Exception("等待读取事件失败,错误码:" + Marshal.GetLastWin32Error());
                        }
                    }
                    else
                    {
                        throw new Exception("读取数据错误,错误码:" + errorCode);
                    }
                }
            }
            catch
            {
                CloseDevice();
                IsOpen=false;
            }
        }

        public void WriteData(byte[] bytes)
        {
            OVERLAPPED overlapped = new OVERLAPPED();
            overlapped.hEvent = writeEvent;
            
            //在开始新的写入操作前，确保事件已重置
            ResetEvent(writeEvent);
            uint bytesWritten = 0;
            //异步写入数据
            bool result = WriteFile(hComm, bytes, (uint)bytes.Length, out bytesWritten, ref overlapped);
            //如果立即完成，直接处理结果
            if (result) return;
            //检查错误码
            int errorCode = Marshal.GetLastWin32Error();
            //如果是I/O挂起，等待操作完成
            if (errorCode == 997) // ERROR_IO_PENDING
            {
                // 使用WaitForSingleObject等待事件
                uint waitResult = WaitForSingleObject(writeEvent, INFINITE);
                if (waitResult == WAIT_OBJECT_0)
                {
                    // 事件已触发，获取操作结果
                    if (GetOverlappedResult(hComm, ref overlapped, out bytesWritten, false)) { }
                    else
                    {
                        throw new Exception("获取写入结果失败,错误码:" + Marshal.GetLastWin32Error());
                    }
                }
                else if (waitResult == WAIT_TIMEOUT)
                {
                    throw new Exception("写入操作超时");
                }
                else
                {
                    throw new Exception("等待写入事件失败,错误码:" + Marshal.GetLastWin32Error());
                }
            }
            else
            {
                throw new Exception("写入数据错误,错误码:" + errorCode);
            }
        }
    }

    
}
