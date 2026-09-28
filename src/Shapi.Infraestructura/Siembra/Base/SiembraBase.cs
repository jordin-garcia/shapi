using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shapi.Dominio.Identidad;
using Shapi.Dominio.Organizaciones;
using Shapi.Dominio.Planes;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Siembra.Base;

public static class SiembraBase
{
    public static async Task EjecutarAsync(ShapiDbContext db, string? adminCorreo, string? adminNombre, string? adminContrasena, Shapi.Aplicacion.Comun.IReloj reloj, Microsoft.AspNetCore.Identity.IPasswordHasher<Usuario> hasher, Microsoft.Extensions.Logging.ILogger logger)
    {
        await db.Database.MigrateAsync();

        var orgPlataforma = await db.Set<Organizacion>().IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Tipo == TipoOrganizacion.Plataforma);
        if (orgPlataforma == null)
        {
            orgPlataforma = new Organizacion("Shapi", TipoOrganizacion.Plataforma);
            db.Set<Organizacion>().Add(orgPlataforma);
            await db.SaveChangesAsync();
        }

        await SembrarPlanesAsync(db);

        if (string.IsNullOrWhiteSpace(adminCorreo) || string.IsNullOrWhiteSpace(adminNombre) || string.IsNullOrWhiteSpace(adminContrasena))
        {
            logger.LogWarning("Faltan las variables SHAPI_ADMIN_*; no se creará el administrador inicial.");
            return;
        }

        // El administrador y su membresía se crean juntos. Si una ejecución anterior dejó al usuario sin ninguna
        // membresía, se completa aquí, para que la siembra siga siendo idempotente. Un usuario que ya pertenece a
        // otra organización (por ejemplo, un proveedor registrado con ese correo) no se convierte en administrador.
        await using var transaccion = await db.Database.BeginTransactionAsync();
        var correo = Usuario.NormalizarCorreo(adminCorreo);
        var admin = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == correo);
        if (admin == null)
        {
            admin = new Usuario(adminNombre, correo);
            admin.DefinirHashContrasena(hasher.HashPassword(admin, adminContrasena));
            admin.VerificarCorreo(reloj.Ahora);
            db.Set<Usuario>().Add(admin);
        }

        var membresias = await db.Set<Membresia>().IgnoreQueryFilters().Where(m => m.UsuarioId == admin.Id).ToListAsync();
        if (membresias.Count == 0)
        {
            // Si una ejecución anterior se cayó a medias, el administrador también puede haber quedado sin contraseña.
            if (admin.HashContrasena is null)
            {
                admin.DefinirHashContrasena(hasher.HashPassword(admin, adminContrasena));
            }

            admin.VerificarCorreo(reloj.Ahora);
            db.Set<Membresia>().Add(new Membresia(admin.Id, orgPlataforma.Id, Rol.Administrador));
        }
        else if (!membresias.Any(m => m.OrganizacionId == orgPlataforma.Id))
        {
            logger.LogWarning("El correo de SHAPI_ADMIN_CORREO ya pertenece a otra organización; no se creó el administrador inicial.");
        }

        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
    }

    private static async Task SembrarPlanesAsync(ShapiDbContext db)
    {
        var planesExistentes = await db.Set<PlanPlataforma>().IgnoreQueryFilters().ToListAsync();

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
