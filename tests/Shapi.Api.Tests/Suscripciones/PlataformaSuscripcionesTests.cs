using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Pagos;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Tests.Suscripciones;

public sealed class PublicadorSuscripcionFalso : IPublicadorCache
{
    public List<Guid> Publicadas { get; } = [];
    public Task PublicarApi(Guid apiId, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task EliminarHost(string host, Guid apiId, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task PublicarClave(Guid claveId, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task ExpirarClave(string hashClave, DateTimeOffset instante, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task EliminarClave(string hashClave, CancellationToken cancelacion = default) => Task.CompletedTask;
    public Task PublicarSuscripcion(Guid suscripcionId, CancellationToken cancelacion = default) { Publicadas.Add(suscripcionId); return Task.CompletedTask; }
    public Task PublicarOrganizacion(Guid organizacionId, CancellationToken cancelacion = default) => Task.CompletedTask;
}

public sealed class AuthPlataformaPrueba(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Esquema = "PlataformaPrueba";
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Prueba-Organizacion", out var org))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, Request.Headers["X-Prueba-Usuario"].FirstOrDefault() ?? Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, "Ana Lucía Morales"),
            new(ClaimTypes.Role, Request.Headers["X-Prueba-Rol"].FirstOrDefault() ?? "Propietario"),
            new("OrganizacionId", org.ToString()),
            new("Ambito", "Personal"),
        ];
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Esquema)), Esquema)));
    }
}

[Collection(nameof(PostgresPersistencia))]
public sealed class PlataformaSuscripcionesTests(PostgresPersistencia postgres) : BaseDePrueba(postgres), IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private int _demoraPagosMs;
    private PublicadorSuscripcionFalso Publicador => _factory!.Services.GetRequiredService<PublicadorSuscripcionFalso>();
    private WebApplicationFactory<Program> Factory => _factory ??= new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
        builder.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
        builder.UseSetting("Pagos:DemoraMs", _demoraPagosMs.ToString());
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IReloj>(Reloj);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthPlataformaPrueba.Esquema;
                options.DefaultChallengeScheme = AuthPlataformaPrueba.Esquema;
                options.DefaultForbidScheme = AuthPlataformaPrueba.Esquema;
            }).AddScheme<AuthenticationSchemeOptions, AuthPlataformaPrueba>(AuthPlataformaPrueba.Esquema, _ => { });
            services.RemoveAll<IPublicadorCache>();
            services.AddSingleton<PublicadorSuscripcionFalso>();
            services.AddSingleton<IPublicadorCache>(sp => sp.GetRequiredService<PublicadorSuscripcionFalso>());
        });
    });

    public new async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    // RF-19: la lista pública excluye planes inactivos y sigue el orden de plataforma.
    [Fact]
    public async Task RF_19_ListarPlanesPlataforma_DevuelveActivosOrdenados()
    {
        var primero = await NuevoPlanPlataforma();
        var segundo = await NuevoPlanPlataforma();
        var inactivo = await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET orden = 500 WHERE id = '{primero}'");
        await Ejecutar($"UPDATE plan_plataforma SET orden = 501 WHERE id = '{segundo}'");
        await Ejecutar($"UPDATE plan_plataforma SET orden = 499, activo = false WHERE id = '{inactivo}'");
        using var client = Factory.CreateClient();
        using var response = await client.GetAsync("/api/planes-plataforma");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var plans = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ids = plans.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray();
        ids.Should().Contain(primero).And.Contain(segundo).And.NotContain(inactivo);
        Array.IndexOf(ids, primero).Should().BeLessThan(Array.IndexOf(ids, segundo));
    }

    // RF-20: contratación de plan de pago finaliza Prueba, guarda pago y publica el estado actualizado.
    [Fact]
    public async Task RF_20_Contratar_DesdePruebaGuardaPagoYPublicaSuscripcion()
    {
        var (org, user, prueba, pago) = await Escenario();
        using var client = Factory.CreateClient();
        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/contratar", org, user,
            new { planId = pago, tarjeta = Tarjeta("4242424242424242") });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var nueva = body.GetProperty("id").GetGuid();
        body.GetProperty("plan").GetProperty("nombre").GetString().Should().Be("Lanzamiento");
        body.GetProperty("plan").GetProperty("precio").GetDecimal().Should().Be(199m);
        body.GetProperty("plan").GetProperty("moneda").GetString().Should().Be("GTQ");
        var inicioCiclo = Shapi.Dominio.Suscripciones.Suscripcion.InicioDeCiclo(Reloj.Ahora);
        body.GetProperty("periodo").GetProperty("inicio").GetDateTimeOffset().Should().Be(inicioCiclo);
        body.GetProperty("proximaRenovacion").GetDateTimeOffset().Should().Be(inicioCiclo.AddDays(30));
        body.GetProperty("tarjetaEnmascarada").GetString().Should().EndWith("4242");
        body.GetProperty("tarjetaEnmascarada").GetString().Should().NotContain("4242424242424242");
        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{prueba}'")).Should().Be("finalizada");
        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{nueva}'")).Should().Be("activa");
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{nueva}' AND estado = 'autorizado' AND monto = 199")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE objetivo_tipo = 'suscripcion_plataforma' AND objetivo_id = '{nueva}' AND accion = 'suscripcion_plataforma.contratada'")).Should().Be(1);
        Publicador.Publicadas.Should().Contain(nueva);
    }

    // RF-20: un plan gratuito vigente puede pasar a uno de pago y su ciclo se cierra al contratar.
    [Fact]
    public async Task RF_20_Contratar_DesdePlanGratuitoGuardaPagoYActivaNuevoCiclo()
    {
        var (org, user, gratuito, pago) = await Escenario();
        await Ejecutar($"UPDATE plan_plataforma SET nombre = 'Gratis', precio = 0, es_prueba = false WHERE id = '{gratuito}'");
        using var client = Factory.CreateClient();
        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/contratar", org, user,
            new { planId = pago, tarjeta = Tarjeta("4242424242424242") });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var nueva = body.GetProperty("id").GetGuid();
        var inicioCiclo = Shapi.Dominio.Suscripciones.Suscripcion.InicioDeCiclo(Reloj.Ahora);
        body.GetProperty("periodo").GetProperty("inicio").GetDateTimeOffset().Should().Be(inicioCiclo);
        body.GetProperty("proximaRenovacion").GetDateTimeOffset().Should().Be(inicioCiclo.AddDays(30));
        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{gratuito}'")).Should().Be("finalizada");
        (await Escalar<long>($"SELECT count(*) FROM suscripcion_plataforma WHERE id = '{nueva}' AND estado = 'activa' AND plan_id = '{pago}'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{nueva}' AND estado = 'autorizado' AND monto = 199")).Should().Be(1);
    }

    // RF-20: un rechazo queda registrado y deja la suscripción de Prueba vigente sin guardar tarjeta.
    [Fact]
    public async Task RF_20_Contratar_TarjetaRechazadaDejaSuscripcionIgual()
    {
        var (org, user, prueba, pago) = await Escenario();
        using var client = Factory.CreateClient();
        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/contratar", org, user,
            new { planId = pago, tarjeta = Tarjeta("4000000000000002") });
        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{prueba}'")).Should().Be("activa");
        (await Escalar<long>("SELECT count(*) FROM suscripcion_plataforma WHERE estado <> 'finalizada'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{prueba}' AND estado = 'rechazado' AND monto = 199 AND motivo_rechazo = 'fondos_insuficientes'")).Should().Be(1);
        (await Escalar<long>("SELECT count(*) FROM medio_pago")).Should().Be(0);
    }

    // RF-19: el propietario consulta el periodo mostrado y los datos de renovación de su organización.
    [Fact]
    public async Task RF_19_ConsultarSuscripcion_DevuelvePlanYPeriodoMostrado()
    {
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var plan = await NuevoPlanPlataforma();
        var sub = await NuevaSuscripcionPlataforma(org, plan);
        using var client = Factory.CreateClient();
        using var response = await Enviar(client, HttpMethod.Get, "/api/suscripcion", org, user, null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("plan").GetProperty("id").GetGuid().Should().Be(plan);
        body.GetProperty("estado").GetString().Should().Be("activa");
        body.GetProperty("periodo").GetProperty("inicio").GetDateTimeOffset().Should().BeAfter(DateTimeOffset.MinValue);
        body.GetProperty("proximaRenovacion").GetDateTimeOffset().Should().BeAfter(DateTimeOffset.MinValue);
        (await Escalar<long>($"SELECT count(*) FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(1);
    }

    // RNF-08: una organización no puede consultar la suscripción de otra.
    [Fact]
    public async Task RNF_08_ConsultarSuscripcion_OtraOrganizacionRecibe404()
    {
        var organizacionDueña = await NuevaOrganizacion();
        var propietarioDueña = await NuevoUsuario();
        await NuevaMembresia(propietarioDueña, organizacionDueña, "propietario");
        var plan = await NuevoPlanPlataforma();
        await NuevaSuscripcionPlataforma(organizacionDueña, plan);
        var otraOrganizacion = await NuevaOrganizacion();
        var otroPropietario = await NuevoUsuario();
        await NuevaMembresia(otroPropietario, otraOrganizacion, "propietario");

        using var client = Factory.CreateClient();
        using var response = await Enviar(client, HttpMethod.Get, "/api/suscripcion", otraOrganizacion, otroPropietario, null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // 04 §3.1: el lector puede consultar la suscripción, pero no contratar.
    [Fact]
    public async Task RF_20_Contratar_RolLectorRecibe403()
    {
        var org = await NuevaOrganizacion();
        var lector = await NuevoUsuario();
        await NuevaMembresia(lector, org, "lector");
        var plan = await NuevoPlanPlataforma();
        using var client = Factory.CreateClient();
        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/contratar", org, lector,
            new { planId = plan, tarjeta = Tarjeta("4242424242424242") }, "Lector");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // RF-25: la bajada se programa y su cancelación borra el plan siguiente.
    [Fact]
    public async Task RF_25_CambiarYCancelarBajada_ProgramaYQuitaCambio()
    {
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var actual = await NuevoPlanPlataforma();
        var siguiente = await NuevoPlanPlataforma();
        var sub = await NuevaSuscripcionPlataforma(org, actual);
        using var client = Factory.CreateClient();
        using var cambio = await Enviar(client, HttpMethod.Post, "/api/suscripcion/cambiar", org, user, new { planId = siguiente });
        cambio.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Escalar<Guid>($"SELECT plan_siguiente_id FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(siguiente);
        using var cancelacion = await Enviar(client, HttpMethod.Delete, "/api/suscripcion/cambio-programado", org, user, null);
        cancelacion.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Escalar<long>($"SELECT count(*) FROM suscripcion_plataforma WHERE id = '{sub}' AND plan_siguiente_id IS NULL")).Should().Be(1);
    }

    // RF-25: una bajada que supera los límites actuales se rechaza sin programarla.
    [Fact]
    public async Task RF_25_CambiarBajada_ExcedeLimitesDevuelve422()
    {
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var actual = await NuevoPlanPlataforma();
        var destino = await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET precio = 99, max_apis = 1 WHERE id = '{destino}'");
        var sub = await NuevaSuscripcionPlataforma(org, actual);
        await NuevaApi(org);
        await NuevaApi(org);
        using var client = Factory.CreateClient();

        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/cambiar", org, user, new { planId = destino });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("codigo").GetString().Should().Be("excede_limites_del_plan");
        (await Escalar<long>($"SELECT count(*) FROM suscripcion_plataforma WHERE id = '{sub}' AND plan_siguiente_id IS NULL")).Should().Be(1);
    }

    // RF-43: las invitaciones pendientes y vigentes también ocupan un espacio del límite de miembros.
    [Fact]
    public async Task RF_43_CambiarBajada_InvitacionesVigentesExcedenLimiteDeMiembros()
    {
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var actual = await NuevoPlanPlataforma();
        var destino = await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET precio = 99, max_miembros = 1 WHERE id = '{destino}'");
        var sub = await NuevaSuscripcionPlataforma(org, actual);
        await Ejecutar($"""
            INSERT INTO token (id, tipo, hash_token, organizacion_id, correo, rol, expira_en)
            VALUES (gen_random_uuid(), 'invitacion_miembro', repeat('a', 64), '{org}', 'invitado@example.com', 'editor', '{Reloj.Ahora.AddDays(7):O}')
            """);
        using var client = Factory.CreateClient();

        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/cambiar", org, user, new { planId = destino });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("codigo").GetString().Should().Be("excede_limites_del_plan");
        (await Escalar<long>($"SELECT count(*) FROM suscripcion_plataforma WHERE id = '{sub}' AND plan_siguiente_id IS NULL")).Should().Be(1);
    }

    // RF-25: una subida prorratea el crédito, cobra la diferencia y publica la cuota nueva.
    [Fact]
    public async Task RF_25_Cambiar_SubidaCobraDiferenciaYPublicaSuscripcion()
    {
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var origen = await NuevoPlanPlataforma();
        var destino = await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET precio = 599 WHERE id = '{destino}'");
        var medio = await NuevoMedioPago(org, null);
        var sub = await Escalar<Guid>($"""
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin, medio_pago_id)
            VALUES (gen_random_uuid(), '{org}', '{origen}', 'activa', '{Reloj.Ahora.AddDays(-17):O}', '{Reloj.Ahora.AddDays(13):O}', '{medio}') RETURNING id
            """);
        using var client = Factory.CreateClient();

        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/cambiar", org, user, new { planId = destino });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("credito").GetDecimal().Should().Be(86.23m);
        body.GetProperty("cargo").GetDecimal().Should().Be(259.57m);
        body.GetProperty("aPagar").GetDecimal().Should().Be(173.34m);
        (await Escalar<Guid>($"SELECT plan_id FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(destino);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{sub}' AND concepto = 'cambio_plan' AND monto = 173.34 AND estado = 'autorizado'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE objetivo_tipo = 'suscripcion_plataforma' AND objetivo_id = '{sub}' AND accion = 'suscripcion_plataforma.cambiada'")).Should().Be(1);
        Publicador.Publicadas.Should().Contain(sub);
    }

    // RF-25: si se rechaza el cobro de una subida, se conserva el plan y periodo vigentes.
    [Fact]
    public async Task RF_25_Cambiar_SubidaRechazadaConservaPlanYPeriodo()
    {
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var origen = await NuevoPlanPlataforma();
        var destino = await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET precio = 599 WHERE id = '{destino}'");
        var medio = await NuevoMedioPago(org, null);
        var inicio = Reloj.Ahora.AddDays(-17);
        var fin = Reloj.Ahora.AddDays(13);
        var sub = await Escalar<Guid>($"""
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin, medio_pago_id)
            VALUES (gen_random_uuid(), '{org}', '{origen}', 'activa', '{inicio:O}', '{fin:O}', '{medio}') RETURNING id
            """);
        using var client = Factory.CreateClient();

        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/cambiar", org, user,
            new { planId = destino, tarjeta = Tarjeta("4000000000000002") });

        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        (await Escalar<Guid>($"SELECT plan_id FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(origen);
        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be("activa");
        (await Escalar<DateTime>($"SELECT inicio FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(inicio.UtcDateTime);
        (await Escalar<DateTime>($"SELECT fin FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(fin.UtcDateTime);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{sub}' AND concepto = 'cambio_plan' AND estado = 'rechazado' AND monto = 173.34")).Should().Be(1);
        (await Escalar<long>("SELECT count(*) FROM medio_pago")).Should().Be(1);
    }

    // RF-17, RF-25: la consulta conserva el plan actual completo aunque se desactive.
    [Fact]
    public async Task RF_25_Consultar_PlanInactivoConservaLimitesParaCambiar()
    {
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var plan = await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET activo = false, max_apis = 3, max_miembros = 5, cuota_peticiones = 250000, dominio_propio = true WHERE id = '{plan}'");
        await NuevaSuscripcionPlataforma(org, plan);
        using var client = Factory.CreateClient();

        using var response = await Enviar(client, HttpMethod.Get, "/api/suscripcion", org, user, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var actual = body.GetProperty("plan");
        actual.GetProperty("id").GetGuid().Should().Be(plan);
        actual.GetProperty("maxApis").GetInt32().Should().Be(3);
        actual.GetProperty("maxMiembros").GetInt32().Should().Be(5);
        actual.GetProperty("cuotaPeticiones").GetInt64().Should().Be(250000);
        actual.GetProperty("dominioPropio").GetBoolean().Should().BeTrue();
        actual.GetProperty("esPrueba").GetBoolean().Should().BeFalse();
    }

    // RF-20, RF-44: la reactivación no permite renovar Prueba ni un plan gratuito.
    [Theory]
    [InlineData(true, "en_gracia")]
    [InlineData(true, "suspendida")]
    [InlineData(false, "en_gracia")]
    [InlineData(false, "suspendida")]
    public async Task RF_44_Pagar_PruebaOGratuitoExigeContratarSinCobrar(bool esPrueba, string estado)
    {
        using var client = Factory.CreateClient();
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var plan = esPrueba
            ? await Escalar<Guid>("SELECT id FROM plan_plataforma WHERE es_prueba = true")
            : await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET precio = 0, es_prueba = {esPrueba} WHERE id = '{plan}'");
        var inicio = Reloj.Ahora.AddDays(-30);
        var fin = Reloj.Ahora.AddDays(-1);
        var sub = await Escalar<Guid>($"""
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin, gracia_hasta)
            VALUES (gen_random_uuid(), '{org}', '{plan}', '{estado}', '{inicio:O}', '{fin:O}', '{Reloj.Ahora.AddDays(7):O}') RETURNING id
            """);
        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/pagar", org, user,
            new { tarjeta = Tarjeta("4242424242424242") });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("codigo").GetString().Should().Be("requiere_plan_de_pago");
        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(estado);
        (await Escalar<DateTime>($"SELECT inicio FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(inicio.UtcDateTime);
        (await Escalar<DateTime>($"SELECT fin FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(fin.UtcDateTime);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{sub}'")).Should().Be(0);
        (await Escalar<long>("SELECT count(*) FROM medio_pago")).Should().Be(0);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE objetivo_id = '{sub}'")).Should().Be(0);
        Publicador.Publicadas.Should().BeEmpty();
    }

    // RF-25: cancelar sin un cambio pendiente es idempotente y no inventa acciones.
    [Fact]
    public async Task RF_25_CancelarCambio_SinCambioNoRegistraBitacora()
    {
        var (org, user, sub, _) = await Escenario();
        using var client = Factory.CreateClient();

        using var response = await Enviar(client, HttpMethod.Delete, "/api/suscripcion/cambio-programado", org, user, null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE objetivo_id = '{sub}'")).Should().Be(0);
    }

    // RF-20: el propietario puede pagar en gracia y el ciclo empieza de nuevo el día del pago.
    [Fact]
    public async Task RF_20_Pagar_ReactivaLaSuscripcionYPublicaOrganizacion()
    {
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var plan = await NuevoPlanPlataforma();
        var sub = await Escalar<Guid>($"""
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin, gracia_hasta)
            VALUES (gen_random_uuid(), '{org}', '{plan}', 'en_gracia', '{Reloj.Ahora.AddDays(-30):O}', '{Reloj.Ahora:O}', '{Reloj.Ahora.AddDays(7):O}') RETURNING id
            """);
        using var client = Factory.CreateClient();
        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/pagar", org, user,
            new { tarjeta = Tarjeta("4242424242424242") });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be("activa");
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{sub}' AND concepto = 'reactivacion' AND estado = 'autorizado'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE objetivo_tipo = 'suscripcion_plataforma' AND objetivo_id = '{sub}' AND accion = 'suscripcion_plataforma.cambiada'")).Should().Be(1);
        Publicador.Publicadas.Should().Contain(sub);
    }

    // RF-20: una suscripción suspendida también puede reactivarse con un pago aprobado.
    [Fact]
    public async Task RF_20_Pagar_DesdeSuspendidaReactivaLaSuscripcion()
    {
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var plan = await NuevoPlanPlataforma();
        var sub = await Escalar<Guid>($"""
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin, gracia_hasta)
            VALUES (gen_random_uuid(), '{org}', '{plan}', 'suspendida', '{Reloj.Ahora.AddDays(-30):O}', '{Reloj.Ahora.AddDays(-7):O}', '{Reloj.Ahora.AddDays(-1):O}') RETURNING id
            """);
        using var client = Factory.CreateClient();

        using var response = await Enviar(client, HttpMethod.Post, "/api/suscripcion/pagar", org, user,
            new { tarjeta = Tarjeta("4242424242424242") });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be("activa");
        (await Escalar<DateTime>($"SELECT inicio FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(Shapi.Dominio.Suscripciones.Suscripcion.InicioDeCiclo(Reloj.Ahora).UtcDateTime);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{sub}' AND concepto = 'reactivacion' AND estado = 'autorizado'")).Should().Be(1);
    }

    // RF-20: dos contrataciones simultáneas desde Prueba generan un cobro y una suscripción de pago.
    [Fact]
    public async Task RF_20_Contratar_ConcurrentesNoDuplicanCobro()
    {
        _demoraPagosMs = 250;
        var (org, user, _, plan) = await Escenario();
        using var client = Factory.CreateClient();
        var solicitud = new { planId = plan, tarjeta = Tarjeta("4242424242424242") };

        var respuestas = await Task.WhenAll(
            Enviar(client, HttpMethod.Post, "/api/suscripcion/contratar", org, user, solicitud),
            Enviar(client, HttpMethod.Post, "/api/suscripcion/contratar", org, user, solicitud));

        respuestas.Select(r => r.StatusCode).Should().ContainSingle(codigo => codigo == HttpStatusCode.Created);
        respuestas.Select(r => r.StatusCode).Should().ContainSingle(codigo => codigo == HttpStatusCode.Conflict);
        foreach (var response in respuestas)
        {
            response.Dispose();
        }
        (await Escalar<long>($"SELECT count(*) FROM suscripcion_plataforma WHERE organizacion_id = '{org}' AND plan_id = '{plan}' AND estado = 'activa'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM pago p JOIN suscripcion_plataforma s ON s.id = p.suscripcion_plataforma_id WHERE s.organizacion_id = '{org}' AND p.concepto = 'contratacion' AND p.estado = 'autorizado'")).Should().Be(1);
    }

    // RF-25: dos cambios simultáneos cobran y aplican el mismo cambio una sola vez.
    [Fact]
    public async Task RF_25_Cambiar_ConcurrentesNoCobranDosVeces()
    {
        _demoraPagosMs = 250;
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var origen = await NuevoPlanPlataforma();
        var destino = await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET precio = 599 WHERE id = '{destino}'");
        var medio = await NuevoMedioPago(org, null);
        var sub = await Escalar<Guid>($"""
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin, medio_pago_id)
            VALUES (gen_random_uuid(), '{org}', '{origen}', 'activa', '{Reloj.Ahora.AddDays(-17):O}', '{Reloj.Ahora.AddDays(13):O}', '{medio}') RETURNING id
            """);
        using var client = Factory.CreateClient();
        var solicitud = new { planId = destino, usarRegistrada = true };

        var respuestas = await Task.WhenAll(
            Enviar(client, HttpMethod.Post, "/api/suscripcion/cambiar", org, user, solicitud),
            Enviar(client, HttpMethod.Post, "/api/suscripcion/cambiar", org, user, solicitud));

        respuestas.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        foreach (var response in respuestas)
        {
            response.Dispose();
        }
        (await Escalar<Guid>($"SELECT plan_id FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be(destino);
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{sub}' AND concepto = 'cambio_plan' AND estado = 'autorizado'")).Should().Be(1);
    }

    // RF-20: dos pagos simultáneos reactivan una vez y el segundo encuentra la suscripción activa.
    [Fact]
    public async Task RF_20_Pagar_ConcurrentesReactivanUnaSolaVez()
    {
        _demoraPagosMs = 250;
        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var plan = await NuevoPlanPlataforma();
        var sub = await Escalar<Guid>($"""
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin, gracia_hasta)
            VALUES (gen_random_uuid(), '{org}', '{plan}', 'en_gracia', '{Reloj.Ahora.AddDays(-30):O}', '{Reloj.Ahora:O}', '{Reloj.Ahora.AddDays(7):O}') RETURNING id
            """);
        using var client = Factory.CreateClient();
        var solicitud = new { tarjeta = Tarjeta("4242424242424242") };

        var respuestas = await Task.WhenAll(
            Enviar(client, HttpMethod.Post, "/api/suscripcion/pagar", org, user, solicitud),
            Enviar(client, HttpMethod.Post, "/api/suscripcion/pagar", org, user, solicitud));

        respuestas.Select(r => r.StatusCode).Should().ContainSingle(codigo => codigo == HttpStatusCode.OK);
        respuestas.Select(r => r.StatusCode).Should().ContainSingle(codigo => codigo == HttpStatusCode.NotFound);
        foreach (var response in respuestas)
        {
            response.Dispose();
        }
        (await Escalar<string>($"SELECT estado FROM suscripcion_plataforma WHERE id = '{sub}'")).Should().Be("activa");
        (await Escalar<long>($"SELECT count(*) FROM pago WHERE suscripcion_plataforma_id = '{sub}' AND concepto = 'reactivacion' AND estado = 'autorizado'")).Should().Be(1);
    }

    private async Task<(Guid Organizacion, Guid Usuario, Guid Prueba, Guid Pago)> Escenario()
    {
        using (var inicializador = Factory.CreateClient())
        {
            await inicializador.GetAsync("/salud");
        }

        var org = await NuevaOrganizacion();
        var user = await NuevoUsuario();
        await NuevaMembresia(user, org, "propietario");
        var prueba = await Escalar<Guid>("SELECT id FROM plan_plataforma WHERE es_prueba = true");
        var pago = await Escalar<Guid>("SELECT id FROM plan_plataforma WHERE nombre = 'Lanzamiento'");
        var pruebaId = await Escalar<Guid>($"""
            INSERT INTO suscripcion_plataforma (id, organizacion_id, plan_id, estado, inicio, fin)
            VALUES (gen_random_uuid(), '{org}', '{prueba}', 'activa', '{Reloj.Ahora.AddDays(-10):O}', '{Reloj.Ahora.AddDays(20):O}') RETURNING id
            """);
        return (org, user, pruebaId, pago);
    }

    private static DatosTarjeta Tarjeta(string numero) => new()
    {
        Numero = numero,
        MesVencimiento = "12",
        AnioVencimiento = "2030",
        Cvv = "123",
        Titular = "Ana Lucía Morales",
    };

    private static async Task<HttpResponseMessage> Enviar(HttpClient client, HttpMethod method, string path, Guid org, Guid user, object? body, string rol = "Propietario")
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Requested-With", "shapi");
        request.Headers.Add("X-Prueba-Organizacion", org.ToString());
        request.Headers.Add("X-Prueba-Usuario", user.ToString());
        request.Headers.Add("X-Prueba-Rol", rol);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }
}
