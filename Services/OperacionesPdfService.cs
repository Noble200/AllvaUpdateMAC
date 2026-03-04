using System;
using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Allva.Desktop.Services;

public static class OperacionesPdfService
{
    public class DatosReporteOperaciones
    {
        public string CodigoLocal { get; set; } = "";
        public string NombreUsuario { get; set; } = "";
        public string FechaGeneracion { get; set; } = "";
        public string HoraGeneracion { get; set; } = "";
        public string PanelSeleccionado { get; set; } = "";
        public int TotalOperaciones { get; set; } = 0;
        public decimal TotalEuros { get; set; } = 0;
        public decimal TotalDivisas { get; set; } = 0;
        public FiltrosReporte Filtros { get; set; } = new();
        public List<OperacionDetalle> Operaciones { get; set; } = new();
    }
    
    public class FiltrosReporte
    {
        public string FechaDesde { get; set; } = "";
        public string FechaHasta { get; set; } = "";
        public string OperacionDesde { get; set; } = "";
        public string OperacionHasta { get; set; } = "";
    }
    
    public class OperacionDetalle
    {
        public string Hora { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string NumeroOperacion { get; set; } = "";
        public string Usuario { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string CantidadDivisa { get; set; } = "";
        public string CantidadPagada { get; set; } = "";
        public string Cliente { get; set; } = "";
        public string TipoDocumento { get; set; } = "";
        public string NumeroDocumento { get; set; } = "";
    }
    
    public static byte[] GenerarPdf(DatosReporteOperaciones datos)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(9));
                
                page.Header().Element(c => ComposeHeader(c, datos));
                page.Content().Element(c => ComposeContent(c, datos));
                page.Footer().Element(c => ComposeFooter(c, datos));
            });
        });
        
        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }
    
    private static void ComposeHeader(IContainer container, DatosReporteOperaciones datos)
    {
        container.Column(column =>
        {
            // Titulo y datos del reporte
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("HISTORIAL DE OPERACIONES")
                        .FontSize(16)
                        .Bold()
                        .FontColor("#0b5394");
                    
                    col.Item().Text($"Panel: {datos.PanelSeleccionado}")
                        .FontSize(10)
                        .FontColor("#666666");
                });
                
                row.ConstantItem(200).AlignRight().Column(col =>
                {
                    col.Item().Text($"Local: {datos.CodigoLocal}")
                        .FontSize(10)
                        .Bold()
                        .FontColor("#0b5394");
                    col.Item().Text($"Usuario: {datos.NombreUsuario}")
                        .FontSize(9)
                        .FontColor("#444444");
                    col.Item().Text($"Generado: {datos.FechaGeneracion} {datos.HoraGeneracion}")
                        .FontSize(9)
                        .FontColor("#666666");
                });
            });
            
            column.Item().PaddingVertical(8).LineHorizontal(1).LineColor("#E0E0E0");
            
            // Filtros aplicados
            var tieneFiltros = !string.IsNullOrEmpty(datos.Filtros.FechaDesde) ||
                              !string.IsNullOrEmpty(datos.Filtros.FechaHasta) ||
                              !string.IsNullOrEmpty(datos.Filtros.OperacionDesde) ||
                              !string.IsNullOrEmpty(datos.Filtros.OperacionHasta);
            
            if (tieneFiltros)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span("Filtros: ").FontSize(9).FontColor("#666666");
                        
                        if (!string.IsNullOrEmpty(datos.Filtros.FechaDesde))
                            text.Span($"Desde: {datos.Filtros.FechaDesde}  ").FontSize(9);
                        if (!string.IsNullOrEmpty(datos.Filtros.FechaHasta))
                            text.Span($"Hasta: {datos.Filtros.FechaHasta}  ").FontSize(9);
                        if (!string.IsNullOrEmpty(datos.Filtros.OperacionDesde))
                            text.Span($"Op. Desde: {datos.Filtros.OperacionDesde}  ").FontSize(9);
                        if (!string.IsNullOrEmpty(datos.Filtros.OperacionHasta))
                            text.Span($"Op. Hasta: {datos.Filtros.OperacionHasta}").FontSize(9);
                    });
                });
                
                column.Item().PaddingBottom(8);
            }
        });
    }
    
    private static void ComposeContent(IContainer container, DatosReporteOperaciones datos)
    {
        container.Column(column =>
        {
            // Tabla de operaciones
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(45);   // Hora
                    columns.ConstantColumn(60);   // Fecha
                    columns.ConstantColumn(75);   // N Operacion
                    columns.ConstantColumn(65);   // Usuario
                    columns.RelativeColumn(1);    // Descripcion
                    columns.ConstantColumn(60);   // Cantidad
                    columns.ConstantColumn(60);   // Pagado
                    columns.RelativeColumn(1);    // Cliente
                    columns.ConstantColumn(50);   // Tipo Doc
                    columns.ConstantColumn(80);   // N Documento
                });
                
                // Header
                table.Header(header =>
                {
                    header.Cell().Background("#0b5394").Padding(5).Text("Hora").FontColor(Colors.White).FontSize(8).Bold().AlignCenter();
                    header.Cell().Background("#0b5394").Padding(5).Text("Fecha").FontColor(Colors.White).FontSize(8).Bold().AlignCenter();
                    header.Cell().Background("#0b5394").Padding(5).Text("N Operacion").FontColor(Colors.White).FontSize(8).Bold().AlignCenter();
                    header.Cell().Background("#0b5394").Padding(5).Text("Usuario").FontColor(Colors.White).FontSize(8).Bold().AlignCenter();
                    header.Cell().Background("#0b5394").Padding(5).Text("Descripcion").FontColor(Colors.White).FontSize(8).Bold();
                    header.Cell().Background("#0b5394").Padding(5).Text("Cantidad").FontColor(Colors.White).FontSize(8).Bold().AlignCenter();
                    header.Cell().Background("#0b5394").Padding(5).Text("Pagado").FontColor(Colors.White).FontSize(8).Bold().AlignCenter();
                    header.Cell().Background("#0b5394").Padding(5).Text("Cliente").FontColor(Colors.White).FontSize(8).Bold();
                    header.Cell().Background("#0b5394").Padding(5).Text("Tipo Doc").FontColor(Colors.White).FontSize(8).Bold().AlignCenter();
                    header.Cell().Background("#0b5394").Padding(5).Text("N Documento").FontColor(Colors.White).FontSize(8).Bold().AlignCenter();
                });
                
                // Filas
                var alternar = false;
                foreach (var op in datos.Operaciones)
                {
                    var bgColor = alternar ? "#F5F5F5" : "#FFFFFF";
                    alternar = !alternar;
                    
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor("#EEEEEE").Padding(4)
                        .Text(op.Hora).FontSize(8).AlignCenter();
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor("#EEEEEE").Padding(4)
                        .Text(op.Fecha).FontSize(8).AlignCenter();
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor("#EEEEEE").Padding(4)
                        .Text(op.NumeroOperacion).FontSize(7).FontColor("#0b5394").AlignCenter();
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor("#EEEEEE").Padding(4)
                        .Text(op.Usuario).FontSize(8).AlignCenter();
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor("#EEEEEE").Padding(4)
                        .Text(op.Descripcion).FontSize(8);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor("#EEEEEE").Padding(4)
                        .Text(op.CantidadDivisa).FontSize(8).FontColor("#0b5394").Bold().AlignCenter();
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor("#EEEEEE").Padding(4)
                        .Text(op.CantidadPagada).FontSize(8).FontColor("#008800").AlignCenter();
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor("#EEEEEE").Padding(4)
                        .Text(op.Cliente).FontSize(8);
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor("#EEEEEE").Padding(4)
                        .Text(op.TipoDocumento).FontSize(7).AlignCenter();
                    table.Cell().Background(bgColor).BorderBottom(1).BorderColor("#EEEEEE").Padding(4)
                        .Text(op.NumeroDocumento).FontSize(7).AlignCenter();
                }
            });
            
            column.Item().PaddingTop(15);
            
            // Resumen
            column.Item().Background("#F8F8F8").Border(1).BorderColor("#E0E0E0").Padding(12).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("RESUMEN").FontSize(12).Bold().FontColor("#0b5394");
                });
                
                row.ConstantItem(400).AlignRight().Row(resumen =>
                {
                    // Total operaciones
                    resumen.ConstantItem(120).Column(col =>
                    {
                        col.Item().Text("Total Operaciones").FontSize(9).FontColor("#666666").AlignCenter();
                        col.Item().PaddingTop(4).Background("#0b5394").Padding(8)
                            .Text(datos.TotalOperaciones.ToString()).FontSize(12).Bold().FontColor(Colors.White).AlignCenter();
                    });
                    
                    resumen.ConstantItem(10);
                    
                    // Total euros
                    resumen.ConstantItem(120).Column(col =>
                    {
                        col.Item().Text("Total Euros").FontSize(9).FontColor("#666666").AlignCenter();
                        col.Item().PaddingTop(4).Background("#008800").Padding(8)
                            .Text($"{datos.TotalEuros:N2}").FontSize(12).Bold().FontColor(Colors.White).AlignCenter();
                    });
                    
                    resumen.ConstantItem(10);
                    
                    // Total divisas
                    resumen.ConstantItem(120).Column(col =>
                    {
                        col.Item().Text("Total Divisas").FontSize(9).FontColor("#666666").AlignCenter();
                        col.Item().PaddingTop(4).Border(2).BorderColor("#0b5394").Background(Colors.White).Padding(8)
                            .Text($"{datos.TotalDivisas:N2}").FontSize(12).Bold().FontColor("#0b5394").AlignCenter();
                    });
                });
            });
        });
    }
    
    private static void ComposeFooter(IContainer container, DatosReporteOperaciones datos)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.Span("AllvaSystem - ").FontSize(8).FontColor("#888888");
                text.Span($"Generado el {datos.FechaGeneracion} a las {datos.HoraGeneracion}").FontSize(8).FontColor("#888888");
            });
            
            row.ConstantItem(100).AlignRight().Text(text =>
            {
                text.CurrentPageNumber().FontSize(8);
                text.Span(" / ").FontSize(8);
                text.TotalPages().FontSize(8);
            });
        });
    }
}