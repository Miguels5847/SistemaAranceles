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

    public DateTimeOffset FechaActualizacion { get; set; } = DateTimeOffset.UtcNow;
    public int ActualizadoPorUsuarioId { get; set; }
    public string? FuenteNotas { get; set; }

    public Usuario? ActualizadoPorUsuario { get; set; }
}
