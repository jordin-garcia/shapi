using FluentValidation;
using Shapi.Api.Organizaciones;
using Shapi.Aplicacion.Organizaciones;

namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Organizaciones. Solo lo edita su dueño (convenciones §2).</summary>
public static class OrganizacionesModulo
{
    public static IServiceCollection AgregarModuloOrganizaciones(this IServiceCollection services)
    {
        services.AddScoped<IValidator<PeticionInvitarMiembro>, ValidadorInvitarMiembro>();
        services.AddScoped<IValidator<PeticionCambiarRolMiembro>, ValidadorCambiarRolMiembro>();
        services.AddScoped<IValidator<PeticionAceptarInvitacionMiembro>, ValidadorAceptarInvitacionMiembro>();
        return services;
    }

    public static WebApplication MapearModuloOrganizaciones(this WebApplication app)
    {
        app.MapearEndpointsMiembros();
        return app;
    }
}
