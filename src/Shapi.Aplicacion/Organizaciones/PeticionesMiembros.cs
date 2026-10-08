using FluentValidation;

namespace Shapi.Aplicacion.Organizaciones;

public sealed record PeticionInvitarMiembro(string? Correo, string? Rol);

public sealed class ValidadorInvitarMiembro : AbstractValidator<PeticionInvitarMiembro>
{
    public ValidadorInvitarMiembro()
    {
        RuleFor(x => x.Correo).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Rol).Must(rol => rol is "editor" or "lector")
            .WithMessage("El rol debe ser editor o lector.");
    }
}

public sealed record PeticionCambiarRolMiembro(string? Rol);

public sealed class ValidadorCambiarRolMiembro : AbstractValidator<PeticionCambiarRolMiembro>
{
    public ValidadorCambiarRolMiembro()
    {
        RuleFor(x => x.Rol).Must(rol => rol is "editor" or "lector")
            .WithMessage("El rol debe ser editor o lector.");
    }
}

public sealed record PeticionAceptarInvitacionMiembro(string? Nombre, string? Contrasena);

public sealed class ValidadorAceptarInvitacionMiembro : AbstractValidator<PeticionAceptarInvitacionMiembro>
{
    public ValidadorAceptarInvitacionMiembro()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Contrasena).NotEmpty().MinimumLength(10);
    }
}
