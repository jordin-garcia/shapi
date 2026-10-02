using System.Text;
using System.Text.Json;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Apis;

namespace Shapi.Aplicacion.Apis;

public sealed class CargarEspecificacion(
    IRepositorioApis repositorio,
    ILectorEspecificacionOpenApi lector,
    IReloj reloj,
    IPublicadorCache publicador)
{
    public const int MaximoBytes = 2 * 1024 * 1024;

    public async Task<Resultado<EspecificacionCargada>> Ejecutar(
        Guid apiId,
        Guid organizacionId,
        string nombreArchivo,
        string contenido,
        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(contenido) || Encoding.UTF8.GetByteCount(contenido) > MaximoBytes)
        {
            return Invalida("archivo", "El archivo debe contener una especificación y no puede superar 2 MB.");
        }

        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        if (api is null)
        {
            return NoEncontrada();
        }

        var lectura = await lector.Leer(contenido, nombreArchivo, cancelacion);
        if (!lectura.EsValida)
        {
            return Invalida(lectura.Error!.Ubicacion, lectura.Error.Mensaje);
        }

        var especificacion = lectura.Especificacion!;
        var ahora = reloj.Ahora;
        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        var existentes = await repositorio.ObtenerRutas(apiId, cancelacion);
        var porClave = existentes.ToDictionary(r => (r.Metodo, r.Patron));
        var clavesNuevas = especificacion.Operaciones.Select(o => (o.Metodo, o.Patron)).ToHashSet();

        var retiradas = existentes.Where(r => !clavesNuevas.Contains((r.Metodo, r.Patron))).ToArray();
        await repositorio.ConsolidarConsumoRutas(retiradas.Select(r => r.Id).ToArray(), ahora, cancelacion);
        foreach (var anterior in retiradas)
        {
            repositorio.EliminarRuta(anterior);
        }

        foreach (var operacion in especificacion.Operaciones)
        {
            if (porClave.TryGetValue((operacion.Metodo, operacion.Patron), out var ruta))
            {
                ruta.ActualizarDefinicion(operacion.Resumen, operacion.Descripcion, operacion.Definicion, ahora);
            }
            else
            {
                repositorio.AgregarRuta(new Ruta(
                    apiId,
                    operacion.Metodo,
                    operacion.Patron,
                    operacion.Resumen,
                    operacion.Descripcion,
                    operacion.Definicion,
                    ahora));
            }
        }

        api.CargarEspecificacion(
            contenido,
            especificacion.Formato,
            especificacion.Titulo,
            especificacion.Descripcion,
            especificacion.Version,
            ahora);
        await repositorio.Guardar(cancelacion);
        await transaccion.Confirmar(cancelacion);

        if (api.Estado == EstadoApi.Publicada)
        {
            await publicador.PublicarApi(apiId, cancelacion);
        }

        var lista = await ConstruirLista(repositorio, api, cancelacion);
        return new EspecificacionCargada(
            api.Id,
            api.Nombre,
            especificacion.Titulo,
            especificacion.Descripcion,
            especificacion.Version,
            especificacion.VersionOpenApi,
            especificacion.Formato.ToString().ToLowerInvariant(),
            ahora,
            lista.Elementos.Count,
            lista.Elementos);
    }

    private static Error Invalida(string ubicacion, string mensaje) =>
        new(CodigosError.EspecificacionInvalida, "La especificación OpenAPI no es válida.", new { ubicacion, mensaje });

    private static Error NoEncontrada() =>
        new(CodigosError.ApiNoEncontrada, "No se encontró la API.");

    internal static async Task<ListaRutas> ConstruirLista(
        IRepositorioApis repositorio,
        Api api,
        CancellationToken cancelacion)
    {
        var rutas = (await repositorio.ObtenerRutas(api.Id, cancelacion))
            .Select(r => new RutaAdministrada(
                r.Id,
                r.Metodo.ToString().ToUpperInvariant(),
                r.Patron,
                r.Resumen,
                r.Descripcion,
                r.Expuesta,
                Orden(r.Definicion)))
            .OrderBy(r => r.Orden)
            .ThenBy(r => r.Patron, StringComparer.Ordinal)
            .ToArray();
        return new ListaRutas(api.Id, api.Nombre, rutas, rutas.Count(r => r.Expuesta), rutas.Count(r => !r.Expuesta));
    }

    private static int Orden(string definicion)
    {
        try
        {
            using var json = JsonDocument.Parse(definicion);
            return json.RootElement.TryGetProperty("orden", out var orden) ? orden.GetInt32() : int.MaxValue;
        }
        catch (JsonException)
        {
            return int.MaxValue;
        }
    }
}

public sealed class ListarRutas(IRepositorioApis repositorio)
{
    public async Task<Resultado<ListaRutas>> Ejecutar(
        Guid apiId,
        Guid organizacionId,
        CancellationToken cancelacion = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        return api is null
            ? new Error(CodigosError.ApiNoEncontrada, "No se encontró la API.")
            : await CargarEspecificacion.ConstruirLista(repositorio, api, cancelacion);
    }
}

public sealed class ActualizarExposicionRutas(
    IRepositorioApis repositorio,
    IBitacora bitacora,
    IReloj reloj,
    IPublicadorCache publicador)
{
    public async Task<Resultado<ListaRutas>> Ejecutar(
        Guid apiId,
        Guid organizacionId,
        SolicitudExposicionRutas solicitud,
        CancellationToken cancelacion = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        if (api is null)
        {
            return new Error(CodigosError.ApiNoEncontrada, "No se encontró la API.");
        }

        if (solicitud.Cambios.Select(c => c.RutaId).Distinct().Count() != solicitud.Cambios.Count)
        {
            return new Error(CodigosError.DatosInvalidos, "No repita rutas en la solicitud.");
        }

        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        var rutas = await repositorio.ObtenerRutas(apiId, cancelacion);
        var porId = rutas.ToDictionary(r => r.Id);
        if (solicitud.Cambios.Any(c => !porId.ContainsKey(c.RutaId)))
        {
            return new Error(CodigosError.ApiNoEncontrada, "No se encontró una de las rutas.");
        }

        var nombreActor = await repositorio.ObtenerNombreUsuario(solicitud.Actor.Id, cancelacion)
            ?? solicitud.Actor.NombreAlterno;
        foreach (var cambio in solicitud.Cambios)
        {
            var ruta = porId[cambio.RutaId];
            if (!ruta.CambiarExposicion(cambio.Expuesta, reloj.Ahora))
            {
                continue;
            }

            var verbo = cambio.Expuesta ? "Expuso" : "Ocultó";
            await bitacora.Registrar(new EntradaBitacora(
                TipoActor.Usuario,
                solicitud.Actor.Id,
                nombreActor,
                organizacionId,
                cambio.Expuesta ? AccionesBitacora.RutaExpuesta : AccionesBitacora.RutaOcultada,
                $"{verbo} la ruta {ruta.Metodo.ToString().ToUpperInvariant()} {ruta.Patron} de la API {api.Nombre}.")
            {
                ObjetivoTipo = "ruta",
                ObjetivoId = ruta.Id,
                Detalle = new { apiId },
                Ip = solicitud.Actor.Ip,
            }, cancelacion);
        }

        await repositorio.Guardar(cancelacion);
        await transaccion.Confirmar(cancelacion);
        if (api.Estado == EstadoApi.Publicada)
        {
            await publicador.PublicarApi(apiId, cancelacion);
        }

        return await CargarEspecificacion.ConstruirLista(repositorio, api, cancelacion);
    }
}
