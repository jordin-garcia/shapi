using FluentValidation;
using Shapi.Api.Apis;
using Shapi.Aplicacion.Apis;
using Shapi.Contratos.Red;
using Shapi.Infraestructura.Apis;

namespace Shapi.Api.Modulos;

/// <summary>Registro del módulo Apis. Solo lo edita su dueño (convenciones §2).</summary>
public static class ApisModulo
{
    public static IServiceCollection AgregarModuloApis(this IServiceCollection services)
    {
        services.AddSingleton<ValidadorDireccionOrigen>();
        services.AddSingleton(sp =>
        {
            var configuracion = sp.GetRequiredService<IConfiguration>();
            var modoDemo = bool.TryParse(configuracion["SHAPI_MODO_DEMO"], out var valor) && valor;
            var permitidos = (configuracion["SHAPI_ORIGENES_PERMITIDOS"]
                    ?? "localhost:5101,localhost:5102,origen-envios:8080,origen-agro:8080")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return new ConfiguracionProteccionOrigen(modoDemo, permitidos);
        });
        services.AddScoped<IValidator<SolicitudRegistroApi>, ValidadorSolicitudRegistroApi>();
        services.AddScoped<IRepositorioApis, RepositorioApis>();
        services.AddScoped<IProbadorOrigen, ProbadorOrigenHttp>();
        services.AddScoped<RegistrarApi>();
        services.AddScoped<ListarApis>();
        return services;
    }

    public static WebApplication MapearModuloApis(this WebApplication app)
    {
        app.MapearEndpointsApis();
        return app;
    }
}
