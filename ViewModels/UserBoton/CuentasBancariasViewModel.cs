using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;
using Allva.Desktop.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Allva.Desktop.ViewModels.UserBoton;

public partial class CuentasBancariasViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CuentaBancariaConModulo> _cuentasBancarias = new();

    [ObservableProperty]
    private ObservableCollection<GrupoCuentasBancarias> _gruposCuentas = new();

    [ObservableProperty]
    private string _mensajeCopiado = "";

    [ObservableProperty]
    private bool _mostrarMensajeCopiado = false;

    // Filtro seleccionado (null = mostrar todos)
    [ObservableProperty]
    private string? _filtroModuloSeleccionado = null;

    // Visibilidad de botones de filtro segun permisos del local
    [ObservableProperty]
    private bool _mostrarFiltroDivisas = false;

    [ObservableProperty]
    private bool _mostrarFiltroAlimentos = false;

    [ObservableProperty]
    private bool _mostrarFiltroBilletes = false;

    [ObservableProperty]
    private bool _mostrarFiltroViajes = false;

    private Action? _cerrarAction;

    // Permisos del local
    private bool _moduloDivisas = false;
    private bool _moduloAlimentos = false;
    private bool _moduloBilletes = false;
    private bool _moduloViajes = false;

    // Todas las cuentas sin filtrar (para aplicar filtros)
    private List<CuentaBancaria> _todasLasCuentas = new();

    public CuentasBancariasViewModel()
    {
        ObtenerPermisosDelLocal();
        CargarCuentasBancarias();
    }

    public CuentasBancariasViewModel(Action? cerrarAction)
    {
        _cerrarAction = cerrarAction;
        ObtenerPermisosDelLocal();
        CargarCuentasBancarias();
    }

    public void SetCerrarAction(Action cerrarAction)
    {
        _cerrarAction = cerrarAction;
    }

    private void ObtenerPermisosDelLocal()
    {
        // Obtener permisos del MainDashboardViewModel
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow?.Content is MainDashboardView dashboardView &&
                dashboardView.DataContext is MainDashboardViewModel mainVm)
            {
                _moduloDivisas = mainVm.ModuloDivisasVisible;
                _moduloAlimentos = mainVm.ModuloAlimentosVisible;
                _moduloBilletes = mainVm.ModuloBilletesVisible;
                _moduloViajes = mainVm.ModuloViajesVisible;

                // Actualizar visibilidad de botones de filtro
                MostrarFiltroDivisas = _moduloDivisas;
                MostrarFiltroAlimentos = _moduloAlimentos;
                MostrarFiltroBilletes = _moduloBilletes;
                MostrarFiltroViajes = _moduloViajes;
            }
        }
    }

    [RelayCommand]
    private void FiltrarPorModulo(string? modulo)
    {
        FiltroModuloSeleccionado = modulo;
        AplicarFiltroYAgrupar();
    }

    private void CargarCuentasBancarias()
    {
        var configService = FrontOfficeConfigService.Instance;
        // Forzar recarga desde BD para obtener datos actualizados
        configService.RecargarConfiguracion();
        _todasLasCuentas = configService.ObtenerCuentasBancarias();
        AplicarFiltroYAgrupar();
    }

    private void AplicarFiltroYAgrupar()
    {
        CuentasBancarias.Clear();
        GruposCuentas.Clear();

        // Agrupar cuentas por modulo (permitiendo repeticiones)
        // Cada modulo habilitado tendra su propia seccion con sus cuentas

        // Grupo: Pack de Comida (Alimentos)
        if (_moduloAlimentos)
        {
            var cuentasAlimentos = new List<CuentaBancariaConModulo>();
            foreach (var cuenta in _todasLasCuentas)
            {
                if (cuenta.DisponiblePackAlimentos)
                {
                    var cuentaConModulo = new CuentaBancariaConModulo
                    {
                        Cuenta = cuenta,
                        NombreModulo = "Bancos - Productos",
                        ColorModulo = "#166534",
                        ColorFondoModulo = "#DCFCE7"
                    };
                    CuentasBancarias.Add(cuentaConModulo);
                    cuentasAlimentos.Add(cuentaConModulo);
                }
            }

            if (cuentasAlimentos.Any())
            {
                GruposCuentas.Add(new GrupoCuentasBancarias
                {
                    NombreGrupo = "Bancos - Productos",
                    NombreModulo = "Bancos - Productos",
                    DescripcionGrupo = "Cuentas para pedidos de productos",
                    ColorTitulo = "#166534",
                    ColorFondo = "#DCFCE7",
                    IconoPath = "M8.1 13.34l2.83-2.83L3.91 3.5c-1.56 1.56-1.56 4.09 0 5.66l4.19 4.18zm6.78-1.81c1.53.71 3.68.21 5.27-1.38 1.91-1.91 2.28-4.65.81-6.12-1.46-1.46-4.2-1.1-6.12.81-1.59 1.59-2.09 3.74-1.38 5.27L3.7 19.87l1.41 1.41L12 14.41l6.88 6.88 1.41-1.41L13.41 13l1.47-1.47z",
                    Cuentas = new ObservableCollection<CuentaBancariaConModulo>(cuentasAlimentos)
                });
            }
        }

        // Grupo: Billetes de Avion
        if (_moduloBilletes)
        {
            var cuentasBilletes = new List<CuentaBancariaConModulo>();
            foreach (var cuenta in _todasLasCuentas)
            {
                if (cuenta.DisponibleBilletesAvion)
                {
                    var cuentaConModulo = new CuentaBancariaConModulo
                    {
                        Cuenta = cuenta,
                        NombreModulo = "Bancos - Billetes de avión",
                        ColorModulo = "#3730A3",
                        ColorFondoModulo = "#E0E7FF"
                    };
                    CuentasBancarias.Add(cuentaConModulo);
                    cuentasBilletes.Add(cuentaConModulo);
                }
            }

            if (cuentasBilletes.Any())
            {
                GruposCuentas.Add(new GrupoCuentasBancarias
                {
                    NombreGrupo = "Bancos - Billetes de avión",
                    NombreModulo = "Bancos - Billetes de avión",
                    DescripcionGrupo = "Cuentas para compra de billetes de avion",
                    ColorTitulo = "#3730A3",
                    ColorFondo = "#E0E7FF",
                    IconoPath = "M21 16v-2l-8-5V3.5c0-.83-.67-1.5-1.5-1.5S10 2.67 10 3.5V9l-8 5v2l8-2.5V19l-2 1.5V22l3.5-1 3.5 1v-1.5L13 19v-5.5l8 2.5z",
                    Cuentas = new ObservableCollection<CuentaBancariaConModulo>(cuentasBilletes)
                });
            }
        }

        // Grupo: Compra de Divisas
        if (_moduloDivisas)
        {
            var cuentasDivisas = new List<CuentaBancariaConModulo>();
            foreach (var cuenta in _todasLasCuentas)
            {
                if (cuenta.DisponibleCompraDivisas)
                {
                    var cuentaConModulo = new CuentaBancariaConModulo
                    {
                        Cuenta = cuenta,
                        NombreModulo = "Bancos - Compra de Divisas",
                        ColorModulo = "#92400E",
                        ColorFondoModulo = "#FEF3C7"
                    };
                    CuentasBancarias.Add(cuentaConModulo);
                    cuentasDivisas.Add(cuentaConModulo);
                }
            }

            if (cuentasDivisas.Any())
            {
                GruposCuentas.Add(new GrupoCuentasBancarias
                {
                    NombreGrupo = "Bancos - Compra de Divisas",
                    NombreModulo = "Bancos - Compra de Divisas",
                    DescripcionGrupo = "Cuentas para operaciones de compra de divisas",
                    ColorTitulo = "#92400E",
                    ColorFondo = "#FEF3C7",
                    IconoPath = "M12 8c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm9-5H3c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h18c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-1 16H4c-.55 0-1-.45-1-1V8h18v10c0 .55-.45 1-1 1z",
                    Cuentas = new ObservableCollection<CuentaBancariaConModulo>(cuentasDivisas)
                });
            }
        }

        // Grupo: Packs de Viajes
        if (_moduloViajes)
        {
            var cuentasViajes = new List<CuentaBancariaConModulo>();
            foreach (var cuenta in _todasLasCuentas)
            {
                if (cuenta.DisponiblePackViajes)
                {
                    var cuentaConModulo = new CuentaBancariaConModulo
                    {
                        Cuenta = cuenta,
                        NombreModulo = "Bancos - Pack de Viajes",
                        ColorModulo = "#1E40AF",
                        ColorFondoModulo = "#DBEAFE"
                    };
                    CuentasBancarias.Add(cuentaConModulo);
                    cuentasViajes.Add(cuentaConModulo);
                }
            }

            if (cuentasViajes.Any())
            {
                GruposCuentas.Add(new GrupoCuentasBancarias
                {
                    NombreGrupo = "Bancos - Pack de Viajes",
                    NombreModulo = "Bancos - Pack de Viajes",
                    DescripcionGrupo = "Cuentas para reservas de packs de viajes",
                    ColorTitulo = "#1E40AF",
                    ColorFondo = "#DBEAFE",
                    IconoPath = "M20.5 3l-.16.03L15 5.1 9 3 3.36 4.9c-.21.07-.36.25-.36.48V20.5c0 .28.22.5.5.5l.16-.03L9 18.9l6 2.1 5.64-1.9c.21-.07.36-.25.36-.48V3.5c0-.28-.22-.5-.5-.5zM15 19l-6-2.11V5l6 2.11V19z",
                    Cuentas = new ObservableCollection<CuentaBancariaConModulo>(cuentasViajes)
                });
            }
        }
    }

    [RelayCommand]
    private async Task CopiarIBAN(CuentaBancariaConModulo? cuenta)
    {
        if (cuenta == null) return;
        await CopiarAlPortapapeles(cuenta.IBANSinEspacios);
        await MostrarNotificacionAsync("IBAN copiado");
    }

    [RelayCommand]
    private async Task CopiarBanco(CuentaBancariaConModulo? cuenta)
    {
        if (cuenta == null) return;
        await CopiarAlPortapapeles(cuenta.NombreBanco);
        await MostrarNotificacionAsync("Banco copiado");
    }

    [RelayCommand]
    private async Task CopiarTitular(CuentaBancariaConModulo? cuenta)
    {
        if (cuenta == null) return;
        await CopiarAlPortapapeles(cuenta.Titular);
        await MostrarNotificacionAsync("Titular copiado");
    }

    [RelayCommand]
    private async Task CopiarTodo(CuentaBancariaConModulo? cuenta)
    {
        if (cuenta == null) return;
        await CopiarAlPortapapeles(cuenta.TextoCompleto);
        await MostrarNotificacionAsync("Datos copiados");
    }

    private async Task CopiarAlPortapapeles(string texto)
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var clipboard = desktop.MainWindow?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(texto);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al copiar: {ex.Message}");
        }
    }

    private async Task MostrarNotificacionAsync(string mensaje)
    {
        MensajeCopiado = mensaje;
        MostrarMensajeCopiado = true;
        await Task.Delay(2000);
        MostrarMensajeCopiado = false;
    }

    [RelayCommand]
    private async Task ExportarPDF()
    {
        try
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow == null) return;

                var storageProvider = mainWindow.StorageProvider;

                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Guardar Cuentas Bancarias",
                    DefaultExtension = "pdf",
                    SuggestedFileName = $"CuentasBancarias_{DateTime.Now:yyyyMMdd}.pdf",
                    FileTypeChoices = new List<FilePickerFileType>
                    {
                        new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
                    }
                });

                if (file != null)
                {
                    var path = file.Path.LocalPath;
                    GenerarPDF(path);
                    await MostrarNotificacionAsync("PDF exportado");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al exportar PDF: {ex.Message}");
        }
    }

    private void GenerarPDF(string rutaArchivo)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Pagina ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(rutaArchivo);
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("CUENTAS BANCARIAS")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken3);

                    col.Item().Text($"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Medium);
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            // Mostrar las cuentas agrupadas por panel/modulo
            foreach (var grupo in GruposCuentas)
            {
                // Titulo del panel con fondo azul y linea negra
                column.Item().PaddingTop(10).Background(Colors.Blue.Darken3)
                    .BorderBottom(2).BorderColor(Colors.Black)
                    .Padding(10)
                    .Text(grupo.NombreModulo)
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.White);

                // Cuentas del panel en formato tabla
                column.Item().Border(1).BorderColor(Colors.Blue.Darken3)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1); // Banco
                            columns.RelativeColumn(2); // IBAN
                        });

                        foreach (var cuenta in grupo.Cuentas)
                        {
                            // Fila: Banco
                            table.Cell().BorderBottom(1).BorderRight(1).BorderColor(Colors.Grey.Lighten2)
                                .Padding(10)
                                .Text(cuenta.NombreBanco)
                                .FontSize(12)
                                .FontColor(Colors.Grey.Darken2);

                            // Fila: IBAN
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .Padding(10)
                                .Text(cuenta.IBAN)
                                .FontFamily("Courier")
                                .FontSize(12)
                                .Bold()
                                .FontColor(Colors.Blue.Darken3);
                        }
                    });

                column.Item().PaddingBottom(15);
            }

            if (!GruposCuentas.Any())
            {
                column.Item().PaddingTop(20).AlignCenter()
                    .Text("No hay cuentas bancarias disponibles")
                    .FontColor(Colors.Grey.Medium);
            }
        });
    }

    [RelayCommand]
    private void Cerrar()
    {
        _cerrarAction?.Invoke();
    }

    [RelayCommand]
    private void Recargar()
    {
        FrontOfficeConfigService.Instance.RecargarConfiguracion();
        CargarCuentasBancarias();
    }
}
