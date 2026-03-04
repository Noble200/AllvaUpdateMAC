using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.Models.Admin;

/// <summary>
/// Modelo de datos para Comercios/Sucursales
/// Representa la entidad principal que agrupa locales
/// ACTUALIZADO: Los permisos ahora están a nivel de LOCAL, no de comercio
/// </summary>
public class ComercioModel
{
    // ============================================
    // PROPIEDADES BÁSICAS
    // ============================================

    public int IdComercio { get; set; }
    
    /// <summary>
    /// Nombre comercial del negocio
    /// </summary>
    public string NombreComercio { get; set; } = string.Empty;
    
    /// <summary>
    /// Razón social / Nombre SRL
    /// </summary>
    public string NombreSrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Dirección central del comercio/sucursal
    /// </summary>
    public string DireccionCentral { get; set; } = string.Empty;
    
    /// <summary>
    /// Número de contacto principal
    /// </summary>
    public string NumeroContacto { get; set; } = string.Empty;
    
    /// <summary>
    /// Email de contacto/contrato
    /// </summary>
    public string MailContacto { get; set; } = string.Empty;
    
    /// <summary>
    /// País donde opera el comercio
    /// </summary>
    public string Pais { get; set; } = string.Empty;

    /// <summary>
    /// Observaciones adicionales
    /// </summary>
    public string? Observaciones { get; set; }

    // ============================================
    // DATOS FISCALES
    // ============================================

    /// <summary>
    /// Tipo de entidad: Persona física o Persona jurídica
    /// </summary>
    public string Entidad { get; set; } = "Persona jurídica";

    /// <summary>
    /// Número de identificación fiscal (NIF, DNI o NIE)
    /// </summary>
    public string? IdentidadFiscal { get; set; }

    /// <summary>
    /// Dirección fiscal del comercio/autónomo
    /// </summary>
    public string? DireccionFiscal { get; set; }

    // ============================================
    // DATOS BANCARIOS
    // ============================================

    /// <summary>
    /// Entidad bancaria: CaixaBank, BBVA, Banco Santander, Banco Sabadell, Otro
    /// </summary>
    public string? Banco { get; set; }

    /// <summary>
    /// Número de cuenta bancaria IBAN
    /// </summary>
    public string? Iban { get; set; }
    
    // ============================================
    // CONFIGURACIÓN DE DIVISAS
    // ============================================
    
    /// <summary>
    /// Porcentaje de comisión por intercambio de divisas
    /// Solo visible para el dueño de la aplicación
    /// </summary>
    public decimal PorcentajeComisionDivisas { get; set; } = 0;
    
    // ============================================
    // ESTADO Y FECHAS
    // ============================================
    
    /// <summary>
    /// Indica si el comercio está activo
    /// </summary>
    public bool Activo { get; set; } = true;
    
    /// <summary>
    /// Fecha de registro del comercio
    /// </summary>
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Fecha de última modificación
    /// </summary>
    public DateTime FechaUltimaModificacion { get; set; } = DateTime.Now;
    
    // ============================================
    // RELACIONES
    // ============================================
    
    /// <summary>
    /// Lista de locales asociados a este comercio
    /// </summary>
    public List<LocalSimpleModel> Locales { get; set; } = new List<LocalSimpleModel>();
    
    /// <summary>
    /// Cantidad total de locales
    /// </summary>
    public int CantidadLocales => Locales?.Count ?? 0;
    
    /// <summary>
    /// Cantidad total de usuarios en todos los locales
    /// </summary>
    public int TotalUsuarios { get; set; } = 0;
    
    // ============================================
    // PROPIEDADES CALCULADAS PARA UI
    // ============================================

    /// <summary>
    /// Texto del estado para mostrar en UI (Badge de estado)
    /// </summary>
    public string EstadoTexto => Activo ? "Activo" : "Inactivo";

    /// <summary>
    /// Color del badge de estado para UI
    /// </summary>
    public string EstadoColor => Activo ? "#28a745" : "#dc3545";

    /// <summary>
    /// Texto para el botón toggle ON/OFF
    /// </summary>
    public string EstadoToggleTexto => TodosLocalesActivos ? "ON" : "OFF";

    /// <summary>
    /// Color del botón toggle: azul si activo, gris oscuro si inactivo
    /// </summary>
    public string EstadoBotonColor => TodosLocalesActivos ? "#0b5394" : "#595959";

    /// <summary>
    /// Indica si todos los locales del comercio están activos
    /// </summary>
    public bool TodosLocalesActivos => Locales?.All(l => l.Activo) ?? false;

    /// <summary>
    /// Cantidad maxima de locales a mostrar en la tarjeta del comercio
    /// </summary>
    private const int MaxLocalesVisibles = 3;

    /// <summary>
    /// Lista de locales visibles en la tarjeta (primeros 3)
    /// </summary>
    public List<LocalSimpleModel> LocalesVisibles => Locales?.Take(MaxLocalesVisibles).ToList() ?? new List<LocalSimpleModel>();

    /// <summary>
    /// Indica si hay mas locales que los mostrados
    /// </summary>
    public bool TieneMasLocales => (Locales?.Count ?? 0) > MaxLocalesVisibles;

    /// <summary>
    /// Cantidad de locales restantes que no se muestran
    /// </summary>
    public int LocalesRestantes => Math.Max(0, (Locales?.Count ?? 0) - MaxLocalesVisibles);
}

/// <summary>
/// Modelo simplificado de Local para mostrar en la lista de comercios
/// VERSIÓN CORREGIDA: Con propiedades observables para contadores de usuarios
/// Ahora hereda de ObservableObject para notificar cambios de propiedades
/// </summary>
public partial class LocalSimpleModel : ObservableObject
{
    public int IdLocal { get; set; }
    public string CodigoLocal { get; set; } = string.Empty;
    public string NombreLocal { get; set; } = string.Empty;
    
    // ============================================
    // UBICACIÓN GEOGRÁFICA - CAMPOS AGREGADOS
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

    /// <summary>
    /// Tipo de vía (Calle, Avenida, Pasaje, etc)
    /// </summary>
    public string TipoVia { get; set; } = string.Empty;
    
    // ============================================
    // DIRECCIÓN COMPLETA (SEGÚN BASE DE DATOS)
    // ============================================
    
    public string Direccion { get; set; } = string.Empty;
    public string LocalNumero { get; set; } = string.Empty;
    public string? Escalera { get; set; }
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
    
    public string? Email { get; set; }
    public string? Observaciones { get; set; }
    
    // ============================================
    // ESTADO Y USUARIOS
    // ============================================
    
    [ObservableProperty]
    private bool _activo = true;
    
    public int NumeroUsuarios { get; set; } = 0;
    
    /// <summary>
    /// Controla si se muestran los detalles expandidos del local
    /// </summary>
    [ObservableProperty]
    private bool _mostrarDetalles = false;
    
    /// <summary>
    /// Lista de usuarios asignados a este local
    /// ✅ CORRECCIÓN: Ahora notifica cambios en las propiedades calculadas
    /// </summary>
    private List<UserSimpleModel> _usuarios = new List<UserSimpleModel>();
    
    /// <summary>
    /// ✅ SOLUCIÓN: Propiedad observable para cantidad de usuarios fijos
    /// </summary>
    [ObservableProperty]
    private int _cantidadUsuariosFijos = 0;
    
    /// <summary>
    /// ✅ SOLUCIÓN: Propiedad observable para cantidad de usuarios flooter
    /// </summary>
    [ObservableProperty]
    private int _cantidadUsuariosFlooter = 0;
    
    public List<UserSimpleModel> Usuarios
    {
        get => _usuarios;
        set
        {
            if (_usuarios != value)
            {
                _usuarios = value;
                OnPropertyChanged(nameof(Usuarios));

                // ✅ CRÍTICO: Actualizar las propiedades observables
                CantidadUsuariosFijos = _usuarios?.Count(u => !u.EsFlooter) ?? 0;
                CantidadUsuariosFlooter = _usuarios?.Count(u => u.EsFlooter) ?? 0;
                OnPropertyChanged(nameof(TotalUsuarios));
                OnPropertyChanged(nameof(NombresUsuarios));
            }
        }
    }

    /// <summary>
    /// Total de usuarios con acceso a este local
    /// </summary>
    public int TotalUsuarios => _usuarios?.Count ?? 0;

    /// <summary>
    /// Nombres de usuarios separados por coma
    /// </summary>
    public string NombresUsuarios => _usuarios != null && _usuarios.Count > 0
        ? string.Join(", ", _usuarios.Select(u => u.NombreCompleto))
        : "Sin usuarios asignados";
    
    // ============================================
    // PERMISOS DE MÓDULOS POR LOCAL
    // ============================================
    
    /// <summary>
    /// Permiso para módulo de Divisas en este local
    /// </summary>
    public bool ModuloDivisas { get; set; } = false;
    
    /// <summary>
    /// Permiso para módulo de Pack de Alimentos en este local
    /// </summary>
    public bool ModuloPackAlimentos { get; set; } = false;
    
    /// <summary>
    /// Permiso para módulo de Billetes de Avión en este local
    /// </summary>
    public bool ModuloBilletesAvion { get; set; } = false;
    
    /// <summary>
    /// Permiso para módulo de Pack de Viajes en este local
    /// </summary>
    public bool ModuloPackViajes { get; set; } = false;

    // ============================================
    // FECHAS DE ACTIVIDAD
    // ============================================

    /// <summary>
    /// Fecha de última conexión de cualquier usuario en este local
    /// </summary>
    public DateTime? UltimaConexion { get; set; }

    /// <summary>
    /// Fecha de última operación realizada en este local
    /// </summary>
    public DateTime? UltimaOperacion { get; set; }

    // ============================================
    // PROPIEDADES CALCULADAS PARA UI
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
                : "Sin módulos activos";
        }
    }
}

/// <summary>
/// Modelo simplificado de Usuario para mostrar en los locales
/// </summary>
public class UserSimpleModel
{
    public int IdUsuario { get; set; }
    public string NumeroUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    
    /// <summary>
    /// CAMBIO: Flotante → Flooter
    /// </summary>
    public bool EsFlooter { get; set; } = false;
    
    /// <summary>
    /// Texto para mostrar si es flooter
    /// </summary>
    public string TipoUsuarioTexto => EsFlooter ? "Flooter" : "Fijo";
}