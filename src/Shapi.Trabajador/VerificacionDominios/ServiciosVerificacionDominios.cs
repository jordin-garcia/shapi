using Shapi.Aplicacion.Apis;
using Shapi.Infraestructura.Apis;

namespace Shapi.Trabajador.VerificacionDominios;

public static class ServiciosVerificacionDominios
{
    public static IServiceCollection AgregarVerificacionDominios(this IServiceCollection services, IConfiguration configuracion)
    {
        var modoSimulado = !string.Equals(configuracion["SHAPI_DNS_MODO"], "real", StringComparison.OrdinalIgnoreCase);
        services.AddSingleton(new ConfiguracionDns(modoSimulado,
            configuracion["SHAPI_DOMINIO_BASE"]?.Trim().TrimEnd('.').ToLowerInvariant() ?? "shapi.localhost"));
        services.AddScoped<ResolutorDnsSimulado>();
        services.AddSingleton<ResolutorDnsReal>();
        services.AddScoped<IResolutorDns>(sp => modoSimulado
            ? sp.GetRequiredService<ResolutorDnsSimulado>()
            : sp.GetRequiredService<ResolutorDnsReal>());
        services.AddScoped<VerificarDominiosPendientes>();
        services.AddSingleton(new OpcionesVerificacionDominios());
        services.AddHostedService<TrabajoVerificacionDominios>();
        return services;
    }
}

public sealed class OpcionesVerificacionDominios
{
    public TimeSpan Intervalo { get; init; } = TimeSpan.FromMinutes(5);
}
