using System.Security.Cryptography;
using System.Text;
using OrigenesDemo.AgroPrecios;

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

app.MapGet("/precios", (string producto, string mercado, string? fecha) =>
{
    var precio = DatosAgro.BuscarPrecio(producto, mercado, fecha);
    return precio is null ? Results.NotFound() : Results.Ok(precio);
});

app.MapGet("/productos", () => Results.Ok(new
{
    productos = DatosAgro.Productos,
}));

app.MapGet("/mercados", () => Results.Ok(new
{
    mercados = DatosAgro.Mercados,
}));

app.MapGet("/historial", (string producto, string mercado, DateOnly? desde, DateOnly? hasta) => Results.Ok(new
{
    producto,
    mercado,
    desde = desde?.ToString("yyyy-MM-dd") ?? "2026-09-08",
    hasta = hasta?.ToString("yyyy-MM-dd") ?? "2026-09-10",
    precios = new[]
    {
        new { fecha = "2026-09-08", precio = "Q 505.00", moneda = "GTQ" },
        new { fecha = "2026-09-09", precio = "Q 508.00", moneda = "GTQ" },
        new { fecha = "2026-09-10", precio = "Q 510.00", moneda = "GTQ" },
    },
}));

app.MapGet("/salud", () => Results.Ok(new { estado = "saludable" }));

app.Run();

static bool EsSecretoValido(string recibido, string esperado)
{
    var bytesRecibidos = Encoding.UTF8.GetBytes(recibido);
    var bytesEsperados = Encoding.UTF8.GetBytes(esperado);
    return bytesRecibidos.Length == bytesEsperados.Length
        && CryptographicOperations.FixedTimeEquals(bytesRecibidos, bytesEsperados);
}

namespace OrigenesDemo.AgroPrecios
{
    /// <summary>Punto de entrada usado por las pruebas de integracion.</summary>
    public sealed class AgroPreciosAplicacion;

    internal static class DatosAgro
    {
        public static object[] Productos { get; } =
        [
            new { clave = "frijol_negro", nombre = "Frijol negro", categoria = "granos", unidad = "quintal" },
            new { clave = "tomate", nombre = "Tomate", categoria = "hortalizas", unidad = "caja" },
            new { clave = "banano", nombre = "Banano", categoria = "frutas", unidad = "ciento" },
        ];

        public static object[] Mercados { get; } =
        [
            new { clave = "cenma", nombre = "CENMA", municipio = "Villa Nueva" },
            new { clave = "terminal", nombre = "La Terminal", municipio = "Ciudad de Guatemala" },
            new { clave = "quetzaltenango", nombre = "La Democracia", municipio = "Quetzaltenango" },
        ];

        public static object? BuscarPrecio(string producto, string mercado, string? fecha)
        {
            if (!string.Equals(producto, "frijol_negro", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(mercado, "cenma", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new
            {
                producto = "Frijol negro",
                mercado = "CENMA",
                unidad = "quintal",
                precio = "Q 510.00",
                fecha = string.IsNullOrWhiteSpace(fecha) || fecha == "2026-09-10" ? "10 sep 2026" : fecha,
            };
        }
    }
}
