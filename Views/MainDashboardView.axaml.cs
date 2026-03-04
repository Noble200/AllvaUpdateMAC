using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Allva.Desktop.ViewModels;

namespace Allva.Desktop.Views;

public partial class MainDashboardView : UserControl
{
    public MainDashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        UpdateMenuSelection("dashboard");
        UpdateModuleHeader("dashboard");
    }

    private MainDashboardViewModel? ViewModel => DataContext as MainDashboardViewModel;

    // ============================================
    // NAVEGACION DEL MENU PRINCIPAL
    // ============================================

    private void NavigateToDashboard(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NavigateToModule("dashboard");
        UpdateMenuSelection("dashboard");
        UpdateModuleHeader("dashboard");
    }

    private void NavigateToDivisas(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NavigateToModule("divisas");
        UpdateMenuSelection("divisas");
        UpdateModuleHeader("divisas");
    }

    private void NavigateToAlimentos(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NavigateToModule("alimentos");
        UpdateMenuSelection("alimentos");
        UpdateModuleHeader("alimentos");
    }

    private void NavigateToBilletes(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NavigateToModule("billetes");
        UpdateMenuSelection("billetes");
        UpdateModuleHeader("billetes");
    }

    private void NavigateToViajes(object? sender, RoutedEventArgs e)
    {
        ViewModel?.NavigateToModule("viajes");
        UpdateMenuSelection("viajes");
        UpdateModuleHeader("viajes");
    }

    private void NavigateToAdministracion(object? sender, RoutedEventArgs e)
    {
        // Mostrar el diálogo de contraseña antes de navegar
        MostrarDialogoContrasena();
    }

    private void MostrarDialogoContrasena()
    {
        if (ViewModel == null) return;

        // Crear el diálogo de contraseña
        var dialogView = new PasswordAdminDialogView();
        var dialogViewModel = new PasswordAdminDialogViewModel(ViewModel.IdComercio);
        dialogView.DataContext = dialogViewModel;

        // Suscribirse al evento de cierre del diálogo
        dialogView.DialogClosed += OnPasswordDialogClosed;

        // Mostrar el diálogo
        var container = this.FindControl<ContentControl>("PasswordDialogContainer");
        if (container != null)
        {
            container.Content = dialogView;
            container.IsVisible = true;
        }
    }

    private void OnPasswordDialogClosed(object? sender, bool resultado)
    {
        // Ocultar el diálogo
        var container = this.FindControl<ContentControl>("PasswordDialogContainer");
        if (container != null)
        {
            container.Content = null;
            container.IsVisible = false;
        }

        // Si la contraseña fue correcta, navegar al panel de administración
        if (resultado && ViewModel != null)
        {
            ViewModel.NavigateToModule("administracion");
            UpdateMenuSelection("administracion");
            UpdateModuleHeader("administracion");
        }
        else
        {
            // Si fue cancelado o la contraseña incorrecta, volver a Últimas Noticias
            UpdateMenuSelection("dashboard");
        }
    }

    // ============================================
    // MENU HAMBURGUESA
    // ============================================

    private void MenuButton_Click(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("MenuPopup");
        if (popup != null)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void NavigateToOperaciones(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("MenuPopup");
        if (popup != null)
        {
            popup.IsOpen = false;
        }
        
        ViewModel?.NavigateToModule("operaciones");
        UpdateMenuSelection("");
        UpdateModuleHeader("operaciones");
    }
    
    private void NavigateToBalanceCuentas(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("MenuPopup");
        if (popup != null)
        {
            popup.IsOpen = false;
        }
        
        ViewModel?.NavigateToModule("balancecuentas");
        UpdateMenuSelection("");
        UpdateModuleHeader("balancecuentas");
    }

    /// <summary>
    /// Metodo publico para navegar a Ultimas Noticias desde otras vistas
    /// </summary>
    public void IrAUltimasNoticias()
    {
        ViewModel?.NavigateToModule("dashboard");
        UpdateMenuSelection("dashboard");
        UpdateModuleHeader("dashboard");
    }

    // ============================================
    // BOTONES DE ACCION DEL HEADER
    // ============================================

    private void OpenNotifications(object? sender, RoutedEventArgs e)
    {
        // TODO: Implementar apertura de notificaciones
    }

    // ============================================
    // POPUP DE USUARIO
    // ============================================

    private void UserButton_Click(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("UserPopup");
        if (popup != null)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

    private void NavigateToMiPerfil(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("UserPopup");
        if (popup != null)
        {
            popup.IsOpen = false;
        }

        ViewModel?.NavigateToModule("miperfil");
        UpdateMenuSelection("");
        UpdateModuleHeader("miperfil");
    }

    private void NavigateToCentroAyudaFromUser(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("UserPopup");
        if (popup != null)
        {
            popup.IsOpen = false;
        }

        ViewModel?.NavigateToModule("centroayuda");
        UpdateMenuSelection("");
        UpdateModuleHeader("centroayuda");
    }

    private void NavigateToCuentasBancarias(object? sender, RoutedEventArgs e)
    {
        var popup = this.FindControl<Popup>("UserPopup");
        if (popup != null)
        {
            popup.IsOpen = false;
        }

        ViewModel?.NavigateToModule("cuentasbancarias");
        UpdateMenuSelection("");
        UpdateModuleHeader("cuentasbancarias");
    }

    // ============================================
    // SESION
    // ============================================

    private void CerrarSesion(object? sender, RoutedEventArgs e)
    {
        ViewModel?.Logout();
    }

    // ============================================
    // UTILIDADES
    // ============================================

    private void UpdateMenuSelection(string selectedModule)
    {
        BtnDashboard.Classes.Set("menu-item-selected", selectedModule == "dashboard");
        BtnDashboard.Classes.Set("menu-item", selectedModule != "dashboard");

        BtnDivisas.Classes.Set("menu-item-selected", selectedModule == "divisas");
        BtnDivisas.Classes.Set("menu-item", selectedModule != "divisas");

        BtnAlimentos.Classes.Set("menu-item-selected", selectedModule == "alimentos");
        BtnAlimentos.Classes.Set("menu-item", selectedModule != "alimentos");

        BtnBilletes.Classes.Set("menu-item-selected", selectedModule == "billetes");
        BtnBilletes.Classes.Set("menu-item", selectedModule != "billetes");

        BtnViajes.Classes.Set("menu-item-selected", selectedModule == "viajes");
        BtnViajes.Classes.Set("menu-item", selectedModule != "viajes");

        BtnAdministracion.Classes.Set("menu-item-selected", selectedModule == "administracion");
        BtnAdministracion.Classes.Set("menu-item", selectedModule != "administracion");
    }

    private void UpdateModuleHeader(string moduleName)
    {
        // Ya no se usa porque el TopBar ahora es estilo BackOffice
        // que solo muestra el nombre de usuario y código de local
        // (sin título ni descripción de módulo)

        /* CODIGO ANTERIOR - Ya no aplica con el nuevo TopBar
        (string title, string description) = moduleName.ToLower() switch
        {
            "dashboard" => ("Ultimas Noticias", "Mantente informado con las ultimas novedades"),
            "divisas" => ("Compra de Divisas", "Gestiona operaciones de cambio de moneda"),
            "alimentos" => ("Pack de Alimentos", "Administra paquetes de alimentacion"),
            "billetes" => ("Billetes de Avion", "Reserva y gestion de vuelos"),
            "viajes" => ("Packs de Viajes", "Paquetes turisticos completos"),
            "administracion" => ("Administracion", "Panel de administracion del sistema"),
            "operaciones" => ("Operaciones", "Historial detallado de todas las operaciones"),
            "balancecuentas" => ("Balance de Cuentas", "Control de balances y movimientos"),
            "miperfil" => ("Mi Perfil", "Gestiona tu informacion personal"),
            "centroayuda" => ("Centro de Ayuda", "Encuentra respuestas y contacta con soporte"),
            "cuentasbancarias" => ("Cuentas Bancarias", "Informacion de cuentas para transferencias"),
            _ => ("Ultimas Noticias", "Mantente informado con las ultimas novedades")
        };

        TxtModuleTitle.Text = title;
        TxtModuleDescription.Text = description;
        */
    }
}