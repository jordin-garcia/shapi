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
                servicios.AddSingleton(new HttpMessageInvoker(Origen.CrearManejador())));
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
    public HttpClient Cliente(string host) =>
        Fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri($"http://{host}") });

    /// <summary>Escribe en Redis una API con su host, como lo hará el publicador de JG-04.</summary>
    public async Task<ContextoApi> SembrarApiAsync(string host, string estado = ContextoApi.EstadoPublicada)
    {
        var api = new ContextoApi(Guid.NewGuid(), Guid.NewGuid(), estado, UrlOrigen, null, null, 1);
        var db = Redis.GetDatabase();
        await db.StringSetAsync(LlavesRedis.ApiPorHost(host), api.ApiId.ToString());
        await db.HashSetAsync(LlavesRedis.Api(api.ApiId), AEntradas(api.ACampos()));
        return api;
    }

    /// <summary>Escribe en Redis una clave de la API, guardada por el hash que se indique.</summary>
    public async Task<ContextoClave> SembrarClaveAsync(ContextoApi api, string hash, string tipo = ContextoClave.TipoProduccion)
    {
        var clave = new ContextoClave(Guid.NewGuid(), Guid.NewGuid(), api.ApiId, api.OrganizacionId, Guid.NewGuid(), tipo);
        await Redis.GetDatabase().HashSetAsync(LlavesRedis.Clave(hash), AEntradas(clave.ACampos()));
        return clave;
    }

    private static HashEntry[] AEntradas(IReadOnlyDictionary<string, string> campos) =>
        [.. campos.Select(campo => new HashEntry(campo.Key, campo.Value))];
}
