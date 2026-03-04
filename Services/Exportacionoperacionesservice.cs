using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;

namespace Allva.Desktop.Services;

public static class ExportacionOperacionesService
{
    // ============================================
    // MODELOS DE DATOS
    // ============================================
    
    public class DatosExportacion
    {
        public string Modulo { get; set; } = "";
        public string FechaGeneracion { get; set; } = "";
        public string HoraGeneracion { get; set; } = "";
        public string? FiltroFechaDesde { get; set; }
        public string? FiltroFechaHasta { get; set; }
        public string? FiltroComercio { get; set; }
        public string? FiltroLocal { get; set; }
        public string? FiltroDivisa { get; set; }
        public int TotalOperaciones { get; set; }
        public decimal TotalEuros { get; set; }
        public Dictionary<string, decimal> TotalesPorDivisa { get; set; } = new();
        public List<OperacionExportar> Operaciones { get; set; } = new();
    }
    
    public class OperacionExportar
    {
        public string Fecha { get; set; } = "";
        public string Hora { get; set; } = "";
        public int IdOperacion { get; set; }
        public string NumeroOperacion { get; set; } = "";
        public string CodigoLocal { get; set; } = "";
        public string NombreComercio { get; set; } = "";
        public string Divisa { get; set; } = "";
        public decimal Cantidad { get; set; }
        public decimal CantidadEuros { get; set; }
        public string Cliente { get; set; } = "";
        public string TipoDocumento { get; set; } = "";
        public string NumeroDocumento { get; set; } = "";
    }

    // ============================================
    // EXPORTAR PDF
    // ============================================
    
    public static byte[] GenerarPDF(DatosExportacion datos)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                
                page.Header().Element(c => ComponerEncabezadoPDF(c, datos));
                page.Content().Element(c => ComponerContenidoPDF(c, datos));
                page.Footer().Element(ComponerPiePDF);
            });
        });
        
        return document.GeneratePdf();
    }
    
    private static void ComponerEncabezadoPDF(IContainer container, DatosExportacion datos)
    {
        container.Column(col =>
        {
            col.Item().Background(Color.FromHex("#0b5394")).Padding(10).Row(row =>
            {
                row.RelativeItem().Text($"REPORTE DE OPERACIONES - {datos.Modulo.ToUpper()}")
                    .FontSize(16).Bold().FontColor(Colors.White);
                row.ConstantItem(200).AlignRight().Text($"Generado: {datos.FechaGeneracion} {datos.HoraGeneracion}")
                    .FontSize(10).FontColor(Colors.White);
            });
            
            col.Item().Height(10);
            
            col.Item().Background(Color.FromHex("#F8F9FA")).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("FILTROS APLICADOS").FontSize(11).Bold().FontColor(Color.FromHex("#0b5394"));
                    c.Item().Height(5);
                    
                    var filtros = new List<string>();
                    if (!string.IsNullOrEmpty(datos.FiltroFechaDesde))
                        filtros.Add($"Desde: {datos.FiltroFechaDesde}");
                    if (!string.IsNullOrEmpty(datos.FiltroFechaHasta))
                        filtros.Add($"Hasta: {datos.FiltroFechaHasta}");
                    if (!string.IsNullOrEmpty(datos.FiltroComercio))
                        filtros.Add($"Comercio: {datos.FiltroComercio}");
                    if (!string.IsNullOrEmpty(datos.FiltroLocal))
                        filtros.Add($"Local: {datos.FiltroLocal}");
                    if (!string.IsNullOrEmpty(datos.FiltroDivisa))
                        filtros.Add($"Divisa: {datos.FiltroDivisa}");
                    
                    if (filtros.Count == 0)
                        filtros.Add("Sin filtros aplicados");
                    
                    c.Item().Text(string.Join(" | ", filtros)).FontSize(9);
                });
            });
            
            col.Item().Height(10);
            
            col.Item().Row(row =>
            {
                row.RelativeItem().Background(Color.FromHex("#0b5394")).Padding(8).Column(c =>
                {
                    c.Item().Text("OPERACIONES").FontSize(9).FontColor(Color.FromHex("#B8D4E8"));
                    c.Item().Text(datos.TotalOperaciones.ToString()).FontSize(16).Bold().FontColor(Colors.White);
                });
                
                row.ConstantItem(10);
                
                row.RelativeItem().Background(Color.FromHex("#28a745")).Padding(8).Column(c =>
                {
                    c.Item().Text("TOTAL EUR").FontSize(9).FontColor(Color.FromHex("#B8E8C8"));
                    c.Item().Text($"{datos.TotalEuros:N2}").FontSize(16).Bold().FontColor(Colors.White);
                });
                
                row.ConstantItem(10);
                
                foreach (var divisa in datos.TotalesPorDivisa.Take(5))
                {
                    var color = divisa.Key switch
                    {
                        "USD" => "#17a2b8",
                        "GBP" => "#6f42c1",
                        "CHF" => "#fd7e14",
                        "JPY" => "#e83e8c",
                        _ => "#6c757d"
                    };
                    
                    row.RelativeItem().Background(Color.FromHex(color)).Padding(8).Column(c =>
                    {
                        c.Item().Text(divisa.Key).FontSize(9).FontColor(Colors.White);
                        c.Item().Text($"{divisa.Value:N2}").FontSize(14).Bold().FontColor(Colors.White);
                    });
                    
                    row.ConstantItem(10);
                }
            });
            
            col.Item().Height(15);
        });
    }
    
    private static void ComponerContenidoPDF(IContainer container, DatosExportacion datos)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(60);
                columns.ConstantColumn(40);
                columns.ConstantColumn(35);
                columns.ConstantColumn(55);
                columns.ConstantColumn(60);
                columns.RelativeColumn(1.5f);
                columns.ConstantColumn(40);
                columns.ConstantColumn(60);
                columns.ConstantColumn(60);
                columns.RelativeColumn(1.5f);
                columns.ConstantColumn(35);
                columns.ConstantColumn(70);
            });
            
            table.Header(header =>
            {
                var headers = new[] { "Fecha", "Hora", "ID", "N Oper.", "Local", "Comercio", "Divisa", "Cantidad", "EUR", "Cliente", "Doc", "N Doc." };
                foreach (var h in headers)
                {
                    header.Cell().Background(Color.FromHex("#0b5394")).Padding(5)
                        .Text(h).FontSize(8).Bold().FontColor(Colors.White);
                }
            });
            
            var alternar = false;
            foreach (var op in datos.Operaciones)
            {
                var bgColor = alternar ? Color.FromHex("#F5F5F5") : Colors.White;
                
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(op.Fecha).FontSize(8);
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(op.Hora).FontSize(8);
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(op.IdOperacion.ToString()).FontSize(8).FontColor(Colors.Grey.Darken1);
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(op.NumeroOperacion).FontSize(8).FontColor(Color.FromHex("#0b5394")).Bold();
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(op.CodigoLocal).FontSize(8);
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(op.NombreComercio).FontSize(8);
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(op.Divisa).FontSize(8).Bold().FontColor(Color.FromHex("#0b5394"));
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .AlignRight().Text($"{op.Cantidad:N2}").FontSize(8);
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .AlignRight().Text($"{op.CantidadEuros:N2}").FontSize(8).FontColor(Color.FromHex("#28a745"));
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(op.Cliente).FontSize(8);
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(op.TipoDocumento).FontSize(7);
                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4)
                    .Text(op.NumeroDocumento).FontSize(7);
                
                alternar = !alternar;
            }
        });
    }
    
    private static void ComponerPiePDF(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Pagina ");
            text.CurrentPageNumber();
            text.Span(" de ");
            text.TotalPages();
        });
    }

    // ============================================
    // EXPORTAR EXCEL
    // ============================================
    
    public static byte[] GenerarExcel(DatosExportacion datos)
    {
        using var workbook = new XLWorkbook();
        
        // Hoja 1: Resumen
        var hojaResumen = workbook.Worksheets.Add("Resumen");
        CrearHojaResumen(hojaResumen, datos);
        
        // Hoja 2: Detalle
        var hojaDetalle = workbook.Worksheets.Add("Operaciones");
        CrearHojaDetalle(hojaDetalle, datos);
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
    
    private static void CrearHojaResumen(IXLWorksheet hoja, DatosExportacion datos)
    {
        int fila = 1;
        
        // Titulo principal
        hoja.Cell(fila, 1).Value = $"REPORTE DE OPERACIONES - {datos.Modulo.ToUpper()}";
        hoja.Range(fila, 1, fila, 6).Merge();
        hoja.Cell(fila, 1).Style.Font.Bold = true;
        hoja.Cell(fila, 1).Style.Font.FontSize = 18;
        hoja.Cell(fila, 1).Style.Font.FontColor = XLColor.White;
        hoja.Cell(fila, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0b5394");
        hoja.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        hoja.Cell(fila, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        hoja.Row(fila).Height = 35;
        fila += 2;
        
        // Informacion de generacion
        hoja.Cell(fila, 1).Value = "INFORMACION DEL REPORTE";
        hoja.Range(fila, 1, fila, 2).Merge();
        hoja.Cell(fila, 1).Style.Font.Bold = true;
        hoja.Cell(fila, 1).Style.Font.FontColor = XLColor.FromHtml("#0b5394");
        hoja.Cell(fila, 1).Style.Font.FontSize = 12;
        fila++;
        
        hoja.Cell(fila, 1).Value = "Fecha de generacion:";
        hoja.Cell(fila, 2).Value = datos.FechaGeneracion;
        hoja.Cell(fila, 3).Value = "Hora:";
        hoja.Cell(fila, 4).Value = datos.HoraGeneracion;
        hoja.Cell(fila, 1).Style.Font.Bold = true;
        hoja.Cell(fila, 3).Style.Font.Bold = true;
        fila += 2;
        
        // Seccion Filtros
        hoja.Cell(fila, 1).Value = "FILTROS APLICADOS";
        hoja.Range(fila, 1, fila, 2).Merge();
        hoja.Cell(fila, 1).Style.Font.Bold = true;
        hoja.Cell(fila, 1).Style.Font.FontColor = XLColor.FromHtml("#0b5394");
        hoja.Cell(fila, 1).Style.Font.FontSize = 12;
        fila++;
        
        bool hayFiltros = false;
        if (!string.IsNullOrEmpty(datos.FiltroFechaDesde))
        {
            hoja.Cell(fila, 1).Value = "Fecha desde:";
            hoja.Cell(fila, 2).Value = datos.FiltroFechaDesde;
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hayFiltros = true;
            fila++;
        }
        if (!string.IsNullOrEmpty(datos.FiltroFechaHasta))
        {
            hoja.Cell(fila, 1).Value = "Fecha hasta:";
            hoja.Cell(fila, 2).Value = datos.FiltroFechaHasta;
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hayFiltros = true;
            fila++;
        }
        if (!string.IsNullOrEmpty(datos.FiltroComercio))
        {
            hoja.Cell(fila, 1).Value = "Comercio:";
            hoja.Cell(fila, 2).Value = datos.FiltroComercio;
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hayFiltros = true;
            fila++;
        }
        if (!string.IsNullOrEmpty(datos.FiltroLocal))
        {
            hoja.Cell(fila, 1).Value = "Local:";
            hoja.Cell(fila, 2).Value = datos.FiltroLocal;
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hayFiltros = true;
            fila++;
        }
        if (!string.IsNullOrEmpty(datos.FiltroDivisa))
        {
            hoja.Cell(fila, 1).Value = "Divisa:";
            hoja.Cell(fila, 2).Value = datos.FiltroDivisa;
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hayFiltros = true;
            fila++;
        }
        
        if (!hayFiltros)
        {
            hoja.Cell(fila, 1).Value = "Sin filtros aplicados";
            hoja.Cell(fila, 1).Style.Font.Italic = true;
            hoja.Cell(fila, 1).Style.Font.FontColor = XLColor.Gray;
            fila++;
        }
        
        fila += 2;
        
        // Seccion Resumen Principal
        hoja.Cell(fila, 1).Value = "RESUMEN PRINCIPAL";
        hoja.Range(fila, 1, fila, 2).Merge();
        hoja.Cell(fila, 1).Style.Font.Bold = true;
        hoja.Cell(fila, 1).Style.Font.FontColor = XLColor.FromHtml("#0b5394");
        hoja.Cell(fila, 1).Style.Font.FontSize = 12;
        fila++;
        
        // Tabla de resumen principal con bordes
        int filaInicioResumen = fila;
        
        // Total operaciones
        hoja.Cell(fila, 1).Value = "Total Operaciones";
        hoja.Cell(fila, 2).Value = datos.TotalOperaciones;
        hoja.Cell(fila, 2).Style.Font.Bold = true;
        hoja.Cell(fila, 2).Style.Font.FontSize = 14;
        hoja.Cell(fila, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#0b5394");
        hoja.Cell(fila, 2).Style.Font.FontColor = XLColor.White;
        hoja.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        fila++;
        
        // Total EUR
        hoja.Cell(fila, 1).Value = "Total EUR";
        hoja.Cell(fila, 2).Value = datos.TotalEuros;
        hoja.Cell(fila, 2).Style.NumberFormat.Format = "#,##0.00 \"EUR\"";
        hoja.Cell(fila, 2).Style.Font.Bold = true;
        hoja.Cell(fila, 2).Style.Font.FontSize = 14;
        hoja.Cell(fila, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#28a745");
        hoja.Cell(fila, 2).Style.Font.FontColor = XLColor.White;
        hoja.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        fila++;
        
        // Promedio por operacion
        decimal promedio = datos.TotalOperaciones > 0 ? datos.TotalEuros / datos.TotalOperaciones : 0;
        hoja.Cell(fila, 1).Value = "Promedio por Operacion";
        hoja.Cell(fila, 2).Value = promedio;
        hoja.Cell(fila, 2).Style.NumberFormat.Format = "#,##0.00 \"EUR\"";
        hoja.Cell(fila, 2).Style.Font.Bold = true;
        hoja.Cell(fila, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#17a2b8");
        hoja.Cell(fila, 2).Style.Font.FontColor = XLColor.White;
        hoja.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        fila++;
        
        // Cantidad de divisas
        hoja.Cell(fila, 1).Value = "Divisas Operadas";
        hoja.Cell(fila, 2).Value = datos.TotalesPorDivisa.Count;
        hoja.Cell(fila, 2).Style.Font.Bold = true;
        hoja.Cell(fila, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#6f42c1");
        hoja.Cell(fila, 2).Style.Font.FontColor = XLColor.White;
        hoja.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        fila++;
        
        // Bordes para la tabla de resumen
        var rangoResumen = hoja.Range(filaInicioResumen, 1, fila - 1, 2);
        rangoResumen.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        rangoResumen.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        
        fila += 2;
        
        // Totales por divisa
        if (datos.TotalesPorDivisa.Any())
        {
            hoja.Cell(fila, 1).Value = "DESGLOSE POR DIVISA";
            hoja.Range(fila, 1, fila, 4).Merge();
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Cell(fila, 1).Style.Font.FontColor = XLColor.FromHtml("#0b5394");
            hoja.Cell(fila, 1).Style.Font.FontSize = 12;
            fila++;
            
            // Headers de tabla
            hoja.Cell(fila, 1).Value = "Divisa";
            hoja.Cell(fila, 2).Value = "Total";
            hoja.Cell(fila, 3).Value = "Operaciones";
            hoja.Cell(fila, 4).Value = "% del Total";
            hoja.Range(fila, 1, fila, 4).Style.Font.Bold = true;
            hoja.Range(fila, 1, fila, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#0b5394");
            hoja.Range(fila, 1, fila, 4).Style.Font.FontColor = XLColor.White;
            hoja.Range(fila, 1, fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            int filaHeaderDivisas = fila;
            fila++;
            
            // Calcular operaciones por divisa
            var opsPorDivisa = datos.Operaciones
                .GroupBy(o => o.Divisa)
                .ToDictionary(g => g.Key, g => g.Count());
            
            decimal totalGeneral = datos.TotalesPorDivisa.Values.Sum();
            
            int filaInicioDivisas = fila;
            foreach (var divisa in datos.TotalesPorDivisa.OrderByDescending(x => x.Value))
            {
                decimal porcentaje = totalGeneral > 0 ? (divisa.Value / totalGeneral) * 100 : 0;
                int numOps = opsPorDivisa.GetValueOrDefault(divisa.Key, 0);
                
                hoja.Cell(fila, 1).Value = divisa.Key;
                hoja.Cell(fila, 1).Style.Font.Bold = true;
                hoja.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                hoja.Cell(fila, 2).Value = divisa.Value;
                hoja.Cell(fila, 2).Style.NumberFormat.Format = "#,##0.00";
                hoja.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                
                hoja.Cell(fila, 3).Value = numOps;
                hoja.Cell(fila, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                hoja.Cell(fila, 4).Value = porcentaje / 100;
                hoja.Cell(fila, 4).Style.NumberFormat.Format = "0.00%";
                hoja.Cell(fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                // Alternar colores
                if ((fila - filaInicioDivisas) % 2 == 1)
                {
                    hoja.Range(fila, 1, fila, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
                }
                
                fila++;
            }
            
            // Fila de totales
            hoja.Cell(fila, 1).Value = "TOTAL";
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Cell(fila, 2).Value = totalGeneral;
            hoja.Cell(fila, 2).Style.NumberFormat.Format = "#,##0.00";
            hoja.Cell(fila, 2).Style.Font.Bold = true;
            hoja.Cell(fila, 3).Value = datos.TotalOperaciones;
            hoja.Cell(fila, 3).Style.Font.Bold = true;
            hoja.Cell(fila, 4).Value = 1;
            hoja.Cell(fila, 4).Style.NumberFormat.Format = "0.00%";
            hoja.Cell(fila, 4).Style.Font.Bold = true;
            hoja.Range(fila, 1, fila, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8E8E8");
            hoja.Range(fila, 1, fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            
            // Bordes para tabla de divisas
            var rangoDivisas = hoja.Range(filaHeaderDivisas, 1, fila, 4);
            rangoDivisas.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            rangoDivisas.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            
            fila += 2;
        }
        
        // Estadisticas adicionales
        if (datos.Operaciones.Any())
        {
            hoja.Cell(fila, 1).Value = "ESTADISTICAS ADICIONALES";
            hoja.Range(fila, 1, fila, 2).Merge();
            hoja.Cell(fila, 1).Style.Font.Bold = true;
            hoja.Cell(fila, 1).Style.Font.FontColor = XLColor.FromHtml("#0b5394");
            hoja.Cell(fila, 1).Style.Font.FontSize = 12;
            fila++;
            
            int filaInicioStats = fila;
            
            // Operacion mayor
            var opMayor = datos.Operaciones.OrderByDescending(o => o.CantidadEuros).FirstOrDefault();
            if (opMayor != null)
            {
                hoja.Cell(fila, 1).Value = "Operacion Mayor";
                hoja.Cell(fila, 2).Value = opMayor.CantidadEuros;
                hoja.Cell(fila, 2).Style.NumberFormat.Format = "#,##0.00 \"EUR\"";
                hoja.Cell(fila, 3).Value = $"({opMayor.NumeroOperacion} - {opMayor.Divisa})";
                hoja.Cell(fila, 3).Style.Font.FontColor = XLColor.Gray;
                fila++;
            }
            
            // Operacion menor
            var opMenor = datos.Operaciones.OrderBy(o => o.CantidadEuros).FirstOrDefault();
            if (opMenor != null)
            {
                hoja.Cell(fila, 1).Value = "Operacion Menor";
                hoja.Cell(fila, 2).Value = opMenor.CantidadEuros;
                hoja.Cell(fila, 2).Style.NumberFormat.Format = "#,##0.00 \"EUR\"";
                hoja.Cell(fila, 3).Value = $"({opMenor.NumeroOperacion} - {opMenor.Divisa})";
                hoja.Cell(fila, 3).Style.Font.FontColor = XLColor.Gray;
                fila++;
            }
            
            // Divisa mas operada
            if (datos.TotalesPorDivisa.Any())
            {
                var divisaMayor = datos.TotalesPorDivisa.OrderByDescending(x => x.Value).First();
                hoja.Cell(fila, 1).Value = "Divisa Principal";
                hoja.Cell(fila, 2).Value = divisaMayor.Key;
                hoja.Cell(fila, 2).Style.Font.Bold = true;
                hoja.Cell(fila, 3).Value = $"({divisaMayor.Value:N2})";
                hoja.Cell(fila, 3).Style.Font.FontColor = XLColor.Gray;
                fila++;
            }
            
            // Bordes
            var rangoStats = hoja.Range(filaInicioStats, 1, fila - 1, 3);
            rangoStats.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rangoStats.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }
        
        // Ajustar anchos
        hoja.Column(1).Width = 24;
        hoja.Column(2).Width = 18;
        hoja.Column(3).Width = 16;
        hoja.Column(4).Width = 14;
        hoja.Column(5).Width = 12;
        hoja.Column(6).Width = 12;
    }
    
    private static void CrearHojaDetalle(IXLWorksheet hoja, DatosExportacion datos)
    {
        // Headers
        var headers = new[] { "Fecha", "Hora", "ID", "N Oper.", "Local", "Comercio", "Divisa", "Cantidad", "EUR", "Cliente", "Tipo Doc", "N Documento" };
        
        for (int i = 0; i < headers.Length; i++)
        {
            hoja.Cell(1, i + 1).Value = headers[i];
            hoja.Cell(1, i + 1).Style.Font.Bold = true;
            hoja.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            hoja.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#0b5394");
            hoja.Cell(1, i + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(1, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }
        hoja.Row(1).Height = 22;
        
        // Datos
        int fila = 2;
        foreach (var op in datos.Operaciones)
        {
            hoja.Cell(fila, 1).Value = op.Fecha;
            hoja.Cell(fila, 2).Value = op.Hora;
            hoja.Cell(fila, 3).Value = op.IdOperacion;
            hoja.Cell(fila, 4).Value = op.NumeroOperacion;
            hoja.Cell(fila, 5).Value = op.CodigoLocal;
            hoja.Cell(fila, 6).Value = op.NombreComercio;
            hoja.Cell(fila, 7).Value = op.Divisa;
            hoja.Cell(fila, 8).Value = op.Cantidad;
            hoja.Cell(fila, 8).Style.NumberFormat.Format = "#,##0.00";
            hoja.Cell(fila, 9).Value = op.CantidadEuros;
            hoja.Cell(fila, 9).Style.NumberFormat.Format = "#,##0.00";
            hoja.Cell(fila, 9).Style.Font.FontColor = XLColor.FromHtml("#28a745");
            hoja.Cell(fila, 10).Value = op.Cliente;
            hoja.Cell(fila, 11).Value = op.TipoDocumento;
            hoja.Cell(fila, 12).Value = op.NumeroDocumento;
            
            // Centrar celdas
            hoja.Cell(fila, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            hoja.Cell(fila, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            hoja.Cell(fila, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            hoja.Cell(fila, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            
            // Alternar color de fila
            if (fila % 2 == 0)
            {
                hoja.Range(fila, 1, fila, 12).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            }
            
            fila++;
        }
        
        // Ajustar anchos de columna
        hoja.Column(1).Width = 12;  // Fecha
        hoja.Column(2).Width = 8;   // Hora
        hoja.Column(3).Width = 8;   // ID
        hoja.Column(4).Width = 12;  // N Oper
        hoja.Column(5).Width = 12;  // Local
        hoja.Column(6).Width = 18;  // Comercio
        hoja.Column(7).Width = 8;   // Divisa
        hoja.Column(8).Width = 14;  // Cantidad
        hoja.Column(9).Width = 14;  // EUR
        hoja.Column(10).Width = 20; // Cliente
        hoja.Column(11).Width = 10; // Tipo Doc
        hoja.Column(12).Width = 14; // N Documento
        
        // Congelar primera fila
        hoja.SheetView.FreezeRows(1);
    }

    // ============================================
    // EXPORTAR CSV
    // ============================================
    
    public static byte[] GenerarCSV(DatosExportacion datos)
    {
        var sb = new StringBuilder();
        
        // Headers
        sb.AppendLine("Fecha,Hora,ID,N Oper.,Local,Comercio,Divisa,Cantidad,EUR,Cliente,Tipo Doc,N Documento");
        
        // Datos
        foreach (var op in datos.Operaciones)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                EscaparCSV(op.Fecha),
                EscaparCSV(op.Hora),
                op.IdOperacion.ToString(),
                EscaparCSV(op.NumeroOperacion),
                EscaparCSV(op.CodigoLocal),
                EscaparCSV(op.NombreComercio),
                EscaparCSV(op.Divisa),
                op.Cantidad.ToString("F2", CultureInfo.InvariantCulture),
                op.CantidadEuros.ToString("F2", CultureInfo.InvariantCulture),
                EscaparCSV(op.Cliente),
                EscaparCSV(op.TipoDocumento),
                EscaparCSV(op.NumeroDocumento)
            }));
        }
        
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
    
    private static string EscaparCSV(string valor)
    {
        if (string.IsNullOrEmpty(valor))
            return "";
        
        if (valor.Contains(',') || valor.Contains('"') || valor.Contains('\n'))
            return $"\"{valor.Replace("\"", "\"\"")}\"";
        
        return valor;
    }
}