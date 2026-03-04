using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.Models;

public partial class VueloItem : ObservableObject
{
    public int Id { get; set; }
    public string OrigenCodigo { get; set; } = string.Empty;
    public string DestinoCodigo { get; set; } = string.Empty;
    public string Aerolinea { get; set; } = string.Empty;
    public string NumeroVuelo { get; set; } = string.Empty;
    public string HoraSalida { get; set; } = string.Empty;
    public string HoraLlegada { get; set; } = string.Empty;
    public string Duracion { get; set; } = string.Empty;
    public int Escalas { get; set; }
    public string? CiudadEscala { get; set; }
    public string? TiempoConexion { get; set; }
    public string? TerminalOrigen { get; set; }
    public string? TerminalDestino { get; set; }
    public string? Avion { get; set; }
    public decimal? PrecioTurista { get; set; }
    public decimal? PrecioTuristaPremium { get; set; }
    public decimal? PrecioBusiness { get; set; }
    public bool DisponibleTurista { get; set; }
    public bool DisponiblePremium { get; set; }
    public bool DisponibleBusiness { get; set; }
    public string EquipajeIncluido { get; set; } = "23kg";

    // Nombres de ciudades (se llenan al cargar)
    public string OrigenCiudad { get; set; } = string.Empty;
    public string DestinoCiudad { get; set; } = string.Empty;
    public string OrigenAeropuerto { get; set; } = string.Empty;
    public string DestinoAeropuerto { get; set; } = string.Empty;

    // --- Propiedades computadas para la UI ---

    public string RutaTexto => $"{OrigenCiudad} - {DestinoCiudad}";
    public string RutaCodigosTexto => $"{OrigenCodigo}  →  {DestinoCodigo}";

    public string TextoEscalas => Escalas == 0
        ? "Directo"
        : $"{Escalas} escala\n{Duracion}";

    public string TextoEscalasCorto => Escalas == 0
        ? "Directo"
        : $"{Escalas} escala {Duracion}";

    public string PrecioTuristaTexto => DisponibleTurista && PrecioTurista.HasValue
        ? $"{PrecioTurista:N0}€"
        : "no disponible";

    public string PrecioPremiumTexto => DisponiblePremium && PrecioTuristaPremium.HasValue
        ? $"{PrecioTuristaPremium:N0}€"
        : "no disponible";

    public string PrecioBusinessTexto => DisponibleBusiness && PrecioBusiness.HasValue
        ? $"{PrecioBusiness:N0}€"
        : "no disponible";

    public string OperadoPorTexto => $"Operado por:  {Aerolinea}";

    // --- Estado de selección ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TuristaSeleccionada))]
    [NotifyPropertyChangedFor(nameof(PremiumSeleccionada))]
    [NotifyPropertyChangedFor(nameof(BusinessSeleccionada))]
    private bool _estaSeleccionado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TuristaSeleccionada))]
    [NotifyPropertyChangedFor(nameof(PremiumSeleccionada))]
    [NotifyPropertyChangedFor(nameof(BusinessSeleccionada))]
    private string _claseSeleccionada = "";

    public bool TuristaSeleccionada => EstaSeleccionado && ClaseSeleccionada == "turista";
    public bool PremiumSeleccionada => EstaSeleccionado && ClaseSeleccionada == "premium";
    public bool BusinessSeleccionada => EstaSeleccionado && ClaseSeleccionada == "business";

    public decimal PrecioSeleccionado => ClaseSeleccionada switch
    {
        "turista" => PrecioTurista ?? 0,
        "premium" => PrecioTuristaPremium ?? 0,
        "business" => PrecioBusiness ?? 0,
        _ => PrecioTurista ?? 0
    };
}

/// <summary>
/// Precio de un día del calendario
/// </summary>
public class PrecioCalendarioItem
{
    public DateTime Fecha { get; set; }
    public decimal PrecioMinimo { get; set; }
    public bool EsVacio => Fecha == DateTime.MinValue;
    public string Dia => EsVacio ? "" : Fecha.Day.ToString();
    public string PrecioTexto => EsVacio ? "" : $"{PrecioMinimo:N0}€";
    public bool EsHoy => Fecha.Date == DateTime.Today;
}
