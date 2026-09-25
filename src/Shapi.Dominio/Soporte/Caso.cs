using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Soporte;

public class Caso : IPerteneceAOrganizacion
{
    public Guid Id { get; set; }
    public int Numero { get; set; }
    public Guid OrganizacionId { get; set; }
    public Guid? ApiId { get; set; }
    public Guid CreadoPor { get; set; }
    public Guid? AsignadoA { get; set; }
    public string Asunto { get; set; } = null!;
    public EstadoCaso Estado { get; set; }
    public DateTimeOffset? CerradoEn { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public Caso() { }
}
