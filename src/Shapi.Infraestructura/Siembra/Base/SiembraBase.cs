using Microsoft.EntityFrameworkCore;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Siembra.Base;

public static class SiembraBase
{
    public static async Task EjecutarAsync(ShapiDbContext db, string? adminCorreo, string? adminNombre, string? adminContrasena)
    {
        await db.Database.MigrateAsync();

        var plataformaOrgId = Guid.Empty;
        var orgPlataforma = await db.Set<Organizacion>().FirstOrDefaultAsync(o => o.Tipo == TipoOrganizacion.Plataforma);
        if (orgPlataforma == null)
        {
            orgPlataforma = (Organizacion)Activator.CreateInstance(typeof(Organizacion), true)!;
            typeof(Organizacion).GetProperty("Nombre")!.SetValue(orgPlataforma, "Shapi");
            typeof(Organizacion).GetProperty("Tipo")!.SetValue(orgPlataforma, TipoOrganizacion.Plataforma);
            db.Set<Organizacion>().Add(orgPlataforma);
            await db.SaveChangesAsync();
        }
        plataformaOrgId = orgPlataforma.Id;

        await SembrarPlanesAsync(db);

        if (!string.IsNullOrWhiteSpace(adminCorreo) && !string.IsNullOrWhiteSpace(adminNombre) && !string.IsNullOrWhiteSpace(adminContrasena))
        {
            var admin = await db.Set<Usuario>().FirstOrDefaultAsync(u => u.Correo == adminCorreo);
            if (admin == null)
            {
                admin = (Usuario)Activator.CreateInstance(typeof(Usuario), true)!;
                typeof(Usuario).GetProperty("Nombre")!.SetValue(admin, adminNombre);
                typeof(Usuario).GetProperty("Correo")!.SetValue(admin, adminCorreo);
                typeof(Usuario).GetProperty("HashContrasena")!.SetValue(admin, adminContrasena);
                typeof(Usuario).GetProperty("CorreoVerificadoEn")!.SetValue(admin, DateTimeOffset.UtcNow);
                db.Set<Usuario>().Add(admin);
                await db.SaveChangesAsync();

                var membresia = (Membresia)Activator.CreateInstance(typeof(Membresia), true)!;
                typeof(Membresia).GetProperty("UsuarioId")!.SetValue(membresia, admin.Id);
                typeof(Membresia).GetProperty("OrganizacionId")!.SetValue(membresia, plataformaOrgId);
                typeof(Membresia).GetProperty("Rol")!.SetValue(membresia, Rol.Administrador);
                db.Set<Membresia>().Add(membresia);
                await db.SaveChangesAsync();
            }
        }
    }

    private static async Task SembrarPlanesAsync(ShapiDbContext db)
    {
        var planesExistentes = await db.Set<PlanPlataforma>().ToListAsync();

        var planes = new List<PlanPlataforma>
        {
            CrearPlan("Prueba", "Publicar una primera API sin costo", 0m, 30, 1, 1, 10000, false, true, true, 1),
            CrearPlan("Lanzamiento", "Empezar a cobrar por una API existente", 199m, 30, 3, 3, 250000, false, false, true, 2),
            CrearPlan("Producto", "Proveedores con clientes establecidos", 599m, 30, 10, 10, 2000000, true, false, true, 3),
            CrearPlan("Escala mensual", "Varias APIs y alto volumen", 1500m, 30, null, null, 10000000, true, false, true, 4),
            CrearPlan("Escala anual", "Alto volumen, con pago anual", 15000m, 365, null, null, 120000000, true, false, true, 5)
        };

        foreach (var plan in planes)
        {
            if (!planesExistentes.Any(p => p.Nombre == plan.Nombre))
            {
                db.Set<PlanPlataforma>().Add(plan);
            }
        }

        await db.SaveChangesAsync();
    }

    private static PlanPlataforma CrearPlan(string nombre, string descripcion, decimal precio, int vigenciaDias, int? maxApis, int? maxMiembros, long cuota, bool dominio, bool esPrueba, bool activo, int orden)
    {
        var plan = (PlanPlataforma)Activator.CreateInstance(typeof(PlanPlataforma), true)!;
        typeof(PlanPlataforma).GetProperty("Nombre")!.SetValue(plan, nombre);
        typeof(PlanPlataforma).GetProperty("Descripcion")!.SetValue(plan, descripcion);
        typeof(PlanPlataforma).GetProperty("Precio")!.SetValue(plan, precio);
        typeof(PlanPlataforma).GetProperty("VigenciaDias")!.SetValue(plan, vigenciaDias);
        typeof(PlanPlataforma).GetProperty("MaxApis")!.SetValue(plan, maxApis);
        typeof(PlanPlataforma).GetProperty("MaxMiembros")!.SetValue(plan, maxMiembros);
        typeof(PlanPlataforma).GetProperty("CuotaPeticiones")!.SetValue(plan, cuota);
        typeof(PlanPlataforma).GetProperty("DominioPropio")!.SetValue(plan, dominio);
        typeof(PlanPlataforma).GetProperty("EsPrueba")!.SetValue(plan, esPrueba);
        typeof(PlanPlataforma).GetProperty("Activo")!.SetValue(plan, activo);
        typeof(PlanPlataforma).GetProperty("Orden")!.SetValue(plan, orden);
        return plan;
    }
}
