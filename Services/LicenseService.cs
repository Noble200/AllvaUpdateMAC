using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Npgsql;

namespace Allva.Desktop.Services;

public class LicenseService
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    public async Task<(bool IsValid, string Message)> ValidarYUsarLicenciaAsync(string codigoLicencia)
    {
        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT id_licencia, usada, nombre_cliente, activa 
                FROM licencias 
                WHERE UPPER(codigo_licencia) = UPPER(@codigo)";

            await using var cmd = new NpgsqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@codigo", codigoLicencia.Trim());

            await using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return (false, "Código de licencia inválido");
            }

            var usada = reader.GetBoolean(1);
            var activa = reader.GetBoolean(3);

            if (usada)
            {
                return (false, "Esta licencia ya ha sido utilizada");
            }

            if (!activa)
            {
                return (false, "Esta licencia no está activa");
            }

            var nombreCliente = reader.IsDBNull(2) ? "Sin asignar" : reader.GetString(2);
            await reader.CloseAsync();

            var macAddress = ObtenerMacAddress();
            var nombreEquipo = Environment.MachineName;

            var updateQuery = @"
                UPDATE licencias 
                SET usada = TRUE, 
                    fecha_activacion = CURRENT_TIMESTAMP,
                    id_maquina = @mac
                WHERE UPPER(codigo_licencia) = UPPER(@codigo)";

            await using var updateCmd = new NpgsqlCommand(updateQuery, connection);
            updateCmd.Parameters.AddWithValue("@codigo", codigoLicencia.Trim());
            updateCmd.Parameters.AddWithValue("@mac", macAddress ?? "N/A");
            await updateCmd.ExecuteNonQueryAsync();

            GuardarActivacionLocal(codigoLicencia, nombreEquipo);

            return (true, $"Licencia activada correctamente para: {nombreCliente}");
        }
        catch (Exception ex)
        {
            return (false, $"Error al validar licencia: {ex.Message}");
        }
    }

    public bool EstaActivada()
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var licenseFile = System.IO.Path.Combine(appDataPath, "AllvaSystem", "license.key");
            
            return System.IO.File.Exists(licenseFile);
        }
        catch
        {
            return false;
        }
    }

    private void GuardarActivacionLocal(string codigoLicencia, string nombreEquipo)
    {
        try
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var allvaFolder = System.IO.Path.Combine(appDataPath, "AllvaSystem");
            
            if (!System.IO.Directory.Exists(allvaFolder))
            {
                System.IO.Directory.CreateDirectory(allvaFolder);
            }

            var licenseFile = System.IO.Path.Combine(allvaFolder, "license.key");
            var data = $"{codigoLicencia}|{DateTime.UtcNow:O}|{nombreEquipo}";
            var encryptedLicense = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data));
            
            System.IO.File.WriteAllText(licenseFile, encryptedLicense);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al guardar activación: {ex.Message}");
        }
    }

    private string? ObtenerMacAddress()
    {
        try
        {
            var mac = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up && 
                             nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            return mac;
        }
        catch
        {
            return null;
        }
    }
}