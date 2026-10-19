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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Correo;

namespace Shapi.Api.Tests.Organizaciones;

[Collection(nameof(PostgresPersistencia))]
public sealed class InvitacionesConsumidoresTests(PostgresPersistencia postgres) : BaseDePrueba(postgres), IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private WebApplicationFactory<Program> Factory => _factory ??= new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
        builder.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
        builder.UseSetting("SHAPI_DOMINIO_BASE", "shapi.localhost");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IReloj>(Reloj);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AutenticacionPrueba.Esquema;
                options.DefaultChallengeScheme = AutenticacionPrueba.Esquema;
                options.DefaultForbidScheme = AutenticacionPrueba.Esquema;
            }).AddScheme<AuthenticationSchemeOptions, AutenticacionPrueba>(AutenticacionPrueba.Esquema, _ => { });
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

    // RF-05: crea una invitación válida siete días y envía el enlace al subdominio del portal.
    [Fact]
    public async Task RF_05_InvitarConsumidor_CreaTokenYEncolaEnlaceDelPortal()
    {
        var (organizacion, propietario) = await Preparar();
        using var client = Factory.CreateClient();

        using var respuesta = await Enviar(client, HttpMethod.Post, $"/api/apis/{_apiId}/invitaciones", organizacion, propietario, new { correo = " Ana@ejemplo.com " });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await Escalar<string>("SELECT plantilla FROM correo_saliente WHERE destinatario = 'ana@ejemplo.com'")).Should().Be("invitacion_consumidor");
        var datosJson = await Escalar<string>("SELECT datos::text FROM correo_saliente WHERE destinatario = 'ana@ejemplo.com'");
        using var datos = JsonDocument.Parse(datosJson);
        var tokenCorreo = datos.RootElement.GetProperty("token").GetString()!;
        datos.RootElement.TryGetProperty("enlace", out _).Should().BeFalse();
        datos.RootElement.GetProperty("hostPortal").GetString().Should().Be("envios.shapi.localhost");
        datos.RootElement.GetProperty("nombreApi").GetString().Should().Be("API Envíos");
        var motor = new MotorPlantillasCorreo(new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SHAPI_DOMINIO_BASE"] = "shapi.localhost" }).Build());
        motor.Renderizar("invitacion_consumidor", datosJson).Texto.Should()
            .Contain($"https://envios.shapi.localhost/invitacion?token={Uri.EscapeDataString(tokenCorreo)}");
        (await Escalar<DateTime>("SELECT expira_en FROM token WHERE correo = 'ana@ejemplo.com' AND tipo = 'invitacion_consumidor'")).Should().Be(Reloj.Ahora.AddDays(7).UtcDateTime);

        using var lista = await Enviar(client, HttpMethod.Get, $"/api/apis/{_apiId}/invitaciones", organizacion, propietario, null);
        lista.StatusCode.Should().Be(HttpStatusCode.OK);
        using var cuerpo = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
        var invitacion = cuerpo.RootElement.GetProperty("elementos")[0];
        invitacion.GetProperty("correo").GetString().Should().Be("ana@ejemplo.com");
        invitacion.GetProperty("enviadaEn").GetDateTimeOffset().Should().Be(Reloj.Ahora);
        invitacion.GetProperty("venceEn").GetDateTimeOffset().Should().Be(Reloj.Ahora.AddDays(7));
    }

    // RF-05: reenviar rota el token y cancelar invalida la invitación pendiente.
    [Fact]
    public async Task RF_05_ReenviarYCancelarInvitacion_RotanTokenYLaRetiranDeLaLista()
    {
        var (organizacion, propietario) = await Preparar();
        using var client = Factory.CreateClient();
        using var creada = await Enviar(client, HttpMethod.Post, $"/api/apis/{_apiId}/invitaciones", organizacion, propietario, new { correo = "ana@ejemplo.com" });
        creada.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var id = await Escalar<Guid>("SELECT id FROM token WHERE correo = 'ana@ejemplo.com' AND tipo = 'invitacion_consumidor'");
        var tokenAnterior = await Escalar<string>("SELECT datos->>'token' FROM correo_saliente WHERE destinatario = 'ana@ejemplo.com'");

        using var reenviada = await Enviar(client, HttpMethod.Post, $"/api/apis/{_apiId}/invitaciones/{id}/reenviar", organizacion, propietario, null);

        reenviada.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await Escalar<long>("SELECT count(*) FROM token WHERE tipo = 'invitacion_consumidor' AND correo = 'ana@ejemplo.com' AND usado_en IS NOT NULL")).Should().Be(1);
        var nuevoToken = await Escalar<string>("SELECT datos->>'token' FROM correo_saliente WHERE destinatario = 'ana@ejemplo.com' ORDER BY creado_en DESC, id DESC LIMIT 1");
        nuevoToken.Should().NotBe(tokenAnterior);
        (await Escalar<DateTime>("SELECT expira_en FROM token WHERE tipo = 'invitacion_consumidor' AND correo = 'ana@ejemplo.com' AND usado_en IS NULL")).Should().Be(Reloj.Ahora.AddDays(7).UtcDateTime);

        using var cancelada = await Enviar(client, HttpMethod.Delete, $"/api/apis/{_apiId}/invitaciones/{await Escalar<Guid>("SELECT id FROM token WHERE tipo = 'invitacion_consumidor' AND correo = 'ana@ejemplo.com' AND usado_en IS NULL")}", organizacion, propietario, null);

        cancelada.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Escalar<long>("SELECT count(*) FROM token WHERE tipo = 'invitacion_consumidor' AND correo = 'ana@ejemplo.com' AND usado_en IS NULL")).Should().Be(0);
        using var lista = await Enviar(client, HttpMethod.Get, $"/api/apis/{_apiId}/invitaciones", organizacion, propietario, null);
        using var cuerpoLista = JsonDocument.Parse(await lista.Content.ReadAsStringAsync());
        cuerpoLista.RootElement.GetProperty("elementos").GetArrayLength().Should().Be(0);
    }

    // RF-05: no se invita a un correo que ya tiene cuenta de consumidor en la organización.
    [Fact]
    public async Task RF_05_InvitarConsumidorExistente_Responde409()
    {
        var (organizacion, propietario) = await Preparar();
        var consumidor = await NuevoConsumidor(organizacion);
        await Ejecutar($"UPDATE consumidor SET correo = 'ana@ejemplo.com' WHERE id = '{consumidor}'");
        using var client = Factory.CreateClient();

        using var respuesta = await Enviar(client, HttpMethod.Post, $"/api/apis/{_apiId}/invitaciones", organizacion, propietario, new { correo = "ana@ejemplo.com" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var cuerpo = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        cuerpo.RootElement.GetProperty("codigo").GetString().Should().Be("consumidor_existente");
    }

    // CU-23: una invitación pendiente vigente impide crear otra para el mismo correo en la organización.
    [Fact]
    public async Task CU_23_InvitarConsumidorConInvitacionPendiente_Responde409()
    {
        var (organizacion, propietario) = await Preparar();
        using var client = Factory.CreateClient();
        using var primera = await Enviar(client, HttpMethod.Post, $"/api/apis/{_apiId}/invitaciones", organizacion, propietario,
            new { correo = "ana@ejemplo.com" });
        primera.StatusCode.Should().Be(HttpStatusCode.Accepted);

        using var duplicada = await Enviar(client, HttpMethod.Post, $"/api/apis/{_apiId}/invitaciones", organizacion, propietario,
            new { correo = "ANA@ejemplo.com" });

        duplicada.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var cuerpo = JsonDocument.Parse(await duplicada.Content.ReadAsStringAsync());
        cuerpo.RootElement.GetProperty("codigo").GetString().Should().Be("invitacion_pendiente");
        (await Escalar<long>("SELECT count(*) FROM token WHERE correo = 'ana@ejemplo.com' AND tipo = 'invitacion_consumidor' AND usado_en IS NULL"))
            .Should().Be(1);
        (await Escalar<long>("SELECT count(*) FROM correo_saliente WHERE destinatario = 'ana@ejemplo.com' AND plantilla = 'invitacion_consumidor'"))
            .Should().Be(1);
    }

    // RF-05 y 04 §3.1: propietario y editor administran invitaciones; lector no puede hacerlo.
    [Fact]
    public async Task RF_05_InvitacionesConsumidores_PropietarioYEditorAccedenYLectorNo()
    {
        var (organizacion, propietario) = await Preparar();
        var editor = await NuevoUsuario();
        await NuevaMembresia(editor, organizacion, "editor");
        var lector = await NuevoUsuario();
        await NuevaMembresia(lector, organizacion, "lector");
        using var client = Factory.CreateClient();

        using var invitacionEditor = await Enviar(client, HttpMethod.Post, $"/api/apis/{_apiId}/invitaciones", organizacion, editor,
            new { correo = "editor@ejemplo.com" }, "Editor");
        using var prohibido = await Enviar(client, HttpMethod.Get, $"/api/apis/{_apiId}/invitaciones", organizacion, lector, null, "Lector");
        using var invitacionPropietario = await Enviar(client, HttpMethod.Post, $"/api/apis/{_apiId}/invitaciones", organizacion, propietario,
            new { correo = "propietario@ejemplo.com" });

        invitacionEditor.StatusCode.Should().Be(HttpStatusCode.Accepted);
        prohibido.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        invitacionPropietario.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    // RNF-08: otra organización no puede listar, crear, reenviar ni cancelar invitaciones de esta API.
    [Fact]
    public async Task RNF_08_InvitacionesConsumidores_OtraOrganizacionRecibe404EnTodosLosEndpoints()
    {
        var (organizacion, propietario) = await Preparar();
        using var client = Factory.CreateClient();
        using var creada = await Enviar(client, HttpMethod.Post, $"/api/apis/{_apiId}/invitaciones", organizacion, propietario,
            new { correo = "ana@ejemplo.com" });
        creada.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var invitacionId = await Escalar<Guid>("SELECT id FROM token WHERE correo = 'ana@ejemplo.com' AND tipo = 'invitacion_consumidor'");

        var otraOrganizacion = await NuevaOrganizacion();
        var otroUsuario = await NuevoUsuario();
        var rutaColeccion = $"/api/apis/{_apiId}/invitaciones";
        var rutaElemento = $"{rutaColeccion}/{invitacionId}";

        using var listar = await Enviar(client, HttpMethod.Get, rutaColeccion, otraOrganizacion, otroUsuario, null);
        using var crear = await Enviar(client, HttpMethod.Post, rutaColeccion, otraOrganizacion, otroUsuario,
            new { correo = "otra@ejemplo.com" });
        using var reenviar = await Enviar(client, HttpMethod.Post, $"{rutaElemento}/reenviar", otraOrganizacion, otroUsuario, null);
        using var cancelar = await Enviar(client, HttpMethod.Delete, rutaElemento, otraOrganizacion, otroUsuario, null);

        listar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        crear.StatusCode.Should().Be(HttpStatusCode.NotFound);
        reenviar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        cancelar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Escalar<long>("SELECT count(*) FROM token WHERE tipo = 'invitacion_consumidor' AND correo = 'ana@ejemplo.com' AND usado_en IS NULL"))
            .Should().Be(1);
        (await Escalar<long>("SELECT count(*) FROM correo_saliente WHERE destinatario IN ('ana@ejemplo.com', 'otra@ejemplo.com')"))
            .Should().Be(1);
    }

    private Guid _apiId;

    private async Task<(Guid Organizacion, Guid Propietario)> Preparar()
    {
        var organizacion = await NuevaOrganizacion();
        var propietario = await NuevoUsuario();
        await NuevaMembresia(propietario, organizacion, "propietario");
        _apiId = await NuevaApi(organizacion);
        await Ejecutar($"UPDATE api SET nombre = 'API Envíos', subdominio = 'envios', portal_nombre = 'Envíos Xelajú', portal_color = '#E11D48' WHERE id = '{_apiId}'");
        return (organizacion, propietario);
    }

    private static Task<HttpResponseMessage> Enviar(HttpClient client, HttpMethod method, string path, Guid org, Guid user, object? body, string rol = "Propietario")
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

        return client.SendAsync(request);
    }

    private sealed class AutenticacionPrueba(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Esquema = "InvitacionesConsumidoresPrueba";
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Prueba-Organizacion", out var org))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            Claim[] claims =
            [
                new(ClaimTypes.NameIdentifier, Request.Headers["X-Prueba-Usuario"].FirstOrDefault() ?? Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, "Ana"),
                new(ClaimTypes.Role, Request.Headers["X-Prueba-Rol"].FirstOrDefault() ?? "Propietario"),
                new("OrganizacionId", org.ToString()),
                new("Ambito", "Personal"),
            ];
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Esquema)), Esquema)));
        }
    }
}
