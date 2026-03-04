using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models.Admin;
using Npgsql;

namespace Allva.Desktop.ViewModels.Admin;

// ============================================
// MODELOS PARA PACK ALIMENTOS
// ============================================

public class ComercioConAlimentosModel
{
    public int IdComercio { get; set; }
    public string NombreComercio { get; set; } = "";
    public string Pais { get; set; } = "";
    public bool Activo { get; set; }
    public decimal ComisionLocal { get; set; }
    public decimal ComisionAllva { get; set; }
    public int CantidadLocales { get; set; }
    public bool UsaComisionPropia { get; set; }

    public string ComisionLocalTexto => $"{ComisionLocal:N2} EUR";
    public string ComisionAllvaTexto => $"{ComisionAllva:N2} EUR";
    public string ComisionTotalTexto => $"{(ComisionLocal + ComisionAllva):N2} EUR";
    public string EstadoTexto => UsaComisionPropia ? "Personalizado" : "Global";
}

public class LocalConAlimentosModel
{
    public int IdLocal { get; set; }
    public int IdComercio { get; set; }
    public string CodigoLocal { get; set; } = "";
    public string NombreLocal { get; set; } = "";
    public string NombreComercio { get; set; } = "";
    public bool Activo { get; set; }
    public decimal ComisionLocalPropia { get; set; }
    public decimal ComisionAllvaPropia { get; set; }
    public decimal ComisionLocalEfectiva { get; set; }
    public decimal ComisionAllvaEfectiva { get; set; }
    public string EstadoComision { get; set; } = "Global";

    public string ComisionLocalTexto => $"{ComisionLocalEfectiva:N2} EUR";
    public string ComisionAllvaTexto => $"{ComisionAllvaEfectiva:N2} EUR";
    public string ComisionTotalTexto => $"{(ComisionLocalEfectiva + ComisionAllvaEfectiva):N2} EUR";
}

public partial class ComisionesViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    // ============================================
    // PANEL ACTUAL (TABS DE MODULOS)
    // ============================================

    [ObservableProperty]
    private string _panelActual = "divisas";

    public bool EsPanelDivisas => PanelActual == "divisas";
    public bool EsPanelAlimentos => PanelActual == "alimentos";
    public bool EsPanelBilletes => PanelActual == "billetes";
    public bool EsPanelViajes => PanelActual == "viajes";

    public string TabDivisasBackground => EsPanelDivisas ? "#ffd966" : "#F0F0F0";
    public string TabDivisasForeground => EsPanelDivisas ? "#0b5394" : "#666666";
    public string TabAlimentosBackground => EsPanelAlimentos ? "#ffd966" : "#F0F0F0";
    public string TabAlimentosForeground => EsPanelAlimentos ? "#0b5394" : "#666666";
    public string TabBilletesBackground => EsPanelBilletes ? "#ffd966" : "#F0F0F0";
    public string TabBilletesForeground => EsPanelBilletes ? "#0b5394" : "#666666";
    public string TabViajesBackground => EsPanelViajes ? "#ffd966" : "#F0F0F0";
    public string TabViajesForeground => EsPanelViajes ? "#0b5394" : "#666666";

    partial void OnPanelActualChanged(string value)
    {
        OnPropertyChanged(nameof(EsPanelDivisas));
        OnPropertyChanged(nameof(EsPanelAlimentos));
        OnPropertyChanged(nameof(EsPanelBilletes));
        OnPropertyChanged(nameof(EsPanelViajes));
        OnPropertyChanged(nameof(TabDivisasBackground));
        OnPropertyChanged(nameof(TabDivisasForeground));
        OnPropertyChanged(nameof(TabAlimentosBackground));
        OnPropertyChanged(nameof(TabAlimentosForeground));
        OnPropertyChanged(nameof(TabBilletesBackground));
        OnPropertyChanged(nameof(TabBilletesForeground));
        OnPropertyChanged(nameof(TabViajesBackground));
        OnPropertyChanged(nameof(TabViajesForeground));
        
        MostrarPanelEdicion = false;
        MostrarPanelEdicionAlimentos = false;
        TextoBusqueda = "";
        TextoBusquedaAlimentos = "";
    }

    [RelayCommand]
    private void CambiarPanel(string? panel)
    {
        if (string.IsNullOrEmpty(panel)) return;
        PanelActual = panel;
    }

    // ============================================
    // PROPIEDADES DE DIVISAS (ORIGINAL)
    // ============================================

    [ObservableProperty]
    private decimal _margenGlobal = 10.00m;
    
    [ObservableProperty]
    private string _margenGlobalTexto = "10.00";

    private ObservableCollection<ComercioConDivisaModel> _comercios = new();
    private ObservableCollection<LocalConDivisaModel> _locales = new();

    [ObservableProperty]
    private ObservableCollection<ComercioConDivisaModel> _comerciosFiltrados = new();

    [ObservableProperty]
    private ObservableCollection<LocalConDivisaModel> _localesFiltrados = new();

    [ObservableProperty]
    private ComercioConDivisaModel? _comercioSeleccionado;

    [ObservableProperty]
    private LocalConDivisaModel? _localSeleccionado;

    [ObservableProperty]
    private bool _cargando;

    [ObservableProperty]
    private bool _mostrarMensajeExito;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    [ObservableProperty]
    private bool _mostrarPanelEdicion = false;

    [ObservableProperty]
    private string _tituloPanelEdicion = "Editar Margen";

    [ObservableProperty]
    private string _vistaActual = "comercios";

    [ObservableProperty]
    private string _nuevoMargenTexto = "10.00";

    [ObservableProperty]
    private string _tipoEdicion = "GLOBAL";

    [ObservableProperty]
    private string _textoBusqueda = string.Empty;

    public bool EsVistaComercios => VistaActual == "comercios";
    public bool EsVistaLocales => VistaActual == "locales";

    // ============================================
    // PROPIEDADES DE PACK ALIMENTOS (NUEVO - DOBLE COMISION)
    // ============================================

    [ObservableProperty]
    private decimal _comisionLocalAlimentosGlobal = 2.00m;

    [ObservableProperty]
    private string _comisionLocalAlimentosGlobalTexto = "2.00";

    [ObservableProperty]
    private decimal _comisionAllvaAlimentosGlobal = 3.00m;

    [ObservableProperty]
    private string _comisionAllvaAlimentosGlobalTexto = "3.00";

    public string ComisionTotalAlimentosGlobalTexto => $"{(ComisionLocalAlimentosGlobal + ComisionAllvaAlimentosGlobal):N2}";

    private ObservableCollection<ComercioConAlimentosModel> _comerciosAlimentos = new();
    private ObservableCollection<LocalConAlimentosModel> _localesAlimentos = new();

    [ObservableProperty]
    private ObservableCollection<ComercioConAlimentosModel> _comerciosAlimentosFiltrados = new();

    [ObservableProperty]
    private ObservableCollection<LocalConAlimentosModel> _localesAlimentosFiltrados = new();

    [ObservableProperty]
    private ComercioConAlimentosModel? _comercioAlimentosSeleccionado;

    [ObservableProperty]
    private LocalConAlimentosModel? _localAlimentosSeleccionado;

    [ObservableProperty]
    private string _vistaActualAlimentos = "comercios";

    [ObservableProperty]
    private string _textoBusquedaAlimentos = string.Empty;

    [ObservableProperty]
    private bool _mostrarPanelEdicionAlimentos = false;

    [ObservableProperty]
    private string _tituloPanelEdicionAlimentos = "Editar Comisiones";

    [ObservableProperty]
    private string _tipoEdicionAlimentos = "GLOBAL";

    [ObservableProperty]
    private string _nuevaComisionLocalTexto = "2.00";

    [ObservableProperty]
    private string _nuevaComisionAllvaTexto = "3.00";

    public bool EsVistaComerciosAlimentos => VistaActualAlimentos == "comercios";
    public bool EsVistaLocalesAlimentos => VistaActualAlimentos == "locales";

    // ============================================
    // PROPIEDADES DE BILLETES Y VIAJES (ORIGINAL)
    // ============================================

    [ObservableProperty]
    private decimal _comisionBilletesGlobal = 3.00m;

    [ObservableProperty]
    private string _comisionBilletesGlobalTexto = "3.00";

    [ObservableProperty]
    private decimal _comisionViajesGlobal = 8.00m;

    [ObservableProperty]
    private string _comisionViajesGlobalTexto = "8.00";

    // ============================================
    // CONSTRUCTOR
    // ============================================

    public ComisionesViewModel()
    {
        _ = CargarDatosAsync();
    }

    private async Task CargarDatosAsync()
    {
        Cargando = true;
        try
        {
            await CrearColumnasAlimentosAsync();
            await CargarMargenGlobalAsync();
            await CargarComisionesModulosAsync();
            await CargarComerciosAsync();
            await CargarLocalesAsync();
            await CargarComerciosAlimentosAsync();
            await CargarLocalesAlimentosAsync();
            AplicarFiltro();
            AplicarFiltroAlimentos();
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar datos: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    // ============================================
    // CREAR COLUMNAS PACK ALIMENTOS
    // ============================================

    private async Task CrearColumnasAlimentosAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            string[] columnas = { "comision_local_alimentos", "comision_allva_alimentos" };
            
            foreach (var col in columnas)
            {
                var checkCom = $"SELECT EXISTS(SELECT FROM information_schema.columns WHERE table_name='comercios' AND column_name='{col}')";
                await using (var cmd = new NpgsqlCommand(checkCom, conn))
                {
                    if (!(bool)(await cmd.ExecuteScalarAsync() ?? false))
                    {
                        await using var alt = new NpgsqlCommand($"ALTER TABLE comercios ADD COLUMN {col} NUMERIC(10,2) DEFAULT 0.00", conn);
                        await alt.ExecuteNonQueryAsync();
                    }
                }

                var checkLoc = $"SELECT EXISTS(SELECT FROM information_schema.columns WHERE table_name='locales' AND column_name='{col}')";
                await using (var cmd = new NpgsqlCommand(checkLoc, conn))
                {
                    if (!(bool)(await cmd.ExecuteScalarAsync() ?? false))
                    {
                        await using var alt = new NpgsqlCommand($"ALTER TABLE locales ADD COLUMN {col} NUMERIC(10,2) DEFAULT 0.00", conn);
                        await alt.ExecuteNonQueryAsync();
                    }
                }
            }
        }
        catch { }
    }

    // ============================================
    // CARGAR DATOS (ORIGINAL)
    // ============================================

    private async Task CargarMargenGlobalAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var checkTableSql = @"SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'configuracion_sistema')";
            await using var checkCmd = new NpgsqlCommand(checkTableSql, conn);
            var tableExists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);
            
            if (!tableExists)
            {
                var createTableSql = @"
                    CREATE TABLE configuracion_sistema (
                        id_config SERIAL PRIMARY KEY,
                        clave VARCHAR(100) UNIQUE NOT NULL,
                        valor_texto TEXT,
                        valor_decimal NUMERIC(10,2),
                        valor_entero INTEGER,
                        valor_booleano BOOLEAN,
                        descripcion TEXT,
                        fecha_creacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        fecha_modificacion TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )";
                await using var createCmd = new NpgsqlCommand(createTableSql, conn);
                await createCmd.ExecuteNonQueryAsync();
                
                var insertSql = @"INSERT INTO configuracion_sistema (clave, valor_decimal, descripcion) VALUES ('margen_divisas_global', 10.00, 'Margen global para operaciones de divisas')";
                await using var insertCmd = new NpgsqlCommand(insertSql, conn);
                await insertCmd.ExecuteNonQueryAsync();
            }

            var sql = "SELECT valor_decimal FROM configuracion_sistema WHERE clave = 'margen_divisas_global' LIMIT 1";
            await using var cmd = new NpgsqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
                MargenGlobal = Convert.ToDecimal(result);
            else
                MargenGlobal = 10.00m;

            MargenGlobalTexto = MargenGlobal.ToString("N2");
        }
        catch
        {
            MargenGlobal = 10.00m;
            MargenGlobalTexto = "10.00";
        }
    }

    private async Task CargarComisionesModulosAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Pack Alimentos - Comision Local
            var sqlLocalAlim = "SELECT valor_decimal FROM configuracion_sistema WHERE clave = 'comision_local_alimentos_global' LIMIT 1";
            await using (var cmd = new NpgsqlCommand(sqlLocalAlim, conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    ComisionLocalAlimentosGlobal = Convert.ToDecimal(result);
            }
            ComisionLocalAlimentosGlobalTexto = ComisionLocalAlimentosGlobal.ToString("N2");

            // Pack Alimentos - Comision Allva
            var sqlAllvaAlim = "SELECT valor_decimal FROM configuracion_sistema WHERE clave = 'comision_allva_alimentos_global' LIMIT 1";
            await using (var cmd = new NpgsqlCommand(sqlAllvaAlim, conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    ComisionAllvaAlimentosGlobal = Convert.ToDecimal(result);
            }
            ComisionAllvaAlimentosGlobalTexto = ComisionAllvaAlimentosGlobal.ToString("N2");
            OnPropertyChanged(nameof(ComisionTotalAlimentosGlobalTexto));

            // Billetes
            var sqlBilletes = "SELECT valor_decimal FROM configuracion_sistema WHERE clave = 'comision_billetes_global' LIMIT 1";
            await using (var cmd = new NpgsqlCommand(sqlBilletes, conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    ComisionBilletesGlobal = Convert.ToDecimal(result);
            }
            ComisionBilletesGlobalTexto = ComisionBilletesGlobal.ToString("N2");

            // Viajes
            var sqlViajes = "SELECT valor_decimal FROM configuracion_sistema WHERE clave = 'comision_viajes_global' LIMIT 1";
            await using (var cmd = new NpgsqlCommand(sqlViajes, conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    ComisionViajesGlobal = Convert.ToDecimal(result);
            }
            ComisionViajesGlobalTexto = ComisionViajesGlobal.ToString("N2");
        }
        catch
        {
            ComisionLocalAlimentosGlobal = 2.00m;
            ComisionLocalAlimentosGlobalTexto = "2.00";
            ComisionAllvaAlimentosGlobal = 3.00m;
            ComisionAllvaAlimentosGlobalTexto = "3.00";
            ComisionBilletesGlobal = 3.00m;
            ComisionBilletesGlobalTexto = "3.00";
            ComisionViajesGlobal = 8.00m;
            ComisionViajesGlobalTexto = "8.00";
        }
    }

    private async Task CargarComerciosAsync()
    {
        _comercios.Clear();
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var checkColumnSql = "SELECT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'comercios' AND column_name = 'porcentaje_comision_divisas')";
            await using var checkCmd = new NpgsqlCommand(checkColumnSql, conn);
            var columnExists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);
            
            if (!columnExists)
            {
                var alterSql = "ALTER TABLE comercios ADD COLUMN porcentaje_comision_divisas NUMERIC(5,2) DEFAULT 0.00";
                await using var alterCmd = new NpgsqlCommand(alterSql, conn);
                await alterCmd.ExecuteNonQueryAsync();
            }

            var sql = @"SELECT c.id_comercio, c.nombre_comercio, c.nombre_srl, c.pais, c.activo,
                COALESCE(c.porcentaje_comision_divisas, 0) as margen,
                (SELECT COUNT(*) FROM locales l WHERE l.id_comercio = c.id_comercio) as cantidad_locales
                FROM comercios c ORDER BY c.nombre_comercio";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var comercio = new ComercioConDivisaModel
                {
                    IdComercio = reader.GetInt32(0),
                    NombreComercio = reader.GetString(1),
                    NombreSrl = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Pais = reader.GetString(3),
                    Activo = reader.GetBoolean(4),
                    MargenPorcentaje = reader.GetDecimal(5),
                    CantidadLocales = reader.GetInt32(6)
                };
                if (comercio.MargenPorcentaje == 0)
                    comercio.MargenPorcentaje = MargenGlobal;
                _comercios.Add(comercio);
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar comercios: {ex.Message}");
        }
    }

    private async Task CargarLocalesAsync()
    {
        _locales.Clear();
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var checkColumnSql = "SELECT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'locales' AND column_name = 'comision_divisas')";
            await using var checkCmd = new NpgsqlCommand(checkColumnSql, conn);
            var columnExists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);
            
            if (!columnExists)
            {
                var alterSql = "ALTER TABLE locales ADD COLUMN comision_divisas NUMERIC(5,2) DEFAULT 0.00";
                await using var alterCmd = new NpgsqlCommand(alterSql, conn);
                await alterCmd.ExecuteNonQueryAsync();
            }

            var sql = @"SELECT l.id_local, l.id_comercio, l.codigo_local, l.nombre_local, c.nombre_comercio, l.activo,
                COALESCE(l.comision_divisas, 0) as comision_local, COALESCE(c.porcentaje_comision_divisas, 0) as margen_comercio,
                COALESCE(l.modulo_divisas, false) as modulo_divisas
                FROM locales l INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                ORDER BY c.nombre_comercio, l.nombre_local";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var comisionLocal = reader.GetDecimal(6);
                var margenComercio = reader.GetDecimal(7);
                var moduloDivisas = reader.GetBoolean(8);

                decimal margenEfectivo;
                string estadoMargen;

                if (comisionLocal > 0) { margenEfectivo = comisionLocal; estadoMargen = "Propio"; }
                else if (margenComercio > 0) { margenEfectivo = margenComercio; estadoMargen = "Del comercio"; }
                else { margenEfectivo = MargenGlobal; estadoMargen = "Global"; }

                _locales.Add(new LocalConDivisaModel
                {
                    IdLocal = reader.GetInt32(0),
                    IdComercio = reader.GetInt32(1),
                    CodigoLocal = reader.GetString(2),
                    NombreLocal = reader.GetString(3),
                    NombreComercio = reader.GetString(4),
                    Activo = reader.GetBoolean(5),
                    ComisionDivisas = comisionLocal,
                    UsaComisionPropia = comisionLocal > 0,
                    MargenEfectivo = margenEfectivo,
                    EstadoMargen = estadoMargen,
                    ModuloDivisas = moduloDivisas
                });
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al cargar locales: {ex.Message}");
        }
    }

    // ============================================
    // CARGAR DATOS PACK ALIMENTOS (NUEVO)
    // ============================================

    private async Task CargarComerciosAlimentosAsync()
    {
        _comerciosAlimentos.Clear();
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = @"SELECT c.id_comercio, c.nombre_comercio, c.pais, c.activo,
                COALESCE(c.comision_local_alimentos, 0), COALESCE(c.comision_allva_alimentos, 0),
                (SELECT COUNT(*) FROM locales WHERE id_comercio = c.id_comercio)
                FROM comercios c ORDER BY c.nombre_comercio";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var comLocal = reader.GetDecimal(4);
                var comAllva = reader.GetDecimal(5);
                _comerciosAlimentos.Add(new ComercioConAlimentosModel
                {
                    IdComercio = reader.GetInt32(0),
                    NombreComercio = reader.GetString(1),
                    Pais = reader.GetString(2),
                    Activo = reader.GetBoolean(3),
                    ComisionLocal = comLocal > 0 ? comLocal : ComisionLocalAlimentosGlobal,
                    ComisionAllva = comAllva > 0 ? comAllva : ComisionAllvaAlimentosGlobal,
                    CantidadLocales = reader.GetInt32(6),
                    UsaComisionPropia = comLocal > 0 || comAllva > 0
                });
            }
        }
        catch (Exception ex) { MostrarMensaje($"Error comercios alimentos: {ex.Message}"); }
    }

    private async Task CargarLocalesAlimentosAsync()
    {
        _localesAlimentos.Clear();
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = @"SELECT l.id_local, l.id_comercio, l.codigo_local, l.nombre_local, c.nombre_comercio, l.activo,
                COALESCE(l.comision_local_alimentos, 0), COALESCE(l.comision_allva_alimentos, 0),
                COALESCE(c.comision_local_alimentos, 0), COALESCE(c.comision_allva_alimentos, 0)
                FROM locales l INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                ORDER BY c.nombre_comercio, l.nombre_local";

            await using var cmd = new NpgsqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var cll = reader.GetDecimal(6); var cal = reader.GetDecimal(7);
                var clc = reader.GetDecimal(8); var cac = reader.GetDecimal(9);
                
                decimal cle, cae; string estado;
                if (cll > 0 || cal > 0) 
                { 
                    cle = cll > 0 ? cll : ComisionLocalAlimentosGlobal; 
                    cae = cal > 0 ? cal : ComisionAllvaAlimentosGlobal; 
                    estado = "Propio"; 
                }
                else if (clc > 0 || cac > 0) 
                { 
                    cle = clc > 0 ? clc : ComisionLocalAlimentosGlobal; 
                    cae = cac > 0 ? cac : ComisionAllvaAlimentosGlobal; 
                    estado = "Del comercio"; 
                }
                else 
                { 
                    cle = ComisionLocalAlimentosGlobal; 
                    cae = ComisionAllvaAlimentosGlobal; 
                    estado = "Global"; 
                }

                _localesAlimentos.Add(new LocalConAlimentosModel
                {
                    IdLocal = reader.GetInt32(0),
                    IdComercio = reader.GetInt32(1),
                    CodigoLocal = reader.GetString(2),
                    NombreLocal = reader.GetString(3),
                    NombreComercio = reader.GetString(4),
                    Activo = reader.GetBoolean(5),
                    ComisionLocalPropia = cll,
                    ComisionAllvaPropia = cal,
                    ComisionLocalEfectiva = cle,
                    ComisionAllvaEfectiva = cae,
                    EstadoComision = estado
                });
            }
        }
        catch (Exception ex) { MostrarMensaje($"Error locales alimentos: {ex.Message}"); }
    }

    // ============================================
    // FILTROS
    // ============================================

    private void AplicarFiltro()
    {
        var filtro = TextoBusqueda?.ToLower().Trim() ?? "";

        if (EsVistaComercios)
        {
            ComerciosFiltrados.Clear();
            var comerciosFiltrados = string.IsNullOrWhiteSpace(filtro) ? _comercios : _comercios.Where(c => c.NombreComercio.ToLower().Contains(filtro));
            foreach (var comercio in comerciosFiltrados)
                ComerciosFiltrados.Add(comercio);
        }
        else
        {
            LocalesFiltrados.Clear();
            // Filtrar solo locales con módulo de divisas activo
            var localesConDivisas = _locales.Where(l => l.ModuloDivisas);

            if (string.IsNullOrWhiteSpace(filtro))
            {
                foreach (var local in localesConDivisas)
                    LocalesFiltrados.Add(local);
            }
            else
            {
                var localesPorCodigo = localesConDivisas.Where(l => l.CodigoLocal.ToLower().Contains(filtro)).ToList();
                if (localesPorCodigo.Any())
                {
                    foreach (var local in localesPorCodigo)
                        LocalesFiltrados.Add(local);
                }
                else
                {
                    foreach (var local in localesConDivisas.Where(l => l.NombreComercio.ToLower().Contains(filtro)))
                        LocalesFiltrados.Add(local);
                }
            }
        }
    }

    private void AplicarFiltroAlimentos()
    {
        var filtro = TextoBusquedaAlimentos?.ToLower().Trim() ?? "";

        if (EsVistaComerciosAlimentos)
        {
            ComerciosAlimentosFiltrados.Clear();
            var lista = string.IsNullOrWhiteSpace(filtro) ? _comerciosAlimentos : _comerciosAlimentos.Where(c => c.NombreComercio.ToLower().Contains(filtro));
            foreach (var item in lista)
                ComerciosAlimentosFiltrados.Add(item);
        }
        else
        {
            LocalesAlimentosFiltrados.Clear();
            if (string.IsNullOrWhiteSpace(filtro))
            {
                foreach (var local in _localesAlimentos)
                    LocalesAlimentosFiltrados.Add(local);
            }
            else
            {
                var porCodigo = _localesAlimentos.Where(l => l.CodigoLocal.ToLower().Contains(filtro)).ToList();
                if (porCodigo.Any())
                {
                    foreach (var local in porCodigo)
                        LocalesAlimentosFiltrados.Add(local);
                }
                else
                {
                    foreach (var local in _localesAlimentos.Where(l => l.NombreComercio.ToLower().Contains(filtro)))
                        LocalesAlimentosFiltrados.Add(local);
                }
            }
        }
    }

    partial void OnTextoBusquedaChanged(string value) => AplicarFiltro();
    partial void OnTextoBusquedaAlimentosChanged(string value) => AplicarFiltroAlimentos();
    
    partial void OnVistaActualChanged(string value)
    {
        OnPropertyChanged(nameof(EsVistaComercios));
        OnPropertyChanged(nameof(EsVistaLocales));
        TextoBusqueda = "";
        AplicarFiltro();
    }

    partial void OnVistaActualAlimentosChanged(string value)
    {
        OnPropertyChanged(nameof(EsVistaComerciosAlimentos));
        OnPropertyChanged(nameof(EsVistaLocalesAlimentos));
        TextoBusquedaAlimentos = "";
        AplicarFiltroAlimentos();
    }

    // ============================================
    // COMANDOS DIVISAS (ORIGINAL)
    // ============================================

    [RelayCommand]
    private void CambiarVista(string? vista)
    {
        if (string.IsNullOrEmpty(vista)) return;
        VistaActual = vista;
        MostrarPanelEdicion = false;
    }

    [RelayCommand]
    private void EditarMargenGlobal()
    {
        TipoEdicion = "GLOBAL";
        TituloPanelEdicion = "Editar Margen Global";
        NuevoMargenTexto = MargenGlobal.ToString("N2");
        ComercioSeleccionado = null;
        LocalSeleccionado = null;
        MostrarPanelEdicion = true;
    }

    [RelayCommand]
    private void SeleccionarComercio(ComercioConDivisaModel? comercio)
    {
        if (comercio == null) return;
        TipoEdicion = "COMERCIO";
        TituloPanelEdicion = $"Editar Margen: {comercio.NombreComercio}";
        NuevoMargenTexto = comercio.MargenPorcentaje.ToString("N2");
        ComercioSeleccionado = comercio;
        LocalSeleccionado = null;
        MostrarPanelEdicion = true;
    }

    [RelayCommand]
    private void SeleccionarLocal(LocalConDivisaModel? local)
    {
        if (local == null) return;
        TipoEdicion = "LOCAL";
        TituloPanelEdicion = $"Editar Margen: {local.NombreLocal}";
        NuevoMargenTexto = local.MargenEfectivo.ToString("N2");
        LocalSeleccionado = local;
        ComercioSeleccionado = null;
        MostrarPanelEdicion = true;
    }

    [RelayCommand]
    private void CancelarEdicion() => MostrarPanelEdicion = false;

    [RelayCommand]
    private async Task GuardarMargenAsync()
    {
        var textoLimpio = NuevoMargenTexto.Replace(",", ".");
        if (!decimal.TryParse(textoLimpio, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal nuevoMargen))
        {
            MostrarMensaje("El margen ingresado no es valido");
            return;
        }

        if (nuevoMargen < 0 || nuevoMargen > 100)
        {
            MostrarMensaje("El margen debe estar entre 0% y 100%");
            return;
        }

        Cargando = true;
        try
        {
            switch (TipoEdicion)
            {
                case "GLOBAL":
                    await GuardarMargenGlobalAsync(nuevoMargen);
                    MostrarMensaje($"Margen global actualizado a {nuevoMargen:N2}%");
                    break;
                case "COMERCIO":
                    await GuardarMargenComercioAsync(nuevoMargen);
                    MostrarMensaje($"Margen del comercio actualizado a {nuevoMargen:N2}%");
                    break;
                case "LOCAL":
                    await GuardarMargenLocalAsync(nuevoMargen);
                    MostrarMensaje($"Margen del local actualizado a {nuevoMargen:N2}%");
                    break;
                case "BILLETES_GLOBAL":
                    await GuardarComisionModuloAsync("comision_billetes_global", nuevoMargen, "Comision global para billetes de avion");
                    ComisionBilletesGlobal = nuevoMargen;
                    ComisionBilletesGlobalTexto = nuevoMargen.ToString("N2");
                    MostrarMensaje($"Comision de Billetes de Avion actualizada a {nuevoMargen:N2}%");
                    break;
                case "VIAJES_GLOBAL":
                    await GuardarComisionModuloAsync("comision_viajes_global", nuevoMargen, "Comision global para packs de viajes");
                    ComisionViajesGlobal = nuevoMargen;
                    ComisionViajesGlobalTexto = nuevoMargen.ToString("N2");
                    MostrarMensaje($"Comision de Packs de Viajes actualizada a {nuevoMargen:N2}%");
                    break;
            }

            if (TipoEdicion == "GLOBAL" || TipoEdicion == "COMERCIO" || TipoEdicion == "LOCAL")
                await CargarDatosAsync();
            MostrarPanelEdicion = false;
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error al guardar: {ex.Message}");
        }
        finally
        {
            Cargando = false;
        }
    }

    private async Task GuardarComisionModuloAsync(string clave, decimal valor, string descripcion)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        var sql = @"INSERT INTO configuracion_sistema (clave, valor_decimal, descripcion) VALUES (@clave, @valor, @descripcion)
            ON CONFLICT (clave) DO UPDATE SET valor_decimal = @valor, fecha_modificacion = CURRENT_TIMESTAMP";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("clave", clave);
        cmd.Parameters.AddWithValue("valor", valor);
        cmd.Parameters.AddWithValue("descripcion", descripcion);
        await cmd.ExecuteNonQueryAsync();
    }

    [RelayCommand]
    private void EditarComisionBilletes()
    {
        TipoEdicion = "BILLETES_GLOBAL";
        TituloPanelEdicion = "Editar Comision Billetes de Avion";
        NuevoMargenTexto = ComisionBilletesGlobal.ToString("N2");
        ComercioSeleccionado = null;
        LocalSeleccionado = null;
        MostrarPanelEdicion = true;
    }

    [RelayCommand]
    private void EditarComisionViajes()
    {
        TipoEdicion = "VIAJES_GLOBAL";
        TituloPanelEdicion = "Editar Comision Packs de Viajes";
        NuevoMargenTexto = ComisionViajesGlobal.ToString("N2");
        ComercioSeleccionado = null;
        LocalSeleccionado = null;
        MostrarPanelEdicion = true;
    }

    [RelayCommand]
    private async Task AplicarMargenGlobalATodosAsync()
    {
        Cargando = true;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using (var cmd = new NpgsqlCommand("UPDATE comercios SET porcentaje_comision_divisas = @margen", conn))
            {
                cmd.Parameters.AddWithValue("margen", MargenGlobal);
                await cmd.ExecuteNonQueryAsync();
            }
            await using (var cmd = new NpgsqlCommand("UPDATE locales SET comision_divisas = 0", conn))
                await cmd.ExecuteNonQueryAsync();
            await CargarDatosAsync();
            MostrarMensaje($"Margen {MargenGlobal:N2}% aplicado a todos");
        }
        catch (Exception ex) { MostrarMensaje($"Error: {ex.Message}"); }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private async Task RefrescarAsync() => await CargarDatosAsync();

    [RelayCommand]
    private async Task ResetearMargenComercioAsync()
    {
        if (ComercioSeleccionado == null) return;
        Cargando = true;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("UPDATE comercios SET porcentaje_comision_divisas = 0 WHERE id_comercio = @id", conn);
            cmd.Parameters.AddWithValue("id", ComercioSeleccionado.IdComercio);
            await cmd.ExecuteNonQueryAsync();
            await CargarDatosAsync();
            MostrarPanelEdicion = false;
            MostrarMensaje("El comercio ahora usa el margen global");
        }
        catch (Exception ex) { MostrarMensaje($"Error: {ex.Message}"); }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private async Task ResetearMargenLocalAsync()
    {
        if (LocalSeleccionado == null) return;
        Cargando = true;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("UPDATE locales SET comision_divisas = 0 WHERE id_local = @id", conn);
            cmd.Parameters.AddWithValue("id", LocalSeleccionado.IdLocal);
            await cmd.ExecuteNonQueryAsync();
            await CargarDatosAsync();
            MostrarPanelEdicion = false;
            MostrarMensaje("El local ahora usa el margen del comercio");
        }
        catch (Exception ex) { MostrarMensaje($"Error: {ex.Message}"); }
        finally { Cargando = false; }
    }

    private async Task GuardarMargenGlobalAsync(decimal nuevoMargen)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        var sql = @"INSERT INTO configuracion_sistema (clave, valor_decimal, descripcion) VALUES ('margen_divisas_global', @valor, 'Margen global para operaciones de divisas')
            ON CONFLICT (clave) DO UPDATE SET valor_decimal = @valor, fecha_modificacion = CURRENT_TIMESTAMP";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("valor", nuevoMargen);
        await cmd.ExecuteNonQueryAsync();

        await using var cmdComercios = new NpgsqlCommand("UPDATE comercios SET porcentaje_comision_divisas = 0", conn);
        await cmdComercios.ExecuteNonQueryAsync();
        await using var cmdLocales = new NpgsqlCommand("UPDATE locales SET comision_divisas = 0", conn);
        await cmdLocales.ExecuteNonQueryAsync();

        MargenGlobal = nuevoMargen;
        MargenGlobalTexto = nuevoMargen.ToString("N2");
    }

    private async Task GuardarMargenComercioAsync(decimal nuevoMargen)
    {
        if (ComercioSeleccionado == null) return;
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        // Actualizar comercio
        await using (var cmd = new NpgsqlCommand("UPDATE comercios SET porcentaje_comision_divisas = @margen WHERE id_comercio = @id", conn))
        {
            cmd.Parameters.AddWithValue("margen", nuevoMargen);
            cmd.Parameters.AddWithValue("id", ComercioSeleccionado.IdComercio);
            await cmd.ExecuteNonQueryAsync();
        }
        // Resetear comisiones de los locales del comercio para que hereden del comercio
        await using (var cmdLocales = new NpgsqlCommand("UPDATE locales SET comision_divisas = 0 WHERE id_comercio = @id", conn))
        {
            cmdLocales.Parameters.AddWithValue("id", ComercioSeleccionado.IdComercio);
            await cmdLocales.ExecuteNonQueryAsync();
        }
    }

    private async Task GuardarMargenLocalAsync(decimal nuevoMargen)
    {
        if (LocalSeleccionado == null) return;
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("UPDATE locales SET comision_divisas = @margen WHERE id_local = @id", conn);
        cmd.Parameters.AddWithValue("margen", nuevoMargen);
        cmd.Parameters.AddWithValue("id", LocalSeleccionado.IdLocal);
        await cmd.ExecuteNonQueryAsync();
    }

    // ============================================
    // COMANDOS PACK ALIMENTOS (NUEVO)
    // ============================================

    [RelayCommand]
    private void CambiarVistaAlimentos(string? vista)
    {
        if (string.IsNullOrEmpty(vista)) return;
        VistaActualAlimentos = vista;
        MostrarPanelEdicionAlimentos = false;
    }

    [RelayCommand]
    private void EditarComisionAlimentosGlobal()
    {
        TipoEdicionAlimentos = "GLOBAL";
        TituloPanelEdicionAlimentos = "Editar Comisiones Globales";
        NuevaComisionLocalTexto = ComisionLocalAlimentosGlobal.ToString("N2");
        NuevaComisionAllvaTexto = ComisionAllvaAlimentosGlobal.ToString("N2");
        ComercioAlimentosSeleccionado = null;
        LocalAlimentosSeleccionado = null;
        MostrarPanelEdicionAlimentos = true;
    }

    [RelayCommand]
    private void SeleccionarComercioAlimentos(ComercioConAlimentosModel? comercio)
    {
        if (comercio == null) return;
        TipoEdicionAlimentos = "COMERCIO";
        TituloPanelEdicionAlimentos = $"Editar Comisiones: {comercio.NombreComercio}";
        NuevaComisionLocalTexto = comercio.ComisionLocal.ToString("N2");
        NuevaComisionAllvaTexto = comercio.ComisionAllva.ToString("N2");
        ComercioAlimentosSeleccionado = comercio;
        LocalAlimentosSeleccionado = null;
        MostrarPanelEdicionAlimentos = true;
    }

    [RelayCommand]
    private void SeleccionarLocalAlimentos(LocalConAlimentosModel? local)
    {
        if (local == null) return;
        TipoEdicionAlimentos = "LOCAL";
        TituloPanelEdicionAlimentos = $"Editar Comisiones: {local.NombreLocal}";
        NuevaComisionLocalTexto = local.ComisionLocalEfectiva.ToString("N2");
        NuevaComisionAllvaTexto = local.ComisionAllvaEfectiva.ToString("N2");
        LocalAlimentosSeleccionado = local;
        ComercioAlimentosSeleccionado = null;
        MostrarPanelEdicionAlimentos = true;
    }

    [RelayCommand]
    private void CancelarEdicionAlimentos() => MostrarPanelEdicionAlimentos = false;

    [RelayCommand]
    private async Task GuardarComisionAlimentosAsync()
    {
        var txtL = NuevaComisionLocalTexto.Replace(",", ".");
        var txtA = NuevaComisionAllvaTexto.Replace(",", ".");

        if (!decimal.TryParse(txtL, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal comL))
        { MostrarMensaje("La comision del local no es valida"); return; }
        if (!decimal.TryParse(txtA, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal comA))
        { MostrarMensaje("La comision de Allva no es valida"); return; }
        if (comL < 0 || comA < 0) { MostrarMensaje("Las comisiones no pueden ser negativas"); return; }

        Cargando = true;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            switch (TipoEdicionAlimentos)
            {
                case "GLOBAL":
                    var sqlL = @"INSERT INTO configuracion_sistema(clave,valor_decimal,descripcion) VALUES('comision_local_alimentos_global',@v,'Comision local pack alimentos') ON CONFLICT(clave) DO UPDATE SET valor_decimal=@v,fecha_modificacion=CURRENT_TIMESTAMP";
                    await using (var c = new NpgsqlCommand(sqlL, conn)) { c.Parameters.AddWithValue("v", comL); await c.ExecuteNonQueryAsync(); }
                    var sqlA = @"INSERT INTO configuracion_sistema(clave,valor_decimal,descripcion) VALUES('comision_allva_alimentos_global',@v,'Comision Allva pack alimentos') ON CONFLICT(clave) DO UPDATE SET valor_decimal=@v,fecha_modificacion=CURRENT_TIMESTAMP";
                    await using (var c = new NpgsqlCommand(sqlA, conn)) { c.Parameters.AddWithValue("v", comA); await c.ExecuteNonQueryAsync(); }
                    await using (var c = new NpgsqlCommand("UPDATE comercios SET comision_local_alimentos=0, comision_allva_alimentos=0", conn)) await c.ExecuteNonQueryAsync();
                    await using (var c = new NpgsqlCommand("UPDATE locales SET comision_local_alimentos=0, comision_allva_alimentos=0", conn)) await c.ExecuteNonQueryAsync();
                    ComisionLocalAlimentosGlobal = comL; ComisionLocalAlimentosGlobalTexto = comL.ToString("N2");
                    ComisionAllvaAlimentosGlobal = comA; ComisionAllvaAlimentosGlobalTexto = comA.ToString("N2");
                    OnPropertyChanged(nameof(ComisionTotalAlimentosGlobalTexto));
                    MostrarMensaje($"Comisiones globales actualizadas: Local {comL:N2} EUR + Allva {comA:N2} EUR");
                    break;

                case "COMERCIO":
                    if (ComercioAlimentosSeleccionado != null)
                    {
                        // Actualizar comercio
                        await using (var cmd = new NpgsqlCommand("UPDATE comercios SET comision_local_alimentos=@l, comision_allva_alimentos=@a WHERE id_comercio=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("l", comL); cmd.Parameters.AddWithValue("a", comA);
                            cmd.Parameters.AddWithValue("id", ComercioAlimentosSeleccionado.IdComercio);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        // Resetear comisiones de los locales del comercio para que hereden del comercio
                        await using (var cmdLocales = new NpgsqlCommand("UPDATE locales SET comision_local_alimentos=0, comision_allva_alimentos=0 WHERE id_comercio=@id", conn))
                        {
                            cmdLocales.Parameters.AddWithValue("id", ComercioAlimentosSeleccionado.IdComercio);
                            await cmdLocales.ExecuteNonQueryAsync();
                        }
                        MostrarMensaje("Comisiones del comercio y sus locales actualizadas");
                    }
                    break;

                case "LOCAL":
                    if (LocalAlimentosSeleccionado != null)
                    {
                        await using var cmd = new NpgsqlCommand("UPDATE locales SET comision_local_alimentos=@l, comision_allva_alimentos=@a WHERE id_local=@id", conn);
                        cmd.Parameters.AddWithValue("l", comL); cmd.Parameters.AddWithValue("a", comA);
                        cmd.Parameters.AddWithValue("id", LocalAlimentosSeleccionado.IdLocal);
                        await cmd.ExecuteNonQueryAsync();
                        MostrarMensaje("Comisiones del local actualizadas");
                    }
                    break;
            }

            await CargarDatosAsync();
            MostrarPanelEdicionAlimentos = false;
        }
        catch (Exception ex) { MostrarMensaje($"Error: {ex.Message}"); }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private async Task AplicarComisionAlimentosGlobalATodosAsync()
    {
        Cargando = true;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using (var c = new NpgsqlCommand("UPDATE comercios SET comision_local_alimentos=@l, comision_allva_alimentos=@a", conn))
            {
                c.Parameters.AddWithValue("l", ComisionLocalAlimentosGlobal);
                c.Parameters.AddWithValue("a", ComisionAllvaAlimentosGlobal);
                await c.ExecuteNonQueryAsync();
            }
            await using (var c = new NpgsqlCommand("UPDATE locales SET comision_local_alimentos=0, comision_allva_alimentos=0", conn))
                await c.ExecuteNonQueryAsync();
            await CargarDatosAsync();
            MostrarMensaje($"Comisiones aplicadas: Local {ComisionLocalAlimentosGlobal:N2} EUR + Allva {ComisionAllvaAlimentosGlobal:N2} EUR");
        }
        catch (Exception ex) { MostrarMensaje($"Error: {ex.Message}"); }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private async Task ResetearComisionAlimentosComercioAsync()
    {
        if (ComercioAlimentosSeleccionado == null) return;
        Cargando = true;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var c = new NpgsqlCommand("UPDATE comercios SET comision_local_alimentos=0, comision_allva_alimentos=0 WHERE id_comercio=@id", conn);
            c.Parameters.AddWithValue("id", ComercioAlimentosSeleccionado.IdComercio);
            await c.ExecuteNonQueryAsync();
            await CargarDatosAsync();
            MostrarPanelEdicionAlimentos = false;
            MostrarMensaje("El comercio ahora usa las comisiones globales");
        }
        catch (Exception ex) { MostrarMensaje($"Error: {ex.Message}"); }
        finally { Cargando = false; }
    }

    [RelayCommand]
    private async Task ResetearComisionAlimentosLocalAsync()
    {
        if (LocalAlimentosSeleccionado == null) return;
        Cargando = true;
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var c = new NpgsqlCommand("UPDATE locales SET comision_local_alimentos=0, comision_allva_alimentos=0 WHERE id_local=@id", conn);
            c.Parameters.AddWithValue("id", LocalAlimentosSeleccionado.IdLocal);
            await c.ExecuteNonQueryAsync();
            await CargarDatosAsync();
            MostrarPanelEdicionAlimentos = false;
            MostrarMensaje("El local ahora usa las comisiones del comercio");
        }
        catch (Exception ex) { MostrarMensaje($"Error: {ex.Message}"); }
        finally { Cargando = false; }
    }

    // ============================================
    // UTILIDADES
    // ============================================

    private void MostrarMensaje(string mensaje)
    {
        MensajeExito = mensaje;
        MostrarMensajeExito = true;
        Task.Delay(3000).ContinueWith(_ => MostrarMensajeExito = false);
    }
}