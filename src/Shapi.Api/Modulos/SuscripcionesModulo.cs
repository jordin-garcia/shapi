namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Suscripciones. Solo lo edita su dueño (convenciones §2).</summary>
public static class SuscripcionesModulo
{
    public static IServiceCollection AgregarModuloSuscripciones(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloSuscripciones(this WebApplication app)
    {
        return app;
    }
}
