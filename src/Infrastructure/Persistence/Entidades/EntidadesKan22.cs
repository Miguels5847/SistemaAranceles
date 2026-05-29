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

    public DateTimeOffset FechaActualizacion { get; set; } = DateTimeOffset.UtcNow;
    public int ActualizadoPorUsuarioId { get; set; }
    public string? FuenteNotas { get; set; }

    public Usuario? ActualizadoPorUsuario { get; set; }
}
