using Shapi.Contratos.Redis;

namespace Shapi.Compuerta;

/// <summary>Lo que los filtros van resolviendo sobre una petición, en el orden de la tubería.</summary>
public sealed class ContextoPeticion(HttpContext http)
{
    public HttpContext Http { get; } = http;

    /// <summary>La API publicada que corresponde al host. La resuelve <c>FiltroApi</c>.</summary>
    public ContextoApi? Api { get; set; }

    /// <summary>La clave válida para <see cref="Api"/>. La resuelve <c>FiltroClave</c>.</summary>
    public ContextoClave? Clave { get; set; }
}
