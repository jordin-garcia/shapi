using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Apis;

namespace Shapi.Aplicacion.Apis;

public sealed class ConfigurarRutas(
    IRepositorioApis repositorio,
    ILectorEspecificacionOpenApi lector,
    IReloj reloj,
    IPublicadorCache publicador)
{
    public async Task<Resultado<ListaRutas>> Ejecutar(
        Guid apiId,
        Guid organizacionId,
        SolicitudConfiguracionRutas solicitud,
        CancellationToken cancelacion = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        if (api is null)
        {
            return NoEncontrada();
        }

        var errores = Validar(solicitud.Cambios);
        if (errores.Count > 0)
        {
            return new Error(CodigosError.DatosInvalidos, "Revise la configuración de las rutas.", errores);
        }

        var cambios = solicitud.Cambios.Select(c => c!).ToArray();
        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        await repositorio.BloquearApi(api, cancelacion);
        var rutas = await repositorio.ObtenerRutas(apiId, cancelacion);
        var porId = rutas.ToDictionary(r => r.Id);
        if (cambios.Any(c => !porId.ContainsKey(c.RutaId!.Value)))
        {
            return NoEncontrada();
        }

        for (var indice = 0; indice < cambios.Length; indice++)
        {
            var cambio = cambios[indice];
            var ruta = porId[cambio.RutaId!.Value];
            if (cambio.CacheSegundos > 0 && ruta.Metodo != MetodoHttp.Get)
            {
                errores[$"[{indice}].cacheSegundos"] = ["La caché solo está disponible para rutas GET."];
            }
        }
        if (errores.Count > 0)
        {
            return new Error(CodigosError.DatosInvalidos, "Revise la configuración de las rutas.", errores);
        }

        var ahora = reloj.Ahora;
        foreach (var cambio in cambios)
        {
            porId[cambio.RutaId!.Value].Configurar(
                cambio.LimiteMinuto,
                cambio.CacheSegundos!.Value,
                cambio.PesoLlamadas!.Value,
                ahora);
        }

        await repositorio.Guardar(cancelacion);
        await transaccion.Confirmar(cancelacion);
        if (api.Estado == EstadoApi.Publicada)
        {
            await publicador.PublicarApi(apiId, cancelacion);
        }

        return await CargarEspecificacion.ConstruirLista(repositorio, lector, api, cancelacion);
    }

    private static Dictionary<string, string[]> Validar(IReadOnlyList<ConfiguracionRutaSolicitada?> cambios)
    {
        var errores = new Dictionary<string, string[]>();
        var vistas = new HashSet<Guid>();
        for (var indice = 0; indice < cambios.Count; indice++)
        {
            var cambio = cambios[indice];
            if (cambio is null)
            {
                errores[$"[{indice}]"] = ["Indique la configuración de la ruta."];
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
            if (cambio.LimiteMinuto is <= 0)
            {
                errores[$"[{indice}].limiteMinuto"] = ["El límite debe ser mayor que cero o quedar vacío."];
            }
            if (cambio.CacheSegundos is null or < 0 or > 86400)
            {
                errores[$"[{indice}].cacheSegundos"] = ["La caché debe estar entre 0 y 86400 segundos."];
            }
            if (cambio.PesoLlamadas is null or < 1 or > 1000)
            {
                errores[$"[{indice}].pesoLlamadas"] = ["El peso debe estar entre 1 y 1000 llamadas."];
            }
        }

        return errores;
    }

    private static Error NoEncontrada() => new(CodigosError.ApiNoEncontrada, "No se encontró la API o una de sus rutas.");
}

public sealed class CambiarPublicacionApi(
    IRepositorioApis repositorio,
    IBitacora bitacora,
    IReloj reloj,
    IPublicadorCache publicador)
{
    public async Task<Resultado<EstadoPublicacionApi>> Publicar(
        Guid apiId,
        Guid organizacionId,
        ActorRegistroApi actor,
        CancellationToken cancelacion = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        if (api is null)
        {
            return NoEncontrada();
        }
        if (!await repositorio.CorreoVerificado(actor.Id, cancelacion))
        {
            return new Error(CodigosError.CorreoNoVerificado, "Verifique su correo antes de publicar la API.");
        }

        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        await repositorio.BloquearApi(api, cancelacion);
        var faltan = new List<string>();
        if (!(await repositorio.ObtenerRutas(apiId, cancelacion)).Any(r => r.Expuesta))
        {
            faltan.Add("ruta_expuesta");
        }
        if (!await repositorio.ExistePlanActivo(apiId, cancelacion))
        {
            faltan.Add("plan_activo");
        }
        if (faltan.Count > 0)
        {
            return new Error(
                CodigosError.PublicacionIncompleta,
                "Complete la configuración antes de publicar la API.",
                new { faltan });
        }

        if (api.Publicar(reloj.Ahora))
        {
            await Registrar(api, organizacionId, actor, AccionesBitacora.ApiPublicada, "Publicó", cancelacion);
        }
        else
        {
            await repositorio.Guardar(cancelacion);
        }
        await transaccion.Confirmar(cancelacion);
        await publicador.PublicarApi(apiId, cancelacion);
        return Estado(api);
    }

    public async Task<Resultado<EstadoPublicacionApi>> Despublicar(
        Guid apiId,
        Guid organizacionId,
        ActorRegistroApi actor,
        CancellationToken cancelacion = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        if (api is null)
        {
            return NoEncontrada();
        }

        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        await repositorio.BloquearApi(api, cancelacion);
        if (api.Despublicar(reloj.Ahora))
        {
            await Registrar(api, organizacionId, actor, AccionesBitacora.ApiDespublicada, "Despublicó", cancelacion);
        }
        else
        {
            await repositorio.Guardar(cancelacion);
        }
        await transaccion.Confirmar(cancelacion);
        await publicador.PublicarApi(apiId, cancelacion);
        return Estado(api);
    }

    private async Task Registrar(
        Api api,
        Guid organizacionId,
        ActorRegistroApi actor,
        string accion,
        string verbo,
        CancellationToken cancelacion)
    {
        var nombreActor = await repositorio.ObtenerNombreUsuario(actor.Id, cancelacion) ?? actor.NombreAlterno;
        await bitacora.Registrar(new EntradaBitacora(
            TipoActor.Usuario,
            actor.Id,
            nombreActor,
            organizacionId,
            accion,
            $"{verbo} la API {api.Nombre}.")
        {
            ObjetivoTipo = "api",
            ObjetivoId = api.Id,
            Ip = actor.Ip,
        }, cancelacion);
    }

    private static EstadoPublicacionApi Estado(Api api) => new(api.Id, api.Subdominio, api.Estado, api.PublicadaEn);

    private static Error NoEncontrada() => new(CodigosError.ApiNoEncontrada, "No se encontró la API.");
}
