namespace Shapi.Dominio.Planes;

public class PlanPlataforma
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public decimal Precio { get; set; }
    public int VigenciaDias { get; set; }
    public int? MaxApis { get; set; }
    public int? MaxMiembros { get; set; }
    public long CuotaPeticiones { get; set; }
    public bool DominioPropio { get; set; }
    public bool EsPrueba { get; set; }
    public bool Activo { get; set; }
    public int Orden { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public PlanPlataforma() { }
}
