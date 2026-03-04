using System;
using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Allva.Desktop.Services
{
    /// <summary>
    /// Servicio para generar recibos PDF de Pack de Alimentos
    /// Requiere instalar: dotnet add package QuestPDF
    /// </summary>
    public class ReciboFoodPackService
    {
        // Colores corporativos Allva
        private static readonly string ColorPrimario = "#0b5394";
        private static readonly string ColorSecundario = "#ffd966";
        private static readonly string ColorTexto = "#333333";
        private static readonly string ColorTextoClaro = "#666666";
        private static readonly string ColorFondo = "#f8f9fa";
        private static readonly string ColorBorde = "#dee2e6";
        private static readonly string ColorExito = "#28a745";

        public class DatosReciboFoodPack
        {
            // Datos de operacion
            public string NumeroOperacion { get; set; } = string.Empty;
            public DateTime FechaOperacion { get; set; } = DateTime.Now;
            public string CodigoLocal { get; set; } = string.Empty;
            public string NombreLocal { get; set; } = string.Empty;
            public string NombreUsuario { get; set; } = string.Empty;
            public string NumeroUsuario { get; set; } = string.Empty;

            // Datos del cliente (comprador)
            public string ClienteNombre { get; set; } = string.Empty;
            public string ClienteTipoDocumento { get; set; } = string.Empty;
            public string ClienteNumeroDocumento { get; set; } = string.Empty;
            public string ClienteTelefono { get; set; } = string.Empty;
            public string ClienteDireccion { get; set; } = string.Empty;
            public string ClienteNacionalidad { get; set; } = string.Empty;

            // Datos del beneficiario (quien recibe)
            public string BeneficiarioNombre { get; set; } = string.Empty;
            public string BeneficiarioTipoDocumento { get; set; } = string.Empty;
            public string BeneficiarioNumeroDocumento { get; set; } = string.Empty;
            public string BeneficiarioDireccion { get; set; } = string.Empty;
            public string BeneficiarioTelefono { get; set; } = string.Empty;
            public string BeneficiarioPaisDestino { get; set; } = string.Empty;
            public string BeneficiarioCiudadDestino { get; set; } = string.Empty;

            // Datos del pack
            public string PackNombre { get; set; } = string.Empty;
            public string PackDescripcion { get; set; } = string.Empty;
            public string[] PackProductos { get; set; } = Array.Empty<string>();
            public string PackImagenBase64 { get; set; } = string.Empty;

            // Totales
            public decimal PrecioPack { get; set; }
            public decimal Total { get; set; }
            public string Moneda { get; set; } = "USD";
            public string MetodoPago { get; set; } = "EFECTIVO";

            // Datos de la empresa
            public string EmpresaNombre { get; set; } = "ALLVA SYSTEM";
            public string EmpresaDireccion { get; set; } = string.Empty;
            public string EmpresaTelefono { get; set; } = string.Empty;
            public string EmpresaRUC { get; set; } = string.Empty;
        }

        public class DatosHistorialEstados
        {
            public string NumeroOperacion { get; set; } = string.Empty;
            public string FechaOperacion { get; set; } = string.Empty;
            public string HoraOperacion { get; set; } = string.Empty;
            public string UsuarioOperacion { get; set; } = string.Empty;
            public string ClienteNombre { get; set; } = string.Empty;
            public string PackNombre { get; set; } = string.Empty;
            public string ImporteTotal { get; set; } = string.Empty;
            public string Moneda { get; set; } = "EUR";
            public string EstadoActual { get; set; } = string.Empty;
            public string ColorEstadoActual { get; set; } = "#6c757d";
            public string CodigoLocal { get; set; } = string.Empty;
            public List<ItemHistorialEstado> Historial { get; set; } = new();
        }

        public class ItemHistorialEstado
        {
            public string EstadoAnterior { get; set; } = string.Empty;
            public string EstadoNuevo { get; set; } = string.Empty;
            public string Fecha { get; set; } = string.Empty;
            public string Usuario { get; set; } = string.Empty;
            public string Observaciones { get; set; } = string.Empty;
            public string ColorEstadoAnterior { get; set; } = "#6c757d";
            public string ColorEstadoNuevo { get; set; } = "#6c757d";
        }

        static ReciboFoodPackService()
        {
            // Configurar licencia de QuestPDF (Community es gratuita para empresas pequenas)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GenerarReciboPdf(DatosReciboFoodPack datos)
        {
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(ColorTexto));

                    page.Header().Element(c => CrearEncabezado(c, datos, "RECIBO", "PACK DE ALIMENTOS"));
                    page.Content().Element(c => CrearContenido(c, datos));
                    page.Footer().Element(c => CrearPiePagina(c, datos));
                });
            });

            return documento.GeneratePdf();
        }

        /// <summary>
        /// Genera un PDF de reimpresión (indica que es una copia)
        /// </summary>
        public byte[] GenerarReimpresionPdf(DatosReciboFoodPack datos)
        {
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(ColorTexto));

                    page.Header().Element(c => CrearEncabezado(c, datos, "REIMPRESIÓN", "PACK DE ALIMENTOS"));
                    page.Content().Element(c => CrearContenido(c, datos, esReimpresion: true));
                    page.Footer().Element(c => CrearPiePagina(c, datos, esReimpresion: true));
                });
            });

            return documento.GeneratePdf();
        }

        /// <summary>
        /// Genera un PDF de comprobante de anulación
        /// </summary>
        public byte[] GenerarReciboAnulacionPdf(DatosReciboFoodPack datos)
        {
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(ColorTexto));

                    page.Header().Element(c => CrearEncabezadoAnulacion(c, datos));
                    page.Content().Element(c => CrearContenidoAnulacion(c, datos));
                    page.Footer().Element(c => CrearPiePagina(c, datos, esAnulacion: true));
                });
            });

            return documento.GeneratePdf();
        }

        /// <summary>
        /// Genera un PDF del historial de estados de una operación
        /// </summary>
        public byte[] GenerarHistorialEstadosPdf(DatosHistorialEstados datos)
        {
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(ColorTexto));

                    page.Header().Element(c => CrearEncabezadoHistorial(c, datos));
                    page.Content().Element(c => CrearContenidoHistorial(c, datos));
                    page.Footer().Element(c => CrearPiePaginaHistorial(c, datos));
                });
            });

            return documento.GeneratePdf();
        }

        private void CrearEncabezadoHistorial(IContainer container, DatosHistorialEstados datos)
        {
            container.Column(column =>
            {
                // Barra superior decorativa
                column.Item().Row(row =>
                {
                    row.RelativeItem(3).Height(6).Background(ColorPrimario);
                    row.RelativeItem(1).Height(6).Background(ColorSecundario);
                });

                // Encabezado principal
                column.Item().PaddingVertical(12).Row(row =>
                {
                    // Logo y nombre de empresa
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("ALLVA SYSTEM")
                            .FontSize(26)
                            .Bold()
                            .FontColor(ColorPrimario);

                        col.Item().Text("Sistema de Gestión Empresarial")
                            .FontSize(9)
                            .FontColor(ColorTextoClaro);
                    });

                    // Tipo de documento
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Border(2).BorderColor(ColorPrimario).Background(ColorSecundario).Padding(12).Column(innerCol =>
                        {
                            innerCol.Item().AlignCenter().Text("HISTORIAL")
                                .FontSize(18)
                                .Bold()
                                .FontColor(ColorPrimario);

                            innerCol.Item().AlignCenter().Text("DE ESTADOS")
                                .FontSize(11)
                                .Bold()
                                .FontColor(ColorPrimario);

                            innerCol.Item().PaddingTop(5).AlignCenter().Text(datos.NumeroOperacion)
                                .FontSize(12)
                                .Bold()
                                .FontColor(ColorTexto);
                        });
                    });
                });

                // Línea divisoria
                column.Item().Height(2).Background(ColorPrimario);

                // Información de la operación
                column.Item().PaddingVertical(8).Background(ColorFondo).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Local: ").SemiBold();
                            text.Span(datos.CodigoLocal);
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Usuario: ").SemiBold();
                            text.Span(datos.UsuarioOperacion);
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Cliente: ").SemiBold();
                            text.Span(datos.ClienteNombre);
                        });
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Fecha: ").SemiBold();
                            text.Span(datos.FechaOperacion);
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Hora: ").SemiBold();
                            text.Span(datos.HoraOperacion);
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Importe: ").SemiBold();
                            text.Span($"{datos.ImporteTotal} {datos.Moneda}");
                        });
                    });
                });
            });
        }

        private void CrearContenidoHistorial(IContainer container, DatosHistorialEstados datos)
        {
            container.PaddingVertical(10).Column(column =>
            {
                // Información del pack y estado actual
                column.Item().Border(1).BorderColor(ColorBorde).Column(infoCol =>
                {
                    infoCol.Item().Background(ColorPrimario).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text("INFORMACIÓN DE LA OPERACIÓN")
                            .FontSize(12)
                            .Bold()
                            .FontColor(Colors.White);
                    });

                    infoCol.Item().Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(text =>
                            {
                                text.Span("Pack: ").SemiBold();
                                text.Span(datos.PackNombre);
                            });
                        });

                        row.AutoItem().Border(1).BorderColor(datos.ColorEstadoActual).Background(datos.ColorEstadoActual).PaddingHorizontal(8).PaddingVertical(4).Text(datos.EstadoActual)
                            .FontSize(11)
                            .Bold()
                            .FontColor(Colors.White);
                    });
                });

                column.Item().Height(15);

                // Tabla de historial
                column.Item().Element(c => CrearSeccion(c, "HISTORIAL DE CAMBIOS DE ESTADO", col =>
                {
                    col.Item().Table(table =>
                    {
                        // Definir columnas
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(35);  // #
                            columns.RelativeColumn(1.2f); // Estado Anterior
                            columns.ConstantColumn(25);  // Flecha
                            columns.RelativeColumn(1.2f); // Estado Nuevo
                            columns.RelativeColumn(1);   // Fecha
                            columns.RelativeColumn(1);   // Usuario
                            columns.RelativeColumn(1.5f); // Observaciones
                        });

                        // Encabezado de la tabla
                        table.Header(header =>
                        {
                            header.Cell().Element(CeldaEncabezado).Text("#");
                            header.Cell().Element(CeldaEncabezado).Text("Estado Anterior");
                            header.Cell().Element(CeldaEncabezado).Text("");
                            header.Cell().Element(CeldaEncabezado).Text("Estado Nuevo");
                            header.Cell().Element(CeldaEncabezado).Text("Fecha/Hora");
                            header.Cell().Element(CeldaEncabezado).Text("Usuario");
                            header.Cell().Element(CeldaEncabezado).Text("Observaciones");
                        });

                        // Filas de datos
                        for (int i = 0; i < datos.Historial.Count; i++)
                        {
                            var item = datos.Historial[i];
                            var usarFondoAlterno = i % 2 == 1;
                            var fondo = usarFondoAlterno ? ColorFondo : "#FFFFFF";

                            // Número
                            table.Cell().Element(c => CeldaDato(c, fondo)).Text($"{i + 1}")
                                .FontSize(9)
                                .FontColor(ColorTextoClaro);

                            // Estado anterior
                            if (!string.IsNullOrEmpty(item.EstadoAnterior))
                            {
                                table.Cell().Element(c => CeldaDato(c, fondo)).Element(e =>
                                    e.AlignCenter().Background(item.ColorEstadoAnterior).PaddingHorizontal(4).PaddingVertical(2).Text(item.EstadoAnterior)
                                        .FontSize(8)
                                        .Bold()
                                        .FontColor(Colors.White));
                            }
                            else
                            {
                                table.Cell().Element(c => CeldaDato(c, fondo)).AlignCenter().Text("-")
                                    .FontSize(9)
                                    .FontColor(ColorTextoClaro);
                            }

                            // Flecha
                            table.Cell().Element(c => CeldaDato(c, fondo)).AlignCenter().Text("→")
                                .FontSize(12)
                                .Bold()
                                .FontColor(ColorPrimario);

                            // Estado nuevo
                            table.Cell().Element(c => CeldaDato(c, fondo)).Element(e =>
                                e.AlignCenter().Background(item.ColorEstadoNuevo).PaddingHorizontal(4).PaddingVertical(2).Text(item.EstadoNuevo)
                                    .FontSize(8)
                                    .Bold()
                                    .FontColor(Colors.White));

                            // Fecha
                            table.Cell().Element(c => CeldaDato(c, fondo)).Text(item.Fecha)
                                .FontSize(9);

                            // Usuario
                            table.Cell().Element(c => CeldaDato(c, fondo)).Text(item.Usuario)
                                .FontSize(9);

                            // Observaciones
                            table.Cell().Element(c => CeldaDato(c, fondo)).Text(item.Observaciones)
                                .FontSize(8)
                                .FontColor(ColorTextoClaro);
                        }
                    });
                }));

                column.Item().Height(15);

                // Resumen
                column.Item().Background(ColorFondo).Border(1).BorderColor(ColorBorde).Padding(12).Column(resumenCol =>
                {
                    resumenCol.Item().Text("RESUMEN")
                        .SemiBold()
                        .FontSize(11)
                        .FontColor(ColorPrimario);

                    resumenCol.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Total de cambios de estado: ").SemiBold();
                            text.Span($"{datos.Historial.Count}");
                        });

                        row.RelativeItem().Text(text =>
                        {
                            text.Span("Estado actual: ").SemiBold();
                            text.Span(datos.EstadoActual).Bold().FontColor(datos.ColorEstadoActual);
                        });
                    });
                });
            });
        }

        private static IContainer CeldaEncabezado(IContainer container)
        {
            return container.Background(ColorPrimario).Padding(6).AlignCenter().DefaultTextStyle(x => x.FontColor(Colors.White).FontSize(9).SemiBold());
        }

        private static IContainer CeldaDato(IContainer container, string fondo)
        {
            return container.Background(fondo).BorderBottom(1).BorderColor(ColorBorde).Padding(6).AlignMiddle();
        }

        private void CrearPiePaginaHistorial(IContainer container, DatosHistorialEstados datos)
        {
            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(ColorBorde);

                column.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Documento generado electrónicamente")
                            .FontSize(8)
                            .FontColor(ColorTextoClaro);

                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                            .FontSize(8)
                            .FontColor(ColorTextoClaro);
                    });

                    row.RelativeItem().AlignCenter().Column(col =>
                    {
                        col.Item().Text("Historial de Estados")
                            .FontSize(9)
                            .SemiBold()
                            .FontColor(ColorPrimario);

                        col.Item().Text("ALLVA SYSTEM")
                            .FontSize(8)
                            .FontColor(ColorTextoClaro);
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8).FontColor(ColorTextoClaro));
                            text.Span("Página ");
                            text.CurrentPageNumber();
                            text.Span(" de ");
                            text.TotalPages();
                        });
                    });
                });

                // Barra inferior decorativa
                column.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem(3).Height(4).Background(ColorPrimario);
                    row.RelativeItem(1).Height(4).Background(ColorSecundario);
                });
            });
        }

        public void GenerarReciboPdf(DatosReciboFoodPack datos, string rutaArchivo)
        {
            var pdfBytes = GenerarReciboPdf(datos);
            File.WriteAllBytes(rutaArchivo, pdfBytes);
        }

        public Stream GenerarReciboPdfStream(DatosReciboFoodPack datos)
        {
            var pdfBytes = GenerarReciboPdf(datos);
            return new MemoryStream(pdfBytes);
        }

        private void CrearEncabezado(IContainer container, DatosReciboFoodPack datos, string titulo = "RECIBO", string subtitulo = "PACK DE ALIMENTOS")
        {
            container.Column(column =>
            {
                // Barra superior decorativa
                column.Item().Row(row =>
                {
                    row.RelativeItem(3).Height(6).Background(ColorPrimario);
                    row.RelativeItem(1).Height(6).Background(ColorSecundario);
                });

                // Encabezado principal
                column.Item().PaddingVertical(12).Row(row =>
                {
                    // Logo y nombre de empresa
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(datos.EmpresaNombre)
                            .FontSize(26)
                            .Bold()
                            .FontColor(ColorPrimario);

                        col.Item().Text("Sistema de Gestion Empresarial")
                            .FontSize(9)
                            .FontColor(ColorTextoClaro);

                        if (!string.IsNullOrEmpty(datos.EmpresaDireccion))
                        {
                            col.Item().PaddingTop(3).Text(datos.EmpresaDireccion)
                                .FontSize(8)
                                .FontColor(ColorTextoClaro);
                        }

                        if (!string.IsNullOrEmpty(datos.EmpresaTelefono))
                        {
                            col.Item().Text($"Tel: {datos.EmpresaTelefono}")
                                .FontSize(8)
                                .FontColor(ColorTextoClaro);
                        }
                    });

                    // Tipo de documento
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Border(2).BorderColor(ColorPrimario).Background(ColorSecundario).Padding(12).Column(innerCol =>
                        {
                            innerCol.Item().AlignCenter().Text(titulo)
                                .FontSize(18)
                                .Bold()
                                .FontColor(ColorPrimario);

                            innerCol.Item().AlignCenter().Text(subtitulo)
                                .FontSize(11)
                                .Bold()
                                .FontColor(ColorPrimario);

                            innerCol.Item().PaddingTop(5).AlignCenter().Text(datos.NumeroOperacion)
                                .FontSize(12)
                                .Bold()
                                .FontColor(ColorTexto);
                        });
                    });
                });

                // Linea divisoria
                column.Item().Height(2).Background(ColorPrimario);

                // Informacion de operacion
                column.Item().PaddingVertical(8).Background(ColorFondo).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Local: ").SemiBold();
                            text.Span(datos.CodigoLocal);
                            if (!string.IsNullOrEmpty(datos.NombreLocal))
                                text.Span($" - {datos.NombreLocal}");
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Usuario: ").SemiBold();
                            text.Span($"{datos.NombreUsuario} ({datos.NumeroUsuario})");
                        });
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Fecha: ").SemiBold();
                            text.Span(datos.FechaOperacion.ToString("dd/MM/yyyy"));
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Hora: ").SemiBold();
                            text.Span(datos.FechaOperacion.ToString("HH:mm:ss"));
                        });
                    });
                });
            });
        }

        private void CrearEncabezadoAnulacion(IContainer container, DatosReciboFoodPack datos)
        {
            var colorAnulacion = "#dc3545";

            container.Column(column =>
            {
                // Barra superior decorativa - roja para anulación
                column.Item().Row(row =>
                {
                    row.RelativeItem(3).Height(6).Background(colorAnulacion);
                    row.RelativeItem(1).Height(6).Background("#ff6b6b");
                });

                // Encabezado principal
                column.Item().PaddingVertical(12).Row(row =>
                {
                    // Logo y nombre de empresa
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(datos.EmpresaNombre)
                            .FontSize(26)
                            .Bold()
                            .FontColor(ColorPrimario);

                        col.Item().Text("Sistema de Gestion Empresarial")
                            .FontSize(9)
                            .FontColor(ColorTextoClaro);

                        if (!string.IsNullOrEmpty(datos.EmpresaDireccion))
                        {
                            col.Item().PaddingTop(3).Text(datos.EmpresaDireccion)
                                .FontSize(8)
                                .FontColor(ColorTextoClaro);
                        }

                        if (!string.IsNullOrEmpty(datos.EmpresaTelefono))
                        {
                            col.Item().Text($"Tel: {datos.EmpresaTelefono}")
                                .FontSize(8)
                                .FontColor(ColorTextoClaro);
                        }
                    });

                    // Tipo de documento - Anulación
                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Border(2).BorderColor(colorAnulacion).Background("#ffe6e6").Padding(12).Column(innerCol =>
                        {
                            innerCol.Item().AlignCenter().Text("ANULACIÓN")
                                .FontSize(18)
                                .Bold()
                                .FontColor(colorAnulacion);

                            innerCol.Item().AlignCenter().Text("PACK DE ALIMENTOS")
                                .FontSize(11)
                                .Bold()
                                .FontColor(colorAnulacion);

                            innerCol.Item().PaddingTop(5).AlignCenter().Text(datos.NumeroOperacion)
                                .FontSize(12)
                                .Bold()
                                .FontColor(ColorTexto);
                        });
                    });
                });

                // Linea divisoria - roja
                column.Item().Height(2).Background(colorAnulacion);

                // Informacion de operacion
                column.Item().PaddingVertical(8).Background("#fff5f5").Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Local: ").SemiBold();
                            text.Span(datos.CodigoLocal);
                            if (!string.IsNullOrEmpty(datos.NombreLocal))
                                text.Span($" - {datos.NombreLocal}");
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Usuario: ").SemiBold();
                            text.Span($"{datos.NombreUsuario} ({datos.NumeroUsuario})");
                        });
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text(text =>
                        {
                            text.Span("Fecha original: ").SemiBold();
                            text.Span(datos.FechaOperacion.ToString("dd/MM/yyyy"));
                        });

                        col.Item().Text(text =>
                        {
                            text.Span("Anulado: ").SemiBold().FontColor(colorAnulacion);
                            text.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontColor(colorAnulacion);
                        });
                    });
                });
            });
        }

        private void CrearContenido(IContainer container, DatosReciboFoodPack datos, bool esReimpresion = false)
        {
            container.PaddingVertical(8).Column(column =>
            {
                // SECCION: DATOS DEL CLIENTE (COMPRADOR)
                column.Item().Element(c => CrearSeccion(c, "DATOS DEL CLIENTE (COMPRADOR)", col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // Fila 1
                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Nombre Completo: ").SemiBold();
                            text.Span(datos.ClienteNombre);
                        });

                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Nacionalidad: ").SemiBold();
                            text.Span(datos.ClienteNacionalidad);
                        });

                        // Fila 2
                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Documento: ").SemiBold();
                            text.Span($"{datos.ClienteTipoDocumento} {datos.ClienteNumeroDocumento}");
                        });

                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Telefono: ").SemiBold();
                            text.Span(datos.ClienteTelefono);
                        });

                        // Fila 3 (direccion completa)
                        if (!string.IsNullOrEmpty(datos.ClienteDireccion))
                        {
                            table.Cell().ColumnSpan(2).Element(CeldaInfo).Text(text =>
                            {
                                text.Span("Direccion: ").SemiBold();
                                text.Span(datos.ClienteDireccion);
                            });
                        }
                    });
                }));

                column.Item().Height(8);

                // SECCION: DATOS DEL BENEFICIARIO (DESTINO)
                column.Item().Element(c => CrearSeccion(c, "DATOS DEL BENEFICIARIO (DESTINO)", col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // Fila 1
                        table.Cell().ColumnSpan(2).Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Nombre Completo: ").SemiBold();
                            text.Span(datos.BeneficiarioNombre);
                        });

                        // Fila 2
                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Documento: ").SemiBold();
                            text.Span($"{datos.BeneficiarioTipoDocumento} {datos.BeneficiarioNumeroDocumento}");
                        });

                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Telefono: ").SemiBold();
                            text.Span(string.IsNullOrEmpty(datos.BeneficiarioTelefono) ? "N/A" : datos.BeneficiarioTelefono);
                        });

                        // Fila 3
                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Pais Destino: ").SemiBold();
                            text.Span(datos.BeneficiarioPaisDestino);
                        });

                        table.Cell().Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Ciudad: ").SemiBold();
                            text.Span(string.IsNullOrEmpty(datos.BeneficiarioCiudadDestino) ? "N/A" : datos.BeneficiarioCiudadDestino);
                        });

                        // Fila 4 (direccion completa)
                        table.Cell().ColumnSpan(2).Element(CeldaInfo).Text(text =>
                        {
                            text.Span("Direccion de Entrega: ").SemiBold();
                            text.Span(datos.BeneficiarioDireccion);
                        });
                    });
                }));

                column.Item().Height(8);

                // SECCION: DETALLE DEL PACK
                column.Item().Element(c => CrearSeccion(c, "DETALLE DEL PACK DE ALIMENTOS", col =>
                {
                    // Encabezado del pack
                    col.Item().Border(1).BorderColor(ColorBorde).Background(ColorSecundario).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Column(packCol =>
                        {
                            packCol.Item().Text(datos.PackNombre)
                                .FontSize(16)
                                .Bold()
                                .FontColor(ColorPrimario);

                            if (!string.IsNullOrEmpty(datos.PackDescripcion))
                            {
                                packCol.Item().PaddingTop(4).Text(datos.PackDescripcion)
                                    .FontSize(9)
                                    .Italic()
                                    .FontColor(ColorTextoClaro);
                            }
                        });

                        row.AutoItem().AlignMiddle().Text($"{datos.Moneda} {datos.PrecioPack:N2}")
                            .FontSize(18)
                            .Bold()
                            .FontColor(ColorPrimario);
                    });

                    // Lista de productos
                    if (datos.PackProductos.Length > 0)
                    {
                        col.Item().PaddingTop(10).Text("Productos incluidos en el pack:")
                            .SemiBold()
                            .FontSize(10);

                        col.Item().PaddingTop(5).Border(1).BorderColor(ColorBorde).Padding(10).Column(prodCol =>
                        {
                            for (int i = 0; i < datos.PackProductos.Length; i++)
                            {
                                var usarFondoAlterno = i % 2 == 1;
                                
                                if (usarFondoAlterno)
                                {
                                    prodCol.Item().Background(ColorFondo).Padding(4).Row(prodRow =>
                                    {
                                        prodRow.AutoItem().Width(20).Text($"{i + 1}.")
                                            .FontColor(ColorPrimario)
                                            .SemiBold();
                                        prodRow.RelativeItem().Text(datos.PackProductos[i]);
                                    });
                                }
                                else
                                {
                                    prodCol.Item().Padding(4).Row(prodRow =>
                                    {
                                        prodRow.AutoItem().Width(20).Text($"{i + 1}.")
                                            .FontColor(ColorPrimario)
                                            .SemiBold();
                                        prodRow.RelativeItem().Text(datos.PackProductos[i]);
                                    });
                                }
                            }
                        });
                    }
                }));

                column.Item().Height(12);

                // SECCION: RESUMEN DE PAGO
                column.Item().Border(2).BorderColor(ColorPrimario).Column(totalCol =>
                {
                    // Encabezado
                    totalCol.Item().Background(ColorPrimario).Padding(10).Text("RESUMEN DE PAGO")
                        .FontSize(12)
                        .Bold()
                        .FontColor(Colors.White);

                    // Detalles
                    totalCol.Item().Padding(12).Column(detalleCol =>
                    {
                        detalleCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Precio del Pack:");
                            row.AutoItem().Text($"{datos.Moneda} {datos.PrecioPack:N2}");
                        });

                        detalleCol.Item().PaddingVertical(5).LineHorizontal(1).LineColor(ColorBorde);

                        detalleCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Metodo de Pago:").SemiBold();
                            row.AutoItem().Text(datos.MetodoPago);
                        });
                    });

                    // Total
                    totalCol.Item().Background(ColorSecundario).Padding(12).Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL PAGADO")
                            .FontSize(14)
                            .Bold()
                            .FontColor(ColorPrimario);

                        row.AutoItem().Text($"{datos.Moneda} {datos.Total:N2}")
                            .FontSize(18)
                            .Bold()
                            .FontColor(ColorPrimario);
                    });
                });

                column.Item().Height(12);

                // INFORMACION IMPORTANTE
                column.Item().Background(ColorFondo).Border(1).BorderColor(ColorBorde).Padding(10).Column(infoCol =>
                {
                    infoCol.Item().Text("INFORMACION IMPORTANTE")
                        .SemiBold()
                        .FontSize(10)
                        .FontColor(ColorPrimario);

                    infoCol.Item().PaddingTop(5).Text(
                        "1. Este recibo es comprobante de su compra. Conservelo para cualquier reclamo.\n" +
                        "2. El tiempo estimado de entrega depende del destino seleccionado.\n" +
                        "3. El beneficiario debe presentar documento de identidad al recibir el pack.\n" +
                        "4. Para consultas sobre el estado de su envio, comuniquese con nuestras oficinas.\n" +
                        "5. Los productos incluidos pueden variar segun disponibilidad en destino.")
                        .FontSize(8)
                        .FontColor(ColorTextoClaro)
                        .LineHeight(1.4f);
                });

                // Estado del envio
                column.Item().PaddingTop(10).AlignCenter().Row(row =>
                {
                    row.AutoItem().Border(1).BorderColor(ColorExito).Background(Colors.White).Padding(8).Row(statusRow =>
                    {
                        statusRow.AutoItem().Text("ESTADO: ")
                            .FontSize(10)
                            .SemiBold();
                        statusRow.AutoItem().Text("PENDIENTE DE ENVIO")
                            .FontSize(10)
                            .Bold()
                            .FontColor(ColorExito);
                    });
                });
            });
        }

        private void CrearSeccion(IContainer container, string titulo, Action<ColumnDescriptor> contenido)
        {
            container.Column(column =>
            {
                // Titulo de seccion
                column.Item().Row(row =>
                {
                    row.AutoItem().Width(4).Height(16).Background(ColorPrimario);
                    row.RelativeItem().PaddingLeft(8).AlignMiddle().Text(titulo)
                        .FontSize(11)
                        .Bold()
                        .FontColor(ColorPrimario);
                });

                // Contenido
                column.Item().PaddingTop(6).PaddingLeft(12).Element(c => c.Column(contenido));
            });
        }

        private static IContainer CeldaInfo(IContainer container)
        {
            return container.PaddingVertical(3);
        }

        private void CrearContenidoAnulacion(IContainer container, DatosReciboFoodPack datos)
        {
            var colorAnulacion = "#dc3545";

            container.PaddingVertical(8).Column(column =>
            {
                // Banner de anulación
                column.Item().Background(colorAnulacion).Padding(15).AlignCenter().Column(bannerCol =>
                {
                    bannerCol.Item().Text("OPERACIÓN ANULADA")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.White);

                    bannerCol.Item().PaddingTop(5).Text("Este pedido ha sido cancelado y no será procesado")
                        .FontSize(11)
                        .FontColor(Colors.White);
                });

                column.Item().Height(15);

                // Datos de la operación anulada
                column.Item().Border(1).BorderColor(colorAnulacion).Column(infoCol =>
                {
                    infoCol.Item().Background("#ffe6e6").Padding(10).Text("DATOS DE LA OPERACIÓN ANULADA")
                        .FontSize(12)
                        .Bold()
                        .FontColor(colorAnulacion);

                    infoCol.Item().Padding(12).Column(detalleCol =>
                    {
                        detalleCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Número de Operación: ").SemiBold();
                                text.Span(datos.NumeroOperacion);
                            });
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Fecha Original: ").SemiBold();
                                text.Span(datos.FechaOperacion.ToString("dd/MM/yyyy HH:mm"));
                            });
                        });

                        detalleCol.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Pack: ").SemiBold();
                                text.Span(datos.PackNombre);
                            });
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Importe: ").SemiBold();
                                text.Span($"{datos.Moneda} {datos.Total:N2}");
                            });
                        });

                        detalleCol.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Cliente: ").SemiBold();
                                text.Span(datos.ClienteNombre);
                            });
                        });

                        detalleCol.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Beneficiario: ").SemiBold();
                                text.Span(datos.BeneficiarioNombre);
                            });
                        });

                        detalleCol.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Destino: ").SemiBold();
                                text.Span($"{datos.BeneficiarioCiudadDestino}, {datos.BeneficiarioPaisDestino}");
                            });
                        });
                    });
                });

                column.Item().Height(15);

                // Estado de reembolso
                column.Item().Border(1).BorderColor(ColorBorde).Background(ColorFondo).Padding(12).Column(reembolsoCol =>
                {
                    reembolsoCol.Item().Text("INFORMACIÓN DE REEMBOLSO")
                        .SemiBold()
                        .FontSize(11)
                        .FontColor(ColorPrimario);

                    reembolsoCol.Item().PaddingTop(8).Text(
                        "El importe de esta operación será reembolsado según las políticas de la empresa.\n" +
                        "Para consultas sobre el estado del reembolso, comuníquese con nuestras oficinas.")
                        .FontSize(10)
                        .FontColor(ColorTextoClaro);
                });

                column.Item().Height(15);

                // Firma o sello
                column.Item().AlignCenter().Column(firmaCol =>
                {
                    firmaCol.Item().Border(2).BorderColor(colorAnulacion).Padding(10).Text("DOCUMENTO ANULADO")
                        .FontSize(14)
                        .Bold()
                        .FontColor(colorAnulacion);

                    firmaCol.Item().PaddingTop(5).Text($"Fecha de anulación: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                        .FontSize(9)
                        .FontColor(ColorTextoClaro);
                });
            });
        }

        private void CrearPiePagina(IContainer container, DatosReciboFoodPack datos, bool esReimpresion = false, bool esAnulacion = false)
        {
            var colorBarra = esAnulacion ? "#dc3545" : ColorPrimario;
            var colorBarraSecundaria = esAnulacion ? "#ff6b6b" : ColorSecundario;

            container.Column(column =>
            {
                column.Item().LineHorizontal(1).LineColor(ColorBorde);

                // Indicador de reimpresión o anulación
                if (esReimpresion)
                {
                    column.Item().PaddingTop(5).AlignCenter().Text("*** REIMPRESIÓN - COPIA DEL DOCUMENTO ORIGINAL ***")
                        .FontSize(9)
                        .Bold()
                        .FontColor("#ff9800");
                }
                else if (esAnulacion)
                {
                    column.Item().PaddingTop(5).AlignCenter().Text("*** DOCUMENTO DE ANULACIÓN ***")
                        .FontSize(9)
                        .Bold()
                        .FontColor("#dc3545");
                }

                column.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Documento generado electronicamente")
                            .FontSize(8)
                            .FontColor(ColorTextoClaro);

                        col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                            .FontSize(8)
                            .FontColor(ColorTextoClaro);
                    });

                    row.RelativeItem().AlignCenter().Column(col =>
                    {
                        if (esAnulacion)
                        {
                            col.Item().Text("Operación Anulada")
                                .FontSize(9)
                                .SemiBold()
                                .FontColor("#dc3545");
                        }
                        else
                        {
                            col.Item().Text("Gracias por su preferencia")
                                .FontSize(9)
                                .SemiBold()
                                .FontColor(ColorPrimario);
                        }

                        col.Item().Text("ALLVA SYSTEM")
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
                    row.RelativeItem(3).Height(4).Background(colorBarra);
                    row.RelativeItem(1).Height(4).Background(colorBarraSecundaria);
                });
            });
        }
    }
}