namespace Shapi.Compuerta;

public sealed class ContextoPeticion(HttpContext http)
{
    public HttpContext Http { get; } = http;
}
