namespace SistemaAranceles.Application.DTOs.TasaRetencion;

public sealed class CriterioReferenciaRetencionDto
{
    public int Id { get; init; }
    public int ConfiguracionRetencionId { get; init; }
    public decimal MetaRetencionPorcentaje { get; init; }
    public decimal MetaGraduacionPorcentaje { get; init; }
}
