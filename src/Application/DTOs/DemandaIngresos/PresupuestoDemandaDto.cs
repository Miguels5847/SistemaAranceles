namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class PresupuestoDemandaDto
{
    public string TipoPresupuesto { get; init; } = string.Empty;
    public decimal MontoAnualInstitucional { get; init; }
    public decimal MontoAnualProrrateado { get; init; }
    public decimal MontoPorSemestre { get; init; }
    public decimal ProporcionEstudiantes { get; init; }

    public string MontoAnualDisplay => $"$ {MontoAnualInstitucional:N2}";
    public string ProrrateadoDisplay => $"$ {MontoAnualProrrateado:N2}";
    public string SemestreDisplay => $"$ {MontoPorSemestre:N2}";
    public string ProporcionDisplay => $"{ProporcionEstudiantes:P2}";
}

public sealed class PresupuestosCarreraDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int EstudiantesUniversidad { get; init; }
    public int DocentesUniversidad { get; init; }
    public decimal EstudiantesCarreraPromedio { get; init; }
    public int SemestresPorAnio { get; init; }
    public decimal PorcentajeBecasInstitucionales { get; init; }
    public decimal PresupuestoAnualCapacitacion { get; init; }
    public decimal PresupuestoAnualInternacionalizacion { get; init; }
    public decimal PresupuestoAnualMarketing { get; init; }
    public decimal PolizaSeguroEstudiantilAnual { get; init; }
    public int? AnioBaseInflacion { get; init; }
    public IReadOnlyList<decimal> FactoresInflacionPeriodos { get; init; } = [];
    public IReadOnlyList<PresupuestoDemandaDto> Presupuestos { get; init; } = [];
    public string? MensajeAdvertencia { get; init; }

    public decimal TotalAnualProrrateado => Presupuestos.Sum(p => p.MontoAnualProrrateado);
    public decimal TotalPorSemestre => Presupuestos.Sum(p => p.MontoPorSemestre);

    public string TotalAnualDisplay => $"$ {TotalAnualProrrateado:N2}";
    public string TotalSemestreDisplay => $"$ {TotalPorSemestre:N2}";
}
