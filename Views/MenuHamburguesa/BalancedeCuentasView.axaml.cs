using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Allva.Desktop.ViewModels;
using System.Linq;

namespace Allva.Desktop.Views.MenuHamburguesa;

public partial class BalancedeCuentasView : UserControl
{
    private int _idComercio;
    private int _idLocal;
    private string _codigoLocal = "";
    private int _idUsuario;
    private string _nombreUsuario = "";

    public BalancedeCuentasView()
    {
        InitializeComponent();
        DataContext = new BalancedeCuentasViewModel();
        ConfigurarFormatoFechas();
    }

    public BalancedeCuentasView(int idComercio, int idLocal, string codigoLocal)
    {
        InitializeComponent();
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        var vm = new BalancedeCuentasViewModel(idComercio, idLocal, codigoLocal);
        vm.OnVolverAInicio += VolverADashboard;
        DataContext = vm;
        ConfigurarFormatoFechas();
    }

    public BalancedeCuentasView(int idComercio, int idLocal, string codigoLocal, int idUsuario, string nombreUsuario)
    {
        InitializeComponent();
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        _idUsuario = idUsuario;
        _nombreUsuario = nombreUsuario;
        var vm = new BalancedeCuentasViewModel(idComercio, idLocal, codigoLocal, idUsuario, nombreUsuario);
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
