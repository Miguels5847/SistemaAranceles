namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class MaterialCantidadCeldaDto
{
    public int RatioId { get; init; }
    public string Categoria { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal Estudiantes { get; init; }
    public decimal Cantidad { get; init; }

    public string CantidadDisplay => Cantidad.ToString("N4");
    public string EstudiantesDisplay => Estudiantes.ToString("N2");
}

public sealed class MaterialMonetarioCeldaDto
{
    public int RatioId { get; init; }
    public string Categoria { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal FactorInflacion { get; init; }
    public decimal Costo { get; init; }

    public string CantidadDisplay => Cantidad.ToString("N4");
    public string PrecioDisplay => $"$ {PrecioUnitario:N2}";
    public string FactorDisplay => FactorInflacion.ToString("N4");
    public string CostoDisplay => $"$ {Costo:N2}";
}

public sealed class MaterialesProyectadosDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public int? AnioBaseInflacion { get; init; }
    public string? MensajeAdvertencia { get; init; }

    public IReadOnlyList<MaterialCantidadCeldaDto> Cantidades { get; init; } = [];
    public IReadOnlyList<MaterialMonetarioCeldaDto> Monetarios { get; init; } = [];

    public decimal TotalCantidad => Cantidades.Sum(c => c.Cantidad);
    public decimal TotalCosto => Monetarios.Sum(m => m.Costo);

    public string TotalCantidadDisplay => TotalCantidad.ToString("N2");
    public string TotalCostoDisplay => $"$ {TotalCosto:N2}";
}
