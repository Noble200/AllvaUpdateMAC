using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Allva.Desktop.ViewModels;
using System.Linq;

namespace Allva.Desktop.Views.MenuHamburguesa;

public partial class OperacionesView : UserControl
{
    public OperacionesView()
    {
        InitializeComponent();
        DataContext = new OperacionesViewModel();
        ConfigurarFormatoFechas();
    }

    public OperacionesView(int idComercio, int idLocal, string codigoLocal)
    {
        InitializeComponent();
        DataContext = new OperacionesViewModel(idComercio, idLocal, codigoLocal);
        ConfigurarFormatoFechas();
    }

    public OperacionesView(int idComercio, int idLocal, string codigoLocal, int idUsuario, string nombreUsuario)
    {
        InitializeComponent();
        var vm = new OperacionesViewModel(idComercio, idLocal, codigoLocal, idUsuario, nombreUsuario);
        vm.OnVolverAInicio += VolverADashboard;
        DataContext = vm;
        ConfigurarFormatoFechas();
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

    private void OnEstadoClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is OperacionPackAlimentoItem operacion)
        {
            if (operacion.EsPendiente && DataContext is OperacionesViewModel vm)
            {
                vm.AnularOperacionCommand.Execute(operacion);
            }
        }
    }

    private void VolverADashboard()
    {
        var mainDashboard = this.GetVisualAncestors()
            .OfType<MainDashboardView>()
            .FirstOrDefault();

        if (mainDashboard != null)
        {
            mainDashboard.IrAUltimasNoticias();
        }
    }
}