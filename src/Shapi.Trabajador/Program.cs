using Shapi.Infraestructura.Comun;

// Procesos en segundo plano (06 §3): cada trabajo es un BackgroundService en su carpeta.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AgregarServiciosComunes();

var host = builder.Build();
host.Run();
