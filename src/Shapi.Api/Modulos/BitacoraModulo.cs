namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Bitacora. Solo lo edita su dueño (convenciones §2).</summary>
public static class BitacoraModulo
{
    public static IServiceCollection AgregarModuloBitacora(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloBitacora(this WebApplication app)
    {
        return app;
    }
}
