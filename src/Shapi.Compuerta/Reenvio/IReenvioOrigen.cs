namespace Shapi.Compuerta.Reenvio;

public interface IReenvioOrigen
{
    Task ReenviarAsync(ContextoPeticion contexto);
}
