namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Apis. Solo lo edita su dueño (convenciones §2).</summary>
public static class ApisModulo
{
    public static IServiceCollection AgregarModuloApis(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloApis(this WebApplication app)
    {
        return app;
    }
}
