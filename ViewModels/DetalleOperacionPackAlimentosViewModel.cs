using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Npgsql;
using Allva.Desktop.Services;

namespace Allva.Desktop.ViewModels;

public partial class DetalleOperacionPackAlimentosViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    private readonly string _numeroOperacion;
    private readonly string _codigoLocal;
    private readonly string _nombreUsuario;
    private readonly int _numeroUsuario;
    private Window? _ventana;

    // Datos de la operación
    [ObservableProperty] private string numeroOperacion = "";
    [ObservableProperty] private string fechaOperacion = "";
    [ObservableProperty] private string horaOperacion = "";
    [ObservableProperty] private string usuarioOperacion = "";

    // Datos del cliente
    [ObservableProperty] private string clienteNombre = "";
    [ObservableProperty] private string clienteDocumento = "";
    [ObservableProperty] private string clienteTelefono = "";

    // Datos del beneficiario (editables)
    [ObservableProperty] private string beneficiarioNombre = "";
    [ObservableProperty] private string beneficiarioDocumento = "";
    [ObservableProperty] private string beneficiarioDireccion = "";
    [ObservableProperty] private string beneficiarioTelefono = "";
    [ObservableProperty] private string beneficiarioPais = "";
    [ObservableProperty] private string beneficiarioCiudad = "";

    // Campos editables del beneficiario
    [ObservableProperty] private string editBeneficiarioNombre = "";
    [ObservableProperty] private string editBeneficiarioApellido = "";
    [ObservableProperty] private string editBeneficiarioTipoDoc = "";
    [ObservableProperty] private string editBeneficiarioNumDoc = "";
    [ObservableProperty] private string editBeneficiarioTelefono = "";
    [ObservableProperty] private string editBeneficiarioDireccion = "";
    [ObservableProperty] private string editBeneficiarioPais = "";
    [ObservableProperty] private string editBeneficiarioCiudad = "";

    // Datos del pack (para compatibilidad con PDF cuando hay un solo artículo)
    [ObservableProperty] private string packNombre = "";
    [ObservableProperty] private string packDescripcion = "";
    [ObservableProperty] private string packProductos = "";

    // Lista de artículos de la operación (para mostrar desglose cuando hay múltiples packs)
    public ObservableCollection<ArticuloPackItem> ArticulosPack { get; } = new();

    // Indica si hay múltiples artículos en la operación
    public bool TieneMultiplesArticulos => ArticulosPack.Count > 1;
    public bool TieneUnSoloArticulo => ArticulosPack.Count == 1;

    // Totales
    [ObservableProperty] private string importeTotal = "";
    [ObservableProperty] private string moneda = "EUR";
    [ObservableProperty] private string estadoEnvio = "";
    [ObservableProperty] private string estadoColor = "#6c757d";

    // Estado
    [ObservableProperty] private bool estaCargando = false;
    [ObservableProperty] private string mensaje = "";
    [ObservableProperty] private bool esMensajeError = false;

    // Control de edición
    [ObservableProperty] private bool modoEdicion = false;
    [ObservableProperty] private bool puedeEditar = false;
    [ObservableProperty] private bool puedeAnular = false;

    // Historial de estados
    public ObservableCollection<HistorialEstadoItem> HistorialEstados { get; } = new();

    public bool TieneHistorial => HistorialEstados.Count > 0;

    public string MensajeColor => EsMensajeError ? "#dc3545" : "#28a745";

    // Indica si la operación está anulada (para mostrar botón de reimprimir anulación)
    public bool EstaAnulado => EstadoEnvio.ToUpper() == "ANULADO";

    partial void OnEsMensajeErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(MensajeColor));
    }

    partial void OnEstadoEnvioChanged(string value)
    {
        // Actualizar permisos cuando cambia el estado
        // Comparar con valores exactos de BD (PENDIENTE, PAGADO, ENVIADO, ANULADO)
        var estadoUpper = value.ToUpper();
        var esPendienteOPagado = estadoUpper == "PENDIENTE" || estadoUpper == "PAGADO";
        PuedeEditar = esPendienteOPagado;
        PuedeAnular = esPendienteOPagado;
        OnPropertyChanged(nameof(EstaAnulado));
    }

    // Datos internos para PDF
    private ReciboFoodPackService.DatosReciboFoodPack? _datosRecibo;
    private long _idOperacion;
    private int _idBeneficiario;

    // Datos originales del beneficiario para detectar cambios
    private string _benefNombreOriginal = "";
    private string _benefApellidoOriginal = "";
    private string _benefTipoDocOriginal = "";
    private string _benefNumDocOriginal = "";
    private string _benefTelefonoOriginal = "";
    private string _benefDireccionOriginal = "";
    private string _benefPaisOriginal = "";
    private string _benefCiudadOriginal = "";

    public DetalleOperacionPackAlimentosViewModel(string numeroOperacion, string codigoLocal, string nombreUsuario, int numeroUsuario)
    {
        _numeroOperacion = numeroOperacion;
        _codigoLocal = codigoLocal;
        _nombreUsuario = nombreUsuario;
        _numeroUsuario = numeroUsuario;
        NumeroOperacion = numeroOperacion;

        // Notificar cuando cambia el historial
        HistorialEstados.CollectionChanged += (s, e) => OnPropertyChanged(nameof(TieneHistorial));
    }

    public void SetVentana(Window ventana)
    {
        _ventana = ventana;
    }

    public async Task CargarDatosAsync()
    {
        try
        {
            EstaCargando = true;
            Mensaje = "";

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            var query = @"
                SELECT
                    o.id_operacion,
                    o.fecha_operacion,
                    o.hora_operacion,
                    o.importe_total,
                    o.moneda,
                    u.nombre as usuario_nombre,
                    u.apellidos as usuario_apellido,
                    c.nombre as cliente_nombre,
                    c.apellidos as cliente_apellido,
                    c.segundo_nombre as cliente_segundo_nombre,
                    c.segundo_apellido as cliente_segundo_apellido,
                    c.documento_tipo as cliente_doc_tipo,
                    c.documento_numero as cliente_doc_numero,
                    c.telefono as cliente_telefono,
                    cb.id_beneficiario,
                    cb.nombre as benef_nombre,
                    cb.apellido as benef_apellido,
                    cb.tipo_documento as benef_doc_tipo,
                    cb.numero_documento as benef_doc_numero,
                    cb.telefono as benef_telefono,
                    CONCAT_WS(', ', cb.calle, cb.numero, cb.piso, cb.ciudad) as benef_direccion,
                    cb.ciudad as benef_ciudad,
                    cb.pais as benef_pais,
                    opa.nombre_pack,
                    opa.estado_envio,
                    opa.pais_destino,
                    opa.ciudad_destino,
                    opa.id_operacion_pack_alimento
                FROM operaciones o
                LEFT JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                LEFT JOIN usuarios u ON o.id_usuario = u.id_usuario
                LEFT JOIN clientes c ON o.id_cliente = c.id_cliente
                LEFT JOIN clientes_beneficiarios cb ON opa.id_beneficiario = cb.id_beneficiario
                LEFT JOIN locales l ON o.id_local = l.id_local
                WHERE o.numero_operacion = @numeroOperacion
                  AND o.modulo = 'PACK_ALIMENTOS'
                  AND l.codigo_local = @codigoLocal";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@numeroOperacion", _numeroOperacion);
            cmd.Parameters.AddWithValue("@codigoLocal", _codigoLocal);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                _idOperacion = reader.GetInt64(0);
                var fecha = reader.IsDBNull(1) ? DateTime.Today : reader.GetDateTime(1);
                var hora = reader.IsDBNull(2) ? TimeSpan.Zero : reader.GetTimeSpan(2);
                var importe = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3);
                var monedaVal = reader.IsDBNull(4) ? "EUR" : reader.GetString(4);

                // Usuario
                var usuarioNom = reader.IsDBNull(5) ? "" : reader.GetString(5);
                var usuarioApe = reader.IsDBNull(6) ? "" : reader.GetString(6);
                UsuarioOperacion = $"{usuarioNom} {usuarioApe}".Trim();

                // Cliente
                var clienteNom = reader.IsDBNull(7) ? "" : reader.GetString(7);
                var clienteApe = reader.IsDBNull(8) ? "" : reader.GetString(8);
                var clienteNom2 = reader.IsDBNull(9) ? "" : reader.GetString(9);
                var clienteApe2 = reader.IsDBNull(10) ? "" : reader.GetString(10);
                var clienteDocTipo = reader.IsDBNull(11) ? "" : reader.GetString(11);
                var clienteDocNum = reader.IsDBNull(12) ? "" : reader.GetString(12);
                var clienteTel = reader.IsDBNull(13) ? "" : reader.GetString(13);

                ClienteNombre = $"{clienteNom} {clienteNom2} {clienteApe} {clienteApe2}".Trim();
                ClienteDocumento = $"{clienteDocTipo}: {clienteDocNum}";
                ClienteTelefono = string.IsNullOrEmpty(clienteTel) ? "N/A" : clienteTel;

                // Beneficiario
                _idBeneficiario = reader.IsDBNull(14) ? 0 : reader.GetInt32(14);
                var benefNom = reader.IsDBNull(15) ? "" : reader.GetString(15);
                var benefApe = reader.IsDBNull(16) ? "" : reader.GetString(16);
                var benefDocTipo = reader.IsDBNull(17) ? "" : reader.GetString(17);
                var benefDocNum = reader.IsDBNull(18) ? "" : reader.GetString(18);
                var benefTel = reader.IsDBNull(19) ? "" : reader.GetString(19);
                var benefDir = reader.IsDBNull(20) ? "" : reader.GetString(20);
                var benefCiudad = reader.IsDBNull(21) ? "" : reader.GetString(21);
                var benefPais = reader.IsDBNull(22) ? "" : reader.GetString(22);

                BeneficiarioNombre = $"{benefNom} {benefApe}".Trim();
                BeneficiarioDocumento = $"{benefDocTipo}: {benefDocNum}";
                BeneficiarioTelefono = string.IsNullOrEmpty(benefTel) ? "N/A" : benefTel;
                BeneficiarioDireccion = benefDir;
                BeneficiarioPais = benefPais;
                BeneficiarioCiudad = benefCiudad;

                // Guardar datos originales para edición
                _benefNombreOriginal = benefNom;
                _benefApellidoOriginal = benefApe;
                _benefTipoDocOriginal = benefDocTipo;
                _benefNumDocOriginal = benefDocNum;
                _benefTelefonoOriginal = benefTel;
                _benefDireccionOriginal = benefDir;
                _benefPaisOriginal = benefPais;
                _benefCiudadOriginal = benefCiudad;

                // Inicializar campos editables
                EditBeneficiarioNombre = benefNom;
                EditBeneficiarioApellido = benefApe;
                EditBeneficiarioTipoDoc = benefDocTipo;
                EditBeneficiarioNumDoc = benefDocNum;
                EditBeneficiarioTelefono = benefTel;
                EditBeneficiarioDireccion = benefDir;
                EditBeneficiarioPais = benefPais;
                EditBeneficiarioCiudad = benefCiudad;

                // Pack
                var packNom = reader.IsDBNull(23) ? "Pack Alimentos" : reader.GetString(23);
                var estado = reader.IsDBNull(24) ? "PENDIENTE" : reader.GetString(24);
                var paisDest = reader.IsDBNull(25) ? benefPais : reader.GetString(25);
                var ciudadDest = reader.IsDBNull(26) ? benefCiudad : reader.GetString(26);

                PackNombre = packNom;
                PackDescripcion = "";
                EstadoEnvio = ObtenerTextoEstado(estado);
                EstadoColor = ObtenerColorEstado(estado);

                FechaOperacion = fecha.ToString("dd/MM/yyyy");
                HoraOperacion = hora.ToString(@"hh\:mm");
                ImporteTotal = $"{importe:N2}";
                Moneda = monedaVal;

                // Preparar datos para PDF
                _datosRecibo = new ReciboFoodPackService.DatosReciboFoodPack
                {
                    NumeroOperacion = _numeroOperacion,
                    FechaOperacion = fecha.Add(hora),
                    CodigoLocal = _codigoLocal,
                    NombreUsuario = _nombreUsuario,
                    NumeroUsuario = _numeroUsuario.ToString(),
                    ClienteNombre = ClienteNombre,
                    ClienteTipoDocumento = clienteDocTipo,
                    ClienteNumeroDocumento = clienteDocNum,
                    ClienteTelefono = ClienteTelefono,
                    ClienteDireccion = "",
                    ClienteNacionalidad = "",
                    BeneficiarioNombre = BeneficiarioNombre,
                    BeneficiarioTipoDocumento = benefDocTipo,
                    BeneficiarioNumeroDocumento = benefDocNum,
                    BeneficiarioDireccion = BeneficiarioDireccion,
                    BeneficiarioTelefono = BeneficiarioTelefono,
                    BeneficiarioPaisDestino = paisDest,
                    BeneficiarioCiudadDestino = ciudadDest,
                    PackNombre = PackNombre,
                    PackDescripcion = PackDescripcion,
                    PackProductos = Array.Empty<string>(),
                    PrecioPack = importe,
                    Total = importe,
                    Moneda = monedaVal,
                    MetodoPago = "EFECTIVO"
                };

                // Cargar productos del pack y historial
                await reader.CloseAsync();
                await CargarArticulosOperacionAsync(conn);
                await CargarProductosPackAsync(conn);
                await CargarHistorialEstadosAsync(conn);

                // Notificar cambios en las propiedades de múltiples artículos
                OnPropertyChanged(nameof(TieneMultiplesArticulos));
                OnPropertyChanged(nameof(TieneUnSoloArticulo));
            }
            else
            {
                Mensaje = "No se encontró la operación";
                EsMensajeError = true;
            }
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al cargar datos: {ex.Message}";
            EsMensajeError = true;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private async Task CargarArticulosOperacionAsync(NpgsqlConnection conn)
    {
        try
        {
            ArticulosPack.Clear();

            var query = @"
                SELECT
                    opa.id_operacion_pack_alimento,
                    opa.nombre_pack,
                    opa.precio_pack,
                    opa.estado_envio
                FROM operaciones_pack_alimentos opa
                WHERE opa.id_operacion = @idOperacion
                ORDER BY opa.id_operacion_pack_alimento";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@idOperacion", _idOperacion);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var idOpaPack = reader.GetInt64(0);
                var nombre = reader.IsDBNull(1) ? "Pack Alimentos" : reader.GetString(1);
                var precio = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2);
                var estado = reader.IsDBNull(3) ? "PENDIENTE" : reader.GetString(3);

                ArticulosPack.Add(new ArticuloPackItem
                {
                    IdOperacionPackAlimento = idOpaPack,
                    NombrePack = nombre,
                    PrecioPack = precio,
                    Moneda = Moneda,
                    EstadoEnvio = estado,
                    EstadoTexto = ObtenerTextoEstado(estado),
                    EstadoColor = ObtenerColorEstado(estado)
                });
            }

            // Si hay múltiples artículos, actualizar PackNombre para mostrar resumen
            if (ArticulosPack.Count > 1)
            {
                PackNombre = $"{ArticulosPack.Count} Articulos";
            }
            else if (ArticulosPack.Count == 1)
            {
                PackNombre = ArticulosPack[0].NombrePack;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar artículos de la operación: {ex.Message}");
        }
    }

    private async Task CargarProductosPackAsync(NpgsqlConnection conn)
    {
        try
        {
            var query = @"
                SELECT pa.nombre_producto, pa.cantidad, pa.unidad
                FROM operaciones_pack_alimentos opa
                INNER JOIN packs_alimentos p ON opa.id_pack = p.id_pack
                INNER JOIN pack_productos pp ON p.id_pack = pp.id_pack
                INNER JOIN productos_alimentos pa ON pp.id_producto = pa.id_producto
                WHERE opa.id_operacion = @idOperacion";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@idOperacion", _idOperacion);

            await using var reader = await cmd.ExecuteReaderAsync();

            var productos = new System.Collections.Generic.List<string>();
            while (await reader.ReadAsync())
            {
                var nombre = reader.IsDBNull(0) ? "" : reader.GetString(0);
                var cantidad = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                var unidad = reader.IsDBNull(2) ? "" : reader.GetString(2);
                productos.Add($"{nombre} ({cantidad} {unidad})");
            }

            PackProductos = string.Join("\n", productos);
            if (_datosRecibo != null)
            {
                _datosRecibo.PackProductos = productos.ToArray();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar productos: {ex.Message}");
        }
    }

    private async Task CargarHistorialEstadosAsync(NpgsqlConnection conn)
    {
        try
        {
            HistorialEstados.Clear();

            // Verificar si existe la tabla de historial
            var checkTableQuery = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables
                    WHERE table_name = 'historial_estados_pack_alimentos'
                )";

            await using var checkCmd = new NpgsqlCommand(checkTableQuery, conn);
            var tableExists = (bool)(await checkCmd.ExecuteScalarAsync() ?? false);

            if (!tableExists)
            {
                // Crear la tabla si no existe
                var createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS historial_estados_pack_alimentos (
                        id_historial SERIAL PRIMARY KEY,
                        id_operacion BIGINT NOT NULL,
                        estado_anterior VARCHAR(50),
                        estado_nuevo VARCHAR(50) NOT NULL,
                        fecha_cambio TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        id_usuario INT,
                        observaciones TEXT,
                        FOREIGN KEY (id_operacion) REFERENCES operaciones(id_operacion) ON DELETE CASCADE
                    )";
                await using var createCmd = new NpgsqlCommand(createTableQuery, conn);
                await createCmd.ExecuteNonQueryAsync();

                // Insertar el estado inicial de la operación actual
                var insertInitialQuery = @"
                    INSERT INTO historial_estados_pack_alimentos (id_operacion, estado_anterior, estado_nuevo, fecha_cambio, observaciones)
                    SELECT o.id_operacion, NULL, opa.estado_envio, o.fecha_operacion + o.hora_operacion, 'Estado inicial'
                    FROM operaciones o
                    INNER JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                    WHERE o.numero_operacion = @numeroOperacion";
                await using var insertCmd = new NpgsqlCommand(insertInitialQuery, conn);
                insertCmd.Parameters.AddWithValue("@numeroOperacion", _numeroOperacion);
                await insertCmd.ExecuteNonQueryAsync();
            }

            // Cargar historial con información del usuario (más antiguo primero)
            var query = @"
                SELECT h.estado_anterior, h.estado_nuevo, h.fecha_cambio, h.observaciones,
                       COALESCE(u.nombre || ' ' || u.apellidos, 'Allva_System') as usuario
                FROM historial_estados_pack_alimentos h
                LEFT JOIN usuarios u ON h.id_usuario = u.id_usuario
                WHERE h.id_operacion = @idOperacion
                ORDER BY h.fecha_cambio ASC";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@idOperacion", _idOperacion);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var estadoAnterior = reader.IsDBNull(0) ? "" : reader.GetString(0);
                var estado = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var fecha = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2);
                var obs = reader.IsDBNull(3) ? "" : reader.GetString(3);
                var usuario = reader.IsDBNull(4) ? "Allva_System" : reader.GetString(4);

                HistorialEstados.Add(new HistorialEstadoItem
                {
                    EstadoAnterior = string.IsNullOrEmpty(estadoAnterior) ? "" : ObtenerTextoEstado(estadoAnterior),
                    Estado = ObtenerTextoEstado(estado),
                    Fecha = fecha.ToString("dd/MM/yyyy HH:mm"),
                    ColorEstadoAnterior = string.IsNullOrEmpty(estadoAnterior) ? "#6c757d" : ObtenerColorEstado(estadoAnterior),
                    ColorEstado = ObtenerColorEstado(estado),
                    Usuario = usuario.Trim(),
                    Observaciones = obs
                });
            }

            // Si no hay historial, agregar el estado actual
            if (HistorialEstados.Count == 0)
            {
                await reader.CloseAsync();

                // Obtener la fecha de creación de la operación
                var fechaQuery = @"
                    SELECT o.fecha_operacion, o.hora_operacion, opa.estado_envio
                    FROM operaciones o
                    INNER JOIN operaciones_pack_alimentos opa ON o.id_operacion = opa.id_operacion
                    WHERE o.id_operacion = @idOperacion";

                await using var fechaCmd = new NpgsqlCommand(fechaQuery, conn);
                fechaCmd.Parameters.AddWithValue("@idOperacion", _idOperacion);
                await using var fechaReader = await fechaCmd.ExecuteReaderAsync();

                if (await fechaReader.ReadAsync())
                {
                    var fechaOp = fechaReader.GetDateTime(0);
                    var horaOp = fechaReader.IsDBNull(1) ? TimeSpan.Zero : fechaReader.GetTimeSpan(1);
                    var estadoActual = fechaReader.IsDBNull(2) ? "PENDIENTE" : fechaReader.GetString(2);

                    HistorialEstados.Add(new HistorialEstadoItem
                    {
                        EstadoAnterior = "",
                        Estado = ObtenerTextoEstado(estadoActual),
                        Fecha = fechaOp.Add(horaOp).ToString("dd/MM/yyyy HH:mm"),
                        ColorEstadoAnterior = "#6c757d",
                        ColorEstado = ObtenerColorEstado(estadoActual),
                        Usuario = "Allva_System",
                        Observaciones = "Estado inicial"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al cargar historial: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ActivarEdicion()
    {
        if (PuedeEditar)
        {
            ModoEdicion = true;
        }
    }

    [RelayCommand]
    private void CancelarEdicion()
    {
        ModoEdicion = false;
        // Restaurar valores originales
        EditBeneficiarioNombre = _benefNombreOriginal;
        EditBeneficiarioApellido = _benefApellidoOriginal;
        EditBeneficiarioTipoDoc = _benefTipoDocOriginal;
        EditBeneficiarioNumDoc = _benefNumDocOriginal;
        EditBeneficiarioTelefono = _benefTelefonoOriginal;
        EditBeneficiarioDireccion = _benefDireccionOriginal;
        EditBeneficiarioPais = _benefPaisOriginal;
        EditBeneficiarioCiudad = _benefCiudadOriginal;
    }

    [RelayCommand]
    private async Task GuardarCambiosBeneficiarioAsync()
    {
        if (!PuedeEditar || _idBeneficiario == 0)
        {
            Mensaje = "No se pueden guardar los cambios en este estado";
            EsMensajeError = true;
            return;
        }

        try
        {
            EstaCargando = true;
            Mensaje = "";

            // Detectar qué campos cambiaron
            var cambios = new System.Collections.Generic.List<string>();
            if (_benefNombreOriginal != EditBeneficiarioNombre)
                cambios.Add($"nombre: \"{_benefNombreOriginal}\" → \"{EditBeneficiarioNombre}\"");
            if (_benefApellidoOriginal != EditBeneficiarioApellido)
                cambios.Add($"apellido: \"{_benefApellidoOriginal}\" → \"{EditBeneficiarioApellido}\"");
            if (_benefTipoDocOriginal != EditBeneficiarioTipoDoc)
                cambios.Add($"tipo documento: \"{_benefTipoDocOriginal}\" → \"{EditBeneficiarioTipoDoc}\"");
            if (_benefNumDocOriginal != EditBeneficiarioNumDoc)
                cambios.Add($"número documento: \"{_benefNumDocOriginal}\" → \"{EditBeneficiarioNumDoc}\"");
            if (_benefTelefonoOriginal != EditBeneficiarioTelefono)
                cambios.Add($"teléfono: \"{_benefTelefonoOriginal}\" → \"{EditBeneficiarioTelefono}\"");
            if (_benefDireccionOriginal != EditBeneficiarioDireccion)
                cambios.Add($"dirección: \"{_benefDireccionOriginal}\" → \"{EditBeneficiarioDireccion}\"");
            if (_benefPaisOriginal != EditBeneficiarioPais)
                cambios.Add($"país: \"{_benefPaisOriginal}\" → \"{EditBeneficiarioPais}\"");
            if (_benefCiudadOriginal != EditBeneficiarioCiudad)
                cambios.Add($"ciudad: \"{_benefCiudadOriginal}\" → \"{EditBeneficiarioCiudad}\"");

            // Si no hay cambios, no hacer nada
            if (cambios.Count == 0)
            {
                ModoEdicion = false;
                Mensaje = "No se detectaron cambios";
                EsMensajeError = false;
                return;
            }

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Actualizar beneficiario
            var query = @"
                UPDATE clientes_beneficiarios
                SET nombre = @nombre,
                    apellido = @apellido,
                    tipo_documento = @tipoDoc,
                    numero_documento = @numDoc,
                    telefono = @telefono,
                    calle = @direccion,
                    pais = @pais,
                    ciudad = @ciudad,
                    fecha_modificacion = CURRENT_TIMESTAMP
                WHERE id_beneficiario = @idBeneficiario";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@nombre", EditBeneficiarioNombre);
            cmd.Parameters.AddWithValue("@apellido", EditBeneficiarioApellido);
            cmd.Parameters.AddWithValue("@tipoDoc", EditBeneficiarioTipoDoc);
            cmd.Parameters.AddWithValue("@numDoc", EditBeneficiarioNumDoc);
            cmd.Parameters.AddWithValue("@telefono", EditBeneficiarioTelefono);
            cmd.Parameters.AddWithValue("@direccion", EditBeneficiarioDireccion);
            cmd.Parameters.AddWithValue("@pais", EditBeneficiarioPais);
            cmd.Parameters.AddWithValue("@ciudad", EditBeneficiarioCiudad);
            cmd.Parameters.AddWithValue("@idBeneficiario", _idBeneficiario);

            await cmd.ExecuteNonQueryAsync();

            // Actualizar también en operaciones_pack_alimentos
            var updateOpQuery = @"
                UPDATE operaciones_pack_alimentos
                SET pais_destino = @pais,
                    ciudad_destino = @ciudad
                WHERE id_operacion = @idOperacion";

            await using var updateOpCmd = new NpgsqlCommand(updateOpQuery, conn);
            updateOpCmd.Parameters.AddWithValue("@pais", EditBeneficiarioPais);
            updateOpCmd.Parameters.AddWithValue("@ciudad", EditBeneficiarioCiudad);
            updateOpCmd.Parameters.AddWithValue("@idOperacion", _idOperacion);
            await updateOpCmd.ExecuteNonQueryAsync();

            // Registrar en historial los cambios del beneficiario
            var observacionCambios = $"Modificación de beneficiario: {string.Join(", ", cambios)}";
            var historialQuery = @"
                INSERT INTO historial_estados_pack_alimentos
                    (id_operacion, estado_anterior, estado_nuevo, fecha_cambio, id_usuario, observaciones)
                VALUES
                    (@idOperacion, @estadoActual, @estadoActual, CURRENT_TIMESTAMP, @idUsuario, @observaciones)";

            await using var historialCmd = new NpgsqlCommand(historialQuery, conn);
            historialCmd.Parameters.AddWithValue("@idOperacion", _idOperacion);
            historialCmd.Parameters.AddWithValue("@estadoActual", EstadoEnvio.ToUpper());
            historialCmd.Parameters.AddWithValue("@idUsuario", _numeroUsuario);
            historialCmd.Parameters.AddWithValue("@observaciones", observacionCambios);

            try
            {
                await historialCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Si falla el historial, no es crítico
            }

            // Añadir al historial local (al principio)
            var fechaActual = DateTime.Now;
            HistorialEstados.Insert(0, new HistorialEstadoItem
            {
                EstadoAnterior = "",
                Estado = EstadoEnvio,
                Fecha = fechaActual.ToString("dd/MM/yyyy HH:mm"),
                ColorEstadoAnterior = "#6c757d",
                ColorEstado = EstadoColor,
                Usuario = _nombreUsuario,
                Observaciones = observacionCambios
            });

            // Actualizar datos mostrados
            BeneficiarioNombre = $"{EditBeneficiarioNombre} {EditBeneficiarioApellido}".Trim();
            BeneficiarioDocumento = $"{EditBeneficiarioTipoDoc}: {EditBeneficiarioNumDoc}";
            BeneficiarioTelefono = string.IsNullOrEmpty(EditBeneficiarioTelefono) ? "N/A" : EditBeneficiarioTelefono;
            BeneficiarioDireccion = EditBeneficiarioDireccion;
            BeneficiarioPais = EditBeneficiarioPais;
            BeneficiarioCiudad = EditBeneficiarioCiudad;

            // Actualizar datos originales
            _benefNombreOriginal = EditBeneficiarioNombre;
            _benefApellidoOriginal = EditBeneficiarioApellido;
            _benefTipoDocOriginal = EditBeneficiarioTipoDoc;
            _benefNumDocOriginal = EditBeneficiarioNumDoc;
            _benefTelefonoOriginal = EditBeneficiarioTelefono;
            _benefDireccionOriginal = EditBeneficiarioDireccion;
            _benefPaisOriginal = EditBeneficiarioPais;
            _benefCiudadOriginal = EditBeneficiarioCiudad;

            // Actualizar datos del recibo para PDF
            if (_datosRecibo != null)
            {
                _datosRecibo.BeneficiarioNombre = BeneficiarioNombre;
                _datosRecibo.BeneficiarioTipoDocumento = EditBeneficiarioTipoDoc;
                _datosRecibo.BeneficiarioNumeroDocumento = EditBeneficiarioNumDoc;
                _datosRecibo.BeneficiarioTelefono = EditBeneficiarioTelefono;
                _datosRecibo.BeneficiarioDireccion = EditBeneficiarioDireccion;
                _datosRecibo.BeneficiarioPaisDestino = EditBeneficiarioPais;
                _datosRecibo.BeneficiarioCiudadDestino = EditBeneficiarioCiudad;
            }

            ModoEdicion = false;
            Mensaje = "Datos del beneficiario actualizados correctamente";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al guardar cambios: {ex.Message}";
            EsMensajeError = true;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task AnularOperacionAsync()
    {
        if (!PuedeAnular)
        {
            Mensaje = "No se puede anular esta operación";
            EsMensajeError = true;
            return;
        }

        try
        {
            EstaCargando = true;
            Mensaje = "";

            await using var conn = new NpgsqlConnection(ConnectionString);
            await conn.OpenAsync();

            // Obtener estado actual (ya viene en formato BD: PENDIENTE, PAGADO, etc.)
            var estadoActual = EstadoEnvio.ToUpper();

            // Actualizar estado a ANULADO
            var query = @"
                UPDATE operaciones_pack_alimentos
                SET estado_envio = 'ANULADO'
                WHERE id_operacion = @idOperacion";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@idOperacion", _idOperacion);
            await cmd.ExecuteNonQueryAsync();

            // Registrar en historial
            var historialQuery = @"
                INSERT INTO historial_estados_pack_alimentos
                    (id_operacion, estado_anterior, estado_nuevo, fecha_cambio, id_usuario, observaciones)
                VALUES
                    (@idOperacion, @estadoAnterior, 'ANULADO', CURRENT_TIMESTAMP, @idUsuario, 'Anulación desde detalle de operación')";

            await using var historialCmd = new NpgsqlCommand(historialQuery, conn);
            historialCmd.Parameters.AddWithValue("@idOperacion", _idOperacion);
            historialCmd.Parameters.AddWithValue("@estadoAnterior", estadoActual);
            historialCmd.Parameters.AddWithValue("@idUsuario", _numeroUsuario);

            try
            {
                await historialCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Si falla el historial, no es crítico
            }

            // Actualizar UI
            EstadoEnvio = "ANULADO";
            EstadoColor = ObtenerColorEstado("ANULADO");

            // Añadir al historial local (al principio porque está ordenado de más reciente a más antiguo)
            HistorialEstados.Insert(0, new HistorialEstadoItem
            {
                EstadoAnterior = estadoActual,
                Estado = "ANULADO",
                Fecha = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                ColorEstadoAnterior = ObtenerColorEstado(estadoActual),
                ColorEstado = "#dc3545",
                Usuario = _nombreUsuario,
                Observaciones = "Anulación desde detalle de operación"
            });

            Mensaje = "Operación anulada correctamente";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al anular operación: {ex.Message}";
            EsMensajeError = true;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task ReimprimirReciboAsync()
    {
        if (_datosRecibo == null)
        {
            Mensaje = "No hay datos para reimprimir";
            EsMensajeError = true;
            return;
        }

        if (_ventana == null)
        {
            Mensaje = "No se puede abrir el dialogo de guardar";
            EsMensajeError = true;
            return;
        }

        try
        {
            EstaCargando = true;
            Mensaje = "";

            var nombreSugerido = $"Recibo_FoodPack_{_numeroOperacion}.pdf";

            var archivo = await _ventana.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar Recibo PDF",
                SuggestedFileName = nombreSugerido,
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Documento PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (archivo == null)
                return;

            // Generar PDF del recibo (siempre el recibo normal, no anulación)
            var pdfService = new ReciboFoodPackService();
            var pdfBytes = await Task.Run(() => pdfService.GenerarReimpresionPdf(_datosRecibo));

            await using var stream = await archivo.OpenWriteAsync();
            await stream.WriteAsync(pdfBytes);

            Mensaje = "Recibo generado correctamente";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al generar PDF: {ex.Message}";
            EsMensajeError = true;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task ReimprimirAnulacionAsync()
    {
        if (_datosRecibo == null)
        {
            Mensaje = "No hay datos para reimprimir";
            EsMensajeError = true;
            return;
        }

        if (_ventana == null)
        {
            Mensaje = "No se puede abrir el dialogo de guardar";
            EsMensajeError = true;
            return;
        }

        try
        {
            EstaCargando = true;
            Mensaje = "";

            var nombreSugerido = $"Anulacion_FoodPack_{_numeroOperacion}.pdf";

            var archivo = await _ventana.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar Comprobante de Anulación",
                SuggestedFileName = nombreSugerido,
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Documento PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (archivo == null)
                return;

            // Generar PDF de anulación
            var pdfService = new ReciboFoodPackService();
            var pdfBytes = await Task.Run(() => pdfService.GenerarReciboAnulacionPdf(_datosRecibo));

            await using var stream = await archivo.OpenWriteAsync();
            await stream.WriteAsync(pdfBytes);

            Mensaje = "Comprobante de anulación generado correctamente";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al generar PDF: {ex.Message}";
            EsMensajeError = true;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task ImprimirHistorialAsync()
    {
        if (HistorialEstados.Count == 0)
        {
            Mensaje = "No hay historial para imprimir";
            EsMensajeError = true;
            return;
        }

        if (_ventana == null)
        {
            Mensaje = "No se puede abrir el diálogo de guardar";
            EsMensajeError = true;
            return;
        }

        try
        {
            EstaCargando = true;
            Mensaje = "";

            var nombreSugerido = $"Historial_{_numeroOperacion}.pdf";

            var archivo = await _ventana.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Guardar Historial de Estados",
                SuggestedFileName = nombreSugerido,
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Documento PDF") { Patterns = new[] { "*.pdf" } }
                }
            });

            if (archivo == null)
                return;

            // Generar PDF del historial
            var pdfService = new ReciboFoodPackService();
            var datosHistorial = new ReciboFoodPackService.DatosHistorialEstados
            {
                NumeroOperacion = _numeroOperacion,
                FechaOperacion = FechaOperacion,
                HoraOperacion = HoraOperacion,
                UsuarioOperacion = UsuarioOperacion,
                ClienteNombre = ClienteNombre,
                PackNombre = PackNombre,
                ImporteTotal = ImporteTotal,
                Moneda = Moneda,
                EstadoActual = EstadoEnvio,
                ColorEstadoActual = EstadoColor,
                CodigoLocal = _codigoLocal,
                Historial = HistorialEstados.Select(h => new ReciboFoodPackService.ItemHistorialEstado
                {
                    EstadoAnterior = h.EstadoAnterior,
                    EstadoNuevo = h.Estado,
                    Fecha = h.Fecha,
                    Usuario = h.Usuario,
                    Observaciones = h.Observaciones,
                    ColorEstadoAnterior = h.ColorEstadoAnterior,
                    ColorEstadoNuevo = h.ColorEstado
                }).ToList()
            };

            var pdfBytes = await Task.Run(() => pdfService.GenerarHistorialEstadosPdf(datosHistorial));

            await using var stream = await archivo.OpenWriteAsync();
            await stream.WriteAsync(pdfBytes);

            Mensaje = "Historial de estados exportado correctamente";
            EsMensajeError = false;
        }
        catch (Exception ex)
        {
            Mensaje = $"Error al generar PDF: {ex.Message}";
            EsMensajeError = true;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private void Cerrar()
    {
        _ventana?.Close();
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
}

// Clase para representar cada artículo/pack de una operación
public class ArticuloPackItem
{
    public long IdOperacionPackAlimento { get; set; }
    public string NombrePack { get; set; } = "";
    public decimal PrecioPack { get; set; }
    public string Moneda { get; set; } = "EUR";
    public string EstadoEnvio { get; set; } = "";
    public string EstadoTexto { get; set; } = "";
    public string EstadoColor { get; set; } = "#6c757d";

    public string PrecioFormateado => $"{PrecioPack:N2} {Moneda}";
}

public class HistorialEstadoItem
{
    public string EstadoAnterior { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Fecha { get; set; } = "";
    public string ColorEstado { get; set; } = "#6c757d";
    public string ColorEstadoAnterior { get; set; } = "#6c757d";
    public string Usuario { get; set; } = "";
    public string Observaciones { get; set; } = "";
    public bool TieneEstadoAnterior => !string.IsNullOrEmpty(EstadoAnterior);

    // Propiedades para la vista de tabla
    public string FechaSolo => Fecha.Length >= 10 ? Fecha.Substring(0, 10) : Fecha;
    public string HoraSolo => Fecha.Length >= 16 ? Fecha.Substring(11, 5) : "";

    public string DescripcionFormateada
    {
        get
        {
            // Si es una modificación de beneficiario, mostrar eso
            if (!string.IsNullOrEmpty(Observaciones) && Observaciones.Contains("Modificación de beneficiario"))
            {
                return Observaciones;
            }

            // Si es una anulación
            if (!string.IsNullOrEmpty(Observaciones) && Observaciones.ToLower().Contains("anulación"))
            {
                return TieneEstadoAnterior
                    ? $"Cambio de estado: {EstadoAnterior} → {Estado}"
                    : Observaciones;
            }

            // Si tiene cambio de estado, mostrar solo eso
            if (TieneEstadoAnterior)
            {
                return $"Cambio de estado: {EstadoAnterior} → {Estado}";
            }

            // Si es estado inicial
            if (!string.IsNullOrEmpty(Observaciones) && Observaciones.ToLower().Contains("estado inicial"))
            {
                return "Operación realizada";
            }

            // Cualquier otra observación
            if (!string.IsNullOrEmpty(Observaciones))
            {
                return Observaciones;
            }

            return "Operación realizada";
        }
    }
}
