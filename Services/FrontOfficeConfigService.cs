using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Npgsql;
using Allva.Desktop.Models;

namespace Allva.Desktop.Services;

/// <summary>
/// Servicio singleton para gestionar la configuracion del Front Office
/// Usa la base de datos PostgreSQL para persistencia
/// </summary>
public class FrontOfficeConfigService
{
    private static FrontOfficeConfigService? _instance;
    private static readonly object _lock = new();

    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    private readonly string _configFolderPath;
    private const string ManualPdfFileName = "manual_usuario.pdf";

    // Cache local para evitar consultas repetidas
    private List<TelefonoContacto>? _telefonosCache;
    private List<CuentaBancaria>? _cuentasCache;

    public static FrontOfficeConfigService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new FrontOfficeConfigService();
                }
            }
            return _instance;
        }
    }

    private FrontOfficeConfigService()
    {
        _configFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AllvaDesktop"
        );

        if (!Directory.Exists(_configFolderPath))
        {
            Directory.CreateDirectory(_configFolderPath);
        }
    }

    /// <summary>
    /// Obtiene la configuracion actual (para compatibilidad)
    /// </summary>
    public FrontOfficeConfig ObtenerConfiguracion()
    {
        return new FrontOfficeConfig
        {
            Telefonos = ObtenerTelefonos(),
            CuentasBancarias = ObtenerCuentasBancarias(),
            UrlManualUsuario = ""
        };
    }

    #region Telefonos de Contacto

    /// <summary>
    /// Obtiene la lista de telefonos de contacto desde la BD
    /// </summary>
    public List<TelefonoContacto> ObtenerTelefonos()
    {
        if (_telefonosCache != null) return _telefonosCache;

        var telefonos = new List<TelefonoContacto>();
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = "SELECT id_telefono, nombre, numero FROM telefonos_ayuda WHERE activo = true ORDER BY id_telefono";
            using var cmd = new NpgsqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                telefonos.Add(new TelefonoContacto
                {
                    Id = reader.GetInt32(0).ToString(),
                    Nombre = reader.GetString(1),
                    Numero = reader.GetString(2)
                });
            }

            _telefonosCache = telefonos;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al obtener telefonos: {ex.Message}");
        }

        return telefonos;
    }

    /// <summary>
    /// Guarda la lista completa de telefonos (sincroniza con BD)
    /// </summary>
    public void GuardarTelefonos(List<TelefonoContacto> telefonos)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            // Desactivar todos los existentes
            using (var cmdDesactivar = new NpgsqlCommand("UPDATE telefonos_ayuda SET activo = false", connection))
            {
                cmdDesactivar.ExecuteNonQuery();
            }

            // Insertar o actualizar cada telefono
            foreach (var tel in telefonos)
            {
                if (int.TryParse(tel.Id, out int idExistente) && idExistente > 0)
                {
                    // Actualizar existente
                    var queryUpdate = @"UPDATE telefonos_ayuda
                                        SET nombre = @Nombre, numero = @Numero, activo = true
                                        WHERE id_telefono = @Id";
                    using var cmdUpdate = new NpgsqlCommand(queryUpdate, connection);
                    cmdUpdate.Parameters.AddWithValue("@Id", idExistente);
                    cmdUpdate.Parameters.AddWithValue("@Nombre", tel.Nombre);
                    cmdUpdate.Parameters.AddWithValue("@Numero", tel.Numero);
                    cmdUpdate.ExecuteNonQuery();
                }
                else
                {
                    // Insertar nuevo
                    var queryInsert = @"INSERT INTO telefonos_ayuda (nombre, numero, activo)
                                        VALUES (@Nombre, @Numero, true)";
                    using var cmdInsert = new NpgsqlCommand(queryInsert, connection);
                    cmdInsert.Parameters.AddWithValue("@Nombre", tel.Nombre);
                    cmdInsert.Parameters.AddWithValue("@Numero", tel.Numero);
                    cmdInsert.ExecuteNonQuery();
                }
            }

            _telefonosCache = null; // Invalidar cache
            System.Diagnostics.Debug.WriteLine("Telefonos guardados en BD");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar telefonos: {ex.Message}");
        }
    }

    /// <summary>
    /// Agrega un nuevo telefono de contacto
    /// </summary>
    public void AgregarTelefono(TelefonoContacto telefono)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = @"INSERT INTO telefonos_ayuda (nombre, numero) VALUES (@Nombre, @Numero) RETURNING id_telefono";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Nombre", telefono.Nombre);
            cmd.Parameters.AddWithValue("@Numero", telefono.Numero);

            var newId = cmd.ExecuteScalar();
            telefono.Id = newId?.ToString() ?? "";

            _telefonosCache = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al agregar telefono: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina un telefono de contacto por ID
    /// </summary>
    public void EliminarTelefono(string id)
    {
        if (!int.TryParse(id, out int idInt)) return;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = "UPDATE telefonos_ayuda SET activo = false WHERE id_telefono = @Id";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", idInt);
            cmd.ExecuteNonQuery();

            _telefonosCache = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al eliminar telefono: {ex.Message}");
        }
    }

    #endregion

    #region Cuentas Bancarias

    /// <summary>
    /// Obtiene la lista de cuentas bancarias desde la BD
    /// </summary>
    public List<CuentaBancaria> ObtenerCuentasBancarias()
    {
        if (_cuentasCache != null) return _cuentasCache;

        var cuentas = new List<CuentaBancaria>();
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = @"SELECT id_cuenta, nombre_banco, titular, entidad, iban,
                                 disponible_compra_divisas, disponible_pack_alimentos,
                                 disponible_billetes_avion, disponible_pack_viajes
                          FROM cuentas_bancarias_frontoffice
                          WHERE activo = true
                          ORDER BY id_cuenta";
            using var cmd = new NpgsqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var cuenta = new CuentaBancaria
                {
                    Id = reader.GetInt32(0).ToString(),
                    NombreBanco = reader.GetString(1),
                    Titular = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    TipoCuenta = "IBAN",
                    DisponibleCompraDivisas = !reader.IsDBNull(5) && reader.GetBoolean(5),
                    DisponiblePackAlimentos = !reader.IsDBNull(6) && reader.GetBoolean(6),
                    DisponibleBilletesAvion = !reader.IsDBNull(7) && reader.GetBoolean(7),
                    DisponiblePackViajes = !reader.IsDBNull(8) && reader.GetBoolean(8)
                };
                // Parsear el IBAN a sus componentes
                var ibanCompleto = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                cuenta.ParseIBAN(ibanCompleto);
                cuentas.Add(cuenta);
            }

            _cuentasCache = cuentas;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al obtener cuentas bancarias: {ex.Message}");
        }

        return cuentas;
    }

    /// <summary>
    /// Guarda la lista completa de cuentas bancarias (sincroniza con BD)
    /// </summary>
    public void GuardarCuentasBancarias(List<CuentaBancaria> cuentas)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            // Primero insertar las cuentas nuevas para obtener sus IDs
            foreach (var cuenta in cuentas)
            {
                // Si el ID no es un entero valido, es una cuenta nueva que necesita insertarse
                if (!int.TryParse(cuenta.Id, out int idExistente) || idExistente <= 0)
                {
                    var queryInsert = @"INSERT INTO cuentas_bancarias_frontoffice
                                        (nombre_banco, titular, entidad, iban, activo,
                                         disponible_compra_divisas, disponible_pack_alimentos,
                                         disponible_billetes_avion, disponible_pack_viajes)
                                        VALUES (@NombreBanco, @Titular, @Entidad, @IBAN, true,
                                                @DisponibleDivisas, @DisponibleAlimentos,
                                                @DisponibleBilletes, @DisponibleViajes)
                                        RETURNING id_cuenta";
                    using var cmdInsert = new NpgsqlCommand(queryInsert, connection);
                    cmdInsert.Parameters.AddWithValue("@NombreBanco", cuenta.NombreBanco);
                    cmdInsert.Parameters.AddWithValue("@Titular", cuenta.Titular ?? string.Empty);
                    cmdInsert.Parameters.AddWithValue("@Entidad", (object?)cuenta.CodigoBanco ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@IBAN", cuenta.IBANSinEspacios);
                    cmdInsert.Parameters.AddWithValue("@DisponibleDivisas", cuenta.DisponibleCompraDivisas);
                    cmdInsert.Parameters.AddWithValue("@DisponibleAlimentos", cuenta.DisponiblePackAlimentos);
                    cmdInsert.Parameters.AddWithValue("@DisponibleBilletes", cuenta.DisponibleBilletesAvion);
                    cmdInsert.Parameters.AddWithValue("@DisponibleViajes", cuenta.DisponiblePackViajes);

                    var newId = cmdInsert.ExecuteScalar();
                    if (newId != null)
                    {
                        cuenta.Id = newId.ToString()!;
                    }
                }
            }

            // Ahora obtener todos los IDs validos (incluyendo los recien insertados)
            var idsExistentes = new List<int>();
            foreach (var cuenta in cuentas)
            {
                if (int.TryParse(cuenta.Id, out int idExistente) && idExistente > 0)
                {
                    idsExistentes.Add(idExistente);
                }
            }

            // Desactivar solo las cuentas que ya no estan en la lista
            if (idsExistentes.Count > 0)
            {
                var idsParam = string.Join(",", idsExistentes);
                using var cmdDesactivar = new NpgsqlCommand(
                    $"UPDATE cuentas_bancarias_frontoffice SET activo = false WHERE id_cuenta NOT IN ({idsParam})",
                    connection);
                cmdDesactivar.ExecuteNonQuery();
            }

            // Actualizar las cuentas existentes
            foreach (var cuenta in cuentas)
            {
                if (int.TryParse(cuenta.Id, out int idExistente) && idExistente > 0)
                {
                    var queryUpdate = @"UPDATE cuentas_bancarias_frontoffice
                                        SET nombre_banco = @NombreBanco, titular = @Titular,
                                            entidad = @Entidad, iban = @IBAN, activo = true,
                                            disponible_compra_divisas = @DisponibleDivisas,
                                            disponible_pack_alimentos = @DisponibleAlimentos,
                                            disponible_billetes_avion = @DisponibleBilletes,
                                            disponible_pack_viajes = @DisponibleViajes
                                        WHERE id_cuenta = @Id";
                    using var cmdUpdate = new NpgsqlCommand(queryUpdate, connection);
                    cmdUpdate.Parameters.AddWithValue("@Id", idExistente);
                    cmdUpdate.Parameters.AddWithValue("@NombreBanco", cuenta.NombreBanco);
                    cmdUpdate.Parameters.AddWithValue("@Titular", cuenta.Titular ?? string.Empty);
                    cmdUpdate.Parameters.AddWithValue("@Entidad", (object?)cuenta.CodigoBanco ?? DBNull.Value);
                    cmdUpdate.Parameters.AddWithValue("@IBAN", cuenta.IBANSinEspacios);
                    cmdUpdate.Parameters.AddWithValue("@DisponibleDivisas", cuenta.DisponibleCompraDivisas);
                    cmdUpdate.Parameters.AddWithValue("@DisponibleAlimentos", cuenta.DisponiblePackAlimentos);
                    cmdUpdate.Parameters.AddWithValue("@DisponibleBilletes", cuenta.DisponibleBilletesAvion);
                    cmdUpdate.Parameters.AddWithValue("@DisponibleViajes", cuenta.DisponiblePackViajes);
                    cmdUpdate.ExecuteNonQuery();
                }
            }

            _cuentasCache = null; // Invalidar cache
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar cuentas bancarias: {ex.Message}");
        }
    }

    /// <summary>
    /// Agrega una nueva cuenta bancaria
    /// </summary>
    public void AgregarCuentaBancaria(CuentaBancaria cuenta)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = @"INSERT INTO cuentas_bancarias_frontoffice (nombre_banco, titular, entidad, iban)
                          VALUES (@NombreBanco, @Titular, @Entidad, @IBAN) RETURNING id_cuenta";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@NombreBanco", cuenta.NombreBanco);
            cmd.Parameters.AddWithValue("@Titular", cuenta.Titular);
            cmd.Parameters.AddWithValue("@Entidad", (object?)cuenta.CodigoBanco ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IBAN", cuenta.IBANSinEspacios);

            var newId = cmd.ExecuteScalar();
            cuenta.Id = newId?.ToString() ?? "";

            _cuentasCache = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al agregar cuenta bancaria: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina una cuenta bancaria por ID
    /// </summary>
    public void EliminarCuentaBancaria(string id)
    {
        if (!int.TryParse(id, out int idInt)) return;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = "UPDATE cuentas_bancarias_frontoffice SET activo = false WHERE id_cuenta = @Id";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", idInt);
            cmd.ExecuteNonQuery();

            _cuentasCache = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al eliminar cuenta bancaria: {ex.Message}");
        }
    }

    #endregion

    #region Manual PDF (Local)

    /// <summary>
    /// Guarda el archivo PDF del manual de usuario (local)
    /// </summary>
    public void GuardarManualPdf(string sourcePath)
    {
        try
        {
            var destPath = Path.Combine(_configFolderPath, ManualPdfFileName);
            File.Copy(sourcePath, destPath, overwrite: true);
            System.Diagnostics.Debug.WriteLine($"Manual PDF guardado en: {destPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar PDF: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina el archivo PDF del manual de usuario
    /// </summary>
    public void EliminarManualPdf()
    {
        try
        {
            var pdfPath = Path.Combine(_configFolderPath, ManualPdfFileName);
            if (File.Exists(pdfPath))
            {
                File.Delete(pdfPath);
                System.Diagnostics.Debug.WriteLine("Manual PDF eliminado");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al eliminar PDF: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene la ruta del archivo PDF del manual si existe
    /// </summary>
    public string? ObtenerRutaManualPdf()
    {
        var pdfPath = Path.Combine(_configFolderPath, ManualPdfFileName);
        return File.Exists(pdfPath) ? pdfPath : null;
    }

    #endregion

    #region Preferencias de UI (Admin Noticias)

    private const string PreferenciasFileName = "admin_preferences.json";

    /// <summary>
    /// Obtiene la preferencia de mostrar imagenes en la lista de noticias
    /// </summary>
    public bool ObtenerPreferenciaMostrarImagenesNoticias()
    {
        try
        {
            var filePath = Path.Combine(_configFolderPath, PreferenciasFileName);
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var prefs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (prefs != null && prefs.TryGetValue("mostrarImagenesNoticias", out var value))
                {
                    if (value is System.Text.Json.JsonElement jsonElement)
                    {
                        return jsonElement.GetBoolean();
                    }
                    return Convert.ToBoolean(value);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al leer preferencia: {ex.Message}");
        }
        return false; // Por defecto no mostrar
    }

    /// <summary>
    /// Guarda la preferencia de mostrar imagenes en la lista de noticias
    /// </summary>
    public void GuardarPreferenciaMostrarImagenesNoticias(bool mostrar)
    {
        try
        {
            var filePath = Path.Combine(_configFolderPath, PreferenciasFileName);
            Dictionary<string, object> prefs;

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                prefs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
            }
            else
            {
                prefs = new Dictionary<string, object>();
            }

            prefs["mostrarImagenesNoticias"] = mostrar;

            var newJson = System.Text.Json.JsonSerializer.Serialize(prefs, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, newJson);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al guardar preferencia: {ex.Message}");
        }
    }

    #endregion

    #region Metodos de compatibilidad

    /// <summary>
    /// Actualiza un telefono existente
    /// </summary>
    public void ActualizarTelefono(TelefonoContacto telefono)
    {
        if (!int.TryParse(telefono.Id, out int idInt)) return;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = "UPDATE telefonos_ayuda SET nombre = @Nombre, numero = @Numero WHERE id_telefono = @Id";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", idInt);
            cmd.Parameters.AddWithValue("@Nombre", telefono.Nombre);
            cmd.Parameters.AddWithValue("@Numero", telefono.Numero);
            cmd.ExecuteNonQuery();

            _telefonosCache = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al actualizar telefono: {ex.Message}");
        }
    }

    /// <summary>
    /// Actualiza una cuenta bancaria existente
    /// </summary>
    public void ActualizarCuentaBancaria(CuentaBancaria cuenta)
    {
        if (!int.TryParse(cuenta.Id, out int idInt)) return;

        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = @"UPDATE cuentas_bancarias_frontoffice
                          SET nombre_banco = @NombreBanco, titular = @Titular, entidad = @Entidad, iban = @IBAN
                          WHERE id_cuenta = @Id";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", idInt);
            cmd.Parameters.AddWithValue("@NombreBanco", cuenta.NombreBanco);
            cmd.Parameters.AddWithValue("@Titular", cuenta.Titular);
            cmd.Parameters.AddWithValue("@Entidad", (object?)cuenta.CodigoBanco ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IBAN", cuenta.IBANSinEspacios);
            cmd.ExecuteNonQuery();

            _cuentasCache = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al actualizar cuenta bancaria: {ex.Message}");
        }
    }

    /// <summary>
    /// Establece la URL del manual de usuario (no usado, mantenido por compatibilidad)
    /// </summary>
    public void EstablecerUrlManual(string url)
    {
        // No se usa, el PDF se guarda localmente
    }

    /// <summary>
    /// Recarga la configuracion desde la BD
    /// </summary>
    public void RecargarConfiguracion()
    {
        _telefonosCache = null;
        _cuentasCache = null;
        _preguntasCache = null;
    }

    #endregion

    #region Preguntas Frecuentes

    private List<PreguntaFrecuente>? _preguntasCache;

    /// <summary>
    /// Obtiene todas las preguntas frecuentes con sus respuestas
    /// </summary>
    public List<PreguntaFrecuente> ObtenerPreguntasFrecuentes()
    {
        if (_preguntasCache != null) return _preguntasCache;

        var preguntas = new List<PreguntaFrecuente>();
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            // Obtener preguntas
            var queryPreguntas = @"SELECT id_pregunta, pregunta, orden
                                   FROM preguntas_frecuentes
                                   WHERE activo = true
                                   ORDER BY orden, id_pregunta";
            using var cmdPreguntas = new NpgsqlCommand(queryPreguntas, connection);
            using var readerPreguntas = cmdPreguntas.ExecuteReader();

            while (readerPreguntas.Read())
            {
                preguntas.Add(new PreguntaFrecuente
                {
                    Id = readerPreguntas.GetInt32(0),
                    Pregunta = readerPreguntas.GetString(1),
                    Orden = readerPreguntas.IsDBNull(2) ? 0 : readerPreguntas.GetInt32(2),
                    Activo = true
                });
            }
            readerPreguntas.Close();

            // Obtener respuestas para cada pregunta
            foreach (var pregunta in preguntas)
            {
                var queryRespuestas = @"SELECT id_respuesta, respuesta, orden
                                        FROM respuestas_pregunta
                                        WHERE id_pregunta = @IdPregunta AND activo = true
                                        ORDER BY orden, id_respuesta";
                using var cmdRespuestas = new NpgsqlCommand(queryRespuestas, connection);
                cmdRespuestas.Parameters.AddWithValue("@IdPregunta", pregunta.Id);
                using var readerRespuestas = cmdRespuestas.ExecuteReader();

                while (readerRespuestas.Read())
                {
                    pregunta.Respuestas.Add(new RespuestaPregunta
                    {
                        Id = readerRespuestas.GetInt32(0),
                        IdPregunta = pregunta.Id,
                        Respuesta = readerRespuestas.GetString(1),
                        Orden = readerRespuestas.IsDBNull(2) ? 0 : readerRespuestas.GetInt32(2),
                        Activo = true
                    });
                }
            }

            _preguntasCache = preguntas;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al obtener preguntas frecuentes: {ex.Message}");
        }

        return preguntas;
    }

    /// <summary>
    /// Guarda una nueva pregunta frecuente
    /// </summary>
    public int AgregarPreguntaFrecuente(string pregunta, int orden = 0)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = @"INSERT INTO preguntas_frecuentes (pregunta, orden, activo)
                          VALUES (@Pregunta, @Orden, true)
                          RETURNING id_pregunta";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Pregunta", pregunta);
            cmd.Parameters.AddWithValue("@Orden", orden);

            var newId = cmd.ExecuteScalar();
            _preguntasCache = null;
            return Convert.ToInt32(newId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al agregar pregunta: {ex.Message}");
            return -1;
        }
    }

    /// <summary>
    /// Actualiza una pregunta existente
    /// </summary>
    public void ActualizarPreguntaFrecuente(int idPregunta, string pregunta, int orden)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = @"UPDATE preguntas_frecuentes
                          SET pregunta = @Pregunta, orden = @Orden, fecha_modificacion = CURRENT_TIMESTAMP
                          WHERE id_pregunta = @Id";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", idPregunta);
            cmd.Parameters.AddWithValue("@Pregunta", pregunta);
            cmd.Parameters.AddWithValue("@Orden", orden);
            cmd.ExecuteNonQuery();

            _preguntasCache = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al actualizar pregunta: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina (desactiva) una pregunta y sus respuestas
    /// </summary>
    public void EliminarPreguntaFrecuente(int idPregunta)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            // Desactivar respuestas
            var queryRespuestas = "UPDATE respuestas_pregunta SET activo = false WHERE id_pregunta = @Id";
            using var cmdRespuestas = new NpgsqlCommand(queryRespuestas, connection);
            cmdRespuestas.Parameters.AddWithValue("@Id", idPregunta);
            cmdRespuestas.ExecuteNonQuery();

            // Desactivar pregunta
            var queryPregunta = "UPDATE preguntas_frecuentes SET activo = false WHERE id_pregunta = @Id";
            using var cmdPregunta = new NpgsqlCommand(queryPregunta, connection);
            cmdPregunta.Parameters.AddWithValue("@Id", idPregunta);
            cmdPregunta.ExecuteNonQuery();

            _preguntasCache = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al eliminar pregunta: {ex.Message}");
        }
    }

    /// <summary>
    /// Agrega una respuesta a una pregunta
    /// </summary>
    public int AgregarRespuesta(int idPregunta, string respuesta, int orden = 0)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = @"INSERT INTO respuestas_pregunta (id_pregunta, respuesta, orden, activo)
                          VALUES (@IdPregunta, @Respuesta, @Orden, true)
                          RETURNING id_respuesta";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@IdPregunta", idPregunta);
            cmd.Parameters.AddWithValue("@Respuesta", respuesta);
            cmd.Parameters.AddWithValue("@Orden", orden);

            var newId = cmd.ExecuteScalar();
            _preguntasCache = null;
            return Convert.ToInt32(newId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al agregar respuesta: {ex.Message}");
            return -1;
        }
    }

    /// <summary>
    /// Actualiza una respuesta existente
    /// </summary>
    public void ActualizarRespuesta(int idRespuesta, string respuesta, int orden)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = @"UPDATE respuestas_pregunta
                          SET respuesta = @Respuesta, orden = @Orden
                          WHERE id_respuesta = @Id";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", idRespuesta);
            cmd.Parameters.AddWithValue("@Respuesta", respuesta);
            cmd.Parameters.AddWithValue("@Orden", orden);
            cmd.ExecuteNonQuery();

            _preguntasCache = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al actualizar respuesta: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina (desactiva) una respuesta
    /// </summary>
    public void EliminarRespuesta(int idRespuesta)
    {
        try
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var query = "UPDATE respuestas_pregunta SET activo = false WHERE id_respuesta = @Id";
            using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Id", idRespuesta);
            cmd.ExecuteNonQuery();

            _preguntasCache = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al eliminar respuesta: {ex.Message}");
        }
    }

    #endregion
}
