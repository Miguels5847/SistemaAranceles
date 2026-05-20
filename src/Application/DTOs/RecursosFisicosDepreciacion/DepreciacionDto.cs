using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;

public sealed class PeriodoDepreciacionDto
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}

public sealed class CeldaDepreciacionDto
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public decimal DepreciacionPeriodo { get; init; }
    public decimal DepreciacionAcumulada { get; init; }

    public string DepreciacionPeriodoDisplay => DepreciacionPeriodo == 0m ? "$ -" : DepreciacionPeriodo.ToString("C2");
    public string DepreciacionAcumuladaDisplay => DepreciacionAcumulada == 0m ? "$ -" : DepreciacionAcumulada.ToString("C2");
}

public sealed class FilaMatrizDepreciacionDto
{
    public int ActivoFijoId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public CategoriaActivoFijo Categoria { get; init; }
    public string CategoriaNombre { get; init; } = string.Empty;
    public decimal ValorInicial { get; init; }
    public decimal ValorResidual { get; init; }
    public int VidaUtilAnios { get; init; }
    public IReadOnlyList<CeldaDepreciacionDto> Celdas { get; init; } = [];

    public string ValorInicialDisplay => ValorInicial == 0m ? "$ -" : ValorInicial.ToString("C2");
    public string ValorResidualDisplay => ValorResidual == 0m ? "$ -" : ValorResidual.ToString("C2");
}

public sealed class TotalPeriodoDepreciacionDto
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public decimal DepreciacionPeriodo { get; init; }
    public decimal DepreciacionAcumulada { get; init; }

    public string DepreciacionPeriodoDisplay => DepreciacionPeriodo == 0m ? "$ -" : DepreciacionPeriodo.ToString("C2");
    public string DepreciacionAcumuladaDisplay => DepreciacionAcumulada == 0m ? "$ -" : DepreciacionAcumulada.ToString("C2");
}

public sealed class MatrizDepreciacionDto
{
    public int CarreraId { get; init; }
    public int EscenarioProyeccionId { get; init; }
    public IReadOnlyList<PeriodoDepreciacionDto> Periodos { get; init; } = [];
    public IReadOnlyList<FilaMatrizDepreciacionDto> Filas { get; init; } = [];
    public IReadOnlyList<TotalPeriodoDepreciacionDto> TotalesPorPeriodo { get; init; } = [];
}
