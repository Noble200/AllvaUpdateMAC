using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Allva.Desktop.Services;
using Allva.Desktop.ViewModels;
using Allva.Desktop.Views;
using Velopack;

namespace Allva.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // ============================================
            // VELOPACK: Debe ser lo PRIMERO
            // ============================================
            try
            {
                VelopackApp.Build().Run();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Velopack no disponible: {ex.Message}");
            }

            // Configurar servicios
            ConfigureServices();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var licenseService = new LicenseService();

                // VERIFICAR SI LA APP ESTÁ ACTIVADA
                if (!licenseService.EstaActivada())
                {
                    // NO ACTIVADA - Mostrar ventana de activación EN PANTALLA COMPLETA
                    var licenseView = new LicenseActivationView
                    {
                        DataContext = new LicenseActivationViewModel(),
                        WindowState = WindowState.Maximized
                    };

                    desktop.MainWindow = licenseView;
                }
                else
                {
                    // YA ACTIVADA - Ir directo al login EN PANTALLA COMPLETA
                    var loginViewModel = new LoginViewModel();
                    var loginView = new LoginView
                    {
                        DataContext = loginViewModel
                    };

                    var mainWindow = new Window
                    {
                        Title = "Allva System - Login",
                        WindowState = WindowState.Maximized,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        CanResize = true,
                        Content = loginView,
                        Icon = CargarIcono()
                    };

                    desktop.MainWindow = mainWindow;
                    
                    // Verificar actualizaciones INMEDIATAMENTE
                    Task.Run(() => CheckAndDownloadUpdates(loginViewModel));
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<LocalizationService>();
            services.AddSingleton<NavigationService>();
            services.AddSingleton<UpdateService>();
            services.AddSingleton<LicenseService>();

            services.AddTransient<LoginViewModel>();
            services.AddTransient<LicenseActivationViewModel>();

            Services = services.BuildServiceProvider();
        }

        private async Task CheckAndDownloadUpdates(LoginViewModel loginViewModel)
        {
            try
            {
                await Task.Delay(1000);

                var updateService = new UpdateService();
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    loginViewModel.ActivarMensajeActualizacion("Verificando actualizaciones...", 0);
                });

                var updateInfo = await updateService.CheckForUpdatesAsync();

                if (updateInfo != null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        loginViewModel.ActivarMensajeActualizacion(
                            $"Descargando actualización v{updateInfo.TargetFullRelease.Version}...", 
                            0
                        );
                    });

                    await updateService.DownloadUpdatesAsync(updateInfo, (progreso) =>
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            loginViewModel.ActivarMensajeActualizacion(
                                $"Descargando actualización v{updateInfo.TargetFullRelease.Version}...", 
                                progreso
                            );
                        });
                    });

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        loginViewModel.ActivarMensajeActualizacion(
                            "Actualización lista. Reiniciando aplicación...", 
                            100
                        );
                    });

                    await Task.Delay(2000);

                    updateService.ApplyUpdatesAndRestart(updateInfo);
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        loginViewModel.DesactivarMensajeActualizacion();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando actualizaciones: {ex.Message}");
                
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    loginViewModel.DesactivarMensajeActualizacion();
                });
            }
        }

        private WindowIcon? CargarIcono()
        {
            try
            {
                var uri = new Uri("avares://Allva.Desktop/Assets/allva-icon.ico");
                return new WindowIcon(AssetLoader.Open(uri));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se pudo cargar el icono: {ex.Message}");
                return null;
            }
        }
    }
}