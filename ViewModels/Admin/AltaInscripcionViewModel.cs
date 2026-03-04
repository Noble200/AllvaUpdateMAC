using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Services;
using ClosedXML.Excel;

namespace Allva.Desktop.ViewModels.Admin;

public partial class AltaInscripcionViewModel : ObservableObject
{
    private readonly ActivacionLocalesService _activacionService;

    [ObservableProperty]
    private ObservableCollection<ComercioInfo> _comercios = new();

    [ObservableProperty]
    private ObservableCollection<LocalInfo> _locales = new();

    [ObservableProperty]
    private ComercioInfo? _comercioSeleccionado;

    [ObservableProperty]
    private LocalInfo? _localSeleccionado;

    [ObservableProperty]
    private ObservableCollection<LocalInfo> _localesFiltrados = new();

    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private bool _procesando;

    [ObservableProperty]
    private string _mensaje = string.Empty;

    [ObservableProperty]
    private bool _mostrarMensaje;

    [ObservableProperty]
    private string _colorMensaje = "#28a745";

    [ObservableProperty]
    private List<ActivacionLocalData>? _ultimasActivacionesGeneradas;

    public bool MostrarResultados => UltimasActivacionesGeneradas != null && UltimasActivacionesGeneradas.Count > 0;

    partial void OnUltimasActivacionesGeneradasChanged(List<ActivacionLocalData>? value)
    {
        OnPropertyChanged(nameof(MostrarResultados));
    }

    [ObservableProperty]
    private bool _modoComercioCompleto = true;

    public string TextoBotonCambiarModo => ModoComercioCompleto ? "Cambiar a Local Único" : "Cambiar a Comercio Completo";

    partial void OnModoComercioCompletoChanged(bool value)
    {
        OnPropertyChanged(nameof(TextoBotonCambiarModo));
    }

    public AltaInscripcionViewModel()
    {
        _activacionService = new ActivacionLocalesService();
        _ = CargarDatosIniciales();
    }

    private async Task CargarDatosIniciales()
    {
        Cargando = true;

        try
        {
            var comercios = await _activacionService.ObtenerComerciosAsync();
            Comercios.Clear();
            foreach (var comercio in comercios)
            {
                Comercios.Add(comercio);
            }

            var locales = await _activacionService.ObtenerTodosLosLocalesAsync();
            Locales.Clear();
            foreach (var local in locales)
            {
                Locales.Add(local);
            }
        }
        catch (Exception ex)
        {
            MostrarError($"Error al cargar datos: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    partial void OnComercioSeleccionadoChanged(ComercioInfo? value)
    {
        if (value != null && ModoComercioCompleto)
        {
            _ = CargarLocalesDelComercio(value.IdComercio);
        }
    }

    partial void OnTextoBusquedaChanged(string value)
    {
        FiltrarLocales();
    }

    private void FiltrarLocales()
    {
        LocalesFiltrados.Clear();

        if (string.IsNullOrWhiteSpace(TextoBusqueda))
        {
            return;
        }

        var busqueda = TextoBusqueda.ToLower();
        var resultados = Locales
            .Where(l =>
                l.CodigoLocal.ToLower().Contains(busqueda) ||
                l.NombreLocal.ToLower().Contains(busqueda) ||
                l.NombreComercio.ToLower().Contains(busqueda))
            .Take(10)
            .ToList();

        foreach (var local in resultados)
        {
            LocalesFiltrados.Add(local);
        }
    }

    private async Task CargarLocalesDelComercio(int idComercio)
    {
        try
        {
            var locales = await _activacionService.ObtenerLocalesPorComercioAsync(idComercio);
            Locales.Clear();
            foreach (var local in locales)
            {
                Locales.Add(local);
            }
        }
        catch (Exception ex)
        {
            MostrarError($"Error al cargar locales: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GenerarActivacionComercio()
    {
        if (ComercioSeleccionado == null)
        {
            MostrarError("Por favor seleccione un comercio");
            return;
        }

        Procesando = true;
        MostrarMensaje = false;

        try
        {
            var activaciones = await _activacionService.GenerarActivacionesComercioAsync(
                ComercioSeleccionado.IdComercio,
                1 // TODO: Obtener el ID del administrador actual
            );

            if (activaciones.Count == 0)
            {
                MostrarAdvertencia("Este comercio no tiene locales registrados");
                return;
            }

            UltimasActivacionesGeneradas = activaciones;

            MostrarExito($"Se generaron {activaciones.Count} contraseñas de activación para el comercio {ComercioSeleccionado.NombreComercio}");
        }
        catch (Exception ex)
        {
            MostrarError($"Error al generar activaciones: {ex.Message}");
        }
        finally
        {
            Procesando = false;
        }
    }

    [RelayCommand]
    private async Task GenerarActivacionLocal()
    {
        if (LocalSeleccionado == null)
        {
            MostrarError("Por favor seleccione un local");
            return;
        }

        Procesando = true;
        MostrarMensaje = false;

        try
        {
            var activacion = await _activacionService.GenerarActivacionLocalAsync(
                LocalSeleccionado.IdLocal,
                1 // TODO: Obtener el ID del administrador actual
            );

            if (activacion == null)
            {
                MostrarError("No se pudo generar la activación");
                return;
            }

            UltimasActivacionesGeneradas = new List<ActivacionLocalData> { activacion };

            MostrarExito($"Contraseña generada para el local {activacion.CodigoLocal} - {activacion.NombreLocal}");
        }
        catch (Exception ex)
        {
            MostrarError($"Error al generar activación: {ex.Message}");
        }
        finally
        {
            Procesando = false;
        }
    }

    [RelayCommand]
    private async Task ExportarCSV()
    {
        if (UltimasActivacionesGeneradas == null || UltimasActivacionesGeneradas.Count == 0)
        {
            MostrarAdvertencia("No hay activaciones para exportar. Primero genere las contraseñas.");
            return;
        }

        try
        {
            var nombreArchivo = $"Activaciones_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var rutaArchivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nombreArchivo);

            var csv = new StringBuilder();
            csv.AppendLine("Código Local,Nombre Local,Nombre Comercio,Contraseña Temporal,Fecha Generación");

            foreach (var activacion in UltimasActivacionesGeneradas)
            {
                csv.AppendLine($"\"{activacion.CodigoLocal}\",\"{activacion.NombreLocal}\",\"{activacion.NombreComercio}\",\"{activacion.ContrasenaTemporal}\",\"{activacion.FechaGeneracion:yyyy-MM-dd HH:mm:ss}\"");
            }

            await File.WriteAllTextAsync(rutaArchivo, csv.ToString(), Encoding.UTF8).ConfigureAwait(false);

            MostrarExito($"Archivo CSV exportado exitosamente: {rutaArchivo}");
        }
        catch (Exception ex)
        {
            MostrarError($"Error al exportar CSV: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ExportarExcel()
    {
        if (UltimasActivacionesGeneradas == null || UltimasActivacionesGeneradas.Count == 0)
        {
            MostrarAdvertencia("No hay activaciones para exportar. Primero genere las contraseñas.");
            return;
        }

        try
        {
            var nombreArchivo = $"Activaciones_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var rutaArchivo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), nombreArchivo);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Activaciones");

                // Encabezados
                worksheet.Cell(1, 1).Value = "Código Local";
                worksheet.Cell(1, 2).Value = "Nombre Local";
                worksheet.Cell(1, 3).Value = "Nombre Comercio";
                worksheet.Cell(1, 4).Value = "Contraseña Temporal";
                worksheet.Cell(1, 5).Value = "Fecha Generación";

                // Estilo de encabezados
                var headerRange = worksheet.Range(1, 1, 1, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(11, 83, 148); // Color azul #0b5394
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Datos
                int row = 2;
                foreach (var activacion in UltimasActivacionesGeneradas)
                {
                    worksheet.Cell(row, 1).Value = activacion.CodigoLocal;
                    worksheet.Cell(row, 2).Value = activacion.NombreLocal;
                    worksheet.Cell(row, 3).Value = activacion.NombreComercio;
                    worksheet.Cell(row, 4).Value = activacion.ContrasenaTemporal;
                    worksheet.Cell(row, 5).Value = activacion.FechaGeneracion.ToString("yyyy-MM-dd HH:mm:ss");
                    row++;
                }

                // Ajustar ancho de columnas
                worksheet.Columns().AdjustToContents();

                // Bordes
                var dataRange = worksheet.Range(1, 1, row - 1, 5);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                workbook.SaveAs(rutaArchivo);
            }

            MostrarExito($"Archivo Excel exportado exitosamente: {rutaArchivo}");
        }
        catch (Exception ex)
        {
            MostrarError($"Error al exportar Excel: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CambiarModo()
    {
        ModoComercioCompleto = !ModoComercioCompleto;
        ComercioSeleccionado = null;
        LocalSeleccionado = null;
        TextoBusqueda = string.Empty;
        LocalesFiltrados.Clear();
        UltimasActivacionesGeneradas = null;
        MostrarMensaje = false;

        if (!ModoComercioCompleto)
        {
            _ = CargarDatosIniciales(); // Recargar todos los locales
        }
    }

    [RelayCommand]
    private void SeleccionarLocal(LocalInfo local)
    {
        LocalSeleccionado = local;
        TextoBusqueda = $"{local.CodigoLocal} - {local.NombreLocal}";
        LocalesFiltrados.Clear();
    }

    private void MostrarExito(string mensaje)
    {
        Mensaje = mensaje;
        ColorMensaje = "#28a745"; // Verde
        MostrarMensaje = true;
    }

    private void MostrarError(string mensaje)
    {
        Mensaje = mensaje;
        ColorMensaje = "#dc3545"; // Rojo
        MostrarMensaje = true;
    }

    private void MostrarAdvertencia(string mensaje)
    {
        Mensaje = mensaje;
        ColorMensaje = "#ffc107"; // Amarillo
        MostrarMensaje = true;
    }
}
