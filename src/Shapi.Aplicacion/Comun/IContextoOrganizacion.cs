namespace Shapi.Aplicacion.Comun;

/// <summary>La organización de la petición actual (10 §2). La usa el filtro global por organización (RNF-08).</summary>
public interface IContextoOrganizacion
{
    /// <summary>Nulo si la petición no pertenece a ninguna organización (por ejemplo, sin sesión).</summary>
    Guid? OrganizacionId { get; }
}
