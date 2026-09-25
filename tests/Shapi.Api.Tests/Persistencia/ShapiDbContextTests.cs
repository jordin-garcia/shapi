#pragma warning disable CS0618
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Planes;
using Shapi.Infraestructura.Persistencia;
using Shapi.Infraestructura.Siembra.Base;
using Testcontainers.PostgreSql;
using Xunit;

namespace Shapi.Api.Tests.Persistencia;

public class ShapiDbContextTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private ShapiDbContext? _db;
    private ContextoPrueba _contextoOrganizacion = new();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var options = new DbContextOptionsBuilder<ShapiDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;

        _db = new ShapiDbContext(options, _contextoOrganizacion);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_db != null)
        {
            await _db.DisposeAsync();
        }

        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task Bitacora_EsInmutable_RechazaUpdateYDelete()
    {
        var entrada = (EntradaBitacora)Activator.CreateInstance(typeof(EntradaBitacora), true)!;
        typeof(EntradaBitacora).GetProperty("ActorTipo")!.SetValue(entrada, ActorTipo.Sistema);
        typeof(EntradaBitacora).GetProperty("ActorNombre")!.SetValue(entrada, "Sistema");
        typeof(EntradaBitacora).GetProperty("Accion")!.SetValue(entrada, "Prueba");
        typeof(EntradaBitacora).GetProperty("Descripcion")!.SetValue(entrada, "Prueba trigger");
        typeof(EntradaBitacora).GetProperty("OrganizacionId")!.SetValue(entrada, Guid.NewGuid());

        _db!.Set<EntradaBitacora>().Add(entrada);
        await _db.SaveChangesAsync();

        var entradaGuardada = await _db.Set<EntradaBitacora>().FirstAsync();
        typeof(EntradaBitacora).GetProperty("Descripcion")!.SetValue(entradaGuardada, "Modificado");

        var exUpdate = await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
        Assert.Contains("This table is append-only", exUpdate.InnerException!.Message);

        // Clear tracker para el delete
        _db.ChangeTracker.Clear();

        var entradaParaBorrar = await _db.Set<EntradaBitacora>().FirstAsync();
        _db.Set<EntradaBitacora>().Remove(entradaParaBorrar);

        var exDelete = await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
        Assert.Contains("This table is append-only", exDelete.InnerException!.Message);
    }

    [Fact]
    public async Task FiltroGlobal_FiltraPorOrganizacion()
    {
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var org1 = (Shapi.Dominio.Organizaciones.Organizacion)Activator.CreateInstance(typeof(Shapi.Dominio.Organizaciones.Organizacion), true)!;
        typeof(Shapi.Dominio.Organizaciones.Organizacion).GetProperty("Nombre")!.SetValue(org1, "Org 1");
        typeof(Shapi.Dominio.Organizaciones.Organizacion).GetProperty("Tipo")!.SetValue(org1, Shapi.Dominio.Organizaciones.TipoOrganizacion.Plataforma);

        var org2 = (Shapi.Dominio.Organizaciones.Organizacion)Activator.CreateInstance(typeof(Shapi.Dominio.Organizaciones.Organizacion), true)!;
        typeof(Shapi.Dominio.Organizaciones.Organizacion).GetProperty("Nombre")!.SetValue(org2, "Org 2");
        typeof(Shapi.Dominio.Organizaciones.Organizacion).GetProperty("Tipo")!.SetValue(org2, Shapi.Dominio.Organizaciones.TipoOrganizacion.Proveedor);

        _db!.Set<Shapi.Dominio.Organizaciones.Organizacion>().AddRange(org1, org2);
        await _db.SaveChangesAsync();

        var api1 = CrearApi("Api 1", org1.Id);
        var api2 = CrearApi("Api 2", org2.Id);

        _db!.Set<Shapi.Dominio.Apis.Api>().AddRange(api1, api2);
        await _db.SaveChangesAsync();

        // Sin contexto, debería devolver CERO filas (fail-closed)
        _contextoOrganizacion.OrganizacionId = null;
        var conteoNulo = await _db.Set<Shapi.Dominio.Apis.Api>().CountAsync();
        Assert.Equal(0, conteoNulo);

        // Con contexto, solo ve org1
        _contextoOrganizacion.OrganizacionId = org1.Id;
        var conteoOrg1 = await _db.Set<Shapi.Dominio.Apis.Api>().CountAsync();
        Assert.Equal(1, conteoOrg1);
        var itemOrg1 = await _db.Set<Shapi.Dominio.Apis.Api>().FirstAsync();
        Assert.Equal(org1.Id, itemOrg1.OrganizacionId);
    }

    [Fact]
    public async Task SiembraBase_EsIdempotente_Y_VerificaDatos()
    {
        var reloj = new Shapi.Infraestructura.Comun.RelojSistema(TimeProvider.System);
        var hasher = new PasswordHasher<Usuario>();

        await SiembraBase.EjecutarAsync(_db!, "admin@shapi.test", "Admin", "Contra123", reloj, hasher);
        var countPlanes = await _db!.Set<PlanPlataforma>().IgnoreQueryFilters().CountAsync();
        var countUsuarios = await _db!.Set<Usuario>().IgnoreQueryFilters().CountAsync();

        Assert.Equal(5, countPlanes);

        var admin = await _db!.Set<Usuario>().IgnoreQueryFilters().FirstAsync(u => u.Correo == "admin@shapi.test");
        Assert.NotEqual("Contra123", admin.HashContrasena);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(admin, admin.HashContrasena!, "Contra123"));

        var orgPlataforma = await _db!.Set<Shapi.Dominio.Organizaciones.Organizacion>().IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Tipo == Shapi.Dominio.Organizaciones.TipoOrganizacion.Plataforma);
        Assert.NotNull(orgPlataforma);

        // Ejecutar de nuevo
        await SiembraBase.EjecutarAsync(_db!, "admin@shapi.test", "Admin", "Contra123", reloj, hasher);

        var countPlanes2 = await _db!.Set<PlanPlataforma>().IgnoreQueryFilters().CountAsync();
        var countUsuarios2 = await _db!.Set<Usuario>().IgnoreQueryFilters().CountAsync();

        Assert.Equal(countPlanes, countPlanes2);
        Assert.Equal(countUsuarios, countUsuarios2);
    }

    private Shapi.Dominio.Apis.Api CrearApi(string nombre, Guid orgId)
    {
        var e = (Shapi.Dominio.Apis.Api)Activator.CreateInstance(typeof(Shapi.Dominio.Apis.Api), true)!;
        typeof(Shapi.Dominio.Apis.Api).GetProperty("Nombre")!.SetValue(e, nombre);
        typeof(Shapi.Dominio.Apis.Api).GetProperty("OrganizacionId")!.SetValue(e, orgId);
        typeof(Shapi.Dominio.Apis.Api).GetProperty("Subdominio")!.SetValue(e, Guid.NewGuid().ToString("N")[..20]);
        typeof(Shapi.Dominio.Apis.Api).GetProperty("UrlOrigen")!.SetValue(e, "https://ejemplo.com");
        typeof(Shapi.Dominio.Apis.Api).GetProperty("SecretoOrigenCifrado")!.SetValue(e, "secreto");
        return e;
    }
}

public class ContextoPrueba : Shapi.Aplicacion.Comun.IContextoOrganizacion
{
    public Guid? OrganizacionId { get; set; }
}
