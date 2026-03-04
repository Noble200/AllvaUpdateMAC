using System;
using System.IO;
using System.Threading.Tasks;
using Allva.Desktop.Models;
using Allva.Desktop.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Allva.Desktop.Services;

public class ReciboDivisasPdfService
{
    public async Task<string> GenerarReciboPdfAsync(
        string numeroOperacion,
        DateTime fechaOperacion,
        ClienteModel cliente,
        string divisaOrigen,
        string nombreDivisa,
        decimal cantidadRecibida,
        decimal totalEntregado,
        decimal tasaCambio,
        int idLocal)
    {
        return await Task.Run(() =>
        {
            QuestPDF.Settings.License = LicenseType.Community;
            
            var tempDir = Path.Combine(Path.GetTempPath(), "AllvaRecibos");
            Directory.CreateDirectory(tempDir);
            
            var fileName = $"Recibo_{numeroOperacion}_{fechaOperacion:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(tempDir, fileName);
            
            var pdfBytes = GenerarPdfInterno(
                numeroOperacion, fechaOperacion,
                cliente.NombreCompleto, cliente.DocumentoCompleto,
                cliente.Telefono ?? "-", cliente.Direccion ?? "-", cliente.Nacionalidad ?? "-",
                divisaOrigen, nombreDivisa, cantidadRecibida, tasaCambio, totalEntregado,
                idLocal.ToString(), false
            );
            
            File.WriteAllBytes(filePath, pdfBytes);
            return filePath;
        });
    }
    
    // Metodo para reimpresion desde Balance de Cuentas
    public byte[] GenerarReciboPdfBytes(
        string numeroOperacion,
        DateTime fechaOperacion,
        ClienteParaRecibo cliente,
        string divisaOrigen,
        string nombreDivisa,
        decimal cantidadRecibida,
        decimal tasaCambio,
        decimal totalEntregado,
        string codigoLocal,
        bool esReimpresion = false)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        return GenerarPdfInterno(
            numeroOperacion, fechaOperacion,
            cliente.NombreCompleto, cliente.DocumentoCompleto,
            cliente.Telefono ?? "-", cliente.Direccion ?? "-", cliente.Nacionalidad ?? "-",
            divisaOrigen, nombreDivisa, cantidadRecibida, tasaCambio, totalEntregado,
            codigoLocal, esReimpresion
        );
    }
    
    private byte[] GenerarPdfInterno(
        string numeroOperacion,
        DateTime fechaOperacion,
        string clienteNombre,
        string clienteDocumento,
        string clienteTelefono,
        string clienteDireccion,
        string clienteNacionalidad,
        string divisaOrigen,
        string nombreDivisa,
        decimal cantidadRecibida,
        decimal tasaCambio,
        decimal totalEntregado,
        string codigoLocal,
        bool esReimpresion)
    {
        var pdfDocument = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));
                
                // Encabezado
                page.Header().Element(header =>
                {
                    header.Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Allva")
                                .FontSize(28)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);
                            
                            col.Item().Text("Compra de Divisas")
                                .FontSize(14)
                                .FontColor(Colors.Grey.Darken1);
                        });
                        
                        row.ConstantItem(200).AlignRight().Column(col =>
                        {
                            if (esReimpresion)
                            {
                                col.Item().Text("COPIA - REIMPRESION")
                                    .FontSize(10)
                                    .Bold()
                                    .FontColor(Colors.Red.Darken2);
                            }
                            
                            col.Item().Text("RECIBO DE OPERACION")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);
                            
                            col.Item().Text(numeroOperacion)
                                .FontSize(16)
                                .Bold();
                            
                            col.Item().Text($"Fecha: {fechaOperacion:dd/MM/yyyy HH:mm}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Darken1);
                        });
                    });
                });
                
                // Contenido
                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().PaddingBottom(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    
                    col.Item().Text("DATOS DEL CLIENTE")
                        .FontSize(12)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);
                    
                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(120);
                            columns.RelativeColumn();
                        });
                        
                        table.Cell().Text("Nombre:").Bold();
                        table.Cell().Text(clienteNombre);
                        
                        table.Cell().Text("Documento:").Bold();
                        table.Cell().Text(clienteDocumento);
                        
                        table.Cell().Text("Telefono:").Bold();
                        table.Cell().Text(clienteTelefono);
                        
                        table.Cell().Text("Direccion:").Bold();
                        table.Cell().Text(clienteDireccion);
                        
                        table.Cell().Text("Nacionalidad:").Bold();
                        table.Cell().Text(clienteNacionalidad);
                    });
                    
                    col.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    
                    col.Item().Text("DETALLE DE LA OPERACION")
                        .FontSize(12)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);
                    
                    col.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(15).Column(opCol =>
                    {
                        opCol.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Divisa recibida:").Bold();
                            row.RelativeItem().AlignRight().Text($"{divisaOrigen} - {nombreDivisa}");
                        });
                        
                        opCol.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text("Cantidad recibida:").Bold();
                            row.RelativeItem().AlignRight().Text($"{cantidadRecibida:N2} {divisaOrigen}")
                                .FontSize(14)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);
                        });
                        
                        opCol.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text("Tasa de cambio:").Bold();
                            row.RelativeItem().AlignRight().Text($"1 {divisaOrigen} = {tasaCambio:N4} EUR");
                        });
                        
                        opCol.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        opCol.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL ENTREGADO:").Bold().FontSize(14);
                            row.RelativeItem().AlignRight().Text($"{totalEntregado:N2} EUR")
                                .FontSize(18)
                                .Bold()
                                .FontColor(Colors.Green.Darken2);
                        });
                    });
                    
                    col.Item().PaddingTop(30);
                    
                    col.Item().Background(Colors.Grey.Lighten4).Padding(15).Column(aviso =>
                    {
                        aviso.Item().Text("INFORMACION IMPORTANTE")
                            .FontSize(10)
                            .Bold();
                        
                        aviso.Item().PaddingTop(5).Text(
                            "Este documento es un comprobante de la operacion de cambio de divisas realizada. " +
                            "Conserve este recibo como justificante de la transaccion. " +
                            "En caso de cualquier consulta, contacte con su oficina Allva.")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken2);
                    });
                });
                
                // Pie
                page.Footer().AlignCenter().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    
                    var textoReimpresion = esReimpresion ? " | COPIA" : "";
                    col.Item().PaddingTop(10).Text($"Allva - Sistema de Gestion Comercial | Local: {codigoLocal} | Documento generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}{textoReimpresion}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        });
        
        using var memoryStream = new MemoryStream();
        pdfDocument.GeneratePdf(memoryStream);
        return memoryStream.ToArray();
    }
}