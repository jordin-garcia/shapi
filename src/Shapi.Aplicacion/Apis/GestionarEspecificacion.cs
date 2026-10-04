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
            return Invalida(lectura.Error!.Ubicacion, lectura.Error.Mensaje, lectura.Error.DetalleTecnico);
        }

        var especificacion = lectura.Especificacion!;
        var ahora = reloj.Ahora;
        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        // Dos cargas simultáneas de la misma API insertarían las mismas rutas y chocarían con el UNIQUE
        // (api_id, metodo, patron): la segunda espera aquí y reconcilia sobre la API y las rutas que dejó la primera.
        await repositorio.BloquearApi(api, cancelacion);
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

        var lista = await ConstruirLista(repositorio, lector, api, cancelacion);
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

    private static Error Invalida(string ubicacion, string mensaje, string? detalleTecnico = null) =>
        new(
            CodigosError.EspecificacionInvalida,
            "La especificación OpenAPI no es válida.",
            detalleTecnico is null ? new { ubicacion, mensaje } : new { ubicacion, mensaje, detalleTecnico });

    private static Error NoEncontrada() =>
        new(CodigosError.ApiNoEncontrada, "No se encontró la API.");

    internal static async Task<ListaRutas> ConstruirLista(
        IRepositorioApis repositorio,
        ILectorEspecificacionOpenApi lector,
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
        return new ListaRutas(
            api.Id,
            api.Nombre,
            rutas,
            rutas.Count(r => r.Expuesta),
            rutas.Count(r => !r.Expuesta),
            Resumen(lector, api));
    }

    private static ResumenEspecificacion? Resumen(ILectorEspecificacionOpenApi lector, Api api) =>
        api.Especificacion is null
            || api.EspecificacionFormato is null
            || api.EspecificacionTitulo is null
            || api.EspecificacionVersion is null
            || api.EspecificacionCargadaEn is null
            ? null
            : new ResumenEspecificacion(
                api.EspecificacionTitulo,
                api.EspecificacionVersion,
                lector.LeerVersionOpenApi(api.Especificacion),
                api.EspecificacionFormato.Value.ToString().ToLowerInvariant(),
                api.EspecificacionCargadaEn.Value,
                Encoding.UTF8.GetByteCount(api.Especificacion));

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

public sealed class ListarRutas(IRepositorioApis repositorio, ILectorEspecificacionOpenApi lector)
{
    public async Task<Resultado<ListaRutas>> Ejecutar(
        Guid apiId,
        Guid organizacionId,
        CancellationToken cancelacion = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        return api is null
            ? new Error(CodigosError.ApiNoEncontrada, "No se encontró la API.")
            : await CargarEspecificacion.ConstruirLista(repositorio, lector, api, cancelacion);
    }
}

public sealed class ActualizarExposicionRutas(
    IRepositorioApis repositorio,
    ILectorEspecificacionOpenApi lector,
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

        var errores = ValidarCambios(solicitud.Cambios);
        if (errores.Count > 0)
        {
            return new Error(CodigosError.DatosInvalidos, "Revise las rutas de la solicitud.", errores);
        }

        var cambios = solicitud.Cambios.Select(c => (RutaId: c!.RutaId!.Value, Expuesta: c.Expuesta!.Value)).ToArray();

        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        // Una recarga simultánea podría borrar una de las rutas mientras se guarda su exposición.
        await repositorio.BloquearApi(api, cancelacion);
        var rutas = await repositorio.ObtenerRutas(apiId, cancelacion);
        var porId = rutas.ToDictionary(r => r.Id);
        if (cambios.Any(c => !porId.ContainsKey(c.RutaId)))
        {
            return new Error(CodigosError.ApiNoEncontrada, "No se encontró una de las rutas.");
        }

        var nombreActor = await repositorio.ObtenerNombreUsuario(solicitud.Actor.Id, cancelacion)
            ?? solicitud.Actor.NombreAlterno;
        foreach (var cambio in cambios)
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

        return await CargarEspecificacion.ConstruirLista(repositorio, lector, api, cancelacion);
    }

    /// <summary>Cada elemento debe traer <c>rutaId</c> y <c>expuesta</c>, y no se puede repetir una ruta (convenciones §5).</summary>
    private static Dictionary<string, string[]> ValidarCambios(IReadOnlyList<CambioExposicionRuta?> cambios)
    {
        var errores = new Dictionary<string, string[]>();
        var vistas = new HashSet<Guid>();
        for (var indice = 0; indice < cambios.Count; indice++)
        {
            var cambio = cambios[indice];
            if (cambio is null)
            {
                errores[$"[{indice}]"] = ["Indique la ruta y si queda expuesta."];
                continue;
            }

            if (cambio.RutaId is null)
            {
                errores[$"[{indice}].rutaId"] = ["Indique la ruta."];
            }
            else if (!vistas.Add(cambio.RutaId.Value))
            {
                errores[$"[{indice}].rutaId"] = ["No repita rutas en la solicitud."];
            }

            if (cambio.Expuesta is null)
            {
                errores[$"[{indice}].expuesta"] = ["Indique si la ruta queda expuesta u oculta."];
            }
        }

        return errores;
    }
}
