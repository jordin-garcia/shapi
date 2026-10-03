using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shapi.Aplicacion.Comun;
using Shapi.Infraestructura.Comun;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Tests.Persistencia;

/// <summary>Las pruebas de persistencia usan el PostgreSQL compartido (JG-18).</summary>
public sealed class PostgresPersistencia : PostgresDePrueba;

[CollectionDefinition(nameof(PostgresPersistencia))]
public sealed class ColeccionPersistencia : ICollectionFixture<PostgresPersistencia>;

public sealed class ContextoPrueba : IContextoOrganizacion
{
    public Guid? OrganizacionId { get; set; }
}

public sealed class RelojFijo(DateTimeOffset ahora) : IReloj
{
    public DateTimeOffset Ahora { get; set; } = ahora;
}

/// <summary>
/// Cada prueba recibe su propia base de datos, recién migrada, para que las restricciones únicas
/// de una prueba no choquen con los datos de otra. La base nace migrada desde la plantilla de
/// <see cref="PostgresCompartido"/>, así que <c>MigrateAsync</c> solo la crea.
/// </summary>
public abstract class BaseDePrueba(PostgresPersistencia postgres) : IAsyncLifetime
{
    protected string Cadena { get; } = new NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString())
    {
        Database = $"prueba_{Guid.NewGuid():N}",
    }.ConnectionString;

    protected ContextoPrueba Contexto { get; } = new();

    protected RelojFijo Reloj { get; } = new(new DateTimeOffset(2026, 9, 26, 18, 0, 0, TimeSpan.Zero));

    public async Task InitializeAsync()
    {
        await using var db = CrearDb();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await PostgresCompartido.EliminarBaseAsync(Cadena);
    }

    /// <summary>Un DbContext configurado como en la aplicación: con el contexto de organización y el interceptor de fechas.</summary>
    protected ShapiDbContext CrearDb()
    {
        var opciones = new DbContextOptionsBuilder<ShapiDbContext>()
            .UseNpgsql(Cadena)
            .AddInterceptors(new InterceptorFechasAuditoria(Reloj))
            .Options;
        return new ShapiDbContext(opciones, Contexto);
    }

    protected async Task Ejecutar(string sql)
    {
        await using var conexion = new NpgsqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        await comando.ExecuteNonQueryAsync();
    }

    protected async Task<T> Escalar<T>(string sql)
    {
        await using var conexion = new NpgsqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return (T)(await comando.ExecuteScalarAsync())!;
    }

    /// <summary>Ejecuta el SQL y devuelve el error de PostgreSQL que debe producir.</summary>
    protected async Task<PostgresException> Rechazo(string sql) =>
        await Assert.ThrowsAsync<PostgresException>(() => Ejecutar(sql));

    // ---------- Filas mínimas válidas, insertadas con SQL para probar la base de datos sin pasar por EF ----------

    protected Task<Guid> NuevaOrganizacion(string tipo = "proveedor") => Escalar<Guid>($"""
        INSERT INTO organizacion (id, nombre, tipo, estado_admin)
        VALUES (gen_random_uuid(), 'Organización de prueba', '{tipo}', 'activa') RETURNING id
        """);

    protected Task<Guid> NuevoUsuario() => Escalar<Guid>("""
        INSERT INTO usuario (id, nombre, correo, estado)
        VALUES (gen_random_uuid(), 'Ana', gen_random_uuid() || '@ejemplo.com', 'activo') RETURNING id
        """);

    protected Task<Guid> NuevaMembresia(Guid usuarioId, Guid organizacionId, string rol) => Escalar<Guid>($"""
        INSERT INTO membresia (id, usuario_id, organizacion_id, rol)
        VALUES (gen_random_uuid(), '{usuarioId}', '{organizacionId}', '{rol}') RETURNING id
        """);

    protected Task<Guid> NuevoConsumidor(Guid organizacionId) => Escalar<Guid>($"""
        INSERT INTO consumidor (id, organizacion_id, nombre, nombre_empresa, correo, hash_contrasena, estado)
        VALUES (gen_random_uuid(), '{organizacionId}', 'Luis', 'Mercadito', gen_random_uuid() || '@ejemplo.com', 'hash', 'activo')
        RETURNING id
        """);

    protected Task<Guid> NuevaApi(Guid organizacionId) => Escalar<Guid>($"""
        INSERT INTO api (id, organizacion_id, nombre, subdominio, url_origen, estado, secreto_origen_cifrado)
        VALUES (gen_random_uuid(), '{organizacionId}', 'Envíos', 'api' || substr(md5(random()::text), 1, 10),
                'https://origen.ejemplo.com', 'borrador', 'secreto')
        RETURNING id
        """);

    protected Task<Guid> NuevaRuta(Guid apiId, string metodo = "GET", int cacheSegundos = 0) => Escalar<Guid>($"""
        INSERT INTO ruta (id, api_id, metodo, patron, definicion, cache_segundos)
        VALUES (gen_random_uuid(), '{apiId}', '{metodo}', '/r' || substr(md5(random()::text), 1, 8), jsonb_build_object(), {cacheSegundos})
        RETURNING id
        """);

    protected Task<Guid> NuevoPlanPlataforma(bool esPrueba = false) => Escalar<Guid>($"""
        INSERT INTO plan_plataforma (id, nombre, descripcion, precio, vigencia_dias, cuota_peticiones, dominio_propio, es_prueba, activo, orden)
        VALUES (gen_random_uuid(), 'Plan ' || gen_random_uuid(), 'Descripción', 199, 30, 1000, false, {esPrueba}, true, 1)
        RETURNING id
        """);

    protected Task<Guid> NuevoPlanApi(Guid apiId) => Escalar<Guid>($"""
        INSERT INTO plan_api (id, api_id, nombre, descripcion, precio, es_gratuito, vigencia_dias, cuota_llamadas, limite_minuto, activo)
        VALUES (gen_random_uuid(), '{apiId}', 'Plan ' || gen_random_uuid(), 'Descripción', 50, false, 30, 1000, 60, true)
        RETURNING id
        """);

    protected Task<Guid> NuevaSuscripcionPlataforma(Guid organizacionId, Guid planId, string estado = "activa") => Escalar<Guid>($"""
        INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin)
        VALUES (gen_random_uuid(), '{organizacionId}', '{planId}', '{estado}', now(), now() + interval '30 days')
        RETURNING id
        """);

    protected Task<Guid> NuevaSuscripcionApi(Guid consumidorId, Guid apiId, Guid planId, string estado = "activa") => Escalar<Guid>($"""
        INSERT INTO suscripcion_api (id, consumidor_id, api_id, plan_id, estado, inicio, fin)
        VALUES (gen_random_uuid(), '{consumidorId}', '{apiId}', '{planId}', '{estado}', now(), now() + interval '30 days')
        RETURNING id
        """);

    protected Task<Guid> NuevaClave(Guid suscripcionId, string tipo = "produccion", string estado = "activa") => Escalar<Guid>($"""
        INSERT INTO clave (id, suscripcion_id, tipo, prefijo, ultimos4, hash_sha256, estado)
        VALUES (gen_random_uuid(), '{suscripcionId}', '{tipo}', 'shp_prod_', 'a1b2',
                encode(sha256(gen_random_uuid()::text::bytea), 'hex'), '{estado}')
        RETURNING id
        """);

    protected static string ValorONulo(Guid? valor) => valor is null ? "NULL" : $"'{valor}'";

    protected Task<Guid> NuevoMedioPago(Guid? organizacionId, Guid? consumidorId, int mesVencimiento = 12) => Escalar<Guid>($"""
        INSERT INTO medio_pago (id, organizacion_id, consumidor_id, token_pasarela, marca, ultimos4, titular, mes_vencimiento, anio_vencimiento)
        VALUES (gen_random_uuid(), {ValorONulo(organizacionId)}, {ValorONulo(consumidorId)}, 'tok_1', 'Visa', '4242', 'Ana', {mesVencimiento}, 2030)
        RETURNING id
        """);

    protected Task<Guid> NuevoPago(Guid? suscripcionPlataformaId, Guid? suscripcionApiId) => Escalar<Guid>($"""
        INSERT INTO pago (id, suscripcion_plataforma_id, suscripcion_api_id, concepto, descripcion, monto, estado)
        VALUES (gen_random_uuid(), {ValorONulo(suscripcionPlataformaId)}, {ValorONulo(suscripcionApiId)},
                'contratacion', 'Contratación', 199, 'autorizado')
        RETURNING id
        """);

    protected Task<long> NuevoConsumoDiario(Guid apiId, Guid? rutaId, Guid? suscripcionId, int rangos = 10) => Escalar<long>($"""
        INSERT INTO consumo_diario (fecha, api_id, ruta_id, suscripcion_id, entorno,
                                    hist_latencia_total, hist_latencia_compuerta, latencia_total_suma_ms, latencia_compuerta_suma_ms)
        VALUES ('2026-09-26', '{apiId}', {ValorONulo(rutaId)}, {ValorONulo(suscripcionId)}, 'produccion',
                array_fill(0, ARRAY[{rangos}]), array_fill(0, ARRAY[10]), 0, 0)
        RETURNING id
        """);

    protected Task<Guid> NuevoCaso(Guid organizacionId, Guid creadoPor) => Escalar<Guid>($"""
        INSERT INTO caso (id, organizacion_id, creado_por, asunto, estado)
        VALUES (gen_random_uuid(), '{organizacionId}', '{creadoPor}', 'No responde la API', 'abierto')
        RETURNING id
        """);

    protected Task<Guid> NuevoCasoMensaje(Guid casoId, Guid autorId) => Escalar<Guid>($"""
        INSERT INTO caso_mensaje (id, caso_id, autor_id, cuerpo)
        VALUES (gen_random_uuid(), '{casoId}', '{autorId}', 'Desde ayer da 502') RETURNING id
        """);

    protected Task<long> NuevaEntradaBitacora(Guid? organizacionId) => Escalar<long>($"""
        INSERT INTO bitacora (actor_tipo, actor_nombre, organizacion_id, accion, descripcion)
        VALUES ('sistema', 'Sistema', {ValorONulo(organizacionId)}, 'suscripcion.suspendida', 'Suspendió la suscripción')
        RETURNING id
        """);
}
