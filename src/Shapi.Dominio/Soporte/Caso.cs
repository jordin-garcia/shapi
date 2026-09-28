using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Soporte;

public class Caso : IPerteneceAOrganizacion
{
    public Guid Id { get; private set; }
    public int Numero { get; private set; }
    public Guid OrganizacionId { get; private set; }
    public Guid? ApiId { get; private set; }
    public Guid CreadoPor { get; private set; }
    public Guid? AsignadoA { get; private set; }
    public string Asunto { get; private set; } = null!;
    public EstadoCaso Estado { get; private set; }
    public DateTimeOffset? CerradoEn { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Caso() { }
}
