using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shapi.Api.Tests.Persistencia;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Apis;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Correo;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Soporte;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;
using ApiDominio = Shapi.Dominio.Apis.Api;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Tests.Soporte;

public sealed class ContenedorPostgresSoporte : PostgresDePrueba;

public sealed class RelojSoporte : IReloj
{
    public DateTimeOffset Ahora { get; set; } = new(2026, 10, 7, 15, 0, 0, TimeSpan.Zero);
}

public class SoporteTests(ContenedorPostgresSoporte postgres) : IClassFixture<ContenedorPostgresSoporte>, IAsyncLifetime
{
    private const string Contrasena = "SuperSecreto123!";
    private readonly RelojSoporte _reloj = new();
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
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Rodrigo Alvarado");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", Contrasena);
            builder.UseSetting("SHAPI_DOMINIO_BASE", "shapi.localhost");
            builder.ConfigureTestServices(services => services.AddSingleton<IReloj>(_reloj));
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await PostgresCompartido.EliminarBaseAsync(_cadena);
    }

    [Theory]
    [InlineData(Rol.Propietario)]
    [InlineData(Rol.Editor)]
    [InlineData(Rol.Lector)]
    public async Task RF_40_Proveedor_PuedeAbrirListarYConsultarSusCasos(Rol rol)
    {
        var (usuario, organizacion) = await CrearUsuario($"Persona {rol}", rol, TipoOrganizacion.Proveedor);
        var api = await CrearApi(organizacion.Id, $"api-{Guid.NewGuid():N}"[..20]);
        var cookie = await CrearSesion(usuario.Id);

        var abrir = await Enviar(HttpMethod.Post, "/api/casos", cookie,
            new { asunto = "El dominio propio no verifica", apiId = api.Id, descripcion = "Sigue pendiente." });

        Assert.Equal(HttpStatusCode.Created, abrir.StatusCode);
        var creado = await abrir.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("CAS-", $"CAS-{creado.GetProperty("numero").GetInt32()}");
        Assert.Single(creado.GetProperty("mensajes").EnumerateArray());

        var lista = await Enviar(HttpMethod.Get, "/api/casos", cookie);
        var elementos = await lista.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Single(elementos.EnumerateArray());
        Assert.Equal(api.Nombre, elementos[0].GetProperty("apiNombre").GetString());

        var otro = await CrearUsuario("Persona ajena", Rol.Propietario, TipoOrganizacion.Proveedor);
        var otroCookie = await CrearSesion(otro.Usuario.Id);
        var aislado = await Enviar(HttpMethod.Get, $"/api/casos/{creado.GetProperty("numero").GetInt32()}", otroCookie);
        Assert.Equal(HttpStatusCode.NotFound, aislado.StatusCode);

        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.True(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().AnyAsync(e => e.Accion == "caso.abierto"));
    }

    [Fact]
    public async Task RF_40_Soporte_AsignaRespondeYCierra_EncolaCorreoYRegistraBitacora()
    {
        var (propietaria, organizacion) = await CrearUsuario("Ana Lucia Morales", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario("Sofia Menchu", Rol.Soporte, TipoOrganizacion.Plataforma);
        var cookiePropietaria = await CrearSesion(propietaria.Id);
        var cookieSoporte = await CrearSesion(soporte.Id);
        var abierto = await Enviar(HttpMethod.Post, "/api/casos", cookiePropietaria,
            new { asunto = "El dominio propio no verifica", descripcion = "Sigue pendiente." });
        var numero = (await abierto.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("numero").GetInt32();

        Assert.Equal(HttpStatusCode.NoContent, (await Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/asignar", cookieSoporte)).StatusCode);
        var respuesta = await Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/mensajes", cookieSoporte,
            new { cuerpo = "Cambie el destino CNAME." });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Enviar(HttpMethod.Post, $"/api/admin/casos/{numero}/cerrar", cookieSoporte)).StatusCode);

        var cerrado = await Enviar(HttpMethod.Post, $"/api/casos/{numero}/mensajes", cookiePropietaria,
            new { cuerpo = "Gracias." });
        await AfirmarProblema(cerrado, HttpStatusCode.UnprocessableEntity, "caso_cerrado");

        await using var db = Db(out var scope);
        using var _ = scope;
        var correo = await db.Set<CorreoSaliente>().SingleAsync(c =>
            c.Plantilla == "respuesta_caso" && c.Destinatario == propietaria.Correo);
        Assert.Equal(propietaria.Correo, correo.Destinatario);
        Assert.Contains($"CAS-{numero}", correo.Datos);
        Assert.True(await db.Set<EntradaBitacoraDominio>().IgnoreQueryFilters().AnyAsync(e => e.Accion == "caso.cerrado" && e.OrganizacionId == organizacion.Id));
    }

    [Fact]
    public async Task RF_40_Administracion_RegistraCasoYConsultaResumenSoloLectura()
    {
        var (propietaria, organizacion) = await CrearUsuario("Ana Lucia Morales", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario("Sofia Menchu", Rol.Soporte, TipoOrganizacion.Plataforma);
        var api = await CrearApi(organizacion.Id, $"api-{Guid.NewGuid():N}"[..20]);
        var cookie = await CrearSesion(soporte.Id);

        var abrir = await Enviar(HttpMethod.Post, "/api/admin/casos", cookie,
            new { organizacionId = organizacion.Id, apiId = api.Id, asunto = "Caso telefonico", descripcion = "Detalle." });
        Assert.Equal(HttpStatusCode.Created, abrir.StatusCode);
        var numero = (await abrir.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("numero").GetInt32();

        var resumen = await Enviar(HttpMethod.Get, $"/api/admin/casos/{numero}/organizacion", cookie);
        Assert.Equal(HttpStatusCode.OK, resumen.StatusCode);
        var json = await resumen.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(organizacion.Nombre, json.GetProperty("organizacion").GetString());
        Assert.Equal(api.Nombre, json.GetProperty("apiAfectada").GetString());
        Assert.True(json.GetProperty("numeroApis").GetInt32() >= 1);
        Assert.True(json.TryGetProperty("numeroConsumidores", out var numeroConsumidores));
        Assert.True(numeroConsumidores.GetInt32() >= 0);

        await using var db = Db(out var scope);
        using var _ = scope;
        Assert.True(await db.Set<CorreoSaliente>().AnyAsync(c => c.Destinatario == propietaria.Correo && c.Plantilla == "respuesta_caso"));
    }

    [Fact]
    public async Task RF_40_Permisos_SeparanPanelProveedorYAdministracion()
    {
        var (propietario, _) = await CrearUsuario("Propietario", Rol.Propietario, TipoOrganizacion.Proveedor);
        var (soporte, _) = await CrearUsuario("Soporte", Rol.Soporte, TipoOrganizacion.Plataforma);
        var cookieProveedor = await CrearSesion(propietario.Id);
        var cookieSoporte = await CrearSesion(soporte.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await Enviar(HttpMethod.Get, "/api/admin/casos", cookieProveedor)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Enviar(HttpMethod.Get, "/api/casos", cookieSoporte)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Enviar(HttpMethod.Get, "/api/admin/casos", cookieSoporte)).StatusCode);
    }

    private async Task<(Usuario Usuario, Organizacion Organizacion)> CrearUsuario(string nombre, Rol rol, TipoOrganizacion tipo)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var organizacion = tipo == TipoOrganizacion.Plataforma
            ? await db.Set<Organizacion>().IgnoreQueryFilters().SingleAsync(o => o.Tipo == tipo)
            : new Organizacion($"Organizacion de {nombre}", tipo);
        var usuario = new Usuario(nombre, $"{Guid.NewGuid():N}@ejemplo.test");
        usuario.DefinirHashContrasena(new PasswordHasher<Usuario>().HashPassword(usuario, Contrasena));
        if (tipo == TipoOrganizacion.Proveedor)
        {
            db.Add(organizacion);
        }

        db.AddRange(usuario, new Membresia(usuario.Id, organizacion.Id, rol));
        await db.SaveChangesAsync();
        return (usuario, organizacion);
    }

    private async Task<ApiDominio> CrearApi(Guid organizacionId, string subdominio)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var api = new ApiDominio(organizacionId, "API de Cotizacion", subdominio, "https://origen.test", "secreto", _reloj.Ahora);
        db.Add(api);
        await db.SaveChangesAsync();
        return api;
    }

    private async Task<string> CrearSesion(Guid usuarioId)
    {
        var valor = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        await using var db = Db(out var scope);
        using var _ = scope;
        db.Add(Sesion.IniciarPersonal(SeguridadTokens.HashearToken(valor), usuarioId, "localhost", null, null, _reloj.Ahora));
        await db.SaveChangesAsync();
        return $"shapi_sesion={valor}";
    }

    private Task<HttpResponseMessage> Enviar(HttpMethod metodo, string url, string cookie, object? cuerpo = null)
    {
        var peticion = new HttpRequestMessage(metodo, url);
        peticion.Headers.Add("Cookie", cookie);
        peticion.Headers.Add("X-Requested-With", "shapi");
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }

        return _cliente.SendAsync(peticion);
    }

    private static async Task AfirmarProblema(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(codigo, problema.GetProperty("codigo").GetString());
    }

    private ShapiDbContext Db(out IServiceScope scope)
    {
        scope = _fabrica.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
    }
}
