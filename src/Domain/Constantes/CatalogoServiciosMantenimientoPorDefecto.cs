using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Domain.Constantes;

/// <summary>
/// Un rubro de servicio básico o mantenimiento por defecto (réplica de Administración de Empresas).
/// </summary>
public sealed record ServicioMantenimientoPorDefecto(
    TipoRubroMantenimiento Tipo,
    string Nombre,
    string Sede,
    decimal CostoAnual);

/// <summary>
/// Catálogo canónico de servicios básicos + mantenimiento por defecto. Alimenta el botón
/// "Generar servicios por defecto" para sembrar una carrera (config general, escenario NULL).
/// </summary>
public static class CatalogoServiciosMantenimientoPorDefecto
{
    public static IReadOnlyList<ServicioMantenimientoPorDefecto> Items { get; } =
    [
        new(TipoRubroMantenimiento.ServicioBasico, "Agua", "General", 300000m),
        new(TipoRubroMantenimiento.ServicioBasico, "Comunicaciones", "General", 108000m),
        new(TipoRubroMantenimiento.ServicioBasico, "Energía Eléctrica", "General", 500000m),
        new(TipoRubroMantenimiento.ServicioBasico, "Internet", "General", 150000m),
        new(TipoRubroMantenimiento.Mantenimiento, "Garantía", "General", 1000m),
        new(TipoRubroMantenimiento.Mantenimiento, "Infraestructura", "General", 400000m),
        new(TipoRubroMantenimiento.Mantenimiento, "Limpieza", "General", 480000m),
        new(TipoRubroMantenimiento.Mantenimiento, "Refacciones", "General", 340540m),
        new(TipoRubroMantenimiento.Mantenimiento, "Seguridad", "General", 960000m),
        new(TipoRubroMantenimiento.Mantenimiento, "Seguros", "General", 720000m),
    ];
}
