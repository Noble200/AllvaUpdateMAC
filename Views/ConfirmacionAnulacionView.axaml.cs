using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Allva.Desktop.Views;

public partial class ConfirmacionAnulacionView : Window
{
    public bool Confirmado { get; private set; } = false;

    public string NumeroOperacion { get; set; } = "";
    public string FechaHora { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string NombreBeneficiario { get; set; } = "";
    public string PaisDestino { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Importe { get; set; } = "";

    public ConfirmacionAnulacionView()
    {
        InitializeComponent();
        DataContext = this;
    }

    public ConfirmacionAnulacionView(string numeroOperacion, string fechaHora, string nombreCliente,
        string nombreBeneficiario, string paisDestino, string descripcion, string importe)
    {
        NumeroOperacion = numeroOperacion;
        FechaHora = fechaHora;
        NombreCliente = nombreCliente;
        NombreBeneficiario = nombreBeneficiario;
        PaisDestino = paisDestino;
        Descripcion = descripcion;
        Importe = importe;

        InitializeComponent();
        DataContext = this;
    }

    private void Confirmar_Click(object? sender, RoutedEventArgs e)
    {
        Confirmado = true;
        Close();
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        Confirmado = false;
        Close();
    }
}
