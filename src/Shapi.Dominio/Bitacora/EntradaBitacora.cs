using System.Net;
using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Bitacora;

public class EntradaBitacora
{
    public long Id { get; set; }
    public DateTimeOffset Fecha { get; set; }
    public ActorTipo ActorTipo { get; set; }
    public Guid? ActorId { get; set; }
    public string ActorNombre { get; set; } = null!;
    public Guid? OrganizacionId { get; set; }
    public string Accion { get; set; } = null!;
    public string? ObjetivoTipo { get; set; }
    public Guid? ObjetivoId { get; set; }
    public string Descripcion { get; set; } = null!;
    public string? Detalle { get; set; } // JSONB
    public IPAddress? Ip { get; set; }


    public EntradaBitacora(ActorTipo actorTipo, Guid? actorId, string actorNombre, Guid? organizacionId, string accion, string? objetivoTipo, Guid? objetivoId, string descripcion, string? detalle, IPAddress? ip)
    {
        Fecha = DateTimeOffset.UtcNow;
        ActorTipo = actorTipo;
        ActorId = actorId;
        ActorNombre = actorNombre;
        OrganizacionId = organizacionId;
        Accion = accion;
        ObjetivoTipo = objetivoTipo;
        ObjetivoId = objetivoId;
        Descripcion = descripcion;
        Detalle = detalle;
        Ip = ip;
    }

    public EntradaBitacora() { }
}
