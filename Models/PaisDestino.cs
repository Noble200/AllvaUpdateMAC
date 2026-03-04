using System;

namespace Allva.Desktop.Models
{
    /// <summary>
    /// Representa un pais designado para packs de alimentos
    /// </summary>
    public class PaisDesignado
    {
        public int IdPais { get; set; }
        public string NombrePais { get; set; } = string.Empty;
        public string? CodigoIso { get; set; }
        public byte[]? BanderaImagen { get; set; }
        public string? BanderaNombre { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        // Propiedades de visualizacion
        public bool TieneBandera => BanderaImagen != null && BanderaImagen.Length > 0;
        public string NombreConCodigo => !string.IsNullOrEmpty(CodigoIso)
            ? $"{NombrePais} ({CodigoIso})"
            : NombrePais;

        // Cantidad de packs en este pais
        public int CantidadPacks { get; set; }
        public string TextoCantidadPacks => CantidadPacks == 1 ? "1 pack" : $"{CantidadPacks} packs";
    }
}