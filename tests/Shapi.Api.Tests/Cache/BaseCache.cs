using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using Shapi.Infraestructura.Cache;
using Shapi.Infraestructura.Comun;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Shapi.Api.Tests.Cache;

/// <summary>Un solo contenedor de Redis 7.4 para las pruebas de la caché. Cada prueba empieza con Redis vacío.</summary>
public sealed class RedisCache : IAsyncLifetime
{
    private readonly RedisContainer _contenedor = new RedisBuilder("redis:7.4-alpine").Build();

    public string Cadena => _contenedor.GetConnectionString();

    public IConnectionMultiplexer Conexion { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();
        Conexion = await ConnectionMultiplexer.ConnectAsync($"{Cadena},allowAdmin=true");
    }

    public async Task DisposeAsync()
    {
        Conexion.Dispose();
        await _contenedor.DisposeAsync();
    }

    public Task VaciarAsync() => Conexion.GetServers()[0].FlushDatabaseAsync();
}

[CollectionDefinition(nameof(RedisCache))]
public sealed class ColeccionCache : ICollectionFixture<PostgresPersistencia>, ICollectionFixture<RedisCache>;

/// <summary>Guarda en memoria lo que se escribe en los registros.</summary>
public sealed class RegistrosCapturados : ILoggerProvider
{
    private readonly ConcurrentQueue<(LogLevel Nivel, string Texto)> _entradas = new();

    public IReadOnlyCollection<(LogLevel Nivel, string Texto)> Entradas => _entradas;

    public ILogger CreateLogger(string categoryName) => new Registrador(_entradas);

    public void Dispose()
    {
    }

    private sealed class Registrador(ConcurrentQueue<(LogLevel, string)> entradas) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            entradas.Enqueue((logLevel, $"{formatter(state, exception)} {exception}"));
    }
}

/// <summary>
/// Base de las pruebas del publicador y de la resincronización: una base de datos propia (como <see cref="BaseDePrueba"/>),
/// Redis vacío y los servicios registrados como en la aplicación.
/// </summary>
public abstract class BaseCache(PostgresPersistencia postgres, RedisCache redis) : BaseDePrueba(postgres), IAsyncLifetime
{
    public const string DominioBase = "shapi.localhost";

    private readonly string _directorioLlaves = Path.Combine(Path.GetTempPath(), $"shapi-dpkeys-{Guid.NewGuid():N}");
    private ServiceProvider? _servicios;

    protected RedisCache RedisCache { get; } = redis;

    protected IDatabase Redis => RedisCache.Conexion.GetDatabase();

    protected RegistrosCapturados Registros { get; } = new();

    /// <summary>Los servicios de la aplicación, con Redis en <paramref name="cadenaRedis"/> o en el contenedor de la prueba.</summary>
    protected ServiceProvider CrearServicios(string? cadenaRedis = null, Action<IServiceCollection>? ajustar = null)
    {
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_POSTGRES_CADENA"] = Cadena,
            ["SHAPI_REDIS"] = cadenaRedis ?? RedisCache.Cadena,
            ["SHAPI_DOMINIO_BASE"] = DominioBase,
            ["SHAPI_DPKEYS_DIR"] = _directorioLlaves,
        }).Build();

        var servicios = new ServiceCollection();
        servicios.AddSingleton<IConfiguration>(configuracion);
        servicios.AddLogging(registros => registros.SetMinimumLevel(LogLevel.Trace).AddProvider(Registros));
        servicios.AgregarServiciosComunes(configuracion);
        servicios.AgregarCacheRedis();
        servicios.RemoveAll<IReloj>();
        servicios.AddSingleton<IReloj>(Reloj);
        servicios.AddSingleton(new OpcionesCacheRedis { EsperasReintento = [TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(40)] });
        ajustar?.Invoke(servicios);
        return servicios.BuildServiceProvider();
    }

    /// <summary>Los servicios de la prueba, creados la primera vez que se piden.</summary>
    protected ServiceProvider Servicios => _servicios ??= CrearServicios();

    protected IPublicadorCache Publicador(IServiceScope alcance) => alcance.ServiceProvider.GetRequiredService<IPublicadorCache>();

    protected string Cifrar(string secreto) => Servicios.GetRequiredService<IProtectorSecretoOrigen>().Cifrar(secreto);

    public new async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await RedisCache.VaciarAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_servicios is not null)
        {
            await _servicios.DisposeAsync();
        }

        await base.DisposeAsync();
        if (Directory.Exists(_directorioLlaves))
        {
            Directory.Delete(_directorioLlaves, recursive: true);
        }
    }

    // ---------- Datos: una API publicada de Envíos con dos rutas, un plan y un consumidor ----------

    protected async Task<Guid> NuevaApiPublicada(
        Guid organizacionId, string subdominio = "envios", string secreto = "shps_secretoDeOrigenDePrueba0001",
        string estado = "publicada", string urlOrigen = "https://origen.ejemplo.com") => await Escalar<Guid>($"""
        INSERT INTO api (id, organizacion_id, nombre, subdominio, url_origen, estado, secreto_origen_cifrado)
        VALUES (gen_random_uuid(), '{organizacionId}', 'Envíos', '{subdominio}', '{urlOrigen}', '{estado}', '{Cifrar(secreto)}')
        RETURNING id
        """);

    protected Task<Guid> NuevaRutaCompleta(
        Guid apiId, string metodo, string patron, bool expuesta, int? limiteMinuto = null, int cacheSegundos = 0,
        int peso = 1) => Escalar<Guid>($"""
        INSERT INTO ruta (id, api_id, metodo, patron, definicion, expuesta, limite_minuto, cache_segundos, peso_llamadas)
        VALUES (gen_random_uuid(), '{apiId}', '{metodo}', '{patron}', jsonb_build_object(), {expuesta},
                {(limiteMinuto is null ? "NULL" : limiteMinuto.ToString())}, {cacheSegundos}, {peso})
        RETURNING id
        """);

    protected Task NuevoDominioPropio(Guid apiId, string dominio, string estado) => Ejecutar($"""
        INSERT INTO dominio_propio (id, api_id, dominio, destino_cname, estado)
        VALUES (gen_random_uuid(), '{apiId}', '{dominio}', 'envios.api.{DominioBase}', '{estado}')
        """);

    protected Task<Guid> NuevoPlanApiCon(Guid apiId, string nombre, long cuota, int limiteMinuto) => Escalar<Guid>($"""
        INSERT INTO plan_api (id, api_id, nombre, descripcion, precio, es_gratuito, vigencia_dias, cuota_llamadas, limite_minuto, activo)
        VALUES (gen_random_uuid(), '{apiId}', '{nombre}', 'Descripción', 450, false, 30, {cuota}, {limiteMinuto}, true)
        RETURNING id
        """);

    protected Task<Guid> NuevaSuscripcionApiEn(
        Guid consumidorId, Guid apiId, Guid planId, string estado, DateTimeOffset inicio, DateTimeOffset fin) => Escalar<Guid>($"""
        INSERT INTO suscripcion_api (id, consumidor_id, api_id, plan_id, estado, inicio, fin)
        VALUES (gen_random_uuid(), '{consumidorId}', '{apiId}', '{planId}', '{estado}', '{inicio:O}', '{fin:O}')
        RETURNING id
        """);

    protected Task<Guid> NuevaSuscripcionPlataformaEn(
        Guid organizacionId, Guid planId, string estado, DateTimeOffset inicio, DateTimeOffset fin) => Escalar<Guid>($"""
        INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin)
        VALUES (gen_random_uuid(), '{organizacionId}', '{planId}', '{estado}', '{inicio:O}', '{fin:O}')
        RETURNING id
        """);

    protected Task<Guid> NuevoPlanPlataformaCon(long cuotaPeticiones) => Escalar<Guid>($"""
        INSERT INTO plan_plataforma (id, nombre, descripcion, precio, vigencia_dias, cuota_peticiones, dominio_propio, es_prueba, activo, orden)
        VALUES (gen_random_uuid(), 'Plan ' || gen_random_uuid(), 'Descripción', 199, 30, {cuotaPeticiones}, false, false, true, 1)
        RETURNING id
        """);

    /// <summary>Una clave cuyo hash es el SHA-256 de <paramref name="claveEnClaro"/>.</summary>
    protected Task<Guid> NuevaClaveCon(
        Guid suscripcionId, string claveEnClaro, string tipo = "produccion", string estado = "activa",
        DateTimeOffset? expiraEn = null) => Escalar<Guid>($"""
        INSERT INTO clave (id, suscripcion_id, tipo, prefijo, ultimos4, hash_sha256, estado, expira_en)
        VALUES (gen_random_uuid(), '{suscripcionId}', '{tipo}', 'shp_prod_', right('{claveEnClaro}', 4),
                '{ContextoClave.CalcularHash(claveEnClaro)}', '{estado}', {(expiraEn is null ? "NULL" : $"'{expiraEn:O}'")})
        RETURNING id
        """);

    protected Task CambiarEstado(string tabla, Guid id, string estado) =>
        Ejecutar($"UPDATE {tabla} SET estado = '{estado}' WHERE id = '{id}'");

    protected async Task<Dictionary<string, string>> Hash(string llave) =>
        (await Redis.HashGetAllAsync(llave)).ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
}
