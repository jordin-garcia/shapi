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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Api.Tests.Organizaciones;

[Collection(nameof(PostgresPersistencia))]
public sealed class MiembrosTests(PostgresPersistencia postgres) : BaseDePrueba(postgres), IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private WebApplicationFactory<Program> Factory => _factory ??= new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
        builder.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
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

    // RF-06, RF-43: el propietario consulta miembros, invitaciones y el límite del plan.
    [Fact]
    public async Task RF_06_ListarMiembros_IncluyePropietarioYConteoDeInvitaciones()
    {
        var (organizacion, propietario) = await PrepararOrganizacion(limite: 3);
        await Ejecutar($"INSERT INTO token (id, tipo, hash_token, organizacion_id, correo, rol, expira_en) VALUES (gen_random_uuid(), 'invitacion_miembro', repeat('a', 64), '{organizacion}', 'pendiente@example.com', 'editor', '{Reloj.Ahora.AddDays(7):O}')");
        using var client = Factory.CreateClient();

        using var response = await Enviar(client, HttpMethod.Get, "/api/miembros", organizacion, propietario, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("plan").GetProperty("nombre").GetString().Should().Be("Producto");
        body.RootElement.GetProperty("plan").GetProperty("maxMiembros").GetInt32().Should().Be(3);
        body.RootElement.GetProperty("total").GetInt32().Should().Be(2);
        body.RootElement.GetProperty("elementos").GetArrayLength().Should().Be(2);
    }

    // RF-06, RF-43: invita, encola el correo, crea una cuenta verificada y consume el token una sola vez.
    [Fact]
    public async Task RF_06_AceptarInvitacion_CreaUsuarioVerificadoYMembresia()
    {
        var (organizacion, propietario) = await PrepararOrganizacion(limite: 3);
        using var client = Factory.CreateClient();
        using var invitacion = await Enviar(client, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo = " Diego.Us@ejemplo.com ", rol = "editor" });
        invitacion.StatusCode.Should().Be(HttpStatusCode.Accepted, await invitacion.Content.ReadAsStringAsync());

        (await Escalar<string>("SELECT plantilla FROM correo_saliente WHERE destinatario = 'diego.us@ejemplo.com' ORDER BY creado_en DESC LIMIT 1")).Should().Be("invitacion_miembro");
        var token = await Escalar<string>("SELECT datos->>'token' FROM correo_saliente WHERE destinatario = 'diego.us@ejemplo.com' ORDER BY creado_en DESC LIMIT 1");
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE organizacion_id = '{organizacion}' AND accion = 'miembro.invitado' AND objetivo_tipo = 'invitacion_miembro'")).Should().Be(1);
        (await Escalar<DateTime>($"SELECT expira_en FROM token WHERE correo = 'diego.us@ejemplo.com'")).Should().Be(Reloj.Ahora.AddDays(7).UtcDateTime);
        using var consulta = await client.GetAsync($"/api/invitaciones/{token}");
        consulta.StatusCode.Should().Be(HttpStatusCode.OK);
        using var datos = JsonDocument.Parse(await consulta.Content.ReadAsStringAsync());
        datos.RootElement.GetProperty("correo").GetString().Should().Be("diego.us@ejemplo.com");
        datos.RootElement.GetProperty("rol").GetString().Should().Be("editor");

        using var aceptar = await Enviar(client, HttpMethod.Post, $"/api/invitaciones/{token}/aceptar", organizacion, propietario,
            new { nombre = "Diego Us Pérez", contrasena = "UnaContrasenaSegura123!" });

        aceptar.StatusCode.Should().Be(HttpStatusCode.Created, await aceptar.Content.ReadAsStringAsync());
        (await Escalar<long>("SELECT count(*) FROM usuario WHERE correo = 'diego.us@ejemplo.com' AND correo_verificado_en IS NOT NULL AND hash_contrasena IS NOT NULL")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM membresia m JOIN usuario u ON u.id = m.usuario_id WHERE m.organizacion_id = '{organizacion}' AND u.correo = 'diego.us@ejemplo.com' AND m.rol = 'editor'")).Should().Be(1);
        (await Escalar<long>("SELECT count(*) FROM token WHERE tipo = 'invitacion_miembro' AND usado_en IS NOT NULL")).Should().Be(1);
        using var repetida = await Enviar(client, HttpMethod.Post, $"/api/invitaciones/{token}/aceptar", organizacion, propietario,
            new { nombre = "Diego Us Pérez", contrasena = "UnaContrasenaSegura123!" });
        repetida.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // RF-06, RF-43: correos ya registrados en otra organización y límites alcanzados son 422.
    [Fact]
    public async Task RF_06_Invitar_OtraOrganizacionYLímiteDevuelven422()
    {
        var (organizacion, propietario) = await PrepararOrganizacion(limite: 1);
        var otra = await NuevaOrganizacion();
        var existente = await NuevoUsuario();
        await NuevaMembresia(existente, otra, "propietario");
        var correo = await Escalar<string>($"SELECT correo FROM usuario WHERE id = '{existente}'");
        using var client = Factory.CreateClient();

        using var otraOrganizacion = await Enviar(client, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo, rol = "lector" });
        otraOrganizacion.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Codigo(otraOrganizacion)).Should().Be("correo_en_otra_organizacion");

        using var limite = await Enviar(client, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo = "nuevo@example.com", rol = "lector" });
        limite.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Codigo(limite)).Should().Be("limite_del_plan");
        using var body = JsonDocument.Parse(await limite.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("detalle").GetProperty("limite").GetString().Should().Be("miembros");
    }

    // RF-06, RF-07: solo el propietario administra miembros, y nunca puede modificarse a sí mismo.
    [Fact]
    public async Task RF_06_AdministrarMiembros_ProtegePermisosYPropietario()
    {
        var (organizacion, propietario) = await PrepararOrganizacion(limite: 5);
        var editor = await NuevoUsuario();
        var miembro = await NuevoUsuario();
        await NuevaMembresia(editor, organizacion, "editor");
        await NuevaMembresia(miembro, organizacion, "lector");
        var membresiaPropietario = await Escalar<Guid>($"SELECT id FROM membresia WHERE usuario_id = '{propietario}'");
        using var client = Factory.CreateClient();
        using var anonima = Factory.CreateClient();

        using var sinSesion = await anonima.GetAsync("/api/miembros");
        sinSesion.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var prohibido = await Enviar(client, HttpMethod.Get, "/api/miembros", organizacion, editor, null, "Editor");
        prohibido.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var cambiarPropietario = await Enviar(client, HttpMethod.Put, $"/api/miembros/{membresiaPropietario}/rol", organizacion, propietario, new { rol = "editor" });
        cambiarPropietario.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var quitarPropietario = await Enviar(client, HttpMethod.Delete, $"/api/miembros/{membresiaPropietario}", organizacion, propietario, null);
        quitarPropietario.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var idMiembro = await Escalar<Guid>($"SELECT id FROM membresia WHERE usuario_id = '{miembro}'");
        using var cambio = await Enviar(client, HttpMethod.Put, $"/api/miembros/{idMiembro}/rol", organizacion, propietario, new { rol = "editor" });
        cambio.StatusCode.Should().Be(HttpStatusCode.OK);
        using var quitar = await Enviar(client, HttpMethod.Delete, $"/api/miembros/{idMiembro}", organizacion, propietario, null);
        quitar.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE organizacion_id = '{organizacion}' AND accion IN ('miembro.rol_cambiado','miembro.quitado')")).Should().Be(2);

        var otraOrganizacion = await NuevaOrganizacion();
        var otroUsuario = await NuevoUsuario();
        var otraMembresia = await NuevaMembresia(otroUsuario, otraOrganizacion, "lector");
        using var aislada = await Enviar(client, HttpMethod.Delete, $"/api/miembros/{otraMembresia}", organizacion, propietario, null);
        aislada.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<(Guid Organizacion, Guid Propietario)> PrepararOrganizacion(int limite)
    {
        var organizacion = await NuevaOrganizacion();
        var propietario = await NuevoUsuario();
        await NuevaMembresia(propietario, organizacion, "propietario");
        var plan = await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET nombre = 'Producto', max_miembros = {limite} WHERE id = '{plan}'");
        await NuevaSuscripcionPlataforma(organizacion, plan);
        return (organizacion, propietario);
    }

    private static async Task<string> Codigo(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("codigo").GetString()!;
    }

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

    private sealed class AutenticacionPrueba(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Esquema = "MiembrosPrueba";

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
}
