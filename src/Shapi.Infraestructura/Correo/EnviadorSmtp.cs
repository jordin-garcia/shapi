using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Shapi.Dominio.Correo;

namespace Shapi.Infraestructura.Correo;

public sealed class EnviadorSmtp : IEnviadorCorreo
{
    private readonly string _host;
    private readonly int _puerto;
    private readonly string? _usuario;
    private readonly string? _contrasena;
    private readonly bool _tls;
    private readonly string _remitente;

    public EnviadorSmtp(IConfiguration configuracion)
    {
        _host = configuracion["SHAPI_SMTP_HOST"]?.Trim() ?? "localhost";
        _puerto = LeerPuerto(configuracion["SHAPI_SMTP_PUERTO"]);
        _usuario = NormalizarOpcional(configuracion["SHAPI_SMTP_USUARIO"]);
        _contrasena = configuracion["SHAPI_SMTP_CONTRASENA"];
        _tls = LeerBooleano(configuracion["SHAPI_SMTP_TLS"]);

        var dominioBase = configuracion["SHAPI_DOMINIO_BASE"]?.Trim().TrimEnd('.') ?? "shapi.localhost";
        _remitente = $"no-responder@{dominioBase}";
    }

    public async Task EnviarAsync(
        CorreoSaliente correo,
        CorreoRenderizado contenido,
        CancellationToken cancelacion = default)
    {
        var mensaje = new MimeMessage();
        mensaje.From.Add(new MailboxAddress(contenido.NombreRemitente ?? string.Empty, _remitente));
        mensaje.To.Add(MailboxAddress.Parse(correo.Destinatario));
        mensaje.Subject = correo.Asunto;
        mensaje.Body = new BodyBuilder
        {
            HtmlBody = contenido.Html,
            TextBody = contenido.Texto,
        }.ToMessageBody();

        using var cliente = new SmtpClient { Timeout = 5_000 };
        var seguridad = _tls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await cliente.ConnectAsync(_host, _puerto, seguridad, cancelacion);
        if (_usuario is not null)
        {
            await cliente.AuthenticateAsync(_usuario, _contrasena ?? string.Empty, cancelacion);
        }

        await cliente.SendAsync(mensaje, cancelacion);
        await cliente.DisconnectAsync(quit: true, cancelacion);
    }

    private static int LeerPuerto(string? valor) =>
        int.TryParse(valor, out var puerto) && puerto is > 0 and <= 65_535
            ? puerto
            : 1025;

    private static bool LeerBooleano(string? valor) =>
        bool.TryParse(valor, out var resultado) && resultado;

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
