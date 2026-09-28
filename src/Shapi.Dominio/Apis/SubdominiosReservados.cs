using System.Collections.Frozen;

namespace Shapi.Dominio.Apis;

/// <summary>
/// Subdominios que no se pueden usar como <c>{sub}</c> (06 §4). Es la única definición: la usan el registro de una
/// API (RF-08) y los enlaces de los correos de un portal (10 §6).
/// </summary>
public static class SubdominiosReservados
{
    public static FrozenSet<string> Todos { get; } = new[]
    {
        "api", "app", "www", "admin", "panel", "correo", "mail", "soporte",
        "docs", "estado", "status", "shapi", "static", "cdn", "interno",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static bool Contiene(string subdominio) => Todos.Contains(subdominio);
}
