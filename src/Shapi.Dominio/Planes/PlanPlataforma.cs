namespace Shapi.Dominio.Planes;

public class PlanPlataforma
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Descripcion { get; private set; } = null!;
    public decimal Precio { get; private set; }
    public int VigenciaDias { get; private set; }
    public int? MaxApis { get; private set; }
    public int? MaxMiembros { get; private set; }
    public long CuotaPeticiones { get; private set; }
    public bool DominioPropio { get; private set; }
    public bool EsPrueba { get; private set; }
    public bool Activo { get; private set; }
    public int Orden { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected PlanPlataforma() { }
}
