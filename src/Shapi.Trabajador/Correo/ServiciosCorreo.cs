using Shapi.Infraestructura.Correo;

namespace Shapi.Trabajador.Correo;

public static class ServiciosCorreo
{
    public static IServiceCollection AgregarProcesamientoCorreo(this IServiceCollection services)
    {
        services.AddSingleton<MotorPlantillasCorreo>();
        services.AddTransient<IEnviadorCorreo, EnviadorSmtp>();
        services.AddScoped<ProcesadorCorreos>();
        services.AddHostedService<DespachadorCorreos>();
        return services;
    }
}
