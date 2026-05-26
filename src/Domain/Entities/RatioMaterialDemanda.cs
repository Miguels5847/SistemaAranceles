using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Ratio de consumo de materiales por estudiante (KAN-34, Épica 9).
/// El precio unitario NO vive aquí: viene de item_material_insumo (KAN-29)
/// cuando ItemMaterialInsumoId está vinculado.
/// </summary>
public sealed class RatioMaterialDemanda : EntidadDominioBase
{
    private RatioMaterialDemanda()
    {
    }

    public RatioMaterialDemanda(
        int? carreraId,
        string categoria,
        string concepto,
        int? itemMaterialInsumoId,
        decimal ratioConsumo,
        UnidadRatioMaterial unidadRatio,
        int mesesOperativos,
        bool aplicaInflacion)
    {
        AsignarCarrera(carreraId);
        Categoria = GuardiaDominio.Requerido(categoria, "Categoría", 60);
        Concepto = GuardiaDominio.Requerido(concepto, "Concepto", 140);
        ItemMaterialInsumoId = NormalizarId(itemMaterialInsumoId);
        RatioConsumo = GuardiaDominio.DecimalNoNegativo(ratioConsumo, "Ratio consumo", 6);
        UnidadRatio = unidadRatio;
        ValidarMeses(mesesOperativos);
        MesesOperativos = mesesOperativos;
        AplicaInflacion = aplicaInflacion;
        EstaActivo = true;
    }

    public int? CarreraId { get; private set; }
    public string Categoria { get; private set; } = string.Empty;
    public string Concepto { get; private set; } = string.Empty;
    public int? ItemMaterialInsumoId { get; private set; }
    public decimal RatioConsumo { get; private set; }
    public UnidadRatioMaterial UnidadRatio { get; private set; }
    public int MesesOperativos { get; private set; }
    public bool AplicaInflacion { get; private set; }
    public bool EstaActivo { get; private set; }

    public void AsignarCarrera(int? carreraId)
    {
        if (carreraId is int valor && valor <= 0)
            throw new DominioException("Carrera debe ser válida o nula.");
        CarreraId = carreraId;
    }

    public void Actualizar(
        string categoria,
        string concepto,
        int? itemMaterialInsumoId,
        decimal ratioConsumo,
        UnidadRatioMaterial unidadRatio,
        int mesesOperativos,
        bool aplicaInflacion)
    {
        Categoria = GuardiaDominio.Requerido(categoria, "Categoría", 60);
        Concepto = GuardiaDominio.Requerido(concepto, "Concepto", 140);
        ItemMaterialInsumoId = NormalizarId(itemMaterialInsumoId);
        RatioConsumo = GuardiaDominio.DecimalNoNegativo(ratioConsumo, "Ratio consumo", 6);
        UnidadRatio = unidadRatio;
        ValidarMeses(mesesOperativos);
        MesesOperativos = mesesOperativos;
        AplicaInflacion = aplicaInflacion;
    }

    public void Activar() => EstaActivo = true;
    public void Desactivar() => EstaActivo = false;

    /// <summary>Cantidad consumida en un periodo según número de estudiantes.</summary>
    public decimal CalcularCantidad(decimal estudiantes)
    {
        if (estudiantes <= 0m) return 0m;
        var multiplicador = UnidadRatio == UnidadRatioMaterial.PorEstudianteMes
            ? MesesOperativos
            : 1;
        return decimal.Round(estudiantes * RatioConsumo * multiplicador, 4);
    }

    private static int? NormalizarId(int? id) => id is int v && v > 0 ? v : null;

    private static void ValidarMeses(int meses)
    {
        if (meses <= 0 || meses > 12)
            throw new DominioException("Meses operativos debe estar entre 1 y 12.");
    }
}
