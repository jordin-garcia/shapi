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
    public bool Activo { get; private set; } = true;
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected PlanApi() { }

    public static PlanApi Crear(Guid id, Guid apiId, string nombre, string descripcion, decimal precio, bool esGratuito, int vigenciaDias, long cuotaLlamadas, int limiteMinuto)
    {
        return new PlanApi
        {
            Id = id,
            ApiId = apiId,
            Nombre = nombre,
            Descripcion = descripcion,
            Precio = esGratuito ? 0 : precio,
            EsGratuito = esGratuito,
            VigenciaDias = vigenciaDias,
            CuotaLlamadas = cuotaLlamadas,
            LimiteMinuto = limiteMinuto,
            Activo = true
        };
    }

    public void Editar(string nombre, string descripcion, decimal precio, bool esGratuito, int vigenciaDias, long cuotaLlamadas, int limiteMinuto)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Precio = esGratuito ? 0 : precio;
        EsGratuito = esGratuito;
        VigenciaDias = vigenciaDias;
        CuotaLlamadas = cuotaLlamadas;
        LimiteMinuto = limiteMinuto;
    }

    public void Desactivar()
    {
        Activo = false;
    }
}
