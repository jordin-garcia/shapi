namespace Shapi.Compuerta.Contexto;

/// <summary>Lee de Redis los datos de una petición y los deja en el <see cref="ContextoPeticion"/> (08 §8).</summary>
public interface ILectorContexto
{
    Task LeerAsync(ContextoPeticion contexto);
}
