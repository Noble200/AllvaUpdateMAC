using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Allva.Desktop.ViewModels;
using Allva.Desktop.Helpers;
using Allva.Desktop.Services;

namespace Allva.Desktop.Views.PanelDivisas;

public partial class NuevoClienteView : UserControl
{
#if WINDOWS
    private ScannerService? _scannerService;
#endif
    private bool _escaneoParaFrontal = true;

    public NuevoClienteView()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        // Conectar eventos de botones de imagen - Seleccionar
        var btnFrontal = this.FindControl<Button>("BtnSeleccionarFrontal");
        var btnTrasera = this.FindControl<Button>("BtnSeleccionarTrasera");

        if (btnFrontal != null)
            btnFrontal.Click += OnSeleccionarImagenFrontalClick;
        if (btnTrasera != null)
            btnTrasera.Click += OnSeleccionarImagenTraseraClick;

        // Conectar eventos de botones de imagen - Escanear
        var btnEscanearFrontal = this.FindControl<Button>("BtnEscanearFrontal");
        var btnEscanearTrasera = this.FindControl<Button>("BtnEscanearTrasera");

        if (btnEscanearFrontal != null)
            btnEscanearFrontal.Click += OnEscanearFrontalClick;
        if (btnEscanearTrasera != null)
            btnEscanearTrasera.Click += OnEscanearTraseraClick;

        // Conectar eventos para formateo de fechas
        TextBoxFormatHelper.ConfigurarFormatoFecha(this.FindControl<TextBox>("TxtFechaNacimiento"));
        TextBoxFormatHelper.ConfigurarFormatoFecha(this.FindControl<TextBox>("TxtFechaCaducidad"));

        // Conectar eventos para formateo de telefono
        TextBoxFormatHelper.ConfigurarFormatoTelefono(this.FindControl<TextBox>("TxtTelefono"));
    }

    private CurrencyExchangePanelViewModel? ViewModel => DataContext as CurrencyExchangePanelViewModel;

    private async void OnSeleccionarImagenFrontalClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar imagen frontal del documento",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imagenes") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } }
            }
        });

        if (files.Count > 0)
        {
            await using var stream = await files[0].OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            ViewModel?.SetImagenDocumentoFrontal(memoryStream.ToArray());
        }
    }

    private async void OnSeleccionarImagenTraseraClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleccionar imagen trasera del documento",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imagenes") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } }
            }
        });

        if (files.Count > 0)
        {
            await using var stream = await files[0].OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            ViewModel?.SetImagenDocumentoTrasera(memoryStream.ToArray());
        }
    }

    private async void OnEscanearFrontalClick(object? sender, RoutedEventArgs e)
    {
        _escaneoParaFrontal = true;
        await IniciarEscaneoAsync();
    }

    private async void OnEscanearTraseraClick(object? sender, RoutedEventArgs e)
    {
        _escaneoParaFrontal = false;
        await IniciarEscaneoAsync();
    }

    private async System.Threading.Tasks.Task IniciarEscaneoAsync()
    {
#if WINDOWS
        try
        {
            // Inicializar servicio si es necesario
            if (_scannerService == null)
            {
                _scannerService = new ScannerService();
                _scannerService.ErrorOcurrido += OnErrorEscaneo;
                _scannerService.EstadoCambiado += OnEstadoEscaneo;
            }

            // Intentar inicializar TWAIN
            bool twainOk = _scannerService.Inicializar();

            // Verificar si hay algun escaner disponible (TWAIN o WIA)
            if (!_scannerService.HayEscaneresDisponibles())
            {
                MostrarError("No se encontraron escaneres conectados (ni por cable ni inalambricos). " +
                             "Conecte un escaner e intente de nuevo.");
                return;
            }

            // Intentar seleccionar escaner TWAIN si hay disponibles
            bool twainSeleccionado = false;
            if (twainOk && _scannerService.HayEscaneresTwain())
            {
                twainSeleccionado = _scannerService.SeleccionarEscanerPredeterminado();
            }

            // Escanear: usa TWAIN si hay, sino WIA como fallback
            MostrarEstado("Escaneando documento...");

            var imagen = await _scannerService.EscanearAsync(mostrarUI: true);

            if (imagen != null && imagen.Length > 0)
            {
                if (_escaneoParaFrontal)
                {
                    ViewModel?.SetImagenDocumentoFrontal(imagen);
                }
                else
                {
                    ViewModel?.SetImagenDocumentoTrasera(imagen);
                }
                LimpiarEstado();
            }
            else
            {
                LimpiarEstado();
            }
        }
        catch (Exception ex)
        {
            MostrarError($"Error inesperado: {ex.Message}");
        }
#else
        await System.Threading.Tasks.Task.CompletedTask;
        MostrarError("El escaneo de documentos no esta disponible en este sistema operativo. Use la opcion de seleccionar imagen.");
#endif
    }

#if WINDOWS
    private void OnErrorEscaneo(object? sender, string mensaje)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            MostrarError(mensaje);
        });
    }

    private void OnEstadoEscaneo(object? sender, string mensaje)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            MostrarEstado(mensaje);
        });
    }
#endif

    private void MostrarError(string mensaje)
    {
        if (ViewModel != null)
        {
            ViewModel.ErrorMessage = mensaje;
        }
    }

    private void MostrarEstado(string mensaje)
    {
        // Mostrar como info, no como error
        if (ViewModel != null)
        {
            ViewModel.ErrorMessage = mensaje;
        }
    }

    private void LimpiarEstado()
    {
        if (ViewModel != null)
        {
            ViewModel.ErrorMessage = "";
        }
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
#if WINDOWS
        // Liberar recursos del escaner al salir de la vista
        if (_scannerService != null)
        {
            _scannerService.ErrorOcurrido -= OnErrorEscaneo;
            _scannerService.EstadoCambiado -= OnEstadoEscaneo;
            _scannerService.Dispose();
            _scannerService = null;
        }
#endif
    }
}
