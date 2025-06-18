using Avalonia.Data.Converters;
using SerialConverter.Plugins.VirtualSerial.DBHelper;
using System.Globalization;

namespace SerialConverter.Plugins.VirtualSerial.Converter
{
    public class ModeToString : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            WorkMode? mode = (WorkMode?)value;
            return mode switch 
            { 
                WorkMode.PortToPort => "SerialPort",
                WorkMode.PortToTcpClient =>"TcpClient",
                WorkMode.PortToTcpServer =>"TcpServer",
                _=> "Unknown",
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
