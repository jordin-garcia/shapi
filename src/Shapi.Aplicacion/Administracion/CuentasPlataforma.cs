using FluentValidation;
using Shapi.Aplicacion.Comun;
using Shapi.Dominio.Identidad;

namespace Shapi.Aplicacion.Administracion;

public sealed record CuentaPlataforma(Guid Id, string Nombre, string Correo, string Rol, string Estado);
public sealed record NuevaCuentaPlataforma(string? Nombre, string? Correo, string? Rol);
public sealed record DefinicionContrasena(string? Token, string? Contrasena);
public sealed record ActorCuenta(Guid Id, string Nombre, string? Ip);

public sealed class ValidadorCuentaPlataforma : AbstractValidator<NuevaCuentaPlataforma>
{
    public ValidadorCuentaPlataforma()
    {
        RuleFor(x => x.Nombre).Must(x => !string.IsNullOrWhiteSpace(x)).WithMessage("Escriba el nombre.")
            .MaximumLength(150).OverridePropertyName("nombre");
        RuleFor(x => x.Correo).Must(x => !string.IsNullOrWhiteSpace(x) && System.Net.Mail.MailAddress.TryCreate(x.Trim(), out var correo) && correo.Address == x.Trim())
            .WithMessage("Escriba un correo válido.").MaximumLength(254).OverridePropertyName("correo");
        RuleFor(x => x.Rol).Must(x => x is "administrador" or "soporte").WithMessage("Seleccione Administrador o Soporte.").OverridePropertyName("rol");
    }
}

public interface IRepositorioCuentasPlataforma
{
    Task<IReadOnlyList<CuentaPlataforma>> Listar(CancellationToken cancelacion);
    Task<Resultado<CuentaPlataforma>> Crear(NuevaCuentaPlataforma solicitud, ActorCuenta actor, CancellationToken cancelacion);
    Task<Resultado> CambiarEstado(Guid id, bool activar, ActorCuenta actor, CancellationToken cancelacion);
    Task<Resultado<string>> DefinirContrasena(DefinicionContrasena solicitud, string host, string? ip, string? agente, CancellationToken cancelacion);
}

public sealed class CrearCuentaPlataforma(IRepositorioCuentasPlataforma repositorio)
{
    public Task<Resultado<CuentaPlataforma>> Ejecutar(NuevaCuentaPlataforma solicitud, ActorCuenta actor, CancellationToken cancelacion) =>
        repositorio.Crear(solicitud, actor, cancelacion);
}

public sealed class CambiarEstadoCuentaPlataforma(IRepositorioCuentasPlataforma repositorio)
{
    public Task<Resultado> Ejecutar(Guid id, bool activar, ActorCuenta actor, CancellationToken cancelacion) =>
        repositorio.CambiarEstado(id, activar, actor, cancelacion);
}

public sealed class DefinirContrasenaPlataforma(IRepositorioCuentasPlataforma repositorio)
{
    public Task<Resultado<string>> Ejecutar(DefinicionContrasena solicitud, string host, string? ip, string? agente, CancellationToken cancelacion) =>
        repositorio.DefinirContrasena(solicitud, host, ip, agente, cancelacion);
}
