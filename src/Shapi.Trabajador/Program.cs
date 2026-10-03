using Shapi.Infraestructura.Comun;
using Shapi.Infraestructura.Siembra.Demo;
using Shapi.Trabajador.Correo;
using Shapi.Trabajador.Resincronizacion;

// Procesos en segundo plano (06 §3): cada trabajo es un BackgroundService en su carpeta.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AgregarServiciosComunes(builder.Configuration);
builder.Services.AgregarProcesamientoCorreo();
builder.Services.AgregarResincronizacion();
builder.Services.AddScoped<SiembraDemo>();

var host = builder.Build();

if (args.FirstOrDefault()?.Equals("sembrar-demo", StringComparison.OrdinalIgnoreCase) == true)
{
    await using var alcance = host.Services.CreateAsyncScope();
    var configuracion = alcance.ServiceProvider.GetRequiredService<IConfiguration>();
    var modoDemo = bool.TryParse(configuracion["SHAPI_MODO_DEMO"], out var habilitado) && habilitado;
    var reiniciar = args.Any(a => a.Equals("--reiniciar", StringComparison.OrdinalIgnoreCase));
    var urls = ConfiguracionSiembraDemo.ResolverUrls(
        configuracion["SHAPI_URL_ORIGEN_ENVIOS"],
        configuracion["SHAPI_URL_ORIGEN_AGRO"],
        builder.Environment.IsProduction());
    await alcance.ServiceProvider.GetRequiredService<SiembraDemo>().EjecutarAsync(
        modoDemo,
        reiniciar,
        urls.Envios,
        urls.Agro,
        configuracion["SHAPI_SECRETO_ORIGEN_ENVIOS"],
        configuracion["SHAPI_SECRETO_ORIGEN_AGRO"]);
    await alcance.ServiceProvider.GetRequiredService<ResincronizarCache>().EjecutarAsync(CancellationToken.None);
    return;
}

host.Run();

/// <summary>
/// Interno a propósito: con el marco de ASP.NET Core (Data Protection, 10 §3), .NET 10 haría pública esta clase y
/// chocaría con el <c>Program</c> de la API en las pruebas.
/// </summary>
internal partial class Program;
