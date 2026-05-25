namespace SistemaAranceles.Infrastructure.Persistence.Entidades;

/// <summary>
/// Configuración de arancel y matrícula por carrera (KAN-32, Épica 9).
/// Tabla nueva. Reemplaza funcionalmente a 'configuracion_arancel' (KAN-03).
/// </summary>
public sealed class ConfiguracionArancelCarrera
{
    public int Id { get; set; }
    public int CarreraId { get; set; }
    public int? EscenarioProyeccionId { get; set; }
    public string ModoCalculoArancel { get; set; } = "Manual";
    public decimal? ArancelManual { get; set; }
    public decimal? PorcentajeMatricula { get; set; }
    public bool UsaPorcentajeMatriculaInstitucional { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public int? CreadoPorUsuarioId { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public bool EstaActivo { get; set; } = true;
    public DateTime? EliminadoEn { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public Carrera? Carrera { get; set; }
    public EscenarioProyeccion? EscenarioProyeccion { get; set; }
}
