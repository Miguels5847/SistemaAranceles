using SistemaAranceles.Application.DTOs.Mantenimiento;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.Mantenimiento;

internal static class MapeoServicioMantenimiento
{
    public static ServicioMantenimientoDto ADto(ServicioMantenimiento s) => new()
    {
        Id = s.Id,
        CarreraId = s.CarreraId,
        TipoRubro = s.TipoRubro,
        TipoRubroNombre = NombreTipo(s.TipoRubro),
        NombreRubro = s.NombreRubro,
        CostoAnualUniversidad = s.CostoAnualUniversidad,
    };

    public static string NombreTipo(TipoRubroMantenimiento tipo) => tipo switch
    {
        TipoRubroMantenimiento.ServicioBasico => "Servicios Básicos",
        TipoRubroMantenimiento.Mantenimiento => "Mantenimiento",
        _ => tipo.ToString()
    };
}
