using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.DTOs.Mantenimiento;

public sealed class ServicioMantenimientoDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public TipoRubroMantenimiento TipoRubro { get; init; }
    public string TipoRubroNombre { get; init; } = string.Empty;
    public string NombreRubro { get; init; } = string.Empty;
    public decimal CostoAnualUniversidad { get; init; }
    public string CostoAnualDisplay => CostoAnualUniversidad == 0m ? "$ -" : CostoAnualUniversidad.ToString("N2");
}

public sealed class CrearServicioMantenimientoDto
{
    public int CarreraId { get; init; }
    public TipoRubroMantenimiento TipoRubro { get; init; }
    public string NombreRubro { get; init; } = string.Empty;
    public decimal CostoAnualUniversidad { get; init; }
}

public sealed class ActualizarServicioMantenimientoDto
{
    public int Id { get; init; }
    public TipoRubroMantenimiento TipoRubro { get; init; }
    public string NombreRubro { get; init; } = string.Empty;
    public decimal CostoAnualUniversidad { get; init; }
}
