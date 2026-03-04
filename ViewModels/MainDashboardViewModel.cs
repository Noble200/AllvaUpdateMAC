using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Allva.Desktop.Views;
using Allva.Desktop.Views.PanelDivisas;
using Allva.Desktop.Views.PanelPackAlimentos;
using Allva.Desktop.Views.MenuHamburguesa;
using Allva.Desktop.Views.UserBoton;
using Allva.Desktop.ViewModels.UserBoton;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el Dashboard principal con menu de navegacion
/// Almacena los datos de sesion del usuario logueado
/// </summary>
public partial class MainDashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private UserControl? _currentView;

    [ObservableProperty]
    private string _userName = "Usuario";

    [ObservableProperty]
    private string _localCode = "CENTRAL";

    [ObservableProperty]
    private string _selectedModule = "dashboard";

    // ============================================
    // DATOS DE SESION DEL USUARIO
    // ============================================

    private int _idUsuario = 0;
    private int _idLocal = 0;
    private int _idComercio = 0;
    private string _numeroUsuario = "";

    public int IdUsuario => _idUsuario;
    public int IdLocal => _idLocal;
    public int IdComercio => _idComercio;
    public string NumeroUsuario => _numeroUsuario;

    // ============================================
    // PERMISOS DE MODULOS DEL LOCAL
    // ============================================

    [ObservableProperty]
    private bool _moduloDivisasVisible = false;

    [ObservableProperty]
    private bool _moduloAlimentosVisible = false;

    [ObservableProperty]
    private bool _moduloBilletesVisible = false;

    [ObservableProperty]
    private bool _moduloViajesVisible = false;

    public MainDashboardViewModel()
    {
        NavigateToModule("dashboard");
    }

    public MainDashboardViewModel(string userName, string localCode)
    {
        UserName = userName;
        LocalCode = localCode;
        NavigateToModule("dashboard");
    }
    
    public MainDashboardViewModel(int idUsuario, string nombreUsuario, string numeroUsuario,
                                   int idLocal, string codigoLocal, int idComercio,
                                   bool moduloDivisas = false, bool moduloAlimentos = false,
                                   bool moduloBilletes = false, bool moduloViajes = false)
    {
        _idUsuario = idUsuario;
        _idLocal = idLocal;
        _idComercio = idComercio;
        _numeroUsuario = numeroUsuario;

        UserName = nombreUsuario;
        LocalCode = codigoLocal;

        // Permisos de modulos del local
        ModuloDivisasVisible = moduloDivisas;
        ModuloAlimentosVisible = moduloAlimentos;
        ModuloBilletesVisible = moduloBilletes;
        ModuloViajesVisible = moduloViajes;

        NavigateToModule("dashboard");
    }

    public void SetSesionData(int idUsuario, string nombreUsuario, string numeroUsuario,
                               int idLocal, string codigoLocal, int idComercio)
    {
        _idUsuario = idUsuario;
        _idLocal = idLocal;
        _idComercio = idComercio;
        _numeroUsuario = numeroUsuario;
        
        UserName = nombreUsuario;
        LocalCode = codigoLocal;
    }

    public void NavigateToModule(string moduleName)
    {
        SelectedModule = moduleName;

        CurrentView = moduleName.ToLower() switch
        {
            "dashboard" or "ultimasnoticias" => CreateLatestNewsView(),
            "divisas" => CreateCurrencyExchangeView(),
            "alimentos" => CreateFoodPacksView(),
            "billetes" => CreateFlightTicketsView(),
            "viajes" => CreateTravelPacksView(),
            "administracion" => CreateAdministracionView(),
            "operaciones" => CreateOperacionesView(),
            "balancecuentas" => CreateBalanceCuentasView(),
            "miperfil" => CreateMiPerfilView(),
            "centroayuda" => CreateCentroAyudaView(),
            "cuentasbancarias" => CreateCuentasBancariasView(),
            _ => CreateLatestNewsView()
        };
    }

    public void Logout()
    {
        var navigationService = new NavigationService();
        navigationService.NavigateToLogin();
    }

    // ============================================
    // METODOS PRIVADOS PARA CREAR VISTAS
    // ============================================

    private UserControl CreateLatestNewsView()
    {
        var view = new LatestNewsView();
        view.DataContext = new LatestNewsViewModel();
        return view;
    }

    private UserControl CreateCurrencyExchangeView()
    {
        var view = new CurrencyExchangePanelView();
        var viewModel = new CurrencyExchangePanelViewModel(
            _idLocal, 
            _idComercio, 
            _idUsuario, 
            UserName, 
            _numeroUsuario
        );
        view.DataContext = viewModel;
        return view;
    }

    private UserControl CreateFoodPacksView()
    {
        var view = new FoodPacksView();
        var viewModel = new FoodPacksViewModel(
            _idComercio, 
            _idLocal, 
            _idUsuario, 
            UserName, 
            _numeroUsuario, 
            LocalCode
        );
        view.DataContext = viewModel;
        return view;
    }
    private UserControl CreateFlightTicketsView()
    {
        var view = new FlightTicketsView();
        view.DataContext = new FlightTicketsViewModel();
        return view;
    }

    private UserControl CreateTravelPacksView()
    {
        var view = new TravelPacksView();
        view.DataContext = new TravelPacksViewModel();
        return view;
    }

    private UserControl CreateOperacionesView()
    {
        var view = new OperacionesView(_idComercio, _idLocal, LocalCode, _idUsuario, UserName);
        return view;
    }
    
    private UserControl CreateBalanceCuentasView()
    {
        var view = new BalancedeCuentasView(_idComercio, _idLocal, LocalCode, _idUsuario, UserName);
        return view;
    }

    private UserControl CreateMiPerfilView()
    {
        var view = new MiPerfilView();
        var viewModel = new MiPerfilViewModel(
            _idUsuario,
            UserName,
            _numeroUsuario,
            _idLocal,
            LocalCode,
            _idComercio,
            () => NavigateToModule("dashboard")
        );
        view.DataContext = viewModel;
        return view;
    }

    private UserControl CreateCentroAyudaView()
    {
        var view = new CentroAyudaView();
        var viewModel = new CentroAyudaViewModel(() => NavigateToModule("dashboard"));
        view.DataContext = viewModel;
        return view;
    }

    private UserControl CreateCuentasBancariasView()
    {
        var view = new CuentasBancariasView();
        var viewModel = new CuentasBancariasViewModel(() => NavigateToModule("dashboard"));
        view.DataContext = viewModel;
        return view;
    }

    private UserControl CreateAdministracionView()
    {
        var view = new AdministracionView();
        var viewModel = new AdministracionViewModel(
            _idUsuario,
            _idLocal,
            _idComercio,
            UserName,
            LocalCode
        );
        view.DataContext = viewModel;
        return view;
    }
}