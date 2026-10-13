using Shapi.Api.Estado;
using Shapi.Aplicacion.Estado;
using Shapi.Infraestructura.Estado;

namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Estado. Solo lo edita su dueño (convenciones §2).</summary>
public static class EstadoModulo
{
    public static IServiceCollection AgregarModuloEstado(this IServiceCollection services)
    {
        services.AddSingleton<CacheEstado>();
        services.AddScoped<ConsultarEstado>();
        services.AddScoped<IObservadorEstado, ObservadorEstado>();
        services.AddHttpClient("salud-api", cliente => cliente.Timeout = TimeSpan.FromSeconds(2));
        return services;
    }

    public static WebApplication MapearModuloEstado(this WebApplication app)
    {
        app.MapearEndpointsEstado();
        return app;
    }
}
