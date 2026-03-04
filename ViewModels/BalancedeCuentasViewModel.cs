using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Services;
using Allva.Desktop.Views;

namespace Allva.Desktop.ViewModels;

public partial class BalancedeCuentasViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    
    private readonly int _idComercio;
    private readonly int _idLocal;
    private readonly string _codigoLocal;
    private readonly int _idUsuario;
    private readonly string _nombreUsuario;
    
    private static readonly TimeZoneInfo _zonaHorariaEspana = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Romance Standard Time" : "Europe/Madrid");

    // Evento para volver a inicio
    public event Action? OnVolverAInicio;

    // Bandera para evitar ejecuciones simultáneas
    private bool _operacionEnProgreso = false;
    
    [ObservableProperty]
    private string localInfo = "";
    
    [ObservableProperty]
    private string tabActual = "divisa";
    
    [ObservableProperty]
    private string fechaDesdeTexto = "";
    
    [ObservableProperty]
    private string fechaHastaTexto = "";
    
    [ObservableProperty]
    private string operacionDesde = "";

    [ObservableProperty]
    private string operacionHasta = "";

    [ObservableProperty]
    private string tipoOperacionFiltro = "Todas";

    public ObservableCollection<string> TiposOperacion { get; } = new()
    {
        "Todas",
        "Compra divisa",
        "Traspaso",
        "Venta divisa"
    };
    
    [ObservableProperty]
    private string fechaActualTexto = "";
    
    [ObservableProperty]
    private string totalEuros = "0.00";
    
    [ObservableProperty]
    private string totalDivisa = "0.00";
    
    [ObservableProperty]
    private string totalEurosColor = "#0b5394";
    
    [ObservableProperty]
    private string salidaEurosTotal = "0.00";
    
    [ObservableProperty]
    private string entradaEurosTotal = "0.00";
    
    private decimal _totalEurosNumerico = 0;
    private decimal _salidaEurosNumerico = 0;
    private decimal _entradaEurosNumerico = 0;
    
    [ObservableProperty]
    private string divisaDepositoSeleccionada = "";
    
    [ObservableProperty]
    private string cantidadDivisaDeposito = "";
    
    [ObservableProperty]
    private string cantidadEurosDeposito = "";
    
    [ObservableProperty]
    private string cantidadTraspaso = "";
    
    [ObservableProperty]
    private bool isLoading = false;
    
    [ObservableProperty]
    private string errorMessage = "";
    
    [ObservableProperty]
    private string successMessage = "";
    
    [ObservableProperty]
    private bool tieneDivisas = false;

    // ============================================
    // PROPIEDADES PARA TABS
    // ============================================
    
    public bool EsTabDivisa => TabActual == "divisa";
    public bool EsTabAlimentos => TabActual == "alimentos";
    public bool EsTabBilletes => TabActual == "billetes";
    public bool EsTabViaje => TabActual == "viaje";
    
    public string TabDivisaBackground => EsTabDivisa ? "#ffd966" : "White";
    public string TabDivisaForeground => EsTabDivisa ? "#0b5394" : "#595959";
    public string TabAlimentosBackground => EsTabAlimentos ? "#ffd966" : "White";
    public string TabAlimentosForeground => EsTabAlimentos ? "#0b5394" : "#595959";
    public string TabBilletesBackground => EsTabBilletes ? "#ffd966" : "White";
    public string TabBilletesForeground => EsTabBilletes ? "#0b5394" : "#595959";
    public string TabViajeBackground => EsTabViaje ? "#ffd966" : "White";
    public string TabViajeForeground => EsTabViaje ? "#0b5394" : "#595959";

    // ============================================
    // PROPIEDADES PACK ALIMENTOS
    // ============================================
    
    [ObservableProperty]
    private string filtroPaisDestino = "Todos";
    
    [ObservableProperty]
    private string filtroNumeroOperacionAlimentos = "";

    // Propiedad para autocompletado de país destino
    [ObservableProperty]
    private string textoBusquedaPaisDestino = "";

    [ObservableProperty]
    private bool mostrarListaPaises = false;

    public ObservableCollection<string> PaisesFiltrados { get; } = new();

    // Propiedad para ordenamiento
    [ObservableProperty]
    private bool ordenAscendente = true; // true = más viejo primero (ASC), false = más reciente primero (DESC)

    public string IconoOrden => OrdenAscendente ? "▲" : "▼";
    public string TooltipOrden => OrdenAscendente ? "Más viejo primero (click para cambiar)" : "Más reciente primero (click para cambiar)";

    [ObservableProperty]
    private int totalPendientes = 0;
    
    [ObservableProperty]
    private int totalEnviados = 0;
    
    [ObservableProperty]
    private int totalAnulados = 0;
    
    [ObservableProperty]
    private string totalImporteAlimentos = "0.00";

    [ObservableProperty]
    private string totalImporteAlimentosColor = "#28a745";

    // Saldo a favor (beneficio_acumulado)
    [ObservableProperty]
    private decimal saldoAFavor = 0;

    [ObservableProperty]
    private string saldoAFavorTexto = "0.00 EUR";

    [ObservableProperty]
    private bool tieneSaldoAFavor = false;

    public ObservableCollection<OperacionItem> Operaciones { get; } = new();
    public ObservableCollection<OperacionPackAlimentoBalanceItem> OperacionesAlimentos { get; } = new();
    public ObservableCollection<DivisaLocal> DivisasDelLocal { get; } = new();
    public ObservableCollection<string> DivisasParaDeposito { get; } = new();
    public ObservableCollection<string> PaisesDestinoDisponibles { get; } = new();
    
    private readonly string[] _mesesEspanol = { 
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" 
    };
    
    private readonly string[] _diasSemana = {
        "domingo", "lunes", "martes", "miercoles", "jueves", "viernes", "sabado"
    };
    
    public BalancedeCuentasViewModel()
    {
        _idComercio = 0;
        _idLocal = 0;
        _codigoLocal = "---";
        _idUsuario = 0;
        _nombreUsuario = "Usuario";
        LocalInfo = $"(Oficina - {_codigoLocal})";
        InicializarFechas();
    }
    
    public BalancedeCuentasViewModel(int idComercio, int idLocal, string codigoLocal)
    {
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        _idUsuario = 0;
        _nombreUsuario = "Usuario";
        LocalInfo = $"(Oficina - {_codigoLocal})";

        InicializarFechas();
        _ = CargarDatosInicialesAsync();
    }

    public BalancedeCuentasViewModel(int idComercio, int idLocal, string codigoLocal, int idUsuario, string nombreUsuario)
    {
        _idComercio = idComercio;
        _idLocal = idLocal;
        _codigoLocal = codigoLocal;
        _idUsuario = idUsuario;
        _nombreUsuario = nombreUsuario;
        LocalInfo = $"(Oficina - {_codigoLocal})";

        InicializarFechas();
        _ = CargarDatosInicialesAsync();
    }
    
    partial void OnDivisaDepositoSeleccionadaChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            var divisa = DivisasDelLocal.FirstOrDefault(d => d.Codigo == value);
            if (divisa != null)
            {
                CantidadDivisaDeposito = divisa.Cantidad.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }
    }
    
    private DateTime ObtenerHoraEspana()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaHorariaEspana);
    }

    private void InicializarFechas()
    {
        var hoy = ObtenerHoraEspana();
        FechaActualTexto = FormatearFechaCompleta(hoy);
        // Dejar filtros de fecha en blanco al entrar
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

    /// <summary>
    /// Normaliza el número de operación añadiendo el prefijo "DI" si no está presente.
    /// Acepta tanto "DI0001" como "0001" y devuelve siempre "DI0001".
    /// </summary>
    private string NormalizarNumeroOperacion(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero)) return numero;

        var trimmed = numero.Trim().ToUpperInvariant();

        // Si ya tiene el prefijo DI, devolverlo tal cual
        if (trimmed.StartsWith("DI"))
            return trimmed;

        // Si no tiene el prefijo, añadirlo
        return "DI" + trimmed;
    }

    private async Task CargarDatosInicialesAsync()
    {
        // Al entrar al panel, cargar las últimas 20 operaciones sin filtros de fecha
        await CargarUltimas20OperacionesAsync();
    }

    private async Task CargarDatosAsync()
    {
        if (_operacionEnProgreso) return;

        try
        {
            _operacionEnProgreso = true;
            IsLoading = true;

            if (EsTabDivisa)
            {
                Operaciones.Clear();
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                await CargarOperacionesDivisasInternoAsync(conn);
                await CargarDivisasDelLocalAsync();
                await CargarBalancesAsync();
            }
            else if (EsTabAlimentos)
            {
                OperacionesAlimentos.Clear();
                await CargarPaisesDestinoAsync();
                await CargarOperacionesAlimentosInternoAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al cargar datos";
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _operacionEnProgreso = false;
        }
    }

    // Método interno para cargar operaciones de divisas (usado por CargarDatosAsync)
    private async Task CargarOperacionesDivisasInternoAsync(NpgsqlConnection conn)
    {
        var fechaDesde = ParsearFecha(FechaDesdeTexto);
        var fechaHasta = ParsearFecha(FechaHastaTexto);

        var queries = new System.Collections.Generic.List<string>();
        var tipoFiltro = TipoOperacionFiltro;
        var ordenamiento = OrdenAscendente ? "ASC" : "DESC";

        // Lógica de filtro N° Operación
        var tieneOpDesde = !string.IsNullOrWhiteSpace(OperacionDesde);
        var tieneOpHasta = !string.IsNullOrWhiteSpace(OperacionHasta);
        var tieneNumeroOp = tieneOpDesde || tieneOpHasta;

        if (string.IsNullOrEmpty(tipoFiltro) || tipoFiltro == "Todas" || tipoFiltro == "Compra divisa")
        {
            var whereDivisa = "o.id_local = @idLocal";
            if (fechaDesde.HasValue) whereDivisa += " AND o.fecha_operacion >= @fechaDesde";
            if (fechaHasta.HasValue) whereDivisa += " AND o.fecha_operacion <= @fechaHasta";

            // Filtro N° Operación: exacto si solo uno, rango si ambos
            if (tieneOpDesde && tieneOpHasta)
            {
                whereDivisa += " AND o.numero_operacion >= @opDesde AND o.numero_operacion <= @opHasta";
            }
            else if (tieneOpDesde)
            {
                whereDivisa += " AND o.numero_operacion = @opDesde";
            }
            else if (tieneOpHasta)
            {
                whereDivisa += " AND o.numero_operacion = @opHasta";
            }

            queries.Add($@"
                SELECT
                    o.fecha_operacion as fecha,
                    o.hora_operacion as hora,
                    o.numero_operacion,
                    'CAMBIO' as tipo,
                    od.divisa_origen as divisa,
                    od.cantidad_destino as cantidad_euros,
                    od.cantidad_origen as cantidad_divisa,
                    od.tipo_cambio,
                    '' as descripcion_extra
                FROM operaciones o
                INNER JOIN operaciones_divisas od ON o.id_operacion = od.id_operacion
                WHERE {whereDivisa}");
        }

        // Solo incluir Venta divisa y Traspaso si NO hay filtro de N° Operación
        // (estas operaciones no tienen número de operación)
        if (!tieneNumeroOp && (string.IsNullOrEmpty(tipoFiltro) || tipoFiltro == "Todas" || tipoFiltro == "Venta divisa"))
        {
            var whereDeposito = "bc.id_local = @idLocal AND bc.tipo_movimiento = 'DEPOSITO'";
            if (fechaDesde.HasValue) whereDeposito += " AND bc.fecha_movimiento >= @fechaDesde";
            if (fechaHasta.HasValue) whereDeposito += " AND bc.fecha_movimiento <= @fechaHasta";

            queries.Add($@"
                SELECT
                    bc.fecha_movimiento::date as fecha,
                    bc.fecha_movimiento::time as hora,
                    NULL as numero_operacion,
                    'DEPOSITO' as tipo,
                    bc.divisa,
                    bc.monto as cantidad_euros,
                    0 as cantidad_divisa,
                    0 as tasa_cambio,
                    bc.descripcion as descripcion_extra
                FROM balance_cuentas bc
                WHERE {whereDeposito}");
        }

        if (!tieneNumeroOp && (string.IsNullOrEmpty(tipoFiltro) || tipoFiltro == "Todas" || tipoFiltro == "Traspaso"))
        {
            var whereTraspaso = "bc.id_local = @idLocal AND bc.tipo_movimiento = 'TRASPASO'";
            if (fechaDesde.HasValue) whereTraspaso += " AND bc.fecha_movimiento >= @fechaDesde";
            if (fechaHasta.HasValue) whereTraspaso += " AND bc.fecha_movimiento <= @fechaHasta";

            queries.Add($@"
                SELECT
                    bc.fecha_movimiento::date as fecha,
                    bc.fecha_movimiento::time as hora,
                    NULL as numero_operacion,
                    'TRASPASO' as tipo,
                    'EUR' as divisa,
                    bc.monto as cantidad_euros,
                    0 as cantidad_divisa,
                    0 as tasa_cambio,
                    '' as descripcion_extra
                FROM balance_cuentas bc
                WHERE {whereTraspaso}");
        }

        if (queries.Count == 0) return;

        var sqlFinal = string.Join(" UNION ALL ", queries) + $" ORDER BY fecha {ordenamiento}, hora {ordenamiento}";

        await using var cmd = new NpgsqlCommand(sqlFinal, conn);
        cmd.Parameters.AddWithValue("@idLocal", _idLocal);

        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta.Value.Date);
        if (tieneOpDesde)
            cmd.Parameters.AddWithValue("@opDesde", NormalizarNumeroOperacion(OperacionDesde));
        if (tieneOpHasta)
            cmd.Parameters.AddWithValue("@opHasta", NormalizarNumeroOperacion(OperacionHasta));

        await using var reader = await cmd.ExecuteReaderAsync();
        int index = 0;

        while (await reader.ReadAsync())
        {
            var fecha = reader.IsDBNull(0) ? DateTime.Today : reader.GetDateTime(0);
            var hora = reader.IsDBNull(1) ? TimeSpan.Zero : reader.GetTimeSpan(1);
            var numOp = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var tipo = reader.GetString(3);
            var divisa = reader.IsDBNull(4) ? "" : reader.GetString(4);
            var cantidadEuros = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5);
            var cantidadDivisa = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6);
            var descripcionExtra = reader.IsDBNull(8) ? "" : reader.GetString(8);

            // Extraer cantidad de divisa de la descripción para venta divisa (ej: "Venta divisa: 500.00 USD")
            if (tipo == "DEPOSITO" && descripcionExtra.Contains(":"))
            {
                var partes = descripcionExtra.Split(':');
                if (partes.Length > 1)
                {
                    var info = partes[1].Trim();
                    var numeros = info.Split(' ');
                    if (numeros.Length > 0 && decimal.TryParse(numeros[0].Replace(",", "."),
                        System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cantidadExtraida))
                    {
                        cantidadDivisa = cantidadExtraida;
                    }
                }
            }

            var item = new OperacionItem
            {
                Fecha = fecha.ToString("dd/MM/yy"),
                Hora = hora.ToString(@"hh\:mm"),
                NumeroOperacion = numOp,
                BackgroundColor = index % 2 == 0 ? "White" : "#F5F5F5",
                FechaHoraOrden = fecha.Add(hora)
            };

            switch (tipo)
            {
                case "CAMBIO":
                    item.Descripcion = "Compra divisa";
                    item.CantidadDivisa = $"{cantidadDivisa:N2} {divisa}";
                    item.SalidaEuros = $"{cantidadEuros:N2}";
                    item.SalidaEurosColor = "#dc3545";
                    item.EntradaEuros = "";
                    item.EsClickeable = true;
                    item.TextoSubrayado = TextDecorations.Underline;
                    break;
                case "DEPOSITO":
                    // Venta divisa - divisa sale (rojo con -), euros entran (verde con +)
                    var codigoDivisaVenta = divisa ?? "USD";
                    item.Descripcion = $"Venta divisa {codigoDivisaVenta}";
                    item.CantidadDivisa = $"-{cantidadDivisa:N2}";
                    item.CantidadDivisaColor = "#CC3333";
                    item.SalidaEuros = "";
                    item.SalidaEurosColor = "#666666";
                    item.EntradaEuros = $"+{cantidadEuros:N2}";
                    item.EntradaEurosColor = "#008800";
                    item.EsClickeable = false;
                    item.TextoSubrayado = null;
                    break;
                case "TRASPASO":
                    // Traspaso - negativo y rojo
                    item.Descripcion = "Traspaso";
                    item.CantidadDivisa = "";
                    item.SalidaEuros = $"-{cantidadEuros:N2}";
                    item.SalidaEurosColor = "#CC3333";
                    item.EntradaEuros = "";
                    item.EntradaEurosColor = "#008800";
                    item.EsClickeable = false;
                    item.TextoSubrayado = null;
                    break;
            }

            Operaciones.Add(item);
            index++;
        }
    }

    // Método interno para cargar operaciones de alimentos (usado por CargarDatosAsync)
    private async Task CargarOperacionesAlimentosInternoAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        var fechaDesde = ParsearFecha(FechaDesdeTexto);
        var fechaHasta = ParsearFecha(FechaHastaTexto);

        var whereClause = "o.id_local = @idLocal AND o.modulo = 'PACK_ALIMENTOS'";

        if (fechaDesde.HasValue)
            whereClause += " AND o.fecha_operacion >= @fechaDesde";
        if (fechaHasta.HasValue)
            whereClause += " AND o.fecha_operacion <= @fechaHasta";
        if (!string.IsNullOrWhiteSpace(FiltroPaisDestino) && FiltroPaisDestino != "Todos")
            whereClause += " AND opa.pais_destino = @paisDestino";
        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionAlimentos))
            whereClause += " AND o.numero_operacion ILIKE @numOp";

        var query = $@"
            SELECT
                o.id_operacion,
                o.numero_operacion,
                o.fecha_operacion,
                o.hora_operacion,
                u.nombre as usuario_nombre,
                u.apellidos as usuario_apellido,
                c.nombre as cliente_nombre,
                c.apellidos as cliente_apellido,
                o.importe_total,
                o.moneda,
                opa.nombre_pack,
                opa.pais_destino,
                opa.ciudad_destino,
                opa.estado_envio,
                cb.nombre as beneficiario_nombre,
                cb.apellido as beneficiario_apellido
            FROM operaciones o
            LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
            LEFT JOIN clientes_beneficiarios cb ON opa.id_beneficiario = cb.id_beneficiario
            LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
            LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
            WHERE {whereClause}
            ORDER BY o.fecha_operacion {(OrdenAscendente ? "ASC" : "DESC")}, o.hora_operacion {(OrdenAscendente ? "ASC" : "DESC")}";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@idLocal", _idLocal);

        if (fechaDesde.HasValue)
            cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde.Value.Date);
        if (fechaHasta.HasValue)
            cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta.Value.Date);
        if (!string.IsNullOrWhiteSpace(FiltroPaisDestino) && FiltroPaisDestino != "Todos")
            cmd.Parameters.AddWithValue("@paisDestino", FiltroPaisDestino);
        if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionAlimentos))
            cmd.Parameters.AddWithValue("@numOp", $"%{FiltroNumeroOperacionAlimentos}%");

        await using var reader = await cmd.ExecuteReaderAsync();

        int index = 0;
        decimal totalPendiente = 0;
        int pendientes = 0, enviados = 0, anulados = 0;

        while (await reader.ReadAsync())
        {
            var fechaOp = reader.IsDBNull(2) ? DateTime.Today : reader.GetDateTime(2);
            var horaOp = reader.IsDBNull(3) ? TimeSpan.Zero : reader.GetTimeSpan(3);
            var estadoEnvio = reader.IsDBNull(13) ? "PENDIENTE" : reader.GetString(13);

            var usuarioNombre = reader.IsDBNull(4) ? "" : reader.GetString(4);
            var usuarioApellido = reader.IsDBNull(5) ? "" : reader.GetString(5);
            var nombreUsuarioCompleto = $"{usuarioNombre} {usuarioApellido}".Trim();

            var clienteNombre = reader.IsDBNull(6) ? "" : reader.GetString(6);
            var clienteApellido = reader.IsDBNull(7) ? "" : reader.GetString(7);
            var primerNombreCliente = clienteNombre.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoCliente = clienteApellido.Split(' ').FirstOrDefault() ?? "";
            var nombreClienteCompleto = $"{primerNombreCliente} {primerApellidoCliente}".Trim();

            var benefNombre = reader.IsDBNull(14) ? "" : reader.GetString(14);
            var benefApellido = reader.IsDBNull(15) ? "" : reader.GetString(15);
            var primerNombreBenef = benefNombre.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoBenef = benefApellido.Split(' ').FirstOrDefault() ?? "";
            var nombreBenefCompleto = $"{primerNombreBenef} {primerApellidoBenef}".Trim();

            var importe = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8);

            switch (estadoEnvio.ToUpper())
            {
                case "PENDIENTE": pendientes++; totalPendiente += importe; break;
                case "ENVIADO": enviados++; break;
                case "ANULADO": anulados++; break;
            }

            var item = new OperacionPackAlimentoBalanceItem
            {
                IdOperacion = reader.GetInt64(0),
                NumeroOperacion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Fecha = fechaOp.ToString("dd/MM/yy"),
                Hora = horaOp.ToString(@"hh\:mm"),
                NombreUsuario = nombreUsuarioCompleto,
                NombreCliente = nombreClienteCompleto,
                Importe = importe,
                ImporteFormateado = $"{importe:N2}",
                ImporteColor = "#0b5394",
                Moneda = reader.IsDBNull(9) ? "EUR" : reader.GetString(9),
                Descripcion = reader.IsDBNull(10) ? "Pack Alimentos" : reader.GetString(10),
                PaisDestino = reader.IsDBNull(11) ? "" : reader.GetString(11),
                CiudadDestino = reader.IsDBNull(12) ? "" : reader.GetString(12),
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

        TotalPendientes = pendientes;
        TotalEnviados = enviados;
        TotalAnulados = anulados;

        // Consultar saldo a favor del local
        await using var connSaldo = new NpgsqlConnection(ConnectionString);
        await connSaldo.OpenAsync();
        var sqlSaldo = "SELECT COALESCE(beneficio_acumulado, 0) FROM locales WHERE id_local = @idLocal";
        await using var cmdSaldo = new NpgsqlCommand(sqlSaldo, connSaldo);
        cmdSaldo.Parameters.AddWithValue("@idLocal", _idLocal);
        var saldoResult = await cmdSaldo.ExecuteScalarAsync();
        SaldoAFavor = saldoResult != null && saldoResult != DBNull.Value ? Convert.ToDecimal(saldoResult) : 0;

        var balance = SaldoAFavor - totalPendiente;

        if (balance < 0)
        {
            TotalImporteAlimentos = balance.ToString("N2");
            TotalImporteAlimentosColor = "#dc3545";
        }
        else if (balance > 0)
        {
            TotalImporteAlimentos = $"+{balance:N2}";
            TotalImporteAlimentosColor = "#28a745";
        }
        else
        {
            TotalImporteAlimentos = "0.00";
            TotalImporteAlimentosColor = "#28a745";
        }
    }
    
    [RelayCommand]
    private async Task CambiarTab(string tab)
    {
        if (_operacionEnProgreso) return;

        TabActual = tab;

        // Notificar cambios de visibilidad
        OnPropertyChanged(nameof(EsTabDivisa));
        OnPropertyChanged(nameof(EsTabAlimentos));
        OnPropertyChanged(nameof(EsTabBilletes));
        OnPropertyChanged(nameof(EsTabViaje));

        // Notificar cambios de colores de tabs
        OnPropertyChanged(nameof(TabDivisaBackground));
        OnPropertyChanged(nameof(TabDivisaForeground));
        OnPropertyChanged(nameof(TabAlimentosBackground));
        OnPropertyChanged(nameof(TabAlimentosForeground));
        OnPropertyChanged(nameof(TabBilletesBackground));
        OnPropertyChanged(nameof(TabBilletesForeground));
        OnPropertyChanged(nameof(TabViajeBackground));
        OnPropertyChanged(nameof(TabViajeForeground));

        // Al cambiar de tab, cargar las últimas 20 operaciones
        await CargarUltimas20OperacionesAsync();
    }
    
    [RelayCommand]
    private async Task BuscarAsync()
    {
        if (_operacionEnProgreso) return;

        if (EsTabDivisa)
        {
            await CargarOperacionesAsync();
            await CargarBalancesAsync();
        }
        else if (EsTabAlimentos)
        {
            await CargarOperacionesPackAlimentosAsync();
        }
    }
    
    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        if (_operacionEnProgreso) return;

        // Dejar filtros de fecha en blanco
        FechaDesdeTexto = "";
        FechaHastaTexto = "";
        OperacionDesde = "";
        OperacionHasta = "";
        TipoOperacionFiltro = "Todas";
        FiltroPaisDestino = "Todos";
        TextoBusquedaPaisDestino = "";
        MostrarListaPaises = false;
        FiltroNumeroOperacionAlimentos = "";

        // Cargar las 20 operaciones más recientes sin filtros
        await CargarUltimas20OperacionesAsync();
    }

    [RelayCommand]
    private async Task CambiarOrdenAsync()
    {
        if (_operacionEnProgreso) return;

        OrdenAscendente = !OrdenAscendente;
        OnPropertyChanged(nameof(IconoOrden));
        OnPropertyChanged(nameof(TooltipOrden));
        await CargarDatosAsync();
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
    // PACK ALIMENTOS - CARGA DE DATOS
    // ============================================
    
    private async Task CargarPaisesDestinoAsync()
    {
        try
        {
            PaisesDestinoDisponibles.Clear();
            PaisesDestinoDisponibles.Add("Todos");

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = @"SELECT DISTINCT opa.pais_destino
                        FROM operaciones o
                        INNER JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                        WHERE o.id_local = @idLocal AND o.modulo = 'PACK_ALIMENTOS'
                        AND opa.pais_destino IS NOT NULL AND opa.pais_destino != ''
                        ORDER BY opa.pais_destino";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                PaisesDestinoDisponibles.Add(reader.GetString(0));
            }

            // Inicializar países filtrados
            FiltrarPaises(TextoBusquedaPaisDestino);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando paises: {ex.Message}");
        }
    }
    
    private async Task CargarOperacionesPackAlimentosAsync()
    {
        if (_operacionEnProgreso) return;

        try
        {
            _operacionEnProgreso = true;
            IsLoading = true;
            OperacionesAlimentos.Clear();
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var fechaDesde = ParsearFecha(FechaDesdeTexto);
            var fechaHasta = ParsearFecha(FechaHastaTexto);
            
            var whereClause = "o.id_local = @idLocal AND o.modulo = 'PACK_ALIMENTOS'";
            
            if (fechaDesde.HasValue)
                whereClause += " AND o.fecha_operacion >= @fechaDesde";
            if (fechaHasta.HasValue)
                whereClause += " AND o.fecha_operacion <= @fechaHasta";
            if (!string.IsNullOrWhiteSpace(FiltroPaisDestino) && FiltroPaisDestino != "Todos")
                whereClause += " AND opa.pais_destino = @paisDestino";
            if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionAlimentos))
                whereClause += " AND o.numero_operacion ILIKE @numOp";
            
            var query = $@"
                SELECT 
                    o.id_operacion,
                    o.numero_operacion,
                    o.fecha_operacion,
                    o.hora_operacion,
                    u.nombre as usuario_nombre,
                    u.apellidos as usuario_apellido,
                    c.nombre as cliente_nombre,
                    c.apellidos as cliente_apellido,
                    o.importe_total,
                    o.moneda,
                    opa.nombre_pack,
                    opa.pais_destino,
                    opa.ciudad_destino,
                    opa.estado_envio,
                    cb.nombre as beneficiario_nombre,
                    cb.apellido as beneficiario_apellido
                FROM operaciones o
                LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                LEFT JOIN clientes_beneficiarios cb ON opa.id_beneficiario = cb.id_beneficiario
                LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
                LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
                WHERE {whereClause}
                ORDER BY o.fecha_operacion {(OrdenAscendente ? "ASC" : "DESC")}, o.hora_operacion {(OrdenAscendente ? "ASC" : "DESC")}";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);

            if (fechaDesde.HasValue)
                cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde.Value.Date);
            if (fechaHasta.HasValue)
                cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta.Value.Date);
            if (!string.IsNullOrWhiteSpace(FiltroPaisDestino) && FiltroPaisDestino != "Todos")
                cmd.Parameters.AddWithValue("@paisDestino", FiltroPaisDestino);
            if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionAlimentos))
                cmd.Parameters.AddWithValue("@numOp", $"%{FiltroNumeroOperacionAlimentos}%");
            
            await using var reader = await cmd.ExecuteReaderAsync();

            int index = 0;
            decimal totalPendiente = 0;
            int pendientes = 0, enviados = 0, anulados = 0;

            while (await reader.ReadAsync())
            {
                var fechaOp = reader.IsDBNull(2) ? DateTime.Today : reader.GetDateTime(2);
                var horaOp = reader.IsDBNull(3) ? TimeSpan.Zero : reader.GetTimeSpan(3);
                var estadoEnvio = reader.IsDBNull(13) ? "PENDIENTE" : reader.GetString(13);

                // Usuario: nombre completo
                var usuarioNombre = reader.IsDBNull(4) ? "" : reader.GetString(4);
                var usuarioApellido = reader.IsDBNull(5) ? "" : reader.GetString(5);
                var nombreUsuarioCompleto = $"{usuarioNombre} {usuarioApellido}".Trim();

                // Cliente: primer nombre y primer apellido
                var clienteNombre = reader.IsDBNull(6) ? "" : reader.GetString(6);
                var clienteApellido = reader.IsDBNull(7) ? "" : reader.GetString(7);
                var primerNombreCliente = clienteNombre.Split(' ').FirstOrDefault() ?? "";
                var primerApellidoCliente = clienteApellido.Split(' ').FirstOrDefault() ?? "";
                var nombreClienteCompleto = $"{primerNombreCliente} {primerApellidoCliente}".Trim();

                // Beneficiario: primer nombre y primer apellido
                var benefNombre = reader.IsDBNull(14) ? "" : reader.GetString(14);
                var benefApellido = reader.IsDBNull(15) ? "" : reader.GetString(15);
                var primerNombreBenef = benefNombre.Split(' ').FirstOrDefault() ?? "";
                var primerApellidoBenef = benefApellido.Split(' ').FirstOrDefault() ?? "";
                var nombreBenefCompleto = $"{primerNombreBenef} {primerApellidoBenef}".Trim();

                var importe = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8);

                // Contar por estado y calcular importes
                // Solo PENDIENTE suma a la deuda (igual que en Operaciones)
                switch (estadoEnvio.ToUpper())
                {
                    case "PENDIENTE":
                        pendientes++;
                        totalPendiente += importe; // Pendientes suman a la deuda
                        break;
                    case "PAGADO":
                        // Pagado no suma a deuda, ya esta saldado
                        break;
                    case "ENVIADO":
                        enviados++;
                        break;
                    case "ANULADO":
                        anulados++;
                        break;
                    default:
                        // Estados desconocidos no suman a deuda
                        break;
                }

                // Formato del importe - solo numero en azul (sin signo)
                string importeFormateado = $"{importe:N2}";
                // Color siempre azul para todos los importes
                string importeColor = "#0b5394";

                var item = new OperacionPackAlimentoBalanceItem
                {
                    IdOperacion = reader.GetInt64(0),
                    NumeroOperacion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Fecha = fechaOp.ToString("dd/MM/yy"),
                    Hora = horaOp.ToString(@"hh\:mm"),
                    NombreUsuario = nombreUsuarioCompleto,
                    NombreCliente = nombreClienteCompleto,
                    Importe = importe,
                    ImporteFormateado = importeFormateado,
                    ImporteColor = importeColor,
                    Moneda = reader.IsDBNull(9) ? "EUR" : reader.GetString(9),
                    Descripcion = reader.IsDBNull(10) ? "Pack Alimentos" : reader.GetString(10),
                    PaisDestino = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    CiudadDestino = reader.IsDBNull(12) ? "" : reader.GetString(12),
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

            // Cerrar el reader antes de ejecutar otra consulta
            await reader.CloseAsync();

            // Actualizar resumen
            TotalPendientes = pendientes;
            TotalEnviados = enviados;
            TotalAnulados = anulados;

            // Consultar saldo a favor del local
            var sqlSaldo = "SELECT COALESCE(beneficio_acumulado, 0) FROM locales WHERE id_local = @idLocal";
            await using var cmdSaldo = new NpgsqlCommand(sqlSaldo, conn);
            cmdSaldo.Parameters.AddWithValue("@idLocal", _idLocal);
            var saldoResult = await cmdSaldo.ExecuteScalarAsync();
            SaldoAFavor = saldoResult != null && saldoResult != DBNull.Value ? Convert.ToDecimal(saldoResult) : 0;
            SaldoAFavorTexto = $"{SaldoAFavor:N2} EUR";
            TieneSaldoAFavor = SaldoAFavor > 0;

            // El balance es: saldoAFavor - totalPendiente
            // Si hay más pendientes que saldo a favor = negativo (debe)
            // Si hay más saldo a favor que pendientes = positivo (a favor)
            var balance = SaldoAFavor - totalPendiente;

            if (balance < 0)
            {
                TotalImporteAlimentos = balance.ToString("N2");
                TotalImporteAlimentosColor = "#dc3545"; // Rojo para negativo (deuda pendiente)
            }
            else if (balance > 0)
            {
                TotalImporteAlimentos = $"+{balance:N2}";
                TotalImporteAlimentosColor = "#28a745"; // Verde para positivo (a favor)
            }
            else
            {
                TotalImporteAlimentos = "0.00";
                TotalImporteAlimentosColor = "#28a745"; // Verde para neutral/saldado
            }
        }
        catch (Exception ex)
        {
            // Mostrar mensaje simplificado sin detalles técnicos
            var mensajeSimple = ex.Message.Contains(":") ? ex.Message.Split(':')[0] : ex.Message;
            ErrorMessage = $"Error al cargar operaciones de productos";
            System.Diagnostics.Debug.WriteLine($"Error completo: {ex.Message}");
            await Task.Delay(3000);
            ErrorMessage = "";
        }
        finally
        {
            IsLoading = false;
            _operacionEnProgreso = false;
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
            "ENVIADO" => "#28a745",
            "ANULADO" => "#dc3545",
            _ => "#6c757d"
        };
    }
    
    [RelayCommand]
    private async Task ImprimirHistorialAsync()
    {
        try
        {
            var ahora = ObtenerHoraEspana();
            
            var confirmacionVM = new ConfirmacionImpresionViewModel
            {
                FechaGeneracion = ahora.ToString("dd/MM/yyyy"),
                HoraGeneracion = ahora.ToString("HH:mm:ss"),
                NombreUsuario = _nombreUsuario,
                CodigoLocal = _codigoLocal,
                BalanceEuros = TotalEuros,
                BalanceEurosEsPositivo = _totalEurosNumerico >= 0,
                TotalDivisas = TotalDivisa,
                CantidadOperaciones = $"{Operaciones.Count} registros"
            };
            
            var hayFiltros = false;
            
            if (!string.IsNullOrWhiteSpace(FechaDesdeTexto))
            {
                confirmacionVM.FiltrosAplicados.Add(new FiltroAplicadoItem { Nombre = "Fecha desde:", Valor = FechaDesdeTexto });
                hayFiltros = true;
            }
            if (!string.IsNullOrWhiteSpace(FechaHastaTexto))
            {
                confirmacionVM.FiltrosAplicados.Add(new FiltroAplicadoItem { Nombre = "Fecha hasta:", Valor = FechaHastaTexto });
                hayFiltros = true;
            }
            if (!string.IsNullOrWhiteSpace(OperacionDesde))
            {
                confirmacionVM.FiltrosAplicados.Add(new FiltroAplicadoItem { Nombre = "Operacion desde:", Valor = OperacionDesde });
                hayFiltros = true;
            }
            if (!string.IsNullOrWhiteSpace(OperacionHasta))
            {
                confirmacionVM.FiltrosAplicados.Add(new FiltroAplicadoItem { Nombre = "Operacion hasta:", Valor = OperacionHasta });
                hayFiltros = true;
            }
            
            confirmacionVM.SinFiltros = !hayFiltros;
            
            if (DivisasDelLocal.Count > 0)
            {
                confirmacionVM.TieneDivisas = true;
                var partes = DivisasDelLocal.Select(d => $"{d.Codigo}: {d.Cantidad:N2}");
                confirmacionVM.DesgloseDivisasTexto = string.Join("  |  ", partes);
            }
            
            var ventanaConfirmacion = new ConfirmacionImpresionView(confirmacionVM);
            
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
                await GenerarPdfHistorial(ahora);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
    }
    
    private async Task GenerarPdfHistorial(DateTime fechaHora)
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
                ErrorMessage = "No se pudo abrir el dialogo de guardado";
                await Task.Delay(2000);
                ErrorMessage = "";
                return;
            }
            
            var timestamp = fechaHora.ToString("yyyyMMdd_HHmmss");
            var nombreSugerido = $"Historial_{_codigoLocal}_{timestamp}.pdf";
            
            var storageProvider = mainWindow.StorageProvider;
            
            var archivo = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Guardar historial de balance",
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
            
            if (archivo == null)
            {
                return;
            }
            
            IsLoading = true;
            ErrorMessage = "";
            
            var datosReporte = new HistorialPdfService.DatosReporte
            {
                CodigoLocal = _codigoLocal,
                NombreUsuario = _nombreUsuario,
                FechaGeneracion = fechaHora.ToString("dd/MM/yyyy"),
                HoraGeneracion = fechaHora.ToString("HH:mm:ss"),
                BalanceActualEuros = _totalEurosNumerico,
                TotalDivisasValor = DivisasDelLocal.Sum(d => d.Cantidad),
                Filtros = new HistorialPdfService.FiltrosReporte
                {
                    FechaDesde = FechaDesdeTexto,
                    FechaHasta = FechaHastaTexto,
                    OperacionDesde = OperacionDesde,
                    OperacionHasta = OperacionHasta
                }
            };
            
            foreach (var div in DivisasDelLocal)
            {
                datosReporte.DesgloseDivisas.Add(new HistorialPdfService.DivisaBalance
                {
                    CodigoDivisa = div.Codigo,
                    Cantidad = div.Cantidad
                });
            }
            
            foreach (var op in Operaciones)
            {
                datosReporte.Operaciones.Add(new HistorialPdfService.OperacionReporte
                {
                    Fecha = op.Fecha,
                    Hora = op.Hora,
                    NumeroOperacion = op.NumeroOperacion,
                    Descripcion = op.Descripcion,
                    Divisa = op.CantidadDivisa,
                    SalidaEuros = op.SalidaEuros,
                    EntradaEuros = op.EntradaEuros
                });
            }
            
            // Generar PDF en hilo separado para no bloquear la UI
            var pdfBytes = await Task.Run(() => HistorialPdfService.GenerarPdf(datosReporte));

            await using var stream = await archivo.OpenWriteAsync();
            await stream.WriteAsync(pdfBytes);
            
            await RegistrarGeneracionPdf(fechaHora);
            
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
    
    private async Task RegistrarGeneracionPdf(DateTime fechaHora)
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var filtrosTexto = "";
            if (!string.IsNullOrWhiteSpace(FechaDesdeTexto))
                filtrosTexto += $"Fecha desde: {FechaDesdeTexto}; ";
            if (!string.IsNullOrWhiteSpace(FechaHastaTexto))
                filtrosTexto += $"Fecha hasta: {FechaHastaTexto}; ";
            if (!string.IsNullOrWhiteSpace(OperacionDesde))
                filtrosTexto += $"Op desde: {OperacionDesde}; ";
            if (!string.IsNullOrWhiteSpace(OperacionHasta))
                filtrosTexto += $"Op hasta: {OperacionHasta}; ";
            
            if (string.IsNullOrEmpty(filtrosTexto))
                filtrosTexto = "Mes en curso";
            
            var sql = @"INSERT INTO historial_generacion_pdf 
                        (id_comercio, id_local, codigo_local, id_usuario, nombre_usuario,
                         modulo, tipo_reporte, filtros_aplicados, 
                         fecha_generacion, hora_generacion, registros_incluidos)
                        VALUES 
                        (@idComercio, @idLocal, @codigoLocal, @idUsuario, @nombreUsuario,
                         'DIVISAS', 'Historial Balance', @filtros,
                         @fecha, @hora, @registros)";
            
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@idComercio", _idComercio);
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);
            cmd.Parameters.AddWithValue("@codigoLocal", _codigoLocal);
            cmd.Parameters.AddWithValue("@idUsuario", _idUsuario);
            cmd.Parameters.AddWithValue("@nombreUsuario", _nombreUsuario);
            cmd.Parameters.AddWithValue("@filtros", filtrosTexto);
            cmd.Parameters.AddWithValue("@fecha", fechaHora.Date);
            cmd.Parameters.AddWithValue("@hora", fechaHora.TimeOfDay);
            cmd.Parameters.AddWithValue("@registros", Operaciones.Count);
            
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al registrar PDF: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task InsertarDepositoAsync()
    {
        if (string.IsNullOrWhiteSpace(DivisaDepositoSeleccionada))
        {
            ErrorMessage = "Seleccione una divisa";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(CantidadDivisaDeposito))
        {
            ErrorMessage = "Ingrese la cantidad de divisa";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (!decimal.TryParse(CantidadDivisaDeposito.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidadDivisa))
        {
            ErrorMessage = "La cantidad de divisa debe ser un numero valido";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (cantidadDivisa <= 0)
        {
            ErrorMessage = "La cantidad de divisa debe ser mayor a cero";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(CantidadEurosDeposito))
        {
            ErrorMessage = "Ingrese la cantidad de euros";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (!decimal.TryParse(CantidadEurosDeposito.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal eurosRecibidos))
        {
            ErrorMessage = "La cantidad de euros debe ser un numero valido";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (eurosRecibidos <= 0)
        {
            ErrorMessage = "La cantidad de euros debe ser mayor a cero";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        var divisaLocal = DivisasDelLocal.FirstOrDefault(d => d.Codigo == DivisaDepositoSeleccionada);
        if (divisaLocal == null || divisaLocal.Cantidad <= 0)
        {
            ErrorMessage = "No hay cantidad disponible de esta divisa";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (cantidadDivisa > divisaLocal.Cantidad)
        {
            ErrorMessage = $"Solo tiene {divisaLocal.Cantidad:N2} {DivisaDepositoSeleccionada} disponibles";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        try
        {
            IsLoading = true;
            ErrorMessage = "";
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var ahora = ObtenerHoraEspana();
            
            var descripcion = $"Venta divisa: {cantidadDivisa:N2} {DivisaDepositoSeleccionada}";
            
            var sqlBalance = @"INSERT INTO balance_cuentas 
                              (id_comercio, id_local, codigo_local, id_usuario, 
                               tipo_movimiento, modulo, descripcion, divisa, monto,
                               fecha_movimiento)
                              VALUES 
                              (@idComercio, @idLocal, @codigoLocal, @idUsuario,
                               'DEPOSITO', 'DIVISAS', @descripcion, @codigoDivisa, @monto,
                               @fecha)";
            
            await using var cmdBalance = new NpgsqlCommand(sqlBalance, conn);
            cmdBalance.Parameters.AddWithValue("@idComercio", _idComercio);
            cmdBalance.Parameters.AddWithValue("@idLocal", _idLocal);
            cmdBalance.Parameters.AddWithValue("@codigoLocal", _codigoLocal);
            cmdBalance.Parameters.AddWithValue("@idUsuario", _idUsuario);
            cmdBalance.Parameters.AddWithValue("@descripcion", descripcion);
            cmdBalance.Parameters.AddWithValue("@codigoDivisa", DivisaDepositoSeleccionada);
            cmdBalance.Parameters.AddWithValue("@monto", eurosRecibidos);
            cmdBalance.Parameters.AddWithValue("@fecha", ahora);

            await cmdBalance.ExecuteNonQueryAsync();

            var sqlActualizarDivisa = @"INSERT INTO balance_divisas
                                        (id_comercio, id_local, id_usuario, codigo_divisa, nombre_divisa,
                                         cantidad_recibida, cantidad_entregada_eur,
                                         tasa_cambio_momento, tasa_cambio_aplicada,
                                         tipo_movimiento, fecha_registro, observaciones)
                                        VALUES
                                        (@idComercio, @idLocal, @idUsuario, @codigoDivisa, @nombreDivisa,
                                         @cantidadDivisa, @cantidadEuros,
                                         1, 1, 'SALIDA', @fecha, 'Venta divisa')";

            await using var cmdDivisa = new NpgsqlCommand(sqlActualizarDivisa, conn);
            cmdDivisa.Parameters.AddWithValue("@idComercio", _idComercio);
            cmdDivisa.Parameters.AddWithValue("@idLocal", _idLocal);
            cmdDivisa.Parameters.AddWithValue("@idUsuario", _idUsuario);
            cmdDivisa.Parameters.AddWithValue("@codigoDivisa", DivisaDepositoSeleccionada);
            cmdDivisa.Parameters.AddWithValue("@nombreDivisa", ObtenerNombreDivisa(DivisaDepositoSeleccionada));
            cmdDivisa.Parameters.AddWithValue("@cantidadDivisa", cantidadDivisa);
            cmdDivisa.Parameters.AddWithValue("@cantidadEuros", eurosRecibidos);
            cmdDivisa.Parameters.AddWithValue("@fecha", ahora);
            
            await cmdDivisa.ExecuteNonQueryAsync();
            
            await CargarDatosAsync();
            
            CantidadDivisaDeposito = "";
            CantidadEurosDeposito = "";
            SuccessMessage = "Venta divisa registrada correctamente";
            
            await Task.Delay(2000);
            SuccessMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task TransferirCajaAsync()
    {
        if (string.IsNullOrWhiteSpace(CantidadTraspaso))
        {
            ErrorMessage = "Ingrese la cantidad a traspasar";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (!decimal.TryParse(CantidadTraspaso.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidad))
        {
            ErrorMessage = "La cantidad debe ser un numero valido";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (cantidad <= 0)
        {
            ErrorMessage = "La cantidad debe ser mayor a cero";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        if (cantidad > _totalEurosNumerico)
        {
            ErrorMessage = $"No puede traspasar mas de {_totalEurosNumerico:N2} EUR";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        try
        {
            IsLoading = true;
            ErrorMessage = "";
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var ahora = ObtenerHoraEspana();
            
            var sqlTraspaso = @"INSERT INTO balance_cuentas 
                               (id_comercio, id_local, codigo_local, id_usuario, 
                                tipo_movimiento, modulo, descripcion, divisa, monto,
                                fecha_movimiento)
                               VALUES 
                               (@idComercio, @idLocal, @codigoLocal, @idUsuario,
                                'TRASPASO', 'DIVISAS', 'Traspaso', 'EUR', @monto,
                                @fecha)";
            
            await using var cmd = new NpgsqlCommand(sqlTraspaso, conn);
            cmd.Parameters.AddWithValue("@idComercio", _idComercio);
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);
            cmd.Parameters.AddWithValue("@codigoLocal", _codigoLocal);
            cmd.Parameters.AddWithValue("@idUsuario", _idUsuario);
            cmd.Parameters.AddWithValue("@monto", cantidad);
            cmd.Parameters.AddWithValue("@fecha", ahora);
            
            await cmd.ExecuteNonQueryAsync();
            
            await CargarDatosAsync();
            
            CantidadTraspaso = "";
            SuccessMessage = "Traspaso registrado correctamente";
            
            await Task.Delay(2000);
            SuccessMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
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
    private void SeleccionarDivisa(string codigoDivisa)
    {
        if (!string.IsNullOrEmpty(codigoDivisa))
        {
            DivisaDepositoSeleccionada = codigoDivisa;

            // Siempre actualizar la cantidad con el valor actual de la divisa
            var divisa = DivisasDelLocal.FirstOrDefault(d => d.Codigo == codigoDivisa);
            if (divisa != null)
            {
                CantidadDivisaDeposito = divisa.Cantidad.ToString("0.##", CultureInfo.InvariantCulture);
            }
        }
    }
    
    private string ObtenerNombreDivisa(string codigo)
    {
        return codigo switch
        {
            "USD" => "Dolar USA",
            "EUR" => "Euro",
            "GBP" => "Libra Esterlina",
            "CHF" => "Franco Suizo",
            "CAD" => "Dolar Canadiense",
            "CNY" => "Yuan Chino",
            "PEN" => "Sol Peruano",
            _ => codigo
        };
    }
    
    private async Task CargarOperacionesAsync()
    {
        if (_operacionEnProgreso) return;

        try
        {
            _operacionEnProgreso = true;
            IsLoading = true;
            Operaciones.Clear();

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var fechaDesde = ParsearFecha(FechaDesdeTexto);
            var fechaHasta = ParsearFecha(FechaHastaTexto);

            // Construir query unificada con UNION ALL similar a pack alimentos
            var queries = new System.Collections.Generic.List<string>();

            // Lógica de filtro N° Operación:
            // - Si solo hay un valor (Desde O Hasta): buscar exactamente esa operación
            // - Si hay dos valores (Desde Y Hasta): buscar el rango entre ambas
            var tieneDesde = !string.IsNullOrWhiteSpace(OperacionDesde);
            var tieneHasta = !string.IsNullOrWhiteSpace(OperacionHasta);
            var tieneNumeroOp = tieneDesde || tieneHasta;

            // Query para compras de divisa
            if (string.IsNullOrEmpty(TipoOperacionFiltro) || TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Compra divisa")
            {
                var sqlCompras = @"SELECT
                    o.fecha_operacion as fecha,
                    o.hora_operacion as hora,
                    o.numero_operacion,
                    'COMPRA' as tipo_operacion,
                    od.divisa_origen as divisa,
                    od.cantidad_origen as cantidad_divisa,
                    od.cantidad_destino as cantidad_euros,
                    '' as descripcion_extra
                FROM operaciones o
                INNER JOIN operaciones_divisas od ON o.id_operacion = od.id_operacion
                WHERE o.id_local = @idLocal";

                if (fechaDesde.HasValue)
                    sqlCompras += " AND o.fecha_operacion >= @fechaDesde";
                if (fechaHasta.HasValue)
                    sqlCompras += " AND o.fecha_operacion <= @fechaHasta";

                if (tieneDesde && tieneHasta)
                {
                    // Rango: buscar entre ambas operaciones (incluyendo las escritas)
                    sqlCompras += " AND o.numero_operacion >= @opDesde AND o.numero_operacion <= @opHasta";
                }
                else if (tieneDesde)
                {
                    // Solo Desde: buscar exactamente esa operación
                    sqlCompras += " AND o.numero_operacion = @opDesde";
                }
                else if (tieneHasta)
                {
                    // Solo Hasta: buscar exactamente esa operación
                    sqlCompras += " AND o.numero_operacion = @opHasta";
                }

                queries.Add($"({sqlCompras})");
            }

            // Solo incluir Venta divisa y Traspaso si NO hay filtro de N° Operación
            // (estas operaciones no tienen número de operación)

            // Query para venta divisa
            if (!tieneNumeroOp && (string.IsNullOrEmpty(TipoOperacionFiltro) || TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Venta divisa"))
            {
                var sqlDepositos = @"SELECT
                    fecha_movimiento::date as fecha,
                    fecha_movimiento::time as hora,
                    '' as numero_operacion,
                    'DEPOSITO' as tipo_operacion,
                    divisa,
                    0 as cantidad_divisa,
                    monto as cantidad_euros,
                    descripcion as descripcion_extra
                FROM balance_cuentas
                WHERE id_local = @idLocal AND tipo_movimiento = 'DEPOSITO'";

                if (fechaDesde.HasValue)
                    sqlDepositos += " AND fecha_movimiento >= @fechaDesde";
                if (fechaHasta.HasValue)
                    sqlDepositos += " AND fecha_movimiento <= @fechaHasta + interval '1 day'";

                queries.Add($"({sqlDepositos})");
            }

            // Query para traspasos
            if (!tieneNumeroOp && (string.IsNullOrEmpty(TipoOperacionFiltro) || TipoOperacionFiltro == "Todas" || TipoOperacionFiltro == "Traspaso"))
            {
                var sqlTraspasos = @"SELECT
                    fecha_movimiento::date as fecha,
                    fecha_movimiento::time as hora,
                    '' as numero_operacion,
                    'TRASPASO' as tipo_operacion,
                    'EUR' as divisa,
                    0 as cantidad_divisa,
                    monto as cantidad_euros,
                    '' as descripcion_extra
                FROM balance_cuentas
                WHERE id_local = @idLocal AND tipo_movimiento = 'TRASPASO'";

                if (fechaDesde.HasValue)
                    sqlTraspasos += " AND fecha_movimiento >= @fechaDesde";
                if (fechaHasta.HasValue)
                    sqlTraspasos += " AND fecha_movimiento <= @fechaHasta + interval '1 day'";

                queries.Add($"({sqlTraspasos})");
            }

            if (queries.Count == 0)
            {
                IsLoading = false;
                return;
            }

            var ordenamiento = OrdenAscendente ? "ASC" : "DESC";
            var sqlFinal = string.Join(" UNION ALL ", queries) + $" ORDER BY fecha {ordenamiento}, hora {ordenamiento}";

            await using var cmd = new NpgsqlCommand(sqlFinal, conn);
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);

            if (fechaDesde.HasValue)
                cmd.Parameters.AddWithValue("@fechaDesde", fechaDesde.Value.Date);
            if (fechaHasta.HasValue)
                cmd.Parameters.AddWithValue("@fechaHasta", fechaHasta.Value.Date);
            if (!string.IsNullOrWhiteSpace(OperacionDesde))
                cmd.Parameters.AddWithValue("@opDesde", NormalizarNumeroOperacion(OperacionDesde));
            if (!string.IsNullOrWhiteSpace(OperacionHasta))
                cmd.Parameters.AddWithValue("@opHasta", NormalizarNumeroOperacion(OperacionHasta));

            await using var reader = await cmd.ExecuteReaderAsync();

            int index = 0;
            while (await reader.ReadAsync())
            {
                var fecha = reader.IsDBNull(0) ? DateTime.Today : reader.GetDateTime(0);
                var hora = reader.IsDBNull(1) ? TimeSpan.Zero : reader.GetTimeSpan(1);
                var numeroOp = reader.IsDBNull(2) ? "" : reader.GetString(2);
                var tipoOp = reader.GetString(3);
                var divisa = reader.IsDBNull(4) ? "" : reader.GetString(4);
                var cantidadDivisa = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5);
                var cantidadEuros = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6);
                var descripcionExtra = reader.IsDBNull(7) ? "" : reader.GetString(7);

                // Extraer cantidad de divisa de la descripción para venta divisa (ej: "Venta divisa: 500.00 USD")
                if (tipoOp == "DEPOSITO" && descripcionExtra.Contains(":"))
                {
                    var partes = descripcionExtra.Split(':');
                    if (partes.Length > 1)
                    {
                        var info = partes[1].Trim();
                        var numeros = info.Split(' ');
                        if (numeros.Length > 0 && decimal.TryParse(numeros[0].Replace(",", "."),
                            System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cantidadExtraida))
                        {
                            cantidadDivisa = cantidadExtraida;
                        }
                    }
                }

                var item = new OperacionItem
                {
                    Fecha = fecha.ToString("dd/MM/yy"),
                    Hora = hora.ToString(@"hh\:mm"),
                    NumeroOperacion = numeroOp,
                    FechaHoraOrden = fecha.Date.Add(hora),
                    BackgroundColor = index % 2 == 0 ? "White" : "#F5F5F5"
                };

                switch (tipoOp)
                {
                    case "COMPRA":
                        item.Descripcion = $"Compra {divisa}";
                        item.CantidadDivisa = $"{cantidadDivisa:N2}";
                        item.SalidaEuros = $"-{cantidadEuros:N2}";
                        item.SalidaEurosColor = "#CC3333";
                        item.EntradaEuros = "";
                        item.EsClickeable = true;
                        item.TextoSubrayado = TextDecorationCollection.Parse("Underline");
                        break;
                    case "DEPOSITO":
                        // Venta divisa - divisa sale (rojo con -), euros entran (verde con +)
                        item.Descripcion = $"Venta divisa {divisa}";
                        item.CantidadDivisa = $"-{cantidadDivisa:N2}";
                        item.CantidadDivisaColor = "#CC3333";
                        item.SalidaEuros = "";
                        item.SalidaEurosColor = "#666666";
                        item.EntradaEuros = $"+{cantidadEuros:N2}";
                        item.EntradaEurosColor = "#008800";
                        item.EsClickeable = false;
                        item.TextoSubrayado = null;
                        break;
                    case "TRASPASO":
                        // Traspaso - negativo y rojo
                        item.Descripcion = "Traspaso";
                        item.CantidadDivisa = "";
                        item.SalidaEuros = $"-{cantidadEuros:N2}";
                        item.SalidaEurosColor = "#CC3333";
                        item.EntradaEuros = "";
                        item.EntradaEurosColor = "#008800";
                        item.EsClickeable = false;
                        item.TextoSubrayado = null;
                        break;
                }

                Operaciones.Add(item);
                index++;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al cargar operaciones";
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _operacionEnProgreso = false;
        }
    }

    private async Task CargarDivisasDelLocalAsync()
    {
        try
        {
            DivisasDelLocal.Clear();
            DivisasParaDeposito.Clear();
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var sql = @"SELECT 
                            codigo_divisa,
                            SUM(CASE WHEN tipo_movimiento = 'ENTRADA' THEN cantidad_recibida ELSE 0 END) -
                            SUM(CASE WHEN tipo_movimiento = 'SALIDA' THEN cantidad_recibida ELSE 0 END) as saldo
                        FROM balance_divisas
                        WHERE id_local = @idLocal
                        GROUP BY codigo_divisa
                        HAVING SUM(CASE WHEN tipo_movimiento = 'ENTRADA' THEN cantidad_recibida ELSE 0 END) -
                               SUM(CASE WHEN tipo_movimiento = 'SALIDA' THEN cantidad_recibida ELSE 0 END) > 0
                        ORDER BY codigo_divisa";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@idLocal", _idLocal);
            
            await using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var codigo = reader.GetString(0);
                var cantidad = reader.GetDecimal(1);
                
                DivisasDelLocal.Add(new DivisaLocal
                {
                    Codigo = codigo,
                    Cantidad = cantidad,
                    CantidadFormateada = $"{cantidad:N2}"
                });
                
                DivisasParaDeposito.Add(codigo);
            }
            
            TieneDivisas = DivisasDelLocal.Count > 0;
            
            if (DivisasParaDeposito.Count > 0 && string.IsNullOrEmpty(DivisaDepositoSeleccionada))
            {
                DivisaDepositoSeleccionada = DivisasParaDeposito[0];
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al cargar divisas";
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
    }
    
    private async Task CargarBalancesAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var sqlSalidas = @"SELECT COALESCE(SUM(od.cantidad_destino), 0)
                               FROM operaciones_divisas od
                               INNER JOIN operaciones o ON od.id_operacion = o.id_operacion
                               WHERE o.id_local = @idLocal";

            await using var cmdSalidas = new NpgsqlCommand(sqlSalidas, conn);
            cmdSalidas.Parameters.AddWithValue("@idLocal", _idLocal);
            _salidaEurosNumerico = Convert.ToDecimal(await cmdSalidas.ExecuteScalarAsync() ?? 0);

            var sqlEntradas = @"SELECT COALESCE(SUM(monto), 0)
                                FROM balance_cuentas
                                WHERE id_local = @idLocal
                                  AND tipo_movimiento = 'DEPOSITO'";

            await using var cmdEntradas = new NpgsqlCommand(sqlEntradas, conn);
            cmdEntradas.Parameters.AddWithValue("@idLocal", _idLocal);
            _entradaEurosNumerico = Convert.ToDecimal(await cmdEntradas.ExecuteScalarAsync() ?? 0);

            var sqlTraspasos = @"SELECT COALESCE(SUM(monto), 0)
                                 FROM balance_cuentas
                                 WHERE id_local = @idLocal
                                   AND tipo_movimiento = 'TRASPASO'";

            await using var cmdTraspasos = new NpgsqlCommand(sqlTraspasos, conn);
            cmdTraspasos.Parameters.AddWithValue("@idLocal", _idLocal);
            var traspasos = Convert.ToDecimal(await cmdTraspasos.ExecuteScalarAsync() ?? 0);
            
            _totalEurosNumerico = _entradaEurosNumerico - _salidaEurosNumerico - traspasos;
            
            decimal totalDivisas = DivisasDelLocal.Sum(d => d.Cantidad);
            
            TotalEuros = $"{_totalEurosNumerico:N2}";
            TotalDivisa = $"{totalDivisas:N2}";
            SalidaEurosTotal = $"{_salidaEurosNumerico:N2}";
            EntradaEurosTotal = $"{_entradaEurosNumerico:N2}";
            
            TotalEurosColor = _totalEurosNumerico >= 0 ? "#008800" : "#CC3333";
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al cargar balances";
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
    }

    private async Task CargarUltimas20OperacionesAsync()
    {
        if (_operacionEnProgreso) return;

        try
        {
            _operacionEnProgreso = true;
            IsLoading = true;
            Operaciones.Clear();
            OperacionesAlimentos.Clear();

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            if (EsTabDivisa)
            {
                // Cargar las últimas 20 operaciones de divisas SIN filtros de fecha
                await CargarUltimas20DivisasSinFiltrosAsync(conn);
                await CargarDivisasDelLocalAsync();
                await CargarBalancesAsync();
            }
            else if (EsTabAlimentos)
            {
                await CargarPaisesDestinoAsync();
                await CargarUltimas20AlimentosSinFiltrosAsync(conn);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al cargar operaciones";
            System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            _operacionEnProgreso = false;
        }
    }

    private async Task CargarUltimas20DivisasSinFiltrosAsync(NpgsqlConnection conn)
    {
        // 1. Cargar TODAS las operaciones de cambio de divisa del local
        var sqlDivisas = @"SELECT
                        o.fecha_operacion,
                        o.hora_operacion,
                        o.numero_operacion,
                        od.divisa_origen,
                        od.cantidad_origen,
                        od.cantidad_destino
                    FROM operaciones o
                    INNER JOIN operaciones_divisas od ON o.id_operacion = od.id_operacion
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
                var divisaOrigen = reader.GetString(3);
                var cantidadOrigen = reader.GetDecimal(4);
                var cantidadDestino = reader.GetDecimal(5);

                Operaciones.Add(new OperacionItem
                {
                    Fecha = fechaDb.ToString("dd/MM/yy"),
                    Hora = hora.ToString(@"hh\:mm"),
                    NumeroOperacion = numeroOp,
                    Descripcion = $"Compra {divisaOrigen}",
                    CantidadDivisa = $"{cantidadOrigen:N2}",
                    SalidaEuros = $"-{cantidadDestino:N2}",
                    SalidaEurosColor = "#CC3333",
                    EntradaEuros = "",
                    EsClickeable = true,
                    TextoSubrayado = TextDecorationCollection.Parse("Underline"),
                    FechaHoraOrden = fechaDb.Date.Add(hora)
                });
            }
        }

        // 2. Cargar TODOS los depósitos en banco del local
        var sqlDepositos = @"SELECT
                        bc.fecha_movimiento,
                        bc.monto,
                        bc.descripcion,
                        bc.divisa
                    FROM balance_cuentas bc
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
                var divisaCodigo = reader.IsDBNull(3) ? "USD" : reader.GetString(3);

                // Extraer cantidad de la descripción (ej: "Venta divisa: 500.00 USD")
                var cantidadTexto = "";
                if (descripcion.Contains(":"))
                {
                    var partes = descripcion.Split(':');
                    if (partes.Length > 1)
                    {
                        var info = partes[1].Trim();
                        // Extraer solo el número
                        var numeros = info.Split(' ');
                        if (numeros.Length > 0)
                            cantidadTexto = numeros[0];
                    }
                }

                Operaciones.Add(new OperacionItem
                {
                    Fecha = fecha.ToString("dd/MM/yy"),
                    Hora = fecha.ToString("HH:mm"),
                    NumeroOperacion = "",
                    Descripcion = $"Venta divisa {divisaCodigo}",
                    CantidadDivisa = string.IsNullOrEmpty(cantidadTexto) ? "" : $"-{cantidadTexto}",
                    CantidadDivisaColor = "#CC3333",
                    SalidaEuros = "",
                    SalidaEurosColor = "#666666",
                    EntradaEuros = $"+{monto:N2}",
                    EntradaEurosColor = "#008800",
                    EsClickeable = false,
                    TextoSubrayado = null,
                    FechaHoraOrden = fecha
                });
            }
        }

        // 3. Cargar TODOS los traspasos a caja del local
        var sqlTraspasos = @"SELECT
                        bc.fecha_movimiento,
                        bc.monto
                    FROM balance_cuentas bc
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

                Operaciones.Add(new OperacionItem
                {
                    Fecha = fecha.ToString("dd/MM/yy"),
                    Hora = fecha.ToString("HH:mm"),
                    NumeroOperacion = "",
                    Descripcion = "Traspaso",
                    CantidadDivisa = "",
                    SalidaEuros = $"-{monto:N2}",
                    SalidaEurosColor = "#CC3333",
                    EntradaEuros = "",
                    EntradaEurosColor = "#008800",
                    EsClickeable = false,
                    TextoSubrayado = null,
                    FechaHoraOrden = fecha
                });
            }
        }

        // Tomar las 20 más recientes y luego ordenar de más vieja a más nueva (ASC)
        var operacionesOrdenadas = Operaciones
            .OrderByDescending(o => o.FechaHoraOrden)
            .Take(20)
            .OrderBy(o => o.FechaHoraOrden) // Más vieja arriba, más nueva abajo
            .ToList();

        Operaciones.Clear();

        for (int i = 0; i < operacionesOrdenadas.Count; i++)
        {
            operacionesOrdenadas[i].BackgroundColor = i % 2 == 0 ? "White" : "#F5F5F5";
            Operaciones.Add(operacionesOrdenadas[i]);
        }
    }

    private async Task CargarUltimas20AlimentosSinFiltrosAsync(NpgsqlConnection conn)
    {
        // Cargar las últimas 20 operaciones de pack alimentos (sin filtros de fecha)
        // Primero obtener las 20 más recientes (DESC) y luego ordenarlas de vieja a nueva (ASC)
        var sql = @"
            SELECT * FROM (
                SELECT
                    o.id_operacion,
                    o.numero_operacion,
                    o.fecha_operacion,
                    o.hora_operacion,
                    u.nombre as usuario_nombre,
                    u.apellidos as usuario_apellido,
                    c.nombre as cliente_nombre,
                    c.apellidos as cliente_apellido,
                    o.importe_total,
                    o.moneda,
                    opa.nombre_pack,
                    opa.pais_destino,
                    opa.ciudad_destino,
                    opa.estado_envio,
                    cb.nombre as beneficiario_nombre,
                    cb.apellido as beneficiario_apellido
                FROM operaciones o
                LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                LEFT JOIN clientes_beneficiarios cb ON opa.id_beneficiario = cb.id_beneficiario
                LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
                LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
                WHERE o.id_local = @idLocal
                  AND o.modulo = 'PACK_ALIMENTOS'
                ORDER BY o.fecha_operacion DESC, o.hora_operacion DESC
                LIMIT 20
            ) sub
            ORDER BY fecha_operacion ASC, hora_operacion ASC";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@idLocal", _idLocal);

        await using var reader = await cmd.ExecuteReaderAsync();

        int index = 0;
        decimal totalPendiente = 0;
        int pendientes = 0, enviados = 0, anulados = 0;

        while (await reader.ReadAsync())
        {
            var fechaOp = reader.IsDBNull(2) ? DateTime.Today : reader.GetDateTime(2);
            var horaOp = reader.IsDBNull(3) ? TimeSpan.Zero : reader.GetTimeSpan(3);
            var estadoEnvio = reader.IsDBNull(13) ? "PENDIENTE" : reader.GetString(13);

            // Usuario: nombre completo
            var usuarioNombre = reader.IsDBNull(4) ? "" : reader.GetString(4);
            var usuarioApellido = reader.IsDBNull(5) ? "" : reader.GetString(5);
            var nombreUsuarioCompleto = $"{usuarioNombre} {usuarioApellido}".Trim();

            // Cliente: primer nombre y primer apellido
            var clienteNombre = reader.IsDBNull(6) ? "" : reader.GetString(6);
            var clienteApellido = reader.IsDBNull(7) ? "" : reader.GetString(7);
            var primerNombreCliente = clienteNombre.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoCliente = clienteApellido.Split(' ').FirstOrDefault() ?? "";
            var nombreClienteCompleto = $"{primerNombreCliente} {primerApellidoCliente}".Trim();

            // Beneficiario: primer nombre y primer apellido
            var benefNombre = reader.IsDBNull(14) ? "" : reader.GetString(14);
            var benefApellido = reader.IsDBNull(15) ? "" : reader.GetString(15);
            var primerNombreBenef = benefNombre.Split(' ').FirstOrDefault() ?? "";
            var primerApellidoBenef = benefApellido.Split(' ').FirstOrDefault() ?? "";
            var nombreBenefCompleto = $"{primerNombreBenef} {primerApellidoBenef}".Trim();

            var importe = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8);

            // Contar por estado
            switch (estadoEnvio.ToUpper())
            {
                case "PENDIENTE":
                    pendientes++;
                    totalPendiente += importe;
                    break;
                case "ENVIADO":
                    enviados++;
                    break;
                case "ANULADO":
                    anulados++;
                    break;
            }

            string importeFormateado = $"{importe:N2}";
            string importeColor = "#0b5394";

            var item = new OperacionPackAlimentoBalanceItem
            {
                IdOperacion = reader.GetInt64(0),
                NumeroOperacion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Fecha = fechaOp.ToString("dd/MM/yy"),
                Hora = horaOp.ToString(@"hh\:mm"),
                NombreUsuario = nombreUsuarioCompleto,
                NombreCliente = nombreClienteCompleto,
                Importe = importe,
                ImporteFormateado = importeFormateado,
                ImporteColor = importeColor,
                Moneda = reader.IsDBNull(9) ? "EUR" : reader.GetString(9),
                Descripcion = reader.IsDBNull(10) ? "Pack Alimentos" : reader.GetString(10),
                PaisDestino = reader.IsDBNull(11) ? "" : reader.GetString(11),
                CiudadDestino = reader.IsDBNull(12) ? "" : reader.GetString(12),
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

        // Actualizar resumen
        TotalPendientes = pendientes;
        TotalEnviados = enviados;
        TotalAnulados = anulados;

        // Consultar saldo a favor del local (usar conexión nueva para evitar conflicto)
        await using var connSaldo = new NpgsqlConnection(ConnectionString);
        await connSaldo.OpenAsync();
        var sqlSaldo = "SELECT COALESCE(beneficio_acumulado, 0) FROM locales WHERE id_local = @idLocal";
        await using var cmdSaldo = new NpgsqlCommand(sqlSaldo, connSaldo);
        cmdSaldo.Parameters.AddWithValue("@idLocal", _idLocal);
        var saldoResult = await cmdSaldo.ExecuteScalarAsync();
        SaldoAFavor = saldoResult != null && saldoResult != DBNull.Value ? Convert.ToDecimal(saldoResult) : 0;
        SaldoAFavorTexto = $"{SaldoAFavor:N2} EUR";
        TieneSaldoAFavor = SaldoAFavor > 0;

        var balance = SaldoAFavor - totalPendiente;

        if (balance < 0)
        {
            TotalImporteAlimentos = balance.ToString("N2");
            TotalImporteAlimentosColor = "#dc3545";
        }
        else if (balance > 0)
        {
            TotalImporteAlimentos = $"+{balance:N2}";
            TotalImporteAlimentosColor = "#28a745";
        }
        else
        {
            TotalImporteAlimentos = "0.00";
            TotalImporteAlimentosColor = "#28a745";
        }
    }
}

public class OperacionItem
{
    public long IdOperacion { get; set; }
    public string Fecha { get; set; } = "";
    public string Hora { get; set; } = "";
    public string NumeroOperacion { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string CantidadDivisa { get; set; } = "";
    public string CantidadDivisaColor { get; set; } = "#0b5394";
    public string SalidaEuros { get; set; } = "";
    public string SalidaEurosColor { get; set; } = "#CC3333";
    public string EntradaEuros { get; set; } = "";
    public string EntradaEurosColor { get; set; } = "#008800";
    public string BackgroundColor { get; set; } = "White";
    public DateTime FechaHoraOrden { get; set; } = DateTime.MinValue;
    public bool EsClickeable { get; set; } = false;
    public TextDecorationCollection? TextoSubrayado { get; set; } = null;
}

public class DivisaLocal
{
    public string Codigo { get; set; } = "";
    public decimal Cantidad { get; set; } = 0;
    public string CantidadFormateada { get; set; } = "0.00";
}

public class OperacionPackAlimentoBalanceItem
{
    public long IdOperacion { get; set; }
    public string NumeroOperacion { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string Hora { get; set; } = "";
    public string NombreUsuario { get; set; } = "";
    public string NombreCliente { get; set; } = "";
    public string NombreBeneficiario { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string PaisDestino { get; set; } = "";
    public string CiudadDestino { get; set; } = "";
    public decimal Importe { get; set; }
    public string ImporteFormateado { get; set; } = "0.00";
    public string ImporteColor { get; set; } = "#444444";
    public string Moneda { get; set; } = "EUR";
    public string EstadoEnvio { get; set; } = "PENDIENTE";
    public string EstadoTexto { get; set; } = "";
    public string EstadoColor { get; set; } = "#ffc107";
    public string BackgroundColor { get; set; } = "White";
    public DateTime FechaHoraOrden { get; set; } = DateTime.MinValue;
}