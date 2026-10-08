using Shapi.Api.Soporte;

namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Soporte. Solo lo edita su dueño (convenciones §2).</summary>
public static class SoporteModulo
{
    public static IServiceCollection AgregarModuloSoporte(this IServiceCollection services)
    {
        services.AddScoped<ListarCasos>();
        services.AddScoped<ListarOrganizacionesParaCaso>();
        services.AddScoped<AbrirCaso>();
        services.AddScoped<ConsultarCaso>();
        services.AddScoped<ResponderCaso>();
        services.AddScoped<AsignarCaso>();
        services.AddScoped<CerrarCaso>();
        services.AddScoped<ConsultarOrganizacionCaso>();
        services.AddScoped<NotificadorCasos>();
        return services;
    }

    public static WebApplication MapearModuloSoporte(this WebApplication app)
    {
        app.MapearEndpointsSoporte();
        return app;
    }
}
