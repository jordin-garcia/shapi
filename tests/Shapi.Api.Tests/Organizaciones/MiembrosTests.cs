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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Correo;
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
        var (organizacion, propietario) = await PrepararOrganizacion(limite: 5);
        var editor = await NuevoUsuario();
        var lector = await NuevoUsuario();
        await NuevaMembresia(editor, organizacion, "editor");
        await NuevaMembresia(lector, organizacion, "lector");
        await Ejecutar($"INSERT INTO token (id, tipo, hash_token, organizacion_id, correo, rol, expira_en) VALUES (gen_random_uuid(), 'invitacion_miembro', repeat('a', 64), '{organizacion}', 'pendiente@example.com', 'editor', '{Reloj.Ahora.AddDays(7):O}')");
        using var client = Factory.CreateClient();

        using var response = await Enviar(client, HttpMethod.Get, "/api/miembros", organizacion, propietario, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("plan").GetProperty("nombre").GetString().Should().Be("Producto");
        body.RootElement.GetProperty("plan").GetProperty("maxMiembros").GetInt32().Should().Be(5);
        body.RootElement.GetProperty("total").GetInt32().Should().Be(4);
        var elementos = body.RootElement.GetProperty("elementos");
        elementos.GetArrayLength().Should().Be(3);
        elementos[0].GetProperty("rol").GetString().Should().Be("propietario");
        elementos[1].GetProperty("rol").GetString().Should().Be("editor");
        elementos[2].GetProperty("rol").GetString().Should().Be("lector");
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
        var datosCorreo = await Escalar<string>("SELECT datos::text FROM correo_saliente WHERE destinatario = 'diego.us@ejemplo.com' ORDER BY creado_en DESC LIMIT 1");
        using var datosCorreoJson = JsonDocument.Parse(datosCorreo);
        var token = datosCorreoJson.RootElement.GetProperty("token").GetString()!;
        datosCorreoJson.RootElement.TryGetProperty("enlace", out _).Should().BeFalse();
        var motor = new MotorPlantillasCorreo(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SHAPI_DOMINIO_BASE"] = "shapi.localhost",
        }).Build());
        var correoRenderizado = motor.Renderizar("invitacion_miembro", datosCorreo);
        correoRenderizado.Html.Should().Contain($"https://shapi.localhost/invitacion?token={Uri.EscapeDataString(token)}");
        correoRenderizado.Texto.Should().Contain($"https://shapi.localhost/invitacion?token={Uri.EscapeDataString(token)}");
        var correoId = await Escalar<Guid>("SELECT id FROM correo_saliente WHERE destinatario = 'diego.us@ejemplo.com' ORDER BY creado_en DESC LIMIT 1");
        await using (var db = CrearDb())
        {
            var correoSaliente = await db.Set<CorreoSaliente>().SingleAsync(c => c.Id == correoId);
            correoSaliente.MarcarEnviado(Reloj.Ahora);
            await db.SaveChangesAsync();
        }

        using var datosCorreoEnviado = JsonDocument.Parse(await Escalar<string>($"SELECT datos::text FROM correo_saliente WHERE id = '{correoId}'"));
        datosCorreoEnviado.RootElement.TryGetProperty("token", out _).Should().BeFalse();
        datosCorreoEnviado.RootElement.TryGetProperty("enlace", out _).Should().BeFalse();
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE organizacion_id = '{organizacion}' AND accion = 'miembro.invitado' AND objetivo_tipo = 'invitacion_miembro'")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE organizacion_id = '{organizacion}' AND accion = 'miembro.invitado' AND actor_nombre = 'Ana'")).Should().Be(1);
        (await Escalar<DateTime>($"SELECT expira_en FROM token WHERE correo = 'diego.us@ejemplo.com'")).Should().Be(Reloj.Ahora.AddDays(7).UtcDateTime);
        using var consulta = await client.GetAsync($"/api/invitaciones/{token}");
        consulta.StatusCode.Should().Be(HttpStatusCode.OK);
        using var datos = JsonDocument.Parse(await consulta.Content.ReadAsStringAsync());
        datos.RootElement.GetProperty("correo").GetString().Should().Be("diego.us@ejemplo.com");
        datos.RootElement.GetProperty("rol").GetString().Should().Be("editor");
        datos.RootElement.GetProperty("cuentaExistente").GetBoolean().Should().BeFalse();

        using var contrasenaLarga = await Enviar(client, HttpMethod.Post, $"/api/invitaciones/{token}/aceptar", organizacion, propietario,
            new { nombre = "Diego Us Pérez", contrasena = new string('x', 129) });
        contrasenaLarga.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var contrasenaCorreo = await Enviar(client, HttpMethod.Post, $"/api/invitaciones/{token}/aceptar", organizacion, propietario,
            new { nombre = "Diego Us Pérez", contrasena = "diego.us@ejemplo.com" });
        contrasenaCorreo.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var aceptar = await Enviar(client, HttpMethod.Post, $"/api/invitaciones/{token}/aceptar", organizacion, propietario,
            new { nombre = "Diego Us Pérez", contrasena = "UnaContrasenaSegura123!" });

        aceptar.StatusCode.Should().Be(HttpStatusCode.Created, await aceptar.Content.ReadAsStringAsync());
        (await Escalar<long>("SELECT count(*) FROM usuario WHERE correo = 'diego.us@ejemplo.com' AND correo_verificado_en IS NOT NULL AND hash_contrasena IS NOT NULL")).Should().Be(1);
        (await Escalar<long>($"SELECT count(*) FROM membresia m JOIN usuario u ON u.id = m.usuario_id WHERE m.organizacion_id = '{organizacion}' AND u.correo = 'diego.us@ejemplo.com' AND m.rol = 'editor'")).Should().Be(1);
        (await Escalar<long>("SELECT count(*) FROM token WHERE tipo = 'invitacion_miembro' AND usado_en IS NOT NULL")).Should().Be(1);
        using var repetida = await Enviar(client, HttpMethod.Post, $"/api/invitaciones/{token}/aceptar", organizacion, propietario,
            new { nombre = "Diego Us Pérez", contrasena = "UnaContrasenaSegura123!" });
        repetida.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // RF-06: al re-invitar una cuenta sin organización, se conserva y solo se agrega la membresía.
    [Fact]
    public async Task RF_06_ReinvitarCuentaSinOrganizacion_ReutilizaLaCuenta()
    {
        var (organizacion, propietario) = await PrepararOrganizacion(limite: 3);
        using var client = Factory.CreateClient();
        using var primera = await Enviar(client, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo = "diego.us@ejemplo.com", rol = "editor" });
        primera.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var primerToken = await Escalar<string>("SELECT datos->>'token' FROM correo_saliente WHERE destinatario = 'diego.us@ejemplo.com' ORDER BY creado_en DESC LIMIT 1");
        using var aceptarPrimera = await Enviar(client, HttpMethod.Post, $"/api/invitaciones/{primerToken}/aceptar", organizacion, propietario,
            new { nombre = "Diego Us Pérez", contrasena = "UnaContrasenaSegura123!" });
        aceptarPrimera.StatusCode.Should().Be(HttpStatusCode.Created);
        var usuarioId = await Escalar<Guid>("SELECT id FROM usuario WHERE correo = 'diego.us@ejemplo.com'");
        var hashAntes = await Escalar<string>($"SELECT hash_contrasena FROM usuario WHERE id = '{usuarioId}'");
        var membresia = await Escalar<Guid>($"SELECT id FROM membresia WHERE usuario_id = '{usuarioId}' AND organizacion_id = '{organizacion}'");

        using var quitar = await Enviar(client, HttpMethod.Delete, $"/api/miembros/{membresia}", organizacion, propietario, null);
        quitar.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var segunda = await Enviar(client, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo = "diego.us@ejemplo.com", rol = "lector" });
        segunda.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var segundoToken = await Escalar<string>("SELECT datos->>'token' FROM correo_saliente WHERE destinatario = 'diego.us@ejemplo.com' ORDER BY creado_en DESC LIMIT 1");
        using var consulta = await client.GetAsync($"/api/invitaciones/{segundoToken}");
        consulta.StatusCode.Should().Be(HttpStatusCode.OK, await consulta.Content.ReadAsStringAsync());
        using var detalle = JsonDocument.Parse(await consulta.Content.ReadAsStringAsync());
        detalle.RootElement.GetProperty("cuentaExistente").GetBoolean().Should().BeTrue();

        using var aceptarSegunda = await Enviar(client, HttpMethod.Post, $"/api/invitaciones/{segundoToken}/aceptar", organizacion, propietario, new { });
        aceptarSegunda.StatusCode.Should().Be(HttpStatusCode.Created, await aceptarSegunda.Content.ReadAsStringAsync());
        (await Escalar<Guid>("SELECT id FROM usuario WHERE correo = 'diego.us@ejemplo.com'")).Should().Be(usuarioId);
        (await Escalar<string>($"SELECT hash_contrasena FROM usuario WHERE id = '{usuarioId}'")).Should().Be(hashAntes);
        (await Escalar<string>($"SELECT nombre FROM usuario WHERE id = '{usuarioId}'")).Should().Be("Diego Us Pérez");
        (await Escalar<string>($"SELECT rol FROM membresia WHERE usuario_id = '{usuarioId}' AND organizacion_id = '{organizacion}'")).Should().Be("lector");
    }

    // RF-06: si una cuenta obtiene otra membresía después de recibir la invitación, no se ofrece como reutilizable y la aceptación se rechaza.
    [Fact]
    public async Task RF_06_CuentaConMembresiaPosterior_NoSeOfreceComoReutilizable()
    {
        var (organizacion, propietario) = await PrepararOrganizacion(limite: 3);
        using var client = Factory.CreateClient();
        using var invitacion = await Enviar(client, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo = "existente@example.com", rol = "lector" });
        invitacion.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var token = await Escalar<string>("SELECT datos->>'token' FROM correo_saliente WHERE destinatario = 'existente@example.com'");

        var otraOrganizacion = await NuevaOrganizacion();
        var usuario = await NuevoUsuario();
        await Ejecutar($"UPDATE usuario SET correo = 'existente@example.com' WHERE id = '{usuario}'");
        await NuevaMembresia(usuario, otraOrganizacion, "lector");

        using var consulta = await client.GetAsync($"/api/invitaciones/{token}");
        consulta.StatusCode.Should().Be(HttpStatusCode.OK);
        using var datos = JsonDocument.Parse(await consulta.Content.ReadAsStringAsync());
        datos.RootElement.GetProperty("cuentaExistente").GetBoolean().Should().BeFalse();

        using var aceptar = await Enviar(client, HttpMethod.Post, $"/api/invitaciones/{token}/aceptar", organizacion, propietario,
            new { nombre = "Cuenta existente", contrasena = "UnaContrasenaSegura123!" });
        aceptar.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Codigo(aceptar)).Should().Be("correo_en_otra_organizacion");
        (await Escalar<long>("SELECT count(*) FROM token WHERE correo = 'existente@example.com' AND usado_en IS NOT NULL")).Should().Be(0);
    }

    // RF-06, RF-43: correos ya registrados en otra organización y límites alcanzados son 422.
    [Fact]
    public async Task RF_06_Invitar_OtraOrganizacionYLímiteDevuelven422()
    {
        var (organizacion, propietario) = await PrepararOrganizacion(limite: 2);
        var otra = await NuevaOrganizacion();
        var existente = await NuevoUsuario();
        await NuevaMembresia(existente, otra, "propietario");
        var correo = await Escalar<string>($"SELECT correo FROM usuario WHERE id = '{existente}'");
        using var client = Factory.CreateClient();

        using var otraOrganizacion = await Enviar(client, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo, rol = "lector" });
        otraOrganizacion.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Codigo(otraOrganizacion)).Should().Be("correo_en_otra_organizacion");

        using var pendiente = await Enviar(client, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo = "pendiente@example.com", rol = "lector" });
        pendiente.StatusCode.Should().Be(HttpStatusCode.Accepted);
        using var limite = await Enviar(client, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo = "nuevo@example.com", rol = "lector" });
        limite.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await Codigo(limite)).Should().Be("limite_del_plan");
        using var body = JsonDocument.Parse(await limite.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("detalle").GetProperty("limite").GetString().Should().Be("miembros");
    }

    // RF-06: las invitaciones vencidas no se pueden consultar ni aceptar.
    [Fact]
    public async Task RF_06_InvitacionVencida_NoSePuedeConsultarNiAceptar()
    {
        var (organizacion, propietario) = await PrepararOrganizacion(limite: 3);
        using var client = Factory.CreateClient();
        using var respuesta = await Enviar(client, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo = "vencida@example.com", rol = "lector" });
        respuesta.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var token = await Escalar<string>("SELECT datos->>'token' FROM correo_saliente WHERE destinatario = 'vencida@example.com'");
        await Ejecutar($"UPDATE token SET expira_en = '{Reloj.Ahora.AddSeconds(-1):O}' WHERE correo = 'vencida@example.com'");

        using var consulta = await client.GetAsync($"/api/invitaciones/{token}");
        consulta.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var aceptar = await Enviar(client, HttpMethod.Post, $"/api/invitaciones/{token}/aceptar", organizacion, propietario,
            new { nombre = "Persona Invitada", contrasena = "UnaContrasenaSegura123!" });
        aceptar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Escalar<long>("SELECT count(*) FROM usuario WHERE correo = 'vencida@example.com'")).Should().Be(0);
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
        var correoMiembro = await Escalar<string>($"SELECT correo FROM usuario WHERE id = '{miembro}'");
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
        (await Escalar<long>($"SELECT count(*) FROM bitacora WHERE organizacion_id = '{organizacion}' AND accion IN ('miembro.rol_cambiado','miembro.quitado') AND actor_nombre = 'Ana'")).Should().Be(2);
        (await Escalar<string>($"SELECT descripcion FROM bitacora WHERE organizacion_id = '{organizacion}' AND accion = 'miembro.rol_cambiado'")).Should().Be($"Cambió el rol de {correoMiembro} de lector a editor");

        var otraOrganizacion = await NuevaOrganizacion();
        var otroUsuario = await NuevoUsuario();
        var otraMembresia = await NuevaMembresia(otroUsuario, otraOrganizacion, "lector");
        using var aislada = await Enviar(client, HttpMethod.Delete, $"/api/miembros/{otraMembresia}", organizacion, propietario, null);
        aislada.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // RF-06: la aceptación de invitación es pública y no requiere sesión.
    [Fact]
    public async Task RF_06_AceptarInvitacion_NoRequiereSesion()
    {
        var (organizacion, propietario) = await PrepararOrganizacion(limite: 3);
        using var clientePropietario = Factory.CreateClient();
        using var invitacion = await Enviar(clientePropietario, HttpMethod.Post, "/api/miembros/invitaciones", organizacion, propietario,
            new { correo = "sin-sesion@example.com", rol = "lector" });
        invitacion.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var token = await Escalar<string>("SELECT datos->>'token' FROM correo_saliente WHERE destinatario = 'sin-sesion@example.com'");

        using var clienteAnonimo = Factory.CreateClient();
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, $"/api/invitaciones/{token}/aceptar")
        {
            Content = JsonContent.Create(new { nombre = "Persona Invitada", contrasena = "UnaContrasenaSegura123!" }),
        };
        solicitud.Headers.Add("X-Requested-With", "shapi");
        using var respuesta = await clienteAnonimo.SendAsync(solicitud);

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created, await respuesta.Content.ReadAsStringAsync());
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
                new(ClaimTypes.Name, "Nombre no confiable de prueba"),
                new(ClaimTypes.Role, Request.Headers["X-Prueba-Rol"].FirstOrDefault() ?? "Propietario"),
                new("OrganizacionId", org.ToString()),
                new("Ambito", "Personal"),
            ];
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Esquema)), Esquema)));
        }
    }
}
