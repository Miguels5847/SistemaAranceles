using FluentValidation;
using SistemaAranceles.Application.DTOs.TasaRetencion;

namespace SistemaAranceles.Application.UseCases.TasaRetencion.Validadores;

public sealed class ActualizarSimulacionRetencionDtoValidador : AbstractValidator<ActualizarSimulacionRetencionDto>
{
    public ActualizarSimulacionRetencionDtoValidador()
    {
        RuleFor(x => x.SimulacionRetencionId)
            .GreaterThan(0)
            .WithMessage("La simulación es requerida.");

        RuleFor(x => x.CohorteAnio)
            .InclusiveBetween(2012, 2050)
            .WithMessage("El año de cohorte debe estar entre 2012 y 2050.");
    }
}
