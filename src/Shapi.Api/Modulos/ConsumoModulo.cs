using Shapi.Api.Consumo;
using Shapi.Aplicacion.Consumo;
using Shapi.Infraestructura.Consumo;

namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Consumo. Solo lo edita su dueño (convenciones §2).</summary>
public static class ConsumoModulo
{
    public static IServiceCollection AgregarModuloConsumo(this IServiceCollection services)
    {
        services.AddScoped<ConsultarConsumoApi>();
        return services.AgregarConsumo();
    }

    public static WebApplication MapearModuloConsumo(this WebApplication app)
    {
        app.MapearEndpointsConsumo();
        return app;
    }
}
