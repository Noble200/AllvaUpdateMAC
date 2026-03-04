using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.Models.Admin;

/// <summary>
/// Modelo para operaciones consolidadas de todos los locales del comercio
/// </summary>
public partial class OperacionConsolidadaModel : ObservableObject
{
    [ObservableProperty]
    private int _idOperacion;

    [ObservableProperty]
    private string _tipoOperacion = string.Empty;

    [ObservableProperty]
    private string _modulo = string.Empty;

    [ObservableProperty]
    private DateTime _fechaHora;

    [ObservableProperty]
    private string _numeroOperacion = string.Empty;

    [ObservableProperty]
    private string _cliente = string.Empty;

    [ObservableProperty]
    private decimal _monto;

    [ObservableProperty]
    private string _moneda = string.Empty;

    [ObservableProperty]
    private string _estado = string.Empty;

    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private int _idUsuario;

    [ObservableProperty]
    private string _local = string.Empty;

    [ObservableProperty]
    private string _codigoLocal = string.Empty;

    [ObservableProperty]
    private int _idLocal;

    [ObservableProperty]
    private string? _observaciones;

    public string FechaHoraFormato => FechaHora.ToString("dd/MM/yyyy HH:mm:ss");

    public string MontoFormato => $"{Monto:N2} {Moneda}";

    public string ColorEstado => Estado.ToLower() switch
    {
        "completada" or "aprobada" => "#28a745",
        "pendiente" => "#ffc107",
        "anulada" or "cancelada" => "#dc3545",
        _ => "#6c757d"
    };
}
