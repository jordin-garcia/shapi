using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shapi.Aplicacion.Pagos;
using Shapi.Infraestructura.Pagos;
using Shapi.Trabajador.AvisosCuota;

namespace Shapi.Trabajador.CierreCiclo;

public static class ServiciosCierreCiclo
{
    public static IServiceCollection AgregarCierreCiclo(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddSingleton(new OpcionesCierreCiclo
        {
            Intervalo = TimeSpan.FromMinutes(1),
            DiasGracia = TimeSpan.FromDays(configuracion.GetValue("Cobros:DiasGracia", 7)),
            DiasAvisoPrueba = TimeSpan.FromDays(configuracion.GetValue("Cobros:AvisoVencimientoPruebaDias", 7)),
            DominioBase = configuracion["SHAPI_DOMINIO_BASE"] ?? "shapi.localhost",
        });
        servicios.AddScoped<IPasarelaPagos, PasarelaSimulada>();
        servicios.AddScoped<CerrarCiclo>();
        servicios.AddHostedService<TrabajoCierreCiclo>();
        servicios.AddScoped<AvisosCuotaPlataforma>();
        servicios.AddHostedService<TrabajoAvisosCuota>();
        return servicios;
    }
}
