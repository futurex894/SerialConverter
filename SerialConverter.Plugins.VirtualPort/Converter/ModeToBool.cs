using Avalonia.Data.Converters;
using System.Globalization;

namespace SerialConverter.Plugins.VirtualSerial.Converter
{
    public class ModeToBool : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string? valueStr = value as string; 
            return valueStr switch 
            {
                "TcpClient"=>true,
                "TcpServer"=>false,
                _=>false,
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
