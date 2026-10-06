using Microsoft.Extensions.DependencyInjection.Extensions;
using Shapi.Infraestructura.Cache;
using Shapi.Infraestructura.Consumo;

namespace Shapi.Trabajador.Consolidacion;

public static class ServiciosConsolidacion
{
    public static IServiceCollection AgregarConsolidacion(this IServiceCollection servicios)
    {
        servicios.AgregarCacheRedis();
        servicios.AgregarConsumo();
        servicios.TryAddSingleton(new OpcionesConsolidacion());
        servicios.AddSingleton<LotesMetricasRedis>();
        servicios.AddScoped<ConsolidarConsumo>();
        servicios.AddHostedService<TrabajoConsolidacion>();
        servicios.AddHostedService<LatidoTrabajador>();
        return servicios;
    }
}
