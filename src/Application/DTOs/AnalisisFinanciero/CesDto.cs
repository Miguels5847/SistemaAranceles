namespace SistemaAranceles.Application.DTOs.AnalisisFinanciero;

/// <summary>
/// Cuadros regulatorios del CES (Consejo de Educación Superior). Reproduce las hojas Excel
/// "INF CES" (presupuesto de la 1ª cohorte por las 4 funciones sustantivas) y "CES"
/// (parámetros de justificación del arancel), más una distribución referencial de costos.
/// </summary>
public sealed class CesInfFilaDto
{
    public string Concepto { get; init; } = string.Empty;
    public string ProvisionDisplay { get; init; } = string.Empty;
    public string FomentoDisplay { get; init; } = string.Empty;
    public string VinculacionDisplay { get; init; } = string.Empty;
    public string OtrosDisplay { get; init; } = string.Empty;
    public string TotalDisplay { get; init; } = string.Empty;
    public string PorcentajeDisplay { get; init; } = string.Empty;
    public string TipoFila { get; init; } = "detalle";
    public bool EsSeccion => string.Equals(TipoFila, "seccion", StringComparison.OrdinalIgnoreCase);
    public bool EsTotal => string.Equals(TipoFila, "total", StringComparison.OrdinalIgnoreCase);
    public bool EsResultado => string.Equals(TipoFila, "resultado", StringComparison.OrdinalIgnoreCase);
}

public sealed class CesParametroFilaDto
{
    public string Parametro { get; init; } = string.Empty;
    public string Criterio { get; init; } = string.Empty;
    public string ValorDisplay { get; init; } = string.Empty;
    public bool EsResultado { get; init; }
}

public sealed class CesDistribucionFilaDto
{
    public string Categoria { get; init; } = string.Empty;
    public string MontoDisplay { get; init; } = string.Empty;
    public string PorcentajeDisplay { get; init; } = string.Empty;
    public string TipoFila { get; init; } = "detalle";
    public bool EsTotal => string.Equals(TipoFila, "total", StringComparison.OrdinalIgnoreCase);
    public bool EsReferencial => string.Equals(TipoFila, "referencial", StringComparison.OrdinalIgnoreCase);
}

public sealed class CesDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;

    // INF CES (presupuesto 1ª cohorte por función) + bloque arancel.
    public IReadOnlyList<CesInfFilaDto> InfCes { get; init; } = [];
    public string ArancelPorSemestreDisplay { get; init; } = string.Empty;
    // Costo referencial (costo carrera ÷ semestres); se muestra junto al arancel óptimo para diferenciarlos.
    public string CostoPorSemestreDisplay { get; init; } = string.Empty;
    public string MatriculaDisplay { get; init; } = string.Empty;
    public string TotalPorSemestreDisplay { get; init; } = string.Empty;

    // CES (parámetros de justificación del arancel).
    public IReadOnlyList<CesParametroFilaDto> Parametros { get; init; } = [];

    // Distribución referencial: cómo se reparte el 100% del costo de la carrera por categoría.
    public IReadOnlyList<CesDistribucionFilaDto> Distribucion { get; init; } = [];

    public string? MensajeAdvertencia { get; init; }
    public bool TieneDatos => InfCes.Count > 0;
}
