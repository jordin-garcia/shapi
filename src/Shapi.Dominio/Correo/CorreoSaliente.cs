using System.Text.Json.Nodes;

namespace Shapi.Dominio.Correo;

public class CorreoSaliente
{
    /// <summary>Espera antes de cada uno de los 5 reintentos (RF-46). Si falla el quinto, el correo queda fallido.</summary>
    private static readonly TimeSpan[] EsperasReintento =
    [
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromHours(1),
    ];

    public Guid Id { get; private set; }
    public string Destinatario { get; private set; } = null!;
    public string Asunto { get; private set; } = null!;
    public string Plantilla { get; private set; } = null!;
    public string Datos { get; private set; } = null!; // JSONB
    public EstadoCorreo Estado { get; private set; }
    public int Intentos { get; private set; }
    public DateTimeOffset? ProximoIntentoEn { get; private set; }
    public string? UltimoError { get; private set; }
    public DateTimeOffset? EnviadoEn { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }


    public CorreoSaliente(
        string plantilla,
        string destinatario,
        string datos,
        string asunto,
        DateTimeOffset proximoIntentoEn)
    {
        Id = Guid.CreateVersion7();
        Estado = EstadoCorreo.Pendiente;
        Plantilla = plantilla;
        Destinatario = destinatario;
        Datos = datos;
        Asunto = asunto;
        ProximoIntentoEn = proximoIntentoEn;
    }

    protected CorreoSaliente() { }

    public void MarcarEnviado(DateTimeOffset enviadoEn)
    {
        Estado = EstadoCorreo.Enviado;
        EnviadoEn = enviadoEn;
        ProximoIntentoEn = null;
        UltimoError = null;
        QuitarToken();
    }

    public void RegistrarFallo(string error, DateTimeOffset ahora)
    {
        if (Estado != EstadoCorreo.Pendiente)
        {
            throw new InvalidOperationException("Solo un correo pendiente puede registrar un fallo.");
        }

        Intentos++;
        UltimoError = error;

        if (Intentos > EsperasReintento.Length)
        {
            Estado = EstadoCorreo.Fallido;
            ProximoIntentoEn = null;
            QuitarToken();
            return;
        }

        ProximoIntentoEn = ahora + EsperasReintento[Intentos - 1];
    }

    /// <summary>
    /// El token solo se guarda en claro mientras el correo espera su envío: al quedar enviado o fallido ya no se
    /// necesita y se borra de los datos (10 §3 y §8).
    /// </summary>
    private void QuitarToken()
    {
        if (JsonNode.Parse(Datos) is JsonObject datos && datos.Remove("token"))
        {
            Datos = datos.ToJsonString();
        }
    }
}
