using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using OrigenesDemo.EnviosXelaju;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Use(async (context, siguiente) =>
{
    var secretoEsperado = app.Configuration["SECRETO_ORIGEN"];
    if (!string.IsNullOrEmpty(secretoEsperado)
        && !EsSecretoValido(context.Request.Headers["X-Shapi-Secreto"].ToString(), secretoEsperado))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Secreto de origen invalido." });
        return;
    }

    await siguiente(context);
});

app.MapPost("/cotizaciones", (CotizacionSolicitud solicitud) =>
{
    if (string.IsNullOrWhiteSpace(solicitud.Origen)
        || string.IsNullOrWhiteSpace(solicitud.Destino)
        || solicitud.PesoKg <= 0)
    {
        return Results.BadRequest(new { error = "Origen, destino y peso_kg son obligatorios." });
    }

    var urgente = string.Equals(solicitud.TipoServicio, "urgente", StringComparison.OrdinalIgnoreCase);
    var tarifa = urgente ? "Q 57.75" : "Q 38.50";
    var entrega = urgente ? "1 día hábil" : "2 días hábiles";

    return Results.Ok(new
    {
        origen = NombreMunicipio(solicitud.Origen),
        destino = NombreMunicipio(solicitud.Destino),
        tarifa,
        entrega_estimada = entrega,
    });
});

app.MapPost("/guias", (GuiaSolicitud solicitud) =>
{
    if (string.IsNullOrWhiteSpace(solicitud.Origen)
        || string.IsNullOrWhiteSpace(solicitud.Destino)
        || string.IsNullOrWhiteSpace(solicitud.Destinatario)
        || string.IsNullOrWhiteSpace(solicitud.Direccion)
        || solicitud.PesoKg <= 0)
    {
        return Results.BadRequest(new { error = "Los datos de la guía son obligatorios." });
    }

    return Results.Ok(new
    {
        numero_guia = "GX-2026-0001",
        estado = "creada",
        origen = NombreMunicipio(solicitud.Origen),
        destino = NombreMunicipio(solicitud.Destino),
        destinatario = solicitud.Destinatario,
    });
});

app.MapGet("/tarifas", () => Results.Ok(new
{
    moneda = "GTQ",
    tarifas = new[]
    {
        new { tipo_servicio = "normal", precio_base = 18.00m, precio_por_kg = 8.20m },
        new { tipo_servicio = "urgente", precio_base = 27.00m, precio_por_kg = 12.30m },
    },
}));

app.MapGet("/rastreo", (string guia) => Results.Ok(new
{
    numero_guia = guia,
    estado = "en_transito",
    eventos = new[]
    {
        new { fecha = "2026-09-10T08:30:00-06:00", descripcion = "Guía creada", ubicacion = "Quetzaltenango" },
        new { fecha = "2026-09-10T15:45:00-06:00", descripcion = "Paquete en tránsito", ubicacion = "Chimaltenango" },
    },
}));

app.MapGet("/cobertura", (string municipio) => Results.Ok(new
{
    municipio,
    nombre = NombreMunicipio(municipio),
    cubierto = municipio is "0901" or "0301" or "0101",
    dias_habiles = municipio == "0901" ? 1 : 2,
}));

app.MapGet("/salud", () => Results.Ok(new { estado = "saludable" }));

app.Run();

static string NombreMunicipio(string codigo) => codigo switch
{
    "0901" => "Quetzaltenango",
    "0301" => "Antigua Guatemala",
    "0101" => "Ciudad de Guatemala",
    _ => codigo,
};

static bool EsSecretoValido(string recibido, string esperado)
{
    var bytesRecibidos = Encoding.UTF8.GetBytes(recibido);
    var bytesEsperados = Encoding.UTF8.GetBytes(esperado);
    return bytesRecibidos.Length == bytesEsperados.Length
        && CryptographicOperations.FixedTimeEquals(bytesRecibidos, bytesEsperados);
}

namespace OrigenesDemo.EnviosXelaju
{
    /// <summary>Punto de entrada usado por las pruebas de integracion.</summary>
    public sealed class EnviosXelajuAplicacion;

    public sealed record CotizacionSolicitud(
        string Origen,
        string Destino,
        [property: JsonPropertyName("peso_kg")] decimal PesoKg,
        [property: JsonPropertyName("tipo_servicio")] string? TipoServicio);

    public sealed record GuiaSolicitud(
        string Origen,
        string Destino,
        string Destinatario,
        string Direccion,
        [property: JsonPropertyName("peso_kg")] decimal PesoKg);
}
