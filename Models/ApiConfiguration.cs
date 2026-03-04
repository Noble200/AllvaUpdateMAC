using System;

namespace Allva.Desktop.Models
{
    /// <summary>
    /// Modelo para almacenar la configuración de APIs externas
    /// </summary>
    public class ApiConfiguration
    {
        public int Id { get; set; }
        public string NombreApi { get; set; } = string.Empty;
        public string TipoApi { get; set; } = string.Empty;
        public string UrlBase { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string UrlFallback { get; set; } = string.Empty;
        public bool RequiereAutenticacion { get; set; }
        public bool Activa { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
        public string? NotasAdicionales { get; set; }
    }

    /// <summary>
    /// Configuración específica para la API de divisas
    /// </summary>
    public class DivisasApiConfig : ApiConfiguration
    {
        public string MonedaBase { get; set; } = "eur";
        public int TiempoCacheMinutos { get; set; } = 10;
        
        public DivisasApiConfig()
        {
            TipoApi = "divisas";
            NombreApi = "API de Divisas";
        }
    }
}