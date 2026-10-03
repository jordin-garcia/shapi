namespace Shapi.Api.Modulos;

using Shapi.Api.Suscripciones;

/// <summary>Registro del módulo Suscripciones. Solo lo edita su dueño (convenciones §2).</summary>
public static class SuscripcionesModulo
{
    public static IServiceCollection AgregarModuloSuscripciones(this IServiceCollection services)
    {
        services.AddScoped<ContratacionApi>();
        return services;
    }

    public static WebApplication MapearModuloSuscripciones(this WebApplication app)
    {
        app.MapearEndpointsSuscripciones();
        return app;
    }
}
