using Shapi.Infraestructura.Comun;
using Shapi.Trabajador.Correo;

// Procesos en segundo plano (06 §3): cada trabajo es un BackgroundService en su carpeta.
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AgregarServiciosComunes(builder.Configuration);
builder.Services.AgregarProcesamientoCorreo();

var host = builder.Build();
host.Run();
