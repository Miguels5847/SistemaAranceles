namespace SistemaAranceles.Application.DTOs.SueldosPlantaCentral;

public sealed class AportePlantaCentralCarreraDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int EscenarioProyeccionId { get; init; }

    public string PeriodoInstitucionalVigente { get; init; } = string.Empty;
    public decimal TotalMensualPlantaCentral { get; init; }
    public decimal TotalAnualPlantaCentral { get; init; }
    public decimal EstudiantesUniversidad { get; init; }

    public List<AportePeriodoDto> Periodos { get; init; } = [];

    public decimal AporteAcumulado { get; init; }
    public decimal AportePromedioAnual { get; init; }
    public decimal PorcentajePromedioSobreTotalAnual { get; init; }

    public string AporteAcumuladoDisplay => AporteAcumulado.ToString("C2");
    public string AportePromedioAnualDisplay => AportePromedioAnual.ToString("C2");
    public string PorcentajePromedioDisplay => PorcentajePromedioSobreTotalAnual.ToString("P2");
}

public sealed class AportePeriodoDto
{
    public int PeriodoAcademicoId { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public int Anio { get; init; }
    public int NumeroPeriodoEnAnio { get; init; }
    public decimal AlumnosCarrera { get; init; }
    public decimal AporteSemestral { get; init; }
    public decimal PorcentajeSobreTotalAnual { get; init; }

    public string AporteSemestralDisplay => AporteSemestral.ToString("C2");
    public string PorcentajeDisplay => PorcentajeSobreTotalAnual.ToString("P2");
}
