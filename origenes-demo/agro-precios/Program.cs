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
        await context.Response.WriteAsJsonAsync(new { error = "Secreto de origen inválido." });
        return;
    }

    await siguiente(context);
});

app.MapGet("/precios", (string producto, string mercado, DateOnly? fecha) =>
{
    var precio = DatosAgro.BuscarPrecio(producto, mercado, fecha ?? DatosAgro.FechaDelDia);
    return precio is null
        ? Results.NotFound(new { error = "No hay precio para ese producto, mercado y fecha." })
        : Results.Ok(precio);
});

app.MapGet("/productos", () => Results.Ok(new
{
    productos = DatosAgro.Productos.Select(producto => new
    {
        clave = producto.Clave,
        nombre = producto.Nombre,
        categoria = producto.Categoria,
        unidad = producto.Unidad,
    }),
}));

app.MapGet("/mercados", () => Results.Ok(new
{
    mercados = DatosAgro.Mercados.Select(mercado => new
    {
        clave = mercado.Clave,
        nombre = mercado.Nombre,
        municipio = mercado.Municipio,
    }),
}));

app.MapGet("/historial", (string producto, string mercado, DateOnly? desde, DateOnly? hasta) =>
{
    var fechaDesde = desde ?? new DateOnly(2026, 9, 8);
    var fechaHasta = hasta ?? DatosAgro.FechaDelDia;
    if (fechaDesde > fechaHasta)
    {
        return Results.BadRequest(new { error = "La fecha desde no puede ser posterior a la fecha hasta." });
    }

    var precios = DatosAgro.ObtenerHistorial(producto, mercado, fechaDesde, fechaHasta);
    return Results.Ok(new
    {
        producto,
        mercado,
        desde = fechaDesde.ToString("yyyy-MM-dd"),
        hasta = fechaHasta.ToString("yyyy-MM-dd"),
        precios,
    });
});

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
    /// <summary>Punto de entrada usado por las pruebas de integración.</summary>
    public sealed class AgroPreciosAplicacion;

    /// <summary>
    /// Datos fijos de la demostración: el "precio del día" es el del 10 de septiembre de 2026, como en los mockups.
    /// /precios y /historial leen la misma tabla, así que siempre coinciden.
    /// </summary>
    internal static class DatosAgro
    {
        public static readonly DateOnly FechaDelDia = new(2026, 9, 10);

        private static readonly DateOnly[] Fechas = [new(2026, 9, 8), new(2026, 9, 9), FechaDelDia];

        private static readonly string[] Meses =
            ["ene", "feb", "mar", "abr", "may", "jun", "jul", "ago", "sep", "oct", "nov", "dic"];

        public static Producto[] Productos { get; } =
        [
            new("frijol_negro", "Frijol negro", "granos", "quintal"),
            new("tomate", "Tomate", "hortalizas", "caja"),
            new("banano", "Banano", "frutas", "ciento"),
        ];

        public static Mercado[] Mercados { get; } =
        [
            new("cenma", "CENMA", "Villa Nueva"),
            new("terminal", "La Terminal", "Ciudad de Guatemala"),
            new("quetzaltenango", "La Democracia", "Quetzaltenango"),
        ];

        /// <summary>Precio de cada producto en cada mercado el 8, el 9 y el 10 de septiembre.</summary>
        private static readonly Dictionary<(string Producto, string Mercado), decimal[]> Precios = new()
        {
            [("frijol_negro", "cenma")] = [505.00m, 508.00m, 510.00m],
            [("frijol_negro", "terminal")] = [512.00m, 515.00m, 518.00m],
            [("frijol_negro", "quetzaltenango")] = [520.00m, 522.00m, 525.00m],
            [("tomate", "cenma")] = [180.00m, 185.00m, 190.00m],
            [("tomate", "terminal")] = [185.00m, 188.00m, 192.00m],
            [("tomate", "quetzaltenango")] = [175.00m, 178.00m, 182.00m],
            [("banano", "cenma")] = [45.00m, 46.00m, 48.00m],
            [("banano", "terminal")] = [47.00m, 48.00m, 50.00m],
            [("banano", "quetzaltenango")] = [44.00m, 45.00m, 46.00m],
        };

        public static object? BuscarPrecio(string claveProducto, string claveMercado, DateOnly fecha)
        {
            var producto = Buscar(Productos, claveProducto, p => p.Clave);
            var mercado = Buscar(Mercados, claveMercado, m => m.Clave);
            var dia = Array.IndexOf(Fechas, fecha);
            if (producto is null || mercado is null || dia < 0)
            {
                return null;
            }

            return new
            {
                producto = producto.Nombre,
                mercado = mercado.Nombre,
                unidad = producto.Unidad,
                precio = Formato(Precios[(producto.Clave, mercado.Clave)][dia]),
                fecha = $"{fecha.Day} {Meses[fecha.Month - 1]} {fecha.Year}",
            };
        }

        public static object[] ObtenerHistorial(string claveProducto, string claveMercado, DateOnly desde, DateOnly hasta)
        {
            var producto = Buscar(Productos, claveProducto, p => p.Clave);
            var mercado = Buscar(Mercados, claveMercado, m => m.Clave);
            if (producto is null || mercado is null)
            {
                return [];
            }

            var precios = Precios[(producto.Clave, mercado.Clave)];
            return Fechas
                .Select((fecha, dia) => (fecha, dia))
                .Where(par => par.fecha >= desde && par.fecha <= hasta)
                .Select(par => (object)new
                {
                    fecha = par.fecha.ToString("yyyy-MM-dd"),
                    precio = Formato(precios[par.dia]),
                    moneda = "GTQ",
                })
                .ToArray();
        }

        private static T? Buscar<T>(IEnumerable<T> elementos, string clave, Func<T, string> claveDe)
            where T : class =>
            elementos.FirstOrDefault(elemento => string.Equals(claveDe(elemento), clave, StringComparison.OrdinalIgnoreCase));

        private static string Formato(decimal precio) =>
            $"Q {precio.ToString("#,##0.00", System.Globalization.CultureInfo.InvariantCulture)}";

        internal sealed record Producto(string Clave, string Nombre, string Categoria, string Unidad);

        internal sealed record Mercado(string Clave, string Nombre, string Municipio);
    }
}
