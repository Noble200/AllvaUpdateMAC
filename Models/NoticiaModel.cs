using System;
using Avalonia.Media;

namespace Allva.Desktop.Models
{
    /// <summary>
    /// Modelo para las noticias del sistema
    /// </summary>
    public class NoticiaModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
        public string ImagenUrl { get; set; } = string.Empty;
        public byte[]? ImagenData { get; set; }

        // Campos para el recorte/encuadre de imagen
        public double ImagenCropX { get; set; } = 0;
        public double ImagenCropY { get; set; } = 0;
        public double ImagenCropWidth { get; set; } = 0;
        public double ImagenCropHeight { get; set; } = 0;

        public string Estado { get; set; } = "Activa";
        public bool EsDestacada { get; set; } = false;
        public int Orden { get; set; } = 1;
        public DateTime FechaPublicacion { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Propiedades calculadas
        public string FechaCreacionTexto => FechaCreacion.ToString("dd/MM/yyyy");
        public string FechaPublicacionTexto => FechaPublicacion.ToString("dd MMM yyyy");

        public string EstadoTexto => Estado;

        public IBrush EstadoBackground => Estado switch
        {
            "Activa" => new SolidColorBrush(Color.Parse("#D4EDDA")),
            "Inactiva" => new SolidColorBrush(Color.Parse("#F8D7DA")),
            "Borrador" => new SolidColorBrush(Color.Parse("#FFF3CD")),
            _ => new SolidColorBrush(Color.Parse("#E2E3E5"))
        };

        public IBrush EstadoForeground => Estado switch
        {
            "Activa" => new SolidColorBrush(Color.Parse("#155724")),
            "Inactiva" => new SolidColorBrush(Color.Parse("#721C24")),
            "Borrador" => new SolidColorBrush(Color.Parse("#856404")),
            _ => new SolidColorBrush(Color.Parse("#383D41"))
        };

        public string DescripcionCorta
        {
            get
            {
                if (string.IsNullOrEmpty(Descripcion))
                    return string.Empty;

                return Descripcion.Length > 100
                    ? Descripcion.Substring(0, 100) + "..."
                    : Descripcion;
            }
        }

        public bool TieneImagen => !string.IsNullOrEmpty(ImagenUrl) || ImagenData != null;
    }
}
