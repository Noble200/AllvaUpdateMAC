using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;

namespace Allva.Desktop.Views.UserBoton;

public partial class VerificarCodigoView : Window
{
    private readonly string _codigoEsperado;
    private readonly int _idUsuario;
    private readonly string _nombreUsuario;
    private readonly DateTime _expiracion;

    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    private bool _mostrandoNuevaContrasena = false;
    private bool _mostrandoConfirmarContrasena = false;

    public bool ContrasenaActualizada { get; private set; } = false;

    public VerificarCodigoView()
    {
        InitializeComponent();
        _codigoEsperado = "";
        _idUsuario = 0;
        _nombreUsuario = "";
        _expiracion = DateTime.Now.AddMinutes(15);
    }

    public VerificarCodigoView(string codigoEsperado, int idUsuario, string nombreUsuario)
    {
        InitializeComponent();
        _codigoEsperado = codigoEsperado;
        _idUsuario = idUsuario;
        _nombreUsuario = nombreUsuario;
        _expiracion = DateTime.Now.AddMinutes(15);
    }

    private async void Confirmar_Click(object? sender, RoutedEventArgs e)
    {
        // Validar que no haya expirado
        if (DateTime.Now > _expiracion)
        {
            MostrarError("El codigo ha expirado. Solicita uno nuevo.");
            return;
        }

        var codigo = CodigoTextBox.Text?.Trim() ?? "";
        var nuevaContrasena = NuevaContrasenaTextBox.Text ?? "";
        var confirmarContrasena = ConfirmarContrasenaTextBox.Text ?? "";

        // Validaciones
        if (string.IsNullOrEmpty(codigo))
        {
            MostrarError("Ingresa el codigo de verificacion.");
            return;
        }

        if (codigo != _codigoEsperado)
        {
            MostrarError("El codigo ingresado es incorrecto.");
            return;
        }

        if (string.IsNullOrEmpty(nuevaContrasena))
        {
            MostrarError("Ingresa la nueva contrasena.");
            return;
        }

        if (nuevaContrasena.Length < 6)
        {
            MostrarError("La contrasena debe tener al menos 6 caracteres.");
            return;
        }

        if (nuevaContrasena != confirmarContrasena)
        {
            MostrarError("Las contrasenas no coinciden. Verifica e intenta de nuevo.");
            return;
        }

        // Deshabilitar UI
        ConfirmarBtn.IsEnabled = false;
        CancelarBtn.IsEnabled = false;
        LoadingPanel.IsVisible = true;
        MensajeEstado.IsVisible = false;

        try
        {
            var resultado = await ActualizarContrasenaAsync(nuevaContrasena);

            if (resultado)
            {
                ContrasenaActualizada = true;
                MostrarExito("Contrasena actualizada correctamente.");
                await Task.Delay(2000);
                Close();
            }
            else
            {
                MostrarError("Error al actualizar la contrasena. Intenta de nuevo.");
                ConfirmarBtn.IsEnabled = true;
                CancelarBtn.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
            ConfirmarBtn.IsEnabled = true;
            CancelarBtn.IsEnabled = true;
        }
        finally
        {
            LoadingPanel.IsVisible = false;
        }
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

    private void VerNuevaContrasena_Click(object? sender, RoutedEventArgs e)
    {
        _mostrandoNuevaContrasena = !_mostrandoNuevaContrasena;
        NuevaContrasenaTextBox.PasswordChar = _mostrandoNuevaContrasena ? '\0' : '*';
        VerNuevaContrasenaBtn.Content = _mostrandoNuevaContrasena ? "🙈" : "👁";
    }

    private void VerConfirmarContrasena_Click(object? sender, RoutedEventArgs e)
    {
        _mostrandoConfirmarContrasena = !_mostrandoConfirmarContrasena;
        ConfirmarContrasenaTextBox.PasswordChar = _mostrandoConfirmarContrasena ? '\0' : '*';
        VerConfirmarContrasenaBtn.Content = _mostrandoConfirmarContrasena ? "🙈" : "👁";
    }

    private void MostrarError(string mensaje)
    {
        MensajeEstado.Text = mensaje;
        MensajeEstado.Foreground = new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.Parse("#CC0000")
        );
        MensajeEstado.IsVisible = true;
    }

    private void MostrarExito(string mensaje)
    {
        MensajeEstado.Text = mensaje;
        MensajeEstado.Foreground = new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.Parse("#008800")
        );
        MensajeEstado.IsVisible = true;
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        ContrasenaActualizada = false;
        Close();
    }
}
