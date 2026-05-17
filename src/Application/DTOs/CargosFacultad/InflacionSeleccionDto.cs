namespace SistemaAranceles.Application.DTOs.CargosFacultad;

/// <summary>
/// Opciones de inflación disponibles para un año.
/// Muestra tanto la inflación previsión (proyectada) como la opción de ingresar manualmente.
/// </summary>
public sealed class OpcionesInflacionPorAnioDto
{
    public int Anio { get; init; }
    public decimal? InflacionProyectada { get; init; }
    public bool TieneInflacionImportada { get; init; }
    public string DisplayText
    {
        get
        {
            if (InflacionProyectada.HasValue)
                return $"{Anio} - PROYECCIÓN: {InflacionProyectada.Value:F2}";

            if (TieneInflacionImportada)
                return $"{Anio} - IMPORTADA";

            return $"{Anio} - Sin datos";
        }
    }
}

/// <summary>
/// Selección de inflación para aplicar a cargos en un período.
/// </summary>
public sealed class SeleccionInflacionDto
{
    public int Anio { get; init; }
    public decimal FactorInflacion { get; init; }
    public bool EsValorManual { get; init; }
    public string DescripcionOrigen => EsValorManual ? "Valor manual ingresado" : "Del módulo de inflación";
}
