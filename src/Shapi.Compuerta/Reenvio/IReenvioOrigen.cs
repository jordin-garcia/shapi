namespace Shapi.Compuerta.Reenvio;

/// <summary>El último paso de la tubería (08 §3, paso 8): reenvía al origen la petición que pasó todos los filtros.</summary>
public interface IReenvioOrigen
{
    Task ReenviarAsync(ContextoPeticion contexto);
}
