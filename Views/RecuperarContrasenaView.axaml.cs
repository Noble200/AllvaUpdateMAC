using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Npgsql;
using Allva.Desktop.Services;
using Allva.Desktop.Views.UserBoton;

namespace Allva.Desktop.Views;

public partial class RecuperarContrasenaView : Window
{
    private readonly EmailService _emailService;
    private const string ConnectionString = "Host=switchyard.proxy.rlwy.net;Port=55839;Database=railway;Username=postgres;Password=ysTQxChOYSWUuAPzmYQokqrjpYnKSGbk;";

    private string? _emailUsuario;
    private string? _nombreUsuario;
    private int _idUsuario;
    private bool _usuarioEncontrado = false;
    private string? _codigoGenerado;

    public RecuperarContrasenaView()
    {
        InitializeComponent();
        _emailService = new EmailService();
    }

    private async void Enviar_Click(object? sender, RoutedEventArgs e)
    {
        if (!_usuarioEncontrado)
        {
            await BuscarUsuarioAsync();
        }
        else
        {
            await EnviarCodigoAsync();
        }
    }

    private async Task BuscarUsuarioAsync()
    {
        var numeroUsuario = NumeroUsuarioTextBox.Text?.Trim().ToUpper() ?? "";

        if (string.IsNullOrEmpty(numeroUsuario))
        {
            MostrarMensaje("Ingresa tu usuario", true);
            return;
        }

        EnviarBtn.IsEnabled = false;
        CancelarBtn.IsEnabled = false;
        LoadingPanel.IsVisible = true;
        MensajeEstado.IsVisible = false;

        try
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT id_usuario, nombre, apellidos, correo
                FROM usuarios
                WHERE UPPER(numero_usuario) = @numeroUsuario
                AND activo = true";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@numeroUsuario", numeroUsuario);

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                _idUsuario = reader.GetInt32(0);
                var nombre = reader.IsDBNull(1) ? "" : reader.GetString(1);
                var apellidos = reader.IsDBNull(2) ? "" : reader.GetString(2);
                _emailUsuario = reader.IsDBNull(3) ? "" : reader.GetString(3);
                _nombreUsuario = $"{nombre} {apellidos}".Trim();

                if (string.IsNullOrEmpty(_emailUsuario))
                {
                    MostrarMensaje("Este usuario no tiene un correo registrado. Contacta al administrador.", true);
                    EnviarBtn.IsEnabled = true;
                    CancelarBtn.IsEnabled = true;
                    return;
                }

                EmailDestinoText.Text = OcultarEmail(_emailUsuario);
                EmailEncontradoPanel.IsVisible = true;
                _usuarioEncontrado = true;
                EnviarBtn.Content = "Enviar Codigo";
                NumeroUsuarioTextBox.IsEnabled = false;

                MostrarMensaje("Usuario encontrado. Presiona 'Enviar Codigo' para continuar.", false);
            }
            else
            {
                MostrarMensaje("No se encontro ningun usuario con ese nombre", true);
            }

            EnviarBtn.IsEnabled = true;
            CancelarBtn.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error: {ex.Message}", true);
            EnviarBtn.IsEnabled = true;
            CancelarBtn.IsEnabled = true;
        }
        finally
        {
            LoadingPanel.IsVisible = false;
        }
    }

    private async Task EnviarCodigoAsync()
    {
        if (string.IsNullOrEmpty(_emailUsuario) || string.IsNullOrEmpty(_nombreUsuario))
        {
            MostrarMensaje("Error: datos de usuario no disponibles", true);
            return;
        }

        EnviarBtn.IsEnabled = false;
        CancelarBtn.IsEnabled = false;
        LoadingPanel.IsVisible = true;
        MensajeEstado.IsVisible = false;

        try
        {
            var resultado = await _emailService.EnviarEmailRecuperacionAsync(_emailUsuario, _nombreUsuario);

            if (resultado.Success)
            {
                _codigoGenerado = resultado.CodigoTemporal;
                MostrarMensaje("Codigo enviado correctamente. Revisa tu correo.", false);

                await Task.Delay(1500);

                var ventanaVerificacion = new VerificarCodigoView(
                    _codigoGenerado!,
                    _idUsuario,
                    _nombreUsuario
                );

                Close();

                if (Owner is Window ownerWindow)
                {
                    await ventanaVerificacion.ShowDialog(ownerWindow);
                }
                else
                {
                    ventanaVerificacion.Show();
                }
            }
            else
            {
                MostrarMensaje($"Error al enviar: {resultado.Error}", true);
                EnviarBtn.IsEnabled = true;
                CancelarBtn.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error: {ex.Message}", true);
            EnviarBtn.IsEnabled = true;
            CancelarBtn.IsEnabled = true;
        }
        finally
        {
            LoadingPanel.IsVisible = false;
        }
    }

    private string OcultarEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return email;

        var partes = email.Split('@');
        var usuario = partes[0];
        var dominio = partes[1];

        if (usuario.Length <= 2)
            return email;

        var usuarioOculto = usuario[0] + new string('*', Math.Min(usuario.Length - 2, 5)) + usuario[^1];
        return $"{usuarioOculto}@{dominio}";
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
        Close();
    }
}
