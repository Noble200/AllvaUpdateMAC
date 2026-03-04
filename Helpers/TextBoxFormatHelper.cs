using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Allva.Desktop.Helpers;

/// <summary>
/// Helper para formateo automático de TextBox (fechas y teléfonos)
/// </summary>
public static class TextBoxFormatHelper
{
    /// <summary>
    /// Configura un TextBox para formatear teléfonos automáticamente con puntos cada 3 dígitos
    /// Ejemplo: 123.456.789
    /// </summary>
    public static void ConfigurarFormatoTelefono(TextBox? textBox)
    {
        if (textBox == null) return;
        textBox.AddHandler(InputElement.TextInputEvent, FormatearTelefonoInput, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Configura un TextBox para formatear fechas automáticamente con barras
    /// Ejemplo: 01/12/2024
    /// </summary>
    public static void ConfigurarFormatoFecha(TextBox? textBox)
    {
        if (textBox == null) return;
        textBox.AddHandler(InputElement.TextInputEvent, FormatearFechaInput, RoutingStrategies.Tunnel);
    }

    private static void FormatearTelefonoInput(object? sender, TextInputEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        var textoActual = textBox.Text ?? "";
        var textoNuevo = e.Text ?? "";

        // Solo permitir números
        if (!string.IsNullOrEmpty(textoNuevo) && !char.IsDigit(textoNuevo[0]))
        {
            e.Handled = true;
            return;
        }

        // Contar solo dígitos actuales
        var soloDigitos = textoActual.Replace(".", "");
        var longitudDigitos = soloDigitos.Length;

        // Agregar punto automáticamente cada 3 dígitos
        // Posiciones: después del 3er dígito (pos 3) y después del 6to (pos 6)
        if (longitudDigitos == 3 || longitudDigitos == 6)
        {
            var posicion = textBox.CaretIndex;
            if (posicion == textoActual.Length && !textoActual.EndsWith("."))
            {
                textBox.Text = textoActual + ".";
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }

    private static void FormatearFechaInput(object? sender, TextInputEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        var textoActual = textBox.Text ?? "";
        var textoNuevo = e.Text ?? "";

        // Solo permitir números
        if (!string.IsNullOrEmpty(textoNuevo) && !char.IsDigit(textoNuevo[0]))
        {
            e.Handled = true;
            return;
        }

        // Agregar / automáticamente
        var posicion = textBox.CaretIndex;
        var longitudActual = textoActual.Replace("/", "").Length;

        if (longitudActual == 2 || longitudActual == 4)
        {
            if (posicion == textoActual.Length && !textoActual.EndsWith("/"))
            {
                textBox.Text = textoActual + "/";
                textBox.CaretIndex = textBox.Text.Length;
            }
        }
    }
}
