using Shapi.Compuerta.Rutas;
using Shapi.Contratos.Redis;

namespace Shapi.Compuerta;

/// <summary>
/// Lo que se sabe de una petición. <c>ILectorContexto</c> lo lee de Redis antes de los filtros (08 §8), tal como
/// está; cada filtro comprueba lo suyo y lo rechaza si no se cumple.
/// </summary>
public sealed class ContextoPeticion(HttpContext http)
{
    public HttpContext Http { get; } = http;

    /// <summary>La API del host (<c>api:{id}</c>). <c>FiltroApi</c> exige que esté publicada.</summary>
    public ContextoApi? Api { get; set; }

    /// <summary>Las rutas de <see cref="Api"/> (<c>api:{id}:rutas</c>), expuestas y ocultas.</summary>
    public TablaRutas Rutas { get; set; } = TablaRutas.Vacia;

    /// <summary>La clave de <c>X-Api-Key</c> (<c>clave:{hash}</c>). <c>FiltroClave</c> exige que sea de <see cref="Api"/>.</summary>
    public ContextoClave? Clave { get; set; }

    /// <summary>La organización proveedora (<c>org:{id}</c>). <c>FiltroOrganizacion</c> exige que esté activa.</summary>
    public ContextoOrganizacion? Organizacion { get; set; }

    /// <summary>
    /// La suscripción de la clave (<c>susc:{id}</c>); <c>null</c> si no está en Redis, como una finalizada.
    /// <c>FiltroSuscripcion</c> exige que esté activa o en gracia.
    /// </summary>
    public ContextoSuscripcion? Suscripcion { get; set; }

    /// <summary>La ruta expuesta que coincide con el método y el camino. La resuelve <c>FiltroRuta</c>.</summary>
    public RutaCache? Ruta { get; set; }

    /// <summary>
    /// Devuelve la cuota que reservó <c>FiltroLimitesYCuotas</c>. El reenvío la llama si no se pudo conectar con el
    /// origen (502), porque la petición no llegó (08 §3). Es <c>null</c> si no se reservó cuota, como con la clave
    /// de pruebas.
    /// </summary>
    public Func<Task>? DevolverReserva { get; set; }
}
