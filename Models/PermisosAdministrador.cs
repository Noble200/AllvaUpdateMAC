namespace Allva.Desktop.Models;

/// <summary>
/// Permisos específicos para administradores del sistema
/// Define qué módulos y funciones puede acceder cada administrador
/// </summary>
public class PermisosAdministrador
{
    /// <summary>
    /// Acceso al módulo de Gestión de Comercios
    /// </summary>
    public bool AccesoGestionComercios { get; set; } = true;
    
    /// <summary>
    /// Acceso al módulo de Gestión de Usuarios de Locales
    /// </summary>
    public bool AccesoGestionUsuariosLocales { get; set; } = true;
    
    /// <summary>
    /// Acceso al módulo de Usuarios Allva (solo para nivel 3 y 4)
    /// </summary>
    public bool AccesoGestionUsuariosAllva { get; set; } = false;
    
    /// <summary>
    /// Acceso al módulo de Configuración de Divisas
    /// Permite modificar márgenes de ganancia por comercio/local
    /// </summary>
    public bool AccesoConfiguracionDivisas { get; set; } = true;
    
    /// <summary>
    /// Acceso a reportes y estadísticas globales
    /// </summary>
    public bool AccesoReportes { get; set; } = true;
    
    /// <summary>
    /// Acceso a incidencias del sistema
    /// </summary>
    public bool AccesoIncidencias { get; set; } = true;
    
    /// <summary>
    /// Acceso a balance de cuentas
    /// </summary>
    public bool AccesoBalanceCuentas { get; set; } = true;
    
    /// <summary>
    /// Puede crear nuevos comercios
    /// </summary>
    public bool PuedeCrearComercios { get; set; } = true;
    
    /// <summary>
    /// Puede editar comercios existentes
    /// </summary>
    public bool PuedeEditarComercios { get; set; } = true;
    
    /// <summary>
    /// Puede eliminar/desactivar comercios
    /// </summary>
    public bool PuedeEliminarComercios { get; set; } = false;
    
    /// <summary>
    /// Puede crear usuarios en locales
    /// </summary>
    public bool PuedeCrearUsuarios { get; set; } = true;
    
    /// <summary>
    /// Puede modificar configuración global del sistema
    /// </summary>
    public bool PuedeModificarConfiguracion { get; set; } = false;
    
    /// <summary>
    /// Nivel de acceso del administrador (1-4)
    /// 1 = Básico, 2 = Intermedio, 3 = Avanzado, 4 = Super Admin
    /// </summary>
    public int NivelAcceso { get; set; } = 1;
    
    /// <summary>
    /// Crea permisos por defecto según el nivel de acceso
    /// </summary>
    public static PermisosAdministrador CrearPorNivel(int nivel)
    {
        return nivel switch
        {
            1 => new PermisosAdministrador
            {
                NivelAcceso = 1,
                AccesoGestionComercios = true,
                AccesoGestionUsuariosLocales = true,
                AccesoGestionUsuariosAllva = false,
                AccesoConfiguracionDivisas = false,
                AccesoReportes = true,
                AccesoIncidencias = true,
                AccesoBalanceCuentas = false,
                PuedeCrearComercios = false,
                PuedeEditarComercios = false,
                PuedeEliminarComercios = false,
                PuedeCrearUsuarios = false,
                PuedeModificarConfiguracion = false
            },
            2 => new PermisosAdministrador
            {
                NivelAcceso = 2,
                AccesoGestionComercios = true,
                AccesoGestionUsuariosLocales = true,
                AccesoGestionUsuariosAllva = false,
                AccesoConfiguracionDivisas = true,
                AccesoReportes = true,
                AccesoIncidencias = true,
                AccesoBalanceCuentas = true,
                PuedeCrearComercios = true,
                PuedeEditarComercios = true,
                PuedeEliminarComercios = false,
                PuedeCrearUsuarios = true,
                PuedeModificarConfiguracion = false
            },
            3 => new PermisosAdministrador
            {
                NivelAcceso = 3,
                AccesoGestionComercios = true,
                AccesoGestionUsuariosLocales = true,
                AccesoGestionUsuariosAllva = true,
                AccesoConfiguracionDivisas = true,
                AccesoReportes = true,
                AccesoIncidencias = true,
                AccesoBalanceCuentas = true,
                PuedeCrearComercios = true,
                PuedeEditarComercios = true,
                PuedeEliminarComercios = true,
                PuedeCrearUsuarios = true,
                PuedeModificarConfiguracion = true
            },
            4 => new PermisosAdministrador
            {
                NivelAcceso = 4,
                AccesoGestionComercios = true,
                AccesoGestionUsuariosLocales = true,
                AccesoGestionUsuariosAllva = true,
                AccesoConfiguracionDivisas = true,
                AccesoReportes = true,
                AccesoIncidencias = true,
                AccesoBalanceCuentas = true,
                PuedeCrearComercios = true,
                PuedeEditarComercios = true,
                PuedeEliminarComercios = true,
                PuedeCrearUsuarios = true,
                PuedeModificarConfiguracion = true
            },
            _ => new PermisosAdministrador()
        };
    }
}