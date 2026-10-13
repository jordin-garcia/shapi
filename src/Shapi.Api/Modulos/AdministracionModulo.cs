using Shapi.Api.Administracion;
using Shapi.Aplicacion.Administracion;

namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Administracion. Solo lo edita su dueño (convenciones §2).</summary>
public static class AdministracionModulo
{
    public static IServiceCollection AgregarModuloAdministracion(this IServiceCollection services)
    {
        services.AddScoped<GestionarOrganizaciones>();
        services.AddScoped<IRepositorioOrganizacionesAdministracion, RepositorioOrganizacionesAdministracion>();
        services.AddSingleton<IEnlacesAdministracion, EnlacesAdministracion>();
        services.AddScoped<IRepositorioCuentasPlataforma, RepositorioCuentasPlataforma>();
        services.AddScoped<CrearCuentaPlataforma>();
        services.AddScoped<CambiarEstadoCuentaPlataforma>();
        services.AddScoped<DefinirContrasenaPlataforma>();
        services.AddScoped<FluentValidation.IValidator<NuevaCuentaPlataforma>, ValidadorCuentaPlataforma>();
        return services;
    }

    public static WebApplication MapearModuloAdministracion(this WebApplication app)
    {
        app.MapearEndpointsAdministracion();
        app.MapearCuentasPlataforma();
        return app;
    }
}
