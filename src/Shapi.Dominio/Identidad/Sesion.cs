using System.Net;

namespace Shapi.Dominio.Identidad;

public class Sesion
{
    /// <summary>Duración máxima de una sesión desde que se inicia (10 §1).</summary>
    public static readonly TimeSpan DuracionMaxima = TimeSpan.FromDays(7);

    /// <summary>Cada cuánto, como máximo, se actualiza <see cref="UltimoUsoEn"/> (10 §1).</summary>
    public static readonly TimeSpan IntervaloActualizacionUso = TimeSpan.FromMinutes(1);

    public Guid Id { get; private set; }
    public AmbitoSesion Ambito { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public Guid? ConsumidorId { get; private set; }
    public string HashIdentificador { get; private set; } = null!;
    public string Host { get; private set; } = null!;
    public DateTimeOffset CreadaEn { get; private set; }
    public DateTimeOffset UltimoUsoEn { get; private set; }
    public DateTimeOffset ExpiraEn { get; private set; }
    public DateTimeOffset? RevocadaEn { get; private set; }
    public IPAddress? Ip { get; private set; }
    public string? AgenteUsuario { get; private set; }

    protected Sesion() { }

    /// <summary>Sesión del personal. Solo se guarda el hash del valor de la cookie.</summary>
    public static Sesion IniciarPersonal(string hashIdentificador, Guid usuarioId, string host, IPAddress? ip, string? agenteUsuario, DateTimeOffset ahora) => new()
    {
        Id = Guid.NewGuid(),
        Ambito = AmbitoSesion.Personal,
        UsuarioId = usuarioId,
        HashIdentificador = hashIdentificador,
        Host = host,
        Ip = ip,
        AgenteUsuario = agenteUsuario,
        CreadaEn = ahora,
        UltimoUsoEn = ahora,
        ExpiraEn = ahora + DuracionMaxima,
    };

    /// <summary>Vigente si no se revocó, no pasó la duración máxima y no lleva más de <paramref name="inactividad"/> sin usarse.</summary>
    public bool EstaVigente(DateTimeOffset ahora, TimeSpan inactividad) =>
        RevocadaEn is null && ExpiraEn > ahora && UltimoUsoEn + inactividad > ahora;

    /// <summary>Actualiza el último uso, como máximo una vez por minuto. Devuelve si hubo cambio.</summary>
    public bool RegistrarUso(DateTimeOffset ahora)
    {
        if (ahora - UltimoUsoEn < IntervaloActualizacionUso)
        {
            return false;
        }
        UltimoUsoEn = ahora;
        return true;
    }

    public void Revocar(DateTimeOffset ahora) => RevocadaEn ??= ahora;
}
