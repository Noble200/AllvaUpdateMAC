using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.Models.Admin;

/// <summary>
/// Modelo de suscripcion de un local - cuota mensual unificada por todos los modulos
/// </summary>
public partial class SuscripcionLocalModel : ObservableObject
{
    // ============================================
    // IDENTIDAD
    // ============================================

    public int IdSuscripcion { get; set; }
    public int IdLocal { get; set; }
    public int IdComercio { get; set; }
    public string CodigoLocal { get; set; } = string.Empty;
    public string NombreComercio { get; set; } = string.Empty;
    public string NombreLocal { get; set; } = string.Empty;
    public DateTime FechaCreacionLocal { get; set; }

    // ============================================
    // ESTADO DE SUSCRIPCION
    // ============================================

    public DateTime FechaInicio { get; set; }
    public DateTime? FechaUltimoPago { get; set; }
    public DateTime FechaPagadoHasta { get; set; }
    public decimal PrecioMensual { get; set; }
    public decimal? ImporteUltimoPago { get; set; }

    // ============================================
    // ACTIVIDAD (para prioridad de desbloqueo)
    // ============================================

    public int TotalOperaciones { get; set; }

    // ============================================
    // PROPIEDADES CALCULADAS
    // ============================================

    public bool Debe => FechaPagadoHasta < DateTime.Today;

    public int MesesDeuda
    {
        get
        {
            if (!Debe) return 0;
            var meses = ((DateTime.Today.Year - FechaPagadoHasta.Year) * 12)
                      + DateTime.Today.Month - FechaPagadoHasta.Month;
            return Math.Max(0, meses);
        }
    }

    public decimal MontoDeuda => MesesDeuda * PrecioMensual;

    public string EstadoPago => Debe ? "Deuda" : "Corriente";
    public string EstadoColor => Debe ? "#dc3545" : "#28a745";
    public string EstadoBackgroundColor => Debe ? "#FFEBEE" : "#E8F5E9";

    // ============================================
    // TEXTOS PARA UI
    // ============================================

    public string FechaPagadoHastaTexto => FechaPagadoHasta.ToString("dd/MM/yyyy");
    public string UltimoPagoTexto => FechaUltimoPago?.ToString("dd/MM/yyyy") ?? "---";
    public string PrecioMensualTexto => $"{PrecioMensual:N2}\u20AC";
    public string MontoDeudaTexto => MontoDeuda > 0 ? $"{MontoDeuda:N2}\u20AC" : "---";
    public string ImporteUltimoPagoTexto => ImporteUltimoPago.HasValue
        ? $"{ImporteUltimoPago.Value:N2}\u20AC" : "---";
}

/// <summary>
/// Modelo agregado de comercio para la pestaña Comercios
/// </summary>
public class ComercioSuscripcionModel
{
    public int IdComercio { get; set; }
    public string NombreComercio { get; set; } = string.Empty;
    public int NumLocales { get; set; }

    public decimal PrecioMesCuota { get; set; }
    public DateTime FechaPagadoHasta { get; set; }
    public DateTime? FechaUltimoPago { get; set; }
    public decimal? ImporteUltimoPago { get; set; }
    public decimal DeudaTotal { get; set; }

    public List<SuscripcionLocalModel> Locales { get; set; } = new();

    // Calculadas
    public bool Debe => DeudaTotal > 0;
    public string EstadoPago => Debe ? "Deuda" : "Corriente";
    public string EstadoColor => Debe ? "#dc3545" : "#28a745";
    public string EstadoBackgroundColor => Debe ? "#FFEBEE" : "#E8F5E9";

    public string FechaPagadoHastaTexto => FechaPagadoHasta.ToString("dd/MM/yyyy");
    public string UltimoPagoTexto => FechaUltimoPago?.ToString("dd/MM/yyyy") ?? "---";
    public string PrecioMesCuotaTexto => $"{PrecioMesCuota:N2}\u20AC";
    public string DeudaTotalTexto => DeudaTotal > 0 ? $"{DeudaTotal:N2}\u20AC" : "---";
    public string ImporteUltimoPagoTexto => ImporteUltimoPago.HasValue
        ? $"{ImporteUltimoPago.Value:N2}\u20AC" : "---";
}

/// <summary>
/// Modelo para registrar pagos de suscripciones
/// </summary>
public class PagoSuscripcionModel
{
    public int IdPago { get; set; }
    public int IdSuscripcion { get; set; }
    public int IdLocal { get; set; }
    public DateTime FechaPago { get; set; }
    public int MesesPagados { get; set; }
    public decimal MontoPagado { get; set; }
    public string? Observaciones { get; set; }
    public string RegistradoPor { get; set; } = string.Empty;
}
