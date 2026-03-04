using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Allva.Desktop.Converters;

// ===================================
// CONVERTERS GENERALES PARA ALLVA DESKTOP
// LOS CONVERTERS DE ESTADO ESTÁN EN EstadoComercioConverters.cs
// ===================================

/// <summary>
/// Convierte un valor booleano a un color
/// true = Verde (#4caf50), false = Rojo (#d32f2f)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return Brush.Parse(boolValue ? "#4caf50" : "#d32f2f");
        }
        return Brush.Parse("#d32f2f");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un valor booleano a un color de fondo
/// true = Azul claro (#e3f2fd), false = Transparente
/// Útil para resaltar items seleccionados
/// </summary>
public class BoolToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return Brush.Parse(boolValue ? "#e3f2fd" : "Transparent");
        }
        return Brush.Parse("Transparent");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte una cadena NO vacía a true (para IsVisible)
/// </summary>
public class NotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un número mayor que cero a true (para IsVisible)
/// </summary>
public class GreaterThanZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return false;
        
        return value switch
        {
            int intValue => intValue > 0,
            long longValue => longValue > 0,
            decimal decimalValue => decimalValue > 0,
            double doubleValue => doubleValue > 0,
            _ => false
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un número igual a cero a true (para IsVisible)
/// </summary>
public class EqualToZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return true;
        
        return value switch
        {
            int intValue => intValue == 0,
            long longValue => longValue == 0,
            decimal decimalValue => decimalValue == 0,
            double doubleValue => doubleValue == 0,
            _ => true
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Invierte un valor booleano
/// true → false, false → true
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// Convierte null a true (para IsVisible)
/// null → true, no-null → false
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte not-null a true (para IsVisible)
/// null → false, no-null → true
/// </summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un booleano a icono de expandir/contraer
/// true = ▼ (expandido), false = ▶ (contraído)
/// </summary>
public class BoolToExpandIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool mostrarDetalles)
        {
            return mostrarDetalles ? "▼" : "▶";
        }

        return "▶";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un booleano a icono de colapsar (version invertida)
/// true = ▲ (expandido/arriba), false = ▼ (colapsado/abajo)
/// </summary>
public class BoolToCollapseIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "▲" : "▼";
        }

        return "▼";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un string nullable a string vacío si es null
/// Útil para mostrar valores opcionales sin mostrar "null"
/// </summary>
public class NullToEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Niega un valor numérico (útil para posicionamiento inverso en Canvas)
/// </summary>
public class NegateConverter : IValueConverter
{
    public static readonly NegateConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return -d;
        if (value is int i)
            return -i;
        if (value is float f)
            return -f;
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return -d;
        if (value is int i)
            return -i;
        if (value is float f)
            return -f;
        return 0;
    }
}

/// <summary>
/// Convierte el ancho a altura para un grid de 3 columnas x 2 filas con celdas cuadradas
/// Altura = Ancho * (2/3) = dos filas de celdas cuadradas
/// </summary>
public class WidthToGridHeightConverter : IValueConverter
{
    public static readonly WidthToGridHeightConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double width && width > 0)
        {
            // Para 3 columnas y 2 filas de celdas cuadradas:
            // Cada celda tiene ancho = width/3
            // Altura de cada celda = width/3 (cuadrada)
            // Altura total = 2 * (width/3) = width * 2/3
            return width * 2.0 / 3.0;
        }
        return 400.0; // Fallback
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte el ancho del contenedor a altura para un grid 3x2 con celdas cuadradas
/// pero limitado al 80% del viewport para que siempre quepan las 6 noticias
/// Recibe: ancho del contenedor, alto del viewport y si hay noticia expandida (via MultiBinding)
/// </summary>
public class NewsGridHeightConverter : IMultiValueConverter
{
    public static readonly NewsGridHeightConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 &&
            values[0] is double containerWidth && containerWidth > 0 &&
            values[1] is double viewportHeight && viewportHeight > 0)
        {
            // Altura ideal para celdas cuadradas: 2 filas * (ancho/3 columnas)
            double idealHeight = containerWidth * 2.0 / 3.0;

            // Altura máxima disponible (viewport - título - márgenes - espacio para "Más Noticias")
            double maxHeight = viewportHeight - 120;

            // El grid siempre mantiene el mismo tamaño (el panel expandido está FUERA)
            return Math.Min(idealHeight, maxHeight);
        }
        return 350.0; // Fallback
    }
}

/// <summary>
/// Convierte un booleano a MaxHeight para animación de expansión suave
/// true = 500 (expandido), false = 0 (colapsado)
/// </summary>
public class BoolToMaxHeightConverter : IValueConverter
{
    public static readonly BoolToMaxHeightConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? 500.0 : 0.0;
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Calcula el ancho de cada tarjeta de noticia para que quepan 3 por fila
/// Recibe el ancho del contenedor y devuelve (ancho - márgenes) / 3
/// </summary>
public class NewsCardWidthConverter : IValueConverter
{
    public static readonly NewsCardWidthConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double containerWidth && containerWidth > 0)
        {
            // 3 tarjetas por fila, restando márgenes extra para asegurar que quepan
            // Cada tarjeta tiene Margin="4" (8px horizontal total por tarjeta)
            // Restamos 30px para dar margen de seguridad
            double cardWidth = (containerWidth - 30) / 3.0;
            // Mínimo 100px, máximo 360px para tarjetas más grandes y cuadradas
            return Math.Min(360, Math.Max(100, cardWidth));
        }
        return 150.0; // Fallback
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un booleano a CornerRadius para tarjetas con panel expandible
/// true (expandida) = solo esquinas superiores redondeadas (8,8,0,0)
/// false (colapsada) = todas las esquinas redondeadas (8)
/// </summary>
public class BoolToCardCornerRadiusConverter : IValueConverter
{
    public static readonly BoolToCardCornerRadiusConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded
                ? new Avalonia.CornerRadius(8, 8, 0, 0)
                : new Avalonia.CornerRadius(8);
        }
        return new Avalonia.CornerRadius(8);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Convierte un booleano a color para indicadores de carrusel (dots)
/// true = Blanco (slide activo), false = Gris semi-transparente (slide inactivo)
/// </summary>
public class BoolToDotColorConverter : IValueConverter
{
    public static readonly BoolToDotColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return Brush.Parse(isActive ? "#FFFFFF" : "#80FFFFFF");
        }
        return Brush.Parse("#80FFFFFF");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}