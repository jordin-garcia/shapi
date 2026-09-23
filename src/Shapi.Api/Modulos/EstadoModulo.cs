namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Estado. Solo lo edita su dueño (convenciones §2).</summary>
public static class EstadoModulo
{
    public static IServiceCollection AgregarModuloEstado(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloEstado(this WebApplication app)
    {
        return app;
    }
}
