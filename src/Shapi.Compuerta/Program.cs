// Plano de datos (08): tubería de filtros y reenvío con YARP.
using Shapi.Compuerta;
using Shapi.Compuerta.Demo;

var builder = WebApplication.CreateBuilder(args);

// Comando temporal de la demostración del Avance 1; lo elimina JG-04.
if (args is [SembradoDemo.Comando, ..])
{
    return await SembradoDemo.EjecutarAsync(builder.Configuration, Console.Out);
}

builder.Services.AddHealthChecks();
builder.Services.AgregarCompuerta();

var app = builder.Build();

// Solo por localhost, para no tapar una ruta /salud de las APIs de los proveedores.
app.MapHealthChecks("/salud").RequireHost("localhost");

// Todo lo demás es tráfico de las APIs publicadas.
app.Map("/{**ruta}", (RequestDelegate)(http => http.RequestServices.GetRequiredService<TuberiaCompuerta>().ProcesarAsync(http)));

await app.RunAsync();
return 0;

/// <summary>Punto de entrada; es público para las pruebas con WebApplicationFactory.</summary>
public partial class Program;
