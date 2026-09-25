using Shapi.Dominio.Comun;

namespace Shapi.Dominio.Suscripciones;

public class SuscripcionPlataforma : Suscripcion, IPerteneceAOrganizacion
{
    public Guid OrganizacionId { get; set; }

    public SuscripcionPlataforma() { }
}
