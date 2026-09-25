namespace Shapi.Dominio.Suscripciones;

public class SuscripcionApi : Suscripcion
{
    public Guid ConsumidorId { get; set; }
    public Guid ApiId { get; set; }

    public SuscripcionApi() { }
}
