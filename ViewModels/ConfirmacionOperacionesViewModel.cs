using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.ViewModels;

public partial class ConfirmacionOperacionesViewModel : ObservableObject
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
    private string totalOperaciones = "0";
    
    [ObservableProperty]
    private string totalEuros = "0.00";
    
    [ObservableProperty]
    private string totalDivisas = "0.00";
    
    [ObservableProperty]
    private string panelSeleccionado = "";
    
    [ObservableProperty]
    private bool sinFiltros = true;
    
    public ObservableCollection<FiltroOperacionItem> FiltrosAplicados { get; } = new();
}

public class FiltroOperacionItem
{
    public string Nombre { get; set; } = "";
    public string Valor { get; set; } = "";
}