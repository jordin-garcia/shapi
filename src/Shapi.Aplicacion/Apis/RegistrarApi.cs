using System.Security.Cryptography;
using FluentValidation;
using Shapi.Aplicacion.Comun;
using Shapi.Contratos;
using Shapi.Contratos.Red;
using Shapi.Dominio.Apis;

namespace Shapi.Aplicacion.Apis;

public sealed class ValidadorSolicitudRegistroApi : AbstractValidator<SolicitudRegistroApi>
{
    public ValidadorSolicitudRegistroApi()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Escriba el nombre de la API.")
            .OverridePropertyName("nombre");
        RuleFor(x => x.UrlOrigen)
            .NotEmpty().WithMessage("Escriba la URL del servidor de origen.")
            .OverridePropertyName("urlOrigen");
        RuleFor(x => x.Subdominio)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Escriba el subdominio.")
            .Matches(Api.PatronSubdominio).WithMessage("Use de 3 a 30 letras minúsculas, números o guiones.")
            .Must(x => x is null || !SubdominiosReservados.Contiene(x)).WithMessage("El subdominio está reservado.")
            .OverridePropertyName("subdominio");
    }
}

public sealed class RegistrarApi(
    IValidator<SolicitudRegistroApi> validador,
    ValidadorDireccionOrigen validadorOrigen,
    ConfiguracionProteccionOrigen configuracionOrigen,
    IProbadorOrigen probadorOrigen,
    IRepositorioApis repositorio,
    IProtectorSecretoOrigen protector,
    IBitacora bitacora,
    IReloj reloj)
{
    private const string AlfabetoBase62 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public async Task<Resultado<ApiRegistrada>> Ejecutar(
        Guid organizacionId,
        ActorRegistroApi actor,
        SolicitudRegistroApi solicitud,
        CancellationToken cancelacion = default)
    {
        // Se recorta antes de validar: en .NET, `$` acepta un salto de línea final, así que "admin\n" pasaría el patrón
        // y la lista de reservados, y la entidad lo rechazaría después con una excepción.
        solicitud = solicitud with
        {
            Nombre = solicitud.Nombre?.Trim(),
            UrlOrigen = solicitud.UrlOrigen?.Trim(),
            Subdominio = solicitud.Subdominio?.Trim(),
        };
        var validacion = await validador.ValidateAsync(solicitud, cancelacion);
        if (!validacion.IsValid)
        {
            return new Error(
                CodigosError.DatosInvalidos,
                "Revise los datos del formulario.",
                validacion.ToDictionary());
        }

        var nombre = solicitud.Nombre!;
        var urlOrigen = solicitud.UrlOrigen!;
        var subdominio = solicitud.Subdominio!;
        if (await repositorio.ExisteSubdominio(subdominio, cancelacion))
        {
            return SubdominioOcupado();
        }

        var validacionOrigen = await validadorOrigen.Validar(
            urlOrigen,
            configuracionOrigen.ModoDemo,
            configuracionOrigen.OrigenesPermitidos,
            cancelacion);
        if (!validacionOrigen.EsValida)
        {
            return validacionOrigen.Error == ErrorDireccionOrigen.ResolucionFallida
                ? new Error(CodigosError.OrigenInaccesible, "No se pudo conectar con el origen.", new { motivo = validacionOrigen.Detalle })
                : new Error(CodigosError.OrigenNoPermitido, "La URL del origen no está permitida.", new { motivo = validacionOrigen.Detalle });
        }

        var prueba = await probadorOrigen.Probar(validacionOrigen.Origen!, cancelacion);
        if (!prueba.EsExitosa)
        {
            return new Error(
                CodigosError.OrigenInaccesible,
                "No se pudo conectar con el origen.",
                new { motivo = prueba.Detalle });
        }

        var secreto = $"shps_{RandomNumberGenerator.GetString(AlfabetoBase62, 32)}";
        var api = new Api(
            organizacionId,
            nombre,
            subdominio,
            validacionOrigen.Direccion!.AbsoluteUri,
            protector.Cifrar(secreto),
            reloj.Ahora);
        await using var transaccion = await repositorio.IniciarTransaccion(cancelacion);
        if (!await repositorio.Agregar(api, cancelacion))
        {
            return SubdominioOcupado();
        }

        var nombreActor = await repositorio.ObtenerNombreUsuario(actor.Id, cancelacion) ?? actor.NombreAlterno;
        await bitacora.Registrar(new EntradaBitacora(
            TipoActor.Usuario,
            actor.Id,
            nombreActor,
            organizacionId,
            AccionesBitacora.ApiRegistrada,
            $"Registró la API {api.Nombre}.")
        {
            ObjetivoTipo = "api",
            ObjetivoId = api.Id,
            Detalle = new { api.Subdominio },
            Ip = actor.Ip,
        }, cancelacion);
        await transaccion.Confirmar(cancelacion);

        return new ApiRegistrada(api.Id, api.Nombre, api.Subdominio, api.Estado, secreto, prueba.Milisegundos);
    }

    private static Error SubdominioOcupado() =>
        new(CodigosError.SubdominioOcupado, "El subdominio ya está ocupado.");
}
