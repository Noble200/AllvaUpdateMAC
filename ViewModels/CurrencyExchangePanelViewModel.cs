using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Allva.Desktop.Models;
using Allva.Desktop.Services;
using Npgsql;
using Avalonia.Media.Imaging;

namespace Allva.Desktop.ViewModels;

public partial class CurrencyExchangePanelViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    
    private readonly CurrencyExchangeService _currencyService;
    
    private static readonly TimeZoneInfo _zonaHorariaEspana = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Romance Standard Time" : "Europe/Madrid");
    
    private decimal _margenGanancia = 10.00m;
    private int _idLocalActual = 0;
    private int _idComercioActual = 0;
    private int _idUsuarioActual = 0;
    private string? _nombreUsuarioActual;
    private string? _numeroUsuarioActual;

    // ═══════════════════════════════════════════════════════════
    // NAVEGACIÓN ENTRE VISTAS
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private bool _vistaPrincipal = true;
    [ObservableProperty] private bool _vistaBuscarCliente = false;
    [ObservableProperty] private bool _vistaNuevoCliente = false;
    [ObservableProperty] private bool _vistaConfirmacionCliente = false;
    [ObservableProperty] private bool _vistaTransaccion = false;
    [ObservableProperty] private bool _vistaConfirmacionTransaccion = false;
    [ObservableProperty] private bool _vistaResumen = false;
    [ObservableProperty] private bool _vistaEditarCliente = false;
    [ObservableProperty]
    private bool _mostrarSugerenciasDivisa = false;

    [ObservableProperty]
    private ObservableCollection<CurrencyModel> _divisasFiltradas = new();

    // ═══════════════════════════════════════════════════════════
    // CLIENTE
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private ClienteModel? _clienteSeleccionado;
    [ObservableProperty] private ObservableCollection<ClienteModel> _clientesEncontrados = new();
    [ObservableProperty] private string _busquedaCliente = string.Empty;
    
    public ObservableCollection<string> TiposBusquedaCliente { get; } = new()
    {
        "Todos", "Nombre", "Documento", "Telefono"
    };
    
    [ObservableProperty] private string _tipoBusquedaSeleccionado = "Todos";
    
    // ═══════════════════════════════════════════════════════════
    // ACUMULADOS DEL CLIENTE
    // ═══════════════════════════════════════════════════════════
    
    private const decimal LIMITE_TRIMESTRE = 3000.00m;
    
    [ObservableProperty] private decimal _acumuladoMes = 0;
    [ObservableProperty] private decimal _acumuladoTrimestre = 0;
    [ObservableProperty] private string _mesActualNombre = string.Empty;
    [ObservableProperty] private string _trimestreActualNombre = string.Empty;
    [ObservableProperty] private decimal _disponibleTrimestre = LIMITE_TRIMESTRE;
    [ObservableProperty] private bool _limiteTrimestreExcedido = false;
    [ObservableProperty] private bool _mostrarDisponibleTrimestre = true;
    
    public string ColorAcumuladoTrimestre => LimiteTrimestreExcedido ? "#FFEBEE" : (AcumuladoTrimestre > LIMITE_TRIMESTRE * 0.8m ? "#FFF3E0" : "#E8F5E9");
    public string ColorTextoAcumuladoTrimestre => LimiteTrimestreExcedido ? "#C62828" : (AcumuladoTrimestre > LIMITE_TRIMESTRE * 0.8m ? "#E65100" : "#2E7D32");

    // ═══════════════════════════════════════════════════════════
    // CAMPOS NUEVO/EDITAR CLIENTE
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private string _nuevoNombre = string.Empty;
    [ObservableProperty] private string _nuevoSegundoNombre = string.Empty;
    [ObservableProperty] private string _nuevoApellido = string.Empty;
    [ObservableProperty] private string _nuevoSegundoApellido = string.Empty;
    [ObservableProperty] private string _nuevoTelefono = string.Empty;
    [ObservableProperty] private string _nuevaDireccion = string.Empty;
    [ObservableProperty] private string _nuevaNacionalidad = string.Empty;
    [ObservableProperty] private string _nuevoTipoDocumento = "DNI";
    [ObservableProperty] private string _nuevoNumeroDocumento = string.Empty;
    
    private int _idClienteEditando = 0;
    [ObservableProperty] private bool _esModoEdicion = false;
    [ObservableProperty] private string _tituloFormularioCliente = "Registrar Nuevo Cliente";
    
    [ObservableProperty] private byte[]? _nuevaImagenDocumentoFrontal;
    [ObservableProperty] private byte[]? _nuevaImagenDocumentoTrasera;
    [ObservableProperty] private bool _tieneImagenDocumentoFrontal = false;
    [ObservableProperty] private bool _tieneImagenDocumentoTrasera = false;
    public bool TieneAlgunaImagenDocumento => TieneImagenDocumentoFrontal || TieneImagenDocumentoTrasera;
    
    [ObservableProperty] private Bitmap? _imagenDocumentoFrontalPreview;
    [ObservableProperty] private Bitmap? _imagenDocumentoTraseraPreview;
    [ObservableProperty] private bool _mostrarPreviewImagen = false;
    [ObservableProperty] private Bitmap? _imagenPreviewActual;
    [ObservableProperty] private string _tituloPreviewImagen = string.Empty;
    
    public string NuevoNombreCompletoPreview
    {
        get
        {
            var partes = new[] { NuevoNombre, NuevoSegundoNombre, NuevoApellido, NuevoSegundoApellido };
            return string.Join(" ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
    }
    
    [ObservableProperty] private string _nuevaFechaNacimientoTexto = string.Empty;
    [ObservableProperty] private string _nuevaCaducidadDocumentoTexto = string.Empty;

    public bool TieneImagenesDocumento => TieneImagenDocumentoFrontal || TieneImagenDocumentoTrasera;
    [ObservableProperty]
    private ObservableCollection<string> _tiposDocumento = new() { "DNI", "NIE", "Pasaporte" };
    public ObservableCollection<string> TiposResidencia { get; } = new() { "España", "Extranjero" };

    [ObservableProperty] 
    private string _nuevoTipoResidencia = string.Empty;

    partial void OnNuevoTipoResidenciaChanged(string value)
    {
        if (value == "España")
        {
            TiposDocumento = new ObservableCollection<string> { "DNI", "NIE" };
            if (NuevoTipoDocumento == "Pasaporte")
                NuevoTipoDocumento = "DNI";
        }
        else if (value == "Extranjero")
        {
            // Extranjero solo permite Pasaporte
            TiposDocumento = new ObservableCollection<string> { "Pasaporte" };
            NuevoTipoDocumento = "Pasaporte";
        }
        else
        {
            TiposDocumento = new ObservableCollection<string> { "DNI", "NIE", "Pasaporte" };
        }
    }

    // ═══════════════════════════════════════════════════════════
    // DIVISAS
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private ObservableCollection<FavoriteCurrencyModel> _favoriteCurrencies = new();
    [ObservableProperty] private ObservableCollection<CurrencyModel> _availableCurrencies = new();
    [ObservableProperty] private ObservableCollection<CurrencyModel> _divisasBusqueda = new();
    [ObservableProperty] private string _busquedaDivisa = string.Empty;
    [ObservableProperty] private string _busquedaDivisaCalculadora = string.Empty;
    [ObservableProperty] private CurrencyModel? _selectedCurrency;
    [ObservableProperty] private bool _mostrarSelectorDivisas = false;

    // ═══════════════════════════════════════════════════════════
    // CALCULADORA
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private string _amountReceived = string.Empty;
    [ObservableProperty] private string _totalInEurosTexto = string.Empty;
    [ObservableProperty] private decimal _totalInEuros;
    [ObservableProperty] private decimal _currentRate;
    [ObservableProperty] private string _rateDisplayText = string.Empty;
    
    private bool _calculandoDesdeDivisa = false;
    private bool _calculandoDesdeEuros = false;

    // ═══════════════════════════════════════════════════════════
    // OPERACIÓN / TRANSACCIÓN
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private long _numeroOperacion = 0;
    [ObservableProperty] private string _numeroOperacionDisplay = string.Empty;
    [ObservableProperty] private DateTime _fechaOperacion;
    
    private bool _operacionGuardada = false;

    // ═══════════════════════════════════════════════════════════
    // ESTADOS
    // ═══════════════════════════════════════════════════════════
    
    [ObservableProperty] private bool _isLoading = false;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _lastUpdateText = string.Empty;
    [ObservableProperty] private bool _puedeRealizarOperacion = false;
    public bool MostrarAvisoHacienda => TotalInEuros >= 1000;

    private List<string> _favoriteCodes = new() { "USD", "CAD", "AUD", "CHF", "GBP", "HKD" };

    // ═══════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════

    public CurrencyExchangePanelViewModel()
    {
        _currencyService = new CurrencyExchangeService();
        InitializeAsync();
    }
    
    public CurrencyExchangePanelViewModel(int idLocal, int idComercio)
    {
        _idLocalActual = idLocal;
        _idComercioActual = idComercio;
        _currencyService = new CurrencyExchangeService();
        InitializeAsync();
    }
    
    public CurrencyExchangePanelViewModel(int idLocal, int idComercio, int idUsuario, string nombreUsuario, string numeroUsuario)
    {
        _idLocalActual = idLocal;
        _idComercioActual = idComercio;
        _idUsuarioActual = idUsuario;
        _nombreUsuarioActual = nombreUsuario;
        _numeroUsuarioActual = numeroUsuario;
        _currencyService = new CurrencyExchangeService();
        InitializeAsync();
    }
    
    public void SetSesionData(int idLocal, int idComercio, int idUsuario, string nombreUsuario, string numeroUsuario)
    {
        _idLocalActual = idLocal;
        _idComercioActual = idComercio;
        _idUsuarioActual = idUsuario;
        _nombreUsuarioActual = nombreUsuario;
        _numeroUsuarioActual = numeroUsuario;
    }

    private DateTime ObtenerHoraEspana()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _zonaHorariaEspana);
    }

    private async void InitializeAsync()
    {
        ActualizarNombresMesYTrimestre();
        await CargarMargenGananciaAsync();
        await LoadDataAsync();
    }
    
    private void ActualizarNombresMesYTrimestre()
    {
        var ahora = ObtenerHoraEspana();
        var cultura = new CultureInfo("es-ES");
        MesActualNombre = cultura.DateTimeFormat.GetMonthName(ahora.Month).ToUpper() + " " + ahora.Year;
        int trimestre = (ahora.Month - 1) / 3 + 1;
        TrimestreActualNombre = $"T{trimestre} {ahora.Year}";
    }

    // ═══════════════════════════════════════════════════════════
    // NAVEGACIÓN
    // ═══════════════════════════════════════════════════════════
    
    private void OcultarTodasLasVistas()
    {
        VistaPrincipal = false;
        VistaBuscarCliente = false;
        VistaNuevoCliente = false;
        VistaConfirmacionCliente = false;
        VistaTransaccion = false;
        VistaConfirmacionTransaccion = false;
        VistaResumen = false;
        VistaEditarCliente = false;
    }
    
    [RelayCommand]
    private async Task IrABuscarClienteAsync()
    {
        OcultarTodasLasVistas();
        ClientesEncontrados.Clear();
        ErrorMessage = string.Empty;
        VistaBuscarCliente = true;
        
        if (!string.IsNullOrWhiteSpace(BusquedaCliente))
            await BuscarClientesAsync(BusquedaCliente);
    }
    
    [RelayCommand]
    private void IrANuevoCliente()
    {
        OcultarTodasLasVistas();
        LimpiarFormularioCliente();
        EsModoEdicion = false;
        TituloFormularioCliente = "Registrar Nuevo Cliente";
        ErrorMessage = string.Empty;
        VistaNuevoCliente = true;
    }
    
    [RelayCommand]
    private void IrAEditarCliente()
    {
        if (ClienteSeleccionado == null) return;
        OcultarTodasLasVistas();
        CargarDatosClienteParaEdicion();
        EsModoEdicion = true;
        TituloFormularioCliente = "Editar Cliente";
        ErrorMessage = string.Empty;
        VistaEditarCliente = true;
    }
    
    private async void CargarDatosClienteParaEdicion()
    {
        if (ClienteSeleccionado == null) return;

        _idClienteEditando = ClienteSeleccionado.IdCliente;
        NuevoNombre = ClienteSeleccionado.Nombre ?? "";
        NuevoSegundoNombre = ClienteSeleccionado.SegundoNombre ?? "";
        NuevoApellido = ClienteSeleccionado.Apellido ?? "";
        NuevoSegundoApellido = ClienteSeleccionado.SegundoApellido ?? "";
        NuevoTelefono = ClienteSeleccionado.Telefono ?? "";
        NuevaDireccion = ClienteSeleccionado.Direccion ?? "";
        NuevaNacionalidad = ClienteSeleccionado.Nacionalidad ?? "";
        NuevoTipoDocumento = ClienteSeleccionado.TipoDocumento ?? "DNI";
        NuevoNumeroDocumento = ClienteSeleccionado.NumeroDocumento ?? "";
        NuevoTipoResidencia = ClienteSeleccionado.TipoResidencia ?? "";
        NuevaFechaNacimientoTexto = ClienteSeleccionado.FechaNacimiento?.ToString("dd/MM/yyyy") ?? "";
        NuevaCaducidadDocumentoTexto = ClienteSeleccionado.CaducidadDocumento?.ToString("dd/MM/yyyy") ?? "";

        NuevaImagenDocumentoFrontal = null;
        NuevaImagenDocumentoTrasera = null;
        TieneImagenDocumentoFrontal = false;
        TieneImagenDocumentoTrasera = false;
        ImagenDocumentoFrontalPreview = null;
        ImagenDocumentoTraseraPreview = null;

        // Cargar imagenes desde carpeta local
        var imagenFrontal = await DocumentImageService.CargarImagenFrontalAsync(_idClienteEditando);
        if (imagenFrontal != null && imagenFrontal.Length > 0)
        {
            NuevaImagenDocumentoFrontal = imagenFrontal;
            TieneImagenDocumentoFrontal = true;
            try
            {
                using var stream = new MemoryStream(imagenFrontal);
                ImagenDocumentoFrontalPreview = new Bitmap(stream);
            }
            catch { }
        }

        var imagenTrasera = await DocumentImageService.CargarImagenTraseraAsync(_idClienteEditando);
        if (imagenTrasera != null && imagenTrasera.Length > 0)
        {
            NuevaImagenDocumentoTrasera = imagenTrasera;
            TieneImagenDocumentoTrasera = true;
            try
            {
                using var stream = new MemoryStream(imagenTrasera);
                ImagenDocumentoTraseraPreview = new Bitmap(stream);
            }
            catch { }
        }

        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
    }
    
    [RelayCommand]
    private void VolverAPrincipal()
    {
        OcultarTodasLasVistas();
        ErrorMessage = string.Empty;
        VistaPrincipal = true;
    }
    
    [RelayCommand]
    private void VolverABuscarCliente()
    {
        OcultarTodasLasVistas();
        ErrorMessage = string.Empty;
        VistaBuscarCliente = true;
    }
    
    [RelayCommand]
    private void VolverANuevoCliente()
    {
        OcultarTodasLasVistas();
        ErrorMessage = string.Empty;
        VistaNuevoCliente = true;
    }
    
    [RelayCommand]
    private void VolverATransaccion()
    {
        OcultarTodasLasVistas();
        ErrorMessage = string.Empty;
        VistaTransaccion = true;
    }
    
    [RelayCommand]
    private void VolverDeEdicionATransaccion()
    {
        OcultarTodasLasVistas();
        ErrorMessage = string.Empty;
        VistaTransaccion = true;
    }
    
    [RelayCommand]
    private void MostrarConfirmacionCliente()
    {
        if (string.IsNullOrWhiteSpace(NuevoNombre))
        {
            ErrorMessage = "El primer nombre es obligatorio";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevoApellido))
        {
            ErrorMessage = "El primer apellido es obligatorio";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevaDireccion))
        {
            ErrorMessage = "La direccion u hospedaje es obligatorio";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevoTipoDocumento))
        {
            ErrorMessage = "El tipo de documento es obligatorio";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevoNumeroDocumento))
        {
            ErrorMessage = "El numero de documento es obligatorio";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevoTipoResidencia))
        {
            ErrorMessage = "Debe seleccionar si es España o Extranjero";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevaNacionalidad))
        {
            ErrorMessage = "La nacionalidad es obligatoria";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevaFechaNacimientoTexto))
        {
            ErrorMessage = "La fecha de nacimiento es obligatoria";
            return;
        }
        if (!DateTime.TryParseExact(NuevaFechaNacimientoTexto, "dd/MM/yyyy", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaNac))
        {
            ErrorMessage = "Formato de fecha de nacimiento invalido. Use dd/mm/aaaa";
            return;
        }
        var hoy = ObtenerHoraEspana().Date;
        var edad = hoy.Year - fechaNac.Year;
        if (fechaNac.Date > hoy.AddYears(-edad)) edad--;
        if (edad < 18)
        {
            ErrorMessage = "El cliente debe ser mayor de edad (18 anos minimo)";
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevaCaducidadDocumentoTexto))
        {
            ErrorMessage = "La fecha de caducidad del documento es obligatoria";
            return;
        }
        if (!DateTime.TryParseExact(NuevaCaducidadDocumentoTexto, "dd/MM/yyyy", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaCad))
        {
            ErrorMessage = "Formato de fecha de caducidad invalido. Use dd/mm/aaaa";
            return;
        }
        if (fechaCad.Date < ObtenerHoraEspana().Date)
        {
            ErrorMessage = "El documento esta vencido. No puede realizar operaciones.";
            return;
        }
        if (!TieneImagenDocumentoFrontal || NuevaImagenDocumentoFrontal == null)
        {
            ErrorMessage = "La imagen frontal del documento es obligatoria";
            return;
        }
        if (!TieneImagenDocumentoTrasera || NuevaImagenDocumentoTrasera == null)
        {
            ErrorMessage = "La imagen trasera del documento es obligatoria";
            return;
        }
        
        ErrorMessage = string.Empty;
        OcultarTodasLasVistas();
        OnPropertyChanged(nameof(NuevoNombreCompletoPreview));
        VistaConfirmacionCliente = true;
    }
    
    /// <summary>
    /// FINALIZAR: Guarda la operación en BD y vuelve a principal
    /// </summary>
    [RelayCommand]
    private async Task FinalizarYVolverAPrincipalAsync()
    {
        if (_operacionGuardada)
        {
            LimpiarDatosOperacion();
            OcultarTodasLasVistas();
            VistaPrincipal = true;
            return;
        }
        
        IsLoading = true;
        ErrorMessage = string.Empty;
        
        try
        {
            var (exito, mensaje) = await GuardarOperacionCompletaAsync();
            
            if (exito)
            {
                _operacionGuardada = true;
                LimpiarDatosOperacion();
                OcultarTodasLasVistas();
                VistaPrincipal = true;
            }
            else
            {
                ErrorMessage = mensaje;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error inesperado: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private void LimpiarDatosOperacion()
    {
        ClienteSeleccionado = null;
        AmountReceived = string.Empty;
        TotalInEurosTexto = string.Empty;
        TotalInEuros = 0;
        NumeroOperacion = 0;
        NumeroOperacionDisplay = string.Empty;
        _operacionGuardada = false;
    }

    // ═══════════════════════════════════════════════════════════
    // ACUMULADOS DEL CLIENTE
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private async Task ActualizarAcumuladosAsync()
    {
        if (ClienteSeleccionado == null) return;
        await CargarAcumuladosClienteAsync(ClienteSeleccionado.IdCliente);
    }
    
    private async Task CargarAcumuladosClienteAsync(int idCliente)
    {
        try
        {
            IsLoading = true;
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            var ahora = ObtenerHoraEspana();
            var primerDiaMes = new DateTime(ahora.Year, ahora.Month, 1);
            int trimestre = (ahora.Month - 1) / 3;
            var primerDiaTrimestre = new DateTime(ahora.Year, trimestre * 3 + 1, 1);
            
            var sqlMes = @"SELECT COALESCE(SUM(importe_total), 0) 
                           FROM operaciones 
                           WHERE id_cliente = @idCliente 
                             AND modulo = 'DIVISAS'
                             AND estado = 'COMPLETADA'
                             AND fecha_operacion >= @fechaInicio";
            
            await using var cmdMes = new NpgsqlCommand(sqlMes, conn);
            cmdMes.Parameters.AddWithValue("idCliente", idCliente);
            cmdMes.Parameters.AddWithValue("fechaInicio", primerDiaMes);
            AcumuladoMes = Convert.ToDecimal(await cmdMes.ExecuteScalarAsync() ?? 0);
            
            var sqlTrimestre = @"SELECT COALESCE(SUM(importe_total), 0) 
                                 FROM operaciones 
                                 WHERE id_cliente = @idCliente 
                                   AND modulo = 'DIVISAS'
                                   AND estado = 'COMPLETADA'
                                   AND fecha_operacion >= @fechaInicio";
            
            await using var cmdTrimestre = new NpgsqlCommand(sqlTrimestre, conn);
            cmdTrimestre.Parameters.AddWithValue("idCliente", idCliente);
            cmdTrimestre.Parameters.AddWithValue("fechaInicio", primerDiaTrimestre);
            AcumuladoTrimestre = Convert.ToDecimal(await cmdTrimestre.ExecuteScalarAsync() ?? 0);
            
            DisponibleTrimestre = LIMITE_TRIMESTRE - AcumuladoTrimestre;
            LimiteTrimestreExcedido = AcumuladoTrimestre >= LIMITE_TRIMESTRE;
            MostrarDisponibleTrimestre = !LimiteTrimestreExcedido;
            
            OnPropertyChanged(nameof(ColorAcumuladoTrimestre));
            OnPropertyChanged(nameof(ColorTextoAcumuladoTrimestre));
        }
        catch
        {
            AcumuladoMes = 0;
            AcumuladoTrimestre = 0;
            DisponibleTrimestre = LIMITE_TRIMESTRE;
            LimiteTrimestreExcedido = false;
            MostrarDisponibleTrimestre = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CARGA DE MARGEN
    // ═══════════════════════════════════════════════════════════

    private async Task CargarMargenGananciaAsync()
    {
        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            decimal? margenLocal = null;
            decimal? margenComercio = null;
            decimal? margenGlobal = null;

            if (_idLocalActual > 0)
            {
                var sqlLocal = "SELECT comision_divisas FROM locales WHERE id_local = @id AND comision_divisas > 0";
                await using var cmdLocal = new NpgsqlCommand(sqlLocal, conn);
                cmdLocal.Parameters.AddWithValue("id", _idLocalActual);
                var result = await cmdLocal.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    margenLocal = Convert.ToDecimal(result);
            }

            if (_idComercioActual > 0 && margenLocal == null)
            {
                var sqlComercio = "SELECT porcentaje_comision_divisas FROM comercios WHERE id_comercio = @id AND porcentaje_comision_divisas > 0";
                await using var cmdComercio = new NpgsqlCommand(sqlComercio, conn);
                cmdComercio.Parameters.AddWithValue("id", _idComercioActual);
                var result = await cmdComercio.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    margenComercio = Convert.ToDecimal(result);
            }

            if (margenLocal == null && margenComercio == null)
            {
                var sqlGlobal = "SELECT valor_decimal FROM configuracion_sistema WHERE clave = 'margen_divisas_global'";
                await using var cmdGlobal = new NpgsqlCommand(sqlGlobal, conn);
                var result = await cmdGlobal.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    margenGlobal = Convert.ToDecimal(result);
            }

            _margenGanancia = margenLocal ?? margenComercio ?? margenGlobal ?? 10.00m;
        }
        catch
        {
            _margenGanancia = 10.00m;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CARGA DE DATOS
    // ═══════════════════════════════════════════════════════════

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        
        try
        {
            var allCurrencies = GetAllCurrenciesList();
            var rates = await _currencyService.GetExchangeRatesAsync();
            
            foreach (var currency in allCurrencies)
            {
                if (rates.TryGetValue(currency.Code, out decimal rate))
                {
                    currency.RateToEur = rate;
                    currency.RateWithMargin = rate / (1m + (_margenGanancia / 100m));
                }
                else
                {
                    currency.RateToEur = 1m;
                    currency.RateWithMargin = 1m / (1m + (_margenGanancia / 100m));  
                }
                currency.DisplayText = $"{currency.Code} | {currency.Name}";
            }
            
            AvailableCurrencies = new ObservableCollection<CurrencyModel>(allCurrencies);
            await LoadFavoriteCurrenciesAsync();
            
            if (AvailableCurrencies.Count > 0 && SelectedCurrency == null)
                SelectedCurrency = AvailableCurrencies.First();
            
            var lastUpdate = _currencyService.GetLastUpdateTime();
            LastUpdateText = lastUpdate > DateTime.MinValue 
                ? $"Actualizado: {lastUpdate:HH:mm}" 
                : "Tasas predeterminadas";
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

    private async Task LoadFavoriteCurrenciesAsync()
    {
        try
        {
            var favoriteCodes = new List<string>();
            
            if (_idLocalActual > 0)
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                
                var sql = @"SELECT codigo_divisa FROM divisas_favoritas_local 
                            WHERE id_local = @idLocal ORDER BY orden, id";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("idLocal", _idLocalActual);
                
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    favoriteCodes.Add(reader.GetString(0));
            }
            
            if (favoriteCodes.Count == 0)
                favoriteCodes = _favoriteCodes;
            
            var favorites = await _currencyService.GetFavoriteCurrenciesAsync(favoriteCodes);
            foreach (var fav in favorites)
                fav.RateWithMargin = fav.RateToEur / (1m + (_margenGanancia / 100m));
            
            FavoriteCurrencies = new ObservableCollection<FavoriteCurrencyModel>(favorites);
        }
        catch
        {
            FavoriteCurrencies = new ObservableCollection<FavoriteCurrencyModel>();
        }
    }

    [RelayCommand]
    private async Task RefreshRatesAsync()
    {
        await CargarMargenGananciaAsync();
        await _currencyService.ReloadConfigurationAsync();
        await LoadDataAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // SELECTOR DE DIVISAS
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private void MostrarAgregarDivisa()
    {
        BusquedaDivisa = string.Empty;
        CargarDivisasDisponibles();
        MostrarSelectorDivisas = true;
    }
    
    [RelayCommand]
    private void CerrarSelectorDivisas()
    {
        MostrarSelectorDivisas = false;
    }
    
    private void CargarDivisasDisponibles()
    {
        var favCodes = FavoriteCurrencies.Select(f => f.Code).ToHashSet();
        DivisasBusqueda.Clear();
        foreach (var divisa in AvailableCurrencies.Where(d => !favCodes.Contains(d.Code)))
            DivisasBusqueda.Add(divisa);
    }
    
    [RelayCommand]
    private async Task AgregarAFavoritasAsync(CurrencyModel? divisa)
    {
        if (divisa == null) return;
        
        if (FavoriteCurrencies.Any(f => f.Code == divisa.Code))
        {
            MostrarSelectorDivisas = false;
            return;
        }
        
        try
        {
            if (_idLocalActual > 0)
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                
                var sqlOrden = "SELECT COALESCE(MAX(orden), 0) + 1 FROM divisas_favoritas_local WHERE id_local = @idLocal";
                await using var cmdOrden = new NpgsqlCommand(sqlOrden, conn);
                cmdOrden.Parameters.AddWithValue("idLocal", _idLocalActual);
                var orden = Convert.ToInt32(await cmdOrden.ExecuteScalarAsync());
                
                var sql = @"INSERT INTO divisas_favoritas_local (id_local, codigo_divisa, orden) 
                            VALUES (@idLocal, @codigo, @orden) ON CONFLICT DO NOTHING";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("idLocal", _idLocalActual);
                cmd.Parameters.AddWithValue("codigo", divisa.Code);
                cmd.Parameters.AddWithValue("orden", orden);
                await cmd.ExecuteNonQueryAsync();
            }
            
            var fav = new FavoriteCurrencyModel
            {
                Code = divisa.Code,
                Name = divisa.Name,
                Country = divisa.Country,
                RateToEur = divisa.RateToEur,
                RateWithMargin = divisa.RateWithMargin
            };
            FavoriteCurrencies.Add(fav);
        }
        catch { }
        
        MostrarSelectorDivisas = false;
    }
    
    [RelayCommand]
    private async Task QuitarDeFavoritasAsync(FavoriteCurrencyModel? divisa)
    {
        if (divisa == null) return;
        
        try
        {
            if (_idLocalActual > 0)
            {
                await using var conn = new NpgsqlConnection(ConnectionString);
                await conn.OpenAsync();
                
                var sql = "DELETE FROM divisas_favoritas_local WHERE id_local = @idLocal AND codigo_divisa = @codigo";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("idLocal", _idLocalActual);
                cmd.Parameters.AddWithValue("codigo", divisa.Code);
                await cmd.ExecuteNonQueryAsync();
            }
            
            FavoriteCurrencies.Remove(divisa);
        }
        catch { }
    }
    
    partial void OnBusquedaDivisaChanged(string value)
    {
        if (MostrarSelectorDivisas)
        {
            var favCodes = FavoriteCurrencies.Select(f => f.Code).ToHashSet();
            DivisasBusqueda.Clear();
            
            IEnumerable<CurrencyModel> resultados;
            if (string.IsNullOrWhiteSpace(value))
                resultados = AvailableCurrencies.Where(d => !favCodes.Contains(d.Code));
            else
            {
                var filtro = value.ToLower();
                resultados = AvailableCurrencies.Where(d =>
                    !favCodes.Contains(d.Code) &&
                    (d.Code.ToLower().Contains(filtro) ||
                     d.Name.ToLower().Contains(filtro) ||
                     d.Country.ToLower().Contains(filtro)));
            }

            foreach (var divisa in resultados.Take(15))
                DivisasBusqueda.Add(divisa);
        }
    }
    
    partial void OnBusquedaDivisaCalculadoraChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("|"))
        {
            MostrarSugerenciasDivisa = false;
            DivisasFiltradas.Clear();
            return;
        }
        
        var filtro = value.ToUpperInvariant();
        var resultados = AvailableCurrencies
            .Where(c => c.Code.ToUpperInvariant().Contains(filtro) || 
                        c.Name.ToUpperInvariant().Contains(filtro))
            .Take(8)
            .ToList();
        
        DivisasFiltradas = new ObservableCollection<CurrencyModel>(resultados);
        MostrarSugerenciasDivisa = resultados.Count > 0;
    }

    [RelayCommand]
    private void SeleccionarDivisaSugerida(CurrencyModel divisa)
    {
        if (divisa == null) return;
        
        SelectedCurrency = divisa;
        BusquedaDivisaCalculadora = $"{divisa.Code} | {divisa.Name}";
        MostrarSugerenciasDivisa = false;
        DivisasFiltradas.Clear();
    }

    [RelayCommand]
    private void SeleccionarDivisaFavorita(FavoriteCurrencyModel? divisa)
    {
        if (divisa == null) return;
        var currency = AvailableCurrencies.FirstOrDefault(c => c.Code == divisa.Code);
        if (currency != null)
        {
            SelectedCurrency = currency;
            BusquedaDivisaCalculadora = $"{currency.Code} | {currency.Name}";
            CurrentRate = currency.RateWithMargin;
            RateDisplayText = $"1 {currency.Code} = {currency.RateWithMargin:N2} EUR";
            CalcularDesdeDivisa();
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CALCULADORA BIDIRECCIONAL
    // ═══════════════════════════════════════════════════════════

    partial void OnSelectedCurrencyChanged(CurrencyModel? value)
    {
        if (value != null)
        {
            CurrentRate = value.RateWithMargin;
            RateDisplayText = $"1 {value.Code} = {value.RateWithMargin:N4} EUR";
            BusquedaDivisaCalculadora = $"{value.Code} | {value.Name}";
            CalcularDesdeDivisa();
        }
    }

    partial void OnAmountReceivedChanged(string value)
    {
        if (!_calculandoDesdeEuros)
            CalcularDesdeDivisa();
    }
    
    partial void OnTotalInEurosTextoChanged(string value)
    {
        if (!_calculandoDesdeDivisa)
            CalcularDesdeEuros();
    }

    private void CalcularDesdeDivisa()
    {
        if (_calculandoDesdeEuros) return;
        _calculandoDesdeDivisa = true;
        
        try
        {
            if (SelectedCurrency == null || string.IsNullOrWhiteSpace(AmountReceived))
            {
                TotalInEuros = 0;
                TotalInEurosTexto = "0,00";
                ValidarOperacion();
                return;
            }

            var textoLimpio = AmountReceived.Replace(",", ".");
            if (!decimal.TryParse(textoLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidad) || cantidad <= 0)
            {
                TotalInEuros = 0;
                TotalInEurosTexto = "0,00";
                ValidarOperacion();
                return;
            }

            TotalInEuros = cantidad * SelectedCurrency.RateWithMargin;
            TotalInEurosTexto = TotalInEuros.ToString("N2", new CultureInfo("es-ES"));
            ValidarOperacion();
        }
        finally
        {
            _calculandoDesdeDivisa = false;
        }
    }
    
    private void CalcularDesdeEuros()
    {
        if (_calculandoDesdeDivisa) return;
        _calculandoDesdeEuros = true;
        
        try
        {
            if (SelectedCurrency == null || string.IsNullOrWhiteSpace(TotalInEurosTexto))
            {
                AmountReceived = "0.00";
                TotalInEuros = 0;
                ValidarOperacion();
                return;
            }

            var textoLimpio = TotalInEurosTexto.Replace(",", ".");
            if (!decimal.TryParse(textoLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal euros) || euros <= 0)
            {
                AmountReceived = "0.00";
                TotalInEuros = 0;
                ValidarOperacion();
                return;
            }

            TotalInEuros = euros;
            
            if (SelectedCurrency.RateWithMargin > 0)
            {
                decimal cantidadDivisa = euros / SelectedCurrency.RateWithMargin;
                AmountReceived = cantidadDivisa.ToString("N2", CultureInfo.InvariantCulture);
            }
            
            ValidarOperacion();
        }
        finally
        {
            _calculandoDesdeEuros = false;
        }
    }

    private void ValidarOperacion()
    {
        var cantidadValida = decimal.TryParse(AmountReceived?.Replace(",", "."),
            NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidad) && cantidad > 0;

        // Verificar que el cliente tenga imagenes de documento en la carpeta local
        bool tieneImagenes = true;
        if (ClienteSeleccionado != null)
        {
            tieneImagenes = DocumentImageService.ExistenImagenes(ClienteSeleccionado.IdCliente);
            if (!tieneImagenes)
            {
                ErrorMessage = "El cliente no tiene imagenes de documento. Edite el cliente para agregarlas.";
            }
            else if (ErrorMessage == "El cliente no tiene imagenes de documento. Edite el cliente para agregarlas.")
            {
                ErrorMessage = string.Empty;
            }
        }

        PuedeRealizarOperacion = ClienteSeleccionado != null && SelectedCurrency != null && cantidadValida && tieneImagenes;
        OnPropertyChanged(nameof(MostrarAvisoHacienda));
    }

    // ═══════════════════════════════════════════════════════════
    // BÚSQUEDA DE CLIENTES
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private async Task BuscarClientesManualAsync()
    {
        if (string.IsNullOrWhiteSpace(BusquedaCliente))
        {
            ErrorMessage = "Ingrese un termino de busqueda";
            return;
        }
        ErrorMessage = string.Empty;
        await BuscarClientesAsync(BusquedaCliente);
    }
    
    private async Task BuscarClientesAsync(string? termino)
    {
        try
        {
            IsLoading = true;
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            string whereClause;
            bool usarTermino = !string.IsNullOrWhiteSpace(termino);
            
            if (usarTermino)
            {
                whereClause = TipoBusquedaSeleccionado switch
                {
                    "Nombre" => @"(nombre ILIKE @termino OR apellidos ILIKE @termino 
                                  OR COALESCE(segundo_nombre, '') ILIKE @termino 
                                  OR COALESCE(segundo_apellido, '') ILIKE @termino
                                  OR CONCAT(nombre, ' ', apellidos) ILIKE @termino)",
                    "Documento" => "documento_numero ILIKE @termino",
                    "Telefono" => "telefono ILIKE @termino",
                    _ => @"(documento_numero ILIKE @termino OR telefono ILIKE @termino OR nombre ILIKE @termino 
                           OR apellidos ILIKE @termino OR CONCAT(nombre, ' ', apellidos) ILIKE @termino)"
                };
            }
            else
            {
                whereClause = "1=1";
            }

            var comercioFilter = _idComercioActual > 0 ? " AND id_comercio_registro = @idComercio" : "";

            var sql = $@"SELECT DISTINCT id_cliente, nombre, apellidos, telefono, direccion,
                            COALESCE(nacionalidad, '') as nacionalidad, 
                            COALESCE(documento_tipo, 'DNI') as documento_tipo, 
                            COALESCE(documento_numero, '') as documento_numero,
                            caducidad_documento, fecha_nacimiento,
                            segundo_nombre, segundo_apellido,
                            imagen_documento_frontal, imagen_documento_trasera,
                            COALESCE(tipo_residencia, '') as tipo_residencia
                        FROM clientes
                        WHERE activo = true AND {whereClause}{comercioFilter}
                        ORDER BY nombre, apellidos
                        LIMIT 50";

            await using var cmd = new NpgsqlCommand(sql, conn);
            if (usarTermino)
                cmd.Parameters.AddWithValue("termino", $"%{termino}%");
            if (_idComercioActual > 0)
                cmd.Parameters.AddWithValue("idComercio", _idComercioActual);

            ClientesEncontrados.Clear();
            var idsAgregados = new HashSet<int>();
            
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var idCliente = reader.GetInt32(0);
                if (idsAgregados.Contains(idCliente)) continue;
                idsAgregados.Add(idCliente);
                
                var cliente = new ClienteModel
                {
                    IdCliente = idCliente,
                    Nombre = reader.GetString(1),
                    Apellido = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Telefono = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Direccion = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Nacionalidad = reader.GetString(5),
                    TipoDocumento = reader.GetString(6),
                    NumeroDocumento = reader.GetString(7),
                    CaducidadDocumento = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    FechaNacimiento = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    SegundoNombre = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    SegundoApellido = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    ImagenDocumentoFrontal = reader.IsDBNull(12) ? null : (byte[])reader[12],
                    ImagenDocumentoTrasera = reader.IsDBNull(13) ? null : (byte[])reader[13],
                    TipoResidencia = reader.GetString(14)
                };
                
                ClientesEncontrados.Add(cliente);
            }

            if (!ClientesEncontrados.Any())
                ErrorMessage = usarTermino ? "No se encontraron clientes" : "No hay clientes registrados";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error en busqueda: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SeleccionarClienteYContinuarAsync(ClienteModel? cliente)
    {
        if (cliente == null) return;
        ClienteSeleccionado = cliente;
        ValidarOperacion();
        await CargarAcumuladosClienteAsync(cliente.IdCliente);
        OcultarTodasLasVistas();
        VistaTransaccion = true;
    }

    // ═══════════════════════════════════════════════════════════
    // FORMULARIO NUEVO CLIENTE
    // ═══════════════════════════════════════════════════════════

    private void LimpiarFormularioCliente()
    {
        _idClienteEditando = 0;
        NuevoNombre = string.Empty;
        NuevoSegundoNombre = string.Empty;
        NuevoApellido = string.Empty;
        NuevoSegundoApellido = string.Empty;
        NuevoTelefono = string.Empty;
        NuevaDireccion = string.Empty;
        NuevaNacionalidad = string.Empty;
        NuevoTipoDocumento = "DNI";
        NuevoNumeroDocumento = string.Empty;
        NuevoTipoResidencia = string.Empty;
        TiposDocumento = new ObservableCollection<string> { "DNI", "NIE", "Pasaporte" };
        NuevaCaducidadDocumentoTexto = string.Empty;
        NuevaFechaNacimientoTexto = string.Empty;
        NuevaImagenDocumentoFrontal = null;
        NuevaImagenDocumentoTrasera = null;
        TieneImagenDocumentoFrontal = false;
        TieneImagenDocumentoTrasera = false;
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
        ImagenDocumentoFrontalPreview = null;
        ImagenDocumentoTraseraPreview = null;
    }

    [RelayCommand]
    private async Task GuardarNuevoClienteAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            DateTime? fechaNacimiento = null;
            DateTime? fechaCaducidad = null;

            if (!string.IsNullOrWhiteSpace(NuevaFechaNacimientoTexto))
            {
                if (DateTime.TryParseExact(NuevaFechaNacimientoTexto, "dd/MM/yyyy", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fn))
                    fechaNacimiento = fn;
            }

            if (!string.IsNullOrWhiteSpace(NuevaCaducidadDocumentoTexto))
            {
                if (DateTime.TryParseExact(NuevaCaducidadDocumentoTexto, "dd/MM/yyyy", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fc))
                    fechaCaducidad = fc;
            }

            var sqlVerificar = @"SELECT COUNT(*) FROM clientes 
                                 WHERE UPPER(documento_tipo) = @tipoDoc 
                                 AND UPPER(documento_numero) = @numDoc 
                                 AND UPPER(nombre) = @nombre
                                 AND UPPER(apellidos) = @apellido
                                 AND fecha_nacimiento = @fechaNac
                                 AND id_comercio_registro = @idComercio
                                 AND activo = true";
            
            await using var cmdVerificar = new NpgsqlCommand(sqlVerificar, conn);
            cmdVerificar.Parameters.AddWithValue("tipoDoc", NuevoTipoDocumento.ToUpper());
            cmdVerificar.Parameters.AddWithValue("numDoc", NuevoNumeroDocumento.Trim().ToUpper());
            cmdVerificar.Parameters.AddWithValue("nombre", NuevoNombre.Trim().ToUpper());
            cmdVerificar.Parameters.AddWithValue("apellido", NuevoApellido.Trim().ToUpper());
            cmdVerificar.Parameters.AddWithValue("fechaNac", (object?)fechaNacimiento ?? DBNull.Value);
            cmdVerificar.Parameters.AddWithValue("idComercio", _idComercioActual > 0 ? _idComercioActual : (object)DBNull.Value);

            var existe = Convert.ToInt32(await cmdVerificar.ExecuteScalarAsync()) > 0;
            
            if (existe)
            {
                ErrorMessage = $"Ya existe un cliente con estos datos. Use la busqueda.";
                IsLoading = false;
                return;
            }

            // Imagenes ya no se guardan en BD, se guardan en carpeta local
            var sql = @"INSERT INTO clientes
                        (nombre, segundo_nombre, apellidos, segundo_apellido,
                        telefono, direccion, nacionalidad, tipo_residencia,
                        documento_tipo, documento_numero, caducidad_documento,
                        fecha_nacimiento,
                        id_comercio_registro, id_local_registro, id_usuario_registro, activo)
                        VALUES
                        (@nombre, @segundoNombre, @apellido, @segundoApellido,
                            @telefono, @direccion, @nacionalidad, @tipoResidencia,
                            @tipoDoc, @numDoc, @caducidad, @fechaNac,
                            @idComercio, @idLocal, @idUsuario, true)
                        RETURNING id_cliente";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("nombre", NuevoNombre);
            cmd.Parameters.AddWithValue("segundoNombre", NuevoSegundoNombre ?? "");
            cmd.Parameters.AddWithValue("apellido", NuevoApellido ?? "");
            cmd.Parameters.AddWithValue("segundoApellido", NuevoSegundoApellido ?? "");
            cmd.Parameters.AddWithValue("telefono", NuevoTelefono ?? "");
            cmd.Parameters.AddWithValue("direccion", NuevaDireccion ?? "");
            cmd.Parameters.AddWithValue("nacionalidad", NuevaNacionalidad ?? "");
            cmd.Parameters.AddWithValue("tipoResidencia", NuevoTipoResidencia ?? "");
            cmd.Parameters.AddWithValue("tipoDoc", NuevoTipoDocumento);
            cmd.Parameters.AddWithValue("numDoc", NuevoNumeroDocumento.Trim().ToUpper());
            cmd.Parameters.AddWithValue("caducidad", (object?)fechaCaducidad ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fechaNac", (object?)fechaNacimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("idComercio", _idComercioActual > 0 ? _idComercioActual : DBNull.Value);
            cmd.Parameters.AddWithValue("idLocal", _idLocalActual > 0 ? _idLocalActual : DBNull.Value);
            cmd.Parameters.AddWithValue("idUsuario", _idUsuarioActual > 0 ? _idUsuarioActual : DBNull.Value);

            var idCliente = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            // Guardar imagenes en carpeta local
            if (NuevaImagenDocumentoFrontal != null)
                await DocumentImageService.GuardarImagenFrontalAsync(idCliente, NuevaImagenDocumentoFrontal);
            if (NuevaImagenDocumentoTrasera != null)
                await DocumentImageService.GuardarImagenTraseraAsync(idCliente, NuevaImagenDocumentoTrasera);

            var nuevoCliente = new ClienteModel
            {
                IdCliente = idCliente,
                Nombre = NuevoNombre,
                SegundoNombre = NuevoSegundoNombre ?? "",
                Apellido = NuevoApellido ?? "",
                SegundoApellido = NuevoSegundoApellido ?? "",
                Telefono = NuevoTelefono ?? "",
                Direccion = NuevaDireccion ?? "",
                Nacionalidad = NuevaNacionalidad ?? "",
                TipoDocumento = NuevoTipoDocumento,
                NumeroDocumento = NuevoNumeroDocumento.Trim().ToUpper(),
                CaducidadDocumento = fechaCaducidad,
                FechaNacimiento = fechaNacimiento,
                ImagenDocumentoFrontal = NuevaImagenDocumentoFrontal,
                ImagenDocumentoTrasera = NuevaImagenDocumentoTrasera
            };

            ClienteSeleccionado = nuevoCliente;
            await CargarAcumuladosClienteAsync(idCliente);
            LimpiarFormularioCliente();
            
            OcultarTodasLasVistas();
            VistaTransaccion = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al crear cliente: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task GuardarEdicionClienteAsync()
    {
        if (_idClienteEditando <= 0)
        {
            ErrorMessage = "No hay cliente para editar";
            return;
        }
        
        if (string.IsNullOrWhiteSpace(NuevoNombre) || string.IsNullOrWhiteSpace(NuevoApellido) ||
            string.IsNullOrWhiteSpace(NuevaDireccion) || string.IsNullOrWhiteSpace(NuevoTipoDocumento) ||
            string.IsNullOrWhiteSpace(NuevoNumeroDocumento) || string.IsNullOrWhiteSpace(NuevaNacionalidad) ||
            string.IsNullOrWhiteSpace(NuevaFechaNacimientoTexto) || string.IsNullOrWhiteSpace(NuevaCaducidadDocumentoTexto) ||
            !TieneImagenDocumentoFrontal || !TieneImagenDocumentoTrasera)
        {
            ErrorMessage = "Todos los campos obligatorios deben estar completos";
            return;
        }
        
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            DateTime? fechaNacimiento = null;
            DateTime? fechaCaducidad = null;

            if (DateTime.TryParseExact(NuevaFechaNacimientoTexto, "dd/MM/yyyy", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var fn))
                fechaNacimiento = fn;

            if (DateTime.TryParseExact(NuevaCaducidadDocumentoTexto, "dd/MM/yyyy", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var fc))
                fechaCaducidad = fc;

            // Imagenes ya no se guardan en BD, se guardan en carpeta local
            var sql = @"UPDATE clientes SET
                        nombre = @nombre, segundo_nombre = @segundoNombre,
                        apellidos = @apellido, segundo_apellido = @segundoApellido,
                        telefono = @telefono, direccion = @direccion,
                        nacionalidad = @nacionalidad, tipo_residencia = @tipoResidencia,
                        documento_tipo = @tipoDoc,
                        documento_numero = @numDoc, caducidad_documento = @caducidad,
                        fecha_nacimiento = @fechaNac
                        WHERE id_cliente = @idCliente";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("idCliente", _idClienteEditando);
            cmd.Parameters.AddWithValue("nombre", NuevoNombre);
            cmd.Parameters.AddWithValue("segundoNombre", NuevoSegundoNombre ?? "");
            cmd.Parameters.AddWithValue("apellido", NuevoApellido ?? "");
            cmd.Parameters.AddWithValue("segundoApellido", NuevoSegundoApellido ?? "");
            cmd.Parameters.AddWithValue("telefono", NuevoTelefono ?? "");
            cmd.Parameters.AddWithValue("direccion", NuevaDireccion ?? "");
            cmd.Parameters.AddWithValue("nacionalidad", NuevaNacionalidad ?? "");
            cmd.Parameters.AddWithValue("tipoResidencia", NuevoTipoResidencia ?? "");
            cmd.Parameters.AddWithValue("tipoDoc", NuevoTipoDocumento);
            cmd.Parameters.AddWithValue("numDoc", NuevoNumeroDocumento.Trim().ToUpper());
            cmd.Parameters.AddWithValue("caducidad", (object?)fechaCaducidad ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fechaNac", (object?)fechaNacimiento ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();

            // Guardar imagenes en carpeta local
            if (NuevaImagenDocumentoFrontal != null)
                await DocumentImageService.GuardarImagenFrontalAsync(_idClienteEditando, NuevaImagenDocumentoFrontal);
            if (NuevaImagenDocumentoTrasera != null)
                await DocumentImageService.GuardarImagenTraseraAsync(_idClienteEditando, NuevaImagenDocumentoTrasera);

            ClienteSeleccionado = new ClienteModel
            {
                IdCliente = _idClienteEditando,
                Nombre = NuevoNombre,
                SegundoNombre = NuevoSegundoNombre ?? "",
                Apellido = NuevoApellido ?? "",
                SegundoApellido = NuevoSegundoApellido ?? "",
                Telefono = NuevoTelefono ?? "",
                Direccion = NuevaDireccion ?? "",
                Nacionalidad = NuevaNacionalidad ?? "",
                TipoDocumento = NuevoTipoDocumento,
                NumeroDocumento = NuevoNumeroDocumento.Trim().ToUpper(),
                CaducidadDocumento = fechaCaducidad,
                FechaNacimiento = fechaNacimiento,
                ImagenDocumentoFrontal = NuevaImagenDocumentoFrontal,
                ImagenDocumentoTrasera = NuevaImagenDocumentoTrasera
            };
            
            OcultarTodasLasVistas();
            VistaTransaccion = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al actualizar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // IMÁGENES DE DOCUMENTO
    // ═══════════════════════════════════════════════════════════
    
    public void SetImagenDocumentoFrontal(byte[] imagen)
    {
        NuevaImagenDocumentoFrontal = imagen;
        TieneImagenDocumentoFrontal = true;
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
        try
        {
            using var stream = new MemoryStream(imagen);
            ImagenDocumentoFrontalPreview = new Bitmap(stream);
        }
        catch { }
    }
    
    public void SetImagenDocumentoTrasera(byte[] imagen)
    {
        NuevaImagenDocumentoTrasera = imagen;
        TieneImagenDocumentoTrasera = true;
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
        try
        {
            using var stream = new MemoryStream(imagen);
            ImagenDocumentoTraseraPreview = new Bitmap(stream);
        }
        catch { }
    }
    
    [RelayCommand]
    private void QuitarImagenFrontal()
    {
        NuevaImagenDocumentoFrontal = null;
        TieneImagenDocumentoFrontal = false;
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
        ImagenDocumentoFrontalPreview = null;
    }
    
    [RelayCommand]
    private void QuitarImagenTrasera()
    {
        NuevaImagenDocumentoTrasera = null;
        TieneImagenDocumentoTrasera = false;
        OnPropertyChanged(nameof(TieneAlgunaImagenDocumento));
        ImagenDocumentoTraseraPreview = null;
    }

    [RelayCommand]
    private void MostrarPreviewImagenFrontal()
    {
        if (ImagenDocumentoFrontalPreview != null)
        {
            ImagenPreviewActual = ImagenDocumentoFrontalPreview;
            TituloPreviewImagen = "Documento - Cara Frontal";
            MostrarPreviewImagen = true;
        }
    }

    [RelayCommand]
    private void MostrarPreviewImagenTrasera()
    {
        if (ImagenDocumentoTraseraPreview != null)
        {
            ImagenPreviewActual = ImagenDocumentoTraseraPreview;
            TituloPreviewImagen = "Documento - Cara Trasera";
            MostrarPreviewImagen = true;
        }
    }

    [RelayCommand]
    private void CerrarPreviewImagen()
    {
        MostrarPreviewImagen = false;
        ImagenPreviewActual = null;
    }

    // ═══════════════════════════════════════════════════════════
    // TRANSACCIÓN - PREPARAR RESUMEN (solo preview, NO incrementa)
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private async Task RealizarTransaccionAsync()
    {
        if (!PuedeRealizarOperacion)
        {
            ErrorMessage = "No se puede realizar la operacion. Verifique los datos.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            FechaOperacion = ObtenerHoraEspana();
            _operacionGuardada = false;

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            int idLocalParaPreview = _idLocalActual;

            if (idLocalParaPreview <= 0)
            {
                var sqlPrimerLocal = "SELECT id_local FROM locales WHERE activo = true ORDER BY id_local LIMIT 1";
                await using var cmdPrimer = new NpgsqlCommand(sqlPrimerLocal, conn);
                var result = await cmdPrimer.ExecuteScalarAsync();
                if (result != null)
                    idLocalParaPreview = Convert.ToInt32(result);
            }

            // Solo LEER el siguiente numero (sin incrementar) para mostrar preview
            var sqlVerificar = @"SELECT COALESCE(ultimo_correlativo, 0) + 1 FROM correlativos_operaciones
                                WHERE id_local = @idLocal AND prefijo = 'DI'";
            await using var cmdVerificar = new NpgsqlCommand(sqlVerificar, conn);
            cmdVerificar.Parameters.AddWithValue("idLocal", idLocalParaPreview);
            var siguiente = await cmdVerificar.ExecuteScalarAsync();
            int numeroPreview = siguiente != null && siguiente != DBNull.Value ? Convert.ToInt32(siguiente) : 1;

            NumeroOperacionDisplay = $"DI{numeroPreview:D4}";

            OcultarTodasLasVistas();
            VistaConfirmacionTransaccion = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al preparar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Confirma la operacion, la guarda en BD y navega al resumen final
    /// </summary>
    [RelayCommand]
    private async Task ConfirmarYGuardarOperacionAsync()
    {
        if (_operacionGuardada)
        {
            OcultarTodasLasVistas();
            VistaResumen = true;
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var (exito, mensaje) = await GuardarOperacionCompletaAsync();

            if (exito)
            {
                _operacionGuardada = true;
                OcultarTodasLasVistas();
                VistaResumen = true;
            }
            else
            {
                ErrorMessage = mensaje;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error inesperado: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Guarda la operación COMPLETA en una sola transacción (incluyendo correlativo)
    /// </summary>
    private async Task<(bool exito, string mensaje)> GuardarOperacionCompletaAsync()
    {
        try
        {
            if (ClienteSeleccionado == null)
                return (false, "Error: No hay cliente seleccionado.");
            
            if (SelectedCurrency == null)
                return (false, "Error: No hay divisa seleccionada.");
            
            var textoLimpio = AmountReceived?.Replace(",", ".") ?? "0";
            if (!decimal.TryParse(textoLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal cantidadOrigen) || cantidadOrigen <= 0)
                return (false, "Error: Cantidad de divisa invalida.");
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();
            
            // TRANSACCIÓN ÚNICA PARA TODO
            await using var transaction = await conn.BeginTransactionAsync();
            
            try
            {
                // 1. Obtener datos del local
                int idComercioOp = _idComercioActual;
                int idLocalOp = _idLocalActual;
                string codigoLocal = "SIN-LOCAL";
                
                if (idLocalOp > 0)
                {
                    var sqlLocal = "SELECT id_comercio, codigo_local FROM locales WHERE id_local = @idLocal";
                    await using var cmdLocal = new NpgsqlCommand(sqlLocal, conn, transaction);
                    cmdLocal.Parameters.AddWithValue("idLocal", idLocalOp);
                    await using var reader = await cmdLocal.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        idComercioOp = reader.GetInt32(0);
                        codigoLocal = reader.GetString(1);
                    }
                    await reader.CloseAsync();
                }
                
                if (idLocalOp <= 0 || idComercioOp <= 0)
                {
                    var sqlPrimer = @"SELECT id_local, id_comercio, codigo_local 
                                     FROM locales WHERE activo = true ORDER BY id_local LIMIT 1";
                    await using var cmdPrimer = new NpgsqlCommand(sqlPrimer, conn, transaction);
                    await using var readerPrimer = await cmdPrimer.ExecuteReaderAsync();
                    if (await readerPrimer.ReadAsync())
                    {
                        idLocalOp = readerPrimer.GetInt32(0);
                        idComercioOp = readerPrimer.GetInt32(1);
                        codigoLocal = readerPrimer.GetString(2);
                    }
                    await readerPrimer.CloseAsync();
                }
                
                if (idLocalOp <= 0 || idComercioOp <= 0)
                {
                    await transaction.RollbackAsync();
                    return (false, "Error: No se encontro local/comercio valido.");
                }
                
                // 2. Datos del usuario
                int idUsuarioOp = _idUsuarioActual;
                string nombreUsuario = _nombreUsuarioActual ?? "";
                string numeroUsuario = _numeroUsuarioActual ?? "";
                
                if (idUsuarioOp <= 0)
                {
                    var sqlUser = @"SELECT id_usuario, COALESCE(nombre, '') || ' ' || COALESCE(apellidos, ''),
                                   COALESCE(numero_usuario, 'USR001')
                                   FROM usuarios WHERE activo = true ORDER BY id_usuario LIMIT 1";
                    await using var cmdUser = new NpgsqlCommand(sqlUser, conn, transaction);
                    await using var readerUser = await cmdUser.ExecuteReaderAsync();
                    if (await readerUser.ReadAsync())
                    {
                        idUsuarioOp = readerUser.GetInt32(0);
                        nombreUsuario = readerUser.GetString(1).Trim();
                        numeroUsuario = readerUser.GetString(2);
                    }
                    await readerUser.CloseAsync();
                }
                
                if (idUsuarioOp <= 0)
                {
                    await transaction.RollbackAsync();
                    return (false, "Error: No se encontro usuario valido.");
                }
                
                if (string.IsNullOrEmpty(nombreUsuario)) nombreUsuario = "Usuario Sistema";
                if (string.IsNullOrEmpty(numeroUsuario)) numeroUsuario = "SYS001";
                
                string nombreCliente = ClienteSeleccionado.NombreCompleto ?? 
                    $"{ClienteSeleccionado.Nombre} {ClienteSeleccionado.Apellido}".Trim();
                if (string.IsNullOrEmpty(nombreCliente)) nombreCliente = "Cliente";
                
                var horaOp = ObtenerHoraEspana();
                
                // 3. OBTENER CORRELATIVO - verificar tabla correlativos Y maximo real en operaciones
                int correlativoTabla = 0;
                int correlativoReal = 0;

                // Obtener de tabla correlativos
                var sqlCheck = @"SELECT ultimo_correlativo FROM correlativos_operaciones 
                                WHERE id_local = @idLocal AND prefijo = 'DI' FOR UPDATE";
                await using var cmdCheck = new NpgsqlCommand(sqlCheck, conn, transaction);
                cmdCheck.Parameters.AddWithValue("idLocal", idLocalOp);
                var resultado = await cmdCheck.ExecuteScalarAsync();
                if (resultado != null && resultado != DBNull.Value)
                    correlativoTabla = Convert.ToInt32(resultado);

                // Obtener maximo real de operaciones para este local
                var sqlMaxReal = @"SELECT COALESCE(MAX(CAST(SUBSTRING(numero_operacion FROM 3) AS INTEGER)), 0)
                                FROM operaciones 
                                WHERE id_local = @idLocal AND numero_operacion LIKE 'DI%'";
                await using var cmdMaxReal = new NpgsqlCommand(sqlMaxReal, conn, transaction);
                cmdMaxReal.Parameters.AddWithValue("idLocal", idLocalOp);
                var resultadoReal = await cmdMaxReal.ExecuteScalarAsync();
                if (resultadoReal != null && resultadoReal != DBNull.Value)
                    correlativoReal = Convert.ToInt32(resultadoReal);

                // Usar el mayor + 1
                int nuevoCorrelativo = Math.Max(correlativoTabla, correlativoReal) + 1;

                // Actualizar o insertar en tabla correlativos
                if (correlativoTabla == 0 && resultado == null)
                {
                    var sqlIns = @"INSERT INTO correlativos_operaciones (id_local, prefijo, ultimo_correlativo, fecha_ultimo_uso)
                                VALUES (@idLocal, 'DI', @correlativo, @fecha)";
                    await using var cmdIns = new NpgsqlCommand(sqlIns, conn, transaction);
                    cmdIns.Parameters.AddWithValue("idLocal", idLocalOp);
                    cmdIns.Parameters.AddWithValue("correlativo", nuevoCorrelativo);
                    cmdIns.Parameters.AddWithValue("fecha", horaOp);
                    await cmdIns.ExecuteNonQueryAsync();
                }
                else
                {
                    var sqlUpd = @"UPDATE correlativos_operaciones 
                                SET ultimo_correlativo = @correlativo, fecha_ultimo_uso = @fecha
                                WHERE id_local = @idLocal AND prefijo = 'DI'";
                    await using var cmdUpd = new NpgsqlCommand(sqlUpd, conn, transaction);
                    cmdUpd.Parameters.AddWithValue("idLocal", idLocalOp);
                    cmdUpd.Parameters.AddWithValue("correlativo", nuevoCorrelativo);
                    cmdUpd.Parameters.AddWithValue("fecha", horaOp);
                    await cmdUpd.ExecuteNonQueryAsync();
                }

                string numOpGenerado = $"DI{nuevoCorrelativo:D4}";
                
                // 4. INSERTAR OPERACIÓN
                var sqlOp = @"INSERT INTO operaciones 
                             (numero_operacion, id_comercio, id_local, codigo_local, 
                              id_usuario, nombre_usuario, numero_usuario,
                              id_cliente, nombre_cliente, modulo, tipo_operacion,
                              importe_total, estado, fecha_operacion, hora_operacion, observaciones)
                             VALUES 
                             (@numOp, @idComercio, @idLocal, @codigoLocal,
                              @idUsuario, @nombreUsuario, @numeroUsuario,
                              @idCliente, @nombreCliente, 'DIVISAS', 'COMPRA',
                              @importe, 'COMPLETADA', @fecha, @hora, @obs)
                             RETURNING id_operacion";
                
                await using var cmdOp = new NpgsqlCommand(sqlOp, conn, transaction);
                cmdOp.Parameters.AddWithValue("numOp", numOpGenerado);
                cmdOp.Parameters.AddWithValue("idComercio", idComercioOp);
                cmdOp.Parameters.AddWithValue("idLocal", idLocalOp);
                cmdOp.Parameters.AddWithValue("codigoLocal", codigoLocal);
                cmdOp.Parameters.AddWithValue("idUsuario", idUsuarioOp);
                cmdOp.Parameters.AddWithValue("nombreUsuario", nombreUsuario);
                cmdOp.Parameters.AddWithValue("numeroUsuario", numeroUsuario);
                cmdOp.Parameters.AddWithValue("idCliente", ClienteSeleccionado.IdCliente);
                cmdOp.Parameters.AddWithValue("nombreCliente", nombreCliente);
                cmdOp.Parameters.AddWithValue("importe", TotalInEuros);
                cmdOp.Parameters.AddWithValue("fecha", horaOp);
                cmdOp.Parameters.AddWithValue("hora", horaOp.TimeOfDay);
                cmdOp.Parameters.AddWithValue("obs", $"Compra de {cantidadOrigen} {SelectedCurrency.Code}");
                
                var idOperacion = Convert.ToInt64(await cmdOp.ExecuteScalarAsync());
                
                // 5. INSERTAR DETALLE DIVISAS
                var sqlDet = @"INSERT INTO operaciones_divisas 
                              (id_operacion, divisa_origen, divisa_destino, 
                               cantidad_origen, cantidad_destino, tipo_cambio, tipo_cambio_aplicado)
                              VALUES (@idOp, @divisaOrigen, 'EUR', @cantOrigen, @cantDestino, @tasa, @tasaAplicada)";
                
                await using var cmdDet = new NpgsqlCommand(sqlDet, conn, transaction);
                cmdDet.Parameters.AddWithValue("idOp", idOperacion);
                cmdDet.Parameters.AddWithValue("divisaOrigen", SelectedCurrency.Code);
                cmdDet.Parameters.AddWithValue("cantOrigen", cantidadOrigen);
                cmdDet.Parameters.AddWithValue("cantDestino", TotalInEuros);
                cmdDet.Parameters.AddWithValue("tasa", SelectedCurrency.RateToEur);
                cmdDet.Parameters.AddWithValue("tasaAplicada", SelectedCurrency.RateWithMargin);
                await cmdDet.ExecuteNonQueryAsync();
                
                // 6. INSERTAR BALANCE DIVISAS
                var sqlBal = @"INSERT INTO balance_divisas 
                              (id_comercio, id_local, id_usuario, id_operacion,
                               codigo_divisa, nombre_divisa, cantidad_recibida, cantidad_entregada_eur,
                               tasa_cambio_momento, tasa_cambio_aplicada, tipo_movimiento, observaciones, fecha_registro)
                              VALUES (@idComercio, @idLocal, @idUsuario, @idOperacion,
                               @codigoDivisa, @nombreDivisa, @cantRecibida, @cantEntregada,
                               @tasaMomento, @tasaAplicada, 'ENTRADA', @obs, @fechaReg)";
                
                await using var cmdBal = new NpgsqlCommand(sqlBal, conn, transaction);
                cmdBal.Parameters.AddWithValue("idComercio", idComercioOp);
                cmdBal.Parameters.AddWithValue("idLocal", idLocalOp);
                cmdBal.Parameters.AddWithValue("idUsuario", idUsuarioOp);
                cmdBal.Parameters.AddWithValue("idOperacion", idOperacion);
                cmdBal.Parameters.AddWithValue("codigoDivisa", SelectedCurrency.Code);
                cmdBal.Parameters.AddWithValue("nombreDivisa", SelectedCurrency.Name);
                cmdBal.Parameters.AddWithValue("cantRecibida", cantidadOrigen);
                cmdBal.Parameters.AddWithValue("cantEntregada", TotalInEuros);
                cmdBal.Parameters.AddWithValue("tasaMomento", SelectedCurrency.RateToEur);
                cmdBal.Parameters.AddWithValue("tasaAplicada", SelectedCurrency.RateWithMargin);
                cmdBal.Parameters.AddWithValue("obs", $"Compra {cantidadOrigen} {SelectedCurrency.Code} - {nombreCliente}");
                cmdBal.Parameters.AddWithValue("fechaReg", horaOp);
                await cmdBal.ExecuteNonQueryAsync();
                
                // COMMIT
                await transaction.CommitAsync();
                
                // Actualizar en memoria
                NumeroOperacion = idOperacion;
                NumeroOperacionDisplay = numOpGenerado;
                FechaOperacion = horaOp;
                
                return (true, "Operacion guardada correctamente");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (false, $"Error en transaccion: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexion: {ex.Message}");
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // IMPRESIÓN DE RECIBO
    // ═══════════════════════════════════════════════════════════
    
    [RelayCommand]
    private async Task ImprimirReciboAsync()
    {
        try
        {
            IsLoading = true;
            
            var pdfService = new ReciboDivisasPdfService();
            var pdfPath = await pdfService.GenerarReciboPdfAsync(
                numeroOperacion: NumeroOperacionDisplay,
                fechaOperacion: FechaOperacion,
                cliente: ClienteSeleccionado!,
                divisaOrigen: SelectedCurrency!.Code,
                nombreDivisa: SelectedCurrency.Name,
                cantidadRecibida: decimal.Parse(AmountReceived.Replace(",", "."), CultureInfo.InvariantCulture),
                totalEntregado: TotalInEuros,
                tasaCambio: SelectedCurrency.RateWithMargin,
                idLocal: _idLocalActual
            );
            
            if (!string.IsNullOrEmpty(pdfPath) && File.Exists(pdfPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al generar recibo: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // LISTA DE DIVISAS
    // ═══════════════════════════════════════════════════════════

    private List<CurrencyModel> GetAllCurrenciesList()
    {
        return new List<CurrencyModel>
        {
            new() { Code = "USD", Name = "Dolar USA", Country = "Estados Unidos" },
            new() { Code = "CAD", Name = "Dolar Canadiense", Country = "Canada" },
            new() { Code = "AUD", Name = "Dolar Australiano", Country = "Australia" },
            new() { Code = "CHF", Name = "Franco Suizo", Country = "Suiza" },
            new() { Code = "GBP", Name = "Libra Esterlina", Country = "Reino Unido" },
            new() { Code = "HKD", Name = "Dolar Hong Kong", Country = "Hong Kong" },
            new() { Code = "AED", Name = "Dirham EAU", Country = "Emiratos Arabes" },
            new() { Code = "BGN", Name = "Lev Bulgaro", Country = "Bulgaria" },
            new() { Code = "BRL", Name = "Real Brasileno", Country = "Brasil" },
            new() { Code = "CNY", Name = "Yuan Renminbi", Country = "China" },
            new() { Code = "CZK", Name = "Corona Checa", Country = "Republica Checa" },
            new() { Code = "HUF", Name = "Forint", Country = "Hungria" },
            new() { Code = "JPY", Name = "Yen Japones", Country = "Japon" },
            new() { Code = "MAD", Name = "Dirham Marroqui", Country = "Marruecos" },
            new() { Code = "MXN", Name = "Peso Mexicano", Country = "Mexico" },
            new() { Code = "NOK", Name = "Corona Noruega", Country = "Noruega" },
            new() { Code = "NZD", Name = "Dolar Neozelandes", Country = "Nueva Zelanda" },
            new() { Code = "PLN", Name = "Zloty", Country = "Polonia" },
            new() { Code = "RON", Name = "Leu Rumano", Country = "Rumania" },
            new() { Code = "RUB", Name = "Rublo Ruso", Country = "Rusia" },
            new() { Code = "SAR", Name = "Rial Saudi", Country = "Arabia Saudita" },
            new() { Code = "SEK", Name = "Corona Sueca", Country = "Suecia" },
            new() { Code = "SGD", Name = "Dolar Singapur", Country = "Singapur" },
            new() { Code = "THB", Name = "Baht Tailandes", Country = "Tailandia" },
            new() { Code = "TND", Name = "Dinar Tunecino", Country = "Tunez" },
            new() { Code = "TRY", Name = "Lira Turca", Country = "Turquia" },
            new() { Code = "ZAR", Name = "Rand Sudafricano", Country = "Sudafrica" }
        };
    }
}