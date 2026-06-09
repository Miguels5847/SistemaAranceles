using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Descuento comercial del arancel por rango de ciclos (KAN-44).
/// Configurable por carrera y opcionalmente por escenario. El arancel base se mantiene;
/// el descuento solo afecta el valor cobrado por ciclo para calcular ingresos.
/// </summary>
public sealed class DescuentoArancelCiclo : EntidadDominioBase
{
    private DescuentoArancelCiclo()
    {
    }

    public DescuentoArancelCiclo(
        int carreraId,
        int? escenarioProyeccionId,
        int cicloDesde,
        int cicloHasta,
        decimal porcentajeDescuento)
    {
        CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");
        AsignarEscenario(escenarioProyeccionId);
        CambiarRango(cicloDesde, cicloHasta, porcentajeDescuento);
        EstaActivo = true;
    }

    public int CarreraId { get; private set; }
    public int? EscenarioProyeccionId { get; private set; }
    public int CicloDesde { get; private set; }
    public int CicloHasta { get; private set; }
    public decimal PorcentajeDescuento { get; private set; }
    public bool EstaActivo { get; private set; }

    public void AsignarEscenario(int? escenarioProyeccionId)
    {
        if (escenarioProyeccionId is int valor && valor <= 0)
        {
            throw new DominioException("El escenario debe ser válido o nulo.");
        }
        EscenarioProyeccionId = escenarioProyeccionId;
    }

    public void CambiarRango(int cicloDesde, int cicloHasta, decimal porcentajeDescuento)
    {
        CicloDesde = GuardiaDominio.EnteroPositivo(cicloDesde, "Ciclo desde");
        if (cicloHasta < cicloDesde)
        {
            throw new DominioException("El ciclo hasta debe ser mayor o igual al ciclo desde.");
        }
        CicloHasta = cicloHasta;
        PorcentajeDescuento = GuardiaDominio.Porcentaje(porcentajeDescuento, "Porcentaje de descuento");
    }

    public void Activar() => EstaActivo = true;
    public void Desactivar() => EstaActivo = false;
}
