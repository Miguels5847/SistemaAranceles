using FluentValidation;
using SistemaAranceles.Application.DTOs.Estudiantes;

namespace SistemaAranceles.Application.UseCases.Estudiantes.Validadores;

public sealed class GenerarProyeccionEstudiantesDtoValidador : AbstractValidator<GenerarProyeccionEstudiantesDto>
{
    public GenerarProyeccionEstudiantesDtoValidador()
    {
        RuleFor(x => x.CarreraId)
            .GreaterThan(0).WithMessage("CarreraId debe ser mayor a cero.");

        RuleFor(x => x.EscenarioProyeccionId)
            .GreaterThan(0).WithMessage("EscenarioProyeccionId debe ser mayor a cero.");

        RuleFor(x => x.SimulacionRetencionId)
            .GreaterThan(0).WithMessage("SimulacionRetencionId debe ser mayor a cero.");

        RuleFor(x => x.SemanasPorSemestre)
            .InclusiveBetween(8, 30).WithMessage("SemanasPorSemestre debe estar entre 8 y 30.");
    }
}
