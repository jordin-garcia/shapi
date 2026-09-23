namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Claves. Solo lo edita su dueño (convenciones §2).</summary>
public static class ClavesModulo
{
    public static IServiceCollection AgregarModuloClaves(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloClaves(this WebApplication app)
    {
        return app;
    }
}
