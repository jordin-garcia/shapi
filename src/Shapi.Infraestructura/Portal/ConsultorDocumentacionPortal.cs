using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shapi.Aplicacion.Portal;
using Shapi.Dominio.Apis;
using Shapi.Infraestructura.Persistencia;

namespace Shapi.Infraestructura.Portal;

public sealed class ConsultorDocumentacionPortal(ShapiDbContext db) : IConsultorDocumentacionPortal
{
    public async Task<DocumentacionPortal> Consultar(
        PortalResuelto portal,
        CancellationToken cancelacion = default)
    {
        var rutasPersistidas = await db.Set<Ruta>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(ruta => ruta.ApiId == portal.ApiId && ruta.Expuesta)
            .ToListAsync(cancelacion);

        var dominioPropio = await db.Set<DominioPropio>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(dominio => dominio.ApiId == portal.ApiId && dominio.Estado == EstadoDominio.Verificado)
            .Select(dominio => dominio.Dominio)
            .SingleOrDefaultAsync(cancelacion);

        var hostApi = dominioPropio ?? portal.HostApi;
        var rutas = rutasPersistidas
            .Select(ruta => Convertir(ruta, hostApi))
            .OrderBy(resultado => resultado.Orden)
            .Select(resultado => resultado.Ruta)
            .ToArray();

        return new DocumentacionPortal(rutas);
    }

    private static RutaConOrden Convertir(Ruta ruta, string hostApi)
    {
        using var documento = JsonDocument.Parse(ruta.Definicion);
        var raiz = documento.RootElement;
        var orden = raiz.TryGetProperty("orden", out var valorOrden) && valorOrden.TryGetInt32(out var numeroOrden)
            ? numeroOrden
            : int.MaxValue;

        var parametros = new List<ParametroDocumentado>();
        var ejemplosParametros = new Dictionary<string, JsonElement>();
        if (raiz.TryGetProperty("parametros", out var parametrosOpenApi)
            && parametrosOpenApi.ValueKind == JsonValueKind.Array)
        {
            foreach (var parametro in parametrosOpenApi.EnumerateArray())
            {
                AgregarParametro(parametros, ejemplosParametros, parametro);
            }
        }

        JsonElement? ejemploPeticion = null;
        if (raiz.TryGetProperty("cuerpo", out var cuerpo) && cuerpo.ValueKind == JsonValueKind.Object)
        {
            ejemploPeticion = LeerContenido(cuerpo);
            AgregarParametrosDelCuerpo(parametros, ejemplosParametros, cuerpo);
        }

        ejemploPeticion ??= ejemplosParametros.Count == 0
            ? null
            : JsonSerializer.SerializeToElement(ejemplosParametros);

        var (codigoRespuesta, ejemploRespuesta) = LeerRespuesta(raiz);
        var metodo = ruta.Metodo.ToString().ToUpperInvariant();
        var urlBase = $"https://{hostApi.TrimEnd('/')}";
        var patron = ruta.Patron.StartsWith("/", StringComparison.Ordinal) ? ruta.Patron : "/" + ruta.Patron;

        return new RutaConOrden(
            orden,
            new RutaDocumentada(
                metodo,
                ruta.Patron,
                ruta.Resumen,
                ruta.Descripcion,
                parametros,
                ejemploPeticion,
                ejemploRespuesta,
                codigoRespuesta,
                ruta.PesoLlamadas,
                urlBase + patron));
    }

    private static void AgregarParametro(
        ICollection<ParametroDocumentado> destino,
        IDictionary<string, JsonElement> ejemplos,
        JsonElement parametro)
    {
        if (!parametro.TryGetProperty("name", out var nombreJson) || nombreJson.GetString() is not { } nombre)
        {
            return;
        }

        var esquema = parametro.TryGetProperty("schema", out var valorEsquema) ? valorEsquema : default;
        destino.Add(new ParametroDocumentado(
            nombre,
            LeerTipo(esquema),
            parametro.TryGetProperty("required", out var obligatorio) && obligatorio.ValueKind == JsonValueKind.True,
            parametro.TryGetProperty("description", out var descripcion) ? descripcion.GetString() : null));

        if (parametro.TryGetProperty("example", out var ejemplo))
        {
            ejemplos[nombre] = ejemplo.Clone();
        }
        else if (esquema.ValueKind == JsonValueKind.Object && esquema.TryGetProperty("example", out ejemplo))
        {
            ejemplos[nombre] = ejemplo.Clone();
        }
    }

    private static void AgregarParametrosDelCuerpo(
        ICollection<ParametroDocumentado> destino,
        IDictionary<string, JsonElement> ejemplos,
        JsonElement cuerpo)
    {
        if (!TryContenido(cuerpo, out var contenido)
            || !contenido.TryGetProperty("schema", out var esquema)
            || !esquema.TryGetProperty("properties", out var propiedades)
            || propiedades.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var obligatorios = esquema.TryGetProperty("required", out var requeridos)
            && requeridos.ValueKind == JsonValueKind.Array
            ? requeridos.EnumerateArray().Select(valor => valor.GetString()).ToHashSet(StringComparer.Ordinal)
            : [];

        foreach (var propiedad in propiedades.EnumerateObject())
        {
            destino.Add(new ParametroDocumentado(
                propiedad.Name,
                LeerTipo(propiedad.Value),
                obligatorios.Contains(propiedad.Name),
                propiedad.Value.TryGetProperty("description", out var descripcion) ? descripcion.GetString() : null));
            if (propiedad.Value.TryGetProperty("example", out var ejemplo))
            {
                ejemplos[propiedad.Name] = ejemplo.Clone();
            }
        }
    }

    private static string LeerTipo(JsonElement esquema)
    {
        if (esquema.ValueKind == JsonValueKind.Object
            && esquema.TryGetProperty("type", out var tipo)
            && tipo.ValueKind == JsonValueKind.String)
        {
            return tipo.GetString() ?? "object";
        }

        return "object";
    }

    private static JsonElement? LeerContenido(JsonElement contenedor)
    {
        return TryContenido(contenedor, out var contenido)
            && contenido.TryGetProperty("example", out var ejemplo)
                ? ejemplo.Clone()
                : null;
    }

    private static bool TryContenido(JsonElement contenedor, out JsonElement contenido)
    {
        contenido = default;
        if (!contenedor.TryGetProperty("content", out var contenidos)
            || contenidos.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (contenidos.TryGetProperty("application/json", out contenido))
        {
            return true;
        }

        var primero = contenidos.EnumerateObject().FirstOrDefault();
        contenido = primero.Value;
        return primero.Name is not null;
    }

    private static (int? Codigo, JsonElement? Ejemplo) LeerRespuesta(JsonElement raiz)
    {
        if (!raiz.TryGetProperty("respuestas", out var respuestas)
            || respuestas.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        foreach (var respuesta in respuestas.EnumerateObject()
                     .Select(valor => (Valor: valor, EsNumero: int.TryParse(valor.Name, CultureInfo.InvariantCulture, out var codigo), Codigo: codigo))
                     .Where(valor => valor.EsNumero && valor.Codigo is >= 200 and <= 299)
                     .OrderBy(valor => valor.Codigo))
        {
            var ejemplo = LeerContenido(respuesta.Valor.Value);
            if (ejemplo is not null)
            {
                return (respuesta.Codigo, ejemplo);
            }
        }

        return (null, null);
    }

    private sealed record RutaConOrden(int Orden, RutaDocumentada Ruta);
}
