using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shapi.Aplicacion.Comun;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Comun;

public static class ServiciosComunes
{
    /// <summary>
    /// Registra el reloj y las implementaciones nulas de <see cref="IColaCorreo"/>, <see cref="IBitacora"/>
    /// e <see cref="IPublicadorCache"/>. Se llama antes que los módulos: el módulo dueño las reemplaza
    /// registrando la suya después o con <c>services.Replace(...)</c>.
    /// </summary>
    public static IServiceCollection AgregarServiciosComunes(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IReloj, RelojSistema>();
        services.AddSingleton<IColaCorreo, ColaCorreoNula>();
        services.AddSingleton<IBitacora, BitacoraNula>();
        services.AddSingleton<IPublicadorCache, PublicadorCacheNulo>();

        services.TryAddScoped<IContextoOrganizacion, ContextoOrganizacionNulo>();

        var connectionString = configuration["SHAPI_POSTGRES_CADENA"] ?? throw new InvalidOperationException("Falta la cadena de conexión 'SHAPI_POSTGRES_CADENA'.");
        services.AddDbContext<ShapiDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
