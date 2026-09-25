using Shapi.Aplicacion.Comun;

namespace Shapi.Infraestructura.Comun;

public class ContextoOrganizacionNulo : IContextoOrganizacion
{
    public Guid? OrganizacionId => null;
}
