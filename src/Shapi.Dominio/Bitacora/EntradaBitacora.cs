using System.Net;
using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Bitacora;

public class EntradaBitacora : IPerteneceAOrganizacion
{
    public long Id { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public ActorTipo ActorTipo { get; private set; }
    public Guid? ActorId { get; private set; }
    public string ActorNombre { get; private set; } = null!;
    public Guid OrganizacionId { get; private set; }
    public string Accion { get; private set; } = null!;
    public string? ObjetivoTipo { get; private set; }
    public Guid? ObjetivoId { get; private set; }
    public string Descripcion { get; private set; } = null!;
    public string? Detalle { get; private set; } // JSONB
    public IPAddress? Ip { get; private set; }

    protected EntradaBitacora() { }
}
