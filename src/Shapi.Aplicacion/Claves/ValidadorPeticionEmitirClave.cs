using FluentValidation;
using Shapi.Contratos.Redis;

namespace Shapi.Aplicacion.Claves;

public sealed class ValidadorPeticionEmitirClave : AbstractValidator<PeticionEmitirClave>
{
    public ValidadorPeticionEmitirClave()
    {
        RuleFor(p => p.Tipo)
            .Must(t => t is ContextoClave.TipoProduccion or ContextoClave.TipoPruebas)
            .WithMessage("El tipo debe ser produccion o pruebas.")
            .OverridePropertyName("tipo");
    }
}
