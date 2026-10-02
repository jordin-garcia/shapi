using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shapi.Api.Identidad;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Dominio.Suscripciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;
using Xunit;

namespace Shapi.Api.Tests.Identidad;

public class RelojFalso : IReloj
{
    public DateTimeOffset Ahora { get; set; } = new(2026, 8, 24, 21, 30, 0, TimeSpan.Zero); // 15:30 en Guatemala

    public void Avanzar(TimeSpan tiempo) => Ahora += tiempo;
}

/// <summary>Un solo contenedor de PostgreSQL para toda la clase; cada prueba usa su propia base de datos.</summary>
public sealed class ContenedorPostgres : IAsyncLifetime
{
    public PostgreSqlContainer Contenedor { get; } = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => Contenedor.StartAsync();

    public Task DisposeAsync() => Contenedor.DisposeAsync().AsTask();
}

public class AutenticacionTests(ContenedorPostgres postgres) : IClassFixture<ContenedorPostgres>, IAsyncLifetime
{
    private const string ContrasenaValida = "ContraValida123";
    private const string CorreoAdmin = "admin@shapi.test";

    private readonly RelojFalso _reloj = new();
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;

    public Task InitializeAsync()
    {
        var cadena = new NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString())
        {
            Database = $"prueba_{Guid.NewGuid():N}",
        }.ConnectionString;

        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            builder.UseSetting("SHAPI_POSTGRES_CADENA", cadena);
            builder.UseSetting("SHAPI_ADMIN_CORREO", CorreoAdmin);
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Admin");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", "SuperSecreto123!");
            builder.ConfigureTestServices(services => services.AddSingleton<IReloj>(_reloj));
        });
        // Las cookies se manejan a mano: la cookie es Secure y el servidor de prueba usa http.
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        NpgsqlConnection.ClearAllPools(); // cada prueba usa otra base: que no se acumulen conexiones inactivas
    }

    // ---------- RF-01 · Registro ----------

    [Fact]
    public async Task RF_01_Registro_CreaUsuarioOrganizacionMembresiaPruebaYCorreoDeVerificacion()
    {
        var respuesta = await Registrar("Ana.Lopez@EnviosXelaju.com", organizacion: "Envíos Xelajú, S.A.");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await using var db = Db(out var scope);
        using var _ = scope;
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Correo == "ana.lopez@enviosxelaju.com");
        Assert.Null(usuario.CorreoVerificadoEn);
        Assert.NotEqual(ContrasenaValida, usuario.HashContrasena);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<Usuario>().VerifyHashedPassword(usuario, usuario.HashContrasena!, ContrasenaValida));

        var membresia = await db.Set<Membresia>().IgnoreQueryFilters().SingleAsync(m => m.UsuarioId == usuario.Id);
        Assert.Equal(Rol.Propietario, membresia.Rol);
        var organizacion = await db.Set<Organizacion>().SingleAsync(o => o.Id == membresia.OrganizacionId);
        Assert.Equal(TipoOrganizacion.Proveedor, organizacion.Tipo);
        Assert.Equal("Envíos Xelajú, S.A.", organizacion.Nombre);

        // 09 §4: inicio redondeado al inicio del día en Guatemala (UTC−6) y fin = inicio + vigencia_dias.
        var prueba = await db.Set<PlanPlataforma>().SingleAsync(p => p.EsPrueba);
        var suscripcion = await db.Set<SuscripcionPlataforma>().IgnoreQueryFilters().SingleAsync(s => s.OrganizacionId == organizacion.Id);
        Assert.Equal(EstadoSuscripcion.Activa, suscripcion.Estado);
        Assert.Equal(prueba.Id, suscripcion.PlanId);
        Assert.Equal(new DateTimeOffset(2026, 8, 24, 6, 0, 0, TimeSpan.Zero), suscripcion.Inicio);
        Assert.Equal(suscripcion.Inicio.AddDays(prueba.VigenciaDias), suscripcion.Fin);
        Assert.Equal(new DateTimeOffset(2026, 9, 23, 6, 0, 0, TimeSpan.Zero), suscripcion.Fin);

        // Token de 24 h: la base solo guarda el SHA-256 en hex; el valor claro solo viaja en el correo encolado.
        var token = await db.Set<Token>().SingleAsync(t => t.UsuarioId == usuario.Id);
        Assert.Equal(TipoToken.VerificacionCorreo, token.Tipo);
        Assert.Equal(_reloj.Ahora.AddHours(24), token.ExpiraEn);
        Assert.Matches("^[0-9a-f]{64}$", token.HashToken);
        var correo = await db.Set<CorreoSaliente>().SingleAsync(c => c.Destinatario == usuario.Correo);
        Assert.Equal("verificacion_correo", correo.Plantilla);
        Assert.Equal(EstadoCorreo.Pendiente, correo.Estado);
        var valorToken = JsonDocument.Parse(correo.Datos).RootElement.GetProperty("token").GetString()!;
        Assert.NotEqual(token.HashToken, valorToken);
        Assert.Equal(token.HashToken, SeguridadTokens.HashearToken(valorToken));
    }

    [Fact]
    public async Task RF_01_Registro_CorreoExistente_Responde409SinCrearOtraCuenta()
    {
        await Registrar("ana@enviosxelaju.com");

        var respuesta = await Registrar("  ANA@EnviosXelaju.com ");

        await AfirmarProblema(respuesta, HttpStatusCode.Conflict, "correo_ya_registrado");
        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.Equal(1, await db.Set<Usuario>().IgnoreQueryFilters().CountAsync(u => u.Correo == "ana@enviosxelaju.com"));
    }

    [Theory]
    [InlineData("Ana", "ana@enviosxelaju.com", "Envíos Xelajú", "123456789", "contrasena")]
    [InlineData("Ana", "ana.lopez@enviosxelaju.com", "Envíos Xelajú", "ANA.LOPEZ@ENVIOSXELAJU.COM", "contrasena")]
    [InlineData("Ana", "ana@enviosxelaju.com", "Envíos Xelajú", null, "contrasena")]
    [InlineData("Ana", "ana@enviosxelaju.com", "E", ContrasenaValida, "organizacion")]
    [InlineData("Ana", "no-es-un-correo", "Envíos Xelajú", ContrasenaValida, "correo")]
    [InlineData("Ana", "a@b", "Envíos Xelajú", ContrasenaValida, "correo")]
    [InlineData("Ana", "ana lopez@enviosxelaju.com", "Envíos Xelajú", ContrasenaValida, "correo")]
    [InlineData("", "ana@enviosxelaju.com", "Envíos Xelajú", ContrasenaValida, "nombre")]
    public async Task RF_01_Registro_DatosInvalidos_Responde400ConErroresPorCampo(string nombre, string correo, string organizacion, string? contrasena, string campo)
    {
        contrasena ??= new string('a', 129); // más de 128 caracteres

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/registro", new { nombre, correo, organizacion, contrasena });

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty(campo, out var mensajes), $"Falta el campo {campo}");
        Assert.NotEmpty(mensajes.EnumerateArray());
        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.False(await db.Set<Usuario>().IgnoreQueryFilters().AnyAsync(u => u.Correo != CorreoAdmin));
    }

    // ---------- RF-02 · Verificación de correo ----------

    [Fact]
    public async Task RF_02_VerificarCorreo_MarcaElCorreoYElTokenEIniciaSesion()
    {
        await Registrar("ana@enviosxelaju.com");
        var token = await TokenDelUltimoCorreo("ana@enviosxelaju.com");
        var registro = _reloj.Ahora;
        _reloj.Avanzar(TimeSpan.FromMinutes(5));

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/verificar-correo", new { token });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cookie = CookieDeSesion(respuesta);
        var sesion = await LeerSesion(cookie);
        Assert.True(sesion.GetProperty("correoVerificado").GetBoolean());
        await using var db = Db(out var scope);
        using var _ = scope;
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Correo == "ana@enviosxelaju.com");
        Assert.Equal(_reloj.Ahora, usuario.CorreoVerificadoEn);
        var tokenUsado = await db.Set<Token>().SingleAsync(t => t.UsuarioId == usuario.Id);
        Assert.Equal(_reloj.Ahora, tokenUsado.UsadoEn);

        // 07 §3: creado_en y actualizado_en con la hora de IReloj, también al marcar el token con ExecuteUpdate.
        Assert.Equal(registro, usuario.CreadoEn);
        Assert.Equal(_reloj.Ahora, usuario.ActualizadoEn);
        Assert.Equal(registro, tokenUsado.CreadoEn);
        Assert.Equal(_reloj.Ahora, tokenUsado.ActualizadoEn);
    }

    [Fact]
    public async Task RF_02_VerificarCorreo_TokenYaUsado_Responde422()
    {
        await Registrar("ana@enviosxelaju.com");
        var token = await TokenDelUltimoCorreo("ana@enviosxelaju.com");
        await Enviar(HttpMethod.Post, "/api/auth/verificar-correo", new { token });

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/verificar-correo", new { token });

        await AfirmarProblema(respuesta, (HttpStatusCode)422, "token_invalido");
    }

    [Fact]
    public async Task RF_02_VerificarCorreo_TokenVencidoALas24Horas_Responde422()
    {
        await Registrar("ana@enviosxelaju.com");
        var token = await TokenDelUltimoCorreo("ana@enviosxelaju.com");
        _reloj.Avanzar(TimeSpan.FromHours(24));

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/verificar-correo", new { token });

        await AfirmarProblema(respuesta, (HttpStatusCode)422, "token_invalido");
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"token\":\"\"}")]
    [InlineData("{\"token\":\"no-existe\"}")]
    public async Task RF_02_VerificarCorreo_TokenAusenteOInexistente_Responde422(string cuerpo)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/auth/verificar-correo")
        {
            Content = new StringContent(cuerpo, Encoding.UTF8, "application/json"),
        };
        peticion.Headers.Add("X-Requested-With", "shapi");

        var respuesta = await _cliente.SendAsync(peticion);

        await AfirmarProblema(respuesta, (HttpStatusCode)422, "token_invalido");
    }

    [Fact]
    public async Task RF_02_ReenviarVerificacion_GeneraUnTokenNuevoYRespondeIgualSiNoExisteLaCuenta()
    {
        await Registrar("ana@enviosxelaju.com");

        var reenvio = await Enviar(HttpMethod.Post, "/api/auth/reenviar-verificacion", new { correo = "ANA@enviosxelaju.com" });
        var inexistente = await Enviar(HttpMethod.Post, "/api/auth/reenviar-verificacion", new { correo = "nadie@enviosxelaju.com" });

        Assert.Equal(HttpStatusCode.OK, reenvio.StatusCode);
        Assert.Equal(HttpStatusCode.OK, inexistente.StatusCode);
        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.Equal(2, await db.Set<Token>().CountAsync(t => t.Correo == "ana@enviosxelaju.com"));
        Assert.Equal(2, await db.Set<CorreoSaliente>().CountAsync(c => c.Destinatario == "ana@enviosxelaju.com"));
        Assert.False(await db.Set<CorreoSaliente>().AnyAsync(c => c.Destinatario == "nadie@enviosxelaju.com"));
    }

    // ---------- RF-04 · Inicio y cierre de sesión ----------

    [Fact]
    public async Task RF_04_Entrar_CreaSesionConHashYCookieHttpOnlySecureLax()
    {
        await Registrar("ana@enviosxelaju.com");

        var respuesta = await Entrar("ana@enviosxelaju.com", ContrasenaValida);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var encabezado = respuesta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("shapi_sesion=", StringComparison.Ordinal));
        var atributos = encabezado.Split(';', StringSplitOptions.TrimEntries).Select(a => a.ToLowerInvariant()).ToArray();
        Assert.Contains("httponly", atributos);
        Assert.Contains("secure", atributos);
        Assert.Contains("samesite=lax", atributos);
        Assert.Contains("path=/", atributos);

        var valor = CookieDeSesion(respuesta)["shapi_sesion=".Length..];
        await using var db = Db(out var scope);
        using var _ = scope;
        var sesion = await db.Set<Sesion>().IgnoreQueryFilters().SingleAsync(s => s.HashIdentificador == SeguridadTokens.HashearToken(valor));
        Assert.Equal(AmbitoSesion.Personal, sesion.Ambito);
        Assert.Equal(_reloj.Ahora.AddDays(7), sesion.ExpiraEn);
    }

    [Fact]
    public async Task RF_04_Entrar_TrasCincoIntentosFallidosBloqueaLaCuenta15Minutos()
    {
        await Registrar("ana@enviosxelaju.com");
        for (var i = 0; i < 5; i++)
        {
            await AfirmarProblema(await Entrar("ana@enviosxelaju.com", "Incorrecta123"), HttpStatusCode.Unauthorized, "credenciales_invalidas");
        }

        await AfirmarProblema(await Entrar("ana@enviosxelaju.com", ContrasenaValida), HttpStatusCode.Locked, "cuenta_bloqueada");
        _reloj.Avanzar(TimeSpan.FromMinutes(14));
        await AfirmarProblema(await Entrar("ana@enviosxelaju.com", ContrasenaValida), HttpStatusCode.Locked, "cuenta_bloqueada");
        _reloj.Avanzar(TimeSpan.FromMinutes(1));
        Assert.Equal(HttpStatusCode.OK, (await Entrar("ana@enviosxelaju.com", ContrasenaValida)).StatusCode);
    }

    [Fact]
    public async Task RF_04_Entrar_CorreoInexistenteYContrasenaIncorrecta_DanElMismoErrorGenerico()
    {
        await Registrar("ana@enviosxelaju.com");

        var inexistente = await AfirmarProblema(await Entrar("nadie@enviosxelaju.com", ContrasenaValida), HttpStatusCode.Unauthorized, "credenciales_invalidas");
        var incorrecta = await AfirmarProblema(await Entrar("ana@enviosxelaju.com", "Incorrecta123"), HttpStatusCode.Unauthorized, "credenciales_invalidas");

        Assert.Equal(inexistente.GetProperty("title").GetString(), incorrecta.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CU_02_2b_Entrar_CuentaDesactivada_SoloSeDiceConLaContrasenaCorrecta()
    {
        await Registrar("ana@enviosxelaju.com");
        await using (var db = Db(out var scope))
        {
            using var _ = scope;
            await db.Set<Usuario>().IgnoreQueryFilters().Where(u => u.Correo == "ana@enviosxelaju.com")
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, EstadoCuenta.Desactivado));
        }

        await AfirmarProblema(await Entrar("ana@enviosxelaju.com", "Incorrecta123"), HttpStatusCode.Unauthorized, "credenciales_invalidas");
        await AfirmarProblema(await Entrar("ana@enviosxelaju.com", ContrasenaValida), HttpStatusCode.Forbidden, "cuenta_desactivada");
    }

    [Fact]
    public async Task RF_04_Salir_RevocaLaSesionYLaCookieDejaDeServir()
    {
        await Registrar("ana@enviosxelaju.com");
        var cookie = CookieDeSesion(await Entrar("ana@enviosxelaju.com", ContrasenaValida));
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie)).StatusCode);

        var salir = await Enviar(HttpMethod.Post, "/api/auth/salir", cookie: cookie);

        Assert.Equal(HttpStatusCode.OK, salir.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie)).StatusCode);
        await using var db = Db(out var scope);
        using var _ = scope;
        var hash = SeguridadTokens.HashearToken(cookie["shapi_sesion=".Length..]);
        Assert.NotNull((await db.Set<Sesion>().IgnoreQueryFilters().SingleAsync(s => s.HashIdentificador == hash)).RevocadaEn);
    }

    [Fact]
    public async Task RF_04_Sesion_VenceTras8HorasDeInactividad()
    {
        await Registrar("ana@enviosxelaju.com");
        var cookie = CookieDeSesion(await Entrar("ana@enviosxelaju.com", ContrasenaValida));

        _reloj.Avanzar(TimeSpan.FromHours(7));
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie)).StatusCode);
        _reloj.Avanzar(TimeSpan.FromHours(7)); // 14 h desde el inicio, 7 h desde el último uso
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie)).StatusCode);
        _reloj.Avanzar(TimeSpan.FromHours(8));

        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie)).StatusCode);
    }

    [Fact]
    public async Task RF_04_Sesion_VenceALos7DiasAunqueSigaEnUso()
    {
        await Registrar("ana@enviosxelaju.com");
        var cookie = CookieDeSesion(await Entrar("ana@enviosxelaju.com", ContrasenaValida));

        for (var i = 0; i < 23; i++) // 161 h, siempre con menos de 8 h entre usos
        {
            _reloj.Avanzar(TimeSpan.FromHours(7));
            Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie)).StatusCode);
        }
        _reloj.Avanzar(TimeSpan.FromHours(7)); // 168 h = 7 días

        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie)).StatusCode);
    }

    // ---------- RF-04 y 10 §1 · Datos de la sesión y destino por rol ----------

    [Theory]
    [InlineData(Rol.Propietario, "propietario", "/panel/apis")]
    [InlineData(Rol.Editor, "editor", "/panel/apis")]
    [InlineData(Rol.Lector, "lector", "/panel/apis")]
    [InlineData(Rol.Administrador, "administrador", "/admin/organizaciones")]
    [InlineData(Rol.Soporte, "soporte", "/admin/casos")]
    public async Task RF_04_Sesion_DevuelveUsuarioOrganizacionRolYDestino(Rol rol, string rolEsperado, string destino)
    {
        var (correo, organizacion) = await CrearMiembro(rol);
        var cookie = CookieDeSesion(await Entrar(correo, ContrasenaValida));

        var sesion = await LeerSesion(cookie);

        Assert.Equal(rolEsperado, sesion.GetProperty("rol").GetString());
        Assert.Equal(destino, sesion.GetProperty("destino").GetString());
        Assert.Equal(correo, sesion.GetProperty("usuario").GetProperty("correo").GetString());
        Assert.Equal(organizacion.Id, sesion.GetProperty("organizacion").GetProperty("id").GetGuid());
        Assert.Equal(organizacion.Nombre, sesion.GetProperty("organizacion").GetProperty("nombre").GetString());
        Assert.False(sesion.GetProperty("correoVerificado").GetBoolean());
    }

    [Fact]
    public async Task RNF_08_ContextoDeLaSesion_ElFiltroGlobalSoloMuestraLaOrganizacionDelUsuario()
    {
        var (_, propia) = await CrearMiembro(Rol.Propietario);
        var (_, ajena) = await CrearMiembro(Rol.Editor);
        using var scope = _fabrica.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(PoliticasAutorizacion.ClaimOrganizacion, propia.Id.ToString())], "Personal")),
        };
        var db = scope.ServiceProvider.GetRequiredService<ShapiDbContext>();

        var visibles = await db.Set<Membresia>().Select(m => m.OrganizacionId).Distinct().ToListAsync();

        Assert.Equal([propia.Id], visibles);
        Assert.NotEqual(propia.Id, ajena.Id);
    }

    [Fact]
    public async Task RF_04_Sesion_SinCookie_Responde401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/sesion")).StatusCode);
    }

    // ---------- RF-04 y 10 §1 · CSRF ----------

    [Fact]
    public async Task RF_04_Csrf_SinXRequestedWith_Responde403Csrf()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/auth/entrar") { Content = JsonContent.Create(new { correo = "a@b.com", contrasena = "x" }) };

        await AfirmarProblema(await _cliente.SendAsync(peticion), HttpStatusCode.Forbidden, "csrf");
    }

    [Fact]
    public async Task RF_04_Csrf_OriginDeOtroHost_Responde403Csrf_YDelMismoHostPasa()
    {
        var ajeno = Peticion(HttpMethod.Post, "/api/auth/entrar", new { correo = "a@b.com", contrasena = "x" });
        ajeno.Headers.Add("Origin", "https://sitio-malicioso.example");
        var propio = Peticion(HttpMethod.Post, "/api/auth/entrar", new { correo = "a@b.com", contrasena = "x" });
        propio.Headers.Add("Origin", "http://localhost");

        await AfirmarProblema(await _cliente.SendAsync(ajeno), HttpStatusCode.Forbidden, "csrf");
        await AfirmarProblema(await _cliente.SendAsync(propio), HttpStatusCode.Unauthorized, "credenciales_invalidas");
    }

    // ---------- RF-04 y 10 §1 · Limitación de peticiones ----------

    [Fact]
    public async Task RF_04_Limite_LaPeticion11EnUnMinuto_Responde429_YLaSesionNoSeLimita()
    {
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await Entrar("nadie@enviosxelaju.com", "Incorrecta123")).StatusCode);
        }

        var respuesta = await Entrar("nadie@enviosxelaju.com", "Incorrecta123");

        await AfirmarProblema(respuesta, HttpStatusCode.TooManyRequests, "demasiadas_peticiones");
        Assert.True(respuesta.Headers.Contains("Retry-After"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/sesion")).StatusCode);
    }

    // 10 §1: registro, verificación, reenvío y entrada llevan el límite de 10 por minuto por IP.
    [Theory]
    [InlineData("/api/auth/registro")]
    [InlineData("/api/auth/verificar-correo")]
    [InlineData("/api/auth/reenviar-verificacion")]
    [InlineData("/api/auth/entrar")]
    public async Task RF_04_Limite_CadaEndpointDeAutenticacion_LaPeticion11Responde429(string ruta)
    {
        var cuerpo = new { correo = "nadie@enviosxelaju.com", contrasena = "Incorrecta123", token = "inexistente" };
        for (var i = 0; i < 10; i++)
        {
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await Enviar(HttpMethod.Post, ruta, cuerpo)).StatusCode);
        }

        await AfirmarProblema(await Enviar(HttpMethod.Post, ruta, cuerpo), HttpStatusCode.TooManyRequests, "demasiadas_peticiones");
    }

    [Fact]
    public async Task RF_04_Limite_DetrasDelBorde_CuentaPorLaIpDelCliente()
    {
        await using var fabrica = Variante(("SHAPI_REDES_BORDE", "172.18.0.0/16"));
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(StatusCodes.Status401Unauthorized, (await EntrarDesdeElBorde(fabrica, "203.0.113.10")).Response.StatusCode);
        }

        Assert.Equal(StatusCodes.Status429TooManyRequests, (await EntrarDesdeElBorde(fabrica, "203.0.113.10")).Response.StatusCode);
        Assert.Equal(StatusCodes.Status401Unauthorized, (await EntrarDesdeElBorde(fabrica, "203.0.113.11")).Response.StatusCode);
    }

    [Fact]
    public async Task RF_04_Limite_DesdeUnaRedQueNoEsElBorde_IgnoraXForwardedFor()
    {
        // Sin SHAPI_REDES_BORDE solo se confía en la máquina: una red privada cualquiera no puede elegir su IP.
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(StatusCodes.Status401Unauthorized, (await EntrarDesdeElBorde(_fabrica, $"203.0.113.{i}")).Response.StatusCode);
        }

        Assert.Equal(StatusCodes.Status429TooManyRequests, (await EntrarDesdeElBorde(_fabrica, "203.0.113.99")).Response.StatusCode);
    }

    // ---------- 04 · Denegar por defecto ----------

    [Fact]
    public async Task Autorizacion_PoliticaPorDefecto_ExigeUnUsuarioAutenticado()
    {
        var proveedor = _fabrica.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        var politica = await proveedor.GetFallbackPolicyAsync();

        Assert.NotNull(politica);
        Assert.Contains(politica.Requirements, r => r is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public void Autorizacion_EndpointsAnonimos_SonSoloLosPublicos()
    {
        var anonimos = _fabrica.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(e => e.RoutePattern.RawText)
            .Order()
            .ToList();

        Assert.Equal(
            // /openapi solo se mapea en Development, que es el entorno de las pruebas.
            ["/api/auth/entrar", "/api/auth/recuperar", "/api/auth/reenviar-verificacion", "/api/auth/registro", "/api/auth/restablecer", "/api/auth/salir", "/api/auth/verificar-correo",
             "/api/portal/auth/entrar", "/api/portal/auth/invitacion/{token}", "/api/portal/auth/invitacion/{token}/aceptar", "/api/portal/auth/recuperar", "/api/portal/auth/reenviar-verificacion", "/api/portal/auth/registro", "/api/portal/auth/restablecer", "/api/portal/auth/salir", "/api/portal/auth/verificar-correo",
             "/api/portal/configuracion", "/api/portal/logo", "/api/portal/planes",
             "/openapi/{documentName}.json", "/salud"],
            anonimos);
    }

    // ---------- RF-04 · Intentos fallidos y tiempo de respuesta ----------

    [Fact]
    public async Task RF_04_Entrar_CincoIntentosFallidosEnParalelo_BloqueanLaCuenta()
    {
        await Registrar("ana@enviosxelaju.com");

        var intentos = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Entrar("ana@enviosxelaju.com", "Incorrecta123")));

        Assert.All(intentos, r => Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode));
        await AfirmarProblema(await Entrar("ana@enviosxelaju.com", ContrasenaValida), HttpStatusCode.Locked, "cuenta_bloqueada");
    }

    [Fact]
    public async Task RF_04_Entrar_CuatroFallosUnAciertoYOtroFallo_NoBloquean()
    {
        await Registrar("ana@enviosxelaju.com");
        for (var i = 0; i < 4; i++)
        {
            await Entrar("ana@enviosxelaju.com", "Incorrecta123");
        }

        Assert.Equal(HttpStatusCode.OK, (await Entrar("ana@enviosxelaju.com", ContrasenaValida)).StatusCode);
        await AfirmarProblema(await Entrar("ana@enviosxelaju.com", "Incorrecta123"), HttpStatusCode.Unauthorized, "credenciales_invalidas");

        Assert.Equal(HttpStatusCode.OK, (await Entrar("ana@enviosxelaju.com", ContrasenaValida)).StatusCode);
    }

    [Fact]
    public async Task RF_04_Entrar_CuentaInexistenteYContrasenaIncorrecta_HacenLasMismasConsultas()
    {
        // 10 §1: el mismo tiempo de respuesta. Se comprueba que las dos rutas hagan el mismo trabajo en la base.
        var contador = new ContadorComandos();
        await using var fabrica = _fabrica.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.ConfigureDbContext<ShapiDbContext>(o => o.AddInterceptors(contador))));
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await cliente.SendAsync(Peticion(HttpMethod.Post, "/api/auth/registro",
            new { nombre = "Ana López", correo = "ana@enviosxelaju.com", organizacion = "Envíos Xelajú", contrasena = ContrasenaValida }));

        contador.Comandos = 0;
        await cliente.SendAsync(Peticion(HttpMethod.Post, "/api/auth/entrar", new { correo = "nadie@enviosxelaju.com", contrasena = "Incorrecta123" }));
        var inexistente = contador.Comandos;
        contador.Comandos = 0;
        await cliente.SendAsync(Peticion(HttpMethod.Post, "/api/auth/entrar", new { correo = "ana@enviosxelaju.com", contrasena = "Incorrecta123" }));

        Assert.Equal(inexistente, contador.Comandos);
    }

    [Fact]
    public async Task RF_04_Entrar_CuentaSinContrasena_Responde401Generico()
    {
        await using (var db = Db(out var scope))
        {
            using var _ = scope;
            var organizacion = await db.Set<Organizacion>().SingleAsync(o => o.Tipo == TipoOrganizacion.Plataforma);
            var usuario = new Usuario("Sofía", "sofia@shapi.test"); // cuenta de plataforma que todavía no define su contraseña
            db.AddRange(usuario, new Membresia(usuario.Id, organizacion.Id, Rol.Soporte));
            await db.SaveChangesAsync();
        }

        await AfirmarProblema(await Entrar("sofia@shapi.test", ContrasenaValida), HttpStatusCode.Unauthorized, "credenciales_invalidas");
    }

    [Fact]
    public async Task RNF_07_Entrar_HashConMenosIteraciones_SeRecalculaAlEntrar()
    {
        var (correo, _) = await CrearMiembro(Rol.Editor);
        var hasherViejo = new PasswordHasher<Usuario>(Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions { IterationCount = 1_000 }));
        string hashViejo;
        await using (var db = Db(out var scope))
        {
            using var _ = scope;
            var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Correo == correo);
            hashViejo = hasherViejo.HashPassword(usuario, ContrasenaValida);
            usuario.DefinirHashContrasena(hashViejo);
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.OK, (await Entrar(correo, ContrasenaValida)).StatusCode);

        await using var lectura = Db(out var scopeLectura);
        using var __ = scopeLectura;
        var actualizado = await lectura.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Correo == correo);
        Assert.NotEqual(hashViejo, actualizado.HashContrasena);
        Assert.Equal(PasswordVerificationResult.Success, new PasswordHasher<Usuario>().VerifyHashedPassword(actualizado, actualizado.HashContrasena!, ContrasenaValida));
    }

    // ---------- RF-01 y RF-02 · Peticiones simultáneas y reenvíos ----------

    [Fact]
    public async Task RF_01_Registro_DosSimultaneosConElMismoCorreo_SoloUnoCreaLaCuenta()
    {
        var respuestas = await Task.WhenAll(Registrar("ana@enviosxelaju.com"), Registrar("ana@enviosxelaju.com"));

        Assert.Equal(
            new[] { HttpStatusCode.OK, HttpStatusCode.Conflict },
            respuestas.Select(r => r.StatusCode).Order().ToArray());
        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.Equal(1, await db.Set<Usuario>().IgnoreQueryFilters().CountAsync(u => u.Correo == "ana@enviosxelaju.com"));
    }

    [Fact]
    public async Task RF_02_VerificarCorreo_DosPeticionesSimultaneas_SoloUnaIniciaSesion()
    {
        await Registrar("ana@enviosxelaju.com");
        var token = await TokenDelUltimoCorreo("ana@enviosxelaju.com");

        var respuestas = await Task.WhenAll(
            Enviar(HttpMethod.Post, "/api/auth/verificar-correo", new { token }),
            Enviar(HttpMethod.Post, "/api/auth/verificar-correo", new { token }));

        Assert.Equal(
            new[] { HttpStatusCode.OK, (HttpStatusCode)422 },
            respuestas.Select(r => r.StatusCode).Order().ToArray());
        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.Equal(1, await db.Set<Sesion>().CountAsync());
    }

    [Fact]
    public async Task RF_02_ReenviarVerificacion_ElEnlaceReenviadoVerificaElCorreo()
    {
        await Registrar("ana@enviosxelaju.com");
        await Enviar(HttpMethod.Post, "/api/auth/reenviar-verificacion", new { correo = "ana@enviosxelaju.com" });
        var reenviado = await TokenDelUltimoCorreo("ana@enviosxelaju.com");

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/verificar-correo", new { token = reenviado });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.True((await LeerSesion(CookieDeSesion(respuesta))).GetProperty("correoVerificado").GetBoolean());
    }

    [Fact]
    public async Task RF_02_ReenviarVerificacion_MasDeTresPorHora_RespondeIgualSinEncolar()
    {
        await Registrar("ana@enviosxelaju.com");
        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Post, "/api/auth/reenviar-verificacion", new { correo = "ana@enviosxelaju.com" })).StatusCode);
        }

        var cuarto = await Enviar(HttpMethod.Post, "/api/auth/reenviar-verificacion", new { correo = "ana@enviosxelaju.com" });
        _reloj.Avanzar(TimeSpan.FromHours(1));
        await Enviar(HttpMethod.Post, "/api/auth/reenviar-verificacion", new { correo = "ana@enviosxelaju.com" });

        Assert.Equal(HttpStatusCode.OK, cuarto.StatusCode);
        await using var db = Db(out var scope);
        using var _ = scope;
        // El del registro, 3 reenvíos y, una hora después, otro más.
        Assert.Equal(5, await db.Set<CorreoSaliente>().CountAsync(c => c.Destinatario == "ana@enviosxelaju.com"));
    }

    [Fact]
    public async Task RF_02_ReenviarVerificacion_OchoSimultaneos_EncolanComoMaximoTres()
    {
        await Registrar("ana@enviosxelaju.com");

        // 1 registro + 8 reenvíos: no alcanza el límite de 10 por minuto por IP.
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            Enviar(HttpMethod.Post, "/api/auth/reenviar-verificacion", new { correo = "ana@enviosxelaju.com" })));

        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        await using var db = Db(out var scope);
        using var _ = scope;
        // El del registro y 3 reenvíos.
        Assert.Equal(4, await db.Set<CorreoSaliente>().CountAsync(c => c.Destinatario == "ana@enviosxelaju.com"));
    }

    [Fact]
    public async Task CU_02_2b_VerificarCorreo_CuentaDesactivada_Responde403SinIniciarSesion()
    {
        await Registrar("ana@enviosxelaju.com");
        var token = await TokenDelUltimoCorreo("ana@enviosxelaju.com");
        await using (var db = Db(out var scope))
        {
            using var _ = scope;
            await db.Set<Usuario>().IgnoreQueryFilters().Where(u => u.Correo == "ana@enviosxelaju.com")
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, EstadoCuenta.Desactivado));
        }

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/verificar-correo", new { token });

        await AfirmarProblema(respuesta, HttpStatusCode.Forbidden, "cuenta_desactivada");
        Assert.False(respuesta.Headers.Contains("Set-Cookie"));
        await using var db2 = Db(out var scope2);
        using var __ = scope2;
        Assert.Equal(0, await db2.Set<Sesion>().CountAsync());
    }

    // ---------- RF-04 · Sesión ----------

    [Fact]
    public async Task RF_04_Sesion_CuentaDesactivadaDespuesDeEntrar_Responde401()
    {
        await Registrar("ana@enviosxelaju.com");
        var cookie = CookieDeSesion(await Entrar("ana@enviosxelaju.com", ContrasenaValida));
        await using (var db = Db(out var scope))
        {
            using var _ = scope;
            await db.Set<Usuario>().IgnoreQueryFilters().Where(u => u.Correo == "ana@enviosxelaju.com")
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Estado, EstadoCuenta.Desactivado));
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie)).StatusCode);
    }

    [Fact]
    public async Task RF_04_Sesion_InactividadHorasConfigurable()
    {
        await using var fabrica = Variante(("Sesion:InactividadHoras", "2"));
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await cliente.SendAsync(Peticion(HttpMethod.Post, "/api/auth/registro",
            new { nombre = "Ana López", correo = "ana@enviosxelaju.com", organizacion = "Envíos Xelajú", contrasena = ContrasenaValida }));
        var cookie = CookieDeSesion(await cliente.SendAsync(Peticion(HttpMethod.Post, "/api/auth/entrar",
            new { correo = "ana@enviosxelaju.com", contrasena = ContrasenaValida })));

        _reloj.Avanzar(TimeSpan.FromMinutes(119));
        Assert.Equal(HttpStatusCode.OK, (await cliente.SendAsync(Peticion(HttpMethod.Get, "/api/auth/sesion", cookie: cookie))).StatusCode);
        _reloj.Avanzar(TimeSpan.FromHours(2));

        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.SendAsync(Peticion(HttpMethod.Get, "/api/auth/sesion", cookie: cookie))).StatusCode);
    }

    [Fact]
    public async Task RF_04_Sesion_UltimoUsoSeActualizaComoMaximoUnaVezPorMinuto()
    {
        await Registrar("ana@enviosxelaju.com");
        var inicio = _reloj.Ahora;
        var cookie = CookieDeSesion(await Entrar("ana@enviosxelaju.com", ContrasenaValida));
        var hash = SeguridadTokens.HashearToken(cookie["shapi_sesion=".Length..]);

        _reloj.Avanzar(TimeSpan.FromSeconds(59));
        await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie);
        Assert.Equal(inicio, await UltimoUso(hash));

        _reloj.Avanzar(TimeSpan.FromSeconds(1));
        await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie);
        Assert.Equal(inicio.AddMinutes(1), await UltimoUso(hash));
    }

    [Fact]
    public async Task RF_04_Salir_SinCookieOConLaSesionVencida_Responde200YBorraLaCookie()
    {
        await Registrar("ana@enviosxelaju.com");
        var cookie = CookieDeSesion(await Entrar("ana@enviosxelaju.com", ContrasenaValida));
        _reloj.Avanzar(TimeSpan.FromDays(8));

        var sinCookie = await Enviar(HttpMethod.Post, "/api/auth/salir");
        var vencida = await Enviar(HttpMethod.Post, "/api/auth/salir", cookie: cookie);

        foreach (var respuesta in new[] { sinCookie, vencida })
        {
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var borrado = respuesta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("shapi_sesion=", StringComparison.Ordinal));
            Assert.Contains("expires=Thu, 01 Jan 1970", borrado, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ---------- RF-04 y 10 §1 · CSRF: esquema y puerto del Origin ----------

    [Theory]
    [InlineData("https://localhost")]
    [InlineData("http://localhost:8080")]
    public async Task RF_04_Csrf_OriginConOtroEsquemaOPuerto_Responde403(string origen)
    {
        var peticion = Peticion(HttpMethod.Post, "/api/auth/entrar", new { correo = "a@b.com", contrasena = "x" });
        peticion.Headers.Add("Origin", origen);

        await AfirmarProblema(await _cliente.SendAsync(peticion), HttpStatusCode.Forbidden, "csrf");
    }

    [Theory]
    [InlineData("127.0.0.1")] // Caddy con Docker Desktop
    [InlineData("172.30.0.7")] // Caddy en la red shapi, en Linux
    public async Task RF_04_Csrf_DetrasDelBorde_ElOriginHttpsDelNavegadorPasa(string ipBorde)
    {
        var contexto = await EntrarDesdeElBorde(_fabrica, "203.0.113.10", ipBorde, "shapi.localhost", "https", "https://shapi.localhost");

        // Pasa el CSRF y llega a la comprobación de credenciales.
        Assert.Equal(StatusCodes.Status401Unauthorized, contexto.Response.StatusCode);
    }

    [Fact]
    public async Task RF_04_Csrf_DesdeOtraRedPrivada_NoSeConfiaEnXForwardedProto()
    {
        var contexto = await EntrarDesdeElBorde(_fabrica, "203.0.113.10", "172.18.0.5", "shapi.localhost", "https", "https://shapi.localhost");

        Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
    }

    [Fact]
    public void RF_04_RedesDelBorde_CidrMalEscrito_DetieneElArranque()
    {
        using var fabrica = Variante(("SHAPI_REDES_BORDE", "172.30.0.0/99"));

        Assert.ThrowsAny<Exception>(() => fabrica.CreateClient());
    }

    // ---------- Convenciones §5 · JSON mal formado ----------

    [Theory]
    [InlineData("/api/auth/registro")]
    [InlineData("/api/auth/entrar")]
    public async Task RF_04_Peticion_JsonMalFormado_Responde400DatosInvalidos(string url)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent("{\"correo\": ", Encoding.UTF8, "application/json") };
        peticion.Headers.Add("X-Requested-With", "shapi");

        await AfirmarProblema(await _cliente.SendAsync(peticion), HttpStatusCode.BadRequest, "datos_invalidos");
    }

    // ---------- Utilidades ----------

    private ShapiDbContext Db(out IServiceScope scope)
    {
        scope = _fabrica.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
    }

    private static HttpRequestMessage Peticion(HttpMethod metodo, string url, object? cuerpo = null, string? cookie = null)
    {
        var peticion = new HttpRequestMessage(metodo, url);
        peticion.Headers.Add("X-Requested-With", "shapi");
        if (cookie is not null)
        {
            peticion.Headers.Add("Cookie", cookie);
        }
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }
        return peticion;
    }

    private Task<HttpResponseMessage> Enviar(HttpMethod metodo, string url, object? cuerpo = null, string? cookie = null) =>
        _cliente.SendAsync(Peticion(metodo, url, cuerpo, cookie));

    private Task<HttpResponseMessage> Registrar(string correo, string contrasena = ContrasenaValida, string organizacion = "Envíos Xelajú") =>
        Enviar(HttpMethod.Post, "/api/auth/registro", new { nombre = "Ana López", correo, organizacion, contrasena });

    private Task<HttpResponseMessage> Entrar(string correo, string contrasena) =>
        Enviar(HttpMethod.Post, "/api/auth/entrar", new { correo, contrasena });

    /// <summary>La misma aplicación y la misma base, con otros ajustes de configuración.</summary>
    private WebApplicationFactory<Program> Variante(params (string Clave, string Valor)[] ajustes) =>
        _fabrica.WithWebHostBuilder(builder =>
        {
            foreach (var (clave, valor) in ajustes)
            {
                builder.UseSetting(clave, valor);
            }
        });

    private async Task<DateTimeOffset> UltimoUso(string hash)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        return (await db.Set<Sesion>().IgnoreQueryFilters().SingleAsync(s => s.HashIdentificador == hash)).UltimoUsoEn;
    }

    private sealed class ContadorComandos : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        private int _comandos;

        public int Comandos { get => _comandos; set => _comandos = value; }

        public override System.Data.Common.DbCommand CommandInitialized(
            Microsoft.EntityFrameworkCore.Diagnostics.CommandEndEventData eventData, System.Data.Common.DbCommand result)
        {
            Interlocked.Increment(ref _comandos);
            return result;
        }
    }

    /// <summary>Un POST a <c>entrar</c> como lo manda Caddy: por http, desde <paramref name="ipBorde"/>, con las cabeceras X-Forwarded-*.</summary>
    private static Task<HttpContext> EntrarDesdeElBorde(
        WebApplicationFactory<Program> fabrica, string ipCliente, string ipBorde = "172.18.0.5", string host = "localhost",
        string? esquemaOriginal = null, string? origen = null) =>
        fabrica.Server.SendAsync(contexto =>
        {
            contexto.Connection.RemoteIpAddress = IPAddress.Parse(ipBorde);
            contexto.Request.Method = HttpMethods.Post;
            contexto.Request.Scheme = "http";
            contexto.Request.Host = new HostString(host);
            contexto.Request.Path = "/api/auth/entrar";
            contexto.Request.Headers["X-Requested-With"] = "shapi";
            contexto.Request.Headers["X-Forwarded-For"] = ipCliente;
            if (esquemaOriginal is not null)
            {
                contexto.Request.Headers["X-Forwarded-Proto"] = esquemaOriginal;
            }
            if (origen is not null)
            {
                contexto.Request.Headers.Origin = origen;
            }
            var cuerpo = Encoding.UTF8.GetBytes("{\"correo\":\"nadie@enviosxelaju.com\",\"contrasena\":\"Incorrecta123\"}");
            contexto.Request.ContentType = "application/json";
            contexto.Request.ContentLength = cuerpo.Length;
            contexto.Request.Body = new MemoryStream(cuerpo);
            contexto.Features.Set<IHttpRequestBodyDetectionFeature>(new ConCuerpo());
        });

    private sealed class ConCuerpo : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }

    private static string CookieDeSesion(HttpResponseMessage respuesta) =>
        respuesta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("shapi_sesion=", StringComparison.Ordinal)).Split(';')[0];

    private async Task<JsonElement> LeerSesion(string cookie)
    {
        var respuesta = await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookie);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<string> TokenDelUltimoCorreo(string destinatario)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var correos = await db.Set<CorreoSaliente>().Where(c => c.Destinatario == destinatario).ToListAsync();
        return JsonDocument.Parse(correos[^1].Datos).RootElement.GetProperty("token").GetString()!;
    }

    private static async Task<JsonElement> AfirmarProblema(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(codigo, problema.GetProperty("codigo").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problema.GetProperty("title").GetString()));
        return problema;
    }

    /// <summary>Crea un usuario con ese rol: en la organización de plataforma si es administrador o soporte, o en una organización proveedora nueva.</summary>
    private async Task<(string Correo, Organizacion Organizacion)> CrearMiembro(Rol rol)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var correo = $"{rol.ToString().ToLowerInvariant()}@miembro.test";
        var usuario = new Usuario($"Persona {rol}", correo);
        usuario.DefinirHashContrasena(new PasswordHasher<Usuario>().HashPassword(usuario, ContrasenaValida));
        var organizacion = rol is Rol.Administrador or Rol.Soporte
            ? await db.Set<Organizacion>().SingleAsync(o => o.Tipo == TipoOrganizacion.Plataforma)
            : new Organizacion($"Organización de {rol}", TipoOrganizacion.Proveedor);
        if (db.Entry(organizacion).State == EntityState.Detached)
        {
            db.Add(organizacion);
        }
        db.AddRange(usuario, new Membresia(usuario.Id, organizacion.Id, rol));
        await db.SaveChangesAsync();
        return (correo, organizacion);
    }
}
