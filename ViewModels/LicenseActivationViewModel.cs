using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Services;
using Allva.Desktop.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Allva.Desktop.ViewModels;

public partial class LicenseActivationViewModel : ObservableObject
{
    private readonly LicenseService _licenseService = new();

    [ObservableProperty]
    private string _codigoLicencia = string.Empty;

    [ObservableProperty]
    private bool _validando;

    [ObservableProperty]
    private bool _mostrarMensaje;

    [ObservableProperty]
    private string _mensaje = string.Empty;

    [ObservableProperty]
    private string _colorMensaje = "#dc3545";

    [RelayCommand]
    private async Task Activar()
    {
        if (string.IsNullOrWhiteSpace(CodigoLicencia))
        {
            MostrarError("Por favor ingrese un código de licencia");
            return;
        }

        Validando = true;
        MostrarMensaje = false;

        await Task.Delay(500);

        var (isValid, message) = await _licenseService.ValidarYUsarLicenciaAsync(CodigoLicencia);

        Validando = false;

        if (isValid)
        {
            MostrarExito(message);
            await Task.Delay(1500);
            AbrirLoginYCerrar();
        }
        else
        {
            MostrarError(message);
        }
    }

    [RelayCommand]
    private void Salir()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void MostrarError(string mensaje)
    {
        Mensaje = mensaje;
        ColorMensaje = "#dc3545";
        MostrarMensaje = true;
    }

    private void MostrarExito(string mensaje)
    {
        Mensaje = mensaje;
        ColorMensaje = "#28a745";
        MostrarMensaje = true;
    }

    private void AbrirLoginYCerrar()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var loginView = new LoginView
            {
                DataContext = new LoginViewModel()
            };

            var mainWindow = new Window
            {
                Title = "Allva System - Login",
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                CanResize = true,
                Content = loginView
            };

            desktop.MainWindow?.Close();
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
        }
    }
}