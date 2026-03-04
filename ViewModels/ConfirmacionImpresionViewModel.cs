using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.ViewModels;

public partial class ConfirmacionImpresionViewModel : ObservableObject
{
    [ObservableProperty]
    private string fechaGeneracion = "";
    
    [ObservableProperty]
    private string horaGeneracion = "";
    
    [ObservableProperty]
    private string nombreUsuario = "";
    
    [ObservableProperty]
    private string codigoLocal = "";
    
    [ObservableProperty]
    private string balanceEuros = "0.00";
    
    [ObservableProperty]
    private bool balanceEurosEsPositivo = true;
    
    [ObservableProperty]
    private string totalDivisas = "0.00";
    
    [ObservableProperty]
    private string cantidadOperaciones = "0 registros";
    
    [ObservableProperty]
    private bool sinFiltros = true;
    
    [ObservableProperty]
    private bool tieneDivisas = false;
    
    [ObservableProperty]
    private string desgloseDivisasTexto = "";
    
    public ObservableCollection<FiltroAplicadoItem> FiltrosAplicados { get; } = new();
    
    // Color de fondo para T.Euros (verde claro si positivo, rojo claro si negativo)
    public string ColorFondoEuros => BalanceEurosEsPositivo ? "#e8f5e9" : "#ffebee";
    
    // Color de texto para T.Euros (verde si positivo, rojo si negativo)
    public string ColorTextoEuros => BalanceEurosEsPositivo ? "#008800" : "#CC3333";
    
    partial void OnBalanceEurosEsPositivoChanged(bool value)
    {
        OnPropertyChanged(nameof(ColorFondoEuros));
        OnPropertyChanged(nameof(ColorTextoEuros));
    }
}

public class FiltroAplicadoItem
{
    public string Nombre { get; set; } = "";
    public string Valor { get; set; } = "";
}