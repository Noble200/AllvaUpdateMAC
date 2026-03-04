using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Allva.Desktop.Views;
using Allva.Desktop.Views.Admin;
using Allva.Desktop.ViewModels;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Services;

/// <summary>
/// Servicio de navegación para cambiar entre vistas
/// ACTUALIZADO: Pasa información completa del usuario y local al MainDashboard
/// </summary>
public class NavigationService
{
    public event EventHandler<object>? NavigationRequested;

    /// <summary>
    /// Navega a una vista específica
    /// </summary>
    public void NavigateTo(string viewName, object? parameter = null)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow == null) return;

            mainWindow.WindowState = WindowState.Maximized;

            UserControl? newView = viewName.ToLower() switch
            {
                "login" => CreateLoginView(),
                "maindashboard" or "dashboard" => CreateMainDashboardView(parameter),
                "admindashboard" or "admin" => CreateAdminDashboardView(parameter),
                _ => null
            };

            if (newView != null)
            {
                mainWindow.Content = newView;
                mainWindow.Title = $"Allva System - {GetViewTitle(viewName)}";
                NavigationRequested?.Invoke(this, newView);
            }
        }
    }

    /// <summary>
    /// Navega al dashboard principal después de un login exitoso de usuario normal
    /// </summary>
    public void NavigateToDashboard(LoginSuccessData? loginData = null)
    {
        NavigateTo("maindashboard", loginData);
    }

    /// <summary>
    /// Navega al panel de administración del sistema
    /// Solo para usuarios con rol "Administrador_Allva"
    /// </summary>
    public void NavigateToAdminDashboard(LoginSuccessData? loginData = null)
    {
        NavigateTo("admindashboard", loginData);
    }

    /// <summary>
    /// Navega al TestPanel (mantener compatibilidad con código existente)
    /// AHORA redirige al MainDashboard
    /// </summary>
    public void NavigateToTestPanel(LoginSuccessData loginData)
    {
        NavigateToDashboard(loginData);
    }

    /// <summary>
    /// Vuelve al login
    /// </summary>
    public void NavigateToLogin()
    {
        NavigateTo("login");
    }

    // ============================================
    // MÉTODOS PRIVADOS PARA CREAR VISTAS
    // ============================================

    private UserControl CreateLoginView()
    {
        var view = new LoginView();
        view.DataContext = new LoginViewModel();
        return view;
    }

    private UserControl CreateMainDashboardView(object? parameter)
    {
        var view = new MainDashboardView();

        if (parameter is LoginSuccessData loginData)
        {
            // Pasar TODOS los datos de sesión al Dashboard incluyendo permisos de modulos
            view.DataContext = new MainDashboardViewModel(
                loginData.IdUsuario,
                loginData.UserName,
                loginData.UserNumber,
                loginData.IdLocal,
                loginData.LocalCode,
                loginData.IdComercio,
                loginData.ModuloDivisas,
                loginData.ModuloPackAlimentos,
                loginData.ModuloBilletesAvion,
                loginData.ModuloPackViajes
            );
        }
        else
        {
            view.DataContext = new MainDashboardViewModel();
        }

        return view;
    }

    /// <summary>
    /// Crea vista del panel de administración del sistema
    /// </summary>
    private UserControl CreateAdminDashboardView(object? parameter)
    {
        var view = new AdminDashboardView();
        
        if (parameter is LoginSuccessData loginData)
        {
            view.DataContext = new AdminDashboardViewModel(loginData);
        }
        else
        {
            view.DataContext = new AdminDashboardViewModel();
        }
        
        return view;
    }

    /// <summary>
    /// Obtiene el título legible de la vista
    /// </summary>
    private string GetViewTitle(string viewName)
    {
        return viewName.ToLower() switch
        {
            "login" => "Login",
            "maindashboard" or "dashboard" => "Panel Principal",
            "admindashboard" or "admin" => "Panel de Administracion",
            _ => "Allva System"
        };
    }
}

/// <summary>
/// Datos del login exitoso
/// ACTUALIZADO: Incluye información completa del usuario y local para tracking
/// El sistema SIEMPRE sabe quién inició sesión y desde qué local
/// </summary>
public class LoginSuccessData
{
    // ============================================
    // IDENTIFICACIÓN DEL USUARIO
    // ============================================
    
    /// <summary>
    /// ID del usuario en la base de datos
    /// </summary>
    public int IdUsuario { get; set; }
    
    /// <summary>
    /// Nombre completo del usuario
    /// </summary>
    public string UserName { get; set; } = string.Empty;
    
    /// <summary>
    /// Número de usuario (ej: "0001", "1234")
    /// </summary>
    public string UserNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo de usuario: "ADMIN_ALLVA", "FLOATER", "USUARIO_LOCAL"
    /// </summary>
    public string UserType { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre del rol del usuario
    /// </summary>
    public string RoleName { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica si el usuario es Floater
    /// </summary>
    public bool EsFloater { get; set; } = false;
    
    // ============================================
    // INFORMACIÓN DEL LOCAL ACTIVO
    // ============================================
    
    /// <summary>
    /// ID del local donde está trabajando
    /// </summary>
    public int IdLocal { get; set; }
    
    /// <summary>
    /// Código del local donde está trabajando (ej: "AGP001", "MAD002")
    /// </summary>
    public string LocalCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Nombre del local donde está trabajando
    /// </summary>
    public string LocalName { get; set; } = string.Empty;
    
    // ============================================
    // INFORMACIÓN DEL COMERCIO
    // ============================================
    
    /// <summary>
    /// ID del comercio al que pertenece el local
    /// </summary>
    public int IdComercio { get; set; }
    
    // ============================================
    // SEGURIDAD Y SESIÓN
    // ============================================
    
    /// <summary>
    /// Token de sesión
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Indica si el usuario es administrador del sistema
    /// </summary>
    public bool IsSystemAdmin { get; set; } = false;

    /// <summary>
    /// Permisos específicos del administrador
    /// </summary>
    public PermisosAdministrador? Permisos { get; set; }

    // ============================================
    // PERMISOS DE MODULOS DEL LOCAL
    // ============================================

    /// <summary>
    /// Indica si el local tiene acceso al modulo de Divisas
    /// </summary>
    public bool ModuloDivisas { get; set; } = false;

    /// <summary>
    /// Indica si el local tiene acceso al modulo de Pack Alimentos
    /// </summary>
    public bool ModuloPackAlimentos { get; set; } = false;

    /// <summary>
    /// Indica si el local tiene acceso al modulo de Billetes de Avion
    /// </summary>
    public bool ModuloBilletesAvion { get; set; } = false;

    /// <summary>
    /// Indica si el local tiene acceso al modulo de Pack Viajes
    /// </summary>
    public bool ModuloPackViajes { get; set; } = false;

    // ============================================
    // PROPIEDADES CALCULADAS
    // ============================================
    
    /// <summary>
    /// Texto descriptivo del tipo de usuario para mostrar en UI
    /// </summary>
    public string TipoUsuarioDisplay => UserType switch
    {
        "ADMIN_ALLVA" => "Administrador Allva",
        "FLOATER" => "Usuario Floater",
        "USUARIO_LOCAL" => "Usuario Local",
        _ => "Usuario"
    };
    
    /// <summary>
    /// Información completa de ubicación para mostrar en UI
    /// </summary>
    public string UbicacionDisplay => IsSystemAdmin 
        ? "Central Allva" 
        : $"{LocalCode} - {LocalName}";
}

/// <summary>
/// Permisos granulares para administradores Allva
/// Define a qué módulos del AdminDashboard puede acceder cada administrador
/// </summary>
public class PermisosAdministrador
{
    /// <summary>
    /// Acceso al módulo "Gestion de Comercios"
    /// </summary>
    public bool AccesoGestionComercios { get; set; } = true;

    /// <summary>
    /// Acceso al módulo "Gestion de Usuarios" (usuarios de locales)
    /// </summary>
    public bool AccesoGestionUsuariosLocales { get; set; } = true;

    /// <summary>
    /// Acceso al módulo "Usuarios Allva" (gestionar otros administradores)
    /// Solo super administradores tienen este permiso
    /// </summary>
    public bool AccesoGestionUsuariosAllva { get; set; } = false;

    /// <summary>
    /// Acceso a Analytics e Informes
    /// </summary>
    public bool AccesoAnalytics { get; set; } = false;

    /// <summary>
    /// Acceso a Configuracion del Sistema
    /// </summary>
    public bool AccesoConfiguracionSistema { get; set; } = false;

    /// <summary>
    /// Acceso a Facturación Global
    /// </summary>
    public bool AccesoFacturacionGlobal { get; set; } = false;

    /// <summary>
    /// Acceso a Auditoria
    /// </summary>
    public bool AccesoAuditoria { get; set; } = false;

    /// <summary>
    /// Verifica si tiene permiso para un módulo específico
    /// </summary>
    public bool TienePermiso(string nombreModulo)
    {
        return nombreModulo.ToLower() switch
        {
            "comercios" => AccesoGestionComercios,
            "usuarios" => AccesoGestionUsuariosLocales,
            "usuarios_allva" => AccesoGestionUsuariosAllva,
            "analytics" => AccesoAnalytics,
            "configuracion" => AccesoConfiguracionSistema,
            "facturacion" => AccesoFacturacionGlobal,
            "auditoria" => AccesoAuditoria,
            _ => false
        };
    }

    /// <summary>
    /// Indica si es un Super Administrador (tiene permiso para gestionar otros admins)
    /// </summary>
    public bool EsSuperAdministrador => AccesoGestionUsuariosAllva;
}