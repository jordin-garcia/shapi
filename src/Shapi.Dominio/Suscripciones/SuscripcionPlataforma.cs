using Shapi.Dominio.Comun;
using Shapi.Dominio.Planes;

namespace Shapi.Dominio.Suscripciones;

public class SuscripcionPlataforma : Suscripcion, IPerteneceAOrganizacion
{
    public Guid OrganizacionId { get; private set; }

    protected SuscripcionPlataforma() { }

    /// <summary>Suscripción activa al plan Prueba desde el registro del proveedor (CU-01), con el ciclo de 09 §4.</summary>
    public static SuscripcionPlataforma IniciarPrueba(Guid organizacionId, PlanPlataforma planPrueba, DateTimeOffset ahora)
    {
        if (!planPrueba.EsPrueba)
        {
            throw new ArgumentException("El plan no es el plan Prueba.", nameof(planPrueba));
        }

        var inicio = InicioDeCiclo(ahora);
        return new SuscripcionPlataforma
        {
            Id = Guid.NewGuid(),
            OrganizacionId = organizacionId,
            PlanId = planPrueba.Id,
            Estado = EstadoSuscripcion.Activa,
            Inicio = inicio,
            Fin = inicio.AddDays(planPrueba.VigenciaDias),
            CreadoEn = ahora,
            ActualizadoEn = ahora,
        };
    }
}
