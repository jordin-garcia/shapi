namespace Shapi.Dominio.Apis;

public class Ruta
{
    public Guid Id { get; private set; }
    public Guid ApiId { get; private set; }
    public MetodoHttp Metodo { get; private set; }
    public string Patron { get; private set; } = null!;
    public string? Resumen { get; private set; }
    public string? Descripcion { get; private set; }
    public string Definicion { get; private set; } = null!; // JSONB
    public bool Expuesta { get; private set; }
    public int? LimiteMinuto { get; private set; }
    public int CacheSegundos { get; private set; }
    public int PesoLlamadas { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected Ruta() { }

    public Ruta(
        Guid apiId,
        MetodoHttp metodo,
        string patron,
        string? resumen,
        string? descripcion,
        string definicion,
        DateTimeOffset ahora)
    {
        if (apiId == Guid.Empty)
        {
            throw new ArgumentException("La API es obligatoria.", nameof(apiId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(patron);
        ArgumentException.ThrowIfNullOrWhiteSpace(definicion);

        Id = Guid.CreateVersion7();
        ApiId = apiId;
        Metodo = metodo;
        Patron = patron;
        Resumen = resumen;
        Descripcion = descripcion;
        Definicion = definicion;
        Expuesta = false;
        CacheSegundos = 0;
        PesoLlamadas = 1;
        CreadoEn = ahora;
        ActualizadoEn = ahora;
    }

    /// <summary>Actualiza lo que proviene de OpenAPI sin tocar la configuración de la ruta (RF-09).</summary>
    public void ActualizarDefinicion(string? resumen, string? descripcion, string definicion, DateTimeOffset ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definicion);
        Resumen = resumen;
        Descripcion = descripcion;
        Definicion = definicion;
        ActualizadoEn = ahora;
    }

    public bool CambiarExposicion(bool expuesta, DateTimeOffset ahora)
    {
        if (Expuesta == expuesta)
        {
            return false;
        }

        Expuesta = expuesta;
        ActualizadoEn = ahora;
        return true;
    }
}
