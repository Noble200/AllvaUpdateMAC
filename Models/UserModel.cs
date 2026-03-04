using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Allva.Desktop.Models;

/// <summary>
/// Modelo de usuario del sistema
/// ACTUALIZADO: Propiedades para mostrar códigos de locales múltiples y comercio para flooters
/// </summary>
public class UserModel : INotifyPropertyChanged
{
    private bool _activo;
    private string _codigosLocalesFlooter = string.Empty;
    private string _nombreComercioFlooter = string.Empty;
    
    public int IdUsuario { get; set; }
    public string NumeroUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string SegundoNombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string SegundoApellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string Password { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public bool EsFlotante { get; set; }
    
    public bool Activo
    {
        get => _activo;
        set
        {
            if (_activo != value)
            {
                _activo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EstadoTexto));
                OnPropertyChanged(nameof(EstadoColor));
                OnPropertyChanged(nameof(EstadoBotonTexto));
                OnPropertyChanged(nameof(EstadoBotonColor));
                OnPropertyChanged(nameof(EstadoTextoDisplay));
                OnPropertyChanged(nameof(EstadoToggleTexto));
            }
        }
    }
    
    public DateTime? UltimoAcceso { get; set; }
    public DateTime? UltimaOperacion { get; set; }

    // ============================================
    // PROPIEDADES DE NAVEGACIÓN
    // ============================================
    
    public int? IdLocalPrincipal { get; set; }
    public string? NombreLocal { get; set; }
    public string? NombreComercio { get; set; }
    
    // ============================================
    // PROPIEDADES PARA FLOOTERS
    // ============================================
    
    /// <summary>
    /// Códigos de todos los locales asignados al flooter (máximo 3 visibles, si hay más muestra "+X más")
    /// Se establece desde el ViewModel al cargar los datos
    /// </summary>
    public string CodigosLocalesFlooter
    {
        get => _codigosLocalesFlooter;
        set
        {
            if (_codigosLocalesFlooter != value)
            {
                _codigosLocalesFlooter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CodigoLocalDisplay));
            }
        }
    }
    
    /// <summary>
    /// Nombre del comercio al que pertenece el flooter (basado en sus locales asignados)
    /// Se establece desde el ViewModel al cargar los datos
    /// </summary>
    public string NombreComercioFlooter
    {
        get => _nombreComercioFlooter;
        set
        {
            if (_nombreComercioFlooter != value)
            {
                _nombreComercioFlooter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NombreComercioDisplay));
            }
        }
    }
    
    // ============================================
    // PROPIEDADES CALCULADAS PARA UI
    // ============================================
    
    public string NombreCompleto => $"{Nombre} {Apellidos}";
    
    /// <summary>
    /// Tipo de usuario para mostrar en UI (Flooter en lugar de Flotante)
    /// </summary>
    public string TipoUsuarioDisplay => EsFlotante ? "Flooter" : "Normal";
    
    /// <summary>
    /// Texto de estado para mostrar en cards (como campo de información)
    /// </summary>
    public string EstadoTextoDisplay => Activo ? "Activo" : "Inactivo";
    
    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
    
    public string EstadoColor => Activo ? "#28a745" : "#dc3545";
    
    public string UltimoAccesoTexto => UltimoAcceso.HasValue
        ? UltimoAcceso.Value.ToString("dd/MM/yyyy HH:mm")
        : "Nunca";

    public string UltimaOperacionTexto => UltimaOperacion.HasValue
        ? UltimaOperacion.Value.ToString("dd/MM/yyyy HH:mm")
        : "Sin operaciones";
    
    // ============================================
    // PROPIEDADES ADICIONALES PARA MÓDULO DE GESTIÓN
    // ============================================
    
    public int IdComercio { get; set; }
    public int IdLocal { get; set; }
    public string CodigoLocal { get; set; } = string.Empty;
    
    /// <summary>
    /// Código de local para mostrar en UI: si es flooter muestra los códigos múltiples, si no el código único
    /// </summary>
    public string CodigoLocalDisplay => EsFlotante 
        ? (string.IsNullOrEmpty(CodigosLocalesFlooter) ? "N/A" : CodigosLocalesFlooter)
        : (string.IsNullOrEmpty(CodigoLocal) ? "N/A" : CodigoLocal);
    
    /// <summary>
    /// Nombre de comercio para mostrar en UI: si es flooter muestra el comercio calculado, si no el normal
    /// </summary>
    public string NombreComercioDisplay => EsFlotante 
        ? (string.IsNullOrEmpty(NombreComercioFlooter) ? "Sin comercio" : NombreComercioFlooter)
        : (string.IsNullOrEmpty(NombreComercio) ? "Sin asignar" : NombreComercio);
    
    public string EstadoBotonTexto => Activo ? "Desactivar" : "Activar";
    public string EstadoBotonColor => Activo ? "#0b5394" : "#595959";

    /// <summary>
    /// Texto para el botón toggle de estado: ON si está activo, OFF si está inactivo
    /// </summary>
    public string EstadoToggleTexto => Activo ? "ON" : "OFF";
    
    // ============================================
    // INotifyPropertyChanged
    // ============================================
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}