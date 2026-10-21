using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Shapi.Aplicacion.Comun;
using Shapi.Aplicacion.Portal;
using Shapi.Contratos;
using Shapi.Dominio.Apis;

namespace Shapi.Aplicacion.Apis;

public sealed class PersonalizarPortal(
    IRepositorioApis repositorio,
    IConsultorDocumentacionPortal consultorDocumentacion,
    IReloj reloj,
    ConfiguracionDns configuracion)
{
    public const int MaximoBytesLogo = Api.MaximoBytesLogo;

    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public async Task<Resultado<PersonalizacionPortal>> Obtener(
        Guid apiId,
        Guid organizacionId,
        CancellationToken cancelacion = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        if (api is null)
        {
            return NoEncontrada();
        }

        return await ConstruirRespuesta(api, organizacionId, cancelacion);
    }

    public async Task<Resultado<PersonalizacionPortal>> Actualizar(
        Guid apiId,
        Guid organizacionId,
        SolicitudPersonalizacionPortal solicitud,
        CancellationToken cancelacion = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        if (api is null)
        {
            return NoEncontrada();
        }

        var errores = Validar(solicitud);
        if (errores.Count > 0)
        {
            return new Error(CodigosError.DatosInvalidos, "Revise los datos del portal.", errores);
        }

        api.ActualizarPortal(solicitud.Nombre, solicitud.Color!.Trim(), solicitud.Bienvenida, reloj.Ahora);
        await repositorio.Guardar(cancelacion);
        return await ConstruirRespuesta(api, organizacionId, cancelacion);
    }

    public async Task<Resultado<PersonalizacionPortal>> CargarLogo(
        Guid apiId,
        Guid organizacionId,
        byte[]? contenido,
        string? nombreArchivo,
        string? tipoContenido,
        CancellationToken cancelacion = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        if (api is null)
        {
            return NoEncontrada();
        }

        if (contenido is null || contenido.Length == 0 || contenido.Length > MaximoBytesLogo)
        {
            return LogoInvalido();
        }

        var tipo = DetectarYValidarLogo(contenido, nombreArchivo, tipoContenido);
        if (tipo is null)
        {
            return LogoInvalido();
        }

        api.ActualizarLogoPortal(contenido, tipo.Value, reloj.Ahora);
        await repositorio.Guardar(cancelacion);
        return await ConstruirRespuesta(api, organizacionId, cancelacion);
    }

    public async Task<Resultado<LogoPortalDescargado>> ObtenerLogo(
        Guid apiId,
        Guid organizacionId,
        CancellationToken cancelacion = default)
    {
        var api = await repositorio.Obtener(apiId, organizacionId, cancelacion);
        if (api?.PortalLogo is null || api.PortalLogoTipo is null)
        {
            return NoEncontrada();
        }

        return new LogoPortalDescargado(api.PortalLogo, api.PortalLogoTipo.Value);
    }

    private async Task<PersonalizacionPortal> ConstruirRespuesta(
        Api api,
        Guid organizacionId,
        CancellationToken cancelacion)
    {
        var nombreOrganizacion = await repositorio.ObtenerNombreOrganizacion(organizacionId, cancelacion) ?? api.Nombre;
        var hostPortal = $"{api.Subdominio}.{configuracion.DominioBase}";
        var hostApi = $"{api.Subdominio}.api.{configuracion.DominioBase}";
        var portalResuelto = new PortalResuelto(
            api.Id,
            organizacionId,
            nombreOrganizacion,
            api.Subdominio,
            api.Nombre,
            api.EspecificacionDescripcion,
            api.PortalNombre ?? api.Nombre,
            api.PortalColor,
            api.PortalBienvenida,
            api.PortalLogo,
            api.PortalLogoTipo,
            hostPortal,
            hostApi);

        var documentacion = await consultorDocumentacion.Consultar(portalResuelto, cancelacion);
        var rutas = documentacion.Rutas
            .Select(r => new RutaVistaPreviaPortal(
                r.Metodo,
                r.Patron,
                r.Resumen,
                r.Descripcion,
                r.EjemploPeticion,
                r.EjemploRespuesta,
                r.CodigoRespuesta,
                r.PesoLlamadas))
            .ToArray();

        var planes = (await repositorio.ObtenerPlanesActivos(api.Id, cancelacion))
            .Select(p => new PlanVistaPreviaPortal(
                p.Id,
                p.Nombre,
                p.Descripcion,
                p.Precio,
                p.EsGratuito,
                p.VigenciaDias,
                p.CuotaLlamadas,
                p.LimiteMinuto))
            .ToArray();

        var tieneLogo = api.PortalLogo is not null && api.PortalLogoTipo is not null;
        return new PersonalizacionPortal(
            api.Id,
            api.Nombre,
            nombreOrganizacion,
            $"https://{hostPortal}",
            api.PortalNombre,
            api.PortalColor,
            api.PortalBienvenida,
            tieneLogo,
            tieneLogo ? $"/api/apis/{api.Id}/portal/logo" : null,
            api.EspecificacionDescripcion,
            rutas,
            planes);
    }

    private static Dictionary<string, string[]> Validar(SolicitudPersonalizacionPortal solicitud)
    {
        var errores = new Dictionary<string, string[]>();
        var color = solicitud.Color?.Trim();
        if (string.IsNullOrWhiteSpace(color) || !Regex.IsMatch(color, Api.PatronColorPortal))
        {
            errores["color"] = ["Use un color hexadecimal #RRGGBB."];
        }

        var bienvenida = string.IsNullOrWhiteSpace(solicitud.Bienvenida) ? null : solicitud.Bienvenida.Trim();
        if (bienvenida is not null && bienvenida.Length > Api.MaximoBienvenida)
        {
            errores["bienvenida"] = ["El texto de bienvenida no puede superar 280 caracteres."];
        }

        return errores;
    }

    private static LogoTipo? DetectarYValidarLogo(byte[] contenido, string? nombreArchivo, string? tipoContenido)
    {
        var extension = Path.GetExtension(nombreArchivo ?? string.Empty).ToLowerInvariant();
        var mime = tipoContenido?.Trim().ToLowerInvariant();

        if (EsPng(contenido))
        {
            var declaraOtroFormato = (!string.IsNullOrEmpty(extension) && extension != ".png")
                || (!string.IsNullOrEmpty(mime) && mime is not ("image/png" or "application/octet-stream"));
            return declaraOtroFormato ? null : LogoTipo.Png;
        }

        if (EsSvgValido(contenido))
        {
            var declaraOtroFormato = (!string.IsNullOrEmpty(extension) && extension != ".svg")
                || (!string.IsNullOrEmpty(mime) && mime is not ("image/svg+xml" or "text/xml" or "application/xml" or "application/octet-stream"));
            return declaraOtroFormato ? null : LogoTipo.Svg;
        }

        return null;
    }

    private static bool EsPng(byte[] contenido) =>
        contenido.Length >= FirmaPng.Length
        && contenido.AsSpan(0, FirmaPng.Length).SequenceEqual(FirmaPng);

    private static bool EsSvgValido(byte[] contenido)
    {
        try
        {
            using var flujo = new MemoryStream(contenido, writable: false);
            using var lector = XmlReader.Create(flujo, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 1024,
            });
            var documento = XDocument.Load(lector);
            return string.Equals(documento.Root?.Name.LocalName, "svg", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is XmlException or DecoderFallbackException or ArgumentException)
        {
            return false;
        }
    }

    private static Error LogoInvalido() =>
        new(CodigosError.LogoInvalido, "El logotipo debe ser una imagen PNG o SVG de hasta 512 KB.");

    private static Error NoEncontrada() =>
        new(CodigosError.ApiNoEncontrada, "No se encontró la API.");
}
