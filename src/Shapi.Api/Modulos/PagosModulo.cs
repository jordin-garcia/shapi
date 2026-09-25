namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Pagos. Solo lo edita su dueño (convenciones §2).</summary>
public static class PagosModulo
{
    public static IServiceCollection AgregarModuloPagos(this IServiceCollection services)
    {
        services.AddScoped<Shapi.Aplicacion.Pagos.IPasarelaPagos, Shapi.Infraestructura.Pagos.PasarelaSimulada>();
        return services;
    }

    public static WebApplication MapearModuloPagos(this WebApplication app)
    {
        return app;
    }
}
