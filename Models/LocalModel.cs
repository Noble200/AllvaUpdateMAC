using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Allva.Desktop.Models;

/// <summary>
/// Modelo auxiliar para gestionar locales en el formulario
/// VERSIÓN CORREGIDA: Sin campo ComisionDivisas
/// </summary>
public class LocalFormModel : INotifyPropertyChanged
{
    private bool _estaSeleccionado;
    private bool _mostrarDetalles;

    public int IdLocal { get; set; }

    /// <summary>
    /// Indica si este local está seleccionado en la búsqueda múltiple
    /// </summary>
    public bool EstaSeleccionado
    {
        get => _estaSeleccionado;
        set
        {
            if (_estaSeleccionado != value)
            {
                _estaSeleccionado = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Controla si se muestran los detalles expandidos del local en la UI
    /// </summary>
    public bool MostrarDetalles
    {
        get => _mostrarDetalles;
        set
        {
            if (_mostrarDetalles != value)
            {
                _mostrarDetalles = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    /// <summary>
    /// ID del comercio al que pertenece este local
    /// IMPORTANTE: Propiedad agregada para compatibilidad con ManageUsersViewModel
    /// </summary>
    public int IdComercio { get; set; }
    
    public string CodigoLocal { get; set; } = string.Empty;
    public string NombreLocal { get; set; } = string.Empty;
    
    // ============================================
    // UBICACIÓN GEOGRÁFICA - NUEVOS CAMPOS
    // ============================================
    
    /// <summary>
    /// País donde se ubica el local
    /// </summary>
    public string Pais { get; set; } = string.Empty;
    
    /// <summary>
    /// Código postal del local
    /// </summary>
    public string CodigoPostal { get; set; } = string.Empty;

    /// <summary>
    /// Ciudad donde se ubica el local
    /// </summary>
    public string Ciudad { get; set; } = string.Empty;

    // ============================================
    // DIRECCIÓN COMPLETA (SEGÚN BASE DE DATOS)
    // ============================================
    
    /// <summary>
    /// Tipo de vía (Calle, Avenida, Pasaje, etc)
    /// </summary>
    public string TipoVia { get; set; } = string.Empty;
    
    /// <summary>
    /// Dirección principal (calle, avenida, etc)
    /// </summary>
    public string Direccion { get; set; } = string.Empty;
    
    /// <summary>
    /// Número del local/establecimiento
    /// </summary>
    public string LocalNumero { get; set; } = string.Empty;
    
    /// <summary>
    /// Escalera (si aplica)
    /// </summary>
    public string? Escalera { get; set; }
    
    /// <summary>
    /// Piso (si aplica)
    /// </summary>
    public string? Piso { get; set; }
    
    // ============================================
    // DATOS DE CONTACTO Y GESTIÓN
    // ============================================
    
    /// <summary>
    /// Número de móvil del local
    /// </summary>
    public string? Movil { get; set; }
    
    /// <summary>
    /// Teléfono de contacto del local
    /// </summary>
    public string? Telefono { get; set; }
    
    /// <summary>
    /// Email de contacto del local
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Número máximo de usuarios permitidos en este local
    /// </summary>
    public int NumeroUsuariosMax { get; set; } = 10;
    
    /// <summary>
    /// Observaciones adicionales del local
    /// </summary>
    public string? Observaciones { get; set; }
    
    // ============================================
    // ESTADO Y FECHAS
    // ============================================
    
    public bool Activo { get; set; } = true;
    
    // ============================================
    // PERMISOS DE MÓDULOS POR LOCAL
    // ============================================
    
    public bool ModuloDivisas { get; set; } = false;
    public bool ModuloPackAlimentos { get; set; } = false;
    public bool ModuloBilletesAvion { get; set; } = false;
    public bool ModuloPackViajes { get; set; } = false;
    
    // ============================================
    // ❌ ELIMINADO: ComisionDivisas (no se usa en esta versión)
    // ============================================
    
    // ============================================
    // PROPIEDADES CALCULADAS
    // ============================================
    
    /// <summary>
    /// Dirección completa formateada para mostrar
    /// </summary>
    public string DireccionCompleta
    {
        get
        {
            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(TipoVia))
                partes.Add(TipoVia);

            if (!string.IsNullOrWhiteSpace(Direccion))
                partes.Add(Direccion);

            if (!string.IsNullOrWhiteSpace(LocalNumero))
                partes.Add($"Nº {LocalNumero}");

            if (!string.IsNullOrWhiteSpace(Escalera))
                partes.Add($"Esc. {Escalera}");

            if (!string.IsNullOrWhiteSpace(Piso))
                partes.Add($"Piso {Piso}");

            if (!string.IsNullOrWhiteSpace(CodigoPostal))
                partes.Add($"CP {CodigoPostal}");

            if (!string.IsNullOrWhiteSpace(Ciudad))
                partes.Add(Ciudad);

            if (!string.IsNullOrWhiteSpace(Pais))
                partes.Add(Pais);

            return partes.Count > 0
                ? string.Join(", ", partes)
                : "Sin dirección";
        }
    }
    
    /// <summary>
    /// Resumen de permisos para mostrar en UI
    /// </summary>
    public string PermisosResumen
    {
        get
        {
            var permisos = new List<string>();
            if (ModuloDivisas) permisos.Add("Divisas");
            if (ModuloPackAlimentos) permisos.Add("Alimentos");
            if (ModuloBilletesAvion) permisos.Add("Billetes");
            if (ModuloPackViajes) permisos.Add("Viajes");
            
            return permisos.Count > 0 
                ? string.Join(", ", permisos) 
                : "Sin módulos";
        }
    }
}