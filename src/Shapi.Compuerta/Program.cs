// Plano de datos (08). JG-02 agrega la tubería de filtros y el reenvío con YARP.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/salud");

app.Run();

/// <summary>Punto de entrada; es público para las pruebas con WebApplicationFactory.</summary>
public partial class Program;
