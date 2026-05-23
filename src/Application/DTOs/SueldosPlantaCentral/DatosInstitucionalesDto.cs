using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.DTOs.SueldosPlantaCentral;

public sealed class DatosInstitucionalesDto
{
    public int Id { get; init; }
    public string Periodo { get; init; } = string.Empty;

    public int NumeroEstudiantesUniversidad { get; init; }
    public int NumeroDocentesUniversidad { get; init; }
    public int NumeroPersonasPlantaCentral { get; init; }

    public decimal SueldoBasico { get; init; }
    public decimal Funcional { get; init; }
    public decimal FondoReserva { get; init; }
    public decimal BeneficioXiv { get; init; }
    public decimal BeneficioXiii { get; init; }
    public decimal AportePatronal { get; init; }
    public decimal Varios { get; init; }
    public int MesesCapitalTrabajo { get; init; } = DatosInstitucionales.MesesCapitalTrabajoPorDefecto;
    public decimal PorcentajeImprevistosInversion { get; init; } = DatosInstitucionales.PorcentajeImprevistosInversionPorDefecto;

    public decimal MasaSalarialMensual { get; init; }
    public decimal TotalMensualPlantaCentral { get; init; }
    public decimal TotalAnualPlantaCentral { get; init; }
    public decimal RatioAdminPorDocente { get; init; }
    public decimal CostoPlantaCentralPorEstudianteAnual { get; init; }

    public DateTimeOffset FechaActualizacion { get; init; }
    public int ActualizadoPorUsuarioId { get; init; }
    public string? ActualizadoPorUsuarioNombre { get; init; }
    public string? FuenteNotas { get; init; }

    public string MasaSalarialMensualDisplay => MasaSalarialMensual.ToString("C2");
    public string TotalAnualDisplay => TotalAnualPlantaCentral.ToString("C2");
    public string RatioAdminPorDocenteDisplay => RatioAdminPorDocente.ToString("F2");
    public string CostoPorEstudianteAnualDisplay => CostoPlantaCentralPorEstudianteAnual.ToString("C2");
    public string PorcentajeImprevistosInversionDisplay => PorcentajeImprevistosInversion.ToString("0.####") + "%";
}

public sealed class GuardarDatosInstitucionalesDto
{
    public string Periodo { get; init; } = string.Empty;

    public int NumeroEstudiantesUniversidad { get; init; }
    public int NumeroDocentesUniversidad { get; init; }
    public int NumeroPersonasPlantaCentral { get; init; }

    public decimal SueldoBasico { get; init; }
    public decimal Funcional { get; init; }
    public decimal FondoReserva { get; init; }
    public decimal BeneficioXiv { get; init; }
    public decimal BeneficioXiii { get; init; }
    public decimal AportePatronal { get; init; }
    public decimal Varios { get; init; }
    public int MesesCapitalTrabajo { get; init; } = DatosInstitucionales.MesesCapitalTrabajoPorDefecto;
    public decimal PorcentajeImprevistosInversion { get; init; } = DatosInstitucionales.PorcentajeImprevistosInversionPorDefecto;

    public string? FuenteNotas { get; init; }
}
