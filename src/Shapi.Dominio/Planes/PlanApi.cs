namespace Shapi.Dominio.Planes;

public class PlanApi
{
    public Guid Id { get; set; }
    public Guid ApiId { get; set; }
    public string Nombre { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public decimal Precio { get; set; }
    public bool EsGratuito { get; set; }
    public int VigenciaDias { get; set; }
    public long CuotaLlamadas { get; set; }
    public int LimiteMinuto { get; set; }
    public bool Activo { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public PlanApi() { }
}
