using Shapi.Api.Modulos;
using Shapi.Infraestructura.Comun;

// Este archivo no se edita por módulo (convenciones §3): cada módulo se registra en Modulos/<Modulo>Modulo.cs.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AgregarServiciosComunes(builder.Configuration);

builder.Services
    .AgregarModuloIdentidad()
    .AgregarModuloOrganizaciones()
    .AgregarModuloPlanes()
    .AgregarModuloSuscripciones()
    .AgregarModuloPagos()
    .AgregarModuloApis()
    .AgregarModuloPortal()
    .AgregarModuloClaves()
    .AgregarModuloConsumo()
    .AgregarModuloCache()
    .AgregarModuloAdministracion()
    .AgregarModuloSoporte()
    .AgregarModuloBitacora()
    .AgregarModuloCorreo()
    .AgregarModuloEstado();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/salud");

app
    .MapearModuloIdentidad()
    .MapearModuloOrganizaciones()
    .MapearModuloPlanes()
    .MapearModuloSuscripciones()
    .MapearModuloPagos()
    .MapearModuloApis()
    .MapearModuloPortal()
    .MapearModuloClaves()
    .MapearModuloConsumo()
    .MapearModuloCache()
    .MapearModuloAdministracion()
    .MapearModuloSoporte()
    .MapearModuloBitacora()
    .MapearModuloCorreo()
    .MapearModuloEstado();

if (bool.TryParse(app.Configuration["SHAPI_APLICAR_MIGRACIONES"], out var aplicar) && aplicar || app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Shapi.Infraestructura.Persistencia.ShapiDbContext>();
    var correo = builder.Configuration["SHAPI_ADMIN_CORREO"];
    var nombre = builder.Configuration["SHAPI_ADMIN_NOMBRE"];
    var contrasena = builder.Configuration["SHAPI_ADMIN_CONTRASENA"];

<<<<<<< HEAD
    var reloj = scope.ServiceProvider.GetRequiredService<Shapi.Aplicacion.Comun.IReloj>();
    var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Shapi.Dominio.Identidad.Usuario>();

    var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
    await Shapi.Infraestructura.Siembra.Base.SiembraBase.EjecutarAsync(db, correo, nombre, contrasena, reloj, hasher, logger);
=======
    await Shapi.Infraestructura.Siembra.Base.SiembraBase.EjecutarAsync(db, correo, nombre, contrasena);
>>>>>>> 8cc28d6 (style: aplicar formato con dotnet format)
}

app.Run();

/// <summary>Punto de entrada; es público para las pruebas con WebApplicationFactory.</summary>
public partial class Program;
