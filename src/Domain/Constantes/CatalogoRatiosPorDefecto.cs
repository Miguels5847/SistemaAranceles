using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Domain.Constantes;

/// <summary>
/// Un consumo (ratio) por defecto de "5. Materiales en Cantidades".
/// <see cref="ItemNombre"/> se re-resuelve por nombre contra item_material_insumo de la carrera destino.
/// </summary>
public sealed record RatioPorDefecto(
    string Categoria,
    string Concepto,
    decimal RatioConsumo,
    string UnidadRatio,
    int MesesOperativos,
    decimal CantidadFijaAdicional,
    bool AplicaInflacion,
    string? ItemNombre);

/// <summary>
/// Catálogo canónico de consumos por defecto para Demanda → 5. Materiales en Cantidades.
/// Réplica de la configuración validada de Administración de Empresas. Los conceptos se respetan textual
/// (incluyen las grafías del docente) para que coincidan con lo ya revisado.
/// </summary>
public static class CatalogoRatiosPorDefecto
{
    private const string PorEstudiante = UnidadRatioMaterialExtensiones.PorEstudianteText;
    private const string PorEstudianteMes = UnidadRatioMaterialExtensiones.PorEstudianteMesText;
    private const string FijoPeriodo = UnidadRatioMaterialExtensiones.FijoPeriodoText;
    private const string PorDocente = UnidadRatioMaterialExtensiones.PorDocenteText;

    public static IReadOnlyList<RatioPorDefecto> Items { get; } =
    [
        new(CatalogoMaterialesPorDefecto.CategoriaAccesorios, "Grapadora", 1m, PorDocente, 1, 4m, true, "Grapadora"),
        new(CatalogoMaterialesPorDefecto.CategoriaAccesorios, "Perforadora", 1m, PorDocente, 1, 4m, true, "Perforadora"),

        new(CatalogoMaterialesPorDefecto.CategoriaAseo, "Cloro", 5m, PorEstudianteMes, 6, 0m, true, "Cloro (Galón)"),
        new(CatalogoMaterialesPorDefecto.CategoriaAseo, "Desinfectante", 5m, PorEstudianteMes, 6, 0m, true, "Desinfectante (Galón)"),
        new(CatalogoMaterialesPorDefecto.CategoriaAseo, "Escoba", 2m, FijoPeriodo, 1, 0m, true, "Escoba"),
        new(CatalogoMaterialesPorDefecto.CategoriaAseo, "Fundas de Basura", 0.2m, PorEstudiante, 1, 0m, true, "Paquete de Fundas de Basura"),
        new(CatalogoMaterialesPorDefecto.CategoriaAseo, "Papel Higenico", 0.13m, PorEstudianteMes, 6, 0m, true, "Papel Higiénico (Rollo Grande)"),
        new(CatalogoMaterialesPorDefecto.CategoriaAseo, "Trapeador", 3m, FijoPeriodo, 1, 0m, true, "Trapeador"),
        new(CatalogoMaterialesPorDefecto.CategoriaAseo, "Uso de Cloro", 5m, PorEstudiante, 6, 0m, true, "Cloro (Galón)"),
        new(CatalogoMaterialesPorDefecto.CategoriaAseo, "Uso de Jabon", 0.009m, PorEstudiante, 6, 0m, true, "Jabón Líquido (Galón)"),
        new(CatalogoMaterialesPorDefecto.CategoriaAseo, "Uso de Jabon Liquido", 6m, PorEstudianteMes, 6, 0m, true, "Jabón Líquido (Galón)"),

        new(CatalogoMaterialesPorDefecto.CategoriaMateriales, "Carpetas de Carton", 2m, PorEstudiante, 1, 0m, true, "Carpetas de cartón"),
        new(CatalogoMaterialesPorDefecto.CategoriaMateriales, "Cartuchos Impresora", 0.02m, PorEstudiante, 1, 0m, true, "Cartuchos de impresora (Color)"),
        new(CatalogoMaterialesPorDefecto.CategoriaMateriales, "Cartuchos Impresora Negro", 0.02m, PorEstudiante, 1, 0m, true, "Cartuchos de impresora (Negro)"),
        new(CatalogoMaterialesPorDefecto.CategoriaMateriales, "Esferos, minas , lapiz, borradores,correctores", 1m, PorEstudiante, 1, 0m, true, "Esferos, micro minas, lápiz, borradores, correctores"),
        new(CatalogoMaterialesPorDefecto.CategoriaMateriales, "Grapas,Clips", 0.02m, PorEstudiante, 1, 0m, true, "Grapas, clips (CAJA)"),
        new(CatalogoMaterialesPorDefecto.CategoriaMateriales, "Porta files", 1m, PorEstudiante, 1, 0m, true, "Porta files"),
        new(CatalogoMaterialesPorDefecto.CategoriaMateriales, "Resema", 0.2m, PorEstudiante, 1, 0m, true, "Resma de papel bond de 75 gramos"),
        new(CatalogoMaterialesPorDefecto.CategoriaMateriales, "Uso de Grapadora", 1m, PorEstudianteMes, 6, 0m, true, "Grapadora"),
    ];
}
