using System;

namespace Allva.Desktop.Models;

public class LicenseModel
{
    public int IdLicencia { get; set; }
    public string CodigoLicencia { get; set; } = string.Empty;
    public string? NombreCliente { get; set; }
    public string? EmailCliente { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime? FechaExpiracion { get; set; }
    public bool Activa { get; set; }
    public bool Usada { get; set; }
    public DateTime? FechaActivacion { get; set; }
    public string? IdMaquina { get; set; }
    public int? IdComercio { get; set; }
    public string? Observaciones { get; set; }
    public string? CreadoPor { get; set; }
    public DateTime FechaCreacion { get; set; }
}