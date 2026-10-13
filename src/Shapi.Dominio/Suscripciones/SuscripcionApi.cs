namespace Shapi.Dominio.Suscripciones;

public class SuscripcionApi : Suscripcion
{
    public Guid ConsumidorId { get; private set; }
    public Guid ApiId { get; private set; }

    protected SuscripcionApi() { }

    public static SuscripcionApi Crear(Guid consumidorId, Guid apiId, Guid planId, DateTimeOffset inicio, DateTimeOffset fin, Guid? medioPagoId = null)
    {
        if (fin <= inicio)
        {
            throw new ArgumentOutOfRangeException(nameof(fin));
        }

        return new SuscripcionApi
        {
            Id = Guid.CreateVersion7(),
            ConsumidorId = consumidorId,
            ApiId = apiId,
            PlanId = planId,
            Estado = EstadoSuscripcion.Activa,
            Inicio = inicio,
            Fin = fin,
            MedioPagoId = medioPagoId,
        };
    }

    /// <summary>Reactiva una suscripción API con un ciclo nuevo desde hoy (EM-10).</summary>
    public void Reactivar(int vigenciaDias, Guid medioPagoId, DateTimeOffset ahora)
    {
        if (Estado is not (EstadoSuscripcion.EnGracia or EstadoSuscripcion.Suspendida))
        {
            throw new InvalidOperationException("Solo se puede reactivar una suscripción en gracia o suspendida.");
        }

        if (vigenciaDias <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(vigenciaDias));
        }

        var inicio = InicioDeCiclo(ahora);
        Inicio = inicio;
        Fin = inicio.AddDays(vigenciaDias);
        GraciaHasta = null;
        MedioPagoId = medioPagoId;
        Estado = EstadoSuscripcion.Activa;
        ActualizadoEn = ahora;
    }
}
