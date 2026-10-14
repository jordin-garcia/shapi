namespace Shapi.Dominio.Apis;

public class RegistroDnsSimulado
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Tipo { get; private set; } = null!;
    public string Valor { get; private set; } = null!;
    public DateTimeOffset CreadoEn { get; private set; }
    public DateTimeOffset ActualizadoEn { get; private set; }

    protected RegistroDnsSimulado() { }

    public RegistroDnsSimulado(string nombre, string valor, DateTimeOffset ahora)
    {
        Id = Guid.CreateVersion7();
        Nombre = nombre;
        Tipo = "CNAME";
        Valor = valor;
        CreadoEn = ahora;
        ActualizadoEn = ahora;
    }

    public void Actualizar(string valor, DateTimeOffset ahora)
    {
        Valor = valor;
        ActualizadoEn = ahora;
    }
}
