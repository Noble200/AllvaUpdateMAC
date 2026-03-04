using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Allva.Desktop.Converters
{
    public class BoolToApiResultColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isSuccess)
            {
                return isSuccess 
                    ? new SolidColorBrush(Color.Parse("#E8F5E9"))
                    : new SolidColorBrush(Color.Parse("#FFEBEE"));
            }
            return new SolidColorBrush(Color.Parse("#F5F5F5"));
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}