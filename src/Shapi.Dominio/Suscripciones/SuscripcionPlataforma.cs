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
            Id = Guid.CreateVersion7(),
            OrganizacionId = organizacionId,
            PlanId = planPrueba.Id,
            Estado = EstadoSuscripcion.Activa,
            Inicio = inicio,
            Fin = inicio.AddDays(planPrueba.VigenciaDias),
            CreadoEn = ahora,
            ActualizadoEn = ahora,
        };
    }

    public static SuscripcionPlataforma Contratar(Guid organizacionId, PlanPlataforma plan, Guid? medioPagoId, DateTimeOffset ahora)
    {
        if (plan.EsPrueba || !plan.Activo)
        {
            throw new ArgumentException("El plan no se puede contratar.", nameof(plan));
        }

        var inicio = InicioDeCiclo(ahora);
        return new SuscripcionPlataforma
        {
            Id = Guid.CreateVersion7(),
            OrganizacionId = organizacionId,
            PlanId = plan.Id,
            MedioPagoId = medioPagoId,
            Estado = EstadoSuscripcion.Activa,
            Inicio = inicio,
            Fin = inicio.AddDays(plan.VigenciaDias),
            CreadoEn = ahora,
            ActualizadoEn = ahora,
        };
    }

    public void Finalizar(DateTimeOffset ahora)
    {
        Estado = EstadoSuscripcion.Finalizada;
        PlanSiguienteId = null;
        ActualizadoEn = ahora;
    }

    public void ProgramarCambio(Guid planId, DateTimeOffset ahora)
    {
        PlanSiguienteId = planId;
        ActualizadoEn = ahora;
    }

    public void CancelarCambio(DateTimeOffset ahora)
    {
        PlanSiguienteId = null;
        ActualizadoEn = ahora;
    }

    public void CambiarPlan(PlanPlataforma plan, Guid? medioPagoId, DateTimeOffset? inicioNuevoCiclo, DateTimeOffset ahora)
    {
        PlanId = plan.Id;
        MedioPagoId = medioPagoId;
        PlanSiguienteId = null;
        if (inicioNuevoCiclo is not null)
        {
            Inicio = inicioNuevoCiclo.Value;
            Fin = Inicio.AddDays(plan.VigenciaDias);
        }
        Estado = EstadoSuscripcion.Activa;
        GraciaHasta = null;
        ActualizadoEn = ahora;
    }

    public void Reactivar(PlanPlataforma plan, Guid medioPagoId, DateTimeOffset ahora)
    {
        var inicio = InicioDeCiclo(ahora);
        PlanId = plan.Id;
        MedioPagoId = medioPagoId;
        Inicio = inicio;
        Fin = inicio.AddDays(plan.VigenciaDias);
        GraciaHasta = null;
        PlanSiguienteId = null;
        Estado = EstadoSuscripcion.Activa;
        ActualizadoEn = ahora;
    }
}
