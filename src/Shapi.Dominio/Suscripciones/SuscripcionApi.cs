namespace Shapi.Dominio.Suscripciones;

public class SuscripcionApi : Suscripcion
{
    public Guid ConsumidorId { get; private set; }
    public Guid ApiId { get; private set; }

    protected SuscripcionApi() { }
}
