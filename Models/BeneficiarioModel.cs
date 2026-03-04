using System;
using System.Collections.Generic;
using System.Linq;

namespace Allva.Desktop.Models;

/// <summary>
/// Modelo de Beneficiario para Pack de Alimentos
/// Los beneficiarios estan asociados a un cliente y comercio especifico
/// Los datos NO se comparten entre comercios
/// </summary>
public class BeneficiarioModel
{
    public int IdBeneficiario { get; set; }
    public int IdCliente { get; set; }
    public int IdComercio { get; set; }
    public int? IdLocalRegistro { get; set; }
    
    // Datos personales
    public string Nombre { get; set; } = string.Empty;
    public string SegundoNombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string SegundoApellido { get; set; } = string.Empty;
    
    // Documentacion
    public string TipoDocumento { get; set; } = "DNI";
    public string NumeroDocumento { get; set; } = string.Empty;
    
    // Contacto
    public string Telefono { get; set; } = string.Empty;
    
    // Direccion de envio
    public string Pais { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Calle { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Piso { get; set; } = string.Empty;
    public string NumeroDepartamento { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    
    // Metadata
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime FechaModificacion { get; set; } = DateTime.Now;
    
    // Propiedades calculadas
    public string NombreCompleto
    {
        get
        {
            var partes = new[] { Nombre, SegundoNombre, Apellido, SegundoApellido };
            return string.Join(" ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
    
    public string DocumentoCompleto => $"{TipoDocumento}: {NumeroDocumento}";
    
    public string DireccionCompleta
    {
        get
        {
            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(Calle))
                partes.Add(Calle);
            if (!string.IsNullOrWhiteSpace(Numero))
                partes.Add($"No. {Numero}");
            if (!string.IsNullOrWhiteSpace(Piso))
                partes.Add($"Piso {Piso}");
            if (!string.IsNullOrWhiteSpace(NumeroDepartamento))
                partes.Add($"Depto. {NumeroDepartamento}");
            if (!string.IsNullOrWhiteSpace(CodigoPostal))
                partes.Add($"CP {CodigoPostal}");

            return string.Join(", ", partes);
        }
    }
    
    public string ResumenCorto => $"{NombreCompleto} - {Pais}, {Ciudad}";
}