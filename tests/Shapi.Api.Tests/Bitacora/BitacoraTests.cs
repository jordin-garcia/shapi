using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Identidad;
using Shapi.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;
using EntradaBitacoraDominio = Shapi.Dominio.Bitacora.EntradaBitacora;

namespace Shapi.Api.Tests.Bitacora;

public sealed class ContenedorPostgresBitacora : IAsyncLifetime
{
    public PostgreSqlContainer Contenedor { get; } = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public Task InitializeAsync() => Contenedor.StartAsync();

    public Task DisposeAsync() => Contenedor.DisposeAsync().AsTask();
}

public sealed class RelojBitacora : IReloj
{
    // 11 sep 2026, 23:30 en Guatemala: el periodo predeterminado es del 5 al 11 de septiembre.
    public DateTimeOffset Ahora { get; set; } = new(2026, 9, 12, 5, 30, 0, TimeSpan.Zero);
}

public class BitacoraTests(ContenedorPostgresBitacora postgres) : IClassFixture<ContenedorPostgresBitacora>, IAsyncLifetime
{
    private readonly RelojBitacora _reloj = new();
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
            builder.UseSetting("SHAPI_ADMIN_CORREO", "admin@shapi.test");
            builder.UseSetting("SHAPI_ADMIN_NOMBRE", "Rodrigo Alvarado");
            builder.UseSetting("SHAPI_ADMIN_CONTRASENA", "SuperSecreto123!");
            builder.ConfigureTestServices(services => services.AddSingleton<IReloj>(_reloj));
        });
        _cliente = _fabrica.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
    }

    [Fact]
    public async Task RF_41_Administrador_ConsultaUltimosSieteDiasOrdenadosConActorAccionYDescripcion()
    {
        // RF-41 · CA1: por defecto, del 5 al 11 sep en Guatemala, de la más reciente a la más antigua.
        var (admin, plataforma) = await UsuarioPorRol(Rol.Administrador);
        var objetivo = await CrearOrganizacion("Datos Chapines, S.A.", TipoOrganizacion.Proveedor);
        await AgregarEntrada(new(2026, 9, 12, 5, 10, 0, TimeSpan.Zero), admin, objetivo.Id,
            "organizacion.suspendida", "Suspendió la organización Datos Chapines, S.A.");
        await AgregarEntrada(new(2026, 9, 5, 6, 0, 0, TimeSpan.Zero), admin, objetivo.Id,
            "cuenta_plataforma.creada", "Creó la cuenta de plataforma de Lucía Ramírez Pineda");
        await AgregarEntrada(new(2026, 9, 5, 5, 59, 59, TimeSpan.Zero), admin, objetivo.Id,
            "fuera.del.periodo", "No debe aparecer");
        var cookie = await CrearSesion(admin.Id);

        var respuesta = await Enviar("/api/admin/bitacora", cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("total").GetInt32());
        var elementos = json.GetProperty("elementos").EnumerateArray().ToArray();
        Assert.Equal(new[] { "organizacion.suspendida", "cuenta_plataforma.creada" },
            elementos.Select(e => e.GetProperty("accion").GetString()));
        var primero = elementos[0];
        Assert.Equal("Suspendió la organización Datos Chapines, S.A.", primero.GetProperty("descripcion").GetString());
        Assert.Equal("Rodrigo Alvarado", primero.GetProperty("actor").GetProperty("nombre").GetString());
        Assert.Equal("administrador", primero.GetProperty("actor").GetProperty("rol").GetString());
        Assert.Equal(plataforma.Nombre, primero.GetProperty("actor").GetProperty("organizacion").GetString());
    }

    [Fact]
    public async Task RF_41_Soporte_FiltraFechasInclusivasYPaginaResultados()
    {
        // RF-41 · CA1: soporte tiene acceso, ambos días son inclusivos y la lista se pagina.
        var (soporte, _) = await CrearUsuario("Sofía Menchú Cojtí", Rol.Soporte, TipoOrganizacion.Plataforma);
        var objetivo = await CrearOrganizacion("Envíos Xelajú, S.A.", TipoOrganizacion.Proveedor);
        await AgregarEntrada(new(2026, 9, 9, 14, 30, 0, TimeSpan.Zero), soporte, objetivo.Id, "caso.abierto", "Primera");
        await AgregarEntrada(new(2026, 9, 10, 14, 30, 0, TimeSpan.Zero), soporte, objetivo.Id, "caso.cerrado", "Segunda");
        await AgregarEntrada(new(2026, 9, 11, 5, 59, 59, TimeSpan.Zero), soporte, objetivo.Id, "caso.abierto", "Tercera");
        var cookie = await CrearSesion(soporte.Id);

        var respuesta = await Enviar("/api/admin/bitacora?desde=2026-09-09&hasta=2026-09-10&pagina=2&tamano=1", cookie);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, json.GetProperty("total").GetInt32());
        var elemento = json.GetProperty("elementos").EnumerateArray().Single();
        Assert.Equal("Segunda", elemento.GetProperty("descripcion").GetString());
        Assert.Equal("soporte", elemento.GetProperty("actor").GetProperty("rol").GetString());
    }

    [Theory]
    [InlineData("?desde=2026-09-11&hasta=2026-09-10")]
    [InlineData("?pagina=0")]
    [InlineData("?tamano=101")]
    [InlineData("?desde=9999-12-31&hasta=9999-12-31")]
    [InlineData("?hasta=0001-01-01")]
    public async Task RF_41_ParametrosInvalidos_Responden400DatosInvalidos(string query)
    {
        // RF-41 · CA1: el contrato limita el periodo y la paginación.
        var (admin, _) = await UsuarioPorRol(Rol.Administrador);
        var cookie = await CrearSesion(admin.Id);

        var respuesta = await Enviar($"/api/admin/bitacora{query}", cookie);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("datos_invalidos", json.GetProperty("codigo").GetString());
    }

    [Fact]
    public async Task RF_41_PersonalProveedor_Recibe403()
    {
        // RF-41 · CA3.
        var (proveedor, _) = await CrearUsuario("Ana Lucía Morales", Rol.Propietario, TipoOrganizacion.Proveedor);
        var cookie = await CrearSesion(proveedor.Id);

        var respuesta = await Enviar("/api/admin/bitacora", cookie);

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    private async Task<(Usuario Usuario, Organizacion Organizacion)> UsuarioPorRol(Rol rol)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var membresia = await db.Set<Membresia>().IgnoreQueryFilters().SingleAsync(m => m.Rol == rol);
        return (
            await db.Set<Usuario>().IgnoreQueryFilters().SingleAsync(u => u.Id == membresia.UsuarioId),
            await db.Set<Organizacion>().IgnoreQueryFilters().SingleAsync(o => o.Id == membresia.OrganizacionId));
    }

    private async Task<(Usuario Usuario, Organizacion Organizacion)> CrearUsuario(string nombre, Rol rol, TipoOrganizacion tipo)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var organizacion = tipo == TipoOrganizacion.Plataforma
            ? await db.Set<Organizacion>().IgnoreQueryFilters().SingleAsync(o => o.Tipo == tipo)
            : new Organizacion($"Organización de {nombre}", tipo);
        var usuario = new Usuario(nombre, $"{Guid.NewGuid():N}@ejemplo.com");
        if (tipo != TipoOrganizacion.Plataforma)
        {
            db.Add(organizacion);
        }
        db.Add(usuario);
        db.Add(new Membresia(usuario.Id, organizacion.Id, rol));
        await db.SaveChangesAsync();
        return (usuario, organizacion);
    }

    private async Task<Organizacion> CrearOrganizacion(string nombre, TipoOrganizacion tipo)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        var organizacion = new Organizacion(nombre, tipo);
        db.Add(organizacion);
        await db.SaveChangesAsync();
        return organizacion;
    }

    private async Task AgregarEntrada(DateTimeOffset fecha, Usuario actor, Guid organizacionId, string accion, string descripcion)
    {
        await using var db = Db(out var scope);
        using var _ = scope;
        db.Add(new EntradaBitacoraDominio(fecha, ActorTipo.Usuario, actor.Id, actor.Nombre, organizacionId,
            accion, null, null, descripcion, null, IPAddress.Loopback));
        await db.SaveChangesAsync();
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

    private Task<HttpResponseMessage> Enviar(string url, string cookie)
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, url);
        peticion.Headers.Add("Cookie", cookie);
        return _cliente.SendAsync(peticion);
    }

    private ShapiDbContext Db(out IServiceScope scope)
    {
        scope = _fabrica.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ShapiDbContext>();
    }
}
