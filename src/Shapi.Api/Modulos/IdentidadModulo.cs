namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Identidad. Solo lo edita su dueño (convenciones §2).</summary>
public static class IdentidadModulo
{
    public static IServiceCollection AgregarModuloIdentidad(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloIdentidad(this WebApplication app)
    {
        return app;
    }
}
