namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Consumo. Solo lo edita su dueño (convenciones §2).</summary>
public static class ConsumoModulo
{
    public static IServiceCollection AgregarModuloConsumo(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloConsumo(this WebApplication app)
    {
        return app;
    }
}
