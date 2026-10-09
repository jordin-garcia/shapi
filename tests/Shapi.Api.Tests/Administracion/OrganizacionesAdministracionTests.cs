extern alias compuerta;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shapi.Api.Tests.Cache;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos.Redis;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;
using StackExchange.Redis;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;
using ProgramaCompuerta = compuerta::Program;

namespace Shapi.Api.Tests.Administracion;

[Collection(nameof(RedisCache))]
public sealed class OrganizacionesAdministracionTests(PostgresPersistencia postgres, RedisCache redis)
    : BaseCache(postgres, redis), IAsyncLifetime
{
    private const string Contrasena = "SuperSecreto123!";
    private const string Clave = "shp_prod_4fN8qT2xLm6Rv0Zk9Wd3Hs7c2e";
    private const string HostApi = "envios.api.shapi.localhost";
    private WebApplicationFactory<Program> _api = null!;
    private HttpClient _cliente = null!;

    async Task IAsyncLifetime.InitializeAsync()
    {
        await base.InitializeAsync();
        _api = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            builder.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
            builder.UseSetting("SHAPI_REDIS", RedisCache.Cadena);
            builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Rodrigo Alvarado");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", Contrasena);
            builder.UseSetting("SHAPI_DOMINIO_BASE", DominioBase);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IReloj>();
                services.AddSingleton<IReloj>(Reloj);
            });
        });
        _cliente = _api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _cliente.Dispose();
        await _api.DisposeAsync();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task RF_38_Listar_AdministradorYSoporteRecibenLosDatosYProveedorRecibe403()
    {
        var datos = await CrearOrganizacionProveedor("activa");
        await NuevaApi(datos.OrganizacionId);
        await NuevaApi(datos.OrganizacionId);
        await NuevoConsumidor(datos.OrganizacionId);
        var segunda = await CrearOrganizacionProveedor("activa");
        var admin = await CrearPersonal(Rol.Administrador, "Administradora");
        var soporte = await CrearPersonal(Rol.Soporte, "Soporte");
        var proveedor = await CrearSesion(datos.PropietarioId);

        var respuestaAdmin = await Enviar(HttpMethod.Get, "/api/admin/organizaciones", await CrearSesion(admin));
        var respuestaSoporte = await Enviar(HttpMethod.Get, "/api/admin/organizaciones", await CrearSesion(soporte));
        var respuestaProveedor = await Enviar(HttpMethod.Get, "/api/admin/organizaciones", proveedor);

        respuestaAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
        respuestaSoporte.StatusCode.Should().Be(HttpStatusCode.OK);
        respuestaProveedor.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var elementos = (await respuestaAdmin.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray();
        elementos.Select(e => e.GetProperty("id").GetGuid()).Should().ContainInOrder(datos.OrganizacionId, segunda.OrganizacionId);
        var elemento = elementos[0];
        elemento.GetProperty("nombre").GetString().Should().Be("Organización de prueba");
        elemento.GetProperty("propietarioCorreo").GetString().Should().Be(datos.PropietarioCorreo);
        elemento.GetProperty("numeroApis").GetInt32().Should().Be(2);
        elemento.GetProperty("numeroConsumidores").GetInt32().Should().Be(1);
        elemento.GetProperty("plan").GetString().Should().NotBeNullOrWhiteSpace();
        elemento.GetProperty("estado").GetString().Should().Be("activa");
    }

    [Fact]
    public async Task RF_38_Suspender_PublicaRedisYLaCompuertaEnMemoriaResponde403()
    {
        var datos = await CrearOrganizacionProveedor("activa");
        var admin = await CrearPersonal(Rol.Administrador, "Rodrigo Alvarado");
        var apiId = Guid.NewGuid();
        var suscripcionId = Guid.NewGuid();
        await PrepararCompuerta(apiId, datos.OrganizacionId, suscripcionId);
        await using var compuerta = CrearCompuerta();
        using var clienteCompuerta = compuerta.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"http://{HostApi}"),
        });

        var respuesta = await Enviar(HttpMethod.Post,
            $"/api/admin/organizaciones/{datos.OrganizacionId}/suspender",
            await CrearSesion(admin), new { motivo = "Revision administrativa" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await using (var db = CrearDb())
        {
            var organizacion = await db.Set<Organizacion>().IgnoreQueryFilters().SingleAsync(o => o.Id == datos.OrganizacionId);
            organizacion.EstadoAdmin.Should().Be(EstadoAdmin.Suspendida);
            organizacion.MotivoSuspension.Should().Be("Revision administrativa");
            (await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().SingleAsync(e => e.Accion == "organizacion.suspendida"))
                .Descripcion.Should().Be("Suspendió la organización Organización de prueba");
            var correo = await db.Set<CorreoSaliente>().SingleAsync(c => c.Plantilla == "organizacion_suspendida");
            correo.Destinatario.Should().Be(datos.PropietarioCorreo);
            correo.Datos.Should().Contain("Revision administrativa");
        }
        (await Redis.HashGetAsync(LlavesRedis.Organizacion(datos.OrganizacionId), ContextoOrganizacion.CampoEstadoEfectivo))
            .ToString().Should().Be("suspendida");

        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/cotizaciones");
        peticion.Headers.Add("X-Api-Key", Clave);
        using var respuestaCompuerta = await clienteCompuerta.SendAsync(peticion);
        respuestaCompuerta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await respuestaCompuerta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetProperty("codigo").GetString()
            .Should().Be("api_no_disponible");
    }

    [Fact]
    public async Task RF_38_Reactivar_SuscripcionSuspendidaMantieneElEstadoEfectivoSuspendido()
    {
        var datos = await CrearOrganizacionProveedor("suspendida");
        await Ejecutar($"UPDATE organizacion SET estado_admin = 'suspendida', motivo_suspension = 'Revision' WHERE id = '{datos.OrganizacionId}'");
        var admin = await CrearPersonal(Rol.Administrador, "Rodrigo Alvarado");

        var respuesta = await Enviar(HttpMethod.Post,
            $"/api/admin/organizaciones/{datos.OrganizacionId}/reactivar", await CrearSesion(admin));

        respuesta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await using var db = CrearDb();
        var organizacion = await db.Set<Organizacion>().IgnoreQueryFilters().SingleAsync(o => o.Id == datos.OrganizacionId);
        organizacion.EstadoAdmin.Should().Be(EstadoAdmin.Activa);
        organizacion.MotivoSuspension.Should().BeNull();
        (await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().SingleAsync(e => e.Accion == "organizacion.reactivada"))
            .Descripcion.Should().Be("Reactivó la organización Organización de prueba");
        (await Redis.HashGetAsync(LlavesRedis.Organizacion(datos.OrganizacionId), ContextoOrganizacion.CampoEstadoEfectivo))
            .ToString().Should().Be("suspendida");
    }

    [Fact]
    public async Task RF_38_Suspender_SoporteRecibe403YMotivoVacioRecibe400()
    {
        var datos = await CrearOrganizacionProveedor("activa");
        var soporte = await CrearPersonal(Rol.Soporte, "Soporte");
        var admin = await CrearPersonal(Rol.Administrador, "Administradora");

        (await Enviar(HttpMethod.Post, $"/api/admin/organizaciones/{datos.OrganizacionId}/suspender",
            await CrearSesion(soporte), new { motivo = "Revision" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Enviar(HttpMethod.Post, $"/api/admin/organizaciones/{datos.OrganizacionId}/reactivar",
            await CrearSesion(soporte))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var invalida = await Enviar(HttpMethod.Post, $"/api/admin/organizaciones/{datos.OrganizacionId}/suspender",
            await CrearSesion(admin), new { motivo = "   " });

        invalida.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalida.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString()
            .Should().Be("datos_invalidos");
    }

    private async Task<(Guid OrganizacionId, Guid PropietarioId, string PropietarioCorreo)> CrearOrganizacionProveedor(string estadoSuscripcion)
    {
        var organizacion = await NuevaOrganizacion();
        var propietario = await NuevoUsuario();
        await NuevaMembresia(propietario, organizacion, "propietario");
        var plan = await NuevoPlanPlataforma();
        await NuevaSuscripcionPlataforma(organizacion, plan, estadoSuscripcion);
        var correo = await Escalar<string>($"SELECT correo FROM usuario WHERE id = '{propietario}'");
        return (organizacion, propietario, correo);
    }

    private async Task<Guid> CrearPersonal(Rol rol, string nombre)
    {
        var organizacion = await Escalar<Guid>("SELECT id FROM organizacion WHERE tipo = 'plataforma'");
        var usuario = await NuevoUsuario();
        await Ejecutar($"UPDATE usuario SET nombre = '{nombre}' WHERE id = '{usuario}'");
        await NuevaMembresia(usuario, organizacion, rol.ToString().ToLowerInvariant());
        return usuario;
    }

    private async Task<string> CrearSesion(Guid usuarioId)
    {
        var valor = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        await using var db = CrearDb();
        db.Add(Sesion.IniciarPersonal(SeguridadTokens.HashearToken(valor), usuarioId, "localhost", null, null, Reloj.Ahora));
        await db.SaveChangesAsync();
        return $"shapi_sesion={valor}";
    }

    private async Task<HttpResponseMessage> Enviar(HttpMethod metodo, string url, string cookie, object? cuerpo = null)
    {
        using var peticion = new HttpRequestMessage(metodo, url);
        peticion.Headers.Add("Cookie", cookie);
        if (metodo != HttpMethod.Get)
        {
            peticion.Headers.Add("X-Requested-With", "shapi");
        }
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }
        return await _cliente.SendAsync(peticion);
    }

    private async Task PrepararCompuerta(Guid apiId, Guid organizacionId, Guid suscripcionId)
    {
        var claveId = Guid.NewGuid();
        var consumidorId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await EscribirHash(LlavesRedis.Api(apiId), new ContextoApi(
            apiId, organizacionId, ContextoApi.EstadoPublicada, "http://origen.prueba", null,
            "envios.shapi.localhost", 1).ACampos());
        await Redis.StringSetAsync(LlavesRedis.ApiPorHost(HostApi), apiId.ToString());
        await EscribirHash(LlavesRedis.Clave(ContextoClave.CalcularHash(Clave)), new ContextoClave(
            claveId, suscripcionId, apiId, organizacionId, consumidorId, ContextoClave.TipoProduccion).ACampos());
        await EscribirHash(LlavesRedis.Suscripcion(suscripcionId), new ContextoSuscripcion(
            suscripcionId, planId, "Comercio", ContextoSuscripcion.EstadoActiva,
            Reloj.Ahora.AddDays(-1).ToUnixTimeSeconds(), Reloj.Ahora.AddDays(29).ToUnixTimeSeconds(), 1000, 60).ACampos());
    }

    private Task EscribirHash(string llave, IReadOnlyDictionary<string, string> campos) =>
        Redis.HashSetAsync(llave, campos.Select(campo => new HashEntry(campo.Key, campo.Value)).ToArray());

    private WebApplicationFactory<ProgramaCompuerta> CrearCompuerta() =>
        new WebApplicationFactory<ProgramaCompuerta>().WithWebHostBuilder(web =>
        {
            web.UseSetting("SHAPI_REDIS", RedisCache.Cadena);
            web.ConfigureTestServices(servicios => servicios.AddSingleton(new HttpMessageInvoker(new OrigenEnMemoria())));
        });

    private sealed class OrigenEnMemoria : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
