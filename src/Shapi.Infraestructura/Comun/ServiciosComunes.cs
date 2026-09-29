using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shapi.Aplicacion.Comun;
using Shapi.Infraestructura.Bitacora;
using Shapi.Infraestructura.Correo;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Comun;

public static class ServiciosComunes
{
    /// <summary>Directorio del anillo de llaves de Data Protection, compartido por la API y el trabajador (10 §3).</summary>
    public const string VariableDirectorioLlaves = "SHAPI_DPKEYS_DIR";

    /// <summary>
    /// Registra el reloj, <see cref="IColaCorreo"/>, <see cref="IBitacora"/> e <see cref="IProtectorSecretoOrigen"/>.
    /// <see cref="IPublicadorCache"/> lo registra <c>AgregarCacheRedis</c> (JG-04). El DbContext se registra siempre;
    /// si falta SHAPI_POSTGRES_CADENA no explota al arrancar (solo al usarse).
    /// </summary>
    public static IServiceCollection AgregarServiciosComunes(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IReloj, RelojSistema>();

        // Cola de correo y bitácora reales (Scoped porque dependen del DbContext)
        services.AddScoped<IColaCorreo, ColaCorreoBaseDatos>();
        services.AddScoped<IBitacora, BitacoraBaseDatos>();

        // Mismo nombre de aplicación y mismo directorio en la API y en el trabajador, para que el trabajador descifre
        // los secretos que cifró la API (10 §3).
        var dataProtection = services.AddDataProtection().SetApplicationName("Shapi");
        var directorioLlaves = configuration[VariableDirectorioLlaves];
        if (!string.IsNullOrWhiteSpace(directorioLlaves))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(directorioLlaves));
        }

        services.AddSingleton<IProtectorSecretoOrigen, ProtectorSecretoOrigen>();

        services.TryAddScoped<IContextoOrganizacion, ContextoOrganizacionNulo>();

        // Registrar DbContext sin lanzar excepción si falta la cadena de conexión.
        // La excepción ocurrirá cuando realmente se use la BD, no al registrar.
        services.AddDbContext<ShapiDbContext>((proveedor, options) =>
        {
            var cs = configuration["SHAPI_POSTGRES_CADENA"];
            if (!string.IsNullOrEmpty(cs))
            {
                options.UseNpgsql(cs);
            }
            else
            {
                options.UseNpgsql("Host=no-configurado;Database=shapi");
            }

            // creado_en y actualizado_en con la hora de IReloj (07 §3).
            options.AddInterceptors(new InterceptorFechasAuditoria(proveedor.GetRequiredService<IReloj>()));
        });

        return services;
    }
}
