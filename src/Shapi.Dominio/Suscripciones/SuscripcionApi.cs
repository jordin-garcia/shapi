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
}
