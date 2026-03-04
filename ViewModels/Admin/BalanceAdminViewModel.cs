using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;

namespace Allva.Desktop.ViewModels.Admin
{
    public partial class BalanceAdminViewModel : ObservableObject
    {
        private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private string _successMessage = "";

        [ObservableProperty]
        private string _fechaActualTexto = "";

        [ObservableProperty]
        private bool _mostrarFiltros = true;

        [ObservableProperty]
        private bool _esPanelAlimentos = true;

        [ObservableProperty]
        private bool _esPanelBilletes = false;

        [ObservableProperty]
        private bool _esPanelViaje = false;

        [ObservableProperty]
        private string _tabAlimentosBackground = "#ffd966";

        [ObservableProperty]
        private string _tabAlimentosForeground = "#0b5394";

        [ObservableProperty]
        private string _tabBilletesBackground = "White";

        [ObservableProperty]
        private string _tabBilletesForeground = "#595959";

        [ObservableProperty]
        private string _tabViajeBackground = "White";

        [ObservableProperty]
        private string _tabViajeForeground = "#595959";

        [ObservableProperty]
        private string _fechaDesdeTexto = "";

        [ObservableProperty]
        private string _fechaHastaTexto = "";

        [ObservableProperty]
        private string _filtroNumeroOperacion = "";

        [ObservableProperty]
        private string _filtroNumeroOperacionGlobal = "";

        [ObservableProperty]
        private string _filtroPaisDestino = "";

        [ObservableProperty]
        private string _filtroEstado = "Todos";

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

        // Autocompletado país destino
        [ObservableProperty]
        private string _textoBusquedaPaisDestino = "";

        [ObservableProperty]
        private bool _mostrarListaPaises = false;

        public ObservableCollection<string> PaisesFiltrados { get; } = new();

        private List<string> _todosPaises = new();

        [ObservableProperty]
        private string _filtroComercioTexto = "";

        [ObservableProperty]
        private bool _mostrarSugerenciasComercio = false;

        public ObservableCollection<string> SugerenciasComercio { get; } = new();

        private List<ComercioItem> _todosLosComerciosData = new();
        private Dictionary<string, int> _comercioIdMap = new();

        [ObservableProperty]
        private string _filtroLocalTexto = "";

        [ObservableProperty]
        private bool _mostrarSugerenciasLocal = false;

        public ObservableCollection<string> SugerenciasLocal { get; } = new();

        private List<LocalItem> _todosLosLocalesData = new();
        private Dictionary<string, int> _localIdMap = new();

        public ObservableCollection<string> PaisesDestino { get; } = new() { "Todos" };

        public ObservableCollection<string> EstadosDisponibles { get; } = new() 
        { 
            "Todos", "PENDIENTE", "PAGADO", "ENVIADO", "ANULADO" 
        };

        public ObservableCollection<BalanceAlimentosItem> OperacionesAlimentos { get; } = new();

        [ObservableProperty]
        private string _totalOperacionesAlimentos = "0";

        [ObservableProperty]
        private string _totalImporteAlimentos = "0.00";

        [ObservableProperty]
        private string _totalDebidoAlimentos = "0.00";

        public string TotalDebidoAlimentosTexto => decimal.TryParse(TotalDebidoAlimentos, out decimal val) && val > 0 
            ? $"-{TotalDebidoAlimentos} EUR" 
            : "0.00 EUR";

        partial void OnTotalDebidoAlimentosChanged(string value)
        {
            OnPropertyChanged(nameof(TotalDebidoAlimentosTexto));
        }

        [ObservableProperty]
        private string _totalPendientes = "0";

        [ObservableProperty]
        private string _totalPagados = "0";

        [ObservableProperty]
        private string _totalEnviados = "0";

        [ObservableProperty]
        private string _totalAnulados = "0";

        public ObservableCollection<LocalBalanceItem> LocalesConDeuda { get; } = new();

        [ObservableProperty]
        private LocalBalanceItem? _localSeleccionadoDeposito;

        [ObservableProperty]
        private string _totalDeudaGlobal = "0.00";

        [ObservableProperty]
        private string _montoDeposito = "";

        [ObservableProperty]
        private string _operacionesAPagar = "0";

        [ObservableProperty]
        private string _montoAPagar = "0.00";

        [ObservableProperty]
        private string _excedente = "0.00";

        [ObservableProperty]
        private string _deudaRestante = "0.00";

        [ObservableProperty]
        private bool _mostrarPrevisualizacion = false;

        [ObservableProperty]
        private bool _mostrarVistaDeposito = false;

        [ObservableProperty]
        private bool _mostrarVistaHistorial = false;

        [ObservableProperty]
        private bool _mostrarPopupLocal = false;

        [ObservableProperty]
        private string _popupLocalCodigo = "";

        [ObservableProperty]
        private string _popupLocalComercio = "";

        [ObservableProperty]
        private string _popupLocalDeuda = "0.00 EUR";

        [ObservableProperty]
        private string _popupLocalBeneficio = "0.00 EUR";

        private int _popupLocalId = 0;

        // Panel agregar monto a favor
        [ObservableProperty]
        private bool _mostrarPanelAgregarAFavor = false;

        [ObservableProperty]
        private string _montoAgregarAFavor = "";

        [ObservableProperty]
        private string _totalPendienteLocalSeleccionado = "0.00";

        [ObservableProperty]
        private int _cantidadOpsPendientesLocal = 0;

        [ObservableProperty]
        private decimal _beneficioDisponibleLocal = 0;

        public string BeneficioDisponibleTexto => BeneficioDisponibleLocal > 0 
            ? $"{BeneficioDisponibleLocal:N2} EUR" 
            : "0.00 EUR";

        partial void OnBeneficioDisponibleLocalChanged(decimal value)
        {
            OnPropertyChanged(nameof(BeneficioDisponibleTexto));
        }

        public ObservableCollection<OperacionLocalItem> OperacionesLocalSeleccionado { get; } = new();
        public ObservableCollection<HistorialOperacionItem> HistorialLocalMes { get; } = new();

        private List<OperacionParaPago> _operacionesParaPago = new();

        public BalanceAdminViewModel()
        {
            InicializarFechas();
            _ = InicializarAsync();
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

        private string ObtenerDiaSemana(DayOfWeek dia) => dia switch
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

        private string ObtenerNombreMes(int mes)
        {
            string[] meses = { "", "enero", "febrero", "marzo", "abril", "mayo", "junio",
                              "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };
            return mes >= 1 && mes <= 12 ? meses[mes] : "";
        }

        private async Task InicializarAsync()
        {
            await CargarComerciosAsync();
            await CargarPaisesDestinoAsync();
            await CargarLocalesConDeudaAsync();
            await CargarOperacionesAlimentosAsync();
        }

        [RelayCommand]
        private void ToggleFiltros()
        {
            MostrarFiltros = !MostrarFiltros;
        }

        partial void OnFiltroComercioTextoChanged(string value)
        {
            FiltrarSugerenciasComercio(value);
            FiltroLocalTexto = "";
            SugerenciasLocal.Clear();
        }

        private bool _isFormattingFechaDesde = false;
        private bool _isFormattingFechaHasta = false;

        partial void OnFechaDesdeTextoChanged(string value)
        {
            if (_isFormattingFechaDesde) return;
            var formateado = FormatearFechaInput(value);
            if (formateado != value)
            {
                _isFormattingFechaDesde = true;
                FechaDesdeTexto = formateado;
                _isFormattingFechaDesde = false;
            }
        }

        partial void OnFechaHastaTextoChanged(string value)
        {
            if (_isFormattingFechaHasta) return;
            var formateado = FormatearFechaInput(value);
            if (formateado != value)
            {
                _isFormattingFechaHasta = true;
                FechaHastaTexto = formateado;
                _isFormattingFechaHasta = false;
            }
        }

        private string FormatearFechaInput(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return valor;
            
            var soloNumeros = new string(valor.Where(char.IsDigit).ToArray());
            
            if (soloNumeros.Length == 0) return "";
            if (soloNumeros.Length > 8) soloNumeros = soloNumeros.Substring(0, 8);
            
            if (soloNumeros.Length <= 2)
                return soloNumeros;
            else if (soloNumeros.Length <= 4)
                return soloNumeros.Substring(0, 2) + "/" + soloNumeros.Substring(2);
            else
                return soloNumeros.Substring(0, 2) + "/" + soloNumeros.Substring(2, 2) + "/" + soloNumeros.Substring(4);
        }

        private void FiltrarSugerenciasComercio(string texto)
        {
            SugerenciasComercio.Clear();
            if (string.IsNullOrWhiteSpace(texto)) { MostrarSugerenciasComercio = false; return; }

            var coincidencias = _todosLosComerciosData
                .Where(c => c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(10).Select(c => c.Nombre);

            foreach (var item in coincidencias) SugerenciasComercio.Add(item);
            MostrarSugerenciasComercio = SugerenciasComercio.Count > 0;
        }

        [RelayCommand]
        private void SeleccionarComercio(string? comercio)
        {
            if (string.IsNullOrEmpty(comercio)) return;
            FiltroComercioTexto = comercio;
            MostrarSugerenciasComercio = false;
        }

        partial void OnFiltroLocalTextoChanged(string value)
        {
            FiltrarSugerenciasLocal(value);
        }

        private void FiltrarSugerenciasLocal(string texto)
        {
            SugerenciasLocal.Clear();
            if (string.IsNullOrWhiteSpace(texto)) { MostrarSugerenciasLocal = false; return; }

            int? idComercioSeleccionado = null;
            if (!string.IsNullOrWhiteSpace(FiltroComercioTexto) && _comercioIdMap.ContainsKey(FiltroComercioTexto))
                idComercioSeleccionado = _comercioIdMap[FiltroComercioTexto];

            var coincidencias = _todosLosLocalesData
                .Where(l => (idComercioSeleccionado == null || l.IdComercio == idComercioSeleccionado) &&
                    l.Codigo.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(10).Select(l => l.Codigo);

            foreach (var item in coincidencias) SugerenciasLocal.Add(item);
            MostrarSugerenciasLocal = SugerenciasLocal.Count > 0;
        }

        [RelayCommand]
        private void SeleccionarLocal(string? local)
        {
            if (string.IsNullOrEmpty(local)) return;
            FiltroLocalTexto = local;
            MostrarSugerenciasLocal = false;
        }

        [RelayCommand]
        private void CerrarSugerencias()
        {
            MostrarSugerenciasComercio = false;
            MostrarSugerenciasLocal = false;
            MostrarListaPaises = false;
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
            await CargarOperacionesAlimentosAsync();
        }

        private async Task CargarComerciosAsync()
        {
            try
            {
                _todosLosComerciosData.Clear();
                _comercioIdMap.Clear();

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var sql = "SELECT id_comercio, nombre_comercio FROM comercios WHERE activo = true ORDER BY nombre_comercio";
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32(0);
                    var nombre = reader.GetString(1);
                    _todosLosComerciosData.Add(new ComercioItem { Id = id, Nombre = nombre });
                    _comercioIdMap[nombre] = id;
                }

                await CargarLocalesDataAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar comercios: {ex.Message}";
            }
        }

        private async Task CargarLocalesDataAsync()
        {
            try
            {
                _todosLosLocalesData.Clear();
                _localIdMap.Clear();

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var sql = @"SELECT l.id_local, l.codigo_local, COALESCE(l.nombre_local, l.codigo_local), l.id_comercio 
                           FROM locales l WHERE l.activo = true ORDER BY l.codigo_local";
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32(0);
                    var codigo = reader.GetString(1);
                    var nombre = reader.GetString(2);
                    var idComercio = reader.GetInt32(3);
                    _todosLosLocalesData.Add(new LocalItem { Id = id, Codigo = codigo, Nombre = nombre, IdComercio = idComercio });
                    _localIdMap[codigo] = id;
                }
            }
            catch { }
        }

        private async Task CargarPaisesDestinoAsync()
        {
            try
            {
                PaisesDestino.Clear();
                PaisesDestino.Add("Todos");
                _todosPaises.Clear();

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var sql = "SELECT DISTINCT pais_destino FROM operaciones_pack_alimentos WHERE pais_destino IS NOT NULL AND pais_destino != '' ORDER BY pais_destino";
                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var pais = reader.GetString(0);
                    PaisesDestino.Add(pais);
                    _todosPaises.Add(pais);
                }
            }
            catch { }
        }

        private async Task CargarLocalesConDeudaAsync()
        {
            try
            {
                LocalesConDeuda.Clear();

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT 
                        l.id_local,
                        l.codigo_local,
                        c.id_comercio,
                        c.nombre_comercio,
                        COUNT(CASE WHEN opa.estado_envio = 'PENDIENTE' THEN 1 END) as total_operaciones,
                        COALESCE(SUM(CASE WHEN opa.estado_envio = 'PENDIENTE' THEN o.importe_total ELSE 0 END), 0) as total_deuda,
                        COALESCE(l.beneficio_acumulado, 0) as beneficio
                    FROM locales l
                    INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                    LEFT JOIN operaciones o ON l.id_local = o.id_local AND o.modulo = 'PACK_ALIMENTOS'
                    LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                    WHERE l.activo = true
                    GROUP BY l.id_local, l.codigo_local, c.id_comercio, c.nombre_comercio, l.beneficio_acumulado
                    HAVING COUNT(CASE WHEN opa.estado_envio = 'PENDIENTE' THEN 1 END) > 0
                    ORDER BY total_deuda DESC";

                await using var cmd = new NpgsqlCommand(sql, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                decimal totalDeuda = 0;
                int index = 0;

                while (await reader.ReadAsync())
                {
                    var idLocal = reader.GetInt32(0);
                    var codigoLocal = reader.GetString(1);
                    var idComercio = reader.GetInt32(2);
                    var nombreComercio = reader.GetString(3);
                    var totalOps = reader.GetInt32(4);
                    var deuda = reader.GetDecimal(5);
                    var beneficio = reader.GetDecimal(6);

                    LocalesConDeuda.Add(new LocalBalanceItem
                    {
                        IdLocal = idLocal,
                        IdComercio = idComercio,
                        CodigoLocal = codigoLocal,
                        NombreComercio = nombreComercio,
                        TotalOperaciones = totalOps,
                        TotalDeuda = deuda,
                        DeudaTexto = $"{deuda:N2} EUR",
                        BeneficioAcumulado = beneficio,
                        BeneficioTexto = $"{beneficio:N2} EUR"
                    });

                    totalDeuda += deuda;
                    index++;
                }

                TotalDeudaGlobal = $"{totalDeuda:N2}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar locales con deuda: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SeleccionarLocalDeposito(LocalBalanceItem? local)
        {
            if (local == null) return;

            // Desmarcar todos los locales
            foreach (var l in LocalesConDeuda)
                l.EstaSeleccionado = false;

            // Marcar el seleccionado
            local.EstaSeleccionado = true;

            LocalSeleccionadoDeposito = local;
            MontoDeposito = "";
            MostrarPrevisualizacion = false;
            TotalPendienteLocalSeleccionado = $"{local.TotalDeuda:N2}";
            CantidadOpsPendientesLocal = local.TotalOperaciones;

            MostrarVistaDeposito = true;
            MostrarVistaHistorial = false;

            await CargarBeneficioLocalAsync(local.IdLocal);
            await CargarOperacionesParaPagoAsync(local.IdLocal);
        }

        private async Task CargarBeneficioLocalAsync(int idLocal)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var sql = "SELECT COALESCE(beneficio_acumulado, 0) FROM locales WHERE id_local = @idLocal";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("idLocal", idLocal);

                var resultado = await cmd.ExecuteScalarAsync();
                BeneficioDisponibleLocal = resultado != null && resultado != DBNull.Value 
                    ? Convert.ToDecimal(resultado) 
                    : 0;
            }
            catch
            {
                BeneficioDisponibleLocal = 0;
            }
        }

        private async Task CargarOperacionesParaPagoAsync(int idLocal)
        {
            try
            {
                _operacionesParaPago.Clear();
                OperacionesLocalSeleccionado.Clear();

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT o.id_operacion, o.fecha_operacion, o.hora_operacion, o.numero_operacion, 
                           opa.nombre_pack, o.importe_total
                    FROM operaciones o
                    INNER JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                    WHERE o.id_local = @idLocal AND o.modulo = 'PACK_ALIMENTOS' AND opa.estado_envio = 'PENDIENTE'
                    ORDER BY o.fecha_operacion ASC, o.hora_operacion ASC";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("idLocal", idLocal);

                await using var reader = await cmd.ExecuteReaderAsync();

                int index = 0;
                while (await reader.ReadAsync())
                {
                    var idOp = reader.GetInt32(0);
                    var fecha = reader.IsDBNull(1) ? DateTime.Today : reader.GetDateTime(1);
                    var hora = reader.IsDBNull(2) ? TimeSpan.Zero : reader.GetTimeSpan(2);
                    var numOp = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    var pack = reader.IsDBNull(4) ? "" : reader.GetString(4);
                    var importe = reader.IsDBNull(5) ? 0 : reader.GetDecimal(5);

                    _operacionesParaPago.Add(new OperacionParaPago
                    {
                        IdOperacion = idOp,
                        Fecha = fecha,
                        Importe = importe
                    });

                    OperacionesLocalSeleccionado.Add(new OperacionLocalItem
                    {
                        IdOperacion = idOp,
                        Fecha = fecha.ToString("dd/MM/yy"),
                        Hora = hora.ToString(@"hh\:mm"),
                        NumeroOperacion = numOp,
                        Descripcion = pack,
                        Importe = importe,
                        ImporteTexto = $"{importe:N2} EUR",
                        EstadoPago = "PENDIENTE",
                        EstadoPagoColor = "#ffc107",
                        BackgroundColor = index % 2 == 0 ? "White" : "#F5F5F5"
                    });
                    index++;
                }
            }
            catch { }
        }

        [RelayCommand]
        private async Task VerHistorialLocal()
        {
            if (LocalSeleccionadoDeposito == null) return;
            
            MostrarVistaDeposito = false;
            MostrarVistaHistorial = true;
            
            await CargarHistorialLocalMesAsync(LocalSeleccionadoDeposito.IdLocal);
        }

        [RelayCommand]
        private async Task AbrirDetalleLocal(BalanceAlimentosItem? operacion)
        {
            if (operacion == null) return;

            // Establecer valores desde la operación primero (siempre visibles)
            PopupLocalCodigo = operacion.CodigoLocal ?? "";
            PopupLocalComercio = operacion.Comercio ?? "";
            PopupLocalDeuda = "0.00 EUR";
            PopupLocalBeneficio = "0.00 EUR";
            _popupLocalId = 0;

            // Buscar info del local
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT
                        l.id_local,
                        l.codigo_local,
                        c.nombre_comercio,
                        COALESCE(SUM(CASE WHEN opa.estado_envio = 'PENDIENTE' THEN o.importe_total ELSE 0 END), 0) as deuda,
                        COALESCE(l.beneficio_acumulado, 0) as beneficio
                    FROM locales l
                    INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                    LEFT JOIN operaciones o ON l.id_local = o.id_local AND o.modulo = 'PACK_ALIMENTOS'
                    LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                    WHERE l.codigo_local = @codigo
                    GROUP BY l.id_local, l.codigo_local, c.nombre_comercio, l.beneficio_acumulado";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("codigo", operacion.CodigoLocal ?? "");

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    _popupLocalId = reader.GetInt32(0);
                    if (!reader.IsDBNull(1)) PopupLocalCodigo = reader.GetString(1);
                    if (!reader.IsDBNull(2)) PopupLocalComercio = reader.GetString(2);
                    var deuda = reader.GetDecimal(3);
                    var beneficio = reader.GetDecimal(4);
                    PopupLocalDeuda = $"{deuda:N2} EUR";
                    PopupLocalBeneficio = $"{beneficio:N2} EUR";
                }

                await reader.CloseAsync();
                await CargarHistorialLocalMesAsync(_popupLocalId);
            }
            catch { }

            // Siempre mostrar el popup
            MostrarPopupLocal = true;
        }

        [RelayCommand]
        private void CerrarPopupLocal()
        {
            MostrarPopupLocal = false;
            MostrarPanelAgregarAFavor = false;
            HistorialLocalMes.Clear();
        }

        [RelayCommand]
        private void AbrirAgregarAFavor()
        {
            MontoAgregarAFavor = "";
            MostrarPanelAgregarAFavor = true;
        }

        [RelayCommand]
        private void CancelarAgregarAFavor()
        {
            MostrarPanelAgregarAFavor = false;
            MontoAgregarAFavor = "";
        }

        [RelayCommand]
        private async Task ConfirmarAgregarAFavor()
        {
            if (_popupLocalId <= 0) return;

            // Parsear el monto
            var montoTexto = MontoAgregarAFavor.Replace(",", ".").Trim();
            if (!decimal.TryParse(montoTexto, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var monto) || monto <= 0)
            {
                ErrorMessage = "Por favor ingrese un monto válido mayor a 0";
                return;
            }

            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                // Sumar al beneficio_acumulado del local
                var sql = "UPDATE locales SET beneficio_acumulado = COALESCE(beneficio_acumulado, 0) + @monto WHERE id_local = @idLocal";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("monto", monto);
                cmd.Parameters.AddWithValue("idLocal", _popupLocalId);
                await cmd.ExecuteNonQueryAsync();

                // Actualizar el popup
                var sqlBeneficio = "SELECT COALESCE(beneficio_acumulado, 0) FROM locales WHERE id_local = @idLocal";
                await using var cmdBeneficio = new NpgsqlCommand(sqlBeneficio, conn);
                cmdBeneficio.Parameters.AddWithValue("idLocal", _popupLocalId);
                var nuevoBeneficio = await cmdBeneficio.ExecuteScalarAsync();
                if (nuevoBeneficio != null && nuevoBeneficio != DBNull.Value)
                {
                    PopupLocalBeneficio = $"{Convert.ToDecimal(nuevoBeneficio):N2} EUR";
                }

                // Cerrar panel y limpiar
                MostrarPanelAgregarAFavor = false;
                MontoAgregarAFavor = "";

                // Recargar datos
                await CargarLocalesConDeudaAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al agregar monto a favor: {ex.Message}";
            }
        }

        [RelayCommand]
        private void VolverVistaDeposito()
        {
            MostrarVistaHistorial = false;
            MostrarVistaDeposito = true;
        }

        private async Task CargarHistorialLocalMesAsync(int idLocal)
        {
            try
            {
                HistorialLocalMes.Clear();
                var hoy = ObtenerHoraEspana();
                var hace30Dias = hoy.AddDays(-30);

                var historialItems = new List<(DateTime FechaCompleta, HistorialOperacionItem Item)>();

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                // Cargar operaciones
                var sqlOperaciones = @"
                    SELECT o.id_operacion, o.fecha_operacion, o.hora_operacion, o.numero_operacion,
                           o.importe_total, opa.estado_envio
                    FROM operaciones o
                    INNER JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                    WHERE o.id_local = @idLocal AND o.modulo = 'PACK_ALIMENTOS'
                      AND o.fecha_operacion >= @fechaDesde";

                await using var cmdOps = new NpgsqlCommand(sqlOperaciones, conn);
                cmdOps.Parameters.AddWithValue("idLocal", idLocal);
                cmdOps.Parameters.AddWithValue("fechaDesde", hace30Dias.Date);

                await using var readerOps = await cmdOps.ExecuteReaderAsync();

                while (await readerOps.ReadAsync())
                {
                    var idOp = readerOps.GetInt32(0);
                    var fecha = readerOps.IsDBNull(1) ? DateTime.Today : readerOps.GetDateTime(1);
                    var hora = readerOps.IsDBNull(2) ? TimeSpan.Zero : readerOps.GetTimeSpan(2);
                    var numOp = readerOps.IsDBNull(3) ? "" : readerOps.GetString(3);
                    var importe = readerOps.IsDBNull(4) ? 0 : readerOps.GetDecimal(4);
                    var estado = readerOps.IsDBNull(5) ? "PENDIENTE" : readerOps.GetString(5).Trim().ToUpper();

                    var estadoColor = estado switch
                    {
                        "PAGADO" => "#17a2b8",
                        "ENVIADO" => "#28a745",
                        "ANULADO" => "#dc3545",
                        _ => "#ffc107"
                    };

                    var fechaCompleta = fecha.Add(hora);
                    historialItems.Add((fechaCompleta, new HistorialOperacionItem
                    {
                        IdOperacion = idOp,
                        Fecha = fecha.ToString("dd/MM/yy"),
                        Hora = hora.ToString(@"hh\:mm"),
                        NumeroOperacion = numOp,
                        Importe = importe,
                        ImporteTexto = $"{importe:N2} EUR",
                        Estado = estado,
                        EstadoColor = estadoColor,
                        EsDeposito = false,
                        Descripcion = "Operación Pack Alimentos"
                    }));
                }

                await readerOps.CloseAsync();

                // Cargar depósitos
                var sqlDepositos = @"
                    SELECT id_deposito, fecha_deposito, hora_deposito, monto_depositado, monto_aplicado,
                           cantidad_operaciones_pagadas, numeros_operaciones_pagadas, excedente
                    FROM depositos_pack_alimentos
                    WHERE id_local = @idLocal AND fecha_deposito >= @fechaDesde";

                await using var cmdDeps = new NpgsqlCommand(sqlDepositos, conn);
                cmdDeps.Parameters.AddWithValue("idLocal", idLocal);
                cmdDeps.Parameters.AddWithValue("fechaDesde", hace30Dias.Date);

                await using var readerDeps = await cmdDeps.ExecuteReaderAsync();

                while (await readerDeps.ReadAsync())
                {
                    var idDeposito = readerDeps.GetInt32(0);
                    var fecha = readerDeps.IsDBNull(1) ? DateTime.Today : readerDeps.GetDateTime(1);
                    var hora = readerDeps.IsDBNull(2) ? TimeSpan.Zero : readerDeps.GetTimeSpan(2);
                    var montoDepositado = readerDeps.IsDBNull(3) ? 0 : readerDeps.GetDecimal(3);
                    var montoAplicado = readerDeps.IsDBNull(4) ? 0 : readerDeps.GetDecimal(4);
                    var cantOps = readerDeps.IsDBNull(5) ? 0 : readerDeps.GetInt32(5);
                    var numerosOps = readerDeps.IsDBNull(6) ? "" : readerDeps.GetString(6);
                    var excedente = readerDeps.IsDBNull(7) ? 0 : readerDeps.GetDecimal(7);

                    var descripcion = $"Pagó {cantOps} op(s)";
                    if (excedente > 0)
                        descripcion += $" + {excedente:N2} EUR a favor";

                    var fechaCompleta = fecha.Add(hora);
                    historialItems.Add((fechaCompleta, new HistorialOperacionItem
                    {
                        IdOperacion = idDeposito,
                        Fecha = fecha.ToString("dd/MM/yy"),
                        Hora = hora.ToString(@"hh\:mm"),
                        NumeroOperacion = $"DEP-{idDeposito:D4}",
                        Importe = montoDepositado,
                        ImporteTexto = $"+{montoDepositado:N2} EUR",
                        Estado = "DEPOSITO",
                        EstadoColor = "#6f42c1", // Morado para depósitos
                        EsDeposito = true,
                        Descripcion = descripcion,
                        CantidadOperacionesPagadas = cantOps,
                        NumerosOperacionesPagadas = numerosOps
                    }));
                }

                // Ordenar por fecha descendente y agregar a la colección
                var itemsOrdenados = historialItems.OrderByDescending(x => x.FechaCompleta).ToList();
                int index = 0;
                foreach (var item in itemsOrdenados)
                {
                    item.Item.BackgroundColor = item.Item.EsDeposito
                        ? "#F3E5F5"  // Fondo morado claro para depósitos
                        : (index % 2 == 0 ? "White" : "#F5F5F5");
                    HistorialLocalMes.Add(item.Item);
                    index++;
                }
            }
            catch { }
        }

        [RelayCommand]
        private async Task AnularOperacion(HistorialOperacionItem? item)
        {
            if (item == null) return;

            try
            {
                IsLoading = true;

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                if (item.EsDeposito)
                {
                    // Anular depósito: revertir las operaciones pagadas a PENDIENTE y eliminar el depósito
                    await using var transaction = await conn.BeginTransactionAsync();
                    try
                    {
                        // Obtener los IDs de las operaciones pagadas por este depósito
                        var sqlGetOps = "SELECT ids_operaciones_pagadas, excedente FROM depositos_pack_alimentos WHERE id_deposito = @idDeposito";
                        await using var cmdGetOps = new NpgsqlCommand(sqlGetOps, conn, transaction);
                        cmdGetOps.Parameters.AddWithValue("idDeposito", item.IdOperacion);

                        string idsOperaciones = "";
                        decimal excedente = 0;
                        await using var reader = await cmdGetOps.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            idsOperaciones = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            excedente = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                        }
                        await reader.CloseAsync();

                        // Revertir las operaciones a PENDIENTE
                        if (!string.IsNullOrEmpty(idsOperaciones))
                        {
                            var ids = idsOperaciones.Split(',').Select(s => s.Trim()).Where(s => int.TryParse(s, out _)).ToArray();
                            foreach (var idStr in ids)
                            {
                                var idOp = int.Parse(idStr);
                                var sqlRevert = @"UPDATE operaciones_pack_alimentos
                                                  SET estado_envio = 'PENDIENTE'
                                                  WHERE id_operacion = @idOp";
                                await using var cmdRevert = new NpgsqlCommand(sqlRevert, conn, transaction);
                                cmdRevert.Parameters.AddWithValue("idOp", idOp);
                                await cmdRevert.ExecuteNonQueryAsync();

                                // Registrar en historial
                                try
                                {
                                    var queryHistorial = @"
                                        INSERT INTO historial_estados_pack_alimentos
                                            (id_operacion, estado_anterior, estado_nuevo, fecha_cambio, id_usuario, observaciones)
                                        VALUES
                                            (@id_operacion, 'PAGADO', 'PENDIENTE', CURRENT_TIMESTAMP, NULL, @observaciones)";

                                    await using var cmdHistorial = new NpgsqlCommand(queryHistorial, conn, transaction);
                                    cmdHistorial.Parameters.AddWithValue("@id_operacion", idOp);
                                    cmdHistorial.Parameters.AddWithValue("@observaciones", "Depósito anulado desde Balance Admin");
                                    await cmdHistorial.ExecuteNonQueryAsync();
                                }
                                catch
                                {
                                    // Si falla el historial, no es crítico
                                }
                            }
                        }

                        // Restar el excedente del beneficio_acumulado del local si había
                        if (excedente > 0 && _popupLocalId > 0)
                        {
                            var sqlBeneficio = @"UPDATE locales
                                                SET beneficio_acumulado = GREATEST(0, COALESCE(beneficio_acumulado, 0) - @excedente)
                                                WHERE id_local = @idLocal";
                            await using var cmdBeneficio = new NpgsqlCommand(sqlBeneficio, conn, transaction);
                            cmdBeneficio.Parameters.AddWithValue("excedente", excedente);
                            cmdBeneficio.Parameters.AddWithValue("idLocal", _popupLocalId);
                            await cmdBeneficio.ExecuteNonQueryAsync();
                        }

                        // Eliminar el depósito
                        var sqlDelete = "DELETE FROM depositos_pack_alimentos WHERE id_deposito = @idDeposito";
                        await using var cmdDelete = new NpgsqlCommand(sqlDelete, conn, transaction);
                        cmdDelete.Parameters.AddWithValue("idDeposito", item.IdOperacion);
                        await cmdDelete.ExecuteNonQueryAsync();

                        await transaction.CommitAsync();

                        SuccessMessage = $"Deposito anulado. {item.CantidadOperacionesPagadas} operacion(es) volvieron a PENDIENTE";
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                else
                {
                    // Anular operación individual (pago)
                    var sql = @"UPDATE operaciones_pack_alimentos
                                SET estado_envio = 'PENDIENTE'
                                WHERE id_operacion = @idOperacion AND estado_envio = 'PAGADO'";

                    await using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("idOperacion", item.IdOperacion);
                    var affected = await cmd.ExecuteNonQueryAsync();

                    if (affected == 0)
                    {
                        ErrorMessage = "No se pudo anular la operacion";
                        await Task.Delay(2000);
                        ErrorMessage = "";
                        return;
                    }

                    // Registrar en historial
                    try
                    {
                        var queryHistorial = @"
                            INSERT INTO historial_estados_pack_alimentos
                                (id_operacion, estado_anterior, estado_nuevo, fecha_cambio, id_usuario, observaciones)
                            VALUES
                                (@id_operacion, 'PAGADO', 'PENDIENTE', CURRENT_TIMESTAMP, NULL, @observaciones)";

                        await using var cmdHistorial = new NpgsqlCommand(queryHistorial, conn);
                        cmdHistorial.Parameters.AddWithValue("@id_operacion", item.IdOperacion);
                        cmdHistorial.Parameters.AddWithValue("@observaciones", "Pago anulado desde Balance Admin");
                        await cmdHistorial.ExecuteNonQueryAsync();
                    }
                    catch
                    {
                        // Si falla el historial, no es crítico
                    }

                    SuccessMessage = "Pago anulado correctamente. La operacion volvio a estado PENDIENTE";
                }

                await CargarLocalesConDeudaAsync();
                await CargarOperacionesAlimentosAsync();

                // Actualizar popup si está abierto
                if (MostrarPopupLocal && _popupLocalId > 0)
                {
                    await CargarHistorialLocalMesAsync(_popupLocalId);
                    // Actualizar deuda y beneficio del local en popup
                    await using var conn2 = new NpgsqlConnection(ConnectionString);
                    await conn2.OpenAsync();
                    var sqlLocal = @"SELECT
                                        COALESCE(SUM(CASE WHEN opa.estado_envio = 'PENDIENTE' THEN o.importe_total ELSE 0 END), 0),
                                        COALESCE(l.beneficio_acumulado, 0)
                                    FROM locales l
                                    LEFT JOIN operaciones o ON l.id_local = o.id_local AND o.modulo = 'PACK_ALIMENTOS'
                                    LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                                    WHERE l.id_local = @idLocal
                                    GROUP BY l.beneficio_acumulado";
                    await using var cmdLocal = new NpgsqlCommand(sqlLocal, conn2);
                    cmdLocal.Parameters.AddWithValue("idLocal", _popupLocalId);
                    await using var readerLocal = await cmdLocal.ExecuteReaderAsync();
                    if (await readerLocal.ReadAsync())
                    {
                        var deuda = readerLocal.GetDecimal(0);
                        var beneficio = readerLocal.GetDecimal(1);
                        PopupLocalDeuda = $"{deuda:N2} EUR";
                        PopupLocalBeneficio = $"{beneficio:N2} EUR";
                    }
                }

                if (LocalSeleccionadoDeposito != null)
                {
                    var idLocalActual = LocalSeleccionadoDeposito.IdLocal;
                    await CargarHistorialLocalMesAsync(idLocalActual);
                    await CargarOperacionesParaPagoAsync(idLocalActual);

                    var localActualizado = LocalesConDeuda.FirstOrDefault(l => l.IdLocal == idLocalActual);
                    if (localActualizado != null)
                    {
                        LocalSeleccionadoDeposito = localActualizado;
                        TotalPendienteLocalSeleccionado = $"{localActualizado.TotalDeuda:N2}";
                        CantidadOpsPendientesLocal = localActualizado.TotalOperaciones;
                    }
                }

                await Task.Delay(2500);
                SuccessMessage = "";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
                await Task.Delay(2000);
                ErrorMessage = "";
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnMontoDepositoChanged(string value)
        {
            CalcularOperacionesAPagar();
        }

        private void CalcularOperacionesAPagar()
        {
            if (string.IsNullOrWhiteSpace(MontoDeposito))
            {
                MostrarPrevisualizacion = false;
                OperacionesAPagar = "0";
                MontoAPagar = "0.00";
                Excedente = "0.00";
                DeudaRestante = TotalPendienteLocalSeleccionado;
                
                int idx = 0;
                foreach (var op in OperacionesLocalSeleccionado)
                {
                    op.EstadoPago = "PENDIENTE";
                    op.EstadoPagoColor = "#ffc107";
                    op.BackgroundColor = idx % 2 == 0 ? "White" : "#F5F5F5";
                    idx++;
                }
                return;
            }

            if (!decimal.TryParse(MontoDeposito.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
            {
                MostrarPrevisualizacion = false;
                return;
            }

            MostrarPrevisualizacion = true;

            decimal montoRestante = monto;
            decimal totalAPagar = 0;
            int count = 0;

            int index = 0;
            foreach (var opLocal in OperacionesLocalSeleccionado)
            {
                var opPago = _operacionesParaPago.FirstOrDefault(o => o.IdOperacion == opLocal.IdOperacion);
                if (opPago != null && montoRestante >= opPago.Importe)
                {
                    opLocal.EstadoPago = "SE PAGARA";
                    opLocal.EstadoPagoColor = "#28a745";
                    opLocal.BackgroundColor = "#C8E6C9";
                    montoRestante -= opPago.Importe;
                    totalAPagar += opPago.Importe;
                    count++;
                }
                else
                {
                    opLocal.EstadoPago = "PENDIENTE";
                    opLocal.EstadoPagoColor = "#ffc107";
                    opLocal.BackgroundColor = index % 2 == 0 ? "White" : "#F5F5F5";
                }
                index++;
            }

            OperacionesAPagar = count.ToString();
            MontoAPagar = $"{totalAPagar:N2}";

            decimal totalPendiente = _operacionesParaPago.Sum(o => o.Importe);
            decimal deudaRestante = totalPendiente - totalAPagar;

            decimal excedenteCalculado = monto - totalAPagar;
            if (excedenteCalculado > 0)
            {
                Excedente = $"{excedenteCalculado:N2}";
            }
            else
            {
                Excedente = "0.00";
            }
            DeudaRestante = $"{deudaRestante:N2}";
        }

        [RelayCommand]
        private async Task ConfirmarDeposito()
        {
            if (LocalSeleccionadoDeposito == null)
            {
                ErrorMessage = "Seleccione un local primero";
                await Task.Delay(2000);
                ErrorMessage = "";
                return;
            }

            if (!decimal.TryParse(MontoDeposito.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal monto) || monto <= 0)
            {
                ErrorMessage = "Ingrese un monto valido";
                await Task.Delay(2000);
                ErrorMessage = "";
                return;
            }

            if (!int.TryParse(OperacionesAPagar, out int cantOps) || cantOps == 0)
            {
                ErrorMessage = "El monto no alcanza para pagar ninguna operacion";
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

                await using var transaction = await conn.BeginTransactionAsync();

                try
                {
                    var ahora = ObtenerHoraEspana();
                    decimal totalPagado = 0;
                    int opsPagadas = 0;

                    decimal montoRestante = monto;
                    foreach (var op in _operacionesParaPago.OrderBy(o => o.Fecha))
                    {
                        if (montoRestante >= op.Importe)
                        {
                            var sqlUpdate = @"UPDATE operaciones_pack_alimentos
                                            SET estado_envio = 'PAGADO'
                                            WHERE id_operacion = @idOperacion";

                            await using var cmdUpdate = new NpgsqlCommand(sqlUpdate, conn, transaction);
                            cmdUpdate.Parameters.AddWithValue("idOperacion", op.IdOperacion);
                            await cmdUpdate.ExecuteNonQueryAsync();

                            // Registrar en historial
                            try
                            {
                                var queryHistorial = @"
                                    INSERT INTO historial_estados_pack_alimentos
                                        (id_operacion, estado_anterior, estado_nuevo, fecha_cambio, id_usuario, observaciones)
                                    VALUES
                                        (@id_operacion, 'PENDIENTE', 'PAGADO', CURRENT_TIMESTAMP, NULL, @observaciones)";

                                await using var cmdHistorial = new NpgsqlCommand(queryHistorial, conn, transaction);
                                cmdHistorial.Parameters.AddWithValue("@id_operacion", op.IdOperacion);
                                cmdHistorial.Parameters.AddWithValue("@observaciones", "Pago por depósito desde Balance Admin");
                                await cmdHistorial.ExecuteNonQueryAsync();
                            }
                            catch
                            {
                                // Si falla el historial, no es crítico
                            }

                            montoRestante -= op.Importe;
                            totalPagado += op.Importe;
                            opsPagadas++;
                        }
                        else break;
                    }

                    var idsOperaciones = string.Join(",", _operacionesParaPago
                        .OrderBy(o => o.Fecha)
                        .Take(opsPagadas)
                        .Select(o => o.IdOperacion.ToString()));
                    
                    var numerosOperaciones = string.Join(",", OperacionesLocalSeleccionado
                        .Where(o => o.EstadoPago == "SE PAGARA")
                        .Select(o => o.NumeroOperacion));

                    decimal excedenteCalc = monto > totalPagado ? monto - totalPagado : 0;
                    decimal deudaRestanteCalc = _operacionesParaPago.Sum(o => o.Importe) - totalPagado;

                    var sqlDeposito = @"INSERT INTO depositos_pack_alimentos 
                        (id_local, codigo_local, id_comercio, nombre_comercio,
                         monto_depositado, monto_aplicado, excedente, deuda_restante,
                         cantidad_operaciones_pagadas, ids_operaciones_pagadas, numeros_operaciones_pagadas,
                         fecha_deposito, hora_deposito, observaciones)
                        VALUES 
                        (@idLocal, @codigoLocal, @idComercio, @nombreComercio,
                         @montoDepositado, @montoAplicado, @excedente, @deudaRestante,
                         @cantidadOps, @idsOps, @numerosOps,
                         @fecha, @hora, @observaciones)";

                    await using var cmdDeposito = new NpgsqlCommand(sqlDeposito, conn, transaction);
                    cmdDeposito.Parameters.AddWithValue("idLocal", LocalSeleccionadoDeposito.IdLocal);
                    cmdDeposito.Parameters.AddWithValue("codigoLocal", LocalSeleccionadoDeposito.CodigoLocal);
                    cmdDeposito.Parameters.AddWithValue("idComercio", LocalSeleccionadoDeposito.IdComercio);
                    cmdDeposito.Parameters.AddWithValue("nombreComercio", LocalSeleccionadoDeposito.NombreComercio ?? "");
                    cmdDeposito.Parameters.AddWithValue("montoDepositado", monto);
                    cmdDeposito.Parameters.AddWithValue("montoAplicado", totalPagado);
                    cmdDeposito.Parameters.AddWithValue("excedente", excedenteCalc);
                    cmdDeposito.Parameters.AddWithValue("deudaRestante", deudaRestanteCalc > 0 ? deudaRestanteCalc : 0);
                    cmdDeposito.Parameters.AddWithValue("cantidadOps", opsPagadas);
                    cmdDeposito.Parameters.AddWithValue("idsOps", idsOperaciones);
                    cmdDeposito.Parameters.AddWithValue("numerosOps", numerosOperaciones);
                    cmdDeposito.Parameters.AddWithValue("fecha", ahora.Date);
                    cmdDeposito.Parameters.AddWithValue("hora", ahora.TimeOfDay);
                    cmdDeposito.Parameters.AddWithValue("observaciones", $"Deposito Pack Alimentos: {opsPagadas} operaciones pagadas");

                    await cmdDeposito.ExecuteNonQueryAsync();

                    if (excedenteCalc > 0)
                    {
                        var sqlBeneficio = @"UPDATE locales 
                                            SET beneficio_acumulado = COALESCE(beneficio_acumulado, 0) + @excedente 
                                            WHERE id_local = @idLocal";
                        await using var cmdBeneficio = new NpgsqlCommand(sqlBeneficio, conn, transaction);
                        cmdBeneficio.Parameters.AddWithValue("excedente", excedenteCalc);
                        cmdBeneficio.Parameters.AddWithValue("idLocal", LocalSeleccionadoDeposito.IdLocal);
                        await cmdBeneficio.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();

                    var mensaje = $"Deposito registrado: {opsPagadas} operaciones pagadas ({totalPagado:N2} EUR)";
                    if (excedenteCalc > 0) mensaje += $". Beneficio acumulado: {excedenteCalc:N2} EUR";

                    SuccessMessage = mensaje;

                    MontoDeposito = "";
                    MostrarPrevisualizacion = false;
                    LocalSeleccionadoDeposito = null;
                    OperacionesLocalSeleccionado.Clear();
                    _operacionesParaPago.Clear();
                    MostrarVistaDeposito = false;
                    MostrarVistaHistorial = false;

                    await CargarLocalesConDeudaAsync();
                    await CargarOperacionesAlimentosAsync();

                    await Task.Delay(3000);
                    SuccessMessage = "";
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al procesar deposito: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void CancelarDeposito()
        {
            MontoDeposito = "";
            MostrarPrevisualizacion = false;
            LocalSeleccionadoDeposito = null;
            OperacionesLocalSeleccionado.Clear();
            _operacionesParaPago.Clear();
            MostrarVistaDeposito = false;
            MostrarVistaHistorial = false;
        }

        [RelayCommand]
        private async Task BuscarOperaciones()
        {
            MostrarSugerenciasComercio = false;
            MostrarSugerenciasLocal = false;
            await CargarOperacionesAlimentosAsync();
        }

        [RelayCommand]
        private async Task LimpiarFiltros()
        {
            var hoy = ObtenerHoraEspana();
            FechaDesdeTexto = $"01/{hoy.Month:D2}/{hoy.Year}";
            FechaHastaTexto = $"{hoy.Day:D2}/{hoy.Month:D2}/{hoy.Year}";
            FiltroNumeroOperacionGlobal = "";
            FiltroNumeroOperacion = "";
            FiltroComercioTexto = "";
            FiltroLocalTexto = "";
            FiltroPaisDestino = "";
            TextoBusquedaPaisDestino = "";
            FiltroEstado = "Todos";
            MostrarSugerenciasComercio = false;
            MostrarSugerenciasLocal = false;
            MostrarListaPaises = false;
            await CargarOperacionesAlimentosAsync();
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
                try { return new DateTime(anio, mes, dia); }
                catch { return null; }
            }
            return null;
        }

        private async Task CargarOperacionesAlimentosAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = "";
                OperacionesAlimentos.Clear();

                var fechaDesde = ParsearFecha(FechaDesdeTexto);
                var fechaHasta = ParsearFecha(FechaHastaTexto);

                int? idComercio = !string.IsNullOrEmpty(FiltroComercioTexto) && _comercioIdMap.ContainsKey(FiltroComercioTexto)
                    ? _comercioIdMap[FiltroComercioTexto] : null;

                int? idLocal = null;
                if (!string.IsNullOrEmpty(FiltroLocalTexto))
                {
                    var localMatch = _localIdMap.Keys.FirstOrDefault(k => k.Equals(FiltroLocalTexto, StringComparison.OrdinalIgnoreCase));
                    if (localMatch != null) idLocal = _localIdMap[localMatch];
                }

                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();

                var sql = @"
                    SELECT 
                        o.id_operacion, o.fecha_operacion, o.hora_operacion, o.numero_operacion,
                        c.nombre_comercio, l.codigo_local, opa.pais_destino, opa.nombre_pack, o.importe_total,
                        CONCAT(cl.nombre, ' ', cl.apellidos) as cliente,
                        CONCAT(cb.nombre, ' ', cb.apellido) as beneficiario,
                        CONCAT_WS(', ', cb.calle, cb.numero, cb.ciudad) as direccion_beneficiario,
                        opa.estado_envio
                    FROM operaciones o
                    INNER JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                    INNER JOIN locales l ON o.id_local = l.id_local
                    INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                    LEFT JOIN clientes cl ON o.id_cliente = cl.id_cliente
                    LEFT JOIN clientes_beneficiarios cb ON opa.id_beneficiario = cb.id_beneficiario
                    WHERE o.modulo = 'PACK_ALIMENTOS'";

                if (fechaDesde.HasValue) sql += " AND o.fecha_operacion >= @fechaDesde";
                if (fechaHasta.HasValue) sql += " AND o.fecha_operacion <= @fechaHasta";
                if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionGlobal)) sql += " AND o.id_operacion::text ILIKE @numOpGlobal";
                if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacion)) sql += " AND o.numero_operacion ILIKE @numOp";
                if (!string.IsNullOrEmpty(FiltroPaisDestino)) sql += " AND opa.pais_destino ILIKE @pais";
                if (!string.IsNullOrEmpty(FiltroEstado) && FiltroEstado != "Todos") sql += " AND opa.estado_envio = @estado";
                if (idComercio.HasValue) sql += " AND l.id_comercio = @idComercio";
                if (idLocal.HasValue) sql += " AND o.id_local = @idLocal";

                // Ordenamiento dinámico
                var ordenDir = OrdenAscendente ? "ASC" : "DESC";
                sql += $" ORDER BY o.fecha_operacion {ordenDir}, o.hora_operacion {ordenDir} LIMIT 500";

                await using var cmd = new NpgsqlCommand(sql, conn);

                if (fechaDesde.HasValue) cmd.Parameters.AddWithValue("fechaDesde", fechaDesde.Value.Date);
                if (fechaHasta.HasValue) cmd.Parameters.AddWithValue("fechaHasta", fechaHasta.Value.Date.AddDays(1).AddSeconds(-1));
                if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacionGlobal)) cmd.Parameters.AddWithValue("numOpGlobal", $"%{FiltroNumeroOperacionGlobal}%");
                if (!string.IsNullOrWhiteSpace(FiltroNumeroOperacion)) cmd.Parameters.AddWithValue("numOp", $"%{FiltroNumeroOperacion}%");
                if (!string.IsNullOrEmpty(FiltroPaisDestino)) cmd.Parameters.AddWithValue("pais", $"%{FiltroPaisDestino}%");
                if (!string.IsNullOrEmpty(FiltroEstado) && FiltroEstado != "Todos") cmd.Parameters.AddWithValue("estado", FiltroEstado);
                if (idComercio.HasValue) cmd.Parameters.AddWithValue("idComercio", idComercio.Value);
                if (idLocal.HasValue) cmd.Parameters.AddWithValue("idLocal", idLocal.Value);

                await using var reader = await cmd.ExecuteReaderAsync();

                int index = 0;
                decimal totalImporte = 0, totalDebido = 0;
                int pendientes = 0, pagados = 0, enviados = 0, anulados = 0;

                while (await reader.ReadAsync())
                {
                    var idOperacion = reader.GetInt32(0);
                    var fechaOperacion = reader.IsDBNull(1) ? DateTime.Today : reader.GetDateTime(1);
                    var horaOperacion = reader.IsDBNull(2) ? TimeSpan.Zero : reader.GetTimeSpan(2);
                    var numOp = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    var comercio = reader.IsDBNull(4) ? "" : reader.GetString(4);
                    var codigoLocal = reader.IsDBNull(5) ? "" : reader.GetString(5);
                    var paisDestino = reader.IsDBNull(6) ? "" : reader.GetString(6);
                    var descripcion = reader.IsDBNull(7) ? "" : reader.GetString(7);
                    var importe = reader.IsDBNull(8) ? 0 : reader.GetDecimal(8);
                    var cliente = reader.IsDBNull(9) ? "" : reader.GetString(9);
                    var beneficiario = reader.IsDBNull(10) ? "" : reader.GetString(10);
                    var direccionBeneficiario = reader.IsDBNull(11) ? "" : reader.GetString(11);
                    var estado = reader.IsDBNull(12) ? "PENDIENTE" : reader.GetString(12);

                    var estadoColor = estado.Trim().ToUpper() switch
                    {
                        "PAGADO" => "#17a2b8",
                        "ENVIADO" => "#28a745",
                        "ANULADO" => "#dc3545",
                        _ => "#ffc107"
                    };

                    OperacionesAlimentos.Add(new BalanceAlimentosItem
                    {
                        Hora = horaOperacion.ToString(@"hh\:mm"),
                        Fecha = fechaOperacion.ToString("dd/MM/yy"),
                        NumeroOperacionGlobal = idOperacion.ToString(),
                        NumeroOperacion = numOp,
                        Comercio = comercio,
                        CodigoLocal = codigoLocal,
                        PaisDestino = paisDestino,
                        Descripcion = descripcion,
                        Importe = $"{importe:N2} EUR",
                        ImporteNum = importe,
                        Cliente = cliente,
                        Beneficiario = beneficiario,
                        DireccionBeneficiario = direccionBeneficiario,
                        Estado = estado.Trim().ToUpper(),
                        EstadoColor = estadoColor,
                        BackgroundColor = index % 2 == 0 ? "White" : "#F5F5F5"
                    });

                    totalImporte += importe;

                    switch (estado.Trim().ToUpper())
                    {
                        case "PENDIENTE": pendientes++; totalDebido += importe; break;
                        case "PAGADO": pagados++; break;
                        case "ENVIADO": enviados++; break;
                        case "ANULADO": anulados++; break;
                        default: pendientes++; totalDebido += importe; break;
                    }
                    index++;
                }

                TotalOperacionesAlimentos = index.ToString();
                TotalImporteAlimentos = $"{totalImporte:N2}";
                TotalDebidoAlimentos = $"{totalDebido:N2}";
                TotalPendientes = pendientes.ToString();
                TotalPagados = pagados.ToString();
                TotalEnviados = enviados.ToString();
                TotalAnulados = anulados.ToString();
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

        [RelayCommand]
        private async Task CambiarPanel(string? panel)
        {
            if (string.IsNullOrEmpty(panel)) return;

            EsPanelAlimentos = false; EsPanelBilletes = false; EsPanelViaje = false;
            TabAlimentosBackground = "White"; TabAlimentosForeground = "#595959";
            TabBilletesBackground = "White"; TabBilletesForeground = "#595959";
            TabViajeBackground = "White"; TabViajeForeground = "#595959";

            switch (panel.ToLower())
            {
                case "alimentos":
                    EsPanelAlimentos = true;
                    TabAlimentosBackground = "#ffd966"; TabAlimentosForeground = "#0b5394";
                    await CargarLocalesConDeudaAsync();
                    await CargarOperacionesAlimentosAsync();
                    break;
                case "billetes":
                    EsPanelBilletes = true;
                    TabBilletesBackground = "#ffd966"; TabBilletesForeground = "#0b5394";
                    break;
                case "viaje":
                    EsPanelViaje = true;
                    TabViajeBackground = "#ffd966"; TabViajeForeground = "#0b5394";
                    break;
            }
        }

        [RelayCommand]
        private async Task RefrescarDatos()
        {
            LocalSeleccionadoDeposito = null;
            MontoDeposito = "";
            MostrarPrevisualizacion = false;
            MostrarVistaDeposito = false;
            MostrarVistaHistorial = false;
            await CargarLocalesConDeudaAsync();
            await CargarOperacionesAlimentosAsync();
            SuccessMessage = "Datos actualizados";
            await Task.Delay(2000);
            SuccessMessage = "";
        }
    }

    public class BalanceAlimentosItem
    {
        public string Hora { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string NumeroOperacionGlobal { get; set; } = "";
        public string NumeroOperacion { get; set; } = "";
        public string Comercio { get; set; } = "";
        public string CodigoLocal { get; set; } = "";
        public string PaisDestino { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public string Importe { get; set; } = "0.00 EUR";
        public decimal ImporteNum { get; set; }
        public string Cliente { get; set; } = "";
        public string Beneficiario { get; set; } = "";
        public string DireccionBeneficiario { get; set; } = "";
        public string Estado { get; set; } = "PENDIENTE";
        public string EstadoColor { get; set; } = "#ffc107";
        public string BackgroundColor { get; set; } = "White";
    }

    public partial class LocalBalanceItem : ObservableObject
    {
        public int IdLocal { get; set; }
        public int IdComercio { get; set; }
        public string CodigoLocal { get; set; } = "";
        public string NombreComercio { get; set; } = "";
        public int TotalOperaciones { get; set; }
        public decimal TotalDeuda { get; set; }
        public string DeudaTexto { get; set; } = "0.00 EUR";
        public decimal BeneficioAcumulado { get; set; }
        public string BeneficioTexto { get; set; } = "0.00 EUR";
        public bool TieneBeneficio => BeneficioAcumulado > 0;

        [ObservableProperty]
        private bool _estaSeleccionado = false;

        public string BackgroundColor => EstaSeleccionado ? "#E3F2FD" : "White";
        public string BorderColor => EstaSeleccionado ? "#0b5394" : "#E0E0E0";

        partial void OnEstaSeleccionadoChanged(bool value)
        {
            OnPropertyChanged(nameof(BackgroundColor));
            OnPropertyChanged(nameof(BorderColor));
        }
    }

    public class OperacionParaPago
    {
        public int IdOperacion { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Importe { get; set; }
    }

    public partial class OperacionLocalItem : ObservableObject
    {
        public int IdOperacion { get; set; }
        public string Fecha { get; set; } = "";
        public string Hora { get; set; } = "";
        public string NumeroOperacion { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Importe { get; set; }
        public string ImporteTexto { get; set; } = "0.00 EUR";

        [ObservableProperty]
        private string _estadoPago = "PENDIENTE";

        [ObservableProperty]
        private string _estadoPagoColor = "#ffc107";

        [ObservableProperty]
        private string _backgroundColor = "White";
    }

    public class HistorialOperacionItem
    {
        public int IdOperacion { get; set; }
        public string Fecha { get; set; } = "";
        public string Hora { get; set; } = "";
        public string NumeroOperacion { get; set; } = "";
        public decimal Importe { get; set; }
        public string ImporteTexto { get; set; } = "0.00 EUR";
        public string Estado { get; set; } = "";
        public string EstadoColor { get; set; } = "#ffc107";
        public bool PuedeAnular => Estado == "PAGADO" || EsDeposito;
        public string BackgroundColor { get; set; } = "White";

        // Campos para diferenciar operaciones de depósitos
        public bool EsDeposito { get; set; } = false;
        public string TipoIcono => EsDeposito ? "💰" : "📦";
        public string Descripcion { get; set; } = "";
        public int CantidadOperacionesPagadas { get; set; }
        public string NumerosOperacionesPagadas { get; set; } = "";

        // Color del importe: morado para depósitos, verde para operaciones
        public string ImporteColor => EsDeposito ? "#6f42c1" : "#28a745";
    }
}