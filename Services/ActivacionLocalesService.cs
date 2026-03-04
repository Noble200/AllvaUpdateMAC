using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Allva.Desktop.Models;

namespace Allva.Desktop.Services;

public class ActivacionLocalesService
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    /// <summary>
    /// Obtiene todos los comercios activos con conteo de locales
    /// </summary>
    public async Task<List<ComercioInfo>> ObtenerComerciosAsync()
    {
        var comercios = new List<ComercioInfo>();

        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT c.id_comercio, c.nombre_comercio, c.nombre_srl, c.pais, c.activo,
                       COUNT(l.id_local) as cantidad_locales
                FROM comercios c
                LEFT JOIN locales l ON c.id_comercio = l.id_comercio
                GROUP BY c.id_comercio, c.nombre_comercio, c.nombre_srl, c.pais, c.activo
                ORDER BY c.nombre_comercio";

            await using var cmd = new NpgsqlCommand(query, connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                comercios.Add(new ComercioInfo
                {
                    IdComercio = reader.GetInt32(0),
                    NombreComercio = reader.GetString(1),
                    NombreSRL = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Pais = reader.GetString(3),
                    Activo = reader.GetBoolean(4),
                    CantidadLocales = reader.GetInt32(5)
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener comercios: {ex.Message}");
        }

        return comercios;
    }

    /// <summary>
    /// Obtiene todos los locales de un comercio específico
    /// </summary>
    public async Task<List<LocalInfo>> ObtenerLocalesPorComercioAsync(int idComercio)
    {
        var locales = new List<LocalInfo>();

        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT id_local, codigo_local, nombre_local, direccion, activo
                FROM locales
                WHERE id_comercio = @idComercio
                ORDER BY codigo_local";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@idComercio", idComercio);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                locales.Add(new LocalInfo
                {
                    IdLocal = reader.GetInt32(0),
                    CodigoLocal = reader.GetString(1),
                    NombreLocal = reader.GetString(2),
                    Direccion = reader.GetString(3),
                    Activo = reader.GetBoolean(4)
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener locales: {ex.Message}");
        }

        return locales;
    }

    /// <summary>
    /// Obtiene todos los locales de la base de datos
    /// </summary>
    public async Task<List<LocalInfo>> ObtenerTodosLosLocalesAsync()
    {
        var locales = new List<LocalInfo>();

        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT l.id_local, l.codigo_local, l.nombre_local, l.direccion, l.activo,
                       l.id_comercio, c.nombre_comercio
                FROM locales l
                INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                ORDER BY l.codigo_local";

            await using var cmd = new NpgsqlCommand(query, connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                locales.Add(new LocalInfo
                {
                    IdLocal = reader.GetInt32(0),
                    CodigoLocal = reader.GetString(1),
                    NombreLocal = reader.GetString(2),
                    Direccion = reader.GetString(3),
                    Activo = reader.GetBoolean(4),
                    IdComercio = reader.GetInt32(5),
                    NombreComercio = reader.GetString(6)
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener locales: {ex.Message}");
        }

        return locales;
    }

    /// <summary>
    /// Genera contraseñas de activación para todos los locales de un comercio
    /// </summary>
    public async Task<List<ActivacionLocalData>> GenerarActivacionesComercioAsync(int idComercio, int idAdministrador)
    {
        var activaciones = new List<ActivacionLocalData>();

        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Obtener los locales del comercio
            var locales = await ObtenerLocalesPorComercioAsync(idComercio);

            foreach (var local in locales)
            {
                var contrasena = GenerarContrasenaAleatoria();

                var insertQuery = @"
                    INSERT INTO activaciones_locales
                    (id_local, codigo_local, contrasena_temporal, generado_por, usado)
                    VALUES (@idLocal, @codigoLocal, @contrasena, @generadoPor, FALSE)
                    RETURNING id_activacion";

                await using var cmd = new NpgsqlCommand(insertQuery, connection);
                cmd.Parameters.AddWithValue("@idLocal", local.IdLocal);
                cmd.Parameters.AddWithValue("@codigoLocal", local.CodigoLocal);
                cmd.Parameters.AddWithValue("@contrasena", contrasena);
                cmd.Parameters.AddWithValue("@generadoPor", idAdministrador);

                var result = await cmd.ExecuteScalarAsync();
                var idActivacion = result != null ? (int)result : 0;

                activaciones.Add(new ActivacionLocalData
                {
                    IdActivacion = idActivacion,
                    IdLocal = local.IdLocal,
                    CodigoLocal = local.CodigoLocal,
                    NombreLocal = local.NombreLocal,
                    ContrasenaTemporal = contrasena,
                    FechaGeneracion = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar activaciones: {ex.Message}");
            throw;
        }

        return activaciones;
    }

    /// <summary>
    /// Genera una contraseña de activación para un solo local
    /// </summary>
    public async Task<ActivacionLocalData?> GenerarActivacionLocalAsync(int idLocal, int idAdministrador)
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Obtener información del local
            var queryLocal = @"
                SELECT l.codigo_local, l.nombre_local, c.nombre_comercio
                FROM locales l
                INNER JOIN comercios c ON l.id_comercio = c.id_comercio
                WHERE l.id_local = @idLocal";

            string codigoLocal;
            string nombreLocal;
            string nombreComercio;

            await using (var cmdLocal = new NpgsqlCommand(queryLocal, connection))
            {
                cmdLocal.Parameters.AddWithValue("@idLocal", idLocal);
                await using var reader = await cmdLocal.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return null;

                codigoLocal = reader.GetString(0);
                nombreLocal = reader.GetString(1);
                nombreComercio = reader.GetString(2);
            }

            // Generar contraseña
            var contrasena = GenerarContrasenaAleatoria();

            // Insertar activación
            var insertQuery = @"
                INSERT INTO activaciones_locales
                (id_local, codigo_local, contrasena_temporal, generado_por, usado)
                VALUES (@idLocal, @codigoLocal, @contrasena, @generadoPor, FALSE)
                RETURNING id_activacion";

            await using var cmd = new NpgsqlCommand(insertQuery, connection);
            cmd.Parameters.AddWithValue("@idLocal", idLocal);
            cmd.Parameters.AddWithValue("@codigoLocal", codigoLocal);
            cmd.Parameters.AddWithValue("@contrasena", contrasena);
            cmd.Parameters.AddWithValue("@generadoPor", idAdministrador);

            var result = await cmd.ExecuteScalarAsync();
            var idActivacion = result != null ? (int)result : 0;

            return new ActivacionLocalData
            {
                IdActivacion = idActivacion,
                IdLocal = idLocal,
                CodigoLocal = codigoLocal,
                NombreLocal = nombreLocal,
                NombreComercio = nombreComercio,
                ContrasenaTemporal = contrasena,
                FechaGeneracion = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar activación: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Valida y usa una contraseña de activación
    /// </summary>
    public async Task<(bool IsValid, string Message, int? IdLocal)> ValidarYUsarActivacionAsync(string codigoLocal, string contrasena)
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Buscar la activación
            var query = @"
                SELECT id_activacion, id_local, usado
                FROM activaciones_locales
                WHERE UPPER(codigo_local) = UPPER(@codigoLocal)
                  AND contrasena_temporal = @contrasena";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@codigoLocal", codigoLocal.Trim());
            cmd.Parameters.AddWithValue("@contrasena", contrasena.Trim());

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return (false, "Código de local o contraseña incorrectos", null);
            }

            var idActivacion = reader.GetInt32(0);
            var idLocal = reader.GetInt32(1);
            var usado = reader.GetBoolean(2);

            if (usado)
            {
                return (false, "Esta contraseña ya ha sido utilizada", null);
            }

            await reader.CloseAsync();

            // Marcar como usado y borrar la contraseña
            var deleteQuery = @"
                DELETE FROM activaciones_locales
                WHERE id_activacion = @idActivacion";

            await using var deleteCmd = new NpgsqlCommand(deleteQuery, connection);
            deleteCmd.Parameters.AddWithValue("@idActivacion", idActivacion);
            await deleteCmd.ExecuteNonQueryAsync();

            return (true, "Activación correcta", idLocal);
        }
        catch (Exception ex)
        {
            return (false, $"Error al validar activación: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Genera una contraseña aleatoria segura
    /// </summary>
    private string GenerarContrasenaAleatoria()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Sin caracteres confusos (I, O, 0, 1)
        var random = new Random();

        // Formato: XXXX-XXXX-XXXX (12 caracteres divididos en 3 grupos)
        var parte1 = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        var parte2 = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        var parte3 = new string(Enumerable.Range(0, 4).Select(_ => chars[random.Next(chars.Length)]).ToArray());

        return $"{parte1}-{parte2}-{parte3}";
    }
}

// Modelos auxiliares
public class ComercioInfo
{
    public int IdComercio { get; set; }
    public string NombreComercio { get; set; } = string.Empty;
    public string? NombreSRL { get; set; }
    public string Pais { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int CantidadLocales { get; set; }
}

public class LocalInfo
{
    public int IdLocal { get; set; }
    public string CodigoLocal { get; set; } = string.Empty;
    public string NombreLocal { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int IdComercio { get; set; }
    public string NombreComercio { get; set; } = string.Empty;
}

public class ActivacionLocalData
{
    public int IdActivacion { get; set; }
    public int IdLocal { get; set; }
    public string CodigoLocal { get; set; } = string.Empty;
    public string NombreLocal { get; set; } = string.Empty;
    public string NombreComercio { get; set; } = string.Empty;
    public string ContrasenaTemporal { get; set; } = string.Empty;
    public DateTime FechaGeneracion { get; set; }
}
