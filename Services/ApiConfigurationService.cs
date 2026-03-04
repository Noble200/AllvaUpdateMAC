using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Allva.Desktop.Models;

namespace Allva.Desktop.Services
{
    /// <summary>
    /// Servicio para gestionar la configuración de APIs externas
    /// </summary>
    public class ApiConfigurationService
    {
        private readonly string _configFilePath;
        private DivisasApiConfig? _divisasConfig;

        public ApiConfigurationService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var allvaPath = Path.Combine(appDataPath, "Allva");
            
            if (!Directory.Exists(allvaPath))
            {
                Directory.CreateDirectory(allvaPath);
            }
            
            _configFilePath = Path.Combine(allvaPath, "api_config.json");
        }

        public async Task<DivisasApiConfig> GetDivisasConfigAsync()
        {
            if (_divisasConfig != null)
            {
                return _divisasConfig;
            }

            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = await File.ReadAllTextAsync(_configFilePath);
                    var config = JsonSerializer.Deserialize<ApiConfigWrapper>(json);
                    
                    if (config?.DivisasApi != null)
                    {
                        _divisasConfig = config.DivisasApi;
                        return _divisasConfig;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar configuración de APIs: {ex.Message}");
            }

            _divisasConfig = GetDefaultDivisasConfig();
            return _divisasConfig;
        }

        public async Task<bool> SaveDivisasConfigAsync(DivisasApiConfig config)
        {
            try
            {
                config.FechaModificacion = DateTime.Now;
                
                var wrapper = new ApiConfigWrapper
                {
                    DivisasApi = config
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(wrapper, options);
                await File.WriteAllTextAsync(_configFilePath, json);
                
                _divisasConfig = config;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar configuración de APIs: {ex.Message}");
                return false;
            }
        }

        public async Task<DivisasApiConfig> ResetToDefaultAsync()
        {
            _divisasConfig = GetDefaultDivisasConfig();
            await SaveDivisasConfigAsync(_divisasConfig);
            return _divisasConfig;
        }

        public async Task<(bool success, string message)> TestApiConnectionAsync(string url)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Conexión exitosa");
                }
                else
                {
                    return (false, $"Error HTTP: {(int)response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (TaskCanceledException)
            {
                return (false, "Tiempo de espera agotado");
            }
            catch (Exception ex)
            {
                return (false, $"Error: {ex.Message}");
            }
        }

        private DivisasApiConfig GetDefaultDivisasConfig()
        {
            return new DivisasApiConfig
            {
                Id = 1,
                NombreApi = "Currency API (Fawaz Ahmed)",
                TipoApi = "divisas",
                UrlBase = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/eur.json",
                UrlFallback = "https://latest.currency-api.pages.dev/v1/currencies/eur.json",
                RequiereAutenticacion = false,
                ApiKey = string.Empty,
                Activa = true,
                MonedaBase = "eur",
                TiempoCacheMinutos = 10,
                FechaCreacion = DateTime.Now,
                NotasAdicionales = "API gratuita sin necesidad de API Key"
            };
        }
    }

    internal class ApiConfigWrapper
    {
        public DivisasApiConfig? DivisasApi { get; set; }
    }
}