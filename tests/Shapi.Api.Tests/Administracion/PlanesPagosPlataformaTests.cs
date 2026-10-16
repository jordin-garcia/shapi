using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Pagos;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Tests.Administracion;

public sealed class PublicadorCacheAdministracionFalso : IPublicadorCache
{
    public List<Guid> SuscripcionesPublicadas { get; } = [];
    public Task PublicarApi(Guid apiId, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task EliminarHost(string host, Guid apiId, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task PublicarClave(Guid claveId, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task ExpirarClave(string hashClave, DateTimeOffset instante, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task EliminarClave(string hashClave, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task PublicarSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default)
    {
        SuscripcionesPublicadas.Add(suscripcionId);
        return Task.CompletedTask;
    }
    public Task PublicarOrganizacion(Guid organizacionId, CancellationToken cancelacion = default) => Task.CompletedTask;
}

[Collection(nameof(PostgresPersistencia))]
public sealed class PlanesPagosPlataformaTests(PostgresPersistencia postgres) : BaseDePrueba(postgres), IAsyncLifetime
{
    private const string ContrasenaAdmin = "AdminDePrueba123!";
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;

    async Task IAsyncLifetime.InitializeAsync()
    {
        await base.InitializeAsync();
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            builder.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
            builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Rodrigo Alvarado");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", ContrasenaAdmin);
            builder.UseSetting("Pagos:DemoraMs", "0");
            builder.ConfigureTestServices(servicios =>
            {
                servicios.AddSingleton<IReloj>(Reloj);
                servicios.RemoveAll<IPublicadorCache>();
                servicios.AddSingleton<PublicadorCacheAdministracionFalso>();
                servicios.AddSingleton<IPublicadorCache>(sp => sp.GetRequiredService<PublicadorCacheAdministracionFalso>());
            });
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://shapi.localhost"),
            HandleCookies = false,
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        await base.DisposeAsync();
    }

    // RF-17: solo el administrador administra planes; desactivar conserva los planes asociados a suscripciones.
    [Fact]
    public async Task RF_17_AdministrarPlanes_CreaEditaDesactivaYDeniegaSoporte()
    {
        var admin = await Admin();
        var planConSuscripcion = await NuevoPlanPlataforma();
        var org = await NuevaOrganizacion();
        var suscripcion = await NuevaSuscripcionPlataforma(org, planConSuscripcion);
        var soporte = await CrearSesion(Rol.Soporte);

        using var crear = await Enviar(HttpMethod.Post, "/api/admin/planes-plataforma", admin,
            new
            {
                nombre = "Administración EM14",
                descripcion = "Para equipos grandes",
                precio = 1999m,
                vigenciaDias = 365,
                maxApis = 50,
                maxMiembros = 100,
                cuotaPeticiones = 500000,
                dominioPropio = true,
                esPrueba = false
            });
        crear.StatusCode.Should().Be(HttpStatusCode.Created, await crear.Content.ReadAsStringAsync());
        var creado = await crear.Content.ReadFromJsonAsync<JsonElement>();
        var planNuevo = creado.GetProperty("id").GetGuid();

        using var editar = await Enviar(HttpMethod.Put, $"/api/admin/planes-plataforma/{planNuevo}", admin,
            new
            {
                nombre = "Administración EM14",
                descripcion = "Actualizado",
                precio = 2099m,
                vigenciaDias = 365,
                maxApis = 50,
                maxMiembros = 100,
                cuotaPeticiones = 500000,
                dominioPropio = true,
                esPrueba = false
            });
        editar.StatusCode.Should().Be(HttpStatusCode.OK, await editar.Content.ReadAsStringAsync());

        using var listar = await Enviar(HttpMethod.Get, "/api/admin/planes-plataforma", admin);
        listar.StatusCode.Should().Be(HttpStatusCode.OK);
        (await listar.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
            .Select(x => x.GetProperty("id").GetGuid()).Should().Contain(planNuevo).And.Contain(planConSuscripcion);

        using var desactivarVigente = await Enviar(HttpMethod.Post,
            $"/api/admin/planes-plataforma/{planConSuscripcion}/desactivar", admin);
        desactivarVigente.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Escalar<string>($"SELECT activo::text FROM plan_plataforma WHERE id = '{planConSuscripcion}'")).Should().Be("false");
        (await Escalar<long>($"SELECT count(*) FROM suscripcion_plataforma WHERE id = '{suscripcion}'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE accion = 'plan_plataforma.creado' AND objetivo_id = '{planNuevo}'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE accion = 'plan_plataforma.editado' AND objetivo_id = '{planNuevo}'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE accion = 'plan_plataforma.desactivado' AND objetivo_id = '{planConSuscripcion}'")).Should().Be(1);

        using var denegada = await Enviar(HttpMethod.Get, "/api/admin/planes-plataforma", soporte);
        denegada.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // RF-24 y RF-41: la lista administrativa muestra pagos de plataforma y el reembolso pone en gracia el ciclo vigente.
    [Fact]
    public async Task RF_24_ListarYRevertirPago_DePlataformaRegistraReembolsoYGracia()
    {
        var admin = await Admin();
        var org = await NuevaOrganizacion();
        var plan = await NuevoPlanPlataforma();
        var suscripcion = await Escalar<Guid>($"""
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin)
            VALUES (gen_random_uuid(), '{org}', '{plan}', 'activa', '{Reloj.Ahora.AddDays(-5):O}', '{Reloj.Ahora.AddDays(25):O}')
            RETURNING id
            """);
        var pago = await Escalar<Guid>($"""
            INSERT INTO pago (id, suscripcion_plataforma_id, concepto, descripcion, monto, estado, referencia_pasarela, periodo_inicio, periodo_fin)
            VALUES (gen_random_uuid(), '{suscripcion}', 'contratacion', 'Plan mensual', 199, 'autorizado', 'ch_prueba_123',
                    '{Reloj.Ahora.AddDays(-5):O}', '{Reloj.Ahora.AddDays(25):O}') RETURNING id
            """);
        var pagoRechazado = await Escalar<Guid>($"""
            INSERT INTO pago (id, suscripcion_plataforma_id, concepto, descripcion, monto, estado, motivo_rechazo)
            VALUES (gen_random_uuid(), '{suscripcion}', 'renovacion', 'Renovación del plan mensual', 199, 'rechazado', 'pago_rechazado') RETURNING id
            """);
        var pagoCambioPlan = await Escalar<Guid>($"""
            INSERT INTO pago (id, suscripcion_plataforma_id, concepto, descripcion, monto, estado)
            VALUES (gen_random_uuid(), '{suscripcion}', 'cambio_plan', 'Lanzamiento → Producto · diferencia prorrateada', 173.34, 'rechazado') RETURNING id
            """);
        var pagoReactivacion = await Escalar<Guid>($"""
            INSERT INTO pago (id, suscripcion_plataforma_id, concepto, descripcion, monto, estado)
            VALUES (gen_random_uuid(), '{suscripcion}', 'reactivacion', 'Reactivación del plan Producto', 599, 'rechazado') RETURNING id
            """);
        var pagoRenovacion = await Escalar<Guid>($"""
            INSERT INTO pago (id, suscripcion_plataforma_id, concepto, descripcion, monto, estado)
            VALUES (gen_random_uuid(), '{suscripcion}', 'renovacion', 'Renovación · Producto', 599, 'rechazado') RETURNING id
            """);
        var api = await NuevaApi(org);
        var planApi = await NuevoPlanApi(api);
        var consumidor = await NuevoConsumidor(org);
        var suscripcionApi = await NuevaSuscripcionApi(consumidor, api, planApi);
        await NuevoPago(null, suscripcionApi);

        using var listar = await Enviar(HttpMethod.Get, "/api/admin/pagos", admin);
        listar.StatusCode.Should().Be(HttpStatusCode.OK);
        var elementos = (await listar.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        elementos.Select(x => x.GetProperty("id").GetGuid()).Should().Contain(pago).And.Contain(pagoRechazado)
            .And.Contain(pagoCambioPlan).And.Contain(pagoReactivacion).And.Contain(pagoRenovacion).And.HaveCount(5);
        foreach (var id in new[] { pagoCambioPlan, pagoReactivacion, pagoRenovacion })
        {
            elementos.Single(x => x.GetProperty("id").GetGuid() == id).GetProperty("plan").GetString().Should().Be("Producto");
        }

        using var revertirRechazado = await Enviar(HttpMethod.Post, $"/api/admin/pagos/{pagoRechazado}/revertir", admin);
        revertirRechazado.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        using var revertir = await Enviar(HttpMethod.Post, $"/api/admin/pagos/{pago}/revertir", admin);
        revertir.StatusCode.Should().Be(HttpStatusCode.NoContent, await revertir.Content.ReadAsStringAsync());
        (await Fila($"SELECT estado, (revertido_en = '{Reloj.Ahora:O}'::timestamptz)::text, revertido_por::text FROM pago WHERE id = '{pago}'"))
            .Should().Equal("revertido", "true", (await IdDeAdmin()).ToString());
        (await Fila($"SELECT estado, (gracia_hasta = '{Reloj.Ahora.AddDays(7):O}'::timestamptz)::text FROM suscripcion_plataforma WHERE id = '{suscripcion}'"))
            .Should().Equal("en_gracia", "true");
        _fabrica.Services.GetRequiredService<PublicadorCacheAdministracionFalso>().SuscripcionesPublicadas.Should().Contain(suscripcion);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE accion = 'pago.revertido' AND objetivo_id = '{pago}'")).Should().Be(1);

        var suscripcionFinalizada = await Escalar<Guid>($"""
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin)
            VALUES (gen_random_uuid(), '{org}', '{plan}', 'finalizada', '{Reloj.Ahora.AddDays(-5):O}', '{Reloj.Ahora.AddDays(25):O}')
            RETURNING id
            """);
        var pagoFinalizado = await Escalar<Guid>($"""
            INSERT INTO pago (id, suscripcion_plataforma_id, concepto, descripcion, monto, estado, referencia_pasarela, periodo_inicio, periodo_fin)
            VALUES (gen_random_uuid(), '{suscripcionFinalizada}', 'contratacion', 'Contratación del plan Lanzamiento', 199, 'autorizado', 'ch_finalizado_123',
                    '{Reloj.Ahora.AddDays(-5):O}', '{Reloj.Ahora.AddDays(25):O}') RETURNING id
            """);
        using var revertirFinalizada = await Enviar(HttpMethod.Post, $"/api/admin/pagos/{pagoFinalizado}/revertir", admin);
        revertirFinalizada.StatusCode.Should().Be(HttpStatusCode.NoContent, await revertirFinalizada.Content.ReadAsStringAsync());
        (await Fila($"SELECT estado FROM suscripcion_plataforma WHERE id = '{suscripcionFinalizada}'")).Should().Equal("finalizada");
        (await Fila($"SELECT estado FROM pago WHERE id = '{pagoFinalizado}'")).Should().Equal("revertido");
    }

    // RF-24: soporte no lista ni revierte pagos de plataforma.
    [Fact]
    public async Task RF_24_Pagos_SoporteRecibe403()
    {
        var soporte = await CrearSesion(Rol.Soporte);
        using var listar = await Enviar(HttpMethod.Get, "/api/admin/pagos", soporte);
        using var revertir = await Enviar(HttpMethod.Post, $"/api/admin/pagos/{Guid.NewGuid()}/revertir", soporte);
        listar.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        revertir.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<string> Admin()
    {
        using var respuesta = await Enviar(HttpMethod.Post, "/api/auth/entrar", null,
            new { correo = "admin@shapi.test", contrasena = ContrasenaAdmin });
        respuesta.StatusCode.Should().Be(HttpStatusCode.OK, await respuesta.Content.ReadAsStringAsync());
        return respuesta.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private async Task<Guid> IdDeAdmin() => await Escalar<Guid>("SELECT id FROM usuario WHERE correo = 'admin@shapi.test'");

    private async Task<string> CrearSesion(Rol rol)
    {
        var organizacion = await Escalar<Guid>("SELECT id FROM organizacion WHERE tipo = 'plataforma'");
        var usuario = await NuevoUsuario();
        await NuevaMembresia(usuario, organizacion, rol.ToString().ToLowerInvariant());
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        await using var db = CrearDb();
        db.Add(Sesion.IniciarPersonal(SeguridadTokens.HashearToken(token), usuario, "localhost", null, null, Reloj.Ahora));
        await db.SaveChangesAsync();
        return $"shapi_sesion={token}";
    }

    private async Task<HttpResponseMessage> Enviar(HttpMethod metodo, string ruta, string? cookie, object? cuerpo = null)
    {
        using var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Add("X-Requested-With", "shapi");
        if (cookie is not null)
        {
            peticion.Headers.Add("Cookie", cookie);
        }

        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }

        return await _cliente.SendAsync(peticion);
    }

    private async Task<string[]> Fila(string sql)
    {
        await using var conexion = new Npgsql.NpgsqlConnection(Cadena);
        await conexion.OpenAsync();
        await using var comando = new Npgsql.NpgsqlCommand(sql, conexion);
        await using var lector = await comando.ExecuteReaderAsync();
        Assert.True(await lector.ReadAsync());
        return Enumerable.Range(0, lector.FieldCount).Select(lector.GetString).ToArray();
    }
}
