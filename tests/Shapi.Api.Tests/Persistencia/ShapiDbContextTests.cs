#pragma warning disable CS0618
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shapi.Dominio.Bitacora;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Infraestructura.Comun;
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
        if (_db != null) await _db.DisposeAsync();
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

        var entrada1 = CrearEntrada("Org 1", org1Id);
        var entrada2 = CrearEntrada("Org 2", org2Id);

        _db!.Set<EntradaBitacora>().AddRange(entrada1, entrada2);
        await _db.SaveChangesAsync();

        // Sin contexto, debería ver ambos
        _contextoOrganizacion.OrganizacionIdActual = null;
        var conteoTotal = await _db.Set<EntradaBitacora>().CountAsync();
        Assert.Equal(2, conteoTotal);

        // Con contexto, solo ve org1
        _contextoOrganizacion.OrganizacionIdActual = org1Id;
        var conteoOrg1 = await _db.Set<EntradaBitacora>().CountAsync();
        Assert.Equal(1, conteoOrg1);
        var itemOrg1 = await _db.Set<EntradaBitacora>().FirstAsync();
        Assert.Equal(org1Id, itemOrg1.OrganizacionId);
    }

    [Fact]
    public async Task SiembraBase_EsIdempotente()
    {
        await SiembraBase.EjecutarAsync(_db!, "admin@shapi.test", "Admin", "Contra123");
        var countPlanes = await _db!.Set<Shapi.Dominio.Planes.PlanPlataforma>().CountAsync();
        var countUsuarios = await _db!.Set<Usuario>().CountAsync();

        // Ejecutar de nuevo
        await SiembraBase.EjecutarAsync(_db!, "admin@shapi.test", "Admin", "Contra123");
        
        var countPlanes2 = await _db!.Set<Shapi.Dominio.Planes.PlanPlataforma>().CountAsync();
        var countUsuarios2 = await _db!.Set<Usuario>().CountAsync();

        Assert.Equal(countPlanes, countPlanes2);
        Assert.Equal(countUsuarios, countUsuarios2);
    }

    private EntradaBitacora CrearEntrada(string accion, Guid orgId)
    {
        var e = (EntradaBitacora)Activator.CreateInstance(typeof(EntradaBitacora), true)!;
        typeof(EntradaBitacora).GetProperty("ActorTipo")!.SetValue(e, ActorTipo.Sistema);
        typeof(EntradaBitacora).GetProperty("ActorNombre")!.SetValue(e, "Sis");
        typeof(EntradaBitacora).GetProperty("Accion")!.SetValue(e, accion);
        typeof(EntradaBitacora).GetProperty("Descripcion")!.SetValue(e, "Desc");
        typeof(EntradaBitacora).GetProperty("OrganizacionId")!.SetValue(e, orgId);
        return e;
    }
}

public class ContextoPrueba : IContextoOrganizacion
{
    public Guid? OrganizacionIdActual { get; set; }
}
