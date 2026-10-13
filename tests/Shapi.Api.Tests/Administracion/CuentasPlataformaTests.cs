using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Identidad;

namespace Shapi.Api.Tests.Administracion;

public sealed class CuentasPostgres : PostgresDePrueba;

public sealed class CuentasPlataformaTests(CuentasPostgres postgres) : IAsyncLifetime, IClassFixture<CuentasPostgres>
{
    private readonly string _cadena = new Npgsql.NpgsqlConnectionStringBuilder(postgres.Contenedor.GetConnectionString())
    {
        Database = $"prueba_{Guid.NewGuid():N}",
    }.ConnectionString;
    private readonly RelojFijo _reloj = new(DateTimeOffset.Parse("2026-10-12T18:00:00Z"));
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;
    private const string Contrasena = "NuevaContrasena123!";

    public Task InitializeAsync()
    {
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SHAPI_APLICAR_MIGRACIONES", "true");
            builder.UseSetting("SHAPI_POSTGRES_CADENA", _cadena);
            builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Rodrigo Alvarado");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", Contrasena);
            builder.ConfigureTestServices(s => s.AddSingleton<IReloj>(_reloj));
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://shapi.localhost"),
            HandleCookies = false,
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        await PostgresCompartido.EliminarBaseAsync(_cadena);
    }

    private async Task<HttpResponseMessage> Enviar(string ruta, object? datos = null, string? cookie = null)
    {
        using var peticion = new HttpRequestMessage(datos is null ? HttpMethod.Get : HttpMethod.Post, ruta);
        peticion.Headers.Add("X-Requested-With", "shapi");
        if (cookie is not null)
        {
            peticion.Headers.Add("Cookie", cookie);
        }

        if (datos is not null)
        {
            peticion.Content = JsonContent.Create(datos);
        }

        return await _cliente.SendAsync(peticion);
    }

    private static string Cookie(HttpResponseMessage respuesta) => respuesta.Headers.GetValues("Set-Cookie").Single().Split(';')[0];

    private async Task<string> Admin()
    {
        var respuesta = await Enviar("/api/auth/entrar", new { correo = "admin@shapi.test", contrasena = Contrasena });
        Assert.True(respuesta.StatusCode == HttpStatusCode.OK, await respuesta.Content.ReadAsStringAsync());
        return Cookie(respuesta);
    }

    private async Task<(Guid Id, string Token)> Crear(string admin, string rol = "soporte")
    {
        var respuesta = await Enviar("/api/admin/cuentas", new { nombre = "Lucía Ramírez", correo = "lucia@shapi.test", rol }, admin);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var id = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        using var scope = _fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Shapi.Infraestructura.Persistencia.ShapiDbContext>();
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == id);
        Assert.Null(usuario.HashContrasena);
        Assert.Null(usuario.CorreoVerificadoEn);
        var membresia = await db.Set<Membresia>().IgnoreQueryFilters().SingleAsync(m => m.UsuarioId == id);
        Assert.Equal(rol == "soporte" ? Rol.Soporte : Rol.Administrador, membresia.Rol);
        Assert.Equal(TipoOrganizacion.Plataforma, (await db.Set<Organizacion>().SingleAsync(o => o.Id == membresia.OrganizacionId)).Tipo);
        var token = await db.Set<Token>().SingleAsync(t => t.UsuarioId == id);
        Assert.Equal(_reloj.Ahora.AddDays(7), token.ExpiraEn);
        var correo = await db.Set<CorreoSaliente>().SingleAsync(c => c.Destinatario == usuario.Correo);
        Assert.Equal("definir_contrasena", correo.Plantilla);
        var valor = JsonDocument.Parse(correo.Datos).RootElement.GetProperty("token").GetString()!;
        Assert.False(JsonDocument.Parse(correo.Datos).RootElement.TryGetProperty("enlace", out _));
        Assert.Equal(SeguridadTokens.HashearToken(valor), token.HashToken);
        Assert.DoesNotContain(valor, token.HashToken);
        var motor = new Shapi.Infraestructura.Correo.MotorPlantillasCorreo(scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>());
        var renderizado = motor.Renderizar(correo.Plantilla, correo.Datos);
        Assert.Contains($"https://shapi.localhost/definir-contrasena?token={Uri.EscapeDataString(valor)}", renderizado.Texto);
        correo.MarcarEnviado(_reloj.Ahora);
        Assert.DoesNotContain(valor, correo.Datos);
        Assert.DoesNotContain("enlace", correo.Datos);
        return (id, valor);
    }

    [Theory]
    [InlineData("administrador")]
    [InlineData("soporte")]
    public async Task RF_42_Crear_DefinirContrasena_VerificaCorreoIniciaSesionYConsumeToken(string rol)
    {
        var admin = await Admin();
        var cuenta = await Crear(admin, rol);
        var respuesta = await Enviar("/api/auth/definir-contrasena", new { token = cuenta.Token, contrasena = Contrasena });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cookie = Cookie(respuesta);
        Assert.Contains("httponly", respuesta.Headers.GetValues("Set-Cookie").Single(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", respuesta.Headers.GetValues("Set-Cookie").Single(), StringComparison.OrdinalIgnoreCase);
        var sesion = await Enviar("/api/auth/sesion", cookie: cookie);
        Assert.Equal(HttpStatusCode.OK, sesion.StatusCode);
        var datos = await sesion.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(rol, datos.GetProperty("rol").GetString());
        Assert.True(datos.GetProperty("correoVerificado").GetBoolean());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Enviar("/api/auth/definir-contrasena", new { token = cuenta.Token, contrasena = "OtraContrasena123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Enviar("/api/auth/entrar", new { correo = "lucia@shapi.test", contrasena = Contrasena })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Enviar("/api/admin/cuentas", new { nombre = "Otra", correo = " LUCIA@shapi.test ", rol }, admin)).StatusCode);
    }

    [Fact]
    public async Task RF_42_Desactivar_RevocaSesionImpideAccesoYPermiteReactivar()
    {
        var admin = await Admin();
        var cuenta = await Crear(admin);
        var cookie = Cookie(await Enviar("/api/auth/definir-contrasena", new { token = cuenta.Token, contrasena = Contrasena }));
        Assert.Equal(HttpStatusCode.Forbidden, (await Enviar("/api/admin/cuentas", cookie: cookie)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Enviar($"/api/admin/cuentas/{cuenta.Id}/desactivar", new { }, admin)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar("/api/auth/sesion", cookie: cookie)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Enviar("/api/auth/entrar", new { correo = "lucia@shapi.test", contrasena = Contrasena })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Enviar($"/api/admin/cuentas/{cuenta.Id}/activar", new { }, admin)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Enviar("/api/auth/entrar", new { correo = "lucia@shapi.test", contrasena = Contrasena })).StatusCode);
        using var scope = _fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Shapi.Infraestructura.Persistencia.ShapiDbContext>();
        var entradas = await db.Set<Shapi.Dominio.Bitacora.EntradaBitacora>().IgnoreQueryFilters().Where(e => e.ObjetivoId == cuenta.Id).ToListAsync();
        Assert.All(entradas, entrada => Assert.Equal("Rodrigo Alvarado", entrada.ActorNombre));
        var acciones = entradas.Select(e => e.Accion).ToList();
        Assert.Contains("cuenta_plataforma.creada", acciones);
        Assert.Contains("cuenta_plataforma.desactivada", acciones);
        Assert.Contains("cuenta_plataforma.activada", acciones);
    }

    [Fact]
    public async Task RF_42_DesactivarCuentaPropiaOUsuarioProveedor_NoSePermite()
    {
        var admin = await Admin();
        var lista = await (await Enviar("/api/admin/cuentas", cookie: admin)).Content.ReadFromJsonAsync<JsonElement>();
        var id = lista[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Enviar($"/api/admin/cuentas/{id}/desactivar", new { }, admin)).StatusCode);
        using var scope = _fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Shapi.Infraestructura.Persistencia.ShapiDbContext>();
        var proveedor = new Usuario("Proveedor", "proveedor@test.com");
        db.Add(proveedor);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await Enviar($"/api/admin/cuentas/{proveedor.Id}/desactivar", new { }, admin)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Enviar($"/api/admin/cuentas/{Guid.NewGuid()}/activar", new { }, admin)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Enviar("/api/admin/cuentas")).StatusCode);
    }

    [Theory]
    [InlineData("", "lucia@test.com", "soporte")]
    [InlineData("Lucía", "incorrecto", "soporte")]
    [InlineData("Lucía", "lucia@test.com", "propietario")]
    public async Task RF_42_Crear_DatosInvalidos_Responde400(string nombre, string correo, string rol)
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await Enviar("/api/admin/cuentas", new { nombre, correo, rol }, await Admin())).StatusCode);
    }

    [Fact]
    public async Task RF_42_Definir_DosSolicitudesSimultaneas_SoloUnaIniciaSesion()
    {
        var cuenta = await Crear(await Admin());
        var respuestas = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ =>
            Enviar("/api/auth/definir-contrasena", new { token = cuenta.Token, contrasena = Contrasena })));
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(respuestas, r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Single(respuestas, r => r.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task RF_42_Definir_CuentaDesactivadaOTokenDeOtroTipo_NoIniciaSesion()
    {
        var admin = await Admin();
        var cuenta = await Crear(admin);
        Assert.Equal(HttpStatusCode.NoContent, (await Enviar($"/api/admin/cuentas/{cuenta.Id}/desactivar", new { }, admin)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Enviar("/api/auth/definir-contrasena", new { token = cuenta.Token, contrasena = Contrasena })).StatusCode);
        using var scope = _fabrica.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Shapi.Infraestructura.Persistencia.ShapiDbContext>();
        var usuario = await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == cuenta.Id);
        Assert.Null(usuario.HashContrasena);
        var otro = SeguridadTokens.GenerarToken();
        db.Add(Token.Recuperacion(SeguridadTokens.HashearToken(otro), cuenta.Id, null, null, usuario.Correo, _reloj.Ahora));
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Enviar("/api/auth/definir-contrasena", new { token = otro, contrasena = Contrasena })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await Enviar("/api/auth/definir-contrasena", new { token = "inventado", contrasena = Contrasena })).StatusCode);
    }

    [Theory]
    [InlineData("corta", false)]
    [InlineData("lucia@shapi.test", false)]
    [InlineData(Contrasena, true)]
    public async Task RF_42_Definir_ContrasenaInvalidaOTokenVencido_NoIniciaSesion(string contrasena, bool vencido)
    {
        var cuenta = await Crear(await Admin());
        if (vencido)
        {
            _reloj.Ahora = _reloj.Ahora.AddDays(7);
        }

        var respuesta = await Enviar("/api/auth/definir-contrasena", new { token = cuenta.Token, contrasena });
        Assert.Equal(vencido ? HttpStatusCode.UnprocessableEntity : HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.False(respuesta.Headers.Contains("Set-Cookie"));
    }
}
