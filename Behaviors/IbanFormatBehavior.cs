using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;

namespace Allva.Desktop.Behaviors;

/// <summary>
/// Behavior que formatea automaticamente un IBAN mientras se escribe.
/// Formato espanol: ES00 0000 0000 00 0000000000
/// - ES: Codigo pais (2 letras)
/// - 00: Digitos de control del pais (2 digitos)
/// - 0000: Codigo del banco (4 digitos)
/// - 0000: Codigo de sucursal (4 digitos)
/// - 00: Digitos de control (2 digitos)
/// - 0000000000: Numero de cuenta del cliente (10 digitos)
/// </summary>
public class IbanFormatBehavior : Behavior<TextBox>
{
    private bool _isUpdating;

    // Posiciones donde van los espacios: despues de pos 4, 8, 12, 14
    // ES00 0000 0000 00 0000000000
    private static readonly int[] SpacePositions = { 4, 8, 12, 14 };

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.TextChanged += OnTextChanged;
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (AssociatedObject != null)
        {
            AssociatedObject.TextChanged -= OnTextChanged;
        }
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isUpdating || AssociatedObject == null) return;

        _isUpdating = true;
        try
        {
            var text = AssociatedObject.Text ?? "";

            // Remover espacios existentes y caracteres no validos
            var cleanText = new string(text.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpper();

            // Limitar a 24 caracteres (IBAN espanol sin espacios)
            if (cleanText.Length > 24)
            {
                cleanText = cleanText.Substring(0, 24);
            }

            // Formatear con el patron correcto
            var formatted = FormatIbanEspanol(cleanText);

            if (formatted != text)
            {
                // Calcular nueva posicion del cursor
                var oldCaretIndex = AssociatedObject.CaretIndex;
                var spacesBeforeCaret = text.Take(oldCaretIndex).Count(c => c == ' ');
                var newCaretIndex = oldCaretIndex - spacesBeforeCaret;

                // Contar espacios en el nuevo texto hasta la posicion del cursor
                var newText = formatted;
                var actualPosition = 0;
                var charCount = 0;

                for (int i = 0; i < newText.Length && charCount < newCaretIndex; i++)
                {
                    if (newText[i] != ' ')
                    {
                        charCount++;
                    }
                    actualPosition = i + 1;
                }

                AssociatedObject.Text = formatted;
                AssociatedObject.CaretIndex = Math.Min(actualPosition, formatted.Length);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    /// <summary>
    /// Formatea IBAN espanol: ES00 0000 0000 00 0000000000
    /// </summary>
    private static string FormatIbanEspanol(string cleanIban)
    {
        if (string.IsNullOrEmpty(cleanIban)) return "";

        var result = "";
        for (int i = 0; i < cleanIban.Length; i++)
        {
            // Agregar espacio en las posiciones correctas
            if (SpacePositions.Contains(i))
            {
                result += " ";
            }
            result += cleanIban[i];
        }
        return result;
    }
}
