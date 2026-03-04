using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;

namespace Allva.Desktop.Views.UserBoton;

public partial class CambioDirectoContrasenaView : Window
{
    private readonly int _idUsuario;
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    public bool ContrasenaActualizada { get; private set; } = false;

    public CambioDirectoContrasenaView()
    {
        InitializeComponent();
        _idUsuario = 0;
    }

    public CambioDirectoContrasenaView(int idUsuario)
    {
        InitializeComponent();
        _idUsuario = idUsuario;
    }

    private async void Guardar_Click(object? sender, RoutedEventArgs e)
    {
        var contrasenaActual = ContrasenaActualTextBox.Text ?? "";
        var nuevaContrasena = NuevaContrasenaTextBox.Text ?? "";
        var confirmarContrasena = ConfirmarContrasenaTextBox.Text ?? "";

        // Validaciones
        if (string.IsNullOrEmpty(contrasenaActual))
        {
            MostrarMensaje("Ingresa tu contrasena actual", true);
            return;
        }

        if (string.IsNullOrEmpty(nuevaContrasena))
        {
            MostrarMensaje("Ingresa la nueva contrasena", true);
            return;
        }

        if (nuevaContrasena.Length < 6)
        {
            MostrarMensaje("La contrasena debe tener al menos 6 caracteres", true);
            return;
        }

        if (nuevaContrasena != confirmarContrasena)
        {
            MostrarMensaje("Las contrasenas no coinciden", true);
            return;
        }

        // Deshabilitar UI
        GuardarBtn.IsEnabled = false;
        CancelarBtn.IsEnabled = false;

        try
        {
            // Verificar contraseña actual
            var contrasenaValida = await VerificarContrasenaActualAsync(contrasenaActual);

            if (!contrasenaValida)
            {
                MostrarMensaje("La contrasena actual es incorrecta", true);
                GuardarBtn.IsEnabled = true;
                CancelarBtn.IsEnabled = true;
                return;
            }

            // Actualizar contraseña
            var resultado = await ActualizarContrasenaAsync(nuevaContrasena);

            if (resultado)
            {
                ContrasenaActualizada = true;
                MostrarMensaje("Contrasena actualizada correctamente", false);
                await Task.Delay(1500);
                Close();
            }
            else
            {
                MostrarMensaje("Error al actualizar la contrasena", true);
                GuardarBtn.IsEnabled = true;
                CancelarBtn.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error: {ex.Message}", true);
            GuardarBtn.IsEnabled = true;
            CancelarBtn.IsEnabled = true;
        }
    }

    private async Task<bool> VerificarContrasenaActualAsync(string contrasena)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var sql = "SELECT password_hash FROM usuarios WHERE id_usuario = @idUsuario";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idUsuario", _idUsuario);

        var hashActual = await command.ExecuteScalarAsync() as string;

        if (string.IsNullOrEmpty(hashActual))
            return false;

        return BCrypt.Net.BCrypt.Verify(contrasena, hashActual);
    }

    private async Task<bool> ActualizarContrasenaAsync(string nuevaContrasena)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(nuevaContrasena);

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var sql = @"
            UPDATE usuarios
            SET password_hash = @passwordHash,
                fecha_modificacion = NOW()
            WHERE id_usuario = @idUsuario";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);
        command.Parameters.AddWithValue("@idUsuario", _idUsuario);

        var filasAfectadas = await command.ExecuteNonQueryAsync();
        return filasAfectadas > 0;
    }

    private void MostrarMensaje(string mensaje, bool esError)
    {
        MensajeEstado.Text = mensaje;
        MensajeEstado.Foreground = new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.Parse(esError ? "#CC0000" : "#008800")
        );
        MensajeEstado.IsVisible = true;
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        ContrasenaActualizada = false;
        Close();
    }
}
