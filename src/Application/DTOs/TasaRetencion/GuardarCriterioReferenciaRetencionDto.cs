namespace SistemaAranceles.Application.DTOs.TasaRetencion;

public sealed class GuardarCriterioReferenciaRetencionDto
{
    public int ConfiguracionRetencionId { get; init; }
    public decimal MetaRetencionPorcentaje { get; init; }
    public decimal MetaGraduacionPorcentaje { get; init; }
}
