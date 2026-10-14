using System.Net;
using System.Security.Cryptography;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Dominio.Apis;

namespace Shapi.Aplicacion.Apis;

public sealed class GestionarDominios(
    IRepositorioApis repositorio,
    IResolutorDns resolutor,
    IBitacora bitacora,
    IReloj reloj,
    IPublicadorCache publicador,
    IProtectorSecretoOrigen protector,
    ConfiguracionDns configuracion)
{
    private const string AlfabetoBase62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public async Task<Resultado<EstadoDominioPropio>> Obtener(Guid apiId, Guid organizacionId, CancellationToken ct = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, ct);
        return api is null ? NoEncontrada() : Estado(api, await repositorio.ObtenerDominio(apiId, ct));
    }

    public async Task<Resultado<EstadoDominioPropio>> Conectar(
        Guid apiId, Guid organizacionId, SolicitudDominioPropio solicitud, ActorRegistroApi actor,
        CancellationToken ct = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, ct);
        if (api is null)
        {
            return NoEncontrada();
        }

        var dominio = Normalizar(solicitud.Dominio);
        if (dominio is null || (configuracion.ModoSimulado && !dominio.EndsWith(".localhost", StringComparison.Ordinal)))
        {
            return new Error(CodigosError.DatosInvalidos, "Revise el dominio.",
                new Dictionary<string, string[]>
                {
                    ["dominio"] = [configuracion.ModoSimulado
                    ? "Use un dominio válido que termine en .localhost."
                    : "Use un nombre de dominio válido."]
                });
        }
        if (!await repositorio.PlanPermiteDominioPropio(organizacionId, ct))
        {
            return new Error(CodigosError.PlanSinDominioPropio, "Su plan no incluye dominio propio.");
        }

        var destino = $"{api.Subdominio}.api.{configuracion.DominioBase}";
        await using var transaccion = await repositorio.IniciarTransaccion(ct);
        await repositorio.BloquearApi(api, ct);
        var actual = await repositorio.ObtenerDominio(apiId, ct);
        var hostAnterior = actual is { Estado: EstadoDominio.Verificado } ? actual.Dominio : null;
        if (actual is null)
        {
            actual = new DominioPropio(apiId, dominio, destino, reloj.Ahora);
            repositorio.AgregarDominio(actual);
        }
        else
        {
            actual.Reemplazar(dominio, destino, reloj.Ahora);
        }

        await Registrar(api, actor, AccionesBitacora.DominioConectado, $"Conectó el dominio {dominio}.", ct);
        await transaccion.Confirmar(ct);
        if (hostAnterior is not null)
        {
            await publicador.EliminarHost(hostAnterior, apiId, ct);
            await publicador.PublicarApi(apiId, ct);
        }
        return Estado(api, actual);
    }

    public async Task<Resultado<bool>> Simular(Guid apiId, Guid organizacionId, CancellationToken ct = default)
    {
        if (!configuracion.ModoSimulado)
        {
            return new Error(CodigosError.ApiNoEncontrada, "No se encontró el recurso.");
        }

        var api = await repositorio.Obtener(apiId, organizacionId, ct);
        if (api is null)
        {
            return NoEncontrada();
        }

        var dominio = await repositorio.ObtenerDominio(apiId, ct);
        if (dominio is null)
        {
            return NoEncontrada();
        }

        await repositorio.SimularCname(dominio.Dominio, dominio.DestinoCname, reloj.Ahora, ct);
        await repositorio.Guardar(ct);
        return true;
    }

    public async Task<Resultado<EstadoDominioPropio>> Verificar(
        Guid apiId, Guid organizacionId, ActorRegistroApi actor, CancellationToken ct = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, ct);
        if (api is null)
        {
            return NoEncontrada();
        }

        var dominio = await repositorio.ObtenerDominio(apiId, ct);
        if (dominio is null)
        {
            return NoEncontrada();
        }

        var yaVerificado = dominio.Estado == EstadoDominio.Verificado;
        var coincide = dominio.Verificar(await resolutor.ResolverCname(dominio.Dominio, ct), reloj.Ahora);
        if (coincide && !yaVerificado)
        {
            await using var transaccion = await repositorio.IniciarTransaccion(ct);
            await Registrar(api, actor, AccionesBitacora.DominioVerificado, $"Verificó el dominio {dominio.Dominio}.", ct);
            await transaccion.Confirmar(ct);
        }
        else
        {
            await repositorio.Guardar(ct);
        }
        if (coincide)
        {
            await publicador.PublicarApi(apiId, ct);
        }
        return Estado(api, dominio);
    }

    public async Task<Resultado<bool>> Eliminar(Guid apiId, Guid organizacionId, CancellationToken ct = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, ct);
        if (api is null)
        {
            return NoEncontrada();
        }

        var dominio = await repositorio.ObtenerDominio(apiId, ct);
        if (dominio is null)
        {
            return true;
        }

        var host = dominio.Dominio;
        repositorio.EliminarDominio(dominio);
        await repositorio.Guardar(ct);
        await publicador.EliminarHost(host, apiId, ct);
        await publicador.PublicarApi(apiId, ct);
        return true;
    }

    public async Task<Resultado<SecretoOrigenRegenerado>> RegenerarSecreto(
        Guid apiId, Guid organizacionId, ActorRegistroApi actor, CancellationToken ct = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, ct);
        if (api is null)
        {
            return NoEncontrada();
        }

        await using var transaccion = await repositorio.IniciarTransaccion(ct);
        await repositorio.BloquearApi(api, ct);
        var secreto = $"shps_{RandomNumberGenerator.GetString(AlfabetoBase62, 32)}";
        api.RegenerarSecretoOrigen(protector.Cifrar(secreto), reloj.Ahora);
        await Registrar(api, actor, AccionesBitacora.SecretoOrigenRegenerado, $"Regeneró el secreto de origen de la API {api.Nombre}.", ct);
        await transaccion.Confirmar(ct);
        await publicador.PublicarApi(apiId, ct);
        return new SecretoOrigenRegenerado(secreto);
    }

    private async Task Registrar(Api api, ActorRegistroApi actor, string accion, string descripcion, CancellationToken ct)
    {
        var nombre = await repositorio.ObtenerNombreUsuario(actor.Id, ct) ?? actor.NombreAlterno;
        await bitacora.Registrar(new EntradaBitacora(TipoActor.Usuario, actor.Id, nombre, api.OrganizacionId, accion, descripcion)
        { ObjetivoTipo = "api", ObjetivoId = api.Id, Ip = actor.Ip }, ct);
    }

    private EstadoDominioPropio Estado(Api api, DominioPropio? dominio) => new(
        api.Id, api.Nombre, $"https://{api.Subdominio}.{configuracion.DominioBase}",
        $"https://{api.Subdominio}.api.{configuracion.DominioBase}", "shps_••••••••••••••••••••••••••••••••",
        configuracion.ModoSimulado, dominio?.Dominio, dominio?.DestinoCname,
        dominio?.Estado.ToString().ToLowerInvariant(), dominio?.Motivo, dominio?.VerificadoEn);

    private static string? Normalizar(string? valor)
    {
        var dominio = valor?.Trim().TrimEnd('.').ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(dominio) && Uri.CheckHostName(dominio) == UriHostNameType.Dns && dominio.Contains('.')
            ? dominio
            : null;
    }

    private static Error NoEncontrada() => new(CodigosError.ApiNoEncontrada, "No se encontró la API o el dominio.");
}

public sealed record ConfiguracionDns(bool ModoSimulado, string DominioBase);

public sealed class AutorizarTls(IRepositorioApis repositorio)
{
    public async Task<bool> EstaAutorizado(string? dominio, CancellationToken ct = default) =>
        dominio is not null && await repositorio.ObtenerDominioVerificado(dominio.Trim().TrimEnd('.').ToLowerInvariant(), ct) is not null;
}
