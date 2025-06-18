using Avalonia.Data.Converters;
using System.Globalization;

namespace SerialConverter.Plugins.VirtualSerial.Converter
{
    public class BoolToColor : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool? result = (bool?)value;
            return result switch
            {
                true => "Green",
                false =>"Red",
                _=>"Red"
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
