using System;

namespace Allva.Desktop.Models.Admin;

/// <summary>
/// Modelo para la configuración de margen de divisas
/// Soporta configuración global, por comercio y por local
/// </summary>
public class ConfiguracionDivisaModel
{
    public int Id { get; set; }
    public string Tipo { get; set; } = "GLOBAL"; // GLOBAL, COMERCIO, LOCAL
    public int? IdComercio { get; set; }
    public int? IdLocal { get; set; }
    public string NombreComercio { get; set; } = string.Empty;
    public string NombreLocal { get; set; } = string.Empty;
    public string CodigoLocal { get; set; } = string.Empty;
    public decimal MargenPorcentaje { get; set; } = 10.00m;
    public bool UsaMargenPropio { get; set; } = false;
    public DateTime FechaModificacion { get; set; } = DateTime.Now;
    public string ModificadoPor { get; set; } = string.Empty;
    
    /// <summary>
    /// Texto descriptivo del margen
    /// </summary>
    public string MargenTexto => $"{MargenPorcentaje:N2}%";
    
    /// <summary>
    /// Indica si hereda el margen del nivel superior
    /// </summary>
    public string EstadoMargen => UsaMargenPropio ? "Personalizado" : "Heredado";
}

/// <summary>
/// Modelo para comercio con su configuración de divisa
/// </summary>
public class ComercioConDivisaModel
{
    public int IdComercio { get; set; }
    public string NombreComercio { get; set; } = string.Empty;
    public string NombreSrl { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public decimal MargenPorcentaje { get; set; } = 10.00m;
    public int CantidadLocales { get; set; }
    
    public string MargenTexto => $"{MargenPorcentaje:N2}%";
    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
}

/// <summary>
/// Modelo para local con su configuración de divisa
/// </summary>
public class LocalConDivisaModel
{
    public int IdLocal { get; set; }
    public int IdComercio { get; set; }
    public string CodigoLocal { get; set; } = string.Empty;
    public string NombreLocal { get; set; } = string.Empty;
    public string NombreComercio { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public decimal ComisionDivisas { get; set; } = 0.00m;
    public bool UsaComisionPropia { get; set; } = false;
    public decimal MargenEfectivo { get; set; } = 10.00m; // El margen que realmente aplica
    public bool ModuloDivisas { get; set; } = false; // Indica si el local tiene el módulo de divisas activo

    public string MargenTexto => $"{MargenEfectivo:N2}%";
    public string EstadoMargen { get; set; } = "Global";
    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
}