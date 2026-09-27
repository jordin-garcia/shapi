using System.Globalization;
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
        await context.Response.WriteAsJsonAsync(new { error = "Secreto de origen inválido." });
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

    // La tarifa sale de la misma tabla que /tarifas: precio_base + precio_por_kg × peso_kg.
    var urgente = string.Equals(solicitud.TipoServicio, "urgente", StringComparison.OrdinalIgnoreCase);
    var servicio = urgente ? Tarifas.Urgente : Tarifas.Normal;
    var monto = Math.Round(
        servicio.PrecioBase + (servicio.PrecioPorKg * solicitud.PesoKg),
        2,
        MidpointRounding.AwayFromZero);
    var tarifa = $"Q {monto.ToString("#,##0.00", CultureInfo.InvariantCulture)}";
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
    tarifas = new[] { Tarifas.Normal, Tarifas.Urgente }.Select(servicio => new
    {
        tipo_servicio = servicio.TipoServicio,
        precio_base = servicio.PrecioBase,
        precio_por_kg = servicio.PrecioPorKg,
    }),
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
    /// <summary>Punto de entrada usado por las pruebas de integración.</summary>
    public sealed class EnviosXelajuAplicacion;

    internal sealed record Tarifa(string TipoServicio, decimal PrecioBase, decimal PrecioPorKg);

    /// <summary>Tarifas de la demostración. Con 2.5 kg, la normal da los Q 38.50 de los mockups.</summary>
    internal static class Tarifas
    {
        public static Tarifa Normal { get; } = new("normal", 18.00m, 8.20m);

        public static Tarifa Urgente { get; } = new("urgente", 27.00m, 12.30m);
    }

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
