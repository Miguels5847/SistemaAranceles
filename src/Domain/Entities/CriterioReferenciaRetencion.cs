using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class CriterioReferenciaRetencion : EntidadDominioBase
{
    private CriterioReferenciaRetencion()
    {
    }

    public CriterioReferenciaRetencion(int configuracionRetencionId, decimal metaRetencionPorcentaje, decimal metaGraduacionPorcentaje)
    {
        ConfiguracionRetencionId = GuardiaDominio.EnteroPositivo(configuracionRetencionId, "Configuración de retención");
        ActualizarMetas(metaRetencionPorcentaje, metaGraduacionPorcentaje);
    }

    public int ConfiguracionRetencionId { get; private set; }
    public decimal MetaRetencionPorcentaje { get; private set; }
    public decimal MetaGraduacionPorcentaje { get; private set; }

    public void ActualizarMetas(decimal metaRetencionPorcentaje, decimal metaGraduacionPorcentaje)
    {
        MetaRetencionPorcentaje = GuardiaDominio.Porcentaje(metaRetencionPorcentaje, "Meta de retención");
        MetaGraduacionPorcentaje = GuardiaDominio.Porcentaje(metaGraduacionPorcentaje, "Meta de graduación");
    }
}
