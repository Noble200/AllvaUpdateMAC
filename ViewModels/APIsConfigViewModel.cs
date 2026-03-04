using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels.Admin
{
    public partial class APIsConfigViewModel : ObservableObject
    {
        private readonly ApiConfigurationService _apiConfigService;

        [ObservableProperty]
        private string _urlPrincipal = string.Empty;

        [ObservableProperty]
        private string _urlFallback = string.Empty;

        [ObservableProperty]
        private string _apiKey = string.Empty;

        [ObservableProperty]
        private bool _requiereApiKey;

        [ObservableProperty]
        private string _monedaBase = "eur";

        [ObservableProperty]
        private int _tiempoCacheMinutos = 10;

        [ObservableProperty]
        private bool _apiActiva = true;

        [ObservableProperty]
        private string _notasAdicionales = string.Empty;

        [ObservableProperty]
        private string _mensajeEstado = string.Empty;

        [ObservableProperty]
        private bool _mostrarMensajeExito;

        [ObservableProperty]
        private bool _mostrarMensajeError;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isTesting;

        [ObservableProperty]
        private string _resultadoPrueba = string.Empty;

        [ObservableProperty]
        private bool _pruebaExitosa;

        public APIsConfigViewModel()
        {
            _apiConfigService = new ApiConfigurationService();
            _ = LoadConfigurationAsync();
        }

        private async Task LoadConfigurationAsync()
        {
            IsLoading = true;
            try
            {
                var config = await _apiConfigService.GetDivisasConfigAsync();
                
                UrlPrincipal = config.UrlBase;
                UrlFallback = config.UrlFallback;
                ApiKey = config.ApiKey;
                RequiereApiKey = config.RequiereAutenticacion;
                MonedaBase = config.MonedaBase;
                TiempoCacheMinutos = config.TiempoCacheMinutos;
                ApiActiva = config.Activa;
                NotasAdicionales = config.NotasAdicionales ?? string.Empty;
            }
            catch (Exception ex)
            {
                MostrarError($"Error al cargar configuración: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GuardarConfiguracion()
        {
            IsLoading = true;
            OcultarMensajes();

            try
            {
                var config = new DivisasApiConfig
                {
                    Id = 1,
                    NombreApi = "API de Divisas",
                    TipoApi = "divisas",
                    UrlBase = UrlPrincipal,
                    UrlFallback = UrlFallback,
                    ApiKey = ApiKey,
                    RequiereAutenticacion = RequiereApiKey,
                    MonedaBase = MonedaBase,
                    TiempoCacheMinutos = TiempoCacheMinutos,
                    Activa = ApiActiva,
                    NotasAdicionales = string.IsNullOrWhiteSpace(NotasAdicionales) ? null : NotasAdicionales
                };

                var success = await _apiConfigService.SaveDivisasConfigAsync(config);

                if (success)
                {
                    MostrarExito("Configuración guardada correctamente");
                }
                else
                {
                    MostrarError("Error al guardar la configuración");
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task ProbarConexion()
        {
            if (string.IsNullOrWhiteSpace(UrlPrincipal))
            {
                MostrarError("Ingrese una URL para probar");
                return;
            }

            IsTesting = true;
            ResultadoPrueba = string.Empty;
            PruebaExitosa = false;

            try
            {
                var (success, message) = await _apiConfigService.TestApiConnectionAsync(UrlPrincipal);
                
                PruebaExitosa = success;
                ResultadoPrueba = message;

                if (success)
                {
                    MostrarExito("La API responde correctamente");
                }
                else
                {
                    MostrarError($"Error de conexión: {message}");
                }
            }
            catch (Exception ex)
            {
                PruebaExitosa = false;
                ResultadoPrueba = ex.Message;
                MostrarError($"Error: {ex.Message}");
            }
            finally
            {
                IsTesting = false;
            }
        }

        [RelayCommand]
        private async Task ProbarUrlFallback()
        {
            if (string.IsNullOrWhiteSpace(UrlFallback))
            {
                MostrarError("Ingrese una URL de fallback para probar");
                return;
            }

            IsTesting = true;
            ResultadoPrueba = string.Empty;

            try
            {
                var (success, message) = await _apiConfigService.TestApiConnectionAsync(UrlFallback);
                
                PruebaExitosa = success;
                ResultadoPrueba = $"Fallback: {message}";

                if (success)
                {
                    MostrarExito("La URL de fallback responde correctamente");
                }
                else
                {
                    MostrarError($"Error en fallback: {message}");
                }
            }
            catch (Exception ex)
            {
                PruebaExitosa = false;
                ResultadoPrueba = ex.Message;
                MostrarError($"Error: {ex.Message}");
            }
            finally
            {
                IsTesting = false;
            }
        }

        [RelayCommand]
        private async Task RestaurarValoresPorDefecto()
        {
            IsLoading = true;
            OcultarMensajes();

            try
            {
                var config = await _apiConfigService.ResetToDefaultAsync();
                
                UrlPrincipal = config.UrlBase;
                UrlFallback = config.UrlFallback;
                ApiKey = config.ApiKey;
                RequiereApiKey = config.RequiereAutenticacion;
                MonedaBase = config.MonedaBase;
                TiempoCacheMinutos = config.TiempoCacheMinutos;
                ApiActiva = config.Activa;
                NotasAdicionales = config.NotasAdicionales ?? string.Empty;

                MostrarExito("Valores restaurados a configuración por defecto");
            }
            catch (Exception ex)
            {
                MostrarError($"Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void MostrarExito(string mensaje)
        {
            MensajeEstado = mensaje;
            MostrarMensajeExito = true;
            MostrarMensajeError = false;
            
            _ = Task.Delay(3000).ContinueWith(_ => OcultarMensajes());
        }

        private void MostrarError(string mensaje)
        {
            MensajeEstado = mensaje;
            MostrarMensajeError = true;
            MostrarMensajeExito = false;
        }

        private void OcultarMensajes()
        {
            MostrarMensajeExito = false;
            MostrarMensajeError = false;
        }
    }
}