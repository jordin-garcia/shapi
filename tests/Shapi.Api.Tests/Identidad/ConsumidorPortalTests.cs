using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;
using Xunit;

namespace Shapi.Api.Tests.Identidad;

/// <summary>Cada prueba usa su propia base de datos en el PostgreSQL compartido (JG-18).</summary>
public sealed class ContenedorPostgresConsumidor : PostgresDePrueba;

public sealed class ConsumidorPortalTests(ContenedorPostgresConsumidor postgres) : IClassFixture<ContenedorPostgresConsumidor>, IAsyncLifetime
{
    private string _cadena = null!;
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;

    public Task InitializeAsync()
    {
        _cadena = new NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString()) { Database = $"portal_{Guid.NewGuid():N}" }.ConnectionString;
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            builder.UseSetting("SHAPI_POSTGRES_CADENA", _cadena);
            builder.UseSetting("SHAPI_DOMINIO_BASE", "shapi.localhost");
            builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Admin");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", "AdminSuperSecreto123!");
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await PostgresCompartido.EliminarBaseAsync(_cadena);
    }

    [Fact]
    public async Task RF_05_MismoCorreoEnDosOrganizaciones_SePermiteYLaSesionQuedaAisladaPorHost()
    {
        // RF-05, RNF-08: la unicidad corresponde a (organizacion, correo) y la cookie solo sirve en el host original.
        var organizacionUno = await InsertarApi("uno");
        await InsertarApi("dos");
        await InsertarApiEnOrganizacion("uno-alterno", organizacionUno);
        var registroUno = await Enviar(HttpMethod.Post, "/api/portal/auth/registro", "uno.shapi.localhost",
            new { nombre = "Ana", nombreEmpresa = "Tienda Uno", correo = "ana@tienda.test", contrasena = "ContrasenaValida123" });
        var registroDos = await Enviar(HttpMethod.Post, "/api/portal/auth/registro", "dos.shapi.localhost",
            new { nombre = "Ana", nombreEmpresa = "Tienda Dos", correo = "ana@tienda.test", contrasena = "OtraContrasena456" });
        Assert.Equal(HttpStatusCode.OK, registroUno.StatusCode);
        Assert.Equal(HttpStatusCode.OK, registroDos.StatusCode);

        var token = await TokenCorreo("ana@tienda.test", "uno.shapi.localhost");
        var verificacion = await Enviar(HttpMethod.Post, "/api/portal/auth/verificar-correo", "uno.shapi.localhost", new { token });
        Assert.Equal(HttpStatusCode.OK, verificacion.StatusCode);
        var cookie = Cookie(verificacion, "portal_sesion");
        var sesionMismoHost = await Enviar(HttpMethod.Get, "/api/portal/auth/sesion", "uno.shapi.localhost", cookie: cookie);
        Assert.Equal(HttpStatusCode.OK, sesionMismoHost.StatusCode);
        Assert.True((await sesionMismoHost.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correoVerificado").GetBoolean());
        var sesionOtroHost = await Enviar(HttpMethod.Get, "/api/portal/auth/sesion", "dos.shapi.localhost", cookie: cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, sesionOtroHost.StatusCode);
        var sesionOtroPortalMismaOrganizacion = await Enviar(HttpMethod.Get, "/api/portal/auth/sesion", "uno-alterno.shapi.localhost", cookie: cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, sesionOtroPortalMismaOrganizacion.StatusCode);

        var inicio = await Enviar(HttpMethod.Post, "/api/portal/auth/entrar", "dos.shapi.localhost", new { correo = "ana@tienda.test", contrasena = "OtraContrasena456" });
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
    }

    [Fact]
    public async Task RF_05_SesionDePortal_AutorizaClavesEnSuHostYSeRechazaEnOtro()
    {
        await InsertarApi("claves-portal");
        await InsertarApi("claves-otro");
        await Enviar(HttpMethod.Post, "/api/portal/auth/registro", "claves-portal.shapi.localhost",
            new { nombre = "Ana", nombreEmpresa = "Tienda", correo = "ana@claves.test", contrasena = "ContrasenaValida123" });
        var token = await TokenCorreo("ana@claves.test", "claves-portal.shapi.localhost");
        var verificar = await Enviar(HttpMethod.Post, "/api/portal/auth/verificar-correo", "claves-portal.shapi.localhost", new { token });
        var cookie = Cookie(verificar, "portal_sesion");

        var clavesPropias = await Enviar(HttpMethod.Get, "/api/portal/claves", "claves-portal.shapi.localhost", cookie: cookie);
        var clavesOtroPortal = await Enviar(HttpMethod.Get, "/api/portal/claves", "claves-otro.shapi.localhost", cookie: cookie);

        Assert.Equal(HttpStatusCode.OK, clavesPropias.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, clavesOtroPortal.StatusCode);
    }

    [Fact]
    public async Task RF_05_InvitacionValida_CreaConsumidorVerificadoYAbreSesion()
    {
        var organizacionId = await InsertarApi("invitado");
        var valor = SeguridadTokens.GenerarToken();
        await using (var alcance = _fabrica.Services.CreateAsyncScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
            db.Add(Token.InvitacionConsumidor(SeguridadTokens.HashearToken(valor), organizacionId, "invitada@tienda.test", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        var consulta = await Enviar(HttpMethod.Get, $"/api/portal/auth/invitacion/{valor}", "invitado.shapi.localhost");
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.Equal("invitada@tienda.test", (await consulta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("correo").GetString());
        var aceptar = await Enviar(HttpMethod.Post, $"/api/portal/auth/invitacion/{valor}/aceptar", "invitado.shapi.localhost",
            new { nombre = "Inés", nombreEmpresa = "Tienda Invitada", contrasena = "ContrasenaValida123" });
        Assert.Equal(HttpStatusCode.OK, aceptar.StatusCode);
        var cookie = Cookie(aceptar, "portal_sesion");
        var sesion = await Enviar(HttpMethod.Get, "/api/portal/auth/sesion", "invitado.shapi.localhost", cookie: cookie);
        Assert.Equal(HttpStatusCode.OK, sesion.StatusCode);
        var json = await sesion.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("correoVerificado").GetBoolean());
        Assert.Equal("Inés", json.GetProperty("consumidor").GetProperty("nombre").GetString());
    }

    [Fact]
    public async Task RF_05_CincoIntentosFallidosEnParalelo_BloqueanAlConsumidor()
    {
        await InsertarApi("bloqueo");
        await Enviar(HttpMethod.Post, "/api/portal/auth/registro", "bloqueo.shapi.localhost",
            new { nombre = "Ana", nombreEmpresa = "Tienda", correo = "ana@bloqueo.test", contrasena = "ContrasenaValida123" });

        var fallos = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Enviar(HttpMethod.Post, "/api/portal/auth/entrar", "bloqueo.shapi.localhost",
            new { correo = "ana@bloqueo.test", contrasena = "ContrasenaIncorrecta" })));

        Assert.All(fallos, respuesta => Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode));
        var correcto = await Enviar(HttpMethod.Post, "/api/portal/auth/entrar", "bloqueo.shapi.localhost",
            new { correo = "ana@bloqueo.test", contrasena = "ContrasenaValida123" });
        Assert.Equal(HttpStatusCode.Locked, correcto.StatusCode);
    }

    [Fact]
    public async Task RF_05_RecuperarConCorreoCompartido_EmiteTokenSoloParaElConsumidor()
    {
        var organizacionId = await InsertarApi("recuperar");
        await Enviar(HttpMethod.Post, "/api/portal/auth/registro", "recuperar.shapi.localhost",
            new { nombre = "Ana", nombreEmpresa = "Tienda", correo = "misma@correo.test", contrasena = "ContrasenaValida123" });
        await using (var alcance = _fabrica.Services.CreateAsyncScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
            db.Add(new Usuario("Ana Personal", "misma@correo.test"));
            await db.SaveChangesAsync();
        }

        var respuesta = await Enviar(HttpMethod.Post, "/api/portal/auth/recuperar", "recuperar.shapi.localhost", new { correo = "misma@correo.test" });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await using var lectura = _fabrica.Services.CreateAsyncScope();
        var contexto = lectura.ServiceProvider.GetRequiredService<ShapiDbContext>();
        var token = await contexto.Set<Token>().IgnoreQueryFilters().OrderByDescending(t => t.CreadoEn).FirstAsync(t => t.Tipo == TipoToken.Recuperacion);
        Assert.Null(token.UsuarioId);
        Assert.NotNull(token.ConsumidorId);
        Assert.Equal(organizacionId, token.OrganizacionId);
        var correoRecuperacion = await contexto.Set<CorreoSaliente>().IgnoreQueryFilters()
            .SingleAsync(c => c.Destinatario == "misma@correo.test" && c.Plantilla == "recuperacion");
        using var datosCorreo = JsonDocument.Parse(correoRecuperacion.Datos);
        Assert.Equal("API de prueba", datosCorreo.RootElement.GetProperty("nombrePortal").GetString());
        Assert.Equal("recuperar.shapi.localhost", datosCorreo.RootElement.GetProperty("hostPortal").GetString());
    }

    [Fact]
    public async Task RF_05_SalirEnPortal_NoRevocaUnaSesionPersonal()
    {
        await InsertarApi("salir");
        var valorCookie = SeguridadTokens.GenerarToken();
        Sesion sesion;
        await using (var alcance = _fabrica.Services.CreateAsyncScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
            var usuario = new Usuario("Usuario Panel", "panel@shapi.test");
            db.Add(usuario);
            sesion = Sesion.IniciarPersonal(SeguridadTokens.HashearToken(valorCookie), usuario.Id, "salir.shapi.localhost", null, null, DateTimeOffset.UtcNow);
            db.Add(sesion);
            await db.SaveChangesAsync();
        }

        var cerrar = await Enviar(HttpMethod.Post, "/api/portal/auth/salir", "salir.shapi.localhost", cookie: $"portal_sesion={valorCookie}");
        Assert.Equal(HttpStatusCode.OK, cerrar.StatusCode);
        await using var lectura = _fabrica.Services.CreateAsyncScope();
        var contexto = lectura.ServiceProvider.GetRequiredService<ShapiDbContext>();
        Assert.Null((await contexto.Set<Sesion>().IgnoreQueryFilters().SingleAsync(s => s.Id == sesion.Id)).RevocadaEn);
    }

    [Fact]
    public async Task RF_05_Entrar_LimitaDiezPeticionesPorMinutoPorIp()
    {
        await InsertarApi("limite");
        for (var i = 0; i < 10; i++)
        {
            var respuesta = await Enviar(HttpMethod.Post, "/api/portal/auth/entrar", "limite.shapi.localhost",
                new { correo = $"desconocido{i}@limite.test", contrasena = "ContrasenaValida123" });
            Assert.NotEqual(HttpStatusCode.TooManyRequests, respuesta.StatusCode);
        }
        var limitada = await Enviar(HttpMethod.Post, "/api/portal/auth/entrar", "limite.shapi.localhost",
            new { correo = "otro@limite.test", contrasena = "ContrasenaValida123" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limitada.StatusCode);
        Assert.Equal("demasiadas_peticiones", (await limitada.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task RF_04_SesionPersonal_NoAutenticaUnaSesionDelPortal()
    {
        await InsertarApi("sesion-personal");
        var valorCookie = SeguridadTokens.GenerarToken();
        await using (var alcance = _fabrica.Services.CreateAsyncScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
            var usuario = new Usuario("Persona del panel", "persona@shapi.test");
            db.Add(usuario);
            db.Add(Sesion.IniciarPersonal(SeguridadTokens.HashearToken(valorCookie), usuario.Id, "shapi.localhost", null, null, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        var respuesta = await Enviar(HttpMethod.Get, "/api/portal/auth/sesion", "sesion-personal.shapi.localhost", cookie: $"portal_sesion={valorCookie}");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task RF_03_RestablecerEnElPortal_CambiaContrasenaRevocaSesionesYRechazaTokenDeOtraOrganizacion()
    {
        var organizacionUno = await InsertarApi("recuperacion-uno");
        await InsertarApi("recuperacion-dos");
        await Enviar(HttpMethod.Post, "/api/portal/auth/registro", "recuperacion-uno.shapi.localhost",
            new { nombre = "Ana", nombreEmpresa = "Tienda Uno", correo = "ana@recuperacion.test", contrasena = "ContrasenaOriginal123" });
        Guid consumidorId;
        Guid sesionId;
        var tokenValor = SeguridadTokens.GenerarToken();
        await using (var alcance = _fabrica.Services.CreateAsyncScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
            var consumidor = await db.Set<Consumidor>().IgnoreQueryFilters().SingleAsync(c => c.Correo == "ana@recuperacion.test");
            consumidorId = consumidor.Id;
            db.Add(Token.Recuperacion(SeguridadTokens.HashearToken(tokenValor), null, consumidor.Id, organizacionUno, consumidor.Correo, DateTimeOffset.UtcNow));
            var sesion = Sesion.IniciarConsumidor(SeguridadTokens.HashearToken(SeguridadTokens.GenerarToken()), consumidor.Id,
                "recuperacion-uno.shapi.localhost", null, null, DateTimeOffset.UtcNow);
            sesionId = sesion.Id;
            db.Add(sesion);
            await db.SaveChangesAsync();
        }

        var otraOrganizacion = await Enviar(HttpMethod.Post, "/api/portal/auth/restablecer", "recuperacion-dos.shapi.localhost",
            new { token = tokenValor, contrasena = "NuevaContrasena456" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, otraOrganizacion.StatusCode);
        var restablecer = await Enviar(HttpMethod.Post, "/api/portal/auth/restablecer", "recuperacion-uno.shapi.localhost",
            new { token = tokenValor, contrasena = "NuevaContrasena456" });
        Assert.Equal(HttpStatusCode.OK, restablecer.StatusCode);
        Assert.Contains(restablecer.Headers.GetValues("Set-Cookie"), c => c.StartsWith("portal_sesion=", StringComparison.Ordinal));

        await using var lectura = _fabrica.Services.CreateAsyncScope();
        var contexto = lectura.ServiceProvider.GetRequiredService<ShapiDbContext>();
        var consumidorActual = await contexto.Set<Consumidor>().IgnoreQueryFilters().SingleAsync(c => c.Id == consumidorId);
        Assert.Equal(Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success,
            new Microsoft.AspNetCore.Identity.PasswordHasher<Consumidor>().VerifyHashedPassword(consumidorActual, consumidorActual.HashContrasena, "NuevaContrasena456"));
        Assert.NotNull((await contexto.Set<Sesion>().IgnoreQueryFilters().SingleAsync(s => s.Id == sesionId)).RevocadaEn);
        Assert.NotNull(await contexto.Set<Token>().IgnoreQueryFilters().Where(t => t.Tipo == TipoToken.Recuperacion && t.HashToken == SeguridadTokens.HashearToken(tokenValor))
            .Select(t => t.UsadoEn).SingleAsync());
    }

    [Fact]
    public async Task RF_02_ReenviarVerificacion_TresReenviosNoCuentanElCorreoInicial()
    {
        await InsertarApi("reenvio");
        await Enviar(HttpMethod.Post, "/api/portal/auth/registro", "reenvio.shapi.localhost",
            new { nombre = "Ana", nombreEmpresa = "Tienda", correo = "ana@reenvio.test", contrasena = "ContrasenaValida123" });
        for (var i = 0; i < 3; i++)
        {
            var respuesta = await Enviar(HttpMethod.Post, "/api/portal/auth/reenviar-verificacion", "reenvio.shapi.localhost", new { correo = "ana@reenvio.test" });
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        }
        await Enviar(HttpMethod.Post, "/api/portal/auth/reenviar-verificacion", "reenvio.shapi.localhost", new { correo = "ana@reenvio.test" });
        await using var alcance = _fabrica.Services.CreateAsyncScope();
        var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
        Assert.Equal(4, await db.Set<CorreoSaliente>().IgnoreQueryFilters().CountAsync(c => c.Destinatario == "ana@reenvio.test" && c.Plantilla == "verificacion_correo"));
    }

    [Fact]
    public async Task RF_05_InvitacionVencidaODeOtraOrganizacion_Responde422()
    {
        var organizacion = await InsertarApi("invitacion-duena");
        await InsertarApi("invitacion-ajena");
        var vencida = SeguridadTokens.GenerarToken();
        var vigente = SeguridadTokens.GenerarToken();
        await using (var alcance = _fabrica.Services.CreateAsyncScope())
        {
            var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
            db.AddRange(
                Token.InvitacionConsumidor(SeguridadTokens.HashearToken(vencida), organizacion, "vencida@tienda.test", DateTimeOffset.UtcNow.AddDays(-8)),
                Token.InvitacionConsumidor(SeguridadTokens.HashearToken(vigente), organizacion, "ajena@tienda.test", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        var expirada = await Enviar(HttpMethod.Get, $"/api/portal/auth/invitacion/{vencida}", "invitacion-duena.shapi.localhost");
        var otraOrganizacion = await Enviar(HttpMethod.Get, $"/api/portal/auth/invitacion/{vigente}", "invitacion-ajena.shapi.localhost");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, expirada.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, otraOrganizacion.StatusCode);
    }

    private async Task<Guid> InsertarApi(string subdominio)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            WITH org AS (
              INSERT INTO organizacion (id, nombre, tipo, estado_admin)
              VALUES (gen_random_uuid(), @nombre, 'proveedor', 'activa') RETURNING id
            )
            INSERT INTO api (id, organizacion_id, nombre, subdominio, url_origen, estado, portal_color, secreto_origen_cifrado)
            SELECT gen_random_uuid(), id, 'API de prueba', @sub, 'https://origen.ejemplo.com', 'publicada', '#3B6FF0', 'secreto' FROM org
            RETURNING organizacion_id
            """;
        comando.Parameters.AddWithValue("nombre", $"Organización {subdominio}");
        comando.Parameters.AddWithValue("sub", subdominio);
        return (Guid)(await comando.ExecuteScalarAsync())!;
    }

    private async Task InsertarApiEnOrganizacion(string subdominio, Guid organizacionId)
    {
        await using var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        await using var comando = conexion.CreateCommand();
        comando.CommandText = """
            INSERT INTO api (id, organizacion_id, nombre, subdominio, url_origen, estado, portal_color, secreto_origen_cifrado)
            VALUES (gen_random_uuid(), @organizacion, 'API secundaria', @sub, 'https://origen.ejemplo.com', 'publicada', '#3B6FF0', 'secreto')
            """;
        comando.Parameters.AddWithValue("organizacion", organizacionId);
        comando.Parameters.AddWithValue("sub", subdominio);
        await comando.ExecuteNonQueryAsync();
    }

    private async Task<string> TokenCorreo(string correo, string host)
    {
        await using var alcance = _fabrica.Services.CreateAsyncScope();
        var db = alcance.ServiceProvider.GetRequiredService<ShapiDbContext>();
        var mensajes = await db.Set<CorreoSaliente>().IgnoreQueryFilters().Where(c => c.Destinatario == correo && c.Plantilla == "verificacion_correo")
            .Select(c => c.Datos).ToListAsync();
        foreach (var datos in mensajes)
        {
            using var json = JsonDocument.Parse(datos);
            if (json.RootElement.GetProperty("hostPortal").GetString() != host)
            {
                continue;
            }

            Assert.Equal("API de prueba", json.RootElement.GetProperty("nombrePortal").GetString());
            return json.RootElement.GetProperty("token").GetString()!;
        }

        throw new Xunit.Sdk.XunitException($"No se encontró correo para {host}.");
    }

    private Task<HttpResponseMessage> Enviar(HttpMethod metodo, string ruta, string host, object? cuerpo = null, string? cookie = null)
    {
        var peticion = new HttpRequestMessage(metodo, ruta);
        peticion.Headers.Host = host;
        peticion.Headers.Add("X-Requested-With", "shapi");
        if (cookie is not null)
        {
            peticion.Headers.Add("Cookie", cookie);
        }

        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }

        return _cliente.SendAsync(peticion);
    }

    private static string Cookie(HttpResponseMessage respuesta, string nombre)
    {
        var valor = respuesta.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(nombre + "=", StringComparison.Ordinal));
        return valor.Split(';')[0];
    }
}
