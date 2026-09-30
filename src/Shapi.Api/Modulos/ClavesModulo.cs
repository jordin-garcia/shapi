using FluentValidation;
using Shapi.Api.Claves;
using Shapi.Aplicacion.Claves;
using Shapi.Infraestructura.Claves;

namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Claves. Solo lo edita su dueño (convenciones §2).</summary>
public static class ClavesModulo
{
    /// <summary><see cref="IServicioClaves"/>, que también usa la contratación (EM-08) para emitir las claves (RF-26).</summary>
    public static IServiceCollection AgregarModuloClaves(this IServiceCollection services)
    {
        services.AddScoped<IServicioClaves, ServicioClaves>();
        services.AddScoped<IValidator<PeticionEmitirClave>, ValidadorPeticionEmitirClave>();
        return services;
    }

    public static WebApplication MapearModuloClaves(this WebApplication app)
    {
        app.MapearEndpointsClaves();
        return app;
    }
}
