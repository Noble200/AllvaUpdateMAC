using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Allva.Desktop.Models
{
    /// <summary>
    /// Representa un pack de alimentos
    /// </summary>
    public class PackAlimento
    {
        public int IdPack { get; set; }
        public string NombrePack { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public byte[]? ImagenPoster { get; set; }
        public string? ImagenPosterNombre { get; set; }
        public string? ImagenPosterTipo { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? CreadoPor { get; set; }
        public int? ModificadoPor { get; set; }

        // Propiedades de Pais Designado
        public int? IdPais { get; set; }
        public string? NombrePais { get; set; }
        public byte[]? BanderaPais { get; set; }

        // Propiedades de Precio
        public decimal PrecioPack { get; set; }
        public string DivisaPack { get; set; } = "EUR";

        // Colecciones relacionadas
        public ObservableCollection<PackAlimentoProducto> Productos { get; set; } = new();
        public ObservableCollection<PackAlimentoImagen> Imagenes { get; set; } = new();
        public ObservableCollection<PackAlimentoPrecio> Precios { get; set; } = new();

        // Propiedades de visualizacion
        public string EstadoTexto => Activo ? "Activo" : "Inactivo";
        public int CantidadProductos => Productos?.Count ?? 0;
        public string ResumenProductos => $"{CantidadProductos} producto(s)";
        
        // Propiedades de visualizacion de Pais
        public bool TienePais => IdPais.HasValue && !string.IsNullOrEmpty(NombrePais);
        public bool TieneBandera => BanderaPais != null && BanderaPais.Length > 0;
        public string PaisTexto => NombrePais ?? "Sin pais designado";
        
        // Propiedades de visualizacion de Precio
        public string PrecioFormateado => PrecioPack > 0 
            ? $"{PrecioPack:N2} {DivisaPack}" 
            : "Sin precio";
        
        // Fecha formateada
        public string FechaCreacionTexto => FechaCreacion.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// Producto individual dentro de un pack
    /// </summary>
    public class PackAlimentoProducto
    {
        public int IdProducto { get; set; }
        public int IdPack { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Detalles { get; set; }
        public decimal Cantidad { get; set; } = 1;
        public string UnidadMedida { get; set; } = "unidad";
        public int Orden { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Imagen del producto
        public byte[]? Imagen { get; set; }
        public string? ImagenNombre { get; set; }
        public string? ImagenTipo { get; set; }

        // Propiedades de visualizacion
        public string CantidadConUnidad => Cantidad == Math.Floor(Cantidad)
            ? $"{Cantidad:0} {UnidadMedida}"
            : $"{Cantidad:0.##} {UnidadMedida}";
        public bool TieneImagen => Imagen != null && Imagen.Length > 0;
        public bool TieneDetalles => !string.IsNullOrWhiteSpace(Detalles);
        public string ResumenDetalles => TieneDetalles 
            ? (Detalles!.Length > 50 ? Detalles.Substring(0, 50) + "..." : Detalles) 
            : "Sin detalles";
    }

    /// <summary>
    /// Imagen adicional del pack
    /// </summary>
    public class PackAlimentoImagen
    {
        public int IdImagen { get; set; }
        public int IdPack { get; set; }
        public byte[] ImagenContenido { get; set; } = Array.Empty<byte>();
        public string? ImagenNombre { get; set; }
        public string? ImagenTipo { get; set; }
        public string? Descripcion { get; set; }
        public int Orden { get; set; }
        public DateTime FechaSubida { get; set; }
    }

    /// <summary>
    /// Precio del pack en una divisa especifica
    /// </summary>
    public class PackAlimentoPrecio
    {
        public int IdPrecio { get; set; }
        public int IdPack { get; set; }
        public string Divisa { get; set; } = "EUR";
        public decimal Precio { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public string PrecioFormateado => Divisa switch
        {
            "EUR" => $"{Precio:N2} EUR",
            "USD" => $"{Precio:N2} USD",
            _ => $"{Precio:N2} {Divisa}"
        };

        public string SimboloDivisa => Divisa switch
        {
            "EUR" => "EUR",
            "USD" => "$",
            _ => Divisa
        };
    }

    public enum TipoAsignacionPack
    {
        Global,
        Comercio,
        Local
    }

    public class PackAlimentoAsignacion
    {
        public int IdAsignacion { get; set; }
        public int IdPack { get; set; }
        public TipoAsignacionPack TipoAsignacion { get; set; }
        public int? IdComercio { get; set; }
        public int? IdLocal { get; set; }
        public int IdPrecio { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaAsignacion { get; set; }
        public int? AsignadoPor { get; set; }

        public string? NombreComercio { get; set; }
        public string? NombreLocal { get; set; }
        public string? CodigoLocal { get; set; }
        public PackAlimentoPrecio? Precio { get; set; }

        public string DestinoTexto => TipoAsignacion switch
        {
            TipoAsignacionPack.Global => "Todos los comercios",
            TipoAsignacionPack.Comercio => NombreComercio ?? "Comercio",
            TipoAsignacionPack.Local => $"{NombreLocal} ({CodigoLocal})" ?? "Local",
            _ => "Desconocido"
        };
    }

    /// <summary>
    /// Categoría de packs de alimentos (pertenece a un país)
    /// </summary>
    public class CategoriaPackAlimento
    {
        public int IdCategoria { get; set; }
        public int IdPais { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public byte[]? ImagenCategoria { get; set; }
        public string? ImagenCategoriaNombre { get; set; }
        public DateTime FechaCreacion { get; set; }

        public bool TieneImagen => ImagenCategoria != null && ImagenCategoria.Length > 0;
        public string FechaCreacionTexto => FechaCreacion.ToString("dd/MM/yyyy");
        public int CantidadPacks { get; set; }
        public string TextoCantidadPacks => CantidadPacks == 1 ? "1 pack" : $"{CantidadPacks} packs";
    }
}