using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Allva.Desktop.Services;

namespace Allva.Desktop.Views.UserBoton;

public partial class CambiarContrasenaView : Window
{
    private readonly EmailService _emailService;
    private readonly string _emailDestino;
    private readonly string _nombreUsuario;

    public bool EmailEnviado { get; private set; } = false;
    public string? CodigoGenerado { get; private set; }

    public CambiarContrasenaView()
    {
        InitializeComponent();
        _emailService = new EmailService();
        _emailDestino = "";
        _nombreUsuario = "";
    }

    public CambiarContrasenaView(string emailDestino, string nombreUsuario)
    {
        InitializeComponent();
        _emailService = new EmailService();
        _emailDestino = emailDestino;
        _nombreUsuario = nombreUsuario;

        EmailDestinoText.Text = OcultarEmail(emailDestino);
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

    private async void Enviar_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_emailDestino))
        {
            MostrarMensaje("No hay correo registrado para este usuario", true);
            return;
        }

        // Deshabilitar botones y mostrar loading
        EnviarBtn.IsEnabled = false;
        CancelarBtn.IsEnabled = false;
        LoadingPanel.IsVisible = true;
        MensajeEstado.IsVisible = false;

        try
        {
            var resultado = await _emailService.EnviarEmailRecuperacionAsync(_emailDestino, _nombreUsuario);

            if (resultado.Success)
            {
                EmailEnviado = true;
                CodigoGenerado = resultado.CodigoTemporal;
                MostrarMensaje("Codigo enviado correctamente. Revisa tu correo.", false);

                // Esperar un momento y cerrar
                await Task.Delay(2000);
                Close();
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

    private void MostrarMensaje(string mensaje, bool esError)
    {
        MensajeEstado.Text = mensaje;
        MensajeEstado.Foreground = new Avalonia.Media.SolidColorBrush(
            esError ? Avalonia.Media.Color.Parse("#CC0000") : Avalonia.Media.Color.Parse("#008800")
        );
        MensajeEstado.IsVisible = true;
    }

    private void Cancelar_Click(object? sender, RoutedEventArgs e)
    {
        EmailEnviado = false;
        Close();
    }
}
