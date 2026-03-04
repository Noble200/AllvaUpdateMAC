using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.Models.Admin;

/// <summary>
/// Modelo para registrar la actividad de usuarios (login/logout)
/// </summary>
public partial class ActividadUsuarioModel : ObservableObject
{
    [ObservableProperty]
    private int _idActividad;

    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private int _idUsuario;

    [ObservableProperty]
    private string _numeroUsuario = string.Empty;

    [ObservableProperty]
    private string _tipoActividad = string.Empty;

    [ObservableProperty]
    private DateTime _fechaHora;

    [ObservableProperty]
    private string _local = string.Empty;

    [ObservableProperty]
    private string _codigoLocal = string.Empty;

    [ObservableProperty]
    private int _idLocal;

    [ObservableProperty]
    private string? _direccionIp;

    [ObservableProperty]
    private string? _detalles;

    public string FechaHoraFormato => FechaHora.ToString("dd/MM/yyyy HH:mm:ss");

    public string TipoActividadFormato => TipoActividad.ToUpper();

    public string ColorActividad => TipoActividad.ToLower() switch
    {
        "login" or "inicio_sesion" => "#28a745",
        "logout" or "cierre_sesion" => "#dc3545",
        "actividad" => "#0b5394",
        _ => "#6c757d"
    };

    public string IconoActividad => TipoActividad.ToLower() switch
    {
        "login" or "inicio_sesion" => "🔓",
        "logout" or "cierre_sesion" => "🔒",
        "actividad" => "⚡",
        _ => "📋"
    };
}
