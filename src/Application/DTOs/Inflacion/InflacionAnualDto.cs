namespace SistemaAranceles.Application.DTOs.Inflacion;

public sealed class InflacionAnualDto
{
    public int Id { get; init; }
    public int Anio { get; init; }
    public decimal PorcentajeInflacion { get; init; }
    public string FuenteNombre { get; init; } = string.Empty;
    public string TipoFuente { get; init; } = string.Empty;
}
