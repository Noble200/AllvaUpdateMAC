using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Allva.Desktop.ViewModels.Admin;

/// <summary>
/// ViewModel para el panel de operaciones del BackOffice
/// Permite ver todas las operaciones de todos los comercios y locales
/// </summary>
public partial class OperacionesAdminViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    // ============================================
    // FILTROS PRINCIPALES
    // ============================================

    [ObservableProperty]
    private string _fechaDesdeTexto = "";

    [ObservableProperty]
    private string _fechaHastaTexto = "";

    [ObservableProperty]
    private string _filtroNumeroOperacion = "";

    [ObservableProperty]
    private string _filtroNumeroOperacionGlobal = "";

    // Filtros con autocompletado - Comercio
    [ObservableProperty]
    private string _filtroComercioTexto = "";

    [ObservableProperty]
    private bool _mostrarSugerenciasComercio = false;

    // Filtros con autocompletado - Local
    [ObservableProperty]
    private string _filtroLocalTexto = "";

    [ObservableProperty]
    private bool _mostrarSugerenciasLocal = false;

    // Filtros con autocompletado - Divisa
    [ObservableProperty]
    private string _filtroDivisaTexto = "";

    [ObservableProperty]
    private bool _mostrarSugerenciasDivisa = false;

    // Filtros especificos por panel (Pack Alimentos)
    [ObservableProperty]
    private string _filtroPaisDestino = "";

    [ObservableProperty]
    private string _filtroEstadoAlimentos = "Todos";

    // Autocompletado país destino
    [ObservableProperty]
    private string _textoBusquedaPaisDestino = "";

    [ObservableProperty]
    private bool _mostrarListaPaises = false;

    public ObservableCollection<string> PaisesFiltrados { get; } = new();

    private List<string> _todosPaises = new();

    // Ordenamiento por fecha
    [ObservableProperty]
    private bool _ordenAscendente = false; // false = más reciente primero

    public string IconoOrden => OrdenAscendente ? "▲" : "▼";
    public string TooltipOrden => OrdenAscendente ? "Ordenar de más reciente a más antiguo" : "Ordenar de más antiguo a más reciente";

    partial void OnOrdenAscendenteChanged(bool value)
    {
        OnPropertyChanged(nameof(IconoOrden));
        OnPropertyChanged(nameof(TooltipOrden));
    }

    // ============================================
    // COLECCIONES PARA AUTOCOMPLETADO
    // ============================================

    // Datos completos
    private List<ComercioItem> _todosLosComerciosData = new();
    private List<LocalItem> _todosLosLocalesData = new();
    private List<string> _todasLasDivisasData = new();

    // Sugerencias filtradas
    public ObservableCollection<string> SugerenciasComercio { get; } = new();
    public ObservableCollection<string> SugerenciasLocal { get; } = new();
    public ObservableCollection<string> SugerenciasDivisa { get; } = new();

    // Colecciones para ComboBox de otros paneles
    public ObservableCollection<string> PaisesDestinoDisponibles { get; } = new() { "Todos" };
    
    // Estados con PAGADO agregado
    public ObservableCollection<string> EstadosAlimentos { get; } = new() 
    { 
        "Todos", 
        "PENDIENTE", 
        "PAGADO",
        "ENVIADO", 
        "ANULADO" 
    };

    // Mapeo de comercio/local a ID
    private readonly Dictionary<string, int> _comercioIdMap = new();
    private readonly Dictionary<string, int> _localIdMap = new();

    // Mapeo de nombres de divisas a codigos
    private static readonly Dictionary<string, string> NombresDivisas = new(StringComparer.OrdinalIgnoreCase)
    {
        { "DOLAR", "USD" }, { "DOLARES", "USD" }, { "DÓLAR", "USD" }, { "DÓLARES", "USD" },
        { "LIBRA", "GBP" }, { "LIBRAS", "GBP" },
        { "FRANCO", "CHF" }, { "FRANCOS", "CHF" },
        { "YEN", "JPY" }, { "YENES", "JPY" },
        { "PESO", "MXN" }, { "PESOS", "MXN" },
        { "REAL", "BRL" }, { "REALES", "BRL" },
        { "YUAN", "CNY" }, { "YUANES", "CNY" },
        { "RUPIA", "INR" }, { "RUPIAS", "INR" },
        { "CORONA", "SEK" }, { "CORONAS", "SEK" },
        { "EURO", "EUR" }, { "EUROS", "EUR" },
        { "DOLAR CANADIENSE", "CAD" }, { "CANADIENSE", "CAD" },
        { "DOLAR AUSTRALIANO", "AUD" }, { "AUSTRALIANO", "AUD" },
        { "PESO ARGENTINO", "ARS" }, { "ARGENTINO", "ARS" },
        { "PESO CHILENO", "CLP" }, { "CHILENO", "CLP" },
        { "WON", "KRW" },
        { "DOLAR SINGAPUR", "SGD" }, { "SINGAPUR", "SGD" },
        { "DOLAR HONG KONG", "HKD" }, { "HONG KONG", "HKD" },
        { "CORONA NORUEGA", "NOK" }, { "NORUEGA", "NOK" },
        { "CORONA SUECA", "SEK" }, { "SUECA", "SEK" },
        { "CORONA DANESA", "DKK" }, { "DANESA", "DKK" },
        { "DOLAR NEOZELANDES", "NZD" }, { "NEOZELANDES", "NZD" },
        { "RAND", "ZAR" },
    };

    // Mapeo inverso: codigo a nombre principal (para mostrar en sugerencias)
    private static readonly Dictionary<string, string> CodigoANombre = new(StringComparer.OrdinalIgnoreCase)
    {
        { "USD", "Dolar" },
        { "EUR", "Euro" },
        { "GBP", "Libra" },
        { "CHF", "Franco Suizo" },
        { "JPY", "Yen" },
        { "MXN", "Peso Mexicano" },
        { "BRL", "Real" },
        { "CNY", "Yuan" },
        { "INR", "Rupia" },
        { "CAD", "Dolar Canadiense" },
        { "AUD", "Dolar Australiano" },
        { "ARS", "Peso Argentino" },
        { "CLP", "Peso Chileno" },
        { "KRW", "Won" },
        { "SGD", "Dolar Singapur" },
        { "HKD", "Dolar Hong Kong" },
        { "NOK", "Corona Noruega" },
        { "SEK", "Corona Sueca" },
        { "DKK", "Corona Danesa" },
        { "NZD", "Dolar Neozelandes" },
        { "ZAR", "Rand" },
    };

    // ============================================
    // PANEL ACTUAL
    // ============================================

    [ObservableProperty]
    private string _panelActual = "divisa";

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
    // COLECCIONES DE DATOS
    // ============================================

    public ObservableCollection<OperacionAdminItem> OperacionesDivisa { get; } = new();
    public ObservableCollection<OperacionAlimentosAdminItem> OperacionesAlimentos { get; } = new();
    public ObservableCollection<OperacionBilletesAdminItem> OperacionesBilletes { get; } = new();
    public ObservableCollection<OperacionViajeAdminItem> OperacionesViaje { get; } = new();

    // Desglose de divisas del local
    public ObservableCollection<DivisaDesglose> DesgloseDivisas { get; } = new();

    // ============================================
    // RESUMEN DIVISAS
    // ============================================

    [ObservableProperty]
    private string _totalOperaciones = "0";

    [ObservableProperty]
    private string _totalEuros = "0.00";

    [ObservableProperty]
    private string _totalDivisas = "0.00";

    // Coleccion dinamica de totales por divisa (solo las que tienen valor > 0)
    public ObservableCollection<TotalDivisaItem> TotalesDivisasVisibles { get; } = new();

    // Mostrar/ocultar popup de desglose de divisas
    [ObservableProperty]
    private bool _mostrarDesgloseDivisas = false;

    // Resumen Pack Alimentos (4 estados)
    [ObservableProperty]
    private string _totalPendientes = "0";

    [ObservableProperty]
    private string _totalPagados = "0";

    [ObservableProperty]
    private string _totalEnviados = "0";

    [ObservableProperty]
    private string _totalAnulados = "0";

    [ObservableProperty]
    private string _totalImporteAlimentos = "0.00";

    [ObservableProperty]
    private string _totalImporteAlimentosColor = "#dc3545";

    // Seleccion multiple
    [ObservableProperty]
    private int _cantidadSeleccionados = 0;

    [ObservableProperty]
    private bool _haySeleccionados = false;

    // ============================================
    // ESTADOS
    // ============================================

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private string _successMessage = "";

    [ObservableProperty]
    private bool _mostrarFiltros = true;

    [ObservableProperty]
    private string _fechaActualTexto = "";

    // ============================================
    // EXPORTACION
    // ============================================

    [ObservableProperty]
    private bool _mostrarModalExportar = false;

    [ObservableProperty]
    private bool _formatoPDF = true;

    [ObservableProperty]
    private bool _formatoExcel = false;

    [ObservableProperty]
    private bool _formatoCSV = false;

    // Diccionario para almacenar totales por divisa (para exportacion)
    private Dictionary<string, decimal> _totalesPorDivisaExport = new();

    public string PanelActualTexto => PanelActual switch
    {
        "divisa" => "Divisas",
        "alimentos" => "Pack Alimentos",
        "billetes" => "Billetes Avion",
        "viaje" => "Pack Viaje",
        _ => PanelActual
    };

    // ============================================
    // CONSTRUCTOR
    // ============================================

    public OperacionesAdminViewModel()
    {
        InicializarFechas();
        _ = CargarDatosInicialesAsync();
    }

    private void InicializarFechas()
    {
        var hoy = ObtenerHoraEspana();
        FechaDesdeTexto = $"01/{hoy.Month:D2}/{hoy.Year}";
        FechaHastaTexto = $"{hoy.Day:D2}/{hoy.Month:D2}/{hoy.Year}";

        var diaSemana = ObtenerDiaSemana(hoy.DayOfWeek);
        var mes = ObtenerNombreMes(hoy.Month);
        FechaActualTexto = $"{diaSemana}, {hoy.Day} de {mes} de {hoy.Year}";
    }

    private DateTime ObtenerHoraEspana()
    {
        try
        {
            var zonaEspana = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaEspana);
        }
        catch
        {
            try
            {
                var zonaEspana = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaEspana);
            }
            catch
            {
                return DateTime.Now;
            }
        }
    }

    private string ObtenerDiaSemana(DayOfWeek dia)
    {
        return dia switch
        {
            DayOfWeek.Monday => "Lunes",
            DayOfWeek.Tuesday => "Martes",
            DayOfWeek.Wednesday => "Miercoles",
            DayOfWeek.Thursday => "Jueves",
            DayOfWeek.Friday => "Viernes",
            DayOfWeek.Saturday => "Sabado",
            DayOfWeek.Sunday => "Domingo",
            _ => ""
        };
    }

    private string ObtenerNombreMes(int mes)
    {
        string[] meses = { "", "enero", "febrero", "marzo", "abril", "mayo", "junio",
                          "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };
        return mes >= 1 && mes <= 12 ? meses[mes] : "";
    }

    // ============================================
    // CONVERSION DE NOMBRES DE DIVISAS
    // ============================================

    private string ObtenerCodigoDivisa(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return texto;

        var textoLimpio = texto.Trim();

        // Si tiene formato "Nombre (CODIGO)", extraer el codigo
        if (textoLimpio.Contains("(") && textoLimpio.EndsWith(")"))
        {
            var inicio = textoLimpio.LastIndexOf('(');
            var codigo = textoLimpio.Substring(inicio + 1, textoLimpio.Length - inicio - 2);
            return codigo;
        }

        // Buscar si es un nombre de divisa conocido
        if (NombresDivisas.TryGetValue(textoLimpio, out var codigoEncontrado))
        {
            return codigoEncontrado;
        }

        // Si no es un nombre conocido, devolver el texto tal cual (puede ser el codigo)
        return textoLimpio;
    }

    // ============================================
    // AUTOCOMPLETADO - COMERCIO
    // ============================================

    partial void OnFiltroComercioTextoChanged(string value)
    {
        FiltrarSugerenciasComercio(value);
        // Al cambiar comercio, resetear local
        FiltroLocalTexto = "";
        SugerenciasLocal.Clear();
    }

    private void FiltrarSugerenciasComercio(string texto)
    {
        SugerenciasComercio.Clear();

        if (string.IsNullOrWhiteSpace(texto))
        {
            MostrarSugerenciasComercio = false;
            return;
        }

        var coincidencias = _todosLosComerciosData
            .Where(c => c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .Take(10)
            .Select(c => c.Nombre);

        foreach (var item in coincidencias)
        {
            SugerenciasComercio.Add(item);
        }

        MostrarSugerenciasComercio = SugerenciasComercio.Count > 0;
    }

    [RelayCommand]
    private void SeleccionarComercio(string? comercio)
    {
        if (string.IsNullOrEmpty(comercio)) return;
        FiltroComercioTexto = comercio;
        MostrarSugerenciasComercio = false;
    }

    // ============================================
    // AUTOCOMPLETADO - LOCAL
    // ============================================

    partial void OnFiltroLocalTextoChanged(string value)
    {
        FiltrarSugerenciasLocal(value);
    }

    private void FiltrarSugerenciasLocal(string texto)
    {
        SugerenciasLocal.Clear();

        if (string.IsNullOrWhiteSpace(texto))
        {
            MostrarSugerenciasLocal = false;
            return;
        }

        // Si hay un comercio seleccionado, filtrar solo sus locales
        int? idComercioSeleccionado = null;
        if (!string.IsNullOrWhiteSpace(FiltroComercioTexto) && _comercioIdMap.ContainsKey(FiltroComercioTexto))
        {
            idComercioSeleccionado = _comercioIdMap[FiltroComercioTexto];
        }

        var coincidencias = _todosLosLocalesData
            .Where(l => 
                (idComercioSeleccionado == null || l.IdComercio == idComercioSeleccionado) &&
                (l.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase) || 
                 l.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)))
            .Take(10)
            .Select(l => $"{l.Codigo} - {l.Nombre}");

        foreach (var item in coincidencias)
        {
            SugerenciasLocal.Add(item);
        }

        MostrarSugerenciasLocal = SugerenciasLocal.Count > 0;
    }

    [RelayCommand]
    private void SeleccionarLocal(string? local)
    {
        if (string.IsNullOrEmpty(local)) return;
        FiltroLocalTexto = local;
        MostrarSugerenciasLocal = false;
    }

    // ============================================
    // AUTOCOMPLETADO - DIVISA
    // ============================================

    partial void OnFiltroDivisaTextoChanged(string value)
    {
        FiltrarSugerenciasDivisa(value);
    }

    private void FiltrarSugerenciasDivisa(string texto)
    {
        SugerenciasDivisa.Clear();

        if (string.IsNullOrWhiteSpace(texto))
        {
            MostrarSugerenciasDivisa = false;
            return;
        }

        var textoLower = texto.ToLower();
        var sugerenciasAgregadas = new HashSet<string>();

        // Buscar por codigo de divisa existente en la base de datos
        foreach (var codigo in _todasLasDivisasData)
        {
            // Obtener el nombre correspondiente al codigo
            var nombre = CodigoANombre.GetValueOrDefault(codigo, codigo);
            var displayText = $"{nombre} ({codigo})";

            // Buscar por codigo o por nombre
            if (codigo.ToLower().Contains(textoLower) ||
                nombre.ToLower().Contains(textoLower))
            {
                if (sugerenciasAgregadas.Add(displayText))
                {
                    SugerenciasDivisa.Add(displayText);
                    if (SugerenciasDivisa.Count >= 10) break;
                }
            }
        }

        MostrarSugerenciasDivisa = SugerenciasDivisa.Count > 0;
    }

    [RelayCommand]
    private void SeleccionarDivisa(string? divisa)
    {
        if (string.IsNullOrEmpty(divisa)) return;
        FiltroDivisaTexto = divisa;
        MostrarSugerenciasDivisa = false;
    }

    // ============================================
    // CARGA DE DATOS INICIALES
    // ============================================

    private async Task CargarDatosInicialesAsync()
    {
        try
        {
            IsLoading = true;
            await CargarComerciosAsync();
            await CargarTodosLosLocalesAsync();
            await CargarDivisasDisponiblesAsync();
            await CargarPaisesDestinoAsync();
            await CargarOperacionesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar datos: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CargarComerciosAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = "SELECT id_comercio, nombre_comercio FROM comercios WHERE activo = true ORDER BY nombre_comercio";
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            _todosLosComerciosData.Clear();
            _comercioIdMap.Clear();

            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var nombre = reader.GetString(1);
                _todosLosComerciosData.Add(new ComercioItem { Id = id, Nombre = nombre });
                _comercioIdMap[nombre] = id;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar comercios: {ex.Message}");
        }
    }

    private async Task CargarTodosLosLocalesAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = "SELECT id_local, id_comercio, codigo_local, nombre_local FROM locales WHERE activo = true ORDER BY codigo_local";
            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            _todosLosLocalesData.Clear();
            _localIdMap.Clear();

            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                var idComercio = reader.GetInt32(1);
                var codigo = reader.GetString(2);
                var nombre = reader.IsDBNull(3) ? codigo : reader.GetString(3);
                
                _todosLosLocalesData.Add(new LocalItem 
                { 
                    Id = id, 
                    IdComercio = idComercio, 
                    Codigo = codigo, 
                    Nombre = nombre 
                });
                _localIdMap[$"{codigo} - {nombre}"] = id;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar locales: {ex.Message}");
        }
    }

    private async Task CargarDivisasDisponiblesAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = @"SELECT DISTINCT od.divisa_origen
                        FROM operaciones_divisas od
                        WHERE od.divisa_origen IS NOT NULL AND od.divisa_origen != ''
                        ORDER BY od.divisa_origen";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            _todasLasDivisasData.Clear();

            while (await reader.ReadAsync())
            {
                var divisa = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(divisa))
                    _todasLasDivisasData.Add(divisa);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar divisas: {ex.Message}");
        }
    }

    private async Task CargarPaisesDestinoAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = @"SELECT DISTINCT opa.pais_destino
                        FROM operaciones_pack_alimentos opa
                        WHERE opa.pais_destino IS NOT NULL AND opa.pais_destino != ''
                        ORDER BY opa.pais_destino";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            PaisesDestinoDisponibles.Clear();
            PaisesDestinoDisponibles.Add("Todos");
            _todosPaises.Clear();

            while (await reader.ReadAsync())
            {
                var pais = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(pais))
                {
                    PaisesDestinoDisponibles.Add(pais);
                    _todosPaises.Add(pais);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar paises: {ex.Message}");
        }
    }

    // ============================================
    // COMANDOS
    // ============================================

    [RelayCommand]
    private void CambiarPanel(string? panel)
    {
        if (string.IsNullOrWhiteSpace(panel)) return;

        PanelActual = panel;

        OnPropertyChanged(nameof(EsPanelDivisa));
        OnPropertyChanged(nameof(EsPanelAlimentos));
        OnPropertyChanged(nameof(EsPanelBilletes));
        OnPropertyChanged(nameof(EsPanelViaje));
        OnPropertyChanged(nameof(TabDivisaBackground));
        OnPropertyChanged(nameof(TabDivisaForeground));
        OnPropertyChanged(nameof(TabAlimentosBackground));
        OnPropertyChanged(nameof(TabAlimentosForeground));
        OnPropertyChanged(nameof(TabBilletesBackground));
        OnPropertyChanged(nameof(TabBilletesForeground));
        OnPropertyChanged(nameof(TabViajeBackground));
        OnPropertyChanged(nameof(TabViajeForeground));

        _ = CargarOperacionesAsync();
    }

    [RelayCommand]
    private async Task BuscarAsync()
    {
        // Cerrar sugerencias
        MostrarSugerenciasComercio = false;
        MostrarSugerenciasLocal = false;
        MostrarSugerenciasDivisa = false;
        MostrarListaPaises = false;

        await CargarOperacionesAsync();
    }

    // Autocompletado de país destino
    partial void OnTextoBusquedaPaisDestinoChanged(string value)
    {
        FiltrarPaises(value);
        FiltroPaisDestino = value;
    }

    private void FiltrarPaises(string texto)
    {
        PaisesFiltrados.Clear();
        if (string.IsNullOrWhiteSpace(texto))
        {
            MostrarListaPaises = false;
            return;
        }

        var coincidencias = _todosPaises
            .Where(p => p.Contains(texto, StringComparison.OrdinalIgnoreCase))
            .Take(10);

        foreach (var pais in coincidencias)
            PaisesFiltrados.Add(pais);

        MostrarListaPaises = PaisesFiltrados.Count > 0;
    }

    [RelayCommand]
    private void SeleccionarPaisDestino(string? pais)
    {
        if (string.IsNullOrEmpty(pais)) return;
        TextoBusquedaPaisDestino = pais;
        FiltroPaisDestino = pais;
        MostrarListaPaises = false;
    }

    [RelayCommand]
    private async Task CambiarOrden()
    {
        OrdenAscendente = !OrdenAscendente;
        await CargarOperacionesAsync();
    }

    [RelayCommand]
    private async Task LimpiarFiltros()
    {
        var hoy = ObtenerHoraEspana();
        FechaDesdeTexto = $"01/{hoy.Month:D2}/{hoy.Year}";
        FechaHastaTexto = $"{hoy.Day:D2}/{hoy.Month:D2}/{hoy.Year}";
        FiltroComercioTexto = "";
        FiltroLocalTexto = "";
        FiltroDivisaTexto = "";
        FiltroNumeroOperacion = "";
        FiltroNumeroOperacionGlobal = "";
        FiltroPaisDestino = "";
        TextoBusquedaPaisDestino = "";
        FiltroEstadoAlimentos = "Todos";

        MostrarSugerenciasComercio = false;
        MostrarSugerenciasLocal = false;
        MostrarSugerenciasDivisa = false;
        MostrarListaPaises = false;

        await CargarOperacionesAsync();
    }

    [RelayCommand]
    private void ToggleFiltros()
    {
        MostrarFiltros = !MostrarFiltros;
    }

    [RelayCommand]
    private void ToggleDesgloseDivisas()
    {
        MostrarDesgloseDivisas = !MostrarDesgloseDivisas;
    }

    [RelayCommand]
    private void CerrarSugerencias()
    {
        MostrarSugerenciasComercio = false;
        MostrarSugerenciasLocal = false;
        MostrarSugerenciasDivisa = false;
    }

    // ============================================
    // CAMBIO DE ESTADO (Solo Pagado -> Enviado)
    // ============================================

    [RelayCommand]
    private async Task CambiarEstadoAEnviado(string? idOperacion)
    {
        if (string.IsNullOrEmpty(idOperacion)) return;

        if (!int.TryParse(idOperacion, out int id))
        {
            ErrorMessage = "ID de operacion invalido";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = "";

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Verificar que el estado actual sea PAGADO
            var sqlVerificar = @"SELECT opa.estado_envio 
                                 FROM operaciones_pack_alimentos opa 
                                 WHERE opa.id_operacion = @idOperacion";
            
            await using var cmdVerificar = new NpgsqlCommand(sqlVerificar, conn);
            cmdVerificar.Parameters.AddWithValue("idOperacion", id);
            
            var estadoActual = await cmdVerificar.ExecuteScalarAsync() as string;

            if (estadoActual?.ToUpper() != "PAGADO")
            {
                ErrorMessage = "Solo se pueden marcar como enviadas las operaciones con estado PAGADO";
                return;
            }

            // Actualizar estado a ENVIADO
            var sqlActualizar = @"UPDATE operaciones_pack_alimentos 
                                  SET estado_envio = 'ENVIADO',
                                      fecha_envio = @fechaEnvio
                                  WHERE id_operacion = @idOperacion";

            await using var cmdActualizar = new NpgsqlCommand(sqlActualizar, conn);
            cmdActualizar.Parameters.AddWithValue("idOperacion", id);
            cmdActualizar.Parameters.AddWithValue("fechaEnvio", ObtenerHoraEspana());

            var filasAfectadas = await cmdActualizar.ExecuteNonQueryAsync();

            if (filasAfectadas > 0)
            {
                // Registrar en historial
                try
                {
                    var queryHistorial = @"
                        INSERT INTO historial_estados_pack_alimentos
                            (id_operacion, estado_anterior, estado_nuevo, fecha_cambio, id_usuario, observaciones)
                        VALUES
                            (@id_operacion, @estado_anterior, 'ENVIADO', CURRENT_TIMESTAMP, NULL, @observaciones)";

                    await using var cmdHistorial = new NpgsqlCommand(queryHistorial, conn);
                    cmdHistorial.Parameters.AddWithValue("@id_operacion", id);
                    cmdHistorial.Parameters.AddWithValue("@estado_anterior", estadoActual ?? "PAGADO");
                    cmdHistorial.Parameters.AddWithValue("@observaciones", "Cambio de estado desde BackOffice Admin");
                    await cmdHistorial.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Si falla el historial, no es crítico
                }

                SuccessMessage = "Estado actualizado a ENVIADO correctamente";

                // Recargar operaciones
                await CargarOperacionesAsync();

                // Limpiar mensaje despues de 3 segundos
                await Task.Delay(3000);
                SuccessMessage = "";
            }
            else
            {
                ErrorMessage = "No se pudo actualizar el estado";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cambiar estado: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task MarcarSeleccionadosComoEnviados()
    {
        var seleccionados = OperacionesAlimentos
            .Where(o => o.EstaSeleccionado && o.PuedeMarcarEnviado)
            .ToList();

        if (seleccionados.Count == 0)
        {
            ErrorMessage = "No hay operaciones PAGADAS seleccionadas";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = "";

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            int actualizados = 0;
            var fechaEnvio = ObtenerHoraEspana();

            foreach (var op in seleccionados)
            {
                if (int.TryParse(op.NumeroOperacionGlobal, out int id))
                {
                    var sql = @"UPDATE operaciones_pack_alimentos
                                SET estado_envio = 'ENVIADO',
                                    fecha_envio = @fechaEnvio
                                WHERE id_operacion = @idOperacion
                                AND estado_envio = 'PAGADO'";

                    await using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("idOperacion", id);
                    cmd.Parameters.AddWithValue("fechaEnvio", fechaEnvio);

                    var resultado = await cmd.ExecuteNonQueryAsync();
                    if (resultado > 0)
                    {
                        actualizados++;

                        // Registrar en historial
                        try
                        {
                            var queryHistorial = @"
                                INSERT INTO historial_estados_pack_alimentos
                                    (id_operacion, estado_anterior, estado_nuevo, fecha_cambio, id_usuario, observaciones)
                                VALUES
                                    (@id_operacion, 'PAGADO', 'ENVIADO', CURRENT_TIMESTAMP, NULL, @observaciones)";

                            await using var cmdHistorial = new NpgsqlCommand(queryHistorial, conn);
                            cmdHistorial.Parameters.AddWithValue("@id_operacion", id);
                            cmdHistorial.Parameters.AddWithValue("@observaciones", "Cambio masivo desde BackOffice Admin");
                            await cmdHistorial.ExecuteNonQueryAsync();
                        }
                        catch
                        {
                            // Si falla el historial, no es crítico
                        }
                    }
                }
            }

            if (actualizados > 0)
            {
                SuccessMessage = $"{actualizados} operacion(es) marcada(s) como ENVIADO";
                await CargarOperacionesAsync();
                
                await Task.Delay(3000);
                SuccessMessage = "";
            }
            else
            {
                ErrorMessage = "No se pudo actualizar ninguna operacion";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cambiar estados: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SeleccionarTodosPagados()
    {
        foreach (var op in OperacionesAlimentos.Where(o => o.PuedeMarcarEnviado))
        {
            op.EstaSeleccionado = true;
        }
        ActualizarContadorSeleccionados();
    }

    [RelayCommand]
    private void DeseleccionarTodos()
    {
        foreach (var op in OperacionesAlimentos)
        {
            op.EstaSeleccionado = false;
        }
        ActualizarContadorSeleccionados();
    }

    [RelayCommand]
    private void ActualizarSeleccion()
    {
        ActualizarContadorSeleccionados();
    }

    private void ActualizarContadorSeleccionados()
    {
        CantidadSeleccionados = OperacionesAlimentos.Count(o => o.EstaSeleccionado && o.PuedeMarcarEnviado);
        HaySeleccionados = CantidadSeleccionados > 0;
    }

    // ============================================
    // EXPORTACION
    // ============================================

    [RelayCommand]
    private void Exportar()
    {
        // Mostrar modal de exportacion
        FormatoPDF = true;
        FormatoExcel = false;
        FormatoCSV = false;
        MostrarModalExportar = true;
    }

    [RelayCommand]
    private void CancelarExportar()
    {
        MostrarModalExportar = false;
    }

    [RelayCommand]
    private async Task ConfirmarExportar()
    {
        try
        {
            MostrarModalExportar = false;

            // Determinar extension y filtro
            string extension;
            string filtroNombre;
            string filtroExtension;

            if (FormatoPDF)
            {
                extension = "pdf";
                filtroNombre = "PDF";
                filtroExtension = "pdf";
            }
            else if (FormatoExcel)
            {
                extension = "xlsx";
                filtroNombre = "Excel";
                filtroExtension = "xlsx";
            }
            else
            {
                extension = "csv";
                filtroNombre = "CSV";
                filtroExtension = "csv";
            }

            // Mostrar dialogo para elegir ubicacion
            var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
                ? desktop.MainWindow 
                : null);

            if (topLevel == null)
            {
                ErrorMessage = "No se pudo abrir el dialogo de guardado";
                return;
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var nombreSugerido = $"Operaciones_{PanelActual}_{timestamp}.{extension}";

            var archivo = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar archivo de exportacion",
                SuggestedFileName = nombreSugerido,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(filtroNombre) { Patterns = new[] { $"*.{filtroExtension}" } }
                }
            });

            if (archivo == null)
            {
                // Usuario cancelo
                return;
            }

            IsLoading = true;

            byte[] contenido;

            if (PanelActual == "alimentos")
            {
                contenido = await ExportarAlimentosAsync(extension);
            }
            else
            {
                // Preparar datos de exportacion para Divisas
                var datos = new ExportacionOperacionesService.DatosExportacion
                {
                    Modulo = PanelActualTexto,
                    FechaGeneracion = DateTime.Now.ToString("dd/MM/yyyy"),
                    HoraGeneracion = DateTime.Now.ToString("HH:mm:ss"),
                    FiltroFechaDesde = string.IsNullOrEmpty(FechaDesdeTexto) ? null : FechaDesdeTexto,
                    FiltroFechaHasta = string.IsNullOrEmpty(FechaHastaTexto) ? null : FechaHastaTexto,
                    FiltroComercio = string.IsNullOrEmpty(FiltroComercioTexto) ? null : FiltroComercioTexto,
                    FiltroLocal = string.IsNullOrEmpty(FiltroLocalTexto) ? null : FiltroLocalTexto,
                    FiltroDivisa = string.IsNullOrEmpty(FiltroDivisaTexto) ? null : FiltroDivisaTexto,
                    TotalOperaciones = int.TryParse(TotalOperaciones, out var total) ? total : 0,
                    TotalEuros = decimal.TryParse(TotalEuros.Replace(",", ""), out var euros) ? euros : 0,
                    TotalesPorDivisa = _totalesPorDivisaExport,
                    Operaciones = OperacionesDivisa.Select(op => new ExportacionOperacionesService.OperacionExportar
                    {
                        Fecha = op.Fecha,
                        Hora = op.Hora,
                        IdOperacion = int.TryParse(op.NumeroOperacionGlobal, out var id) ? id : 0,
                        NumeroOperacion = op.NumeroOperacion,
                        CodigoLocal = op.CodigoLocal,
                        NombreComercio = op.NombreComercio,
                        Divisa = op.Divisa,
                        Cantidad = op.CantidadDivisaNum,
                        CantidadEuros = op.CantidadPagadaNum,
                        Cliente = op.Cliente,
                        TipoDocumento = op.TipoDocumento,
                        NumeroDocumento = op.NumeroDocumento
                    }).ToList()
                };

                if (FormatoPDF)
                    contenido = ExportacionOperacionesService.GenerarPDF(datos);
                else if (FormatoExcel)
                    contenido = ExportacionOperacionesService.GenerarExcel(datos);
                else
                    contenido = ExportacionOperacionesService.GenerarCSV(datos);
            }

            // Guardar archivo en la ubicacion seleccionada
            await using var stream = await archivo.OpenWriteAsync();
            await stream.WriteAsync(contenido);
            
            SuccessMessage = $"Archivo exportado correctamente";
            
            // Limpiar mensaje despues de 4 segundos
            await Task.Delay(4000);
            SuccessMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al exportar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<byte[]> ExportarAlimentosAsync(string formato)
    {
        // Usar el mismo servicio de exportacion adaptando los datos
        var datos = new ExportacionOperacionesService.DatosExportacion
        {
            Modulo = "Pack Alimentos",
            FechaGeneracion = DateTime.Now.ToString("dd/MM/yyyy"),
            HoraGeneracion = DateTime.Now.ToString("HH:mm:ss"),
            FiltroFechaDesde = string.IsNullOrEmpty(FechaDesdeTexto) ? null : FechaDesdeTexto,
            FiltroFechaHasta = string.IsNullOrEmpty(FechaHastaTexto) ? null : FechaHastaTexto,
            FiltroComercio = string.IsNullOrEmpty(FiltroComercioTexto) ? null : FiltroComercioTexto,
            FiltroLocal = string.IsNullOrEmpty(FiltroLocalTexto) ? null : FiltroLocalTexto,
            FiltroDivisa = null,
            TotalOperaciones = int.TryParse(TotalOperaciones, out var total) ? total : 0,
            TotalEuros = decimal.TryParse(TotalImporteAlimentos.Replace(",", "").Replace("-", ""), out var imp) ? imp : 0,
            TotalesPorDivisa = new Dictionary<string, decimal>
            {
                { "Pendientes", int.TryParse(TotalPendientes, out var pend) ? pend : 0 },
                { "Pagados", int.TryParse(TotalPagados, out var pag) ? pag : 0 },
                { "Enviados", int.TryParse(TotalEnviados, out var env) ? env : 0 },
                { "Anulados", int.TryParse(TotalAnulados, out var anul) ? anul : 0 }
            },
            Operaciones = OperacionesAlimentos.Select(op => new ExportacionOperacionesService.OperacionExportar
            {
                Fecha = op.Fecha,
                Hora = op.Hora,
                IdOperacion = int.TryParse(op.NumeroOperacionGlobal, out var id) ? id : 0,
                NumeroOperacion = op.NumeroOperacion,
                CodigoLocal = op.CodigoLocal,
                NombreComercio = op.NombreComercio,
                Divisa = op.PaisDestino,
                Cantidad = 0,
                CantidadEuros = op.ImporteNum,
                Cliente = op.NombreCliente,
                TipoDocumento = op.EstadoTexto,
                NumeroDocumento = op.NombreBeneficiario
            }).ToList()
        };

        await Task.CompletedTask;

        return formato switch
        {
            "pdf" => ExportacionOperacionesService.GenerarPDF(datos),
            "xlsx" => ExportacionOperacionesService.GenerarExcel(datos),
            _ => ExportacionOperacionesService.GenerarCSV(datos)
        };
    }

    // ============================================
    // CARGA DE OPERACIONES
    // ============================================

    private async Task CargarOperacionesAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = "";

            var fechaDesde = ParsearFecha(FechaDesdeTexto);
            var fechaHasta = ParsearFecha(FechaHastaTexto);

            int? idComercio = !string.IsNullOrEmpty(FiltroComercioTexto) && _comercioIdMap.ContainsKey(FiltroComercioTexto)
                ? _comercioIdMap[FiltroComercioTexto]
                : null;

            int? idLocal = null;
            if (!string.IsNullOrEmpty(FiltroLocalTexto))
            {
                // Buscar el local por el texto (puede ser parcial)
                var localMatch = _localIdMap.Keys.FirstOrDefault(k => k.Equals(FiltroLocalTexto, StringComparison.OrdinalIgnoreCase));
                if (localMatch != null)
                {
                    idLocal = _localIdMap[localMatch];
                }
            }

            switch (PanelActual)
            {
                case "divisa":
                    await CargarOperacionesDivisaAsync(fechaDesde, fechaHasta, idComercio, idLocal);
                    await CargarDesgloseDivisasAsync(idLocal);
                    break;
                case "alimentos":
                    await CargarOperacionesAlimentosAsync(fechaDesde, fechaHasta, idComercio, idLocal);
                    break;
                case "billetes":
                    await CargarOperacionesBilletesAsync(fechaDesde, fechaHasta, idComercio, idLocal);
                    break;
                case "viaje":
                    await CargarOperacionesViajeAsync(fechaDesde, fechaHasta, idComercio, idLocal);
                    break;
            }
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

    private DateTime? ParsearFecha(string fechaTexto)
    {
        if (string.IsNullOrWhiteSpace(fechaTexto)) return null;

        var partes = fechaTexto.Split('/');
        if (partes.Length == 3 &&
            int.TryParse(partes[0], out int dia) &&
            int.TryParse(partes[1], out int mes) &&
            int.TryParse(partes[2], out int anio))
        {
            try
            {
                return new DateTime(anio, mes, dia);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    // ============================================
    // OPERACIONES DIVISA
    // ============================================

    private async Task CargarOperacionesDivisaAsync(DateTime? fechaDesde, DateTime? fechaHasta, int? idComercio, int? idLocal)
    {
        OperacionesDivisa.Clear();

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var sql = @"SELECT 
                        o.id_operacion,
                        o.fecha_operacion,
                        o.hora_operacion,
                        o.numero_operacion,
                        l.codigo_local,
                        c.nombre_comercio,
                        od.divisa_origen,
                        od.cantidad_origen,
                        od.cantidad_destino,
                        CONCAT(cl.nombre, ' ', cl.apellidos) as cliente,
                        cl.documento_tipo,
                        cl.documento_numero
                    FROM operaciones o
                    INNER JOIN operaciones_divisas od ON o.id_operacion = od.id_operacion
                    INNER JOIN locales l ON o.id_local = l.id_local
                    INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                    LEFT JOIN clientes cl ON o.id_cliente = cl.id_cliente
                    WHERE o.modulo = 'DIVISAS'";

        if (fechaDesde.HasValue)
            sql += " AND o.fecha_operacion >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND o.fecha_operacion <= @fechaHasta";
        if (idComercio.HasValue)
            sql += " AND l.id_comercio = @idComercio";
        if (idLocal.HasValue)
            sql += " AND o.id_local = @idLocal";
        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacion))
            sql += " AND o.numero_operacion ILIKE @numOp";
        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionGlobal))
            sql += " AND o.id_operacion::text ILIKE @numOpGlobal";
        if (!string.IsNullOrWhiteSpace(FiltroDivisaTexto))
        {
            // Buscar por nombre de divisa o por codigo
            var codigoDivisa = ObtenerCodigoDivisa(FiltroDivisaTexto);
            sql += " AND od.divisa_origen ILIKE @divisa";
        }

        var ordenDir = OrdenAscendente ? "ASC" : "DESC";
        sql += $" ORDER BY o.fecha_operacion {ordenDir}, o.hora_operacion {ordenDir} LIMIT 500";

        await using var cmd = new NpgsqlCommand(sql, conn);

        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("fechaHasta", fechaHasta.Value.Date.AddDays(1).AddSeconds(-1));
        if (idComercio.HasValue)
            cmd.Parameters.AddWithValue("idComercio", idComercio.Value);
        if (idLocal.HasValue)
            cmd.Parameters.AddWithValue("idLocal", idLocal.Value);
        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacion))
            cmd.Parameters.AddWithValue("numOp", $"%{FiltroNumeroOperacion}%");
        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionGlobal))
            cmd.Parameters.AddWithValue("numOpGlobal", $"%{FiltroNumeroOperacionGlobal}%");
        if (!string.IsNullOrWhiteSpace(FiltroDivisaTexto))
        {
            var codigoDivisa = ObtenerCodigoDivisa(FiltroDivisaTexto);
            cmd.Parameters.AddWithValue("divisa", $"%{codigoDivisa}%");
        }

        await using var reader = await cmd.ExecuteReaderAsync();

        int index = 0;
        decimal totalEuros = 0;
        
        // Totales por divisa
        var totalesPorDivisa = new Dictionary<string, decimal>();

        while (await reader.ReadAsync())
        {
            var idOperacion = reader.GetInt32(0);
            var fecha = reader.GetDateTime(1);
            var hora = reader.IsDBNull(2) ? TimeSpan.Zero : reader.GetTimeSpan(2);
            var numOp = reader.IsDBNull(3) ? "" : reader.GetString(3);
            var codigoLocal = reader.GetString(4);
            var comercio = reader.GetString(5);
            var divisa = reader.GetString(6);
            var cantidadOrigen = reader.GetDecimal(7);
            var cantidadDestino = reader.GetDecimal(8);
            var cliente = reader.IsDBNull(9) ? "" : reader.GetString(9);
            var tipoDoc = reader.IsDBNull(10) ? "" : reader.GetString(10);
            var numDoc = reader.IsDBNull(11) ? "" : reader.GetString(11);

            OperacionesDivisa.Add(new OperacionAdminItem
            {
                NumeroOperacionGlobal = idOperacion.ToString(),
                Fecha = fecha.ToString("dd/MM/yy"),
                Hora = hora.ToString(@"hh\:mm"),
                NumeroOperacion = numOp,
                CodigoLocal = codigoLocal,
                NombreComercio = comercio,
                Divisa = divisa,
                CantidadDivisa = $"{cantidadOrigen:N2}",
                CantidadPagada = $"{cantidadDestino:N2}",
                Cliente = cliente,
                TipoDocumento = tipoDoc,
                NumeroDocumento = numDoc,
                BackgroundColor = index % 2 == 0 ? "White" : "#F5F5F5",
                CantidadDivisaNum = cantidadOrigen,
                CantidadPagadaNum = cantidadDestino
            });

            totalEuros += cantidadDestino;
            
            // Acumular por divisa
            if (!totalesPorDivisa.ContainsKey(divisa))
                totalesPorDivisa[divisa] = 0;
            totalesPorDivisa[divisa] += cantidadOrigen;
            
            index++;
        }

        TotalOperaciones = index.ToString();
        TotalEuros = $"{totalEuros:N2}";

        // Almacenar para exportacion (totales de operaciones del periodo)
        _totalesPorDivisaExport = new Dictionary<string, decimal>(totalesPorDivisa);

        // NOTA: TotalDivisas y TotalesDivisasVisibles se llenan en CargarDesgloseDivisasAsync
        // con el stock actual del local (ENTRADA - SALIDA), no con las operaciones del periodo
    }

    // ============================================
    // DESGLOSE DE DIVISAS DEL LOCAL (Stock actual)
    // ============================================

    private async Task CargarDesgloseDivisasAsync(int? idLocal)
    {
        DesgloseDivisas.Clear();
        TotalesDivisasVisibles.Clear();

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Obtener el stock actual de divisas usando balance_divisas
            // ENTRADA = compra de divisas (entra al local)
            // SALIDA = deposito al banco (sale del local)
            // Stock actual = ENTRADA - SALIDA
            string sql;

            if (idLocal.HasValue)
            {
                // Si hay un local seleccionado, mostrar solo sus divisas
                sql = @"SELECT
                            codigo_divisa,
                            SUM(CASE WHEN tipo_movimiento = 'ENTRADA' THEN cantidad_recibida ELSE 0 END) -
                            SUM(CASE WHEN tipo_movimiento = 'SALIDA' THEN cantidad_recibida ELSE 0 END) as saldo
                        FROM balance_divisas
                        WHERE id_local = @idLocal
                        GROUP BY codigo_divisa
                        HAVING SUM(CASE WHEN tipo_movimiento = 'ENTRADA' THEN cantidad_recibida ELSE 0 END) -
                               SUM(CASE WHEN tipo_movimiento = 'SALIDA' THEN cantidad_recibida ELSE 0 END) > 0
                        ORDER BY saldo DESC";
            }
            else
            {
                // Si no hay local seleccionado, mostrar el stock de TODOS los locales combinado
                sql = @"SELECT
                            codigo_divisa,
                            SUM(CASE WHEN tipo_movimiento = 'ENTRADA' THEN cantidad_recibida ELSE 0 END) -
                            SUM(CASE WHEN tipo_movimiento = 'SALIDA' THEN cantidad_recibida ELSE 0 END) as saldo
                        FROM balance_divisas
                        GROUP BY codigo_divisa
                        HAVING SUM(CASE WHEN tipo_movimiento = 'ENTRADA' THEN cantidad_recibida ELSE 0 END) -
                               SUM(CASE WHEN tipo_movimiento = 'SALIDA' THEN cantidad_recibida ELSE 0 END) > 0
                        ORDER BY saldo DESC";
            }

            await using var cmd = new NpgsqlCommand(sql, conn);
            if (idLocal.HasValue)
            {
                cmd.Parameters.AddWithValue("idLocal", idLocal.Value);
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            decimal totalDivisasStock = 0;

            while (await reader.ReadAsync())
            {
                var codigo = reader.GetString(0);
                var saldo = reader.GetDecimal(1);

                // Agregar al desglose
                DesgloseDivisas.Add(new DivisaDesglose
                {
                    CodigoDivisa = codigo,
                    CantidadDisponible = saldo,
                    CantidadDepositada = 0,
                    CantidadTotal = saldo
                });

                // Agregar a la coleccion visible de totales por divisa
                TotalesDivisasVisibles.Add(new TotalDivisaItem
                {
                    Codigo = codigo,
                    Total = saldo.ToString("N2"),
                    Color = TotalDivisaItem.ObtenerColor(codigo)
                });

                totalDivisasStock += saldo;
            }

            // Actualizar el total de divisas con el stock actual
            TotalDivisas = $"{totalDivisasStock:N2}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar desglose de divisas: {ex.Message}");
        }
    }

    // ============================================
    // OPERACIONES PACK ALIMENTOS
    // ============================================

    private async Task CargarOperacionesAlimentosAsync(DateTime? fechaDesde, DateTime? fechaHasta, int? idComercio, int? idLocal)
    {
        OperacionesAlimentos.Clear();

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var sql = @"SELECT 
                        o.id_operacion,
                        o.fecha_operacion,
                        o.hora_operacion,
                        o.numero_operacion,
                        l.codigo_local,
                        c.nombre_comercio,
                        opa.nombre_pack,
                        opa.pais_destino,
                        CONCAT(cl.nombre, ' ', cl.apellidos) as cliente,
                        CONCAT(cb.nombre, ' ', cb.apellido) as beneficiario,
                        opa.estado_envio,
                        o.importe_total
                    FROM operaciones o
                    INNER JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                    INNER JOIN locales l ON o.id_local = l.id_local
                    INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                    LEFT JOIN clientes cl ON o.id_cliente = cl.id_cliente
                    LEFT JOIN clientes_beneficiarios cb ON opa.id_beneficiario = cb.id_beneficiario
                    WHERE o.modulo = 'PACK_ALIMENTOS'";

        if (fechaDesde.HasValue)
            sql += " AND o.fecha_operacion >= @fechaDesde";
        if (fechaHasta.HasValue)
            sql += " AND o.fecha_operacion <= @fechaHasta";
        if (idComercio.HasValue)
            sql += " AND l.id_comercio = @idComercio";
        if (idLocal.HasValue)
            sql += " AND o.id_local = @idLocal";
        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacion))
            sql += " AND o.numero_operacion ILIKE @numOp";
        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionGlobal))
            sql += " AND o.id_operacion::text ILIKE @numOpGlobal";
        if (!string.IsNullOrEmpty(FiltroPaisDestino))
            sql += " AND opa.pais_destino ILIKE @pais";
        if (!string.IsNullOrEmpty(FiltroEstadoAlimentos) && FiltroEstadoAlimentos != "Todos")
            sql += " AND opa.estado_envio = @estado";

        var ordenDir = OrdenAscendente ? "ASC" : "DESC";
        sql += $" ORDER BY o.fecha_operacion {ordenDir}, o.hora_operacion {ordenDir} LIMIT 500";

        await using var cmd = new NpgsqlCommand(sql, conn);

        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("fechaHasta", fechaHasta.Value.Date.AddDays(1).AddSeconds(-1));
        if (idComercio.HasValue)
            cmd.Parameters.AddWithValue("idComercio", idComercio.Value);
        if (idLocal.HasValue)
            cmd.Parameters.AddWithValue("idLocal", idLocal.Value);
        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacion))
            cmd.Parameters.AddWithValue("numOp", $"%{FiltroNumeroOperacion}%");
        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionGlobal))
            cmd.Parameters.AddWithValue("numOpGlobal", $"%{FiltroNumeroOperacionGlobal}%");
        if (!string.IsNullOrEmpty(FiltroPaisDestino))
            cmd.Parameters.AddWithValue("pais", $"%{FiltroPaisDestino}%");
        if (!string.IsNullOrEmpty(FiltroEstadoAlimentos) && FiltroEstadoAlimentos != "Todos")
            cmd.Parameters.AddWithValue("estado", FiltroEstadoAlimentos);

        await using var reader = await cmd.ExecuteReaderAsync();

        int index = 0;
        int pendientes = 0, pagados = 0, enviados = 0, anulados = 0;
        decimal importePendientes = 0; // Pendientes = negativo (deuda)
        decimal importeEnviados = 0;   // Enviados = positivo (cobrado)

        while (await reader.ReadAsync())
        {
            var idOperacion = reader.GetInt32(0);
            var fecha = reader.GetDateTime(1);
            var hora = reader.IsDBNull(2) ? TimeSpan.Zero : reader.GetTimeSpan(2);
            var numOp = reader.IsDBNull(3) ? "" : reader.GetString(3);
            var codigoLocal = reader.GetString(4);
            var comercio = reader.GetString(5);
            var nombrePack = reader.IsDBNull(6) ? "" : reader.GetString(6);
            var paisDestino = reader.IsDBNull(7) ? "" : reader.GetString(7);
            var cliente = reader.IsDBNull(8) ? "" : reader.GetString(8);
            var beneficiario = reader.IsDBNull(9) ? "" : reader.GetString(9);
            var estado = reader.IsDBNull(10) ? "PENDIENTE" : reader.GetString(10);
            var importe = reader.IsDBNull(11) ? 0 : reader.GetDecimal(11);

            var estadoUpper = estado.Trim().ToUpper();
            var estadoColor = estadoUpper switch
            {
                "PAGADO" => "#17a2b8",
                "ENVIADO" => "#28a745",
                "ANULADO" => "#dc3545",
                _ => "#ffc107"  // Por defecto amarillo (PENDIENTE y otros)
            };

            // Determinar si se puede cambiar a Enviado (solo si esta en PAGADO)
            var puedeEnviar = estadoUpper == "PAGADO";

            OperacionesAlimentos.Add(new OperacionAlimentosAdminItem
            {
                NumeroOperacionGlobal = idOperacion.ToString(),
                Fecha = fecha.ToString("dd/MM/yy"),
                Hora = hora.ToString(@"hh\:mm"),
                NumeroOperacion = numOp,
                CodigoLocal = codigoLocal,
                NombreComercio = comercio,
                Descripcion = nombrePack,
                PaisDestino = paisDestino,
                NombreCliente = cliente,
                NombreBeneficiario = beneficiario,
                EstadoTexto = estadoUpper,
                EstadoColor = estadoColor,
                Importe = $"{importe:N2} EUR",
                ImporteNum = importe,
                BackgroundColor = index % 2 == 0 ? "White" : "#F5F5F5",
                PuedeMarcarEnviado = puedeEnviar
            });

            // Contar por estado y calcular importe
            switch (estadoUpper)
            {
                case "PENDIENTE":
                    pendientes++;
                    // Pendientes = deuda (se resta del total)
                    importePendientes += importe;
                    break;
                case "PAGADO":
                    pagados++;
                    // Pagado no cuenta para el importe total
                    break;
                case "ENVIADO":
                    enviados++;
                    // Enviados = cobrado (se suma al total)
                    importeEnviados += importe;
                    break;
                case "ANULADO":
                    anulados++;
                    // Anulado no cuenta para el importe total
                    break;
            }

            index++;
        }

        TotalOperaciones = index.ToString();
        TotalPendientes = pendientes.ToString();
        TotalPagados = pagados.ToString();
        TotalEnviados = enviados.ToString();
        TotalAnulados = anulados.ToString();

        // Importe total = Enviados (positivo) - Pendientes (negativo)
        // Pendientes representan deuda, Enviados representan ingresos cobrados
        decimal importeTotal = importeEnviados - importePendientes;

        if (importeTotal < 0)
        {
            TotalImporteAlimentos = $"{importeTotal:N2}";
            TotalImporteAlimentosColor = "#dc3545"; // Rojo (balance negativo)
        }
        else if (importeTotal > 0)
        {
            TotalImporteAlimentos = $"{importeTotal:N2}";
            TotalImporteAlimentosColor = "#28a745"; // Verde (balance positivo)
        }
        else
        {
            TotalImporteAlimentos = "0,00";
            TotalImporteAlimentosColor = "#6c757d"; // Gris (neutro)
        }
    }

    // ============================================
    // OPERACIONES BILLETES AVION (Placeholder)
    // ============================================

    private async Task CargarOperacionesBilletesAsync(DateTime? fechaDesde, DateTime? fechaHasta, int? idComercio, int? idLocal)
    {
        OperacionesBilletes.Clear();
        TotalOperaciones = "0";
        await Task.CompletedTask;
    }

    // ============================================
    // OPERACIONES PACK VIAJE (Placeholder)
    // ============================================

    private async Task CargarOperacionesViajeAsync(DateTime? fechaDesde, DateTime? fechaHasta, int? idComercio, int? idLocal)
    {
        OperacionesViaje.Clear();
        TotalOperaciones = "0";
        await Task.CompletedTask;
    }
}

// ============================================
// MODELOS AUXILIARES
// ============================================

public class ComercioItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

public class LocalItem
{
    public int Id { get; set; }
    public int IdComercio { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
}

// ============================================
// MODELOS DE DATOS
// ============================================

public class OperacionAdminItem
{
    public string NumeroOperacionGlobal { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string Hora { get; set; } = "";
    public string NumeroOperacion { get; set; } = "";
    public string CodigoLocal { get; set; } = "";
    public string NombreComercio { get; set; } = "";
    public string Divisa { get; set; } = "";
    public string CantidadDivisa { get; set; } = "";
    public string CantidadPagada { get; set; } = "";
    public string Cliente { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string BackgroundColor { get; set; } = "White";
    public decimal CantidadDivisaNum { get; set; }
    public decimal CantidadPagadaNum { get; set; }
}

public partial class OperacionAlimentosAdminItem : ObservableObject
{
    public string NumeroOperacionGlobal { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string Hora { get; set; } = "";
    public string NumeroOperacion { get; set; } = "";
    public string CodigoLocal { get; set; } = "";
    public string NombreComercio { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string PaisDestino { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string NombreBeneficiario { get; set; } = "";
    public string EstadoTexto { get; set; } = "";
    public string EstadoColor { get; set; } = "#ffc107";
    public string Importe { get; set; } = "";
    public decimal ImporteNum { get; set; }
    public string BackgroundColor { get; set; } = "White";
    public bool PuedeMarcarEnviado { get; set; } = false;
    
    // Para seleccion multiple
    [ObservableProperty]
    private bool _estaSeleccionado;
}

public class OperacionBilletesAdminItem
{
    public string NumeroOperacionGlobal { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string Hora { get; set; } = "";
    public string NumeroOperacion { get; set; } = "";
    public string CodigoLocal { get; set; } = "";
    public string NombreComercio { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Importe { get; set; } = "";
    public string BackgroundColor { get; set; } = "White";
}

public class OperacionViajeAdminItem
{
    public string NumeroOperacionGlobal { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string Hora { get; set; } = "";
    public string NumeroOperacion { get; set; } = "";
    public string CodigoLocal { get; set; } = "";
    public string NombreComercio { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Destino { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Importe { get; set; } = "";
    public string BackgroundColor { get; set; } = "White";
}

public class DivisaDesglose
{
    public string CodigoDivisa { get; set; } = "";
    public decimal CantidadDisponible { get; set; }
    public decimal CantidadDepositada { get; set; }
    public decimal CantidadTotal { get; set; }
    
    public string CantidadDisponibleTexto => $"{CantidadDisponible:N2}";
    public string CantidadDepositadaTexto => $"{CantidadDepositada:N2}";
    public string CantidadTotalTexto => $"{CantidadTotal:N2}";
}

public class TotalDivisaItem
{
    public string Codigo { get; set; } = "";
    public string Total { get; set; } = "0.00";
    public string Color { get; set; } = "#6c757d";
    
    // Diccionario de colores por divisa
    private static readonly Dictionary<string, string> ColoresDivisa = new()
    {
        { "USD", "#17a2b8" },  // Cyan
        { "GBP", "#6f42c1" },  // Violeta
        { "CHF", "#fd7e14" },  // Naranja
        { "JPY", "#e83e8c" },  // Rosa
        { "CAD", "#20c997" },  // Verde agua
        { "AUD", "#007bff" },  // Azul
        { "MXN", "#28a745" },  // Verde
        { "BRL", "#ffc107" },  // Amarillo
        { "ARS", "#6610f2" },  // Indigo
        { "CLP", "#dc3545" },  // Rojo
        { "CNY", "#fd7e14" },  // Naranja
        { "INR", "#17a2b8" },  // Cyan
        { "KRW", "#e83e8c" },  // Rosa
        { "SGD", "#20c997" },  // Verde agua
        { "HKD", "#007bff" },  // Azul
        { "NOK", "#6f42c1" },  // Violeta
        { "SEK", "#28a745" },  // Verde
        { "DKK", "#dc3545" },  // Rojo
        { "NZD", "#6610f2" },  // Indigo
        { "ZAR", "#ffc107" },  // Amarillo
    };
    
    public static string ObtenerColor(string codigoDivisa)
    {
        return ColoresDivisa.GetValueOrDefault(codigoDivisa, "#6c757d");
    }
}