using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models.Admin;
using Npgsql;

namespace Allva.Desktop.ViewModels.Admin;

public partial class PagoSuscripcionesViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    // ============================================
    // PRECIO GLOBAL
    // ============================================

    [ObservableProperty]
    private decimal _precioMensualPorLocal;

    [ObservableProperty]
    private string _nuevoPrecioTexto = "";

    // ============================================
    // TABS: LOCALES / COMERCIOS
    // ============================================

    [ObservableProperty]
    private string _tabActual = "locales";

    public bool EsTabLocales => TabActual == "locales";
    public bool EsTabComercios => TabActual == "comercios";

    public string TabLocalesBackground => EsTabLocales ? "#1d4f8c" : "#ddd";
    public string TabLocalesForeground => EsTabLocales ? "White" : "#333333";
    public string TabComerciosBackground => EsTabComercios ? "#1d4f8c" : "#ddd";
    public string TabComerciosForeground => EsTabComercios ? "White" : "#333333";

    partial void OnTabActualChanged(string value)
    {
        OnPropertyChanged(nameof(EsTabLocales));
        OnPropertyChanged(nameof(EsTabComercios));
        OnPropertyChanged(nameof(TabLocalesBackground));
        OnPropertyChanged(nameof(TabLocalesForeground));
        OnPropertyChanged(nameof(TabComerciosBackground));
        OnPropertyChanged(nameof(TabComerciosForeground));
        OnPropertyChanged(nameof(PlaceholderBusqueda));
        TextoBusqueda = "";
        MostrarPanelPago = false;
        AplicarFiltros();
    }

    // ============================================
    // FILTROS
    // ============================================

    [ObservableProperty]
    private string _filtroEstado = "Todos";

    [ObservableProperty]
    private string _textoBusqueda = "";

    public string BtnTodosBackground => FiltroEstado == "Todos" ? "#1d4f8c" : "#F0F0F0";
    public string BtnTodosForeground => FiltroEstado == "Todos" ? "White" : "#666666";
    public string BtnCorrienteBackground => FiltroEstado == "Corriente" ? "#1d4f8c" : "#F0F0F0";
    public string BtnCorrienteForeground => FiltroEstado == "Corriente" ? "White" : "#666666";
    public string BtnDeudaBackground => FiltroEstado == "Deuda" ? "#1d4f8c" : "#F0F0F0";
    public string BtnDeudaForeground => FiltroEstado == "Deuda" ? "White" : "#666666";

    public string PlaceholderBusqueda => EsTabLocales
        ? "Codigo local (ej: NOVA0001)..."
        : "Nombre de comercio...";

    partial void OnFiltroEstadoChanged(string value)
    {
        OnPropertyChanged(nameof(BtnTodosBackground));
        OnPropertyChanged(nameof(BtnTodosForeground));
        OnPropertyChanged(nameof(BtnCorrienteBackground));
        OnPropertyChanged(nameof(BtnCorrienteForeground));
        OnPropertyChanged(nameof(BtnDeudaBackground));
        OnPropertyChanged(nameof(BtnDeudaForeground));
        AplicarFiltros();
    }

    partial void OnTextoBusquedaChanged(string value) => AplicarFiltros();

    // ============================================
    // DATOS
    // ============================================

    private List<SuscripcionLocalModel> _todasSuscripciones = new();
    private List<ComercioSuscripcionModel> _todosComerciosSuscripcion = new();

    [ObservableProperty]
    private ObservableCollection<SuscripcionLocalModel> _localesFiltrados = new();

    [ObservableProperty]
    private ObservableCollection<ComercioSuscripcionModel> _comerciosFiltrados = new();

    // ============================================
    // PANEL DE PAGO
    // ============================================

    [ObservableProperty]
    private bool _mostrarPanelPago = false;

    [ObservableProperty]
    private string _pagoIdentificador = "";

    [ObservableProperty]
    private string _pagoImporteTexto = "";

    [ObservableProperty]
    private bool _pagoPorComercio = false;

    [ObservableProperty]
    private int? _pagoIdComercio;

    [ObservableProperty]
    private int? _pagoIdLocal;

    [ObservableProperty]
    private string _pagoNota = "";

    // ============================================
    // ESTADO UI
    // ============================================

    [ObservableProperty]
    private bool _cargando = false;

    [ObservableProperty]
    private bool _mostrarMensaje = false;

    [ObservableProperty]
    private string _mensaje = "";

    // ============================================
    // CONSTRUCTOR
    // ============================================

    public PagoSuscripcionesViewModel()
    {
        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        await CargarPrecioMensualAsync();
        await CargarDatosAsync();
    }

    // ============================================
    // CARGAR PRECIO GLOBAL
    // ============================================

    private async Task CargarPrecioMensualAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = "SELECT valor_decimal FROM configuracion_sistema WHERE clave = 'precio_suscripcion_mensual' LIMIT 1";
            await using var cmd = new NpgsqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                PrecioMensualPorLocal = Convert.ToDecimal(result);
                NuevoPrecioTexto = PrecioMensualPorLocal.ToString("N2");
            }
            else
            {
                PrecioMensualPorLocal = 0;
                NuevoPrecioTexto = "";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando precio: {ex.Message}");
        }
    }

    // ============================================
    // CARGAR DATOS
    // ============================================

    private async Task CargarDatosAsync()
    {
        Cargando = true;
        try
        {
            _todasSuscripciones.Clear();

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = @"
                SELECT
                    l.id_local,
                    l.id_comercio,
                    l.codigo_local,
                    l.nombre_local,
                    l.fecha_creacion,
                    c.nombre_comercio,
                    s.id_suscripcion,
                    s.fecha_inicio,
                    s.fecha_ultimo_pago,
                    s.fecha_pagado_hasta,
                    s.precio_mensual,
                    (SELECT monto_pagado FROM pagos_suscripciones ps
                     WHERE ps.id_local = l.id_local AND ps.tipo_panel = 'general'
                     ORDER BY ps.fecha_pago DESC LIMIT 1) AS importe_ultimo_pago,
                    (SELECT COUNT(*) FROM operaciones o
                     WHERE o.id_local = l.id_local) AS total_operaciones
                FROM locales l
                INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                LEFT JOIN suscripciones_locales s ON l.id_local = s.id_local AND s.tipo_panel = 'general'
                WHERE l.activo = true
                ORDER BY c.nombre_comercio, l.codigo_local";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var localesSinSuscripcion = new List<SuscripcionLocalModel>();

            while (await reader.ReadAsync())
            {
                var suscripcion = new SuscripcionLocalModel
                {
                    IdLocal = reader.GetInt32(0),
                    IdComercio = reader.GetInt32(1),
                    CodigoLocal = reader.GetString(2),
                    NombreLocal = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    FechaCreacionLocal = reader.IsDBNull(4) ? DateTime.Today : reader.GetDateTime(4),
                    NombreComercio = reader.GetString(5),
                    TotalOperaciones = reader.IsDBNull(12) ? 0 : Convert.ToInt32(reader.GetInt64(12))
                };

                if (!reader.IsDBNull(6))
                {
                    suscripcion.IdSuscripcion = reader.GetInt32(6);
                    suscripcion.FechaInicio = reader.GetDateTime(7);
                    suscripcion.FechaUltimoPago = reader.IsDBNull(8) ? null : reader.GetDateTime(8);
                    suscripcion.FechaPagadoHasta = reader.GetDateTime(9);
                    suscripcion.PrecioMensual = reader.GetDecimal(10);
                    suscripcion.ImporteUltimoPago = reader.IsDBNull(11) ? null : reader.GetDecimal(11);
                }
                else
                {
                    suscripcion.FechaInicio = DateTime.Today;
                    suscripcion.FechaPagadoHasta = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
                    suscripcion.PrecioMensual = PrecioMensualPorLocal;

                    if (PrecioMensualPorLocal > 0)
                        localesSinSuscripcion.Add(suscripcion);
                }

                _todasSuscripciones.Add(suscripcion);
            }

            await reader.CloseAsync();

            // Crear suscripciones para locales que no tienen
            foreach (var local in localesSinSuscripcion)
            {
                await CrearSuscripcionAsync(conn, local);
            }

            ConstruirComerciosDesdeLocales();
            AplicarFiltros();
        }
        catch (Exception ex)
        {
            MostrarMensajeUI($"Error al cargar datos: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task CrearSuscripcionAsync(NpgsqlConnection conn, SuscripcionLocalModel suscripcion)
    {
        try
        {
            var sql = @"
                INSERT INTO suscripciones_locales (id_local, tipo_panel, fecha_inicio, fecha_pagado_hasta, precio_mensual)
                VALUES (@idLocal, 'general', @fechaInicio, @fechaPagadoHasta, @precio)
                ON CONFLICT (id_local, tipo_panel) DO NOTHING
                RETURNING id_suscripcion";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("idLocal", suscripcion.IdLocal);
            cmd.Parameters.AddWithValue("fechaInicio", suscripcion.FechaInicio);
            cmd.Parameters.AddWithValue("fechaPagadoHasta", suscripcion.FechaPagadoHasta);
            cmd.Parameters.AddWithValue("precio", suscripcion.PrecioMensual);

            var result = await cmd.ExecuteScalarAsync();
            if (result != null)
                suscripcion.IdSuscripcion = Convert.ToInt32(result);
        }
        catch { }
    }

    // ============================================
    // AGRUPAR POR COMERCIO
    // ============================================

    private void ConstruirComerciosDesdeLocales()
    {
        _todosComerciosSuscripcion = _todasSuscripciones
            .GroupBy(s => s.IdComercio)
            .Select(g =>
            {
                var localesOrdenados = g
                    .OrderByDescending(l => l.TotalOperaciones)
                    .ThenBy(l => l.FechaCreacionLocal)
                    .ToList();

                return new ComercioSuscripcionModel
                {
                    IdComercio = g.Key,
                    NombreComercio = g.First().NombreComercio,
                    NumLocales = g.Count(),
                    PrecioMesCuota = g.Count() * PrecioMensualPorLocal,
                    FechaPagadoHasta = g.Min(x => x.FechaPagadoHasta),
                    FechaUltimoPago = g.Where(x => x.FechaUltimoPago.HasValue)
                                       .Select(x => x.FechaUltimoPago)
                                       .DefaultIfEmpty(null)
                                       .Max(),
                    ImporteUltimoPago = g.Where(x => x.ImporteUltimoPago.HasValue)
                                         .OrderByDescending(x => x.FechaUltimoPago)
                                         .Select(x => x.ImporteUltimoPago)
                                         .FirstOrDefault(),
                    DeudaTotal = g.Sum(x => x.MontoDeuda),
                    Locales = localesOrdenados
                };
            })
            .ToList();
    }

    // ============================================
    // FILTROS
    // ============================================

    private void AplicarFiltros()
    {
        if (EsTabLocales)
        {
            var filtradas = _todasSuscripciones.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                var busqueda = TextoBusqueda.ToLower().Trim();
                filtradas = filtradas.Where(s =>
                    s.CodigoLocal.ToLower().Contains(busqueda) ||
                    s.NombreComercio.ToLower().Contains(busqueda));
            }

            if (FiltroEstado == "Corriente")
                filtradas = filtradas.Where(s => !s.Debe);
            else if (FiltroEstado == "Deuda")
                filtradas = filtradas.Where(s => s.Debe);

            LocalesFiltrados = new ObservableCollection<SuscripcionLocalModel>(
                filtradas.OrderByDescending(x => x.MesesDeuda).ThenBy(x => x.CodigoLocal));
        }
        else
        {
            var filtrados = _todosComerciosSuscripcion.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TextoBusqueda))
            {
                var busqueda = TextoBusqueda.ToLower().Trim();
                filtrados = filtrados.Where(c =>
                    c.NombreComercio.ToLower().Contains(busqueda));
            }

            if (FiltroEstado == "Corriente")
                filtrados = filtrados.Where(c => !c.Debe);
            else if (FiltroEstado == "Deuda")
                filtrados = filtrados.Where(c => c.Debe);

            ComerciosFiltrados = new ObservableCollection<ComercioSuscripcionModel>(
                filtrados.OrderByDescending(x => x.DeudaTotal).ThenBy(x => x.NombreComercio));
        }
    }

    // ============================================
    // COMANDOS DE NAVEGACION
    // ============================================

    [RelayCommand]
    private void CambiarTab(string? tab)
    {
        if (string.IsNullOrEmpty(tab)) return;
        TabActual = tab;
    }

    [RelayCommand]
    private void CambiarFiltroEstado(string? estado)
    {
        if (string.IsNullOrEmpty(estado)) return;
        FiltroEstado = estado;
    }

    [RelayCommand]
    private void Buscar()
    {
        AplicarFiltros();
    }

    // ============================================
    // SELECCION PARA PAGO
    // ============================================

    [RelayCommand]
    private void SeleccionarLocal(SuscripcionLocalModel? local)
    {
        if (local == null) return;
        PagoPorComercio = false;
        PagoIdLocal = local.IdLocal;
        PagoIdComercio = null;
        PagoIdentificador = local.CodigoLocal;
        PagoImporteTexto = "";
        PagoNota = $"Pago individual para local {local.CodigoLocal}. Precio mensual: {PrecioMensualPorLocal:N2}\u20AC.";
        MostrarPanelPago = true;
    }

    [RelayCommand]
    private void SeleccionarComercio(ComercioSuscripcionModel? comercio)
    {
        if (comercio == null) return;
        PagoPorComercio = true;
        PagoIdComercio = comercio.IdComercio;
        PagoIdLocal = null;
        PagoIdentificador = comercio.NombreComercio;
        PagoImporteTexto = "";
        PagoNota = $"Pago para comercio \"{comercio.NombreComercio}\" ({comercio.NumLocales} locales). Cuota mensual total: {comercio.PrecioMesCuota:N2}\u20AC.";
        MostrarPanelPago = true;
    }

    // ============================================
    // GUARDAR PRECIO
    // ============================================

    [RelayCommand]
    private async Task GuardarPrecioAsync()
    {
        var textoLimpio = NuevoPrecioTexto.Replace(",", ".");
        if (!decimal.TryParse(textoLimpio, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal nuevoPrecio) || nuevoPrecio <= 0)
        {
            MostrarMensajeUI("Introduce un precio valido mayor a 0");
            return;
        }

        Cargando = true;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Guardar en configuracion_sistema
            var sql = @"INSERT INTO configuracion_sistema (clave, valor_decimal, descripcion)
                        VALUES ('precio_suscripcion_mensual', @valor, 'Precio mensual por local - suscripcion unificada')
                        ON CONFLICT (clave) DO UPDATE SET valor_decimal = @valor, fecha_modificacion = CURRENT_TIMESTAMP";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("valor", nuevoPrecio);
            await cmd.ExecuteNonQueryAsync();

            // Actualizar todas las suscripciones existentes
            var sqlUpdate = "UPDATE suscripciones_locales SET precio_mensual = @precio WHERE tipo_panel = 'general'";
            await using var cmdUpdate = new NpgsqlCommand(sqlUpdate, conn);
            cmdUpdate.Parameters.AddWithValue("precio", nuevoPrecio);
            await cmdUpdate.ExecuteNonQueryAsync();

            PrecioMensualPorLocal = nuevoPrecio;
            MostrarMensajeUI($"Precio actualizado a {nuevoPrecio:N2}\u20AC/mes");

            await CargarDatosAsync();
        }
        catch (Exception ex)
        {
            MostrarMensajeUI($"Error al guardar precio: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    // ============================================
    // REGISTRAR PAGO
    // ============================================

    [RelayCommand]
    private async Task RegistrarPagoAsync()
    {
        var textoLimpio = PagoImporteTexto.Replace(",", ".");
        if (!decimal.TryParse(textoLimpio, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal importe) || importe <= 0)
        {
            MostrarMensajeUI("Introduce un importe valido mayor a 0");
            return;
        }

        if (PrecioMensualPorLocal <= 0)
        {
            MostrarMensajeUI("Configura primero el precio mensual por local");
            return;
        }

        Cargando = true;
        try
        {
            if (PagoPorComercio && PagoIdComercio.HasValue)
            {
                await DistribuirPagoComercio(PagoIdComercio.Value, importe);
            }
            else if (!PagoPorComercio && PagoIdLocal.HasValue)
            {
                await DistribuirPagoLocal(PagoIdLocal.Value, importe);
            }

            MostrarPanelPago = false;
            await CargarDatosAsync();
        }
        catch (Exception ex)
        {
            MostrarMensajeUI($"Error al registrar pago: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    // ============================================
    // DISTRIBUCION DE PAGO - LOCAL INDIVIDUAL
    // ============================================

    private async Task DistribuirPagoLocal(int idLocal, decimal importe)
    {
        var local = _todasSuscripciones.FirstOrDefault(s => s.IdLocal == idLocal);
        if (local == null) return;

        var precio = PrecioMensualPorLocal;
        int mesesCompletos = (int)(importe / precio);

        if (mesesCompletos < 1)
        {
            MostrarMensajeUI("El importe no cubre ni un mes completo");
            return;
        }

        var nuevaFecha = local.FechaPagadoHasta.AddMonths(mesesCompletos);

        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        // Insertar registro de pago
        var sqlPago = @"INSERT INTO pagos_suscripciones
            (id_suscripcion, id_local, tipo_panel, meses_pagados, monto_pagado, observaciones, registrado_por)
            VALUES (@idSusc, @idLocal, 'general', @meses, @monto, @obs, 'Admin')";

        await using var cmdPago = new NpgsqlCommand(sqlPago, conn);
        cmdPago.Parameters.AddWithValue("idSusc", local.IdSuscripcion);
        cmdPago.Parameters.AddWithValue("idLocal", idLocal);
        cmdPago.Parameters.AddWithValue("meses", mesesCompletos);
        cmdPago.Parameters.AddWithValue("monto", importe);
        cmdPago.Parameters.AddWithValue("obs", $"Pago individual - {mesesCompletos} mes(es)");
        await cmdPago.ExecuteNonQueryAsync();

        // Actualizar suscripcion
        var sqlUpdate = @"UPDATE suscripciones_locales
            SET fecha_ultimo_pago = CURRENT_DATE, fecha_pagado_hasta = @fecha
            WHERE id_suscripcion = @id";
        await using var cmdUpdate = new NpgsqlCommand(sqlUpdate, conn);
        cmdUpdate.Parameters.AddWithValue("fecha", nuevaFecha);
        cmdUpdate.Parameters.AddWithValue("id", local.IdSuscripcion);
        await cmdUpdate.ExecuteNonQueryAsync();

        MostrarMensajeUI($"Pago de {importe:N2}\u20AC registrado para {local.CodigoLocal} ({mesesCompletos} mes/es)");
    }

    // ============================================
    // DISTRIBUCION DE PAGO - COMERCIO (LUMP SUM)
    // ============================================

    private async Task DistribuirPagoComercio(int idComercio, decimal importe)
    {
        var comercio = _todosComerciosSuscripcion.FirstOrDefault(c => c.IdComercio == idComercio);
        if (comercio == null || !comercio.Locales.Any()) return;

        var precio = PrecioMensualPorLocal;

        // Locales ya estan ordenados por prioridad (mas operaciones DESC, mas antiguo ASC)
        var localesOrdenados = comercio.Locales;

        // Tracking de distribucion por local
        var distribucion = new Dictionary<int, (int meses, DateTime nuevaFecha, decimal monto)>();
        foreach (var local in localesOrdenados)
        {
            distribucion[local.IdLocal] = (0, local.FechaPagadoHasta, 0m);
        }

        decimal importeRestante = importe;

        // Distribuir round-robin por prioridad
        bool seguir = true;
        while (seguir && importeRestante >= precio)
        {
            seguir = false;
            foreach (var local in localesOrdenados)
            {
                if (importeRestante >= precio)
                {
                    var (meses, fecha, monto) = distribucion[local.IdLocal];
                    distribucion[local.IdLocal] = (meses + 1, fecha.AddMonths(1), monto + precio);
                    importeRestante -= precio;
                    seguir = true;
                }
                else break;
            }
        }

        // Si queda dinero para meses individuales por prioridad
        foreach (var local in localesOrdenados)
        {
            if (importeRestante >= precio)
            {
                var (meses, fecha, monto) = distribucion[local.IdLocal];
                distribucion[local.IdLocal] = (meses + 1, fecha.AddMonths(1), monto + precio);
                importeRestante -= precio;
            }
            else break;
        }

        // Persistir en transaccion
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            int localesActualizados = 0;
            int totalMeses = 0;

            foreach (var local in localesOrdenados)
            {
                var (meses, nuevaFecha, monto) = distribucion[local.IdLocal];
                if (meses <= 0) continue;

                // Insertar pago
                var sqlPago = @"INSERT INTO pagos_suscripciones
                    (id_suscripcion, id_local, tipo_panel, meses_pagados, monto_pagado, observaciones, registrado_por)
                    VALUES (@idSusc, @idLocal, 'general', @meses, @monto, @obs, 'Admin')";

                await using var cmdPago = new NpgsqlCommand(sqlPago, conn);
                cmdPago.Parameters.AddWithValue("idSusc", local.IdSuscripcion);
                cmdPago.Parameters.AddWithValue("idLocal", local.IdLocal);
                cmdPago.Parameters.AddWithValue("meses", meses);
                cmdPago.Parameters.AddWithValue("monto", monto);
                cmdPago.Parameters.AddWithValue("obs", $"Pago comercio {comercio.NombreComercio} - {meses} mes(es)");
                await cmdPago.ExecuteNonQueryAsync();

                // Actualizar suscripcion
                var sqlUpdate = @"UPDATE suscripciones_locales
                    SET fecha_ultimo_pago = CURRENT_DATE, fecha_pagado_hasta = @fecha
                    WHERE id_suscripcion = @id";
                await using var cmdUpdate = new NpgsqlCommand(sqlUpdate, conn);
                cmdUpdate.Parameters.AddWithValue("fecha", nuevaFecha);
                cmdUpdate.Parameters.AddWithValue("id", local.IdSuscripcion);
                await cmdUpdate.ExecuteNonQueryAsync();

                localesActualizados++;
                totalMeses += meses;
            }

            await transaction.CommitAsync();
            MostrarMensajeUI($"Pago de {importe:N2}\u20AC registrado para {comercio.NombreComercio} ({localesActualizados} locales, {totalMeses} mes/es total)");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ============================================
    // UTILIDADES
    // ============================================

    private void MostrarMensajeUI(string texto)
    {
        Mensaje = texto;
        MostrarMensaje = true;
        Task.Delay(4000).ContinueWith(_ => MostrarMensaje = false);
    }
}
