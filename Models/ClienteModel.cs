using System;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;

namespace Allva.Desktop.Models;

/// <summary>
/// Modelo de Cliente para operaciones de divisas
/// ACTUALIZADO: Agregado IdComercioRegistro e IdUsuarioRegistro
/// Los clientes pertenecen a un comercio (compartidos entre locales del mismo comercio)
/// </summary>
public class ClienteModel
{
    public int IdCliente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string SegundoNombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string SegundoApellido { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Nacionalidad { get; set; } = string.Empty;
    public string TipoDocumento { get; set; } = "DNI"; // DNI, NIE, Pasaporte

    public string? TipoResidencia { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public DateTime? CaducidadDocumento { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    
    // Imágenes del documento (frontal y trasera)
    public byte[]? ImagenDocumentoFrontal { get; set; }
    public byte[]? ImagenDocumentoTrasera { get; set; }
    
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    
    // Registro: comercio, local y usuario que creó el cliente
    public int? IdComercioRegistro { get; set; }
    public int? IdLocalRegistro { get; set; }
    public int? IdUsuarioRegistro { get; set; }
    
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
    public string CaducidadTexto => CaducidadDocumento?.ToString("dd/MM/yyyy") ?? "Sin fecha";
    public string FechaNacimientoTexto => FechaNacimiento?.ToString("dd/MM/yyyy") ?? "";
    
    // Propiedades para imágenes
    public bool TieneImagenFrontal => ImagenDocumentoFrontal != null && ImagenDocumentoFrontal.Length > 0;
    public bool TieneImagenTrasera => ImagenDocumentoTrasera != null && ImagenDocumentoTrasera.Length > 0;
    public bool TieneImagenes => TieneImagenFrontal || TieneImagenTrasera;
    
    // Bitmaps para mostrar en UI
    private Bitmap? _imagenFrontalBitmap;
    private Bitmap? _imagenTraseraBitmap;
    
    public Bitmap? ImagenFrontalBitmap
    {
        get
        {
            if (_imagenFrontalBitmap == null && TieneImagenFrontal)
            {
                try
                {
                    using var stream = new MemoryStream(ImagenDocumentoFrontal!);
                    _imagenFrontalBitmap = new Bitmap(stream);
                }
                catch { _imagenFrontalBitmap = null; }
            }
            return _imagenFrontalBitmap;
        }
    }
    
    public Bitmap? ImagenTraseraBitmap
    {
        get
        {
            if (_imagenTraseraBitmap == null && TieneImagenTrasera)
            {
                try
                {
                    using var stream = new MemoryStream(ImagenDocumentoTrasera!);
                    _imagenTraseraBitmap = new Bitmap(stream);
                }
                catch { _imagenTraseraBitmap = null; }
            }
            return _imagenTraseraBitmap;
        }
    }
}

/// <summary>
/// Modelo de Divisa para mostrar en la lista
/// </summary>
public class DivisaModel
{
    public string Codigo { get; set; } = string.Empty; // USD, EUR, GBP
    public string Nombre { get; set; } = string.Empty; // Dólar USA, Euro, Libra
    public string Pais { get; set; } = string.Empty;
    public decimal TasaCambioOriginal { get; set; } // Tasa real del mercado
    public decimal TasaCambioConMargen { get; set; } // Tasa con margen aplicado (OCULTA al usuario)
    public bool EsFavorita { get; set; } = false;
    public DateTime UltimaActualizacion { get; set; } = DateTime.Now;
    
    // Solo mostrar la tasa CON margen aplicado (el usuario no ve la original)
    public string TasaTexto => $"1 {Codigo} = {TasaCambioConMargen:N4} EUR";
    public string NombreCompleto => $"{Pais}, {Nombre}";
}

/// <summary>
/// Modelo para el resultado de conversión
/// </summary>
public class ResultadoConversionModel
{
    public string DivisaOrigen { get; set; } = string.Empty;
    public string DivisaDestino { get; set; } = "EUR";
    public decimal CantidadOrigen { get; set; }
    public decimal CantidadDestino { get; set; }
    public decimal TasaAplicada { get; set; } // Tasa con margen (la que ve el usuario)
    
    // Campos internos (NUNCA mostrar al usuario)
    internal decimal TasaReal { get; set; }
    internal decimal MargenPorcentaje { get; set; }
    internal decimal BeneficioAllva { get; set; }
}