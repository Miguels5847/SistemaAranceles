namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class IngresoPeriodoCeldaDto
{
    public int NumeroCiclo { get; init; }
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal Estudiantes { get; init; }
    public decimal IngresoBruto { get; init; }
    public decimal Becas { get; init; }
    public decimal IngresoNeto { get; init; }

    public string CicloDisplay => $"Ciclo {NumeroCiclo}";
    public string EstudiantesDisplay => Estudiantes.ToString("N2");
    public string BrutoDisplay => $"$ {IngresoBruto:N2}";
    public string BecasDisplay => $"$ {Becas:N2}";
    public string NetoDisplay => $"$ {IngresoNeto:N2}";
}

public sealed class IngresoFilaCicloDto
{
    public int NumeroCiclo { get; init; }
    public IReadOnlyList<IngresoPeriodoCeldaDto> Periodos { get; init; } = [];

    public decimal TotalEstudiantes => Periodos.Sum(p => p.Estudiantes);
    public decimal TotalBruto => Periodos.Sum(p => p.IngresoBruto);
    public decimal TotalBecas => Periodos.Sum(p => p.Becas);
    public decimal TotalNeto => Periodos.Sum(p => p.IngresoNeto);

    public string CicloDisplay => $"Ciclo {NumeroCiclo}";
    public string TotalEstudiantesDisplay => TotalEstudiantes.ToString("N2");
    public string TotalBrutoDisplay => $"$ {TotalBruto:N2}";
    public string TotalBecasDisplay => $"$ {TotalBecas:N2}";
    public string TotalNetoDisplay => $"$ {TotalNeto:N2}";
}

public sealed class IngresosProyectadosDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public decimal ArancelEfectivo { get; init; }
    public decimal MatriculaEfectiva { get; init; }
    public decimal PorcentajeBecasAplicado { get; init; }
    public string? MensajeAdvertencia { get; init; }

    public IReadOnlyList<string> EtiquetasPeriodos { get; init; } = [];
    public IReadOnlyList<IngresoFilaCicloDto> Filas { get; init; } = [];

    /// <summary>Vista plana: una fila por celda (Ciclo × Periodo). Poblada por el query.</summary>
    public IReadOnlyList<IngresoPeriodoCeldaDto> CeldasPlanas { get; init; } = [];

    public decimal TotalEstudiantesPeriodo(int periodoAcademicoId)
        => Filas.SelectMany(f => f.Periodos)
            .Where(p => p.PeriodoAcademicoId == periodoAcademicoId)
            .Sum(p => p.Estudiantes);

    public decimal TotalNetoPeriodo(int periodoAcademicoId)
        => Filas.SelectMany(f => f.Periodos)
            .Where(p => p.PeriodoAcademicoId == periodoAcademicoId)
            .Sum(p => p.IngresoNeto);

    public decimal TotalGeneralBruto => Filas.Sum(f => f.TotalBruto);
    public decimal TotalGeneralBecas => Filas.Sum(f => f.TotalBecas);
    public decimal TotalGeneralNeto => Filas.Sum(f => f.TotalNeto);

    public string ArancelDisplay => $"$ {ArancelEfectivo:N2}";
    public string MatriculaDisplay => $"$ {MatriculaEfectiva:N2}";
    public string PorcentajeBecasDisplay => $"{PorcentajeBecasAplicado:0.##}%";
    public string TotalBrutoDisplay => $"$ {TotalGeneralBruto:N2}";
    public string TotalBecasDisplay => $"$ {TotalGeneralBecas:N2}";
    public string TotalNetoDisplay => $"$ {TotalGeneralNeto:N2}";
}
