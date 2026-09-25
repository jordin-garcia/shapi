namespace Shapi.Dominio.Planes;

public class PlanApi
{
    public Guid Id { get; private set; }
    public Guid ApiId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Descripcion { get; private set; } = null!;
    public decimal Precio { get; private set; }
    public bool EsGratuito { get; private set; }
    public int VigenciaDias { get; private set; }
    public long CuotaLlamadas { get; private set; }
    public int LimiteMinuto { get; private set; }
    public bool Activo { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected PlanApi() { }
}
