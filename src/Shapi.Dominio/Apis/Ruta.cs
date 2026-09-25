namespace Shapi.Dominio.Apis;

public class Ruta
{
    public Guid Id { get; set; }
    public Guid ApiId { get; set; }
    public MetodoHttp Metodo { get; set; }
    public string Patron { get; set; } = null!;
    public string? Resumen { get; set; }
    public string? Descripcion { get; set; }
    public string Definicion { get; set; } = null!; // JSONB
    public bool Expuesta { get; set; }
    public int? LimiteMinuto { get; set; }
    public int CacheSegundos { get; set; }
    public int PesoLlamadas { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset ActualizadoEn { get; set; }

    public Ruta() { }
}
