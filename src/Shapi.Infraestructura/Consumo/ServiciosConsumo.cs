using Microsoft.Extensions.DependencyInjection;
using Shapi.Aplicacion.Consumo;

namespace Shapi.Infraestructura.Consumo;

public static class ServiciosConsumo
{
    public static IServiceCollection AgregarConsumo(this IServiceCollection servicios)
    {
        servicios.AddScoped<RepositorioConsolidacion>();
        servicios.AddScoped<IConsultaConsumo, ConsultaConsumo>();
        return servicios;
    }
}
