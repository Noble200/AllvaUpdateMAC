using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Allva.Desktop.Models;

namespace Allva.Desktop.Services
{
    /// <summary>
    /// Servicio para obtener tasas de cambio de divisas en tiempo real
    /// ACTUALIZADO: Ahora usa la configuración guardada desde el panel de APIs
    /// </summary>
    public class CurrencyExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly ApiConfigurationService _configService;
        private Dictionary<string, decimal> _cachedRates;
        private DateTime _lastUpdate;
        private TimeSpan _cacheExpiration;
        
        private string _apiBaseUrl = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/eur.json";
        private string _apiFallbackUrl = "https://latest.currency-api.pages.dev/v1/currencies/eur.json";
        
        public CurrencyExchangeService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _cachedRates = new Dictionary<string, decimal>();
            _lastUpdate = DateTime.MinValue;
            _cacheExpiration = TimeSpan.FromMinutes(10);
            _configService = new ApiConfigurationService();
            
            _ = LoadConfigurationAsync();
        }
        
        private async Task LoadConfigurationAsync()
        {
            try
            {
                var config = await _configService.GetDivisasConfigAsync();
                
                if (!string.IsNullOrWhiteSpace(config.UrlBase))
                {
                    _apiBaseUrl = config.UrlBase;
                }
                
                if (!string.IsNullOrWhiteSpace(config.UrlFallback))
                {
                    _apiFallbackUrl = config.UrlFallback;
                }
                
                _cacheExpiration = TimeSpan.FromMinutes(config.TiempoCacheMinutos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar configuración de API de divisas: {ex.Message}");
            }
        }
        
        public async Task ReloadConfigurationAsync()
        {
            await LoadConfigurationAsync();
            _cachedRates.Clear();
            _lastUpdate = DateTime.MinValue;
        }
        
        public async Task<Dictionary<string, decimal>> GetExchangeRatesAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiBaseUrl))
            {
                await LoadConfigurationAsync();
            }
            
            if (_cachedRates.Count > 0 && DateTime.Now - _lastUpdate < _cacheExpiration)
            {
                return _cachedRates;
            }
            
            try
            {
                var response = await _httpClient.GetStringAsync(_apiBaseUrl);
                var rates = ParseApiResponse(response);
                
                if (rates.Count > 0)
                {
                    _cachedRates = rates;
                    _lastUpdate = DateTime.Now;
                    return rates;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error con API principal: {ex.Message}");
                
                try
                {
                    var fallbackResponse = await _httpClient.GetStringAsync(_apiFallbackUrl);
                    var rates = ParseApiResponse(fallbackResponse);
                    
                    if (rates.Count > 0)
                    {
                        _cachedRates = rates;
                        _lastUpdate = DateTime.Now;
                        return rates;
                    }
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"Error con API de fallback: {fallbackEx.Message}");
                }
            }
            
            return _cachedRates.Count > 0 ? _cachedRates : GetDefaultRates();
        }
        
        /// <summary>
        /// Obtiene las divisas favoritas (método requerido por CurrencyExchangePanelViewModel)
        /// </summary>
        public async Task<List<FavoriteCurrencyModel>> GetFavoriteCurrenciesAsync(List<string> favoriteCodes)
        {
            var rates = await GetExchangeRatesAsync();
            var result = new List<FavoriteCurrencyModel>();
            
            foreach (var code in favoriteCodes)
            {
                var currency = AvailableCurrencies.GetAllCurrencies()
                    .Find(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
                    
                if (currency != null)
                {
                    var favorite = new FavoriteCurrencyModel
                    {
                        Code = currency.Code,
                        Name = currency.Name,
                        Country = currency.Country
                    };
                    
                    if (rates.TryGetValue(code, out var rate))
                    {
                        favorite.RateToEur = rate;
                    }
                    
                    result.Add(favorite);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Refresca las tasas de cambio (método requerido por CurrencyExchangePanelViewModel)
        /// </summary>
        public async Task<Dictionary<string, decimal>> RefreshRatesAsync()
        {
            _cachedRates.Clear();
            _lastUpdate = DateTime.MinValue;
            return await GetExchangeRatesAsync();
        }
        
        private Dictionary<string, decimal> ParseApiResponse(string json)
        {
            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("eur", out var eurRates))
                {
                    foreach (var rate in eurRates.EnumerateObject())
                    {
                        if (rate.Value.TryGetDecimal(out var value) && value > 0)
                        {
                            rates[rate.Name.ToUpper()] = 1m / value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al parsear respuesta de API: {ex.Message}");
            }
            
            return rates;
        }
        
        private Dictionary<string, decimal> GetDefaultRates()
        {
            // Devolver vacío para que se muestre error en lugar de tasas falsas
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }
        
        public async Task<decimal> ConvertToEuroAsync(string fromCurrency, decimal amount)
        {
            var rates = await GetExchangeRatesAsync();
            
            if (rates.TryGetValue(fromCurrency.ToUpper(), out var rate) && rate > 0)
            {
                return amount / rate;
            }
            
            return amount;
        }
        
        public async Task<decimal> GetRateAsync(string currency)
        {
            var rates = await GetExchangeRatesAsync();
            
            if (rates.TryGetValue(currency.ToUpper(), out var rate))
            {
                return rate;
            }
            
            return 1m;
        }
        
        public DateTime GetLastUpdateTime()
        {
            return _lastUpdate;
        }
        
        public bool IsUsingCache()
        {
            return _cachedRates.Count > 0 && DateTime.Now - _lastUpdate < _cacheExpiration;
        }
    }
}