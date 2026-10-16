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
    public bool Activo { get; private set; } = true;
    public int Orden { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected PlanPlataforma() { }

    public static PlanPlataforma Crear(Guid id, string nombre, string descripcion, decimal precio, int vigenciaDias,
        int? maxApis, int? maxMiembros, long cuotaPeticiones, bool dominioPropio, int orden, DateTimeOffset ahora) => new()
        {
            Id = id,
            Nombre = nombre,
            Descripcion = descripcion,
            Precio = precio,
            VigenciaDias = vigenciaDias,
            MaxApis = maxApis,
            MaxMiembros = maxMiembros,
            CuotaPeticiones = cuotaPeticiones,
            DominioPropio = dominioPropio,
            EsPrueba = false,
            Activo = true,
            Orden = orden,
            CreadoEn = ahora,
            ActualizadoEn = ahora,
        };

    public void Editar(string nombre, string descripcion, decimal precio, int vigenciaDias, int? maxApis,
        int? maxMiembros, long cuotaPeticiones, bool dominioPropio, DateTimeOffset ahora)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Precio = precio;
        VigenciaDias = vigenciaDias;
        MaxApis = maxApis;
        MaxMiembros = maxMiembros;
        CuotaPeticiones = cuotaPeticiones;
        DominioPropio = dominioPropio;
        ActualizadoEn = ahora;
    }

    public void Desactivar(DateTimeOffset ahora)
    {
        Activo = false;
        ActualizadoEn = ahora;
    }
}
