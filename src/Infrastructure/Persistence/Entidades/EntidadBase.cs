namespace SistemaAranceles.Infrastructure.Persistence.Entidades;

public abstract class EntidadBase
{
    public int Id { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public int? CreadoPorUsuarioId { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public bool EstaActivo { get; set; } = true;
    public DateTime? EliminadoEn { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
}
