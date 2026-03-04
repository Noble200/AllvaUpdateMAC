using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Services;
using Allva.Desktop.Views;
using Allva.Desktop.Views.MenuHamburguesa;

namespace Allva.Desktop.ViewModels;

public partial class OperacionesViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    
    private readonly int _idComercio;
    private readonly int _idLocal;
    private readonly string _codigoLocal;
    private readonly int _idUsuario;
    private readonly string _nombreUsuario;
    
    private static readonly TimeZoneInfo _zonaHorariaEspana = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Romance Standard Time" : "Europe/Madrid");

    // Evento para volver al inicio
    public event Action? OnVolverAInicio;
    
    [ObservableProperty]
    private string localInfo = "";
    
    [ObservableProperty]
    private string panelActual = "divisa";
    
    [ObservableProperty]
    private string fechaDesdeTexto = "";
    
    [ObservableProperty]
    private string fechaHastaTexto = "";

    // ============================================
    // FILTRO N° OPERACIÓN DIVISAS (Desde/Hasta)
    // ============================================

    [ObservableProperty]
    private string filtroOperacionDivisaDesde = "";

    [ObservableProperty]
    private string filtroOperacionDivisaHasta = "";

    [ObservableProperty]
    private string tipoOperacionFiltro = "Todas";

    public ObservableCollection<string> TiposOperacion { get; } = new()
    {
        "Todas",
        "Compra divisa",
        "Traspaso",
        "Venta Divisa"
    };

    // ============================================
    // FILTRO DIVISA (autocompletado)
    // ============================================

    [ObservableProperty]
    private string filtroDivisa = "Todas";

    [ObservableProperty]
    private string textoBusquedaDivisa = "";

    [ObservableProperty]
    private bool mostrarListaDivisas = false;

    public ObservableCollection<DivisaFiltroItem> DivisasFiltradas { get; } = new();
    public ObservableCollection<DivisaFiltroItem> DivisasDisponibles { get; } = new();

    // ============================================
    // FILTROS PACK ALIMENTOS
    // ============================================

    [ObservableProperty]
    private string filtroPaisDestino = "Todos";

    [ObservableProperty]
    private string filtroEstadoOperacion = "Todos";

    // Propiedad para autocompletado de país destino
    [ObservableProperty]
    private string textoBusquedaPaisDestino = "";

    [ObservableProperty]
    private bool mostrarListaPaises = false;

    public ObservableCollection<string> PaisesFiltrados { get; } = new();

    // ============================================
    // FILTRO N° OPERACIÓN PACK ALIMENTOS (Desde/Hasta)
    // ============================================

    [ObservableProperty]
    private string filtroOperacionAlimentosDesde = "";

    [ObservableProperty]
    private string filtroOperacionAlimentosHasta = "";

    // Propiedad para ordenamiento
    [ObservableProperty]
    private bool ordenAscendente = true; // true = más viejo primero (ASC), false = más reciente primero (DESC)

    public string IconoOrden => OrdenAscendente ? "▲" : "▼";
    public string TooltipOrden => OrdenAscendente ? "Más antiguo primero (clic para cambiar)" : "Más reciente primero (clic para cambiar)";

    public ObservableCollection<string> PaisesDestinoDisponibles { get; } = new() { "Todos" };

    public ObservableCollection<string> EstadosOperacionDisponibles { get; } = new()
    {
        "Todos",
        "PENDIENTE",
        "PAGADO",
        "ENVIADO",
        "ENTREGADO",
        "ANULADO"
    };

    // ============================================
    // PROPIEDADES DE VISIBILIDAD
    // ============================================

    public bool EsPanelDivisa => PanelActual == "divisa";
    public bool EsPanelAlimentos => PanelActual == "alimentos";
    public bool EsPanelBilletes => PanelActual == "billetes";
    public bool EsPanelViaje => PanelActual == "viaje";

    // Colores de tabs
    public string TabDivisaBackground => EsPanelDivisa ? "#ffd966" : "White";
    public string TabDivisaForeground => EsPanelDivisa ? "#0b5394" : "#595959";
    public string TabAlimentosBackground => EsPanelAlimentos ? "#ffd966" : "White";
    public string TabAlimentosForeground => EsPanelAlimentos ? "#0b5394" : "#595959";
    public string TabBilletesBackground => EsPanelBilletes ? "#ffd966" : "White";
    public string TabBilletesForeground => EsPanelBilletes ? "#0b5394" : "#595959";
    public string TabViajeBackground => EsPanelViaje ? "#ffd966" : "White";
    public string TabViajeForeground => EsPanelViaje ? "#0b5394" : "#595959";

    // ============================================
    // COLECCIONES
    // ============================================
    
    [ObservableProperty]
    private string fechaActualTexto = "";
    
    [ObservableProperty]
    private string totalOperaciones = "0";
    
    [ObservableProperty]
    private string totalEurosMovidos = "0.00";
    
    [ObservableProperty]
    private string totalDivisasMovidas = "0.00";

    // Resumen Pack Alimentos
    [ObservableProperty]
    private string totalPendientes = "0";

    [ObservableProperty]
    private string totalPagados = "0";

    [ObservableProperty]
    private string totalEnviados = "0";

    [ObservableProperty]
    private string totalAnulados = "0";

    [ObservableProperty]
    private string totalImporteAlimentos = "0.00";

    [ObservableProperty]
    private string totalImporteAlimentosColor = "#0b5394";
    
    [ObservableProperty]
    private bool isLoading = false;
    
    [ObservableProperty]
    private string errorMessage = "";
    
    [ObservableProperty]
    private string successMessage = "";

    [ObservableProperty]
    private bool mostrarFiltros = true;
    
    public ObservableCollection<OperacionDetalleItem> Operaciones { get; } = new();
    public ObservableCollection<OperacionPackAlimentoItem> OperacionesAlimentos { get; } = new();
    
    private readonly string[] _mesesEspanol = { 
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" 
    };
    
    private readonly string[] _diasSemana = {
        "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado"
    };
    
    public OperacionesViewModel()
    {
        _idComercio = 0;
        _idLocal = 0;
        _codigoLocal = "---";
        _idUsuario = 0;
        _nombreUsuario = "Usuario";
        LocalInfo = $"(Oficina - {_codigoLocal})";
        InicializarFechas();
        CargarDivisasDisponibles();
    }

    public OperacionesViewModel(int idComercio, int idLocal, string codigoLocal)
    {
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        _idUsuario = 0;
        _nombreUsuario = "Usuario";
        LocalInfo = $"(Oficina - {_codigoLocal})";

        InicializarFechas();
        CargarDivisasDisponibles();
        _ = CargarDatosAsync();
    }

    public OperacionesViewModel(int idComercio, int idLocal, string codigoLocal, int idUsuario, string nombreUsuario)
    {
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        _idUsuario = idUsuario;
        _nombreUsuario = nombreUsuario;
        LocalInfo = $"(Oficina - {_codigoLocal})";

        InicializarFechas();
        CargarDivisasDisponibles();
        _ = CargarDatosAsync();
    }
    
    private DateTime ObtenerHoraEspana()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaHorariaEspana);
    }

    private void InicializarFechas()
    {
        var hoy = ObtenerHoraEspana();
        FechaActualTexto = FormatearFechaCompleta(hoy);
        // Fechas en blanco por defecto - se muestran las últimas 20 operaciones sin filtro de fecha
        FechaDesdeTexto = "";
        FechaHastaTexto = "";
    }
    
    private string FormatearFechaCompleta(DateTime fecha)
    {
        var diaSemana = _diasSemana[(int)fecha.DayOfWeek];
        var mes = _mesesEspanol[fecha.Month - 1];
        return $"{diaSemana}, {fecha.Day} de {mes} de {fecha.Year}";
    }
    
    private DateTime? ParsearFecha(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        
        if (DateTime.TryParseExact(texto, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return fecha;
        if (DateTime.TryParseExact(texto, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha;
        if (DateTime.TryParseExact(texto, "d/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha;
        if (DateTime.TryParseExact(texto, "dd/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha))
            return fecha;
            
        return null;
    }
    
    private async Task CargarDatosAsync()
    {
        // Al entrar al panel, cargar las últimas 20 operaciones sin filtros de fecha
        await CargarUltimas20OperacionesAsync();
        ActualizarResumen();
    }
    
    [RelayCommand]
    private void CambiarPanel(string panel)
    {
        PanelActual = panel;
        OnPropertyChanged(nameof(EsPanelDivisa));
        OnPropertyChanged(nameof(EsPanelAlimentos));
        OnPropertyChanged(nameof(EsPanelBilletes));
        OnPropertyChanged(nameof(EsPanelViaje));
        
        // Actualizar colores de tabs
        OnPropertyChanged(nameof(TabDivisaBackground));
        OnPropertyChanged(nameof(TabDivisaForeground));
        OnPropertyChanged(nameof(TabAlimentosBackground));
        OnPropertyChanged(nameof(TabAlimentosForeground));
        OnPropertyChanged(nameof(TabBilletesBackground));
        OnPropertyChanged(nameof(TabBilletesForeground));
        OnPropertyChanged(nameof(TabViajeBackground));
        OnPropertyChanged(nameof(TabViajeForeground));

        if (panel == "alimentos")
        {
            _ = CargarPaisesDestinoAsync();
        }
        else if (panel == "divisa")
        {
            CargarDivisasDisponibles();
        }

        // Al cambiar de panel, cargar las últimas 20 operaciones
        _ = CargarUltimas20OperacionesAsync();
    }
    
    [RelayCommand]
    private async Task BuscarAsync()
    {
        await CargarOperacionesAsync();
        ActualizarResumen();
    }
    
    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        // Fechas en blanco - se muestran las últimas 20 operaciones sin filtro de fecha
        FechaDesdeTexto = "";
        FechaHastaTexto = "";
        TipoOperacionFiltro = "Todas";

        // Filtro número operación divisas - limpiar
        FiltroOperacionDivisaDesde = "";
        FiltroOperacionDivisaHasta = "";

        // Filtro divisa - limpiar todo
        FiltroDivisa = "Todas";
        TextoBusquedaDivisa = "";
        MostrarListaDivisas = false;
        DivisasFiltradas.Clear();

        // Filtros pack alimentos - limpiar todo
        FiltroPaisDestino = "Todos";
        TextoBusquedaPaisDestino = "";
        MostrarListaPaises = false;
        PaisesFiltrados.Clear();
        FiltroEstadoOperacion = "Todos";
        FiltroOperacionAlimentosDesde = "";
        FiltroOperacionAlimentosHasta = "";

        // Resetear ordenamiento a más antiguo primero
        OrdenAscendente = true;
        OnPropertyChanged(nameof(IconoOrden));
        OnPropertyChanged(nameof(TooltipOrden));

        // Cargar las últimas 20 operaciones (sin filtros de fecha)
        await CargarUltimas20OperacionesAsync();
    }

    [RelayCommand]
    private async Task CambiarOrdenAsync()
    {
        OrdenAscendente = !OrdenAscendente;
        OnPropertyChanged(nameof(IconoOrden));
        OnPropertyChanged(nameof(TooltipOrden));
        await CargarOperacionesAsync();
    }

    partial void OnTextoBusquedaPaisDestinoChanged(string value)
    {
        FiltrarPaises(value);
        MostrarListaPaises = PaisesFiltrados.Count > 0;
    }

    private void FiltrarPaises(string texto)
    {
        PaisesFiltrados.Clear();

        if (string.IsNullOrWhiteSpace(texto))
        {
            // No mostrar lista cuando el campo está vacío
            return;
        }

        var filtrados = PaisesDestinoDisponibles
            .Where(p => p.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var pais in filtrados)
        {
            PaisesFiltrados.Add(pais);
        }
    }

    [RelayCommand]
    private void SeleccionarPaisDestino(string pais)
    {
        if (!string.IsNullOrEmpty(pais))
        {
            FiltroPaisDestino = pais;
            TextoBusquedaPaisDestino = pais == "Todos" ? "" : pais;
            MostrarListaPaises = false;
            PaisesFiltrados.Clear();
        }
    }

    // ============================================
    // AUTOCOMPLETADO DIVISA
    // ============================================

    partial void OnTextoBusquedaDivisaChanged(string value)
    {
        FiltrarDivisas(value);
        MostrarListaDivisas = DivisasFiltradas.Count > 0;
    }

    private void FiltrarDivisas(string texto)
    {
        DivisasFiltradas.Clear();

        if (string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        var textoNormalizado = RemoverAcentos(texto.ToLowerInvariant());

        var filtradas = DivisasDisponibles
            .Where(d => RemoverAcentos(d.Codigo.ToLowerInvariant()).Contains(textoNormalizado) ||
                        RemoverAcentos(d.Nombre.ToLowerInvariant()).Contains(textoNormalizado) ||
                        RemoverAcentos(d.Pais.ToLowerInvariant()).Contains(textoNormalizado))
            .Take(8)
            .ToList();

        foreach (var divisa in filtradas)
        {
            DivisasFiltradas.Add(divisa);
        }
    }

    private static string RemoverAcentos(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;

        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    [RelayCommand]
    private void SeleccionarDivisa(DivisaFiltroItem? divisa)
    {
        if (divisa != null)
        {
            FiltroDivisa = divisa.Codigo;
            TextoBusquedaDivisa = divisa.EsTodas ? "" : divisa.Codigo;
            MostrarListaDivisas = false;
            DivisasFiltradas.Clear();
        }
    }

    private void CargarDivisasDisponibles()
    {
        DivisasDisponibles.Clear();

        // Agregar opción "Todas"
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "Todas", Nombre = "Todas las divisas", Pais = "", EsTodas = true });

        // Divisas disponibles en el panel de cambio de divisas (27 divisas)
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "USD", Nombre = "Dólar USA", Pais = "Estados Unidos" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "CAD", Nombre = "Dólar Canadiense", Pais = "Canadá" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "AUD", Nombre = "Dólar Australiano", Pais = "Australia" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "CHF", Nombre = "Franco Suizo", Pais = "Suiza" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "GBP", Nombre = "Libra Esterlina", Pais = "Reino Unido" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "HKD", Nombre = "Dólar Hong Kong", Pais = "Hong Kong" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "AED", Nombre = "Dírham EAU", Pais = "Emiratos Árabes" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "BGN", Nombre = "Lev Búlgaro", Pais = "Bulgaria" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "BRL", Nombre = "Real Brasileño", Pais = "Brasil" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "CNY", Nombre = "Yuan Renminbi", Pais = "China" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "CZK", Nombre = "Corona Checa", Pais = "República Checa" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "HUF", Nombre = "Forint", Pais = "Hungría" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "JPY", Nombre = "Yen Japonés", Pais = "Japón" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "MAD", Nombre = "Dírham Marroquí", Pais = "Marruecos" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "MXN", Nombre = "Peso Mexicano", Pais = "México" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "NOK", Nombre = "Corona Noruega", Pais = "Noruega" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "NZD", Nombre = "Dólar Neozelandés", Pais = "Nueva Zelanda" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "PLN", Nombre = "Zloty", Pais = "Polonia" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "RON", Nombre = "Leu Rumano", Pais = "Rumanía" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "RUB", Nombre = "Rublo Ruso", Pais = "Rusia" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "SAR", Nombre = "Rial Saudí", Pais = "Arabia Saudita" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "SEK", Nombre = "Corona Sueca", Pais = "Suecia" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "SGD", Nombre = "Dólar Singapur", Pais = "Singapur" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "THB", Nombre = "Baht Tailandés", Pais = "Tailandia" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "TND", Nombre = "Dinar Tunecino", Pais = "Túnez" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "TRY", Nombre = "Lira Turca", Pais = "Turquía" });
        DivisasDisponibles.Add(new DivisaFiltroItem { Codigo = "ZAR", Nombre = "Rand Sudafricano", Pais = "Sudáfrica" });
    }

    [RelayCommand]
    private async Task ImprimirHistorialAsync()
    {
        try
        {
            var ahora = ObtenerHoraEspana();
            
            var confirmacionVM = new ConfirmacionOperacionesViewModel
            {
                FechaGeneracion = ahora.ToString("dd/MM/yyyy"),
                HoraGeneracion = ahora.ToString("HH:mm:ss"),
                NombreUsuario = _nombreUsuario,
                CodigoLocal = _codigoLocal,
                TotalOperaciones = TotalOperaciones,
                TotalEuros = TotalEurosMovidos,
                TotalDivisas = TotalDivisasMovidas,
                PanelSeleccionado = ObtenerNombrePanel(PanelActual)
            };
            
            if (!string.IsNullOrWhiteSpace(FechaDesdeTexto))
                confirmacionVM.FiltrosAplicados.Add(new FiltroOperacionItem { Nombre = "Fecha desde:", Valor = FechaDesdeTexto });
            if (!string.IsNullOrWhiteSpace(FechaHastaTexto))
                confirmacionVM.FiltrosAplicados.Add(new FiltroOperacionItem { Nombre = "Fecha hasta:", Valor = FechaHastaTexto });
            if (!string.IsNullOrWhiteSpace(FiltroOperacionDivisaDesde))
                confirmacionVM.FiltrosAplicados.Add(new FiltroOperacionItem { Nombre = "Operación desde:", Valor = FiltroOperacionDivisaDesde });
            if (!string.IsNullOrWhiteSpace(FiltroOperacionDivisaHasta))
                confirmacionVM.FiltrosAplicados.Add(new FiltroOperacionItem { Nombre = "Operación hasta:", Valor = FiltroOperacionDivisaHasta });
            
            confirmacionVM.SinFiltros = confirmacionVM.FiltrosAplicados.Count == 0;
            
            var ventanaConfirmacion = new ConfirmacionOperacionesView(confirmacionVM);
            
            Window? mainWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
            }
            
            if (mainWindow != null)
            {
                await ventanaConfirmacion.ShowDialog(mainWindow);
            }
            else
            {
                ventanaConfirmacion.Show();
                await Task.Delay(100);
                while (ventanaConfirmacion.IsVisible)
                    await Task.Delay(100);
            }
            
            if (ventanaConfirmacion.Confirmado)
            {
                await GenerarPdfOperaciones(ahora);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
    }
    
    private string ObtenerNombrePanel(string panel)
    {
        return panel switch
        {
            "divisa" => "Divisas",
            "billetes" => "Billetes de Avión",
            "viaje" => "Pack de Viaje",
            "alimentos" => "Pack de Alimentos",
            _ => "Todos"
        };
    }
    
    private async Task GenerarPdfOperaciones(DateTime fechaHora)
    {
        try
        {
            Window? mainWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
            }
            
            if (mainWindow == null)
            {
                ErrorMessage = "No se pudo abrir el diálogo de guardado";
                await Task.Delay(2000);
                ErrorMessage = "";
                return;
            }
            
            var timestamp = fechaHora.ToString("yyyyMMdd_HHmmss");
            var nombreSugerido = $"Operaciones_{_codigoLocal}_{timestamp}.pdf";
            
            var storageProvider = mainWindow.StorageProvider;
            
            var archivo = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Guardar historial de operaciones",
                SuggestedFileName = nombreSugerido,
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Archivos PDF")
                    {
                        Patterns = new[] { "*.pdf" }
                    }
                }
            });
            
            if (archivo == null) return;
            
            IsLoading = true;
            ErrorMessage = "";
            
            var datosReporte = new OperacionesPdfService.DatosReporteOperaciones
            {
                CodigoLocal = _codigoLocal,
                NombreUsuario = _nombreUsuario,
                FechaGeneracion = fechaHora.ToString("dd/MM/yyyy"),
                HoraGeneracion = fechaHora.ToString("HH:mm:ss"),
                PanelSeleccionado = ObtenerNombrePanel(PanelActual),
                TotalOperaciones = Operaciones.Count,
                TotalEuros = Operaciones.Sum(o => o.CantidadPagadaNum),
                TotalDivisas = Operaciones.Sum(o => o.CantidadDivisaNum),
                Filtros = new OperacionesPdfService.FiltrosReporte
                {
                    FechaDesde = FechaDesdeTexto,
                    FechaHasta = FechaHastaTexto,
                    OperacionDesde = FiltroOperacionDivisaDesde,
                    OperacionHasta = FiltroOperacionDivisaHasta
                }
            };
            
            foreach (var op in Operaciones)
            {
                datosReporte.Operaciones.Add(new OperacionesPdfService.OperacionDetalle
                {
                    Hora = op.Hora,
                    Fecha = op.Fecha,
                    NumeroOperacion = op.NumeroOperacion,
                    Usuario = op.Usuario,
                    Descripcion = op.Descripcion,
                    CantidadDivisa = op.CantidadDivisa,
                    CantidadPagada = op.CantidadPagada,
                    Cliente = op.Cliente,
                    TipoDocumento = op.TipoDocumento,
                    NumeroDocumento = op.NumeroDocumento
                });
            }
            
            // Generar PDF en hilo separado para no bloquear la UI
            var pdfBytes = await Task.Run(() => OperacionesPdfService.GenerarPdf(datosReporte));

            await using var stream = await archivo.OpenWriteAsync();
            await stream.WriteAsync(pdfBytes);
            
            SuccessMessage = "PDF guardado correctamente";
            await Task.Delay(3000);
            SuccessMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al generar PDF: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private void Volver()
    {
        OnVolverAInicio?.Invoke();
    }
    
    [RelayCommand]
    private void ToggleFiltros()
    {
        MostrarFiltros = !MostrarFiltros;
    }

    [RelayCommand]
    private async Task AnularOperacionAsync(OperacionPackAlimentoItem? operacion)
    {
        if (operacion == null || !operacion.EsPendiente)
            return;

        try
        {
            Window? mainWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
            }

            if (mainWindow == null)
                return;

            var ventanaConfirmacion = new Views.ConfirmacionAnulacionView(
                operacion.NumeroOperacion,
                $"{operacion.Fecha} {operacion.Hora}",
                operacion.NombreCliente,
                operacion.NombreBeneficiario,
                operacion.PaisDestino,
                operacion.Descripcion,
                operacion.ImporteFormateado
            );

            await ventanaConfirmacion.ShowDialog(mainWindow);

            if (ventanaConfirmacion.Confirmado)
            {
                await AnularOperacionEnBDAsync(operacion);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
    }

    private async Task AnularOperacionEnBDAsync(OperacionPackAlimentoItem operacion)
    {
        try
        {
            IsLoading = true;

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Obtener estado actual antes de anular
            var estadoAnterior = operacion.EstadoEnvio.ToUpper();

            var query = @"UPDATE operaciones_pack_alimentos
                          SET estado_envio = 'ANULADO'
                          WHERE id_operacion = @idOperacion";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@idOperacion", operacion.IdOperacion);

            var rowsAffected = await cmd.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                // Registrar en historial
                try
                {
                    var queryHistorial = @"
                        INSERT INTO historial_estados_pack_alimentos
                            (id_operacion, estado_anterior, estado_nuevo, fecha_cambio, id_usuario, observaciones)
                        VALUES
                            (@id_operacion, @estado_anterior, 'ANULADO', CURRENT_TIMESTAMP, @id_usuario, @observaciones)";

                    await using var cmdHistorial = new NpgsqlCommand(queryHistorial, conn);
                    cmdHistorial.Parameters.AddWithValue("@id_operacion", operacion.IdOperacion);
                    cmdHistorial.Parameters.AddWithValue("@estado_anterior", estadoAnterior);
                    cmdHistorial.Parameters.AddWithValue("@id_usuario", _idUsuario);
                    cmdHistorial.Parameters.AddWithValue("@observaciones", "Anulación desde lista de operaciones");
                    await cmdHistorial.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Si falla el historial, no es crítico
                }

                await CargarOperacionesAsync();

                SuccessMessage = $"Operación {operacion.NumeroOperacion} anulada correctamente";
                await Task.Delay(3000);
                SuccessMessage = "";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al anular: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task VerDetalleOperacionAsync(OperacionPackAlimentoItem? operacion)
    {
        if (operacion == null || string.IsNullOrEmpty(operacion.NumeroOperacion))
            return;

        try
        {
            Window? mainWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
            }

            if (mainWindow == null)
                return;

            var dialog = new DetalleOperacionPackAlimentosView(
                operacion.NumeroOperacion,
                _codigoLocal,
                _nombreUsuario,
                _idUsuario);

            await dialog.ShowDialog(mainWindow);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al abrir detalle: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
    }

    [RelayCommand]
    private async Task VerDetalleOperacionDivisaAsync(OperacionDetalleItem? operacion)
    {
        if (operacion == null || string.IsNullOrEmpty(operacion.NumeroOperacion) || operacion.NumeroOperacion == "-")
            return;

        try
        {
            Window? mainWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
            }

            if (mainWindow == null)
                return;

            var vm = new DetalleOperacionDivisaViewModel(operacion.NumeroOperacion, _codigoLocal);
            var dialog = new DetalleOperacionDivisaView(vm);

            await dialog.ShowDialog(mainWindow);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al abrir detalle: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
    }

    private void ActualizarResumen()
    {
        if (PanelActual == "alimentos")
        {
            TotalOperaciones = OperacionesAlimentos.Count.ToString();
            TotalPendientes = OperacionesAlimentos.Count(o => o.EstadoEnvio.ToUpper() == "PENDIENTE").ToString();
            TotalPagados = OperacionesAlimentos.Count(o => o.EstadoEnvio.ToUpper() == "PAGADO").ToString();
            TotalEnviados = OperacionesAlimentos.Count(o => o.EstadoEnvio.ToUpper() == "ENVIADO").ToString();
            TotalAnulados = OperacionesAlimentos.Count(o => o.EstadoEnvio.ToUpper() == "ANULADO").ToString();

            // Sumar el total de TODAS las operaciones MENOS las anuladas
            var totalImporte = OperacionesAlimentos
                .Where(o => o.EstadoEnvio.ToUpper() != "ANULADO")
                .Sum(o => o.Importe);

            // Mostrar el total (siempre positivo, es la suma de ventas)
            TotalImporteAlimentos = totalImporte.ToString("N2");
            TotalImporteAlimentosColor = "#0b5394"; // Azul para el total
        }
        else
        {
            TotalOperaciones = Operaciones.Count.ToString();
            TotalEurosMovidos = Operaciones.Sum(o => o.CantidadPagadaNum).ToString("N2");
            TotalDivisasMovidas = Operaciones.Sum(o => o.CantidadDivisaNum).ToString("N2");
        }
    }
    
    private async Task CargarOperacionesAsync()
    {
        try
        {
            IsLoading = true;
            Operaciones.Clear();
            OperacionesAlimentos.Clear();

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var fechaDesde = ParsearFecha(FechaDesdeTexto);
            var fechaHasta = ParsearFecha(FechaHastaTexto);

            if (PanelActual == "divisa")
            {
                // Si hay un número de operación seleccionado, solo buscar operaciones de divisa
                // Si hay filtro de N° Operación, solo cargar operaciones de divisas
                // (no traspasos ni ventas ya que estos no tienen número de operación)
                var tieneFiltroPorOperacion = !string.IsNullOrWhiteSpace(FiltroOperacionDivisaDesde) ||
                                               !string.IsNullOrWhiteSpace(FiltroOperacionDivisaHasta);

                if (tieneFiltroPorOperacion)
                {
                    await CargarOperacionesDivisasAsync(conn, fechaDesde, fechaHasta);
                }
                else
                {
                    if (TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Compra divisa")
                        await CargarOperacionesDivisasAsync(conn, fechaDesde, fechaHasta);
                    if (TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Venta Divisa")
                        await CargarVentaDivisaAsync(conn, fechaDesde, fechaHasta);
                    if (TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Traspaso")
                        await CargarTraspasosAsync(conn, fechaDesde, fechaHasta);
                }
                
                // Ordenar según la preferencia del usuario
                var operacionesOrdenadas = OrdenAscendente
                    ? Operaciones.OrderBy(o => o.FechaHoraOrden).ToList()
                    : Operaciones.OrderByDescending(o => o.FechaHoraOrden).ToList();
                Operaciones.Clear();
                
                for (int i = 0; i < operacionesOrdenadas.Count; i++)
                {
                    operacionesOrdenadas[i].BackgroundColor = i % 2 == 0 ? "White" : "#F5F5F5";
                    Operaciones.Add(operacionesOrdenadas[i]);
                }
            }
            else if (PanelActual == "alimentos")
            {
                await CargarOperacionesPackAlimentosAsync(conn, fechaDesde, fechaHasta);
            }
            
            ActualizarResumen();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar operaciones: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Carga las últimas 20 operaciones sin aplicar filtros de fecha.
    /// Se usa al entrar al panel o al cambiar de pestaña.
    /// </summary>
    private async Task CargarUltimas20OperacionesAsync()
    {
        try
        {
            IsLoading = true;
            Operaciones.Clear();
            OperacionesAlimentos.Clear();

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            if (PanelActual == "divisa")
            {
                // Cargar las últimas 20 operaciones SIN NINGÚN FILTRO
                await CargarUltimas20DivisasSinFiltrosAsync(conn);

                // Primero tomar las 20 más recientes, luego ordenar según preferencia del usuario
                var las20MasRecientes = Operaciones
                    .OrderByDescending(o => o.FechaHoraOrden)
                    .Take(20)
                    .ToList();

                var operacionesOrdenadas = OrdenAscendente
                    ? las20MasRecientes.OrderBy(o => o.FechaHoraOrden).ToList()
                    : las20MasRecientes;

                Operaciones.Clear();

                for (int i = 0; i < operacionesOrdenadas.Count; i++)
                {
                    operacionesOrdenadas[i].BackgroundColor = i % 2 == 0 ? "White" : "#F5F5F5";
                    Operaciones.Add(operacionesOrdenadas[i]);
                }
            }
            else if (PanelActual == "alimentos")
            {
                await CargarUltimas20AlimentosAsync(conn);
            }

            ActualizarResumen();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar operaciones: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Carga todas las operaciones de divisas sin ningún filtro (fecha, divisa, número operación)
    /// Solo filtra por id_local. Luego se ordenan y se toman las 20 más recientes.
    /// </summary>
    private async Task CargarUltimas20DivisasSinFiltrosAsync(NpgsqlConnection conn)
    {
        // 1. Cargar TODAS las operaciones de cambio de divisa del local
        var sqlDivisas = @"SELECT
                        o.fecha_operacion,
                        o.hora_operacion,
                        o.numero_operacion,
                        u.numero_usuario,
                        od.divisa_origen,
                        od.cantidad_origen,
                        od.cantidad_destino,
                        c.nombre,
                        c.apellidos,
                        c.documento_tipo,
                        c.documento_numero
                    FROM operaciones o
                    INNER JOIN operaciones_divisas od ON o.id_operacion = od.id_operacion
                    LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
                    LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
                    WHERE o.id_local = @idLocal
                    ORDER BY o.fecha_operacion DESC, o.hora_operacion DESC";

        await using (var cmd = new NpgsqlCommand(sqlDivisas, conn))
        {
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var fechaDb = reader.GetDateTime(0);
                var hora = reader.IsDBNull(1) ? TimeSpan.Zero : reader.GetTimeSpan(1);
                var numeroOp = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var numeroUsuario = reader.IsDBNull(3) ? "-" : reader.GetString(3);
                var divisaOrigen = reader.GetString(4);
                var cantidadOrigen = reader.GetDecimal(5);
                var cantidadDestino = reader.GetDecimal(6);
                var nombreCliente = reader.IsDBNull(7) ? "" : reader.GetString(7);
                var apellidosCliente = reader.IsDBNull(8) ? "" : reader.GetString(8);
                var tipoDoc = reader.IsDBNull(9) ? "" : reader.GetString(9);
                var numDoc = reader.IsDBNull(10) ? "" : reader.GetString(10);

                var primerNombreCliente = nombreCliente.Split(' ').FirstOrDefault() ?? "";
                var primerApellidoCliente = apellidosCliente.Split(' ').FirstOrDefault() ?? "";
                var clienteNombreCorto = $"{primerNombreCliente} {primerApellidoCliente}".Trim();
                if (string.IsNullOrWhiteSpace(clienteNombreCorto)) clienteNombreCorto = "-";

                Operaciones.Add(new OperacionDetalleItem
                {
                    Hora = hora.ToString(@"hh\:mm"),
                    Fecha = fechaDb.ToString("dd/MM/yy"),
                    NumeroOperacion = numeroOp,
                    Usuario = numeroUsuario,
                    Descripcion = $"Compra {divisaOrigen}",
                    CantidadDivisa = $"{cantidadOrigen:N2}",
                    CantidadDivisaNum = cantidadOrigen,
                    CantidadPagada = $"-{cantidadDestino:N2}",
                    CantidadPagadaNum = cantidadDestino,
                    Cliente = clienteNombreCorto,
                    TipoDocumento = string.IsNullOrWhiteSpace(tipoDoc) ? "-" : tipoDoc,
                    NumeroDocumento = string.IsNullOrWhiteSpace(numDoc) ? "-" : numDoc,
                    FechaHoraOrden = fechaDb.Date.Add(hora),
                    EsSalida = true
                });
            }
        }

        // 2. Cargar TODAS las ventas de divisa del local
        var sqlDepositos = @"SELECT
                        bc.fecha_movimiento,
                        bc.monto,
                        bc.descripcion,
                        bc.divisa,
                        u.numero_usuario
                    FROM balance_cuentas bc
                    LEFT JOIN usuarios u ON bc.id_usuario = u.id_usuario
                    WHERE bc.id_local = @idLocal
                    AND bc.tipo_movimiento = 'DEPOSITO'
                    ORDER BY bc.fecha_movimiento DESC";

        await using (var cmd = new NpgsqlCommand(sqlDepositos, conn))
        {
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var fecha = reader.GetDateTime(0);
                var monto = reader.GetDecimal(1);
                var descripcion = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var divisa = reader.IsDBNull(3) ? "" : reader.GetString(3);
                var usuario = reader.IsDBNull(4) ? "" : reader.GetString(4);

                var cantidadDivisaTexto = "-";
                decimal cantidadDivisaNum = 0;
                var divisaExtraida = divisa;

                if (descripcion.Contains(":"))
                {
                    var partes = descripcion.Split(':');
                    if (partes.Length > 1)
                    {
                        var textoCompleto = partes[1].Trim();
                        var partesTexto = textoCompleto.Split(' ');
                        if (partesTexto.Length >= 1)
                        {
                            decimal.TryParse(partesTexto[0].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out cantidadDivisaNum);
                            cantidadDivisaTexto = cantidadDivisaNum.ToString("N2");
                        }
                        if (partesTexto.Length >= 2 && !string.IsNullOrEmpty(partesTexto[1]))
                        {
                            divisaExtraida = partesTexto[1];
                        }
                    }
                }

                var descripcionFinal = !string.IsNullOrEmpty(divisaExtraida) ? $"Venta {divisaExtraida}" : "Venta Divisa";

                // La cantidad de divisa es negativa porque se vende (sale)
                var cantidadDivisaConSigno = cantidadDivisaNum > 0 ? $"-{cantidadDivisaNum:N2}" : cantidadDivisaTexto;

                Operaciones.Add(new OperacionDetalleItem
                {
                    Hora = fecha.ToString("HH:mm"),
                    Fecha = fecha.ToString("dd/MM/yy"),
                    NumeroOperacion = "-",
                    Usuario = usuario,
                    Descripcion = descripcionFinal,
                    CantidadDivisa = cantidadDivisaConSigno,
                    CantidadDivisaNum = cantidadDivisaNum,
                    CantidadPagada = $"+{monto:N2}",
                    CantidadPagadaNum = monto,
                    Cliente = "-",
                    TipoDocumento = "-",
                    NumeroDocumento = "-",
                    FechaHoraOrden = fecha,
                    EsSalidaDivisa = true
                });
            }
        }

        // 3. Cargar TODOS los traspasos del local
        var sqlTraspasos = @"SELECT
                        bc.fecha_movimiento,
                        bc.monto,
                        u.numero_usuario
                    FROM balance_cuentas bc
                    LEFT JOIN usuarios u ON bc.id_usuario = u.id_usuario
                    WHERE bc.id_local = @idLocal
                    AND bc.tipo_movimiento = 'TRASPASO'
                    ORDER BY bc.fecha_movimiento DESC";

        await using (var cmd = new NpgsqlCommand(sqlTraspasos, conn))
        {
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var fecha = reader.GetDateTime(0);
                var monto = reader.GetDecimal(1);
                var usuario = reader.IsDBNull(2) ? "" : reader.GetString(2);

                Operaciones.Add(new OperacionDetalleItem
                {
                    Hora = fecha.ToString("HH:mm"),
                    Fecha = fecha.ToString("dd/MM/yy"),
                    NumeroOperacion = "-",
                    Usuario = usuario,
                    Descripcion = "Traspaso",
                    CantidadDivisa = "-",
                    CantidadDivisaNum = 0,
                    CantidadPagada = $"-{monto:N2}",
                    CantidadPagadaNum = monto,
                    Cliente = "-",
                    TipoDocumento = "-",
                    NumeroDocumento = "-",
                    FechaHoraOrden = fecha,
                    EsSalida = true
                });
            }
        }
    }

    private async Task CargarUltimas20AlimentosAsync(NpgsqlConnection conn)
    {
        // Cargar las últimas 20 operaciones de pack alimentos (sin filtros)
        // Agrupadas por numero_operacion para mostrar una sola fila por operación
        // Ordenar según preferencia del usuario (por defecto: más viejas primero)
        var ordenSql = OrdenAscendente ? "ASC" : "DESC";
        var sql = $@"
            SELECT
                MIN(o.id_operacion) as id_operacion,
                o.numero_operacion,
                MIN(o.fecha_operacion) as fecha_operacion,
                MIN(o.hora_operacion) as hora_operacion,
                MIN(c.nombre) as cliente_nombre,
                MIN(c.apellidos) as cliente_apellido,
                MIN(o.importe_total) as importe_total,
                MIN(o.moneda) as moneda,
                COUNT(opa.id_operacion_pack_alimento) as cantidad_articulos,
                CASE
                    WHEN COUNT(opa.id_operacion_pack_alimento) = 1 THEN MIN(opa.nombre_pack)
                    ELSE COUNT(opa.id_operacion_pack_alimento)::text || ' Articulos'
                END as descripcion,
                MIN(opa.pais_destino) as pais_destino,
                MIN(opa.ciudad_destino) as ciudad_destino,
                CASE
                    WHEN bool_or(UPPER(opa.estado_envio) = 'PENDIENTE') THEN 'PENDIENTE'
                    WHEN bool_or(UPPER(opa.estado_envio) = 'PAGADO') THEN 'PAGADO'
                    WHEN bool_or(UPPER(opa.estado_envio) = 'ENVIADO') THEN 'ENVIADO'
                    WHEN bool_or(UPPER(opa.estado_envio) = 'ENTREGADO') THEN 'ENTREGADO'
                    ELSE MIN(opa.estado_envio)
                END as estado_envio,
                MIN(cb.nombre) as beneficiario_nombre,
                MIN(cb.apellido) as beneficiario_apellido,
                MIN(u.numero_usuario) as usuario_nombre
            FROM operaciones o
            LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
            LEFT JOIN clientes_beneficiarios cb ON opa.id_beneficiario = cb.id_beneficiario
            LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
            LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
            WHERE o.id_local = @idLocal
              AND o.modulo = 'PACK_ALIMENTOS'
            GROUP BY o.numero_operacion
            ORDER BY MIN(o.fecha_operacion) {ordenSql}, MIN(o.hora_operacion) {ordenSql}
            LIMIT 20";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@idLocal", _idLocal);

        await using var reader = await cmd.ExecuteReaderAsync();

        int index = 0;
        while (await reader.ReadAsync())
        {
            var fechaOp = reader.IsDBNull(2) ? DateTime.Today : reader.GetDateTime(2);
            var horaOp = reader.IsDBNull(3) ? TimeSpan.Zero : reader.GetTimeSpan(3);
            // Columna 12 es estado_envio después del GROUP BY
            var estadoEnvio = reader.IsDBNull(12) ? "PENDIENTE" : reader.GetString(12);

            // Cliente: primer nombre y primer apellido
            var clienteNombre = reader.IsDBNull(4) ? "" : reader.GetString(4);
            var clienteApellido = reader.IsDBNull(5) ? "" : reader.GetString(5);
            var primerNombreCliente = clienteNombre.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoCliente = clienteApellido.Split(' ').FirstOrDefault() ?? "";
            var nombreClienteCompleto = $"{primerNombreCliente} {primerApellidoCliente}".Trim();

            // Beneficiario: primer nombre y primer apellido (columnas 13 y 14)
            var benefNombre = reader.IsDBNull(13) ? "" : reader.GetString(13);
            var benefApellido = reader.IsDBNull(14) ? "" : reader.GetString(14);
            var primerNombreBenef = benefNombre.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoBenef = benefApellido.Split(' ').FirstOrDefault() ?? "";
            var nombreBenefCompleto = $"{primerNombreBenef} {primerApellidoBenef}".Trim();

            // Usuario (columna 15)
            var usuarioNombre = reader.IsDBNull(15) ? "" : reader.GetString(15);

            // Descripción ya viene formateada del query (columna 9)
            // Si es 1 artículo muestra el nombre, si son más muestra "X Articulos"
            var descripcion = reader.IsDBNull(9) ? "Pack Alimentos" : reader.GetString(9);

            var item = new OperacionPackAlimentoItem
            {
                IdOperacion = reader.GetInt64(0),
                NumeroOperacion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Fecha = fechaOp.ToString("dd/MM/yy"),
                Hora = horaOp.ToString(@"hh\:mm"),
                Usuario = usuarioNombre,
                NombreCliente = nombreClienteCompleto,
                Importe = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                Moneda = reader.IsDBNull(7) ? "EUR" : reader.GetString(7),
                Descripcion = descripcion,
                PaisDestino = reader.IsDBNull(10) ? "" : reader.GetString(10),
                CiudadDestino = reader.IsDBNull(11) ? "" : reader.GetString(11),
                NombreBeneficiario = nombreBenefCompleto,
                EstadoEnvio = estadoEnvio,
                EstadoTexto = ObtenerTextoEstado(estadoEnvio),
                EstadoColor = ObtenerColorEstado(estadoEnvio),
                BackgroundColor = index % 2 == 0 ? "White" : "#F5F5F5",
                FechaHoraOrden = fechaOp.Add(horaOp)
            };

            OperacionesAlimentos.Add(item);
            index++;
        }
    }

    // ============================================
    // CARGAR PAISES DESTINO PARA FILTRO
    // ============================================

    private async Task CargarPaisesDestinoAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var query = @"
                SELECT DISTINCT opa.pais_destino
                FROM operaciones_pack_alimentos opa
                INNER JOIN operaciones o ON o.id_operacion = opa.id_operacion
                WHERE o.id_local = @idLocal
                  AND opa.pais_destino IS NOT NULL
                  AND opa.pais_destino != ''
                ORDER BY opa.pais_destino";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);

            await using var reader = await cmd.ExecuteReaderAsync();

            PaisesDestinoDisponibles.Clear();
            PaisesDestinoDisponibles.Add("Todos");

            while (await reader.ReadAsync())
            {
                var pais = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(pais))
                    PaisesDestinoDisponibles.Add(pais);
            }

            // Inicializar países filtrados
            FiltrarPaises(TextoBusquedaPaisDestino);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar paises destino: {ex.Message}");
        }
    }

    // ============================================
    // CARGAR OPERACIONES PACK ALIMENTOS
    // ============================================

    private async Task CargarOperacionesPackAlimentosAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var whereConditions = new System.Collections.Generic.List<string>
        {
            "o.id_local = @idLocal",
            "o.modulo = 'PACK_ALIMENTOS'"
        };

        if (fechaDesde.HasValue)
            whereConditions.Add("o.fecha_operacion >= @fechaDesde");

        if (fechaHasta.HasValue)
            whereConditions.Add("o.fecha_operacion <= @fechaHasta");

        // Filtro por rango de número de operación (Desde/Hasta)
        // Usamos CAST con REGEXP_REPLACE para extraer solo dígitos y comparar numéricamente
        var tieneDesde = !string.IsNullOrWhiteSpace(FiltroOperacionAlimentosDesde);
        var tieneHasta = !string.IsNullOrWhiteSpace(FiltroOperacionAlimentosHasta);

        if (tieneDesde && tieneHasta)
        {
            // Comparación numérica: extraer dígitos y comparar como enteros
            whereConditions.Add(@"CAST(NULLIF(REGEXP_REPLACE(o.numero_operacion, '[^0-9]', '', 'g'), '') AS INTEGER)
                      BETWEEN CAST(NULLIF(REGEXP_REPLACE(@numOpDesde, '[^0-9]', '', 'g'), '') AS INTEGER)
                      AND CAST(NULLIF(REGEXP_REPLACE(@numOpHasta, '[^0-9]', '', 'g'), '') AS INTEGER)");
        }
        else if (tieneDesde)
        {
            // Solo Desde: buscar operaciones que coincidan con el texto
            whereConditions.Add("o.numero_operacion ILIKE @numOpDesde");
        }
        else if (tieneHasta)
        {
            // Solo Hasta: buscar operaciones que coincidan con el texto
            whereConditions.Add("o.numero_operacion ILIKE @numOpHasta");
        }

        if (FiltroPaisDestino != "Todos" && !string.IsNullOrWhiteSpace(FiltroPaisDestino))
            whereConditions.Add("opa.pais_destino = @paisDestino");

        if (FiltroEstadoOperacion != "Todos" && !string.IsNullOrWhiteSpace(FiltroEstadoOperacion))
            whereConditions.Add("UPPER(opa.estado_envio) = @estadoOperacion");

        var whereClause = string.Join(" AND ", whereConditions);

        // Query agrupada por numero_operacion para mostrar una sola fila por operación
        var query = $@"
            SELECT
                MIN(o.id_operacion) as id_operacion,
                o.numero_operacion,
                MIN(o.fecha_operacion) as fecha_operacion,
                MIN(o.hora_operacion) as hora_operacion,
                MIN(c.nombre) as cliente_nombre,
                MIN(c.apellidos) as cliente_apellido,
                MIN(o.importe_total) as importe_total,
                MIN(o.moneda) as moneda,
                COUNT(opa.id_operacion_pack_alimento) as cantidad_articulos,
                CASE
                    WHEN COUNT(opa.id_operacion_pack_alimento) = 1 THEN MIN(opa.nombre_pack)
                    ELSE COUNT(opa.id_operacion_pack_alimento)::text || ' Articulos'
                END as descripcion,
                MIN(opa.pais_destino) as pais_destino,
                MIN(opa.ciudad_destino) as ciudad_destino,
                CASE
                    WHEN bool_or(UPPER(opa.estado_envio) = 'PENDIENTE') THEN 'PENDIENTE'
                    WHEN bool_or(UPPER(opa.estado_envio) = 'PAGADO') THEN 'PAGADO'
                    WHEN bool_or(UPPER(opa.estado_envio) = 'ENVIADO') THEN 'ENVIADO'
                    WHEN bool_or(UPPER(opa.estado_envio) = 'ENTREGADO') THEN 'ENTREGADO'
                    ELSE MIN(opa.estado_envio)
                END as estado_envio,
                MIN(cb.nombre) as beneficiario_nombre,
                MIN(cb.apellido) as beneficiario_apellido,
                MIN(u.numero_usuario) as usuario_nombre
            FROM operaciones o
            LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
            LEFT JOIN clientes_beneficiarios cb ON opa.id_beneficiario = cb.id_beneficiario
            LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
            LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
            WHERE {whereClause}
            GROUP BY o.numero_operacion
            ORDER BY MIN(o.fecha_operacion) {(OrdenAscendente ? "ASC" : "DESC")}, MIN(o.hora_operacion) {(OrdenAscendente ? "ASC" : "DESC")}";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idLocal", _idLocal);

        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde.Value.Date);

        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta.Value.Date.AddDays(1).AddSeconds(-1));

        // Parámetros para rango Desde/Hasta
        if (!string.IsNullOrWhiteSpace(FiltroOperacionAlimentosDesde))
            cmd.Parameters.AddWithValue("@numOpDesde", tieneDesde && tieneHasta ? FiltroOperacionAlimentosDesde : $"%{FiltroOperacionAlimentosDesde}%");
        if (!string.IsNullOrWhiteSpace(FiltroOperacionAlimentosHasta))
            cmd.Parameters.AddWithValue("@numOpHasta", tieneDesde && tieneHasta ? FiltroOperacionAlimentosHasta : $"%{FiltroOperacionAlimentosHasta}%");

        if (FiltroPaisDestino != "Todos" && !string.IsNullOrWhiteSpace(FiltroPaisDestino))
            cmd.Parameters.AddWithValue("@paisDestino", FiltroPaisDestino);

        if (FiltroEstadoOperacion != "Todos" && !string.IsNullOrWhiteSpace(FiltroEstadoOperacion))
            cmd.Parameters.AddWithValue("@estadoOperacion", FiltroEstadoOperacion.ToUpper());

        await using var reader = await cmd.ExecuteReaderAsync();

        int index = 0;
        while (await reader.ReadAsync())
        {
            var fechaOp = reader.IsDBNull(2) ? DateTime.Today : reader.GetDateTime(2);
            var horaOp = reader.IsDBNull(3) ? TimeSpan.Zero : reader.GetTimeSpan(3);
            // Columna 12 es estado_envio después del GROUP BY
            var estadoEnvio = reader.IsDBNull(12) ? "PENDIENTE" : reader.GetString(12);

            // Cliente: primer nombre y primer apellido
            var clienteNombre = reader.IsDBNull(4) ? "" : reader.GetString(4);
            var clienteApellido = reader.IsDBNull(5) ? "" : reader.GetString(5);
            var primerNombreCliente = clienteNombre.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoCliente = clienteApellido.Split(' ').FirstOrDefault() ?? "";
            var nombreClienteCompleto = $"{primerNombreCliente} {primerApellidoCliente}".Trim();

            // Beneficiario: primer nombre y primer apellido (columnas 13 y 14)
            var benefNombre = reader.IsDBNull(13) ? "" : reader.GetString(13);
            var benefApellido = reader.IsDBNull(14) ? "" : reader.GetString(14);
            var primerNombreBenef = benefNombre.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoBenef = benefApellido.Split(' ').FirstOrDefault() ?? "";
            var nombreBenefCompleto = $"{primerNombreBenef} {primerApellidoBenef}".Trim();

            // Usuario (columna 15)
            var usuarioNombre = reader.IsDBNull(15) ? "" : reader.GetString(15);

            // Descripción ya viene formateada del query (columna 9)
            // Si es 1 artículo muestra el nombre, si son más muestra "X Articulos"
            var descripcion = reader.IsDBNull(9) ? "Pack Alimentos" : reader.GetString(9);

            var item = new OperacionPackAlimentoItem
            {
                IdOperacion = reader.GetInt64(0),
                NumeroOperacion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Fecha = fechaOp.ToString("dd/MM/yy"),
                Hora = horaOp.ToString(@"hh\:mm"),
                Usuario = usuarioNombre,
                NombreCliente = nombreClienteCompleto,
                Importe = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                Moneda = reader.IsDBNull(7) ? "EUR" : reader.GetString(7),
                Descripcion = descripcion,
                PaisDestino = reader.IsDBNull(10) ? "" : reader.GetString(10),
                CiudadDestino = reader.IsDBNull(11) ? "" : reader.GetString(11),
                NombreBeneficiario = nombreBenefCompleto,
                EstadoEnvio = estadoEnvio,
                EstadoTexto = ObtenerTextoEstado(estadoEnvio),
                EstadoColor = ObtenerColorEstado(estadoEnvio),
                BackgroundColor = index % 2 == 0 ? "White" : "#F5F5F5",
                FechaHoraOrden = fechaOp.Add(horaOp)
            };

            OperacionesAlimentos.Add(item);
            index++;
        }
    }

    private string ObtenerTextoEstado(string estado)
    {
        // Mostrar el estado con formato legible para la UI
        return estado.ToUpper() switch
        {
            "PENDIENTE" => "Pendiente",
            "PAGADO" => "Pagado",
            "ENVIADO" => "Enviado",
            "ANULADO" => "Anulado",
            _ => estado
        };
    }

    private string ObtenerColorEstado(string estado)
    {
        return estado.ToUpper() switch
        {
            "PENDIENTE" => "#ffc107",
            "PAGADO" => "#17a2b8",
            "ENVIADO" => "#28a745",
            "ANULADO" => "#dc3545",
            _ => "#6c757d"
        };
    }
    
    private async Task CargarOperacionesDivisasAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = @"SELECT
                        o.fecha_operacion,
                        o.hora_operacion,
                        o.numero_operacion,
                        u.numero_usuario,
                        od.divisa_origen,
                        od.cantidad_origen,
                        od.cantidad_destino,
                        c.nombre,
                        c.apellidos,
                        c.documento_tipo,
                        c.documento_numero,
                        c.segundo_nombre,
                        c.segundo_apellido
                    FROM operaciones o
                    INNER JOIN operaciones_divisas od ON o.id_operacion = od.id_operacion
                    LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
                    LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
                    WHERE o.id_local = @idLocal
                      AND o.modulo = 'DIVISAS'";
        
        if (fechaDesde.HasValue)
            sql += " AND o.fecha_operacion >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND o.fecha_operacion <= @fechaHasta";

        // Filtro por rango de número de operación (Desde/Hasta)
        // Usamos CAST con REGEXP_REPLACE para extraer solo dígitos y comparar numéricamente
        var tieneDesde = !string.IsNullOrWhiteSpace(FiltroOperacionDivisaDesde);
        var tieneHasta = !string.IsNullOrWhiteSpace(FiltroOperacionDivisaHasta);

        if (tieneDesde && tieneHasta)
        {
            // Comparación numérica: extraer dígitos y comparar como enteros
            sql += @" AND CAST(NULLIF(REGEXP_REPLACE(o.numero_operacion, '[^0-9]', '', 'g'), '') AS INTEGER)
                      BETWEEN CAST(NULLIF(REGEXP_REPLACE(@numOpDesde, '[^0-9]', '', 'g'), '') AS INTEGER)
                      AND CAST(NULLIF(REGEXP_REPLACE(@numOpHasta, '[^0-9]', '', 'g'), '') AS INTEGER)";
        }
        else if (tieneDesde)
        {
            sql += " AND o.numero_operacion ILIKE @numOpDesde";
        }
        else if (tieneHasta)
        {
            sql += " AND o.numero_operacion ILIKE @numOpHasta";
        }

        if (FiltroDivisa != "Todas" && !string.IsNullOrWhiteSpace(FiltroDivisa))
            sql += " AND od.divisa_origen = @filtroDivisa";

        sql += " ORDER BY o.fecha_operacion DESC, o.hora_operacion DESC";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@idLocal", _idLocal);

        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta.Value.Date.AddDays(1).AddSeconds(-1));

        // Parámetros para rango Desde/Hasta
        if (!string.IsNullOrWhiteSpace(FiltroOperacionDivisaDesde))
            cmd.Parameters.AddWithValue("@numOpDesde", tieneDesde && tieneHasta ? FiltroOperacionDivisaDesde : $"%{FiltroOperacionDivisaDesde}%");
        if (!string.IsNullOrWhiteSpace(FiltroOperacionDivisaHasta))
            cmd.Parameters.AddWithValue("@numOpHasta", tieneDesde && tieneHasta ? FiltroOperacionDivisaHasta : $"%{FiltroOperacionDivisaHasta}%");

        if (FiltroDivisa != "Todas" && !string.IsNullOrWhiteSpace(FiltroDivisa))
            cmd.Parameters.AddWithValue("@filtroDivisa", FiltroDivisa);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        while (await reader.ReadAsync())
        {
            var fechaDb = reader.GetDateTime(0);
            var hora = reader.IsDBNull(1) ? TimeSpan.Zero : reader.GetTimeSpan(1);
            var numeroOp = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var numeroUsuario = reader.IsDBNull(3) ? "-" : reader.GetString(3);
            var divisaOrigen = reader.GetString(4);
            var cantidadOrigen = reader.GetDecimal(5);
            var cantidadDestino = reader.GetDecimal(6);
            var nombreCliente = reader.IsDBNull(7) ? "" : reader.GetString(7);
            var apellidosCliente = reader.IsDBNull(8) ? "" : reader.GetString(8);
            var tipoDoc = reader.IsDBNull(9) ? "" : reader.GetString(9);
            var numDoc = reader.IsDBNull(10) ? "" : reader.GetString(10);
            var segundoNombre = reader.IsDBNull(11) ? "" : reader.GetString(11);
            var segundoApellido = reader.IsDBNull(12) ? "" : reader.GetString(12);

            // Cliente: primer nombre y primer apellido solamente
            var primerNombreCliente = nombreCliente.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoCliente = apellidosCliente.Split(' ').FirstOrDefault() ?? "";
            var clienteNombreCorto = $"{primerNombreCliente} {primerApellidoCliente}".Trim();
            if (string.IsNullOrWhiteSpace(clienteNombreCorto)) clienteNombreCorto = "-";

            Operaciones.Add(new OperacionDetalleItem
            {
                Hora = hora.ToString(@"hh\:mm"),
                Fecha = fechaDb.ToString("dd/MM/yy"),
                NumeroOperacion = numeroOp,
                Usuario = numeroUsuario,
                Descripcion = $"Compra {divisaOrigen}",
                CantidadDivisa = $"{cantidadOrigen:N2}",
                CantidadDivisaNum = cantidadOrigen,
                CantidadPagada = $"-{cantidadDestino:N2}",
                CantidadPagadaNum = cantidadDestino,
                Cliente = clienteNombreCorto,
                TipoDocumento = string.IsNullOrWhiteSpace(tipoDoc) ? "-" : tipoDoc,
                NumeroDocumento = string.IsNullOrWhiteSpace(numDoc) ? "-" : numDoc,
                FechaHoraOrden = fechaDb.Date.Add(hora),
                EsSalida = true
            });
        }
    }
    
    private async Task CargarVentaDivisaAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = @"SELECT
                        bc.fecha_movimiento,
                        bc.monto,
                        bc.descripcion,
                        bc.divisa,
                        u.numero_usuario
                    FROM balance_cuentas bc
                    LEFT JOIN usuarios u ON bc.id_usuario = u.id_usuario
                    WHERE bc.id_local = @idLocal
                    AND bc.tipo_movimiento = 'DEPOSITO'
                    AND bc.modulo = 'DIVISAS'";

        if (fechaDesde.HasValue)
            sql += " AND bc.fecha_movimiento >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND bc.fecha_movimiento <= @fechaHasta";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@idLocal", _idLocal);

        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta.Value.Date.AddDays(1));

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var fecha = reader.GetDateTime(0);
            var monto = reader.GetDecimal(1);
            var descripcion = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var divisa = reader.IsDBNull(3) ? "" : reader.GetString(3);
            var usuario = reader.IsDBNull(4) ? "" : reader.GetString(4);

            var cantidadDivisaTexto = "-";
            decimal cantidadDivisaNum = 0;

            // Extraer la divisa de la descripción si está disponible
            // Formato típico: "Depósito banco: 100 USD"
            var divisaExtraida = divisa;
            if (descripcion.Contains(":"))
            {
                var partes = descripcion.Split(':');
                if (partes.Length > 1)
                {
                    var textoCompleto = partes[1].Trim();
                    var partesTexto = textoCompleto.Split(' ');
                    if (partesTexto.Length >= 1)
                    {
                        decimal.TryParse(partesTexto[0].Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out cantidadDivisaNum);
                        cantidadDivisaTexto = cantidadDivisaNum.ToString("N2");
                    }
                    if (partesTexto.Length >= 2 && !string.IsNullOrEmpty(partesTexto[1]))
                    {
                        divisaExtraida = partesTexto[1];
                    }
                }
            }

            // Descripción: "Venta USD" o "Venta [divisa]"
            var descripcionFinal = !string.IsNullOrEmpty(divisaExtraida) ? $"Venta {divisaExtraida}" : "Venta Divisa";

            // La cantidad de divisa es negativa porque se vende (sale)
            var cantidadDivisaConSigno = cantidadDivisaNum > 0 ? $"-{cantidadDivisaNum:N2}" : cantidadDivisaTexto;

            Operaciones.Add(new OperacionDetalleItem
            {
                Hora = fecha.ToString("HH:mm"),
                Fecha = fecha.ToString("dd/MM/yy"),
                NumeroOperacion = "-",
                Usuario = usuario,
                Descripcion = descripcionFinal,
                CantidadDivisa = cantidadDivisaConSigno,
                CantidadDivisaNum = cantidadDivisaNum,
                CantidadPagada = $"+{monto:N2}",
                CantidadPagadaNum = monto,
                Cliente = "-",
                TipoDocumento = "-",
                NumeroDocumento = "-",
                FechaHoraOrden = fecha,
                EsSalidaDivisa = true
            });
        }
    }

    private async Task CargarTraspasosAsync(NpgsqlConnection conn, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var sql = @"SELECT
                        bc.fecha_movimiento,
                        bc.monto,
                        u.numero_usuario
                    FROM balance_cuentas bc
                    LEFT JOIN usuarios u ON bc.id_usuario = u.id_usuario
                    WHERE bc.id_local = @idLocal
                    AND bc.tipo_movimiento = 'TRASPASO'
                    AND bc.modulo = 'DIVISAS'";

        if (fechaDesde.HasValue)
            sql += " AND bc.fecha_movimiento >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND bc.fecha_movimiento <= @fechaHasta";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@idLocal", _idLocal);

        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta.Value.Date.AddDays(1));

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var fecha = reader.GetDateTime(0);
            var monto = reader.GetDecimal(1);
            var usuario = reader.IsDBNull(2) ? "" : reader.GetString(2);

            Operaciones.Add(new OperacionDetalleItem
            {
                Hora = fecha.ToString("HH:mm"),
                Fecha = fecha.ToString("dd/MM/yy"),
                NumeroOperacion = "-",
                Usuario = usuario,
                Descripcion = "Traspaso",
                CantidadDivisa = "-",
                CantidadDivisaNum = 0,
                CantidadPagada = $"-{monto:N2}",
                CantidadPagadaNum = monto,
                Cliente = "-",
                TipoDocumento = "-",
                NumeroDocumento = "-",
                FechaHoraOrden = fecha,
                EsSalida = true
            });
        }
    }
}

public class OperacionDetalleItem
{
    public string Hora { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string NumeroOperacion { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string CantidadDivisa { get; set; } = "";
    public decimal CantidadDivisaNum { get; set; } = 0;
    public string CantidadPagada { get; set; } = "";
    public decimal CantidadPagadaNum { get; set; } = 0;
    public string Cliente { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string BackgroundColor { get; set; } = "White";
    public DateTime FechaHoraOrden { get; set; } = DateTime.MinValue;
    public bool EsSalida { get; set; } = false;
    public bool EsSalidaDivisa { get; set; } = false;

    // Propiedad para saber si el numero de operacion es clickeable (solo operaciones de divisa, no depositos/traspasos)
    public bool EsClickeable => !string.IsNullOrEmpty(NumeroOperacion) && NumeroOperacion != "-";

    // Color para la columna Euros: rojo si sale, verde si entra
    public string ColorEuros => EsSalida ? "#CC3333" : "#008800";

    // Color para la columna Divisa: rojo si sale (venta), azul si entra (compra)
    public string ColorDivisa => EsSalidaDivisa ? "#CC3333" : "#0b5394";
}

public class OperacionPackAlimentoItem
{
    public long IdOperacion { get; set; }
    public string NumeroOperacion { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string Hora { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string NombreBeneficiario { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string PaisDestino { get; set; } = "";
    public string CiudadDestino { get; set; } = "";
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = "EUR";
    public string EstadoEnvio { get; set; } = "";
    public string EstadoTexto { get; set; } = "";
    public string EstadoColor { get; set; } = "#6c757d";
    public string BackgroundColor { get; set; } = "White";
    public DateTime FechaHoraOrden { get; set; }

    public string ImporteFormateado => $"{Importe:N2} {Moneda}";

    // Propiedades para anulacion
    public bool EsPendiente => EstadoEnvio.ToUpper() == "PENDIENTE";
    public Avalonia.Input.StandardCursorType CursorEstado => EsPendiente ? Avalonia.Input.StandardCursorType.Hand : Avalonia.Input.StandardCursorType.Arrow;
}

public class DivisaFiltroItem
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Pais { get; set; } = "";
    public bool EsTodas { get; set; } = false;

    public string DisplayText => EsTodas ? Nombre : $"{Codigo} - {Nombre}";
    public string DisplayCompleto => EsTodas ? Nombre : $"{Codigo} - {Nombre} ({Pais})";
}