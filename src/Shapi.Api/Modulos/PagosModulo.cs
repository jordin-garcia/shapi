namespace Shapi.Api.Modulos;

using Shapi.Api.Pagos;

/// <summary>Registro del módulo Pagos. Solo lo edita su dueño (convenciones §2).</summary>
public static class PagosModulo
{
    public static IServiceCollection AgregarModuloPagos(this IServiceCollection services)
    {
        services.AddScoped<Shapi.Aplicacion.Pagos.IPasarelaPagos, Shapi.Infraestructura.Pagos.PasarelaSimulada>();
        services.AddScoped<Shapi.Aplicacion.Pagos.IConsultaPagos, Shapi.Infraestructura.Pagos.ConsultaPagos>();
        services.AddScoped<Shapi.Aplicacion.Pagos.ConsultarPagos>();
        services.AddScoped<AdministrarPagosPlataforma>();
        return services;
    }

    public static WebApplication MapearModuloPagos(this WebApplication app)
    {
        app.MapearEndpointsPagos();
        return app;
    }
}
