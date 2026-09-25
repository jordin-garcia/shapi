using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shapi.Aplicacion.Comun;
<<<<<<< HEAD
using Shapi.Infraestructura.Bitacora;
using Shapi.Infraestructura.Correo;
=======
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Comun;

public static class ServiciosComunes
{
    /// <summary>
    /// Registra el reloj, <see cref="IColaCorreo"/>, <see cref="IBitacora"/>
    /// e <see cref="IPublicadorCache"/>. El DbContext se registra siempre;
    /// si falta SHAPI_POSTGRES_CADENA no explota al arrancar (solo al usarse).
    /// </summary>
    public static IServiceCollection AgregarServiciosComunes(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IReloj, RelojSistema>();

        // Cola de correo y bitácora reales (Scoped porque dependen del DbContext)
        services.AddScoped<IColaCorreo, ColaCorreoBaseDatos>();
        services.AddScoped<IBitacora, BitacoraBaseDatos>();

        services.AddSingleton<IPublicadorCache, PublicadorCacheNulo>();

        services.TryAddScoped<IContextoOrganizacion, ContextoOrganizacionNulo>();

        // Registrar DbContext sin lanzar excepción si falta la cadena de conexión.
        // La excepción ocurrirá cuando realmente se use la BD, no al registrar.
        services.AddDbContext<ShapiDbContext>(options =>
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
        });

        return services;
    }
}
