using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin;

public partial class OperacionesAdminView : UserControl
{
    public OperacionesAdminView()
    {
        InitializeComponent();
        DataContext = new OperacionesAdminViewModel();
        ConfigurarFormatoFechas();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ConfigurarFormatoFechas()
    {
        var txtFechaDesde = this.FindControl<TextBox>("TxtFechaDesde");
        var txtFechaHasta = this.FindControl<TextBox>("TxtFechaHasta");

        if (txtFechaDesde != null)
            txtFechaDesde.AddHandler(TextInputEvent, FormatearFechaInput, RoutingStrategies.Tunnel);
        if (txtFechaHasta != null)
            txtFechaHasta.AddHandler(TextInputEvent, FormatearFechaInput, RoutingStrategies.Tunnel);
    }

    private void FormatearFechaInput(object? sender, TextInputEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        var textoActual = textBox.Text ?? "";
        var textoNuevo = e.Text ?? "";

        // Solo permitir numeros
        if (!string.IsNullOrEmpty(textoNuevo) && !char.IsDigit(textoNuevo[0]))
        {
            e.Handled = true;
            return;
        }

        // Agregar / automaticamente
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
