using System.Net;

namespace Shapi.Dominio.Identidad;

public class Sesion
{
    public Guid Id { get; set; }
    public AmbitoSesion Ambito { get; set; }
    public Guid? UsuarioId { get; set; }
    public Guid? ConsumidorId { get; set; }
    public string HashIdentificador { get; set; } = null!;
    public string Host { get; set; } = null!;
    public DateTimeOffset CreadaEn { get; set; }
    public DateTimeOffset UltimoUsoEn { get; set; }
    public DateTimeOffset ExpiraEn { get; set; }
    public DateTimeOffset? RevocadaEn { get; set; }
    public IPAddress? Ip { get; set; }
    public string? AgenteUsuario { get; set; }

    public Sesion() { }
}
