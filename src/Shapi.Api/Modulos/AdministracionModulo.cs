namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Administracion. Solo lo edita su dueño (convenciones §2).</summary>
public static class AdministracionModulo
{
    public static IServiceCollection AgregarModuloAdministracion(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloAdministracion(this WebApplication app)
    {
        return app;
    }
}
