var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/salud", () => Results.Ok(new { estado = "saludable" }));

app.Run();

namespace OrigenesDemo.AgroPrecios
{
    /// <summary>Punto de entrada usado por las pruebas de integracion.</summary>
    public sealed class AgroPreciosAplicacion;
}
