using FluentValidation;
using SistemaAranceles.Application.DTOs.TasaRetencion;

namespace SistemaAranceles.Application.UseCases.TasaRetencion.Validadores;

public sealed class ActualizarConfiguracionRetencionDtoValidador : AbstractValidator<ActualizarConfiguracionRetencionDto>
{
    public ActualizarConfiguracionRetencionDtoValidador()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("El Id es requerido.");
        RuleFor(x => x.CarreraId).GreaterThan(0).WithMessage("Debe seleccionar una carrera.");
        RuleFor(x => x.EscenarioProyeccionId).GreaterThan(0).WithMessage("Debe seleccionar un escenario.");
        RuleFor(x => x.TotalCiclos).InclusiveBetween(1, 20).WithMessage("El total de ciclos debe estar entre 1 y 20.");
        RuleFor(x => x.TasaRetencionPorcentaje).InclusiveBetween(0m, 100m).WithMessage("La tasa de retención debe estar entre 0% y 100%.");
        RuleFor(x => x.TasaGraduacionPorcentaje).InclusiveBetween(0m, 100m).WithMessage("La tasa de graduación debe estar entre 0% y 100%.");
        RuleFor(x => x.MetaRetencionPorcentaje).InclusiveBetween(0m, 100m).WithMessage("La meta de retención debe estar entre 0% y 100%.");
        RuleFor(x => x.MetaGraduacionPorcentaje).InclusiveBetween(0m, 100m).WithMessage("La meta de graduación debe estar entre 0% y 100%.");
        RuleFor(x => x.EstudiantesPeriodo1).GreaterThanOrEqualTo(0m).WithMessage("Los estudiantes del período 1 no pueden ser negativos.");
        RuleFor(x => x.EstudiantesPeriodo2).GreaterThanOrEqualTo(0m).WithMessage("Los estudiantes del período 2 no pueden ser negativos.");
        RuleFor(x => x.ParalelosPeriodo1).GreaterThanOrEqualTo(0).WithMessage("Los paralelos del período 1 no pueden ser negativos.");
        RuleFor(x => x.ParalelosPeriodo2).GreaterThanOrEqualTo(0).WithMessage("Los paralelos del período 2 no pueden ser negativos.");
    }
}
