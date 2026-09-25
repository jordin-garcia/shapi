using FluentValidation;

namespace Shapi.Aplicacion.Identidad;

/// <summary>Datos del registro de un proveedor (CU-01, A1.1).</summary>
public record RegistroProveedor(string? Nombre, string? Correo, string? Organizacion, string? Contrasena);

/// <summary>Reglas de 10 §1 (contraseña) y 07 §3.1 (organización). Los nombres de campo van en camelCase (convenciones §5).</summary>
public class ValidadorRegistroProveedor : AbstractValidator<RegistroProveedor>
{
    public const int LargoMinimoContrasena = 10;
    public const int LargoMaximoContrasena = 128;

    public ValidadorRegistroProveedor()
    {
        RuleFor(r => r.Nombre)
            .NotEmpty().WithMessage("Escriba su nombre.")
            .MaximumLength(120).WithMessage("El nombre no puede tener más de 120 caracteres.")
            .OverridePropertyName("nombre");

        RuleFor(r => r.Correo)
            .NotEmpty().WithMessage("Escriba su correo.")
            .MaximumLength(254).WithMessage("El correo no puede tener más de 254 caracteres.")
            .EmailAddress().WithMessage("Escriba un correo válido.")
            .OverridePropertyName("correo");

        RuleFor(r => r.Organizacion)
            .Must(o => o?.Trim().Length is >= 2 and <= 120).WithMessage("El nombre de la organización debe tener entre 2 y 120 caracteres.")
            .OverridePropertyName("organizacion");

        RuleFor(r => r.Contrasena)
            .NotEmpty().WithMessage("Escriba una contraseña.")
            .Length(LargoMinimoContrasena, LargoMaximoContrasena).WithMessage($"La contraseña debe tener entre {LargoMinimoContrasena} y {LargoMaximoContrasena} caracteres.")
            .Must((r, contrasena) => !string.Equals(contrasena?.Trim(), r.Correo?.Trim(), StringComparison.OrdinalIgnoreCase))
            .WithMessage("La contraseña no puede ser igual al correo.")
            .OverridePropertyName("contrasena");
    }
}
