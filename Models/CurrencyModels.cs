using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Allva.Desktop.Models
{
    /// <summary>
    /// Modelo que representa una divisa con su información completa
    /// </summary>
    public partial class CurrencyModel : ObservableObject
    {
        [ObservableProperty]
        private string _code = string.Empty;
        
        [ObservableProperty]
        private string _name = string.Empty;
        
        [ObservableProperty]
        private string _country = string.Empty;
        
        [ObservableProperty]
        private decimal _rateToEur;
        
        /// <summary>
        /// Tasa con margen aplicado (INTERNO - el usuario ve esta tasa sin saber que tiene margen)
        /// </summary>
        [ObservableProperty]
        private decimal _rateWithMargin;
        
        [ObservableProperty]
        private bool _isFavorite;
        
        [ObservableProperty]
        private string _displayText = string.Empty;
        
        public string FullDisplay => $"{Country}, {Name}";
        
        /// <summary>
        /// Muestra la tasa CON margen (el usuario no sabe que tiene margen)
        /// </summary>
        public string RateDisplay => RateWithMargin > 0 
            ? $"1 {Code} = {RateWithMargin:F2} EUR" 
            : "Error al cargar tasa";
    }
    
    /// <summary>
    /// Modelo para las divisas favoritas/preferidas que se muestran en el panel principal
    /// </summary>
    public partial class FavoriteCurrencyModel : ObservableObject
    {
        [ObservableProperty]
        private string _code = string.Empty;
        
        [ObservableProperty]
        private string _name = string.Empty;
        
        [ObservableProperty]
        private string _country = string.Empty;
        
        [ObservableProperty]
        private decimal _rateToEur;
        
        /// <summary>
        /// Tasa con margen aplicado (INTERNO - el usuario ve esta tasa sin saber que tiene margen)
        /// </summary>
        [ObservableProperty]
        private decimal _rateWithMargin;
        
        public string FullDisplay => $"{Country}, {Name}";
        
        /// <summary>
        /// Muestra la tasa CON margen (el usuario no sabe que tiene margen)
        /// </summary>
        public string RateDisplay => $"1 {Code} = {RateWithMargin:F2} EUR";
    }
    
    /// <summary>
    /// Modelo para el resultado de una transacción de cambio de divisas
    /// </summary>
    public class TransactionResult
    {
        public string CurrencyCode { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public decimal AmountReceived { get; set; }
        public decimal AmountInEuros { get; set; }
        public decimal ExchangeRate { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionTime { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Lista de divisas disponibles con información predefinida
    /// </summary>
    public static class AvailableCurrencies
    {
        public static List<CurrencyModel> GetAllCurrencies()
        {
            return new List<CurrencyModel>
            {
                new CurrencyModel { Code = "USD", Name = "Dólar USA", Country = "Estados Unidos" },
                new CurrencyModel { Code = "CAD", Name = "Dólar Canadiense", Country = "Canadá" },
                new CurrencyModel { Code = "CHF", Name = "Franco Suizo", Country = "Suiza" },
                new CurrencyModel { Code = "GBP", Name = "Libra Esterlina", Country = "Reino Unido" },
                new CurrencyModel { Code = "CNY", Name = "Yuan Chino", Country = "China" },
                new CurrencyModel { Code = "JPY", Name = "Yen Japonés", Country = "Japón" },
                new CurrencyModel { Code = "AUD", Name = "Dólar Australiano", Country = "Australia" },
                new CurrencyModel { Code = "MXN", Name = "Peso Mexicano", Country = "México" },
                new CurrencyModel { Code = "BRL", Name = "Real Brasileño", Country = "Brasil" },
                new CurrencyModel { Code = "ARS", Name = "Peso Argentino", Country = "Argentina" },
                new CurrencyModel { Code = "CLP", Name = "Peso Chileno", Country = "Chile" },
                new CurrencyModel { Code = "COP", Name = "Peso Colombiano", Country = "Colombia" },
                new CurrencyModel { Code = "PEN", Name = "Sol Peruano", Country = "Perú" },
                new CurrencyModel { Code = "BOB", Name = "Boliviano", Country = "Bolivia" },
                new CurrencyModel { Code = "VES", Name = "Bolívar", Country = "Venezuela" },
                new CurrencyModel { Code = "DOP", Name = "Peso Dominicano", Country = "Rep. Dominicana" },
                new CurrencyModel { Code = "UYU", Name = "Peso Uruguayo", Country = "Uruguay" },
                new CurrencyModel { Code = "INR", Name = "Rupia India", Country = "India" },
                new CurrencyModel { Code = "KRW", Name = "Won Surcoreano", Country = "Corea del Sur" },
                new CurrencyModel { Code = "SGD", Name = "Dólar Singapur", Country = "Singapur" },
                new CurrencyModel { Code = "HKD", Name = "Dólar Hong Kong", Country = "Hong Kong" },
                new CurrencyModel { Code = "NZD", Name = "Dólar Neozelandés", Country = "Nueva Zelanda" },
                new CurrencyModel { Code = "SEK", Name = "Corona Sueca", Country = "Suecia" },
                new CurrencyModel { Code = "NOK", Name = "Corona Noruega", Country = "Noruega" },
                new CurrencyModel { Code = "DKK", Name = "Corona Danesa", Country = "Dinamarca" },
                new CurrencyModel { Code = "PLN", Name = "Zloty Polaco", Country = "Polonia" },
                new CurrencyModel { Code = "CZK", Name = "Corona Checa", Country = "República Checa" },
                new CurrencyModel { Code = "HUF", Name = "Florín Húngaro", Country = "Hungría" },
                new CurrencyModel { Code = "RON", Name = "Leu Rumano", Country = "Rumania" },
                new CurrencyModel { Code = "TRY", Name = "Lira Turca", Country = "Turquía" },
                new CurrencyModel { Code = "ZAR", Name = "Rand Sudafricano", Country = "Sudáfrica" },
                new CurrencyModel { Code = "THB", Name = "Baht Tailandés", Country = "Tailandia" },
                new CurrencyModel { Code = "MYR", Name = "Ringgit Malayo", Country = "Malasia" },
                new CurrencyModel { Code = "PHP", Name = "Peso Filipino", Country = "Filipinas" },
                new CurrencyModel { Code = "IDR", Name = "Rupia Indonesia", Country = "Indonesia" },
                new CurrencyModel { Code = "AED", Name = "Dirham EAU", Country = "Emiratos Árabes" },
                new CurrencyModel { Code = "SAR", Name = "Riyal Saudí", Country = "Arabia Saudita" },
                new CurrencyModel { Code = "ILS", Name = "Shekel Israelí", Country = "Israel" },
                new CurrencyModel { Code = "EGP", Name = "Libra Egipcia", Country = "Egipto" },
                new CurrencyModel { Code = "MAD", Name = "Dirham Marroquí", Country = "Marruecos" }
            };
        }
        
        public static List<string> GetDefaultFavorites()
        {
            return new List<string> { "USD", "CAD", "CHF", "GBP", "CNY" };
        }
    }
}