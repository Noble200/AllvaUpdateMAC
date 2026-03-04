#if WINDOWS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using NTwain;
using NTwain.Data;

namespace Allva.Desktop.Services;

/// <summary>
/// Servicio para escanear documentos usando TWAIN y WIA (fallback).
/// Soporta escaneres por cable USB, WiFi y Bluetooth.
/// </summary>
[SupportedOSPlatform("windows")]
public class ScannerService : IDisposable
{
    private TwainSession? _session;
    private DataSource? _currentSource;
    private bool _isInitialized = false;
    private bool _isScanning = false;
    private TaskCompletionSource<byte[]?>? _scanCompletionSource;
    private CancellationTokenSource? _timeoutCts;

    /// <summary>
    /// Timeout para operaciones de escaneo (por defecto 120s para escaneres inalambricos)
    /// </summary>
    public int TimeoutSegundos { get; set; } = 120;

    /// <summary>
    /// Numero maximo de reintentos al fallar el escaneo
    /// </summary>
    public int MaxReintentos { get; set; } = 2;

    /// <summary>
    /// Tipo de conexion del escaner detectado
    /// </summary>
    public TipoConexionEscaner TipoConexion { get; private set; } = TipoConexionEscaner.Desconocido;

    /// <summary>
    /// Nombre del escaner seleccionado actualmente
    /// </summary>
    public string? NombreEscanerActual { get; private set; }

    /// <summary>
    /// Evento que se dispara cuando se escanea una imagen exitosamente
    /// </summary>
    public event EventHandler<byte[]>? ImagenEscaneada;

    /// <summary>
    /// Evento que se dispara cuando ocurre un error
    /// </summary>
    public event EventHandler<string>? ErrorOcurrido;

    /// <summary>
    /// Evento informativo sobre el estado del escaneo
    /// </summary>
    public event EventHandler<string>? EstadoCambiado;

    /// <summary>
    /// Indica si el servicio esta inicializado
    /// </summary>
    public bool EstaInicializado => _isInitialized;

    /// <summary>
    /// Indica si hay un escaneo en progreso
    /// </summary>
    public bool EstaEscaneando => _isScanning;

    /// <summary>
    /// Indica si hay soporte WIA disponible en el sistema
    /// </summary>
    public bool WiaDisponible => WiaScannerHelper.EstaDisponible();

    /// <summary>
    /// Inicializa la sesion TWAIN
    /// </summary>
    public bool Inicializar()
    {
        if (_isInitialized) return true;

        try
        {
            var appId = TWIdentity.CreateFromAssembly(
                DataGroups.Image,
                System.Reflection.Assembly.GetExecutingAssembly());

            _session = new TwainSession(appId);

            _session.TransferReady += OnTransferReady;
            _session.DataTransferred += OnDataTransferred;
            _session.TransferError += OnTransferError;
            _session.SourceDisabled += OnSourceDisabled;

            var rc = _session.Open();
            _isInitialized = (rc == ReturnCode.Success);

            if (!_isInitialized)
            {
                ErrorOcurrido?.Invoke(this, $"No se pudo inicializar TWAIN. Codigo: {rc}");
            }

            return _isInitialized;
        }
        catch (Exception ex)
        {
            ErrorOcurrido?.Invoke(this, $"Error inicializando escaner TWAIN: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verifica si hay escaneres disponibles (TWAIN + WIA)
    /// </summary>
    public bool HayEscaneresDisponibles()
    {
        return HayEscaneresTwain() || HayEscaneresWia();
    }

    /// <summary>
    /// Verifica si hay escaneres TWAIN disponibles
    /// </summary>
    public bool HayEscaneresTwain()
    {
        if (!_isInitialized || _session == null) return false;

        try
        {
            return _session.GetSources().Any();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScannerService] Error verificando TWAIN: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verifica si hay escaneres WIA disponibles
    /// </summary>
    public bool HayEscaneresWia()
    {
        try
        {
            return WiaScannerHelper.HayEscaneresDisponibles();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScannerService] Error verificando WIA: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene lista de todos los escaneres disponibles (TWAIN + WIA)
    /// </summary>
    public List<InfoEscaner> ObtenerTodosLosEscaneres()
    {
        var escaneres = new List<InfoEscaner>();

        // Escaneres TWAIN
        if (_isInitialized && _session != null)
        {
            try
            {
                foreach (var source in _session.GetSources())
                {
                    escaneres.Add(new InfoEscaner
                    {
                        Nombre = source.Name,
                        Protocolo = ProtocoloEscaner.TWAIN,
                        NombreCompleto = $"[TWAIN] {source.Name}"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScannerService] Error listando TWAIN: {ex.Message}");
            }
        }

        // Escaneres WIA
        try
        {
            var wiaEscaneres = WiaScannerHelper.ObtenerEscaneres();
            foreach (var wia in wiaEscaneres)
            {
                // Evitar duplicados (mismo escaner puede aparecer en TWAIN y WIA)
                if (!escaneres.Any(e => e.Nombre.Contains(wia.Nombre, StringComparison.OrdinalIgnoreCase)))
                {
                    escaneres.Add(wia);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScannerService] Error listando WIA: {ex.Message}");
        }

        return escaneres;
    }

    /// <summary>
    /// Obtiene lista de nombres de escaneres disponibles (compatibilidad)
    /// </summary>
    public List<string> ObtenerEscaneres()
    {
        return ObtenerTodosLosEscaneres().Select(e => e.NombreCompleto).ToList();
    }

    /// <summary>
    /// Selecciona un escaner por nombre
    /// </summary>
    public bool SeleccionarEscaner(string nombreEscaner)
    {
        if (!_isInitialized || _session == null) return false;

        try
        {
            CerrarSourceActual();

            _currentSource = _session.GetSources()
                .FirstOrDefault(s => s.Name == nombreEscaner);

            if (_currentSource == null)
            {
                ErrorOcurrido?.Invoke(this, $"Escaner no encontrado: {nombreEscaner}");
                return false;
            }

            var rc = _currentSource.Open();
            if (rc != ReturnCode.Success)
            {
                ErrorOcurrido?.Invoke(this, $"No se pudo abrir el escaner. Codigo: {rc}");
                _currentSource = null;
                return false;
            }

            NombreEscanerActual = nombreEscaner;
            DetectarTipoConexion(nombreEscaner);
            return true;
        }
        catch (Exception ex)
        {
            ErrorOcurrido?.Invoke(this, $"Error seleccionando escaner: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Selecciona el escaner predeterminado del sistema
    /// </summary>
    public bool SeleccionarEscanerPredeterminado()
    {
        if (!_isInitialized || _session == null) return false;

        try
        {
            CerrarSourceActual();

            _currentSource = _session.DefaultSource;

            if (_currentSource == null)
            {
                _currentSource = _session.GetSources().FirstOrDefault();
            }

            if (_currentSource == null)
            {
                ErrorOcurrido?.Invoke(this, "No se encontraron escaneres TWAIN disponibles");
                return false;
            }

            var rc = _currentSource.Open();
            if (rc != ReturnCode.Success)
            {
                ErrorOcurrido?.Invoke(this, $"No se pudo abrir el escaner. Codigo: {rc}");
                _currentSource = null;
                return false;
            }

            NombreEscanerActual = _currentSource.Name;
            DetectarTipoConexion(_currentSource.Name);
            return true;
        }
        catch (Exception ex)
        {
            ErrorOcurrido?.Invoke(this, $"Error seleccionando escaner: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Inicia el escaneo de un documento
    /// </summary>
    public bool Escanear(bool mostrarUI = true)
    {
        if (_currentSource == null)
        {
            ErrorOcurrido?.Invoke(this, "No hay escaner seleccionado");
            return false;
        }

        if (_isScanning)
        {
            ErrorOcurrido?.Invoke(this, "Ya hay un escaneo en progreso");
            return false;
        }

        try
        {
            _isScanning = true;
            EstadoCambiado?.Invoke(this, "Iniciando escaneo...");

            var mode = mostrarUI ? SourceEnableMode.ShowUI : SourceEnableMode.NoUI;
            var rc = _currentSource.Enable(mode, false, IntPtr.Zero);

            if (rc != ReturnCode.Success && rc != ReturnCode.CheckStatus)
            {
                _isScanning = false;
                ErrorOcurrido?.Invoke(this, $"Error al iniciar escaneo. Codigo: {rc}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _isScanning = false;
            ErrorOcurrido?.Invoke(this, $"Error al escanear: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Escanea de forma asincrona con timeout y reintentos.
    /// Intenta TWAIN primero; si no hay escaneres TWAIN, usa WIA como fallback.
    /// </summary>
    public async Task<byte[]?> EscanearAsync(bool mostrarUI = true, CancellationToken cancellationToken = default)
    {
        // Si hay un source TWAIN seleccionado, usar TWAIN con reintentos
        if (_currentSource != null)
        {
            return await EscanearTwainConReintentosAsync(mostrarUI, cancellationToken);
        }

        // Si no hay TWAIN pero hay WIA, usar WIA
        if (HayEscaneresWia())
        {
            EstadoCambiado?.Invoke(this, "Usando escaner WIA...");
            return await EscanearWiaAsync(cancellationToken);
        }

        ErrorOcurrido?.Invoke(this, "No hay escaneres disponibles (ni TWAIN ni WIA)");
        return null;
    }

    /// <summary>
    /// Escanea usando TWAIN con timeout y reintentos automaticos
    /// </summary>
    private async Task<byte[]?> EscanearTwainConReintentosAsync(bool mostrarUI, CancellationToken cancellationToken)
    {
        for (int intento = 0; intento <= MaxReintentos; intento++)
        {
            if (cancellationToken.IsCancellationRequested) return null;

            if (intento > 0)
            {
                EstadoCambiado?.Invoke(this, $"Reintentando escaneo ({intento}/{MaxReintentos})...");

                // Esperar un momento antes de reintentar (los inalambricos necesitan tiempo)
                await Task.Delay(2000, cancellationToken);

                // Reinicializar la conexion con el escaner
                if (!ReconectarEscaner())
                {
                    continue;
                }
            }

            var resultado = await EscanearTwainConTimeoutAsync(mostrarUI, cancellationToken);
            if (resultado != null && resultado.Length > 0)
            {
                return resultado;
            }

            // Si el usuario cancelo desde la UI del escaner, no reintentar
            if (!_isScanning && intento == 0)
            {
                return null;
            }
        }

        ErrorOcurrido?.Invoke(this,
            "No se pudo completar el escaneo tras varios intentos. " +
            "Verifique que el escaner esta encendido y conectado.");
        return null;
    }

    /// <summary>
    /// Escanea usando TWAIN con timeout
    /// </summary>
    private async Task<byte[]?> EscanearTwainConTimeoutAsync(bool mostrarUI, CancellationToken cancellationToken)
    {
        _scanCompletionSource = new TaskCompletionSource<byte[]?>();
        _timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timeoutCts.CancelAfter(TimeSpan.FromSeconds(TimeoutSegundos));

        using var registration = _timeoutCts.Token.Register(() =>
        {
            if (_isScanning)
            {
                EstadoCambiado?.Invoke(this, "Tiempo de espera agotado");
                _scanCompletionSource?.TrySetResult(null);
                _isScanning = false;
            }
        });

        if (!Escanear(mostrarUI))
        {
            return null;
        }

        try
        {
            return await _scanCompletionSource.Task;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            _timeoutCts?.Dispose();
            _timeoutCts = null;
        }
    }

    /// <summary>
    /// Escanea usando WIA (Windows Image Acquisition) - fallback para inalambricos
    /// </summary>
    private async Task<byte[]?> EscanearWiaAsync(CancellationToken cancellationToken)
    {
        try
        {
            _isScanning = true;
            EstadoCambiado?.Invoke(this, "Abriendo escaner WIA...");

            var resultado = await Task.Run(() =>
            {
                return WiaScannerHelper.Escanear();
            }, cancellationToken);

            if (resultado != null && resultado.Length > 0)
            {
                ImagenEscaneada?.Invoke(this, resultado);
                EstadoCambiado?.Invoke(this, "Escaneo WIA completado");
            }

            return resultado;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            ErrorOcurrido?.Invoke(this, $"Error en escaneo WIA: {ex.Message}");
            return null;
        }
        finally
        {
            _isScanning = false;
        }
    }

    /// <summary>
    /// Intenta reconectar con el escaner actual (util para inalambricos)
    /// </summary>
    public bool ReconectarEscaner()
    {
        var nombreAnterior = NombreEscanerActual;

        try
        {
            EstadoCambiado?.Invoke(this, "Reconectando con el escaner...");
            CerrarSourceActual();

            // Dar tiempo al escaner inalambrico para estabilizar la conexion
            Thread.Sleep(1000);

            if (nombreAnterior != null)
            {
                return SeleccionarEscaner(nombreAnterior);
            }
            else
            {
                return SeleccionarEscanerPredeterminado();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScannerService] Error reconectando: {ex.Message}");
            ErrorOcurrido?.Invoke(this, $"Error al reconectar: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verifica si el escaner actual sigue accesible
    /// </summary>
    public bool VerificarConexion()
    {
        if (_currentSource == null) return false;

        try
        {
            // Verificar que el source sigue en la lista de disponibles
            if (_session == null) return false;
            var sources = _session.GetSources().ToList();
            return sources.Any(s => s.Name == NombreEscanerActual);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Cancela el escaneo actual si hay uno en progreso
    /// </summary>
    public void CancelarEscaneo()
    {
        if (_isScanning)
        {
            try
            {
                _timeoutCts?.Cancel();
                _currentSource?.Close();
                _isScanning = false;
                _scanCompletionSource?.TrySetResult(null);
                EstadoCambiado?.Invoke(this, "Escaneo cancelado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScannerService] Error cancelando: {ex.Message}");
            }
        }
    }

    private void OnTransferReady(object? sender, TransferReadyEventArgs e)
    {
        EstadoCambiado?.Invoke(this, "Recibiendo imagen del escaner...");
    }

    private void OnDataTransferred(object? sender, DataTransferredEventArgs e)
    {
        try
        {
            byte[]? imageBytes = null;

            if (e.NativeData != IntPtr.Zero)
            {
                using var stream = e.GetNativeImageStream();
                if (stream != null)
                {
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    imageBytes = ms.ToArray();
                }
            }

            if (imageBytes != null && imageBytes.Length > 0)
            {
                EstadoCambiado?.Invoke(this, "Imagen escaneada correctamente");
                ImagenEscaneada?.Invoke(this, imageBytes);
                _scanCompletionSource?.TrySetResult(imageBytes);
            }
            else
            {
                ErrorOcurrido?.Invoke(this, "No se pudo obtener la imagen escaneada");
                _scanCompletionSource?.TrySetResult(null);
            }
        }
        catch (Exception ex)
        {
            ErrorOcurrido?.Invoke(this, $"Error procesando imagen: {ex.Message}");
            _scanCompletionSource?.TrySetResult(null);
        }
        finally
        {
            _isScanning = false;
        }
    }

    private void OnTransferError(object? sender, TransferErrorEventArgs e)
    {
        _isScanning = false;
        var mensaje = e.Exception?.Message ?? "Error desconocido en transferencia";
        System.Diagnostics.Debug.WriteLine($"[ScannerService] TransferError: {mensaje}");
        ErrorOcurrido?.Invoke(this, $"Error en transferencia: {mensaje}");
        _scanCompletionSource?.TrySetResult(null);
    }

    private void OnSourceDisabled(object? sender, EventArgs e)
    {
        _isScanning = false;
        // Si no se obtuvo resultado, completar con null (usuario cancelo desde UI del escaner)
        _scanCompletionSource?.TrySetResult(null);
    }

    private void CerrarSourceActual()
    {
        if (_currentSource != null)
        {
            try
            {
                _currentSource.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScannerService] Error cerrando source: {ex.Message}");
            }
            _currentSource = null;
        }
    }

    /// <summary>
    /// Detecta el tipo de conexion del escaner basandose en su nombre
    /// </summary>
    private void DetectarTipoConexion(string nombreEscaner)
    {
        var nombre = nombreEscaner.ToUpperInvariant();

        if (nombre.Contains("BLUETOOTH") || nombre.Contains("BT ") || nombre.Contains("(BT)"))
        {
            TipoConexion = TipoConexionEscaner.Bluetooth;
        }
        else if (nombre.Contains("WIFI") || nombre.Contains("WI-FI") || nombre.Contains("WIRELESS") ||
                 nombre.Contains("NETWORK") || nombre.Contains("NET ") || nombre.Contains("INALAMBRICO") ||
                 nombre.Contains("INALÁMBRICO"))
        {
            TipoConexion = TipoConexionEscaner.WiFi;
        }
        else if (nombre.Contains("USB"))
        {
            TipoConexion = TipoConexionEscaner.USB;
        }
        else
        {
            TipoConexion = TipoConexionEscaner.Desconocido;
        }

        // Para escaneres inalambricos, usar timeout mas largo
        if (TipoConexion == TipoConexionEscaner.Bluetooth || TipoConexion == TipoConexionEscaner.WiFi)
        {
            TimeoutSegundos = 180; // 3 minutos para inalambricos
            MaxReintentos = 3;
        }
    }

    /// <summary>
    /// Cierra el escaner actual
    /// </summary>
    public void CerrarEscaner()
    {
        CerrarSourceActual();
        NombreEscanerActual = null;
        TipoConexion = TipoConexionEscaner.Desconocido;
    }

    /// <summary>
    /// Libera todos los recursos
    /// </summary>
    public void Dispose()
    {
        CancelarEscaneo();
        CerrarSourceActual();

        if (_session != null)
        {
            try
            {
                _session.TransferReady -= OnTransferReady;
                _session.DataTransferred -= OnDataTransferred;
                _session.TransferError -= OnTransferError;
                _session.SourceDisabled -= OnSourceDisabled;
                _session.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScannerService] Error en Dispose: {ex.Message}");
            }
            _session = null;
        }

        _timeoutCts?.Dispose();
        _timeoutCts = null;
        _isInitialized = false;
        _isScanning = false;
        NombreEscanerActual = null;
    }
}

/// <summary>
/// Tipos de conexion de escaner
/// </summary>
public enum TipoConexionEscaner
{
    Desconocido,
    USB,
    WiFi,
    Bluetooth
}

/// <summary>
/// Protocolos de escaneo soportados
/// </summary>
public enum ProtocoloEscaner
{
    TWAIN,
    WIA
}

/// <summary>
/// Informacion de un escaner detectado
/// </summary>
public class InfoEscaner
{
    public string Nombre { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public ProtocoloEscaner Protocolo { get; set; }
    public string? DeviceId { get; set; }
}

/// <summary>
/// Helper para escaneo WIA (Windows Image Acquisition).
/// Muchos escaneres inalambricos (WiFi/Bluetooth) solo exponen interfaz WIA, no TWAIN.
/// Usa COM interop con wiaaut.dll que viene preinstalado en Windows.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WiaScannerHelper
{
    private const string WIA_DEVICE_MANAGER_PROGID = "WIA.DeviceManager";
    private const string WIA_COMMON_DIALOG_PROGID = "WIA.CommonDialog";
    private const int WIA_DEVICE_TYPE_SCANNER = 1;
    private const string WIA_FORMAT_BMP = "{B96B3CAB-0728-11D3-9D7B-0000F81EF32E}";

    /// <summary>
    /// Verifica si WIA esta disponible en el sistema
    /// </summary>
    public static bool EstaDisponible()
    {
        try
        {
            var type = Type.GetTypeFromProgID(WIA_DEVICE_MANAGER_PROGID);
            return type != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifica si hay escaneres WIA disponibles
    /// </summary>
    public static bool HayEscaneresDisponibles()
    {
        try
        {
            var type = Type.GetTypeFromProgID(WIA_DEVICE_MANAGER_PROGID);
            if (type == null) return false;

            dynamic manager = Activator.CreateInstance(type)!;
            dynamic devices = manager.DeviceInfos;
            int count = devices.Count;

            for (int i = 1; i <= count; i++)
            {
                dynamic deviceInfo = devices.Item(i);
                if ((int)deviceInfo.Type == WIA_DEVICE_TYPE_SCANNER)
                {
                    Marshal.ReleaseComObject(deviceInfo);
                    Marshal.ReleaseComObject(devices);
                    Marshal.ReleaseComObject(manager);
                    return true;
                }
                Marshal.ReleaseComObject(deviceInfo);
            }

            Marshal.ReleaseComObject(devices);
            Marshal.ReleaseComObject(manager);
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WIA] Error verificando disponibilidad: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene lista de escaneres WIA disponibles
    /// </summary>
    public static List<InfoEscaner> ObtenerEscaneres()
    {
        var resultado = new List<InfoEscaner>();

        try
        {
            var type = Type.GetTypeFromProgID(WIA_DEVICE_MANAGER_PROGID);
            if (type == null) return resultado;

            dynamic manager = Activator.CreateInstance(type)!;
            dynamic devices = manager.DeviceInfos;
            int count = devices.Count;

            for (int i = 1; i <= count; i++)
            {
                dynamic deviceInfo = devices.Item(i);
                if ((int)deviceInfo.Type == WIA_DEVICE_TYPE_SCANNER)
                {
                    string nombre = "";
                    string deviceId = "";
                    try
                    {
                        // Leer propiedades del dispositivo
                        dynamic props = deviceInfo.Properties;
                        nombre = ObtenerPropiedad(props, "Name") ?? $"Escaner WIA {i}";
                        deviceId = ObtenerPropiedad(props, "Unique Device ID") ?? "";
                        Marshal.ReleaseComObject(props);
                    }
                    catch
                    {
                        nombre = $"Escaner WIA {i}";
                    }

                    resultado.Add(new InfoEscaner
                    {
                        Nombre = nombre,
                        NombreCompleto = $"[WIA] {nombre}",
                        Protocolo = ProtocoloEscaner.WIA,
                        DeviceId = deviceId
                    });
                }
                Marshal.ReleaseComObject(deviceInfo);
            }

            Marshal.ReleaseComObject(devices);
            Marshal.ReleaseComObject(manager);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WIA] Error listando escaneres: {ex.Message}");
        }

        return resultado;
    }

    /// <summary>
    /// Escanea usando el dialogo WIA del sistema.
    /// Muestra la UI nativa de Windows para seleccionar escaner y configurar.
    /// </summary>
    public static byte[]? Escanear()
    {
        try
        {
            var dialogType = Type.GetTypeFromProgID(WIA_COMMON_DIALOG_PROGID);
            if (dialogType == null)
            {
                System.Diagnostics.Debug.WriteLine("[WIA] CommonDialog no disponible");
                return null;
            }

            dynamic dialog = Activator.CreateInstance(dialogType)!;

            // ShowAcquireImage muestra la UI nativa de Windows para escanear
            // Parametros: DeviceType (Scanner=1), Intent (Color=1), Bias (MaxQuality=131072),
            //             FormatID (BMP), AlwaysSelectDevice, UseCommonUI, CancelError
            dynamic? image = dialog.ShowAcquireImage(
                WIA_DEVICE_TYPE_SCANNER, // Solo escaneres
                1,                        // Color
                131072,                   // MaxQuality
                WIA_FORMAT_BMP,          // Formato BMP
                false,                    // No forzar seleccion si solo hay uno
                true,                     // Usar UI comun de Windows
                false                     // No lanzar excepcion al cancelar
            );

            if (image == null)
            {
                Marshal.ReleaseComObject(dialog);
                return null;
            }

            // Obtener los bytes de la imagen
            dynamic fileData = image.FileData;
            byte[] bytes = (byte[])fileData.BinaryData;

            Marshal.ReleaseComObject(image);
            Marshal.ReleaseComObject(dialog);

            return bytes;
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x80210006))
        {
            // WIA_ERROR_OFFLINE - Escaner no disponible/desconectado
            System.Diagnostics.Debug.WriteLine("[WIA] Escaner desconectado (offline)");
            return null;
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x80210001))
        {
            // WIA_ERROR_GENERAL - Error general
            System.Diagnostics.Debug.WriteLine($"[WIA] Error general: {ex.Message}");
            return null;
        }
        catch (COMException ex) when (ex.HResult == unchecked((int)0x80210064))
        {
            // Usuario cancelo el dialogo
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WIA] Error escaneando: {ex.Message}");
            return null;
        }
    }

    private static string? ObtenerPropiedad(dynamic properties, string nombre)
    {
        try
        {
            int count = properties.Count;
            for (int i = 1; i <= count; i++)
            {
                dynamic prop = properties.Item(i);
                string propName = prop.Name;
                if (propName == nombre)
                {
                    string value = prop.Value?.ToString() ?? "";
                    Marshal.ReleaseComObject(prop);
                    return value;
                }
                Marshal.ReleaseComObject(prop);
            }
        }
        catch
        {
            // Ignorar errores al leer propiedades
        }
        return null;
    }
}
#endif
