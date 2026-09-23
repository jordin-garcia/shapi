namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Pagos. Solo lo edita su dueño (convenciones §2).</summary>
public static class PagosModulo
{
    public static IServiceCollection AgregarModuloPagos(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloPagos(this WebApplication app)
    {
        return app;
    }
}
