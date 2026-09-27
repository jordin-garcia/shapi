using System.Net.Security;
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
    private readonly RemoteCertificateValidationCallback? _validarCertificado;

    public EnviadorSmtp(IConfiguration configuracion)
        : this(configuracion, validarCertificado: null)
    {
    }

    /// <summary>Solo para las pruebas: acepta el certificado autofirmado de un Mailpit con TLS.</summary>
    internal EnviadorSmtp(IConfiguration configuracion, RemoteCertificateValidationCallback? validarCertificado)
    {
        _validarCertificado = validarCertificado;
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
        if (_validarCertificado is not null)
        {
            cliente.ServerCertificateValidationCallback = _validarCertificado;
        }

        await cliente.ConnectAsync(_host, _puerto, SeguridadPara(_tls, _puerto), cancelacion);
        if (_usuario is not null)
        {
            await cliente.AuthenticateAsync(_usuario, _contrasena ?? string.Empty, cancelacion);
        }

        await cliente.SendAsync(mensaje, cancelacion);

        // El servidor ya aceptó el correo: un error o una cancelación al cerrar la sesión no debe provocar un
        // reenvío (auditoría H-63).
        try
        {
            await cliente.DisconnectAsync(quit: true, CancellationToken.None);
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Con <c>SHAPI_SMTP_TLS=true</c> el cifrado es obligatorio: SMTPS implícito en el puerto 465 y STARTTLS en los
    /// demás. Nunca se degrada a texto plano si el servidor no ofrece STARTTLS (10 §6).
    /// </summary>
    internal static SecureSocketOptions SeguridadPara(bool tls, int puerto) => (tls, puerto) switch
    {
        (false, _) => SecureSocketOptions.None,
        (true, 465) => SecureSocketOptions.SslOnConnect,
        (true, _) => SecureSocketOptions.StartTls,
    };

    private static int LeerPuerto(string? valor) =>
        int.TryParse(valor, out var puerto) && puerto is > 0 and <= 65_535
            ? puerto
            : 1025;

    private static bool LeerBooleano(string? valor) =>
        bool.TryParse(valor, out var resultado) && resultado;

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
