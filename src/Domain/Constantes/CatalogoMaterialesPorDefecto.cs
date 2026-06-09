namespace SistemaAranceles.Domain.Constantes;

/// <summary>
/// Un material/insumo del catálogo por defecto de Capital de Trabajo.
/// </summary>
public sealed record MaterialPorDefecto(
    string Categoria,
    string Nombre,
    string Unidad,
    decimal CantidadBase,
    decimal PrecioUnitario);

/// <summary>
/// Catálogo canónico de materiales y suministros que aplican a (casi) todas las carreras.
/// Fuente única reutilizada por la semilla inicial (carrera base) y por el botón
/// "Generar materiales por defecto" que los siembra en cualquier carrera.
/// </summary>
public static class CatalogoMaterialesPorDefecto
{
    public const string CategoriaMateriales = "MATERIALES_SUMINISTROS";
    public const string CategoriaAseo = "ASEO_LIMPIEZA";
    public const string CategoriaAccesorios = "ACCESORIOS_MATERIALES";

    public static IReadOnlyList<MaterialPorDefecto> Items { get; } =
    [
        // B. Materiales y Suministros
        new(CategoriaMateriales, "Resma de papel bond de 75 gramos", "Resma", 6.0m, 3.25m),
        new(CategoriaMateriales, "Cartuchos de impresora (Color)", "Unidad", 0.6m, 50.00m),
        new(CategoriaMateriales, "Cartuchos de impresora (Negro)", "Unidad", 0.6m, 40.00m),
        new(CategoriaMateriales, "Carpetas de cartón", "Unidad", 60.0m, 1.00m),
        new(CategoriaMateriales, "Porta files", "Unidad", 30.0m, 2.00m),
        new(CategoriaMateriales, "Esferos, micro minas, lápiz, borradores, correctores", "Unidad", 30.0m, 0.25m),
        new(CategoriaMateriales, "Grapas, clips (CAJA)", "Caja", 0.6m, 1.00m),

        // C. Suministros de Aseo y Limpieza
        new(CategoriaAseo, "Desinfectante (Galón)", "Galón", 0.9m, 4.00m),
        new(CategoriaAseo, "Jabón Líquido (Galón)", "Galón", 1.08m, 3.00m),
        new(CategoriaAseo, "Papel Higiénico (Rollo Grande)", "Rollo", 23.4m, 10.50m),
        new(CategoriaAseo, "Escoba", "Unidad", 2.0m, 10.50m),
        new(CategoriaAseo, "Paquete de Fundas de Basura", "Paquete", 6.0m, 0.80m),
        new(CategoriaAseo, "Trapeador", "Unidad", 3.0m, 2.50m),
        new(CategoriaAseo, "Cloro (Galón)", "Galón", 0.9m, 2.50m),

        // D. Accesorios y Materiales
        new(CategoriaAccesorios, "Grapadora", "Unidad", 5.25m, 15.00m),
        new(CategoriaAccesorios, "Perforadora", "Unidad", 5.25m, 10.00m),
    ];
}
