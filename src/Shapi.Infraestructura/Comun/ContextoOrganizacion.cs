namespace Shapi.Infraestructura.Comun;

public interface IContextoOrganizacion
{
    Guid? OrganizacionIdActual { get; }
}

public class ContextoOrganizacionNulo : IContextoOrganizacion
{
    public Guid? OrganizacionIdActual => null;
}
