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
}
