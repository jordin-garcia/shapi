using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;
using Xunit;

namespace Shapi.Api.Tests.Identidad;

/// <summary>Pruebas de integración de recuperación de contraseña y perfil (EM-04).</summary>
public class RecuperacionTests(ContenedorPostgres postgres) : IClassFixture<ContenedorPostgres>, IAsyncLifetime
{
    private const string ContrasenaValida = "ContraValida123";
    private const string CorreoAna = "ana@enviosxelaju.com";

    private readonly RelojFalso _reloj = new();
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;
    private string _cadena = null!;

    public Task InitializeAsync()
    {
        _cadena = new NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString())
        {
            Database = $"prueba_{Guid.NewGuid():N}",
        }.ConnectionString;

        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            builder.UseSetting("SHAPI_POSTGRES_CADENA", _cadena);
            builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Admin");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", "SuperSecreto123!");
            builder.ConfigureTestServices(services => services.AddSingleton<IReloj>(_reloj));
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await PostgresCompartido.EliminarBaseAsync(_cadena);
    }

    // ---------- RF-03 · Recuperar contraseña ----------

    [Fact]
    public async Task RF_03_Recuperar_CorreoExistente_Responde200YEncola()
    {
        await RegistrarYVerificar(CorreoAna);

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await using var db = Db(out var scope);
        using var _ = scope;
        var token = await db.Set<Token>().IgnoreQueryFilters()
            .SingleAsync(t => t.Tipo == TipoToken.Recuperacion);
        Assert.Equal(_reloj.Ahora.AddHours(1), token.ExpiraEn); // 60 minutos
        Assert.Matches("^[0-9a-f]{64}$", token.HashToken);
        var correo = await db.Set<CorreoSaliente>().SingleAsync(c => c.Destinatario == CorreoAna && c.Plantilla == "recuperacion");
        Assert.Equal(EstadoCorreo.Pendiente, correo.Estado);
    }

    [Fact]
    public async Task RF_03_Recuperar_CorreoInexistente_Responde200SinEncolar()
    {
        // La respuesta debe ser idéntica, para no revelar si el correo existe (10 §1)
        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = "noexiste@test.com" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.False(await db.Set<Token>().IgnoreQueryFilters().AnyAsync(t => t.Tipo == TipoToken.Recuperacion));
        Assert.False(await db.Set<CorreoSaliente>().AnyAsync(c => c.Plantilla == "recuperacion"));
    }

    [Fact]
    public async Task RF_03_Recuperar_ExistaONoLaCuenta_RespuestaIdentica()
    {
        // 10 §1 y §8 (auditoría 2026-10-03, H-20): mismo código, mismo cuerpo y mismas cabeceras.
        await RegistrarYVerificar(CorreoAna);

        var existente = await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna });
        var inexistente = await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = "noexiste@enviosxelaju.com" });

        Assert.Equal(existente.StatusCode, inexistente.StatusCode);
        Assert.Equal(await existente.Content.ReadAsStringAsync(), await inexistente.Content.ReadAsStringAsync());
        Assert.Equal(existente.Content.Headers.ContentType?.ToString(), inexistente.Content.Headers.ContentType?.ToString());
        Assert.Equal(NombresCabeceras(existente), NombresCabeceras(inexistente));
    }

    [Fact]
    public async Task RF_03_Recuperar_ExistaONoLaCuenta_HacenLasMismasConsultas()
    {
        // 10 §8 (auditoría 2026-10-03, H-15): el mismo tiempo de respuesta. Se comprueba que las dos rutas hagan el
        // mismo trabajo en la base, como en entrar (H-51).
        await RegistrarYVerificar(CorreoAna);
        var contador = new ContadorComandos();
        await using var fabrica = _fabrica.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.ConfigureDbContext<ShapiDbContext>(o => o.AddInterceptors(contador))));
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        contador.Comandos = 0;
        await cliente.SendAsync(Peticion(HttpMethod.Post, "/api/auth/recuperar", new { correo = "noexiste@enviosxelaju.com" }));
        var inexistente = contador.Comandos;
        contador.Comandos = 0;
        await cliente.SendAsync(Peticion(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna }));

        Assert.Equal(inexistente, contador.Comandos);
        Assert.True(contador.Comandos > 1);
    }

    [Fact]
    public async Task RF_03_Recuperar_MasDeTresEnUnaHora_NoEncolaMasYRespondeIgual()
    {
        // 10 §1 (auditoría 2026-10-03, decisión del paso 3): como máximo 3 solicitudes por hora por cuenta.
        await RegistrarYVerificar(CorreoAna);
        for (var i = 0; i < 4; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna })).StatusCode);
        }

        Assert.Equal(3, await CorreosDeRecuperacion());
        _reloj.Avanzar(TimeSpan.FromMinutes(61));
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna })).StatusCode);
        Assert.Equal(4, await CorreosDeRecuperacion());
    }

    [Fact]
    public async Task RF_03_Restablecer_InvalidaLosDemasEnlacesYReiniciaElBloqueo()
    {
        // 10 §1 (auditoría 2026-10-03, decisión del paso 3).
        await RegistrarYVerificar(CorreoAna);
        await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna });
        var primero = await TokenDelUltimoCorreo(CorreoAna, "recuperacion");
        await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna });
        var segundo = await TokenDelUltimoCorreo(CorreoAna, "recuperacion");
        // La cuenta queda bloqueada como tras 5 intentos fallidos (sin gastar el límite de 10 peticiones por IP).
        await using (var db = Db(out var scope))
        {
            using var _ = scope;
            var ahora = _reloj.Ahora;
            await db.Set<Usuario>().IgnoreQueryFilters().Where(u => u.Correo == CorreoAna)
                .ExecuteUpdateAsync(u => u.SetProperty(x => x.BloqueadoHasta, ahora + Usuario.DuracionBloqueo));
        }

        await AfirmarProblema(await Entrar(CorreoAna, ContrasenaValida), HttpStatusCode.Locked, "cuenta_bloqueada");

        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Post, "/api/auth/restablecer", new { token = segundo, contrasena = "NuevaContra456" })).StatusCode);
        await AfirmarProblema(await Enviar(HttpMethod.Post, "/api/auth/restablecer", new { token = primero, contrasena = "OtraContra789" }),
            HttpStatusCode.UnprocessableEntity, "token_invalido");
        Assert.Equal(HttpStatusCode.OK, (await Entrar(CorreoAna, "NuevaContra456")).StatusCode);
    }

    [Fact]
    public async Task RF_03_Restablecer_TokenQueNoExiste_Responde422TokenInvalido()
    {
        // Criterio 2 (auditoría 2026-10-03, H-23).
        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/restablecer", new { token = "inventado", contrasena = "NuevaContra456" });

        await AfirmarProblema(respuesta, HttpStatusCode.UnprocessableEntity, "token_invalido");
    }

    [Fact]
    public async Task RF_03_Restablecer_TokenDeUnConsumidorEnElPanel_Responde422TokenInvalido()
    {
        // Criterio 4 (auditoría 2026-10-03, H-23): un enlace del portal no sirve en /api/auth/restablecer.
        var valor = SeguridadTokens.GenerarToken();
        await using (var db = Db(out var scope))
        {
            using var _ = scope;
            var organizacion = new Organizacion("Tienda de prueba", TipoOrganizacion.Proveedor);
            var consumidor = new Consumidor(organizacion.Id, "Ana", "Tienda", "ana@tienda.test", "hash");
            db.AddRange(organizacion, consumidor);
            db.Add(Token.Recuperacion(SeguridadTokens.HashearToken(valor), null, consumidor.Id, organizacion.Id, consumidor.Correo, _reloj.Ahora));
            await db.SaveChangesAsync();
        }

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/restablecer", new { token = valor, contrasena = "NuevaContra456" });

        await AfirmarProblema(respuesta, HttpStatusCode.UnprocessableEntity, "token_invalido");
    }

    [Fact]
    public async Task RF_03_Restablecer_TokenValido_CambiaContrasenaRevocaSesionesEIniciaNueva()
    {
        await RegistrarYVerificar(CorreoAna);
        // Crear una sesión preexistente
        var sesionPrevia = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));

        await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna });
        var token = await TokenDelUltimoCorreo(CorreoAna, "recuperacion");

        var nuevaContrasena = "NuevaContra456";
        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/restablecer", new { token, contrasena = nuevaContrasena });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        // La sesión previa fue revocada
        var sesionRespuesta = await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: sesionPrevia);
        Assert.Equal(HttpStatusCode.Unauthorized, sesionRespuesta.StatusCode);
        // Se inició una nueva sesión con la cookie de la respuesta
        var cookieNueva = CookieDeSesion(respuesta);
        var sesionNueva = await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: cookieNueva);
        Assert.Equal(HttpStatusCode.OK, sesionNueva.StatusCode);
        // La contraseña anterior ya no funciona
        var entrarConAnterior = await Entrar(CorreoAna, ContrasenaValida);
        Assert.Equal(HttpStatusCode.Unauthorized, entrarConAnterior.StatusCode);
        // La nueva contraseña sí funciona
        var entrarConNueva = await Entrar(CorreoAna, nuevaContrasena);
        Assert.Equal(HttpStatusCode.OK, entrarConNueva.StatusCode);
    }

    [Fact]
    public async Task RF_03_Restablecer_TokenUsado_Responde422TokenInvalido()
    {
        await RegistrarYVerificar(CorreoAna);
        await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna });
        var token = await TokenDelUltimoCorreo(CorreoAna, "recuperacion");

        // Primer uso: OK
        await Enviar(HttpMethod.Post, "/api/auth/restablecer", new { token, contrasena = "NuevaContra456" });

        // Segundo uso: token inválido
        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/restablecer", new { token, contrasena = "OtraContra789" });

        await AfirmarProblema(respuesta, HttpStatusCode.UnprocessableEntity, "token_invalido");
    }

    [Fact]
    public async Task RF_03_Restablecer_TokenVencido_Responde422TokenInvalido()
    {
        await RegistrarYVerificar(CorreoAna);
        await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna });
        var token = await TokenDelUltimoCorreo(CorreoAna, "recuperacion");

        // Avanzar más de 60 minutos
        _reloj.Avanzar(TimeSpan.FromMinutes(61));

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/restablecer", new { token, contrasena = "NuevaContra456" });

        await AfirmarProblema(respuesta, HttpStatusCode.UnprocessableEntity, "token_invalido");
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta")]    // menos de 10 caracteres
    public async Task RF_03_Restablecer_ContrasenaInvalida_Responde400SinConsumirToken(string contrasenaMala)
    {
        await RegistrarYVerificar(CorreoAna);
        await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna });
        var token = await TokenDelUltimoCorreo(CorreoAna, "recuperacion");

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/restablecer", new { token, contrasena = contrasenaMala });

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("contrasena", out var _contrasena));
        // El token NO fue consumido
        await using var db = Db(out var scope);
        using var scope2 = scope;
        var entidadToken = await db.Set<Token>().IgnoreQueryFilters()
            .SingleAsync(t => t.Tipo == TipoToken.Recuperacion);
        Assert.Null(entidadToken.UsadoEn);
    }

    [Fact]
    public async Task RF_03_Restablecer_ContrasenaIgualAlCorreo_Responde400SinConsumirToken()
    {
        // 10 §1: la contraseña no puede ser igual al correo
        await RegistrarYVerificar(CorreoAna);
        await Enviar(HttpMethod.Post, "/api/auth/recuperar", new { correo = CorreoAna });
        var token = await TokenDelUltimoCorreo(CorreoAna, "recuperacion");

        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/restablecer", new { token, contrasena = CorreoAna });

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("contrasena", out var _correoErr));
        // El token NO fue consumido
        await using var db = Db(out var scope);
        using var scope2 = scope;
        var entidadToken = await db.Set<Token>().IgnoreQueryFilters()
            .SingleAsync(t => t.Tipo == TipoToken.Recuperacion);
        Assert.Null(entidadToken.UsadoEn);
    }

    // ---------- RF-04 · Perfil ----------

    [Fact]
    public async Task RF_04_ObtenerPerfil_UsuarioAutenticado_RetornaNombre()
    {
        await RegistrarYVerificar(CorreoAna);
        var cookie = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));

        var respuesta = await Enviar(HttpMethod.Get, "/api/perfil/", cookie: cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var datos = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Ana López", datos.GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task RF_04_EditarPerfil_NombreValido_ActualizaElNombre()
    {
        await RegistrarYVerificar(CorreoAna);
        var cookie = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));

        var respuesta = await Enviar(HttpMethod.Put, "/api/perfil/", new { nombre = "Ana Morales" }, cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await using var db = Db(out var scope);
        using var _ = scope;
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Correo == CorreoAna);
        Assert.Equal("Ana Morales", usuario.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RF_04_EditarPerfil_NombreVacio_Responde400ConErrorNombre(string nombreVacio)
    {
        await RegistrarYVerificar(CorreoAna);
        var cookie = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));

        var respuesta = await Enviar(HttpMethod.Put, "/api/perfil/", new { nombre = nombreVacio }, cookie);

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("nombre", out _));
    }

    [Fact]
    public async Task RF_04_EditarPerfil_NombreMuyLargo_Responde400ConErrorNombre()
    {
        await RegistrarYVerificar(CorreoAna);
        var cookie = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));
        var nombreLargo = new string('a', 121); // más de 120 caracteres

        var respuesta = await Enviar(HttpMethod.Put, "/api/perfil/", new { nombre = nombreLargo }, cookie);

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("nombre", out _));
    }

    [Fact]
    public async Task RF_04_CambiarContrasena_ContrasenaActualCorrecta_CambiaYRevocaDemasSesiones()
    {
        await RegistrarYVerificar(CorreoAna);
        var otraSesion = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));
        var sesionActual = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));

        var nuevaContrasena = "NuevaContra456";
        var respuesta = await Enviar(HttpMethod.Post, "/api/perfil/contrasena",
            new { contrasenaActual = ContrasenaValida, contrasenaNueva = nuevaContrasena }, sesionActual);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        // La otra sesión fue revocada
        var sesionOtraRespuesta = await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: otraSesion);
        Assert.Equal(HttpStatusCode.Unauthorized, sesionOtraRespuesta.StatusCode);
        // La sesión actual sigue activa
        var sesionActualRespuesta = await Enviar(HttpMethod.Get, "/api/auth/sesion", cookie: sesionActual);
        Assert.Equal(HttpStatusCode.OK, sesionActualRespuesta.StatusCode);
    }

    [Fact]
    public async Task RF_04_CambiarContrasena_ContrasenaActualIncorrecta_Responde400EnElCampoYNoLaCambia()
    {
        // 11 §4 (auditoría 2026-10-03, H-19 y H-23): el error va debajo de «Contraseña actual» y la contraseña sigue
        // igual.
        await RegistrarYVerificar(CorreoAna);
        var cookie = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));

        var respuesta = await Enviar(HttpMethod.Post, "/api/perfil/contrasena",
            new { contrasenaActual = "Incorrecta123!", contrasenaNueva = "NuevaContra456" }, cookie);

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.Equal("La contraseña actual no es correcta.",
            problema.GetProperty("errores").GetProperty("contrasenaActual")[0].GetString());
        Assert.Equal(HttpStatusCode.OK, (await Entrar(CorreoAna, ContrasenaValida)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Entrar(CorreoAna, "NuevaContra456")).StatusCode);
    }

    [Fact]
    public async Task RF_04_CambiarContrasena_CincoIntentosConLaActualIncorrecta_BloqueanLaCuenta()
    {
        // RF-04 y 10 §1 (auditoría 2026-10-03, decisión del paso 3): una sesión robada no puede probar contraseñas
        // sin límite.
        await RegistrarYVerificar(CorreoAna);
        var cookie = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));
        for (var i = 0; i < 5; i++)
        {
            await Enviar(HttpMethod.Post, "/api/perfil/contrasena",
                new { contrasenaActual = "Incorrecta123!", contrasenaNueva = "NuevaContra456" }, cookie);
        }

        await AfirmarProblema(await Enviar(HttpMethod.Post, "/api/perfil/contrasena",
            new { contrasenaActual = ContrasenaValida, contrasenaNueva = "NuevaContra456" }, cookie), HttpStatusCode.Locked, "cuenta_bloqueada");
        await AfirmarProblema(await Entrar(CorreoAna, ContrasenaValida), HttpStatusCode.Locked, "cuenta_bloqueada");
    }

    [Fact]
    public async Task RF_04_CambiarContrasena_LaNuevaFuncionaYLaAnteriorNo()
    {
        // Criterio 3 (auditoría 2026-10-03, H-23).
        await RegistrarYVerificar(CorreoAna);
        var cookie = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));

        var respuesta = await Enviar(HttpMethod.Post, "/api/perfil/contrasena",
            new { contrasenaActual = ContrasenaValida, contrasenaNueva = "NuevaContra456" }, cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Entrar(CorreoAna, "NuevaContra456")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Entrar(CorreoAna, ContrasenaValida)).StatusCode);
    }

    [Theory]
    [InlineData("corta")]   // menos de 10 caracteres
    [InlineData("")]
    public async Task RF_04_CambiarContrasena_ContrasenaNuevaInvalida_Responde400ConErrorContrasenaNueva(string contrasenaNueva)
    {
        await RegistrarYVerificar(CorreoAna);
        var cookie = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));

        var respuesta = await Enviar(HttpMethod.Post, "/api/perfil/contrasena",
            new { contrasenaActual = ContrasenaValida, contrasenaNueva }, cookie);

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("contrasenaNueva", out _));
    }

    [Fact]
    public async Task RF_04_CambiarContrasena_ContrasenaNuevaIgualAlCorreo_Responde400ConErrorContrasenaNueva()
    {
        // 10 §1: la contraseña no puede ser igual al correo
        await RegistrarYVerificar(CorreoAna);
        var cookie = CookieDeSesion(await Entrar(CorreoAna, ContrasenaValida));

        var respuesta = await Enviar(HttpMethod.Post, "/api/perfil/contrasena",
            new { contrasenaActual = ContrasenaValida, contrasenaNueva = CorreoAna }, cookie);

        var problema = await AfirmarProblema(respuesta, HttpStatusCode.BadRequest, "datos_invalidos");
        Assert.True(problema.GetProperty("errores").TryGetProperty("contrasenaNueva", out _));
    }

    // ---------- Helpers ----------

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

    private Task<HttpResponseMessage> Registrar(string correo) =>
        Enviar(HttpMethod.Post, "/api/auth/registro", new { nombre = "Ana López", correo, organizacion = "Envíos Xelajú", contrasena = ContrasenaValida });

    private Task<HttpResponseMessage> Entrar(string correo, string contrasena) =>
        Enviar(HttpMethod.Post, "/api/auth/entrar", new { correo, contrasena });

    private async Task RegistrarYVerificar(string correo)
    {
        await Registrar(correo);
        await using var db = Db(out var scope);
        using (scope)
        {
            var correoNorm = Usuario.NormalizarCorreo(correo);
            var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Correo == correoNorm);
            var token = await db.Set<Token>().IgnoreQueryFilters()
                .SingleAsync(t => t.UsuarioId == usuario.Id && t.Tipo == TipoToken.VerificacionCorreo);
            var correoSaliente = await db.Set<CorreoSaliente>().SingleAsync(c => c.Destinatario == correoNorm);
            var valorToken = System.Text.Json.JsonDocument.Parse(correoSaliente.Datos).RootElement.GetProperty("token").GetString()!;
            await Enviar(HttpMethod.Post, "/api/auth/verificar-correo", new { token = valorToken });
        }
    }

    private static string CookieDeSesion(HttpResponseMessage respuesta) =>
        respuesta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("shapi_sesion=", StringComparison.Ordinal)).Split(';')[0];

    private async Task<string> TokenDelUltimoCorreo(string destinatario, string plantilla)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var correos = await db.Set<CorreoSaliente>()
            .Where(c => c.Destinatario == destinatario && c.Plantilla == plantilla)
            .ToListAsync();
        return System.Text.Json.JsonDocument.Parse(correos[^1].Datos).RootElement.GetProperty("token").GetString()!;
    }

    private async Task<int> CorreosDeRecuperacion()
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        return await db.Set<CorreoSaliente>().CountAsync(c => c.Destinatario == CorreoAna && c.Plantilla == "recuperacion");
    }

    private static string[] NombresCabeceras(HttpResponseMessage respuesta) =>
        respuesta.Headers.Select(c => c.Key).Where(n => n != "Date").Order(StringComparer.Ordinal).ToArray();

    private static async Task<JsonElement> AfirmarProblema(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(codigo, problema.GetProperty("codigo").GetString());
        return problema;
    }
}
