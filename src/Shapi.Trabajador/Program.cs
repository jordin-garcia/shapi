using Shapi.Infraestructura.Comun;
using Shapi.Infraestructura.Siembra.Demo;
using Shapi.Trabajador.Consolidacion;
using Shapi.Trabajador.Correo;
using Shapi.Trabajador.Resincronizacion;
using Shapi.Trabajador.Siembra;

// Procesos en segundo plano (06 §3): cada trabajo es un BackgroundService en su carpeta.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AgregarServiciosComunes(builder.Configuration);
builder.Services.AgregarProcesamientoCorreo();
builder.Services.AgregarResincronizacion();
builder.Services.AgregarConsolidacion();
builder.Services.AddScoped<SiembraDemo>();

var host = builder.Build();

if (ComandoSembrarDemo.EsElComando(args))
{
    await using var alcance = host.Services.CreateAsyncScope();
    await ComandoSembrarDemo.EjecutarAsync(
        args,
        alcance.ServiceProvider.GetRequiredService<IConfiguration>(),
        builder.Environment.IsProduction(),
        (modoDemo, reiniciar, envios, agro, secretoEnvios, secretoAgro) =>
            alcance.ServiceProvider.GetRequiredService<SiembraDemo>().EjecutarAsync(
                modoDemo, reiniciar, envios, agro, secretoEnvios, secretoAgro),
        () => alcance.ServiceProvider.GetRequiredService<ResincronizarCache>().EjecutarAsync(CancellationToken.None));
    return;
}

host.Run();

/// <summary>
/// Interno a propósito: con el marco de ASP.NET Core (Data Protection, 10 §3), .NET 10 haría pública esta clase y
/// chocaría con el <c>Program</c> de la API en las pruebas.
/// </summary>
internal partial class Program;
