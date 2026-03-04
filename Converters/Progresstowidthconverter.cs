using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Allva.Desktop.Converters
{
    public class ProgressToWidthConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int progreso)
            {
                // Convertir porcentaje (0-100) a ancho (0-400)
                return (progreso / 100.0) * 400;
            }
            return 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}