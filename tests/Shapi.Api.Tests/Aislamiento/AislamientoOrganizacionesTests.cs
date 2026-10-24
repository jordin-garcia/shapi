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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;

namespace Shapi.Api.Tests.Aislamiento;

[Collection(nameof(PostgresPersistencia))]
public sealed class AislamientoOrganizacionesTests(PostgresPersistencia postgres)
    : BaseDePrueba(postgres), IAsyncLifetime
{
    private WebApplicationFactory<Program>? _fabrica;

    private WebApplicationFactory<Program> Fabrica => _fabrica ??= new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
        builder.UseSetting("SHAPI_POSTGRES_CADENA", Cadena);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IReloj>(Reloj);
            services.AddAuthentication(opciones =>
            {
                opciones.DefaultAuthenticateScheme = AutenticacionPrueba.Esquema;
                opciones.DefaultChallengeScheme = AutenticacionPrueba.Esquema;
                opciones.DefaultForbidScheme = AutenticacionPrueba.Esquema;
            }).AddScheme<AuthenticationSchemeOptions, AutenticacionPrueba>(AutenticacionPrueba.Esquema, _ => { });
        });
    });

    public new async Task DisposeAsync()
    {
        if (_fabrica is not null)
        {
            await _fabrica.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    // RNF-08: cada ruta de control que recibe el identificador de una API oculta el recurso de otra organización.
    [Theory]
    [MemberData(nameof(RutasApiPorIdentificador))]
    public async Task RNF_08_RutasDeApi_OtraOrganizacionSiempreRecibe404(HttpMethod metodo, string plantilla, bool requiereCuerpo)
    {
        var organizacionPropia = await NuevaOrganizacion();
        var usuario = await NuevoUsuario();
        await NuevaMembresia(usuario, organizacionPropia, "propietario");
        var organizacionAjena = await NuevaOrganizacion();
        var apiAjena = await NuevaApi(organizacionAjena);
        var dominioAjeno = $"api-{Guid.NewGuid():N}.ejemplo.com";
        await Ejecutar($"""
            INSERT INTO dominio_propio (id, api_id, dominio, destino_cname, estado, creado_en, actualizado_en)
            VALUES (gen_random_uuid(), '{apiAjena}', '{dominioAjeno}', '{apiAjena}.api.shapi.localhost', 'pendiente',
                    '{Reloj.Ahora:O}', '{Reloj.Ahora:O}')
            """);
        await Ejecutar($"UPDATE api SET portal_logo = decode('89504E470D0A1A0A', 'hex'), portal_logo_tipo = 'image/png' WHERE id = '{apiAjena}'");
        var planAjeno = await NuevoPlanApi(apiAjena);
        var consumidorAjeno = await NuevoConsumidor(organizacionAjena);
        var claveAjena = await CrearClave(apiAjena, consumidorAjeno, planAjeno);
        var correoInvitacion = $"{Guid.NewGuid():N}@ejemplo.com";
        var invitacionAjena = await Escalar<Guid>($"""
            INSERT INTO token (id, tipo, hash_token, organizacion_id, correo, rol, expira_en)
            VALUES (gen_random_uuid(), 'invitacion_consumidor', repeat('a', 64), '{organizacionAjena}', '{correoInvitacion}',
                    'consumidor', '{Reloj.Ahora.AddDays(7):O}') RETURNING id
            """);
        var ruta = plantilla
            .Replace("{apiId}", apiAjena.ToString())
            .Replace("{id}", apiAjena.ToString())
            .Replace("{planId}", planAjeno.ToString())
            .Replace("{claveId}", claveAjena.ToString())
            .Replace("{invitacionId}", invitacionAjena.ToString());

        using var cliente = Fabrica.CreateClient();
        using var respuesta = await Enviar(cliente, metodo, ruta, organizacionPropia, usuario,
            requiereCuerpo ? CuerpoPara(ruta) : null);

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound, $"{metodo} {plantilla} no debe revelar recursos de otra organización");
    }

    public static TheoryData<HttpMethod, string, bool> RutasApiPorIdentificador() => new()
    {
        { HttpMethod.Put, "/api/apis/{id}/especificacion", true },
        { HttpMethod.Get, "/api/apis/{id}/rutas", false },
        { HttpMethod.Put, "/api/apis/{id}/rutas/exposicion", true },
        { HttpMethod.Put, "/api/apis/{id}/configuracion-rutas", true },
        { HttpMethod.Post, "/api/apis/{id}/publicar", false },
        { HttpMethod.Post, "/api/apis/{id}/despublicar", false },
        { HttpMethod.Get, "/api/apis/{id}/dominio", false },
        { HttpMethod.Post, "/api/apis/{id}/dominio", true },
        { HttpMethod.Post, "/api/apis/{id}/dominio/simular-registro", false },
        { HttpMethod.Post, "/api/apis/{id}/dominio/verificar", false },
        { HttpMethod.Delete, "/api/apis/{id}/dominio", false },
        { HttpMethod.Post, "/api/apis/{id}/secreto/regenerar", false },
        { HttpMethod.Get, "/api/apis/{id}/portal", false },
        { HttpMethod.Put, "/api/apis/{id}/portal", true },
        { HttpMethod.Get, "/api/apis/{id}/portal/logo", false },
        { HttpMethod.Put, "/api/apis/{id}/portal/logo", true },
        { HttpMethod.Get, "/api/apis/{apiId}/consumidores", false },
        { HttpMethod.Get, "/api/apis/{apiId}/consumo", false },
        { HttpMethod.Get, "/api/apis/{apiId}/invitaciones", false },
        { HttpMethod.Post, "/api/apis/{apiId}/invitaciones", true },
        { HttpMethod.Post, "/api/apis/{apiId}/invitaciones/{invitacionId}/reenviar", false },
        { HttpMethod.Delete, "/api/apis/{apiId}/invitaciones/{invitacionId}", false },
        { HttpMethod.Get, "/api/apis/{apiId}/claves", false },
        { HttpMethod.Post, "/api/apis/{apiId}/claves/{claveId}/revocar", false },
        { HttpMethod.Get, "/api/apis/{apiId}/planes", false },
        { HttpMethod.Post, "/api/apis/{apiId}/planes", true },
        { HttpMethod.Put, "/api/apis/{apiId}/planes/{planId}", true },
        { HttpMethod.Post, "/api/apis/{apiId}/planes/{planId}/desactivar", false },
    };

    // RNF-08: los endpoints de miembros también filtran por la organización de la sesión.
    [Fact]
    public async Task RNF_08_MiembrosDeOtraOrganizacion_Responden404AlCambiarRolYEliminar()
    {
        var organizacionPropia = await NuevaOrganizacion();
        var propietario = await NuevoUsuario();
        await NuevaMembresia(propietario, organizacionPropia, "propietario");
        var organizacionAjena = await NuevaOrganizacion();
        var miembroAjeno = await NuevoUsuario();
        var membresiaAjena = await NuevaMembresia(miembroAjeno, organizacionAjena, "editor");
        using var cliente = Fabrica.CreateClient();

        using var cambiarRol = await Enviar(cliente, HttpMethod.Put, $"/api/miembros/{membresiaAjena}/rol",
            organizacionPropia, propietario, new { rol = "lector" });
        using var eliminar = await Enviar(cliente, HttpMethod.Delete, $"/api/miembros/{membresiaAjena}",
            organizacionPropia, propietario, null);

        cambiarRol.StatusCode.Should().Be(HttpStatusCode.NotFound);
        eliminar.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // RF-07: propietario y editor pueden efectuar sus operaciones de escritura permitidas.
    [Fact]
    public async Task RF_07_PropietarioInvitaMiembroYEditorInvitaConsumidor()
    {
        var organizacion = await NuevaOrganizacion();
        var propietario = await NuevoUsuario();
        await NuevaMembresia(propietario, organizacion, "propietario");
        var editor = await NuevoUsuario();
        await NuevaMembresia(editor, organizacion, "editor");
        await PrepararSuscripcionPlataforma(organizacion);
        var api = await NuevaApi(organizacion);
        using var cliente = Fabrica.CreateClient();

        using var invitacionMiembro = await Enviar(cliente, HttpMethod.Post, "/api/miembros/invitaciones",
            organizacion, propietario, new { correo = "nuevo-miembro@ejemplo.com", rol = "lector" });
        using var invitacionConsumidor = await Enviar(cliente, HttpMethod.Post, $"/api/apis/{api}/invitaciones",
            organizacion, editor, new { correo = "nuevo-consumidor@ejemplo.com" }, "Editor");

        invitacionMiembro.StatusCode.Should().Be(HttpStatusCode.Accepted);
        invitacionConsumidor.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    // RF-07: soporte y administración consultan su panel; el consumidor accede a su historial en su portal.
    [Fact]
    public async Task RF_07_AdministracionSoporteYConsumidor_AccedenALasLecturasPermitidas()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        await Ejecutar($"UPDATE api SET estado = 'publicada' WHERE id = '{api}'");
        var subdominio = await Escalar<string>($"SELECT subdominio FROM api WHERE id = '{api}'");
        var consumidor = await NuevoConsumidor(organizacion);
        var usuarioAdmin = await NuevoUsuario();
        var usuarioSoporte = await NuevoUsuario();
        using var cliente = Fabrica.CreateClient();

        using var admin = await Enviar(cliente, HttpMethod.Get, "/api/admin/organizaciones", organizacion, usuarioAdmin, null, "Administrador");
        using var soporte = await Enviar(cliente, HttpMethod.Get, "/api/admin/organizaciones", organizacion, usuarioSoporte, null, "Soporte");
        using var consumidorPortal = await Enviar(cliente, HttpMethod.Get, "/api/portal/pagos", organizacion, consumidor, null,
            "Consumidor", $"{subdominio}.shapi.localhost");

        admin.StatusCode.Should().Be(HttpStatusCode.OK);
        soporte.StatusCode.Should().Be(HttpStatusCode.OK);
        consumidorPortal.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // RNF-08: los casos solo son visibles para personal de la organización que los abrió.
    [Fact]
    public async Task RNF_08_CasosDeOtraOrganizacion_ConsultarYResponderDevuelven404()
    {
        var organizacionAjena = await NuevaOrganizacion();
        var propietarioAjeno = await NuevoUsuario();
        await NuevaMembresia(propietarioAjeno, organizacionAjena, "propietario");
        var organizacionPropia = await NuevaOrganizacion();
        var propietarioPropio = await NuevoUsuario();
        await NuevaMembresia(propietarioPropio, organizacionPropia, "propietario");
        using var cliente = Fabrica.CreateClient();

        using var abrir = await Enviar(cliente, HttpMethod.Post, "/api/casos", organizacionAjena, propietarioAjeno,
            new { asunto = "Caso de otra organización", descripcion = "No debe ser visible." });
        abrir.StatusCode.Should().Be(HttpStatusCode.Created);
        using var detalle = JsonDocument.Parse(await abrir.Content.ReadAsStringAsync());
        var numero = detalle.RootElement.GetProperty("numero").GetInt32();

        using var consultar = await Enviar(cliente, HttpMethod.Get, $"/api/casos/{numero}", organizacionPropia, propietarioPropio, null);
        using var responder = await Enviar(cliente, HttpMethod.Post, $"/api/casos/{numero}/mensajes",
            organizacionPropia, propietarioPropio, new { cuerpo = "No debe agregarse." });

        consultar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        responder.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // RF-07/RNF-08: los identificadores de administración son globales y las claves de otro consumidor
    // responden 404 en el portal autenticado.
    [Fact]
    public async Task RNF_08_IdsDeAdministracionYPortal_NoExponenRecursosInexistentes()
    {
        var organizacion = await NuevaOrganizacion();
        var api = await NuevaApi(organizacion);
        await Ejecutar($"UPDATE api SET estado = 'publicada' WHERE id = '{api}'");
        var subdominio = await Escalar<string>($"SELECT subdominio FROM api WHERE id = '{api}'");
        var consumidor = await NuevoConsumidor(organizacion);
        var administrador = await NuevoUsuario();
        var idInexistente = Guid.NewGuid();
        using var cliente = Fabrica.CreateClient();

        var otraOrganizacion = await NuevaOrganizacion();
        var otraApi = await NuevaApi(otraOrganizacion);
        await Ejecutar($"UPDATE api SET estado = 'publicada' WHERE id = '{otraApi}'");
        var otroConsumidor = await NuevoConsumidor(otraOrganizacion);
        var planOtro = await NuevoPlanApi(otraApi);
        var claveOtroPortal = await CrearClave(otraApi, otroConsumidor, planOtro);
        var rutasAdmin = new (HttpMethod Metodo, string Ruta, object? Cuerpo)[]
        {
            (HttpMethod.Put, $"/api/admin/planes-plataforma/{idInexistente}", new
            {
                nombre = "Plan de prueba",
                descripcion = "Plan válido para comprobar la búsqueda por identificador.",
                precio = 10m,
                vigenciaDias = 30,
                maxApis = 5,
                maxMiembros = 10,
                cuotaPeticiones = 1000L,
                dominioPropio = false,
            }),
            (HttpMethod.Post, $"/api/admin/planes-plataforma/{idInexistente}/desactivar", null),
            (HttpMethod.Post, $"/api/admin/cuentas/{idInexistente}/desactivar", null),
            (HttpMethod.Post, $"/api/admin/cuentas/{idInexistente}/activar", null),
            (HttpMethod.Post, $"/api/admin/organizaciones/{idInexistente}/suspender", new { motivo = "Prueba" }),
            (HttpMethod.Post, $"/api/admin/organizaciones/{idInexistente}/reactivar", null),
            (HttpMethod.Post, $"/api/admin/pagos/{idInexistente}/revertir", null),
            (HttpMethod.Get, "/api/admin/casos/2000000000", null),
            (HttpMethod.Post, "/api/admin/casos/2000000000/asignar", null),
            (HttpMethod.Post, "/api/admin/casos/2000000000/mensajes", new { cuerpo = "No debe guardarse." }),
            (HttpMethod.Post, "/api/admin/casos/2000000000/cerrar", null),
            (HttpMethod.Get, "/api/admin/casos/2000000000/organizacion", null),
        };
        foreach (var (metodo, ruta, cuerpo) in rutasAdmin)
        {
            using var respuesta = await Enviar(cliente, metodo, ruta, organizacion, administrador, cuerpo, "Administrador");
            respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound, $"{metodo} {ruta}");
        }

        using var claveAjenaRotar = await Enviar(cliente, HttpMethod.Post,
            $"/api/portal/claves/{claveOtroPortal}/rotar", organizacion, consumidor, null,
            "Consumidor", $"{subdominio}.shapi.localhost");
        using var claveAjenaRevocar = await Enviar(cliente, HttpMethod.Post,
            $"/api/portal/claves/{claveOtroPortal}/revocar", organizacion, consumidor, null,
            "Consumidor", $"{subdominio}.shapi.localhost");

        claveAjenaRotar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        claveAjenaRevocar.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // RF-07: los roles de proveedor, administración y soporte aplican la matriz de 04 §3 en HTTP.
    [Theory]
    [InlineData("Lector", "POST", "/api/apis", 403)]
    [InlineData("Editor", "POST", "/api/miembros/invitaciones", 403)]
    [InlineData("Soporte", "POST", "/api/admin/organizaciones/00000000-0000-0000-0000-000000000001/suspender", 403)]
    [InlineData("Administrador", "GET", "/api/apis", 403)]
    [InlineData("Soporte", "GET", "/api/apis", 403)]
    [InlineData("Consumidor", "GET", "/api/miembros", 403)]
    [InlineData("Propietario", "GET", "/api/apis", 200)]
    [InlineData("Editor", "GET", "/api/apis", 200)]
    [InlineData("Lector", "GET", "/api/apis", 200)]
    public async Task RF_07_RolesProveedorYAdministracion_AplicanPermisosEnLosEndpoints(string rol, string metodo, string ruta, int estadoEsperado)
    {
        var organizacion = await NuevaOrganizacion();
        var usuario = await NuevoUsuario();
        if (rol is "Propietario" or "Editor" or "Lector")
        {
            await NuevaMembresia(usuario, organizacion, rol.ToLowerInvariant());
        }
        await PrepararSuscripcionPlataforma(organizacion);
        using var cliente = Fabrica.CreateClient();

        using var respuesta = await Enviar(cliente, new HttpMethod(metodo), ruta, organizacion, usuario,
            metodo == "POST" ? new { } : null, rol);

        respuesta.StatusCode.Should().Be((HttpStatusCode)estadoEsperado);
    }

    private static Task<HttpResponseMessage> Enviar(
        HttpClient cliente,
        HttpMethod metodo,
        string ruta,
        Guid organizacion,
        Guid usuario,
        object? cuerpo,
        string rol = "Propietario",
        string? host = null)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        solicitud.Headers.Add("X-Requested-With", "shapi");
        solicitud.Headers.Add("X-Prueba-Organizacion", organizacion.ToString());
        solicitud.Headers.Add("X-Prueba-Usuario", usuario.ToString());
        solicitud.Headers.Add("X-Prueba-Rol", rol);
        if (host is not null)
        {
            solicitud.Headers.Host = host;
        }
        if (cuerpo is not null)
        {
            if (ruta.EndsWith("/especificacion", StringComparison.Ordinal) || ruta.EndsWith("/portal/logo", StringComparison.Ordinal))
            {
                var formulario = new MultipartFormDataContent();
                formulario.Add(new ByteArrayContent("archivo de prueba"u8.ToArray()), "archivo", "prueba.txt");
                solicitud.Content = formulario;
            }
            else
            {
                solicitud.Content = JsonContent.Create(cuerpo);
            }
        }

        return cliente.SendAsync(solicitud);
    }

    private static object CuerpoPara(string ruta)
    {
        if (ruta.EndsWith("/rutas/exposicion", StringComparison.Ordinal)
            || ruta.EndsWith("/configuracion-rutas", StringComparison.Ordinal))
        {
            return Array.Empty<object>();
        }

        return ruta.EndsWith("/invitaciones", StringComparison.Ordinal)
            ? new { correo = "aislamiento@ejemplo.com" }
            : new { };
    }

    private async Task PrepararSuscripcionPlataforma(Guid organizacion)
    {
        var plan = await NuevoPlanPlataforma();
        await Ejecutar($"UPDATE plan_plataforma SET max_apis = 5, max_miembros = 10 WHERE id = '{plan}'");
        await NuevaSuscripcionPlataforma(organizacion, plan);
    }

    private Task<Guid> CrearClave(Guid api, Guid consumidor, Guid plan) => Escalar<Guid>($"""
        WITH suscripcion AS (
            INSERT INTO suscripcion_api (id, consumidor_id, api_id, plan_id, estado, inicio, fin)
            VALUES (gen_random_uuid(), '{consumidor}', '{api}', '{plan}', 'activa', '{Reloj.Ahora.AddDays(-1):O}', '{Reloj.Ahora.AddDays(29):O}')
            RETURNING id
        )
        INSERT INTO clave (id, suscripcion_id, tipo, prefijo, ultimos4, hash_sha256, estado)
        SELECT gen_random_uuid(), suscripcion.id, 'produccion', 'shp_prod_', '1234', repeat('a', 64), 'activa'
        FROM suscripcion
        RETURNING id
        """);

    private sealed class AutenticacionPrueba(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Esquema = "AislamientoOrganizacionesPrueba";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Prueba-Organizacion", out var organizacion))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            Claim[] claims =
            [
                new(ClaimTypes.NameIdentifier, Request.Headers["X-Prueba-Usuario"].FirstOrDefault() ?? Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, "Usuario de prueba"),
                new(ClaimTypes.Role, Request.Headers["X-Prueba-Rol"].FirstOrDefault() ?? "Propietario"),
                new("OrganizacionId", organizacion.ToString()),
                new("Ambito", Request.Headers["X-Prueba-Rol"].FirstOrDefault() == "Consumidor" ? "Consumidor" : "Personal"),
            ];
            var identidad = new ClaimsIdentity(claims, Esquema);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema)));
        }
    }
}
