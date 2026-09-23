namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Organizaciones. Solo lo edita su dueño (convenciones §2).</summary>
public static class OrganizacionesModulo
{
    public static IServiceCollection AgregarModuloOrganizaciones(this IServiceCollection services)
    {
        return services;
    }

    public static WebApplication MapearModuloOrganizaciones(this WebApplication app)
    {
        return app;
    }
}
