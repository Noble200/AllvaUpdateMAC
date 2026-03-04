using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Allva.Desktop.Services;

/// <summary>
/// Servicio para generar PDF con el catalogo de packs de alimentos por pais
/// Incluye fotos de productos, detalles y precios
/// </summary>
public static class CatalogoPacksPdfService
{
    private static readonly string ColorAzul = "#0b5394";
    private static readonly string ColorAmarillo = "#ffd966";
    private static readonly string ColorTexto = "#333333";
    private static readonly string ColorTextoClaro = "#666666";
    private static readonly string ColorFondo = "#f8f9fa";
    private static readonly string ColorBorde = "#dee2e6";

    public class DatosCatalogo
    {
        public string NombrePais { get; set; } = "";
        public string CodigoPais { get; set; } = "";
        public byte[]? BanderaPais { get; set; }
        public string FechaGeneracion { get; set; } = "";
        public string NombreComercio { get; set; } = "";
        public string CodigoLocal { get; set; } = "";
        public List<PackCatalogo> Packs { get; set; } = new();
    }

    public class PackCatalogo
    {
        public string NombrePack { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Precio { get; set; }
        public string Divisa { get; set; } = "EUR";
        public byte[]? ImagenPoster { get; set; }
        public List<ProductoCatalogo> Productos { get; set; } = new();
        public List<byte[]> ImagenesAdicionales { get; set; } = new();
    }

    public class ProductoCatalogo
    {
        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; } = "unidad";
        public byte[]? Imagen { get; set; }

        public string CantidadFormateada => Cantidad == Math.Floor(Cantidad)
            ? $"{Cantidad:0}"
            : $"{Cantidad:0.##}";
    }

    static CatalogoPacksPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GenerarPdf(DatosCatalogo datos)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(ColorTexto));

                page.Header().Element(c => CrearEncabezado(c, datos));
                page.Content().Element(c => CrearContenido(c, datos));
                page.Footer().Element(c => CrearPiePagina(c, datos));
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Genera un PDF con catálogos de múltiples países
    /// Cada país comienza en una nueva página con su encabezado
    /// </summary>
    public static byte[] GenerarPdfMultiplesPaises(List<DatosCatalogo> catalogos)
    {
        if (catalogos.Count == 1)
        {
            return GenerarPdf(catalogos[0]);
        }

        var document = Document.Create(container =>
        {
            foreach (var datos in catalogos)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(ColorTexto));

                    page.Header().Element(c => CrearEncabezado(c, datos));
                    page.Content().Element(c => CrearContenido(c, datos));
                    page.Footer().Element(c => CrearPiePaginaMultiple(c, datos, catalogos.Count));
                });
            }
        });

        return document.GeneratePdf();
    }

    private static void CrearEncabezado(IContainer container, DatosCatalogo datos)
    {
        container.Column(column =>
        {
            // Barra superior decorativa
            column.Item().Row(row =>
            {
                row.RelativeItem(3).Height(6).Background(ColorAzul);
                row.RelativeItem(1).Height(6).Background(ColorAmarillo);
            });

            // Encabezado principal
            column.Item().PaddingVertical(12).Row(row =>
            {
                // Logo y titulo
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("ALLVA SYSTEM")
                        .FontSize(24)
                        .Bold()
                        .FontColor(ColorAzul);

                    col.Item().Text("Catalogo de Packs de Alimentos")
                        .FontSize(14)
                        .FontColor(ColorTextoClaro);

                    if (!string.IsNullOrEmpty(datos.NombreComercio))
                    {
                        col.Item().PaddingTop(3).Text($"Comercio: {datos.NombreComercio}")
                            .FontSize(9)
                            .FontColor(ColorTextoClaro);
                    }
                });

                // Pais y bandera
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Border(2).BorderColor(ColorAzul).Background(ColorAmarillo).Padding(12).Column(innerCol =>
                    {
                        // Mostrar bandera si existe
                        if (datos.BanderaPais != null && datos.BanderaPais.Length > 0)
                        {
                            try
                            {
                                innerCol.Item().AlignCenter().Height(30).Image(datos.BanderaPais);
                            }
                            catch
                            {
                                // Si falla la imagen, solo mostrar el texto
                            }
                        }

                        innerCol.Item().AlignCenter().Text(datos.NombrePais)
                            .FontSize(16)
                            .Bold()
                            .FontColor(ColorAzul);

                        if (!string.IsNullOrEmpty(datos.CodigoPais))
                        {
                            innerCol.Item().AlignCenter().Text($"({datos.CodigoPais})")
                                .FontSize(10)
                                .FontColor(ColorTexto);
                        }
                    });
                });
            });

            // Linea divisoria
            column.Item().Height(2).Background(ColorAzul);

            // Info de generacion
            column.Item().PaddingVertical(6).Background(ColorFondo).Padding(8).Row(row =>
            {
                row.RelativeItem().Text($"Local: {datos.CodigoLocal}")
                    .FontSize(9)
                    .FontColor(ColorTextoClaro);

                row.RelativeItem().AlignRight().Text($"Generado: {datos.FechaGeneracion}")
                    .FontSize(9)
                    .FontColor(ColorTextoClaro);
            });
        });
    }

    private static void CrearContenido(IContainer container, DatosCatalogo datos)
    {
        container.PaddingVertical(8).Column(column =>
        {
            // Resumen
            column.Item().PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(ColorAzul).Padding(10).Column(col =>
                {
                    col.Item().Text($"Total de Packs Disponibles: {datos.Packs.Count}")
                        .FontSize(12)
                        .Bold()
                        .FontColor(ColorAzul);
                });
            });

            // Iterar sobre cada pack
            foreach (var pack in datos.Packs)
            {
                column.Item().Element(c => CrearSeccionPack(c, pack));
                column.Item().Height(15);
            }
        });
    }

    private static void CrearSeccionPack(IContainer container, PackCatalogo pack)
    {
        container.Border(1).BorderColor(ColorBorde).Column(column =>
        {
            // Encabezado del pack
            column.Item().Background(ColorAzul).Padding(12).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(pack.NombrePack)
                        .FontSize(16)
                        .Bold()
                        .FontColor(Colors.White);

                    if (!string.IsNullOrEmpty(pack.Descripcion))
                    {
                        col.Item().PaddingTop(4).Text(pack.Descripcion)
                            .FontSize(10)
                            .FontColor(Colors.White);
                    }
                });

                row.AutoItem().AlignMiddle().Background(ColorAmarillo).Padding(8).Text($"{pack.Precio:N2} {pack.Divisa}")
                    .FontSize(14)
                    .Bold()
                    .FontColor(ColorAzul);
            });

            // Contenido del pack
            column.Item().Padding(12).Column(contentCol =>
            {
                // Imagen poster del pack (si existe)
                if (pack.ImagenPoster != null && pack.ImagenPoster.Length > 0)
                {
                    try
                    {
                        contentCol.Item().AlignCenter().MaxHeight(150).Image(pack.ImagenPoster).FitArea();
                        contentCol.Item().Height(10);
                    }
                    catch
                    {
                        // Si falla la imagen, continuar sin ella
                    }
                }

                // Lista de productos
                if (pack.Productos.Any())
                {
                    contentCol.Item().Text("PRODUCTOS INCLUIDOS:")
                        .FontSize(11)
                        .Bold()
                        .FontColor(ColorAzul);

                    contentCol.Item().Height(8);

                    contentCol.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60);  // Imagen
                            columns.RelativeColumn(2);   // Nombre y descripcion
                            columns.ConstantColumn(80);  // Cantidad
                        });

                        // Encabezado de tabla
                        table.Header(header =>
                        {
                            header.Cell().Background(ColorFondo).Border(1).BorderColor(ColorBorde).Padding(6)
                                .Text("Imagen").FontSize(9).Bold().AlignCenter();
                            header.Cell().Background(ColorFondo).Border(1).BorderColor(ColorBorde).Padding(6)
                                .Text("Producto").FontSize(9).Bold();
                            header.Cell().Background(ColorFondo).Border(1).BorderColor(ColorBorde).Padding(6)
                                .Text("Cantidad").FontSize(9).Bold().AlignCenter();
                        });

                        // Filas de productos
                        var alternar = false;
                        foreach (var producto in pack.Productos)
                        {
                            var bgColor = alternar ? ColorFondo : "#FFFFFF";

                            // Celda de imagen
                            var imgCell = table.Cell().Background(bgColor).Border(1).BorderColor(ColorBorde).Padding(4);
                            if (producto.Imagen != null && producto.Imagen.Length > 0)
                            {
                                try
                                {
                                    imgCell.AlignCenter().MaxHeight(50).MaxWidth(50).Image(producto.Imagen).FitArea();
                                }
                                catch
                                {
                                    imgCell.AlignCenter().Text("-").FontSize(9);
                                }
                            }
                            else
                            {
                                imgCell.AlignCenter().AlignMiddle().Text("-").FontSize(9).FontColor(ColorTextoClaro);
                            }

                            // Celda de nombre y descripcion
                            table.Cell().Background(bgColor).Border(1).BorderColor(ColorBorde).Padding(6).Column(prodCol =>
                            {
                                prodCol.Item().Text(producto.Nombre).FontSize(10).Bold();
                                if (!string.IsNullOrEmpty(producto.Descripcion))
                                {
                                    prodCol.Item().Text(producto.Descripcion).FontSize(8).FontColor(ColorTextoClaro);
                                }
                            });

                            // Celda de cantidad
                            table.Cell().Background(bgColor).Border(1).BorderColor(ColorBorde).Padding(6)
                                .AlignCenter().AlignMiddle()
                                .Text($"{producto.CantidadFormateada} {producto.UnidadMedida}").FontSize(10);

                            alternar = !alternar;
                        }
                    });
                }

                // Imagenes adicionales
                if (pack.ImagenesAdicionales.Any())
                {
                    contentCol.Item().Height(10);
                    contentCol.Item().Text("GALERIA DE IMAGENES:")
                        .FontSize(10)
                        .Bold()
                        .FontColor(ColorAzul);

                    contentCol.Item().Height(6);

                    contentCol.Item().Row(imgRow =>
                    {
                        foreach (var imagen in pack.ImagenesAdicionales.Take(4)) // Maximo 4 imagenes
                        {
                            try
                            {
                                imgRow.AutoItem().Padding(4).Border(1).BorderColor(ColorBorde)
                                    .MaxHeight(80).MaxWidth(100).Image(imagen).FitArea();
                            }
                            catch
                            {
                                // Si falla la imagen, continuar
                            }
                        }
                    });
                }
            });
        });
    }

    private static void CrearPiePagina(IContainer container, DatosCatalogo datos)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(ColorBorde);

            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Documento generado automaticamente")
                        .FontSize(8)
                        .FontColor(ColorTextoClaro);

                    col.Item().Text($"ALLVA SYSTEM - {datos.FechaGeneracion}")
                        .FontSize(8)
                        .FontColor(ColorTextoClaro);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8).FontColor(ColorTextoClaro));
                        text.Span("Pagina ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });

            // Barra inferior decorativa
            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem(3).Height(4).Background(ColorAzul);
                row.RelativeItem(1).Height(4).Background(ColorAmarillo);
            });
        });
    }

    private static void CrearPiePaginaMultiple(IContainer container, DatosCatalogo datos, int totalPaises)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(ColorBorde);

            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Catalogo multiple - {totalPaises} paises incluidos")
                        .FontSize(8)
                        .FontColor(ColorTextoClaro);

                    col.Item().Text($"ALLVA SYSTEM - {datos.FechaGeneracion}")
                        .FontSize(8)
                        .FontColor(ColorTextoClaro);
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8).FontColor(ColorTextoClaro));
                        text.Span("Pagina ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });

            // Barra inferior decorativa
            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem(3).Height(4).Background(ColorAzul);
                row.RelativeItem(1).Height(4).Background(ColorAmarillo);
            });
        });
    }

    public static string GuardarPdf(byte[] pdfBytes, string nombrePais)
    {
        // Generar nombre de archivo
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var paisLimpio = nombrePais.Replace(" ", "_").Replace(".", "");
        var nombreArchivo = $"Catalogo_Packs_{paisLimpio}_{timestamp}.pdf";

        // Guardar en la carpeta de Descargas del usuario
        var downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");

        if (!Directory.Exists(downloadsPath))
            downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        var rutaCompleta = Path.Combine(downloadsPath, nombreArchivo);

        // Si ya existe, agregar numero
        var contador = 1;
        var nombreBase = Path.GetFileNameWithoutExtension(nombreArchivo);
        var extension = Path.GetExtension(nombreArchivo);

        while (File.Exists(rutaCompleta))
        {
            rutaCompleta = Path.Combine(downloadsPath, $"{nombreBase}_{contador}{extension}");
            contador++;
        }

        File.WriteAllBytes(rutaCompleta, pdfBytes);

        return rutaCompleta;
    }
}
