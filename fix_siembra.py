with open('src/Shapi.Infraestructura/Siembra/Base/SiembraBase.cs', 'r') as f:
    content = f.read()

# Add ILogger logger to parameters
content = content.replace(
    'public static async Task EjecutarAsync(ShapiDbContext db, string? adminCorreo, string? adminNombre, string? adminContrasena, Shapi.Aplicacion.Comun.IReloj reloj, Microsoft.AspNetCore.Identity.IPasswordHasher<Usuario> hasher)',
    'public static async Task EjecutarAsync(ShapiDbContext db, string? adminCorreo, string? adminNombre, string? adminContrasena, Shapi.Aplicacion.Comun.IReloj reloj, Microsoft.AspNetCore.Identity.IPasswordHasher<Usuario> hasher, Microsoft.Extensions.Logging.ILogger logger)'
)

# Replace the admin section
admin_logic = """if (!string.IsNullOrWhiteSpace(adminCorreo) && !string.IsNullOrWhiteSpace(adminNombre) && !string.IsNullOrWhiteSpace(adminContrasena))
        {
            var admin = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == adminCorreo);"""

new_admin_logic = """if (string.IsNullOrWhiteSpace(adminCorreo) || string.IsNullOrWhiteSpace(adminNombre) || string.IsNullOrWhiteSpace(adminContrasena))
        {
            logger.LogWarning("Faltan las variables SHAPI_ADMIN_*; no se creará el administrador inicial.");
        }
        else
        {
            adminCorreo = adminCorreo.Trim().ToLowerInvariant();
            var admin = await db.Set<Usuario>().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Correo == adminCorreo);"""

content = content.replace(admin_logic, new_admin_logic)

with open('src/Shapi.Infraestructura/Siembra/Base/SiembraBase.cs', 'w') as f:
    f.write(content)
