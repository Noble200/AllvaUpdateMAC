using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Npgsql;

namespace Allva.Desktop.ViewModels;

/// <summary>
/// ViewModel para el diálogo de verificación de contraseña de administración
/// </summary>
public partial class PasswordAdminDialogViewModel : ObservableObject
{
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    [ObservableProperty]
    private string _passwordInput = string.Empty;

    [ObservableProperty]
    private bool _mostrarError = false;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private bool _validandoPassword = false;

    private readonly int _idComercio;

    public PasswordAdminDialogViewModel(int idComercio)
    {
        _idComercio = idComercio;
    }

    /// <summary>
    /// Verifica si la contraseña ingresada coincide con la almacenada en la base de datos
    /// </summary>
    public async Task<bool> VerificarPasswordAsync()
    {
        // Validar que se haya ingresado una contraseña
        if (string.IsNullOrWhiteSpace(PasswordInput))
        {
            MostrarError = true;
            MensajeError = "Por favor, ingrese la contraseña.";
            return false;
        }

        ValidandoPassword = true;
        MostrarError = false;
        MensajeError = string.Empty;

        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Obtener la contraseña hash del comercio
            var query = @"
                SELECT contrasena_hash, contrasena_visible, activo
                FROM contrasenas_admin_front
                WHERE id_comercio = @idComercio AND activo = true";

            await using var command = new NpgsqlCommand(query, connection);
            command.Parameters.AddWithValue("@idComercio", _idComercio);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var contrasenaHash = reader.GetString(0);
                var contrasenaVisible = reader.GetString(1);
                var activo = reader.GetBoolean(2);

                if (!activo)
                {
                    MostrarError = true;
                    MensajeError = "No hay contraseña configurada para este comercio.";
                    return false;
                }

                // Verificar la contraseña
                var passwordHash = HashPassword(PasswordInput.Trim());

                // Comparar con el hash almacenado
                if (passwordHash == contrasenaHash)
                {
                    // Contraseña correcta
                    return true;
                }
                else
                {
                    // También verificar con la contraseña visible por si acaso
                    if (PasswordInput.Trim() == contrasenaVisible)
                    {
                        return true;
                    }

                    MostrarError = true;
                    MensajeError = "Contraseña incorrecta. Intente nuevamente.";
                    PasswordInput = string.Empty;
                    return false;
                }
            }
            else
            {
                MostrarError = true;
                MensajeError = "No hay contraseña configurada para este comercio.";
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al verificar contraseña: {ex.Message}");
            MostrarError = true;
            MensajeError = $"Error al verificar la contraseña: {ex.Message}";
            return false;
        }
        finally
        {
            ValidandoPassword = false;
        }
    }

    /// <summary>
    /// Genera el hash SHA256 de una contraseña
    /// </summary>
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
