namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Correo. Solo lo edita su dueño (convenciones §2).</summary>
public static class CorreoModulo
{
    public static IServiceCollection AgregarModuloCorreo(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloCorreo(this WebApplication app)
    {
        return app;
    }
}
