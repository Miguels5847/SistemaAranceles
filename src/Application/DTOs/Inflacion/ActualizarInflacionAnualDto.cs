namespace SistemaAranceles.Application.DTOs.Inflacion;

public sealed class ActualizarInflacionAnualDto
{
    public int Id { get; init; }
    public int Anio { get; init; }
    public decimal PorcentajeInflacion { get; init; }
    public string? FuenteNombre { get; init; }
    public string? TipoFuente { get; init; }
}
