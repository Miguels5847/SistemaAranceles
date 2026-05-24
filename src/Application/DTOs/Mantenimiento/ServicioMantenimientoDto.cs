using System.Globalization;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.DTOs.Mantenimiento;

public sealed class ServicioMantenimientoDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public int? EscenarioProyeccionId { get; init; }
    public string Sede { get; init; } = "General";
    public TipoRubroMantenimiento TipoRubro { get; init; }
    public string TipoRubroNombre { get; init; } = string.Empty;
    public string NombreRubro { get; init; } = string.Empty;
    public decimal CostoAnualUniversidad { get; init; }
    public string CostoAnualDisplay => FormatoMonetarioMantenimiento.Formatear(CostoAnualUniversidad);
}

internal static class FormatoMonetarioMantenimiento
{
    private static readonly CultureInfo Cultura = new("es-EC");

    public static string Formatear(decimal valor, bool mostrarSimbolo = true)
    {
        if (valor == 0m)
            return mostrarSimbolo ? "$ -" : "-";

        var formato = decimal.Truncate(valor) == valor ? "N0" : "N2";
        var texto = valor.ToString(formato, Cultura);
        return mostrarSimbolo ? $"$ {texto}" : texto;
    }
}

public sealed class CrearServicioMantenimientoDto
{
    public int CarreraId { get; init; }
    public int? EscenarioProyeccionId { get; init; }
    public string Sede { get; init; } = "General";
    public TipoRubroMantenimiento TipoRubro { get; init; }
    public string NombreRubro { get; init; } = string.Empty;
    public decimal CostoAnualUniversidad { get; init; }
}

public sealed class ActualizarServicioMantenimientoDto
{
    public int Id { get; init; }
    public int? EscenarioProyeccionId { get; init; }
    public string Sede { get; init; } = "General";
    public TipoRubroMantenimiento TipoRubro { get; init; }
    public string NombreRubro { get; init; } = string.Empty;
    public decimal CostoAnualUniversidad { get; init; }
}
