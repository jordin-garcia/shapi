namespace Shapi.Aplicacion.Pagos;

public class ResultadoTokenizacion
{
    public bool Exitoso { get; set; }
    public string? Token { get; set; }
    public string? Error { get; set; }
    public string? Marca { get; set; }
    public string? Ultimos4 { get; set; }
    public string? Titular { get; set; }
}
