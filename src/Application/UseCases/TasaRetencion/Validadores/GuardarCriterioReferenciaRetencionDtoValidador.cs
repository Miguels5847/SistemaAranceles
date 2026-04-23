using FluentValidation;
using SistemaAranceles.Application.DTOs.TasaRetencion;

namespace SistemaAranceles.Application.UseCases.TasaRetencion.Validadores;

public sealed class GuardarCriterioReferenciaRetencionDtoValidador : AbstractValidator<GuardarCriterioReferenciaRetencionDto>
{
    public GuardarCriterioReferenciaRetencionDtoValidador()
    {
        RuleFor(x => x.ConfiguracionRetencionId).GreaterThan(0).WithMessage("La configuración es requerida.");
        RuleFor(x => x.MetaRetencionPorcentaje).InclusiveBetween(0m, 100m).WithMessage("La meta de retención debe estar entre 0% y 100%.");
        RuleFor(x => x.MetaGraduacionPorcentaje).InclusiveBetween(0m, 100m).WithMessage("La meta de graduación debe estar entre 0% y 100%.");
    }
}
