namespace Shapi.Dominio.Apis;

public class RegistroDnsSimulado
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Tipo { get; private set; } = null!;
    public string Valor { get; private set; } = null!;

    protected RegistroDnsSimulado() { }
}
