using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Domain.Constantes;

/// <summary>Un rubro de activo diferido por defecto (permisos legales amortizables).</summary>
public sealed record ActivoDiferidoPorDefecto(string Nombre, decimal Valor, decimal TasaAnual);

/// <summary>
/// Catálogo canónico de activos diferidos por defecto (KAN-31): permisos legales con valor 0
/// para que el administrador cargue el monto real. Alimenta la siembra al crear una carrera y el
/// botón "Generar por defecto" del módulo de Activos Diferidos.
/// </summary>
public static class CatalogoActivosDiferidosPorDefecto
{
    public static IReadOnlyList<ActivoDiferidoPorDefecto> Items { get; } =
    [
        new("Permiso Municipal", 0m, ActivoDiferido.TasaPorDefecto),
        new("Permiso de Bomberos", 0m, ActivoDiferido.TasaPorDefecto),
    ];
}
