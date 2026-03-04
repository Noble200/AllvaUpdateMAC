using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models.Admin;
using Npgsql;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el panel de Administración consolidado
/// Muestra operaciones, balances y actividad de todos los locales del comercio
/// </summary>
public partial class AdministracionViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    [ObservableProperty]
    private string _titulo = "Administración";

    [ObservableProperty]
    private string _descripcion = "Panel de administración del sistema";

    // Datos de sesión
    private int _idUsuario;
    private int _idLocal;
    private int _idComercio;
    private string _userName = "";
    private string _localCode = "";

    // Colecciones de datos
    [ObservableProperty]
    private ObservableCollection<OperacionConsolidadaModel> _operaciones = new();

    [ObservableProperty]
    private ObservableCollection<OperacionConsolidadaModel> _operacionesFiltradas = new();

    [ObservableProperty]
    private ObservableCollection<ActividadUsuarioModel> _actividadesUsuarios = new();

    [ObservableProperty]
    private ObservableCollection<ActividadUsuarioModel> _actividadesFiltradas = new();

    [ObservableProperty]
    private ObservableCollection<BalanceConsolidadoModel> _balances = new();

    // Estados y filtros
    [ObservableProperty]
    private bool _cargando = false;

    [ObservableProperty]
    private string _vistaActual = "resumen";

    // Propiedades para la navegación entre vistas
    public bool EsVistaResumen => VistaActual == "resumen";
    public bool EsVistaOperaciones => VistaActual == "operaciones";
    public bool EsVistaBalance => VistaActual == "balance";

    public string FechaActual => DateTime.Now.ToString("dd/MM/yyyy");

    // Propiedades para el resumen por módulo
    [ObservableProperty]
    private int _totalOperacionesDivisas = 0;

    [ObservableProperty]
    private decimal _totalMontoDivisas = 0;

    [ObservableProperty]
    private int _totalOperacionesAlimentos = 0;

    [ObservableProperty]
    private decimal _totalMontoAlimentos = 0;

    [ObservableProperty]
    private int _totalOperacionesBilletes = 0;

    [ObservableProperty]
    private decimal _totalMontoBilletes = 0;

    [ObservableProperty]
    private int _totalOperacionesViajes = 0;

    [ObservableProperty]
    private decimal _totalMontoViajes = 0;

    [ObservableProperty]
    private decimal _totalGeneral = 0;

    [ObservableProperty]
    private int _totalOperacionesGeneral = 0;

    [ObservableProperty]
    private string _filtroFechaDesde = DateTime.Now.AddYears(-1).ToString("dd/MM/yyyy");

    [ObservableProperty]
    private string _filtroFechaHasta = DateTime.Now.ToString("dd/MM/yyyy");

    [ObservableProperty]
    private string _filtroBusqueda = string.Empty;

    [ObservableProperty]
    private string _filtroLocal = "Todos";

    [ObservableProperty]
    private string _filtroTipoOperacion = "Todas";

    [ObservableProperty]
    private string _filtroEstado = "Todos";

    [ObservableProperty]
    private ObservableCollection<string> _localesDisponibles = new() { "Todos" };

    // Filtros para Operaciones
    [ObservableProperty]
    private string _localFiltro = "Todos";

    [ObservableProperty]
    private string _fechaDesde = DateTime.Now.AddMonths(-1).ToString("dd/MM/yyyy");

    [ObservableProperty]
    private string _fechaHasta = DateTime.Now.ToString("dd/MM/yyyy");

    [ObservableProperty]
    private string _moduloFiltro = "Todos";

    [ObservableProperty]
    private string _estadoFiltro = "Todos";

    [ObservableProperty]
    private ObservableCollection<string> _modulosDisponibles = new()
    {
        "Todos",
        "DIVISAS",
        "PACK_ALIMENTOS",
        "BILLETES",
        "PACK_VIAJES"
    };

    [ObservableProperty]
    private ObservableCollection<string> _estadosDisponibles = new()
    {
        "Todos",
        "COMPLETADA",
        "PENDIENTE",
        "ANULADA",
        "CANCELADA"
    };

    // Filtros para Balance
    [ObservableProperty]
    private string _localFiltroBalance = "Todos";

    [ObservableProperty]
    private string _fechaDesdeBalance = DateTime.Now.AddMonths(-1).ToString("dd/MM/yyyy");

    [ObservableProperty]
    private string _fechaHastaBalance = DateTime.Now.ToString("dd/MM/yyyy");

    [ObservableProperty]
    private string _moduloFiltroBalance = "Todos";

    // Totales calculados
    [ObservableProperty]
    private decimal _totalMontoOperaciones = 0;

    [ObservableProperty]
    private decimal _balanceGeneral = 0;

    [ObservableProperty]
    private string _colorBalanceGeneral = "#28a745";

    // Estadísticas
    [ObservableProperty]
    private int _totalOperaciones = 0;

    [ObservableProperty]
    private decimal _totalIngresos = 0;

    [ObservableProperty]
    private decimal _totalEgresos = 0;

    [ObservableProperty]
    private int _totalLocales = 0;

    [ObservableProperty]
    private int _usuariosActivos = 0;

    public AdministracionViewModel()
    {
        // Constructor por defecto
    }

    public AdministracionViewModel(int idUsuario, int idLocal, int idComercio, string userName, string localCode)
    {
        _idUsuario = idUsuario;
        _idLocal = idLocal;
        _idComercio = idComercio;
        _userName = userName;
        _localCode = localCode;

        _ = CargarDatosIniciales();
    }

    private async Task CargarDatosIniciales()
    {
        Cargando = true;
        try
        {
            // Cargar resumen por defecto
            await CargarResumenPorModulo();

            // Cargar cada uno por separado para que los errores no bloqueen los demás
            await CargarLocalesDelComercio();
            await CargarOperaciones();

            try
            {
                await CargarActividadUsuarios();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar actividad (tabla puede no existir): {ex.Message}");
                ActividadesUsuarios.Clear();
                ActividadesFiltradas.Clear();
            }

            await CargarBalances();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error general al cargar datos iniciales: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    [RelayCommand]
    private void CambiarVista(string vista)
    {
        VistaActual = vista;
        OnPropertyChanged(nameof(EsVistaResumen));
        OnPropertyChanged(nameof(EsVistaOperaciones));
        OnPropertyChanged(nameof(EsVistaBalance));
    }

    private async Task CargarResumenPorModulo()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Query para obtener estadísticas por módulo
            var query = @"
                SELECT
                    o.modulo,
                    COUNT(*) as cantidad,
                    COALESCE(SUM(o.importe_total), 0) as total_monto
                FROM operaciones o
                WHERE o.id_comercio = @idComercio
                    AND o.estado != 'ANULADA'
                    AND o.estado != 'CANCELADA'
                GROUP BY o.modulo";

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@idComercio", _idComercio);

            await using var reader = await command.ExecuteReaderAsync();

            // Resetear totales
            TotalOperacionesDivisas = 0;
            TotalMontoDivisas = 0;
            TotalOperacionesAlimentos = 0;
            TotalMontoAlimentos = 0;
            TotalOperacionesBilletes = 0;
            TotalMontoBilletes = 0;
            TotalOperacionesViajes = 0;
            TotalMontoViajes = 0;

            while (await reader.ReadAsync())
            {
                var modulo = reader.GetString(0);
                var cantidad = Convert.ToInt32(reader.GetInt64(1));
                var monto = reader.GetDecimal(2);

                switch (modulo.ToUpper())
                {
                    case "DIVISAS":
                        TotalOperacionesDivisas = cantidad;
                        TotalMontoDivisas = monto;
                        break;
                    case "PACK_ALIMENTOS":
                        TotalOperacionesAlimentos = cantidad;
                        TotalMontoAlimentos = monto;
                        break;
                    case "BILLETES":
                        TotalOperacionesBilletes = cantidad;
                        TotalMontoBilletes = monto;
                        break;
                    case "PACK_VIAJES":
                        TotalOperacionesViajes = cantidad;
                        TotalMontoViajes = monto;
                        break;
                }
            }

            // Calcular totales generales
            TotalGeneral = TotalMontoDivisas + TotalMontoAlimentos + TotalMontoBilletes + TotalMontoViajes;
            TotalOperacionesGeneral = TotalOperacionesDivisas + TotalOperacionesAlimentos + TotalOperacionesBilletes + TotalOperacionesViajes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar resumen por módulo: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CargarLocalesDelComercio()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT DISTINCT codigo_local
                FROM locales
                WHERE id_comercio = @idComercio AND activo = true
                ORDER BY codigo_local";

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@idComercio", _idComercio);

            await using var reader = await command.ExecuteReaderAsync();

            LocalesDisponibles.Clear();
            LocalesDisponibles.Add("Todos");

            while (await reader.ReadAsync())
            {
                LocalesDisponibles.Add(reader.GetString(0));
            }

            TotalLocales = LocalesDisponibles.Count - 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar locales: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CargarOperaciones()
    {
        try
        {
            Console.WriteLine($"[DEBUG] Iniciando carga de operaciones...");
            Console.WriteLine($"[DEBUG] IdComercio: {_idComercio}");
            Console.WriteLine($"[DEBUG] FechaDesde: {FiltroFechaDesde}");
            Console.WriteLine($"[DEBUG] FechaHasta: {FiltroFechaHasta}");

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Query consolidada que une operaciones de todos los módulos
            var query = @"
                SELECT
                    o.id_operacion as id,
                    o.tipo_operacion,
                    o.modulo,
                    o.fecha_operacion as fecha,
                    o.numero_operacion as numero,
                    o.nombre_cliente as cliente,
                    o.importe_total as monto,
                    o.moneda,
                    o.estado,
                    o.nombre_usuario as usuario,
                    o.id_usuario,
                    o.codigo_local,
                    o.id_local,
                    o.observaciones
                FROM operaciones o
                WHERE o.id_comercio = @idComercio
                    AND o.fecha_operacion >= @fechaDesde::date
                    AND o.fecha_operacion <= @fechaHasta::date + interval '1 day'
                ORDER BY o.fecha_operacion DESC, o.hora_operacion DESC
                LIMIT 1000";

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@idComercio", _idComercio);
            command.Parameters.AddWithValue("@fechaDesde", ParseFecha(FiltroFechaDesde));
            command.Parameters.AddWithValue("@fechaHasta", ParseFecha(FiltroFechaHasta));

            Console.WriteLine($"[DEBUG] Query preparada, ejecutando...");

            await using var reader = await command.ExecuteReaderAsync();

            Operaciones.Clear();

            int contador = 0;
            while (await reader.ReadAsync())
            {
                var operacion = new OperacionConsolidadaModel
                {
                    IdOperacion = reader.GetInt32(0),
                    TipoOperacion = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Modulo = reader.GetString(2),
                    FechaHora = reader.GetDateTime(3),
                    NumeroOperacion = reader.GetString(4),
                    Cliente = reader.IsDBNull(5) ? "N/A" : reader.GetString(5),
                    Monto = reader.GetDecimal(6),
                    Moneda = reader.IsDBNull(7) ? "EUR" : reader.GetString(7),
                    Estado = reader.GetString(8),
                    Usuario = reader.IsDBNull(9) ? "N/A" : reader.GetString(9),
                    IdUsuario = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                    CodigoLocal = reader.GetString(11),
                    IdLocal = reader.GetInt32(12),
                    Local = reader.GetString(11), // Usar codigo_local como nombre de local por ahora
                    Observaciones = reader.IsDBNull(13) ? null : reader.GetString(13)
                };

                Operaciones.Add(operacion);
                contador++;
            }

            Console.WriteLine($"[DEBUG] Operaciones cargadas: {contador}");
            Console.WriteLine($"[DEBUG] Total en colección: {Operaciones.Count}");

            TotalOperaciones = Operaciones.Count;
            TotalIngresos = Operaciones.Where(o => o.Estado != "ANULADA" && o.Estado != "CANCELADA").Sum(o => o.Monto);

            AplicarFiltrosOperaciones();

            Console.WriteLine($"[DEBUG] Operaciones filtradas: {OperacionesFiltradas.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error al cargar operaciones: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    private async Task CargarActividadUsuarios()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Esta tabla debe crearse para registrar la actividad
            var query = @"
                SELECT
                    a.id_actividad,
                    u.nombre_usuario,
                    a.id_usuario,
                    u.numero_usuario,
                    a.tipo_actividad,
                    a.fecha_hora,
                    l.nombre_local,
                    l.codigo_local,
                    a.id_local,
                    a.direccion_ip,
                    a.detalles
                FROM actividad_usuarios a
                INNER JOIN usuarios u ON a.id_usuario = u.id_usuario
                INNER JOIN locales l ON a.id_local = l.id_local
                WHERE l.id_comercio = @idComercio
                    AND a.fecha_hora >= @fechaDesde::timestamp
                    AND a.fecha_hora <= @fechaHasta::timestamp + interval '1 day'
                ORDER BY a.fecha_hora DESC
                LIMIT 500";

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@idComercio", _idComercio);
            command.Parameters.AddWithValue("@fechaDesde", ParseFecha(FiltroFechaDesde));
            command.Parameters.AddWithValue("@fechaHasta", ParseFecha(FiltroFechaHasta));

            await using var reader = await command.ExecuteReaderAsync();

            ActividadesUsuarios.Clear();

            while (await reader.ReadAsync())
            {
                var actividad = new ActividadUsuarioModel
                {
                    IdActividad = reader.GetInt32(0),
                    Usuario = reader.GetString(1),
                    IdUsuario = reader.GetInt32(2),
                    NumeroUsuario = reader.GetString(3),
                    TipoActividad = reader.GetString(4),
                    FechaHora = reader.GetDateTime(5),
                    Local = reader.GetString(6),
                    CodigoLocal = reader.GetString(7),
                    IdLocal = reader.GetInt32(8),
                    DireccionIp = reader.IsDBNull(9) ? null : reader.GetString(9),
                    Detalles = reader.IsDBNull(10) ? null : reader.GetString(10)
                };

                ActividadesUsuarios.Add(actividad);
            }

            UsuariosActivos = ActividadesUsuarios
                .Where(a => a.TipoActividad.ToLower().Contains("login"))
                .Select(a => a.IdUsuario)
                .Distinct()
                .Count();

            AplicarFiltrosActividad();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar actividad: {ex.Message}");
            // Si la tabla no existe, crear colección vacía
            ActividadesUsuarios.Clear();
            ActividadesFiltradas.Clear();
        }
    }

    [RelayCommand]
    private async Task CargarBalances()
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Calcular balance por local desde la tabla operaciones
            var query = @"
                SELECT
                    o.id_local,
                    o.codigo_local,
                    SUM(CASE WHEN o.estado != 'ANULADA' THEN o.importe_total ELSE 0 END) as ingresos,
                    COUNT(CASE WHEN o.estado != 'ANULADA' THEN 1 END) as cantidad,
                    MAX(o.fecha_operacion) as ultima_operacion
                FROM operaciones o
                WHERE o.id_comercio = @idComercio
                    AND o.fecha_operacion >= @fechaDesde::date
                    AND o.fecha_operacion <= @fechaHasta::date + interval '1 day'
                GROUP BY o.id_local, o.codigo_local
                ORDER BY o.codigo_local";

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@idComercio", _idComercio);
            command.Parameters.AddWithValue("@fechaDesde", ParseFecha(FiltroFechaDesde));
            command.Parameters.AddWithValue("@fechaHasta", ParseFecha(FiltroFechaHasta));

            await using var reader = await command.ExecuteReaderAsync();

            Balances.Clear();

            while (await reader.ReadAsync())
            {
                var balance = new BalanceConsolidadoModel
                {
                    IdLocal = reader.GetInt32(0),
                    CodigoLocal = reader.GetString(1),
                    Local = reader.GetString(1), // Usar codigo_local como nombre
                    SaldoInicial = 0,
                    Ingresos = reader.GetDecimal(2),
                    Egresos = 0,
                    SaldoFinal = reader.GetDecimal(2),
                    CantidadOperaciones = reader.GetInt32(3),
                    FechaUltimaOperacion = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4)
                };

                Balances.Add(balance);
            }

            TotalLocales = Balances.Count;
            TotalIngresos = Balances.Sum(b => b.Ingresos);
            TotalEgresos = Balances.Sum(b => b.Egresos);
            BalanceGeneral = TotalIngresos - TotalEgresos;
            ColorBalanceGeneral = BalanceGeneral >= 0 ? "#28a745" : "#dc3545";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar balances: {ex.Message}");
        }
    }

    partial void OnFiltroBusquedaChanged(string value)
    {
        AplicarFiltrosOperaciones();
        AplicarFiltrosActividad();
    }

    partial void OnFiltroLocalChanged(string value)
    {
        AplicarFiltrosOperaciones();
        AplicarFiltrosActividad();
    }

    partial void OnFiltroTipoOperacionChanged(string value)
    {
        AplicarFiltrosOperaciones();
    }

    partial void OnFiltroEstadoChanged(string value)
    {
        AplicarFiltrosOperaciones();
    }

    [RelayCommand]
    private void AplicarFiltros()
    {
        AplicarFiltrosOperaciones();
    }

    [RelayCommand]
    private void AplicarFiltrosOperaciones()
    {
        var filtradas = Operaciones.AsEnumerable();

        // Aplicar filtro por local
        if (LocalFiltro != "Todos")
        {
            filtradas = filtradas.Where(o => o.CodigoLocal == LocalFiltro);
        }

        // Aplicar filtro por módulo
        if (ModuloFiltro != "Todos")
        {
            filtradas = filtradas.Where(o => o.Modulo == ModuloFiltro);
        }

        // Aplicar filtro por estado
        if (EstadoFiltro != "Todos")
        {
            filtradas = filtradas.Where(o => o.Estado == EstadoFiltro);
        }

        // Aplicar búsqueda general
        if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
        {
            var busqueda = FiltroBusqueda.ToLower();
            filtradas = filtradas.Where(o =>
                o.NumeroOperacion.ToLower().Contains(busqueda) ||
                o.Cliente.ToLower().Contains(busqueda) ||
                o.Usuario.ToLower().Contains(busqueda) ||
                o.Local.ToLower().Contains(busqueda));
        }

        // Filtros de fecha antiguos para compatibilidad
        if (FiltroLocal != "Todos")
        {
            filtradas = filtradas.Where(o => o.CodigoLocal == FiltroLocal);
        }

        if (FiltroTipoOperacion != "Todas")
        {
            filtradas = filtradas.Where(o => o.TipoOperacion == FiltroTipoOperacion);
        }

        if (FiltroEstado != "Todos")
        {
            filtradas = filtradas.Where(o => o.Estado == FiltroEstado);
        }

        OperacionesFiltradas.Clear();
        foreach (var op in filtradas)
        {
            OperacionesFiltradas.Add(op);
        }

        // Calcular total de monto de operaciones filtradas
        TotalMontoOperaciones = OperacionesFiltradas
            .Where(o => o.Estado != "ANULADA" && o.Estado != "CANCELADA")
            .Sum(o => o.Monto);
    }

    [RelayCommand]
    private void LimpiarFiltrosOperaciones()
    {
        LocalFiltro = "Todos";
        FechaDesde = DateTime.Now.AddMonths(-1).ToString("dd/MM/yyyy");
        FechaHasta = DateTime.Now.ToString("dd/MM/yyyy");
        ModuloFiltro = "Todos";
        EstadoFiltro = "Todos";
        FiltroBusqueda = string.Empty;
        AplicarFiltrosOperaciones();
    }

    [RelayCommand]
    private void AplicarFiltrosBalance()
    {
        // Calcular totales de balance
        BalanceGeneral = TotalIngresos - TotalEgresos;
        ColorBalanceGeneral = BalanceGeneral >= 0 ? "#28a745" : "#dc3545";
    }

    [RelayCommand]
    private void LimpiarFiltrosBalance()
    {
        LocalFiltroBalance = "Todos";
        FechaDesdeBalance = DateTime.Now.AddMonths(-1).ToString("dd/MM/yyyy");
        FechaHastaBalance = DateTime.Now.ToString("dd/MM/yyyy");
        ModuloFiltroBalance = "Todos";
    }

    [RelayCommand]
    private void AplicarFiltrosActividad()
    {
        var filtradas = ActividadesUsuarios.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroBusqueda))
        {
            var busqueda = FiltroBusqueda.ToLower();
            filtradas = filtradas.Where(a =>
                a.Usuario.ToLower().Contains(busqueda) ||
                a.NumeroUsuario.ToLower().Contains(busqueda) ||
                a.Local.ToLower().Contains(busqueda));
        }

        if (FiltroLocal != "Todos")
        {
            filtradas = filtradas.Where(a => a.CodigoLocal == FiltroLocal);
        }

        ActividadesFiltradas.Clear();
        foreach (var act in filtradas)
        {
            ActividadesFiltradas.Add(act);
        }
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        FiltroBusqueda = string.Empty;
        FiltroLocal = "Todos";
        FiltroTipoOperacion = "Todas";
        FiltroEstado = "Todos";
        FiltroFechaDesde = DateTime.Now.AddYears(-1).ToString("dd/MM/yyyy");
        FiltroFechaHasta = DateTime.Now.ToString("dd/MM/yyyy");
    }

    [RelayCommand]
    private async Task ActualizarDatos()
    {
        await CargarDatosIniciales();
    }


    private DateTime ParseFecha(string fecha)
    {
        try
        {
            if (DateTime.TryParseExact(fecha, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var result))
                return result;
            return DateTime.Now;
        }
        catch
        {
            return DateTime.Now;
        }
    }
}
