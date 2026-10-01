using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shapi.Contratos.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Shapi.Compuerta.Tests.Soporte;

/// <summary>
/// Compuerta en memoria con un Redis real (Testcontainers) y un origen falso en memoria.
/// El origen recibe las peticiones a través de un <see cref="HttpMessageInvoker"/> que reemplaza al de producción.
/// </summary>
public sealed class EntornoCompuerta : IAsyncLifetime
{
    public const string UrlOrigen = "http://origen.prueba";

    /// <summary>La IP del consumidor en las peticiones de prueba (TestServer no tiene conexión real).</summary>
    public static readonly IPAddress IpCliente = IPAddress.Parse("203.0.113.7");

    /// <summary>Los caminos que usan las pruebas, expuestos con todos los métodos, para que pasen el filtro 5.</summary>
    public static readonly IReadOnlyList<RutaCache> RutasPorDefecto =
    [
        .. new[] { "/cotizaciones", "/rastreo", "/rastreo/{guia}", "/descarga" }
            .SelectMany(patron => new[] { "GET", "POST", "PUT", "PATCH", "DELETE" }.Select(metodo => Ruta(metodo, patron))),
    ];

    private readonly RedisContainer _contenedor = new RedisBuilder("redis:7.4-alpine").Build();

    public OrigenFalso Origen { get; } = new();

    public RegistrosCapturados Registros { get; } = new();

    public WebApplicationFactory<Program> Fabrica { get; private set; } = null!;

    public IConnectionMultiplexer Redis { get; private set; } = null!;

    public string CadenaRedis => _contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();
        await Origen.IniciarAsync();
        Redis = await ConnectionMultiplexer.ConnectAsync(CadenaRedis);
        Fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", CadenaRedis);
            web.ConfigureLogging(registros => registros.SetMinimumLevel(LogLevel.Trace).AddProvider(Registros));
            web.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton(new HttpMessageInvoker(Origen.CrearManejador()));
                servicios.AddSingleton<IStartupFilter, FiltroIpCliente>();
            });
        });
    }

    public async Task DisposeAsync()
    {
        await Fabrica.DisposeAsync();
        Redis.Dispose();
        await Origen.DisposeAsync();
        await _contenedor.DisposeAsync();
    }

    /// <summary>Un cliente que llama a la compuerta con el host de la API.</summary>
    public HttpClient Cliente(string host) => Cliente(Fabrica, host);

    /// <summary>Sin seguir redirecciones: las pruebas ven la respuesta de la compuerta tal cual.</summary>
    public static HttpClient Cliente(WebApplicationFactory<Program> fabrica, string host) =>
        fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"http://{host}"),
            AllowAutoRedirect = false,
        });

    public static RutaCache Ruta(string metodo, string patron, bool expuesta = true) =>
        new(Guid.NewGuid(), metodo, patron, expuesta, null, 0, 1);

    /// <summary>
    /// Escribe en Redis una API con su host, sus rutas y su organización activa, como lo hace el publicador de la API
    /// de control (JG-04).
    /// </summary>
    public async Task<ContextoApi> SembrarApiAsync(
        string host, string estado = ContextoApi.EstadoPublicada, string urlOrigen = UrlOrigen, string? secreto = null,
        string? portalHost = null, IReadOnlyList<RutaCache>? rutas = null)
    {
        var api = new ContextoApi(Guid.NewGuid(), Guid.NewGuid(), estado, urlOrigen, secreto, portalHost, 1);
        var db = Redis.GetDatabase();
        await db.StringSetAsync(LlavesRedis.ApiPorHost(host), api.ApiId.ToString());
        await db.HashSetAsync(LlavesRedis.Api(api.ApiId), AEntradas(api.ACampos()));
        await SembrarRutasAsync(api, rutas ?? RutasPorDefecto);
        await SembrarOrganizacionAsync(api.OrganizacionId, ContextoOrganizacion.EstadoActiva);
        return api;
    }

    public Task SembrarRutasAsync(ContextoApi api, IEnumerable<RutaCache> rutas) =>
        Redis.GetDatabase().StringSetAsync(LlavesRedis.RutasApi(api.ApiId), RutaCache.Serializar(rutas));

    /// <summary>Sube la <c>version</c> de <c>api:{id}</c>, como cada publicación (07 §4).</summary>
    public Task CambiarVersionAsync(ContextoApi api, long version) =>
        Redis.GetDatabase().HashSetAsync(LlavesRedis.Api(api.ApiId), ContextoApi.CampoVersion,
            version.ToString(CultureInfo.InvariantCulture));

    public Task SembrarOrganizacionAsync(Guid organizacionId, string estadoEfectivo)
    {
        var organizacion = new ContextoOrganizacion(organizacionId, estadoEfectivo, 100_000, 1_790_000_000, 1_792_600_000);
        return ReemplazarHashAsync(LlavesRedis.Organizacion(organizacionId), organizacion.ACampos());
    }

    /// <summary>
    /// Escribe en Redis una clave de la API, guardada por el hash que se indique, con su suscripción. Con
    /// <paramref name="estadoSuscripcion"/> en <c>null</c> no se escribe <c>susc:{id}</c>, como una suscripción
    /// finalizada (07 §4).
    /// </summary>
    public async Task<ContextoClave> SembrarClaveAsync(ContextoApi api, string hash, string tipo = ContextoClave.TipoProduccion,
        string? estadoSuscripcion = ContextoSuscripcion.EstadoActiva)
    {
        var clave = new ContextoClave(Guid.NewGuid(), Guid.NewGuid(), api.ApiId, api.OrganizacionId, Guid.NewGuid(), tipo);
        await Redis.GetDatabase().HashSetAsync(LlavesRedis.Clave(hash), AEntradas(clave.ACampos()));
        if (estadoSuscripcion is not null)
        {
            await SembrarSuscripcionAsync(clave.SuscripcionId, estadoSuscripcion);
        }

        return clave;
    }

    public Task SembrarSuscripcionAsync(Guid suscripcionId, string estado, long inicio = 1_790_000_000, long fin = 1_792_600_000)
    {
        var suscripcion = new ContextoSuscripcion(suscripcionId, Guid.NewGuid(), "Comercio", estado, inicio, fin, 50_000, 60);
        return ReemplazarHashAsync(LlavesRedis.Suscripcion(suscripcionId), suscripcion.ACampos());
    }

    private async Task ReemplazarHashAsync(string llave, IReadOnlyDictionary<string, string> campos)
    {
        var db = Redis.GetDatabase();
        await db.KeyDeleteAsync(llave);
        await db.HashSetAsync(llave, AEntradas(campos));
    }

    private static HashEntry[] AEntradas(IReadOnlyDictionary<string, string> campos) =>
        [.. campos.Select(campo => new HashEntry(campo.Key, campo.Value))];

    /// <summary>Pone <see cref="IpCliente"/> como la IP de la conexión, que TestServer deja vacía.</summary>
    private sealed class FiltroIpCliente : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> siguiente) => app =>
        {
            app.Use((http, continuar) =>
            {
                http.Connection.RemoteIpAddress = IpCliente;
                return continuar(http);
            });
            siguiente(app);
        };
    }
}
