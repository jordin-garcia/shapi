using Shapi.Infraestructura.Comun;
using Shapi.Trabajador.Correo;
using Shapi.Trabajador.Resincronizacion;

// Procesos en segundo plano (06 §3): cada trabajo es un BackgroundService en su carpeta.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AgregarServiciosComunes(builder.Configuration);
builder.Services.AgregarProcesamientoCorreo();
builder.Services.AgregarResincronizacion();

var host = builder.Build();
host.Run();

/// <summary>
/// Interno a propósito: con el marco de ASP.NET Core (Data Protection, 10 §3), .NET 10 haría pública esta clase y
/// chocaría con el <c>Program</c> de la API en las pruebas.
/// </summary>
internal partial class Program;
