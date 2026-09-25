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

if (builder.Configuration.GetValue<bool>("SHAPI_APLICAR_MIGRACIONES"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<Shapi.Infraestructura.Persistencia.ShapiDbContext>();
    var correo = builder.Configuration["SHAPI_ADMIN_CORREO"];
    var nombre = builder.Configuration["SHAPI_ADMIN_NOMBRE"];
    var contrasena = builder.Configuration["SHAPI_ADMIN_CONTRASENA"];
    
    await Shapi.Infraestructura.Siembra.Base.SiembraBase.EjecutarAsync(db, correo, nombre, contrasena);
}

app.Run();

/// <summary>Punto de entrada; es público para las pruebas con WebApplicationFactory.</summary>
public partial class Program;
