using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Allva.Desktop.Services;

public static class HistorialPdfService
{
    private static readonly string ColorAzul = "#0b5394";
    
    public class FiltrosReporte
    {
        public string? FechaDesde { get; set; }
        public string? FechaHasta { get; set; }
        public string? OperacionDesde { get; set; }
        public string? OperacionHasta { get; set; }
    }
    
    public class DatosReporte
    {
        public string CodigoLocal { get; set; } = "";
        public string NombreUsuario { get; set; } = "";
        public string FechaGeneracion { get; set; } = "";
        public string HoraGeneracion { get; set; } = "";
        public FiltrosReporte Filtros { get; set; } = new();
        public decimal BalanceActualEuros { get; set; }
        public decimal TotalDivisasValor { get; set; }
        public List<DivisaBalance> DesgloseDivisas { get; set; } = new();
        public List<OperacionReporte> Operaciones { get; set; } = new();
    }
    
    public class DivisaBalance
    {
        public string CodigoDivisa { get; set; } = "";
        public decimal Cantidad { get; set; }
    }
    
    public class OperacionReporte
    {
        public string Fecha { get; set; } = "";
        public string Hora { get; set; } = "";
        public string NumeroOperacion { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Divisa { get; set; } = "";
        public string SalidaEuros { get; set; } = "";
        public string EntradaEuros { get; set; } = "";
    }
    
    public static byte[] GenerarPdf(DatosReporte datos)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                
                page.Header().Element(c => ComposeHeader(c, datos));
                page.Content().Element(c => ComposeContent(c, datos));
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Pagina ").FontSize(9);
                    text.CurrentPageNumber().FontSize(9);
                    text.Span(" de ").FontSize(9);
                    text.TotalPages().FontSize(9);
                });
            });
        });
        
        return document.GeneratePdf();
    }
    
    private static void ComposeHeader(IContainer container, DatosReporte datos)
    {
        container.Column(column =>
        {
            // Titulo principal
            column.Item().Background(ColorAzul).Padding(15).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("HISTORIAL DE BALANCE DE CUENTAS")
                        .FontSize(18).Bold().FontColor(Colors.White);
                    col.Item().Text($"Local: {datos.CodigoLocal}")
                        .FontSize(12).FontColor(Colors.White);
                });
                
                row.ConstantItem(180).AlignRight().Column(col =>
                {
                    col.Item().Text($"Fecha: {datos.FechaGeneracion}")
                        .FontSize(10).FontColor(Colors.White);
                    col.Item().Text($"Hora: {datos.HoraGeneracion}")
                        .FontSize(10).FontColor(Colors.White);
                    col.Item().Text($"Usuario: {datos.NombreUsuario}")
                        .FontSize(10).FontColor(Colors.White);
                });
            });
            
            column.Item().Height(10);
            
            // Filtros aplicados
            column.Item().Border(1).BorderColor(Colors.Grey.Medium).Padding(10).Column(filtrosCol =>
            {
                filtrosCol.Item().Text("FILTROS APLICADOS").Bold().FontSize(11);
                filtrosCol.Item().Height(5);
                
                var hayFiltros = false;
                
                filtrosCol.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        if (!string.IsNullOrEmpty(datos.Filtros.FechaDesde))
                        {
                            c.Item().Text($"Fecha desde: {datos.Filtros.FechaDesde}").FontSize(9);
                            hayFiltros = true;
                        }
                        if (!string.IsNullOrEmpty(datos.Filtros.FechaHasta))
                        {
                            c.Item().Text($"Fecha hasta: {datos.Filtros.FechaHasta}").FontSize(9);
                            hayFiltros = true;
                        }
                    });
                    
                    row.RelativeItem().Column(c =>
                    {
                        if (!string.IsNullOrEmpty(datos.Filtros.OperacionDesde))
                        {
                            c.Item().Text($"Operacion desde: {datos.Filtros.OperacionDesde}").FontSize(9);
                            hayFiltros = true;
                        }
                        if (!string.IsNullOrEmpty(datos.Filtros.OperacionHasta))
                        {
                            c.Item().Text($"Operacion hasta: {datos.Filtros.OperacionHasta}").FontSize(9);
                            hayFiltros = true;
                        }
                    });
                });
                
                if (!hayFiltros)
                {
                    filtrosCol.Item().Text("Mes en curso (sin filtros adicionales)").FontSize(9).Italic();
                }
            });
            
            column.Item().Height(10);
            
            // Resumen de balances - Solo T.Euros y T.Divisa
            column.Item().Row(row =>
            {
                // T.Euros con color dinamico (verde si positivo, rojo si negativo)
                var colorTotalEuros = datos.BalanceActualEuros >= 0 ? "#008800" : "#CC3333";
                
                row.RelativeItem().Border(1).BorderColor(colorTotalEuros).Column(balCol =>
                {
                    balCol.Item().Background(colorTotalEuros).Padding(5)
                        .Text("T.Euros").FontSize(10).Bold().FontColor(Colors.White).AlignCenter();
                    balCol.Item().Padding(8).Text($"{datos.BalanceActualEuros:N2}")
                        .FontSize(14).Bold().FontColor(colorTotalEuros).AlignCenter();
                });
                
                row.ConstantItem(15);
                
                // T.Divisa
                row.RelativeItem().Border(1).BorderColor(ColorAzul).Column(divCol =>
                {
                    divCol.Item().Background(ColorAzul).Padding(5)
                        .Text("T.Divisa").FontSize(10).Bold().FontColor(Colors.White).AlignCenter();
                    divCol.Item().Padding(8).Text($"{datos.TotalDivisasValor:N2}")
                        .FontSize(14).Bold().AlignCenter();
                });
            });
            
            column.Item().Height(8);
            
            // Desglose de divisas
            if (datos.DesgloseDivisas.Any())
            {
                column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(desCol =>
                {
                    desCol.Item().Text("DESGLOSE DE DIVISAS").Bold().FontSize(10);
                    desCol.Item().Height(5);
                    
                    // Mostrar cada divisa con su cantidad
                    desCol.Item().Row(divisasRow =>
                    {
                        foreach (var divisa in datos.DesgloseDivisas)
                        {
                            divisasRow.AutoItem().Padding(5).Border(1).BorderColor(ColorAzul).Column(dCol =>
                            {
                                dCol.Item().Background(ColorAzul).Padding(4)
                                    .Text(divisa.CodigoDivisa).FontSize(9).Bold().FontColor(Colors.White).AlignCenter();
                                dCol.Item().Padding(6).Text($"{divisa.Cantidad:N2}")
                                    .FontSize(11).Bold().AlignCenter();
                            });
                            divisasRow.ConstantItem(8);
                        }
                    });
                });
            }
            
            column.Item().Height(10);
        });
    }
    
    private static void ComposeContent(IContainer container, DatosReporte datos)
    {
        container.Column(column =>
        {
            column.Item().Text($"OPERACIONES ({datos.Operaciones.Count} registros)")
                .FontSize(11).Bold();
            column.Item().Height(5);
            
            // Tabla de operaciones
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(55);  // Fecha
                    columns.ConstantColumn(40);  // Hora
                    columns.ConstantColumn(65);  // N Operacion
                    columns.RelativeColumn();    // Descripcion
                    columns.ConstantColumn(60);  // Divisa
                    columns.ConstantColumn(65);  // S.Euros
                    columns.ConstantColumn(65);  // E.Euros
                });
                
                // Encabezado de tabla
                table.Header(header =>
                {
                    header.Cell().Background(ColorAzul).Padding(5)
                        .Text("Fecha").FontSize(9).Bold().FontColor(Colors.White);
                    header.Cell().Background(ColorAzul).Padding(5)
                        .Text("Hora").FontSize(9).Bold().FontColor(Colors.White);
                    header.Cell().Background(ColorAzul).Padding(5)
                        .Text("N Oper.").FontSize(9).Bold().FontColor(Colors.White);
                    header.Cell().Background(ColorAzul).Padding(5)
                        .Text("Descripcion").FontSize(9).Bold().FontColor(Colors.White);
                    header.Cell().Background(ColorAzul).Padding(5)
                        .Text("Divisa").FontSize(9).Bold().FontColor(Colors.White);
                    header.Cell().Background(ColorAzul).Padding(5)
                        .Text("S.Euros").FontSize(9).Bold().FontColor(Colors.White);
                    header.Cell().Background(ColorAzul).Padding(5)
                        .Text("E.Euros").FontSize(9).Bold().FontColor(Colors.White);
                });
                
                // Filas de datos
                var alternar = false;
                foreach (var op in datos.Operaciones)
                {
                    var bgColor = alternar ? Colors.Grey.Lighten4 : Colors.White;
                    
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).Text(op.Fecha).FontSize(8);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).Text(op.Hora).FontSize(8);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).Text(op.NumeroOperacion).FontSize(8);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).Text(op.Descripcion).FontSize(8);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).Text(op.Divisa).FontSize(8);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).Text(op.SalidaEuros).FontSize(8)
                        .FontColor(!string.IsNullOrEmpty(op.SalidaEuros) && op.SalidaEuros.StartsWith("-") ? Colors.Red.Medium : Colors.Black);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                        .Padding(4).Text(op.EntradaEuros).FontSize(8)
                        .FontColor(!string.IsNullOrEmpty(op.EntradaEuros) ? Colors.Green.Medium : Colors.Black);
                    
                    alternar = !alternar;
                }
            });
        });
    }
    
    public static string GuardarPdf(byte[] pdfBytes, string codigoLocal)
    {
        // Generar nombre de archivo
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var nombreArchivo = $"Historial_{codigoLocal}_{timestamp}.pdf";
        
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