namespace SistemaAranceles.Infrastructure.Persistence.Entidades;

public sealed class DatosInstitucionales
{
    public int Id { get; set; }
    public string Periodo { get; set; } = string.Empty;

    public int NumeroEstudiantesUniversidad { get; set; }
    public int NumeroDocentesUniversidad { get; set; }
    public int NumeroPersonasPlantaCentral { get; set; }

    public decimal SueldoBasico { get; set; }
    public decimal Funcional { get; set; }
    public decimal FondoReserva { get; set; }
    public decimal BeneficioXiv { get; set; }
    public decimal BeneficioXiii { get; set; }
    public decimal AportePatronal { get; set; }
    public decimal Varios { get; set; }
    public int MesesCapitalTrabajo { get; set; } = 2;
    public decimal PorcentajeImprevistosInversion { get; set; } = 5m;

    // KAN-35: parámetros Épica 9
    public decimal PorcentajeMatriculaDefault { get; set; } = 10m;
    public decimal PorcentajeBecasInstitucionales { get; set; } = 10m;
    public int SemestresPorAnio { get; set; } = 2;
    public int MesesOperativosCiclo { get; set; } = 6;
    public decimal PresupuestoAnualCapacitacion { get; set; }
    public decimal PresupuestoAnualInternacionalizacion { get; set; }
    public decimal PresupuestoAnualMarketing { get; set; }
    public decimal PolizaSeguroEstudiantilAnual { get; set; }
    public string? FuenteInflacion { get; set; }
    public int? AnioBaseProyeccion { get; set; }

    // KAN-36: parámetros Épica 10
    public decimal PresupuestoBaseUniversidad { get; set; }
    public decimal PresupuestoGobiernoBecas { get; set; }
    public decimal PorcentajeInvestigacion { get; set; } = 5m;
    public decimal PorcentajeVinculacion { get; set; } = 1m;
    public decimal PorcentajeBecasEstudiantes { get; set; } = 90m;
    public decimal PorcentajeBecasDocentes { get; set; } = 10m;

    // KAN-40: parámetros Épica 11
    public decimal TasaInteresFinanciera { get; set; } = 8m;
    public decimal PremioRiesgo { get; set; } = 5m;
    public decimal TmrManual { get; set; }
    public bool UsarTmrManual { get; set; }

    // KAN-44: parámetros bisección arancel óptimo
    public decimal ToleranciaVanArancel { get; set; } = 1m;
    public decimal MargenAproximacionVanArancel { get; set; } = 2m;
    public decimal ArancelMinimoBusqueda { get; set; } = 500m;
    public decimal ArancelMaximoBusqueda { get; set; } = 5000m;
    public int MaxIteracionesBiseccion { get; set; } = 60;

    // KAN-44B: financiamiento módulo Amortización
    public decimal PorcentajeFinanciadoPrestamo { get; set; }
    public decimal PorcentajeFinanciadoConvenio { get; set; }
    public string? NombreEntidadPrestamo { get; set; }
    public string? NombreEntidadConvenio { get; set; }
    public decimal TasaInteresAnualPrestamo { get; set; } = 15.02m;
    public int PlazoPrestamoMeses { get; set; } = 24;

    public DateTimeOffset FechaActualizacion { get; set; } = DateTimeOffset.UtcNow;
    public int ActualizadoPorUsuarioId { get; set; }
    public string? FuenteNotas { get; set; }

    public Usuario? ActualizadoPorUsuario { get; set; }
}
