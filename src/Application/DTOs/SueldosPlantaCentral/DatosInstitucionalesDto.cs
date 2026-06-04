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

    // KAN-35
    public decimal PorcentajeMatriculaDefault { get; init; } = DatosInstitucionales.PorcentajeMatriculaDefaultPorDefecto;
    public decimal PorcentajeBecasInstitucionales { get; init; } = DatosInstitucionales.PorcentajeBecasInstitucionalesPorDefecto;
    public int SemestresPorAnio { get; init; } = DatosInstitucionales.SemestresPorAnioPorDefecto;
    public int MesesOperativosCiclo { get; init; } = DatosInstitucionales.MesesOperativosCicloPorDefecto;
    public decimal PresupuestoAnualCapacitacion { get; init; }
    public decimal PresupuestoAnualInternacionalizacion { get; init; }
    public decimal PresupuestoAnualMarketing { get; init; }
    public decimal PolizaSeguroEstudiantilAnual { get; init; }
    public string? FuenteInflacion { get; init; }
    public int? AnioBaseProyeccion { get; init; }

    // KAN-36
    public decimal PresupuestoBaseUniversidad { get; init; } = DatosInstitucionales.PresupuestoBaseUniversidadPorDefecto;
    public decimal PresupuestoGobiernoBecas { get; init; } = DatosInstitucionales.PresupuestoGobiernoBecasPorDefecto;
    public decimal PorcentajeInvestigacion { get; init; } = DatosInstitucionales.PorcentajeInvestigacionPorDefecto;
    public decimal PorcentajeVinculacion { get; init; } = DatosInstitucionales.PorcentajeVinculacionPorDefecto;
    public decimal PorcentajeBecasEstudiantes { get; init; } = DatosInstitucionales.PorcentajeBecasEstudiantesPorDefecto;
    public decimal PorcentajeBecasDocentes { get; init; } = DatosInstitucionales.PorcentajeBecasDocentesPorDefecto;

    // KAN-40
    public decimal TasaInteresFinanciera { get; init; } = DatosInstitucionales.TasaInteresFinancieraPorDefecto;
    public decimal PremioRiesgo { get; init; } = DatosInstitucionales.PremioRiesgoPorDefecto;
    public decimal TmrManual { get; init; } = DatosInstitucionales.TmrManualPorDefecto;
    public bool UsarTmrManual { get; init; } = DatosInstitucionales.UsarTmrManualPorDefecto;

    // KAN-44
    public decimal ToleranciaVanArancel { get; init; } = DatosInstitucionales.ToleranciaVanArancelPorDefecto;
    public decimal MargenAproximacionVanArancel { get; init; } = DatosInstitucionales.MargenAproximacionVanArancelPorDefecto;
    public decimal ArancelMinimoBusqueda { get; init; } = DatosInstitucionales.ArancelMinimoBusquedaPorDefecto;
    public decimal ArancelMaximoBusqueda { get; init; } = DatosInstitucionales.ArancelMaximoBusquedaPorDefecto;
    public int MaxIteracionesBiseccion { get; init; } = DatosInstitucionales.MaxIteracionesBiseccionPorDefecto;

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

    // KAN-35
    public decimal PorcentajeMatriculaDefault { get; init; } = DatosInstitucionales.PorcentajeMatriculaDefaultPorDefecto;
    public decimal PorcentajeBecasInstitucionales { get; init; } = DatosInstitucionales.PorcentajeBecasInstitucionalesPorDefecto;
    public int SemestresPorAnio { get; init; } = DatosInstitucionales.SemestresPorAnioPorDefecto;
    public int MesesOperativosCiclo { get; init; } = DatosInstitucionales.MesesOperativosCicloPorDefecto;
    public decimal PresupuestoAnualCapacitacion { get; init; }
    public decimal PresupuestoAnualInternacionalizacion { get; init; }
    public decimal PresupuestoAnualMarketing { get; init; }
    public decimal PolizaSeguroEstudiantilAnual { get; init; }
    public string? FuenteInflacion { get; init; }
    public int? AnioBaseProyeccion { get; init; }

    // KAN-36
    public decimal PresupuestoBaseUniversidad { get; init; } = DatosInstitucionales.PresupuestoBaseUniversidadPorDefecto;
    public decimal PresupuestoGobiernoBecas { get; init; } = DatosInstitucionales.PresupuestoGobiernoBecasPorDefecto;
    public decimal PorcentajeInvestigacion { get; init; } = DatosInstitucionales.PorcentajeInvestigacionPorDefecto;
    public decimal PorcentajeVinculacion { get; init; } = DatosInstitucionales.PorcentajeVinculacionPorDefecto;
    public decimal PorcentajeBecasEstudiantes { get; init; } = DatosInstitucionales.PorcentajeBecasEstudiantesPorDefecto;
    public decimal PorcentajeBecasDocentes { get; init; } = DatosInstitucionales.PorcentajeBecasDocentesPorDefecto;

    // KAN-40
    public decimal TasaInteresFinanciera { get; init; } = DatosInstitucionales.TasaInteresFinancieraPorDefecto;
    public decimal PremioRiesgo { get; init; } = DatosInstitucionales.PremioRiesgoPorDefecto;
    public decimal TmrManual { get; init; } = DatosInstitucionales.TmrManualPorDefecto;
    public bool UsarTmrManual { get; init; } = DatosInstitucionales.UsarTmrManualPorDefecto;

    // KAN-44
    public decimal ToleranciaVanArancel { get; init; } = DatosInstitucionales.ToleranciaVanArancelPorDefecto;
    public decimal MargenAproximacionVanArancel { get; init; } = DatosInstitucionales.MargenAproximacionVanArancelPorDefecto;
    public decimal ArancelMinimoBusqueda { get; init; } = DatosInstitucionales.ArancelMinimoBusquedaPorDefecto;
    public decimal ArancelMaximoBusqueda { get; init; } = DatosInstitucionales.ArancelMaximoBusquedaPorDefecto;
    public int MaxIteracionesBiseccion { get; init; } = DatosInstitucionales.MaxIteracionesBiseccionPorDefecto;

    public string? FuenteNotas { get; init; }
}
