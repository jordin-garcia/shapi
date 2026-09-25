using System.Net;

namespace Shapi.Dominio.Identidad;

public class Sesion
{
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
}
