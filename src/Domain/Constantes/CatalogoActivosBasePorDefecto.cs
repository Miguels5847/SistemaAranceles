using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Domain.Constantes;

/// <summary>
/// Una plantilla de activo fijo del catálogo por defecto (réplica de Administración de Empresas).
/// </summary>
public sealed record ActivoBasePorDefecto(
    string Descripcion,
    CategoriaActivoFijo Categoria,
    TipoCalculoCantidad TipoCalculo,
    decimal CantidadDefault,
    string UnidadMedida,
    decimal ValorUnitario,
    decimal FactorMultiplicador,
    decimal OffsetCantidad,
    int VidaUtilAnios,
    decimal PorcentajeResidual);

/// <summary>
/// Catálogo canónico de activos fijos por defecto. Alimenta catalogo_activo_base (global) para que el
/// botón "Generar activos por defecto" siembre cualquier carrera. Réplica de Administración de Empresas
/// (sin el registro de prueba "Laboratorio prueba"). EVEA/Zoom suelen existir ya en el catálogo (KAN-24).
/// </summary>
public static class CatalogoActivosBasePorDefecto
{
    public static IReadOnlyList<ActivoBasePorDefecto> Items { get; } =
    [
        new("Operatividad EVEA", CategoriaActivoFijo.LaboratoriosEquipos, TipoCalculoCantidad.PorEstudiante, 0m, "UNI", 9.00m, 1m, 0m, 1, 0.05m),
        new("Licencias Zoom", CategoriaActivoFijo.LaboratoriosEquipos, TipoCalculoCantidad.PorDocente, 0m, "UNI", 13.50m, 1m, 0m, 1, 0.05m),
        new("Archivador de madera", CategoriaActivoFijo.MueblesEnseres, TipoCalculoCantidad.Manual, 2m, "UNI", 20.00m, 1m, 0m, 10, 0.05m),
        new("Escritorio docentes", CategoriaActivoFijo.MueblesEnseres, TipoCalculoCantidad.Manual, 2m, "UNI", 30.00m, 1m, 0m, 10, 0.05m),
        new("Libros", CategoriaActivoFijo.MueblesEnseres, TipoCalculoCantidad.Manual, 10m, "UNI", 5.00m, 1m, 0m, 10, 0.05m),
        new("Mesas de laboratorio", CategoriaActivoFijo.MueblesEnseres, TipoCalculoCantidad.Manual, 1m, "UNI", 25.00m, 1m, 0m, 10, 0.05m),
        new("Silla de Espera", CategoriaActivoFijo.MueblesEnseres, TipoCalculoCantidad.Manual, 2m, "UNI", 45.50m, 1m, 0m, 10, 0.05m),
        new("Silla Giratoria", CategoriaActivoFijo.MueblesEnseres, TipoCalculoCantidad.Manual, 1m, "UNI", 1.00m, 1m, 0m, 10, 0.05m),
    ];
}
