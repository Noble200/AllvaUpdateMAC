using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.Models.Admin;

/// <summary>
/// Modelo para balance consolidado de todos los locales
/// </summary>
public partial class BalanceConsolidadoModel : ObservableObject
{
    [ObservableProperty]
    private string _local = string.Empty;

    [ObservableProperty]
    private string _codigoLocal = string.Empty;

    [ObservableProperty]
    private int _idLocal;

    [ObservableProperty]
    private decimal _saldoInicial;

    [ObservableProperty]
    private decimal _ingresos;

    [ObservableProperty]
    private decimal _egresos;

    [ObservableProperty]
    private decimal _saldoFinal;

    [ObservableProperty]
    private int _cantidadOperaciones;

    [ObservableProperty]
    private DateTime _fechaUltimaOperacion;

    [ObservableProperty]
    private string _moneda = "EUR";

    public string SaldoInicialFormato => $"{SaldoInicial:N2} {Moneda}";

    public string IngresosFormato => $"+{Ingresos:N2} {Moneda}";

    public string EgresosFormato => $"-{Egresos:N2} {Moneda}";

    public string SaldoFinalFormato => $"{SaldoFinal:N2} {Moneda}";

    public string ColorSaldo => SaldoFinal >= 0 ? "#28a745" : "#dc3545";

    public string FechaUltimaOperacionFormato => FechaUltimaOperacion != DateTime.MinValue
        ? FechaUltimaOperacion.ToString("dd/MM/yyyy HH:mm")
        : "Sin operaciones";
}
