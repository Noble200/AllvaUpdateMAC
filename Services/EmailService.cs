using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Allva.Desktop.Services;

/// <summary>
/// Servicio para envío de emails usando SMTP de Gmail
/// </summary>
public class EmailService
{
    private const string SmtpHost = "smtp.gmail.com";
    private const int SmtpPort = 587;
    private const string FromEmail = "allvasoftware@gmail.com";
    private const string FromPassword = "rfpq gxdv fmbh wzqv";
    private const string FromName = "AllvaSystem";

    /// <summary>
    /// Envía un email de recuperación de contraseña con un código temporal
    /// </summary>
    public async Task<(bool Success, string? CodigoTemporal, string? Error)> EnviarEmailRecuperacionAsync(
        string destinatarioEmail,
        string nombreUsuario)
    {
        try
        {
            // Generar código temporal de 6 dígitos
            var codigoTemporal = GenerarCodigoTemporal();

            var asunto = "Recuperación de contraseña - AllvaSystem";
            var cuerpo = GenerarCuerpoEmail(nombreUsuario, codigoTemporal);

            using var smtp = new SmtpClient(SmtpHost, SmtpPort)
            {
                Credentials = new NetworkCredential(FromEmail, FromPassword),
                EnableSsl = true
            };

            var mensaje = new MailMessage
            {
                From = new MailAddress(FromEmail, FromName),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = true
            };
            mensaje.To.Add(destinatarioEmail);

            await smtp.SendMailAsync(mensaje);

            return (true, codigoTemporal, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Genera un código temporal de 6 dígitos
    /// </summary>
    private string GenerarCodigoTemporal()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    /// <summary>
    /// Genera el cuerpo HTML del email
    /// </summary>
    private string GenerarCuerpoEmail(string nombreUsuario, string codigo)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
        <div style='background-color: #0b5394; padding: 30px; text-align: center;'>
            <h1 style='color: white; margin: 0; font-size: 24px;'>AllvaSystem</h1>
        </div>
        <div style='padding: 40px;'>
            <h2 style='color: #333; margin-top: 0;'>Recuperación de contraseña</h2>
            <p style='color: #666; font-size: 16px;'>Hola <strong>{nombreUsuario}</strong>,</p>
            <p style='color: #666; font-size: 16px;'>Has solicitado restablecer tu contraseña. Utiliza el siguiente código para continuar:</p>
            <div style='background-color: #f8f9fa; border: 2px dashed #0b5394; border-radius: 8px; padding: 20px; text-align: center; margin: 30px 0;'>
                <span style='font-size: 32px; font-weight: bold; color: #0b5394; letter-spacing: 8px;'>{codigo}</span>
            </div>
            <p style='color: #666; font-size: 14px;'>Este código expira en <strong>15 minutos</strong>.</p>
            <p style='color: #666; font-size: 14px;'>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
            <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;'>
            <p style='color: #999; font-size: 12px; text-align: center;'>Este es un mensaje automático, por favor no respondas a este correo.</p>
        </div>
    </div>
</body>
</html>";
    }
}
