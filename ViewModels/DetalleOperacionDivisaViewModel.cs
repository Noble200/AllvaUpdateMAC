using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels;

public partial class DetalleOperacionDivisaViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";
    
    private readonly string _numeroOperacion;
    private readonly string _codigoLocal;
    
    // Datos de la operacion
    [ObservableProperty]
    private string numeroOperacionDisplay = "";
    
    [ObservableProperty]
    private string fechaOperacion = "";
    
    [ObservableProperty]
    private string horaOperacion = "";
    
    [ObservableProperty]
    private string usuarioOperacion = "";
    
    // Datos del cliente
    [ObservableProperty]
    private string clienteNombre = "";
    
    [ObservableProperty]
    private string clienteTipoDocumento = "";
    
    [ObservableProperty]
    private string clienteNumeroDocumento = "";
    
    [ObservableProperty]
    private string clienteTelefono = "";
    
    [ObservableProperty]
    private string clienteDireccion = "";
    
    [ObservableProperty]
    private string clienteNacionalidad = "";
    
    // Datos de la divisa
    [ObservableProperty]
    private string divisaCodigo = "";
    
    [ObservableProperty]
    private string divisaNombre = "";
    
    [ObservableProperty]
    private string cantidadDivisa = "";
    
    [ObservableProperty]
    private string tasaCambio = "";
    
    [ObservableProperty]
    private string totalEuros = "";
    
    // Estados
    [ObservableProperty]
    private bool isLoading = false;
    
    [ObservableProperty]
    private string errorMessage = "";
    
    [ObservableProperty]
    private string successMessage = "";
    
    [ObservableProperty]
    private bool datosValidos = false;
    
    // Datos para reimprimir
    private ClienteParaRecibo? _clienteRecibo;
    private DateTime _fechaHoraOperacion;
    private decimal _cantidadRecibida;
    private decimal _tasaCambioValor;
    private decimal _totalEntregado;
    
    // Evento para cerrar ventana
    public event Action? SolicitarCierre;
    
    public DetalleOperacionDivisaViewModel()
    {
        _numeroOperacion = "";
        _codigoLocal = "";
    }
    
    public DetalleOperacionDivisaViewModel(string numeroOperacion, string codigoLocal)
    {
        _numeroOperacion = numeroOperacion;
        _codigoLocal = codigoLocal;
        NumeroOperacionDisplay = numeroOperacion;
        _ = CargarDatosOperacionAsync();
    }
    
    private async Task CargarDatosOperacionAsync()
    {
        if (string.IsNullOrEmpty(_numeroOperacion)) return;
        
        try
        {
            IsLoading = true;
            ErrorMessage = "";
            
            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Obtener id_local a partir del codigo_local
            int? idLocal = null;
            if (!string.IsNullOrEmpty(_codigoLocal))
            {
                var sqlLocal = "SELECT id_local FROM locales WHERE codigo_local = @codigo";
                await using var cmdLocal = new NpgsqlCommand(sqlLocal, conn);
                cmdLocal.Parameters.AddWithValue("codigo", _codigoLocal);
                var result = await cmdLocal.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    idLocal = Convert.ToInt32(result);
            }

            // Primero buscar la operacion basica
            var sqlOperacion = @"SELECT
                                    o.id_operacion,
                                    o.numero_operacion,
                                    o.fecha_operacion,
                                    o.hora_operacion,
                                    o.id_usuario,
                                    o.id_cliente
                                FROM operaciones o
                                WHERE o.numero_operacion = @numOp";

            if (idLocal.HasValue)
                sqlOperacion += " AND o.id_local = @idLocal";

            int idOperacion = 0;
            int? idUsuario = null;
            int? idCliente = null;

            await using (var cmdOp = new NpgsqlCommand(sqlOperacion, conn))
            {
                cmdOp.Parameters.AddWithValue("numOp", _numeroOperacion);
                if (idLocal.HasValue)
                    cmdOp.Parameters.AddWithValue("idLocal", idLocal.Value);
                await using var readerOp = await cmdOp.ExecuteReaderAsync();
                
                if (await readerOp.ReadAsync())
                {
                    idOperacion = readerOp.GetInt32(0);
                    NumeroOperacionDisplay = readerOp.IsDBNull(1) ? _numeroOperacion : readerOp.GetString(1);
                    
                    if (!readerOp.IsDBNull(2))
                    {
                        var fechaDb = readerOp.GetDateTime(2);
                        FechaOperacion = fechaDb.ToString("dd/MM/yyyy");
                        _fechaHoraOperacion = fechaDb;
                    }
                    else
                    {
                        FechaOperacion = "-";
                    }
                    
                    if (!readerOp.IsDBNull(3))
                    {
                        var horaDb = readerOp.GetTimeSpan(3);
                        HoraOperacion = horaDb.ToString(@"hh\:mm\:ss");
                        _fechaHoraOperacion = _fechaHoraOperacion.Date.Add(horaDb);
                    }
                    else
                    {
                        HoraOperacion = "-";
                    }
                    
                    idUsuario = readerOp.IsDBNull(4) ? null : readerOp.GetInt32(4);
                    idCliente = readerOp.IsDBNull(5) ? null : readerOp.GetInt32(5);
                }
                else
                {
                    ErrorMessage = "No se encontro la operacion";
                    DatosValidos = false;
                    return;
                }
            }
            
            // Buscar datos del usuario
            if (idUsuario.HasValue)
            {
                var sqlUsuario = "SELECT nombre, apellidos FROM usuarios WHERE id_usuario = @id";
                await using var cmdUsr = new NpgsqlCommand(sqlUsuario, conn);
                cmdUsr.Parameters.AddWithValue("id", idUsuario.Value);
                await using var readerUsr = await cmdUsr.ExecuteReaderAsync();

                if (await readerUsr.ReadAsync())
                {
                    var nombre = readerUsr.IsDBNull(0) ? "" : readerUsr.GetString(0);
                    var apellidos = readerUsr.IsDBNull(1) ? "" : readerUsr.GetString(1);
                    UsuarioOperacion = string.IsNullOrWhiteSpace(nombre) && string.IsNullOrWhiteSpace(apellidos)
                        ? "-"
                        : $"{nombre} {apellidos}".Trim();
                }
            }
            
            if (string.IsNullOrEmpty(UsuarioOperacion))
                UsuarioOperacion = "-";
            
            // Buscar datos del cliente
            if (idCliente.HasValue)
            {
                var sqlCliente = @"SELECT nombre, segundo_nombre, apellidos, segundo_apellido,
                                          documento_tipo, documento_numero, telefono, direccion, nacionalidad
                                   FROM clientes WHERE id_cliente = @id";
                await using var cmdCli = new NpgsqlCommand(sqlCliente, conn);
                cmdCli.Parameters.AddWithValue("id", idCliente.Value);
                await using var readerCli = await cmdCli.ExecuteReaderAsync();
                
                if (await readerCli.ReadAsync())
                {
                    var nombre = readerCli.IsDBNull(0) ? "" : readerCli.GetString(0);
                    var segundoNombre = readerCli.IsDBNull(1) ? "" : readerCli.GetString(1);
                    var apellidos = readerCli.IsDBNull(2) ? "" : readerCli.GetString(2);
                    var segundoApellido = readerCli.IsDBNull(3) ? "" : readerCli.GetString(3);
                    
                    var partesNombre = new[] { nombre, segundoNombre, apellidos, segundoApellido }
                        .Where(s => !string.IsNullOrWhiteSpace(s));
                    ClienteNombre = string.Join(" ", partesNombre);
                    
                    ClienteTipoDocumento = readerCli.IsDBNull(4) ? "-" : readerCli.GetString(4);
                    ClienteNumeroDocumento = readerCli.IsDBNull(5) ? "-" : readerCli.GetString(5);
                    ClienteTelefono = readerCli.IsDBNull(6) ? "-" : readerCli.GetString(6);
                    ClienteDireccion = readerCli.IsDBNull(7) ? "-" : readerCli.GetString(7);
                    ClienteNacionalidad = readerCli.IsDBNull(8) ? "-" : readerCli.GetString(8);
                }
            }
            
            if (string.IsNullOrEmpty(ClienteNombre))
                ClienteNombre = "Cliente no disponible";
            
            // Buscar datos de la divisa
            var sqlDivisa = @"SELECT divisa_origen, cantidad_origen, cantidad_destino, tipo_cambio_aplicado
                              FROM operaciones_divisas WHERE id_operacion = @id";
            await using var cmdDiv = new NpgsqlCommand(sqlDivisa, conn);
            cmdDiv.Parameters.AddWithValue("id", idOperacion);
            await using var readerDiv = await cmdDiv.ExecuteReaderAsync();
            
            if (await readerDiv.ReadAsync())
            {
                DivisaCodigo = readerDiv.IsDBNull(0) ? "-" : readerDiv.GetString(0);
                _cantidadRecibida = readerDiv.IsDBNull(1) ? 0 : readerDiv.GetDecimal(1);
                _totalEntregado = readerDiv.IsDBNull(2) ? 0 : readerDiv.GetDecimal(2);
                _tasaCambioValor = readerDiv.IsDBNull(3) ? 0 : readerDiv.GetDecimal(3);
                
                CantidadDivisa = $"{_cantidadRecibida:N2} {DivisaCodigo}";
                TasaCambio = _tasaCambioValor > 0 ? $"1 {DivisaCodigo} = {_tasaCambioValor:N4} EUR" : "-";
                TotalEuros = $"{_totalEntregado:N2} EUR";
                
                DivisaNombre = ObtenerNombreDivisa(DivisaCodigo);
            }
            else
            {
                DivisaCodigo = "-";
                CantidadDivisa = "-";
                TasaCambio = "-";
                TotalEuros = "-";
                DivisaNombre = "-";
            }
            
            // Preparar datos para reimprimir
            _clienteRecibo = new ClienteParaRecibo
            {
                NombreCompleto = ClienteNombre,
                TipoDocumento = ClienteTipoDocumento ?? "-",
                NumeroDocumento = ClienteNumeroDocumento ?? "-",
                DocumentoCompleto = $"{ClienteTipoDocumento ?? "-"}: {ClienteNumeroDocumento ?? "-"}",
                Telefono = ClienteTelefono ?? "-",
                Direccion = ClienteDireccion ?? "-",
                Nacionalidad = ClienteNacionalidad ?? "-"
            };
            
            // Datos validos si al menos tenemos la operacion
            DatosValidos = idOperacion > 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar: {ex.Message}";
            DatosValidos = false;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    private string ObtenerNombreDivisa(string codigo)
    {
        return codigo switch
        {
            "USD" => "Dolar Estadounidense",
            "GBP" => "Libra Esterlina",
            "CHF" => "Franco Suizo",
            "CAD" => "Dolar Canadiense",
            "CNY" => "Yuan Chino",
            "PEN" => "Sol Peruano",
            "MXN" => "Peso Mexicano",
            "COP" => "Peso Colombiano",
            "ARS" => "Peso Argentino",
            "BRL" => "Real Brasileno",
            "CLP" => "Peso Chileno",
            "BOB" => "Boliviano",
            "VES" => "Bolivar Venezolano",
            "DOP" => "Peso Dominicano",
            _ => codigo
        };
    }
    
    [RelayCommand]
    private async Task ReimprimirComprobanteAsync()
    {
        if (!DatosValidos || _clienteRecibo == null)
        {
            ErrorMessage = "No hay datos validos para reimprimir";
            await Task.Delay(2000);
            ErrorMessage = "";
            return;
        }
        
        try
        {
            IsLoading = true;
            ErrorMessage = "";
            
            // Obtener ventana principal
            Window? mainWindow = null;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                mainWindow = desktop.MainWindow;
            }
            
            if (mainWindow == null)
            {
                ErrorMessage = "No se pudo abrir el dialogo";
                await Task.Delay(2000);
                ErrorMessage = "";
                return;
            }
            
            // Nombre sugerido
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var nombreSugerido = $"Recibo_{NumeroOperacionDisplay}_{timestamp}.pdf";
            
            // Dialogo para guardar
            var storageProvider = mainWindow.StorageProvider;
            var archivo = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Guardar comprobante",
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

            // Generar PDF en hilo separado para no bloquear la UI
            var pdfService = new ReciboDivisasPdfService();
            var pdfBytes = await Task.Run(() => pdfService.GenerarReciboPdfBytes(
                numeroOperacion: NumeroOperacionDisplay,
                fechaOperacion: _fechaHoraOperacion,
                cliente: _clienteRecibo,
                divisaOrigen: DivisaCodigo,
                nombreDivisa: DivisaNombre,
                cantidadRecibida: _cantidadRecibida,
                tasaCambio: _tasaCambioValor,
                totalEntregado: _totalEntregado,
                codigoLocal: _codigoLocal,
                esReimpresion: true
            ));

            // Guardar
            await using var stream = await archivo.OpenWriteAsync();
            await stream.WriteAsync(pdfBytes);
            
            SuccessMessage = "Comprobante guardado correctamente";
            await Task.Delay(2000);
            SuccessMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            await Task.Delay(3000);
            ErrorMessage = "";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private void Cerrar()
    {
        SolicitarCierre?.Invoke();
    }
}

// Clase auxiliar para el recibo
public class ClienteParaRecibo
{
    public string NombreCompleto { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string DocumentoCompleto { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Nacionalidad { get; set; } = "";
}