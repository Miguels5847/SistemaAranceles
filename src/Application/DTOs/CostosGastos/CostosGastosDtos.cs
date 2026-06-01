namespace SistemaAranceles.Application.DTOs.CostosGastos;

public static class FormatoMatrizCostosGastos
{
    public const string Moneda = "moneda";
    public const string Porcentaje = "porcentaje";
    public const string Entero = "entero";
    public const string Decimal = "decimal";

    public static string Formatear(decimal valor, string formato)
        => formato switch
        {
            Porcentaje => valor.ToString("P2"),
            Entero => valor.ToString("N0"),
            Decimal => valor.ToString("N2"),
            _ => $"$ {valor:N2}"
        };
}

public sealed class PeriodoCostoGastoDto
{
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public int Semestre => NumeroPeriodo <= 0 ? 1 : ((NumeroPeriodo - 1) % 2) + 1;
}

public sealed class CostoGastoRubroDto
{
    public string Grupo { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public bool EsEncabezadoGrupo { get; init; }
    public bool EsTotal { get; init; }
    public string FormatoValor { get; init; } = FormatoMatrizCostosGastos.Moneda;

    public IReadOnlyList<string> PeriodosDisplay => Periodos
        .Select(v => EsEncabezadoGrupo ? string.Empty : FormatoMatrizCostosGastos.Formatear(v, FormatoValor))
        .ToList();

    public string TotalDisplay => EsEncabezadoGrupo
        ? string.Empty
        : FormatoMatrizCostosGastos.Formatear(Total, FormatoValor);
}

public sealed class PonderacionCostoGastoDto
{
    public string Grupo { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public bool EsEncabezadoGrupo { get; init; }
    public bool EsTotal { get; init; }

    public IReadOnlyList<string> PeriodosDisplay => Periodos
        .Select(v => EsEncabezadoGrupo ? string.Empty : FormatoMatrizCostosGastos.Formatear(v, FormatoMatrizCostosGastos.Porcentaje))
        .ToList();

    public string TotalDisplay => EsEncabezadoGrupo
        ? string.Empty
        : FormatoMatrizCostosGastos.Formatear(Total, FormatoMatrizCostosGastos.Porcentaje);
}

public sealed class InvVinBecasPeriodoDto
{
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal EstudiantesCarrera { get; init; }
    public decimal DocentesCarrera { get; init; }
    public decimal FactorInflacion { get; init; } = 1m;
    public decimal InflacionAnual { get; init; }
    public decimal BecasInstitucionales { get; init; }
    public decimal PresupuestoUniversidad { get; init; }
    public decimal NumeroEstudiantesUniversidad { get; init; }
    public decimal Investigacion { get; init; }
    public decimal Vinculacion { get; init; }
    public decimal PresupuestoGobierno { get; init; }
    public decimal NumeroDocentesUniversidad { get; init; }
    public decimal BecasEstudiantes { get; init; }
    public decimal BecasDocentes { get; init; }
    public decimal TotalBecasGobierno => BecasEstudiantes + BecasDocentes;
    // Becas Institucionales es dato de referencia (viene de Demanda/Ingresos) y NO se suma aquí.
    public decimal TotalInvVinBecas => Investigacion + Vinculacion + BecasEstudiantes + BecasDocentes;
}

public sealed class MatrizInvVinBecasDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public IReadOnlyList<PeriodoCostoGastoDto> Periodos { get; init; } = [];
    public IReadOnlyList<InvVinBecasPeriodoDto> ValoresPorPeriodo { get; init; } = [];
    public IReadOnlyList<CostoGastoRubroDto> Filas { get; init; } = [];
    public string? MensajeAdvertencia { get; init; }
    public bool TieneDatos => ValoresPorPeriodo.Count > 0;
    public IReadOnlyList<string> EtiquetasPeriodos => Periodos.Select(p => p.Etiqueta).ToList();
    public decimal TotalGeneral => ValoresPorPeriodo.Sum(p => p.TotalInvVinBecas);
    public string TotalGeneralDisplay => $"$ {TotalGeneral:N2}";
}

public sealed class CostoGastoPeriodoDto
{
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal Mantenimiento { get; init; }
    public decimal MantenimientoEdificio { get; init; }
    public decimal CapacitacionDocente { get; init; }
    public decimal Internacionalizacion { get; init; }
    public decimal InsumosPracticasLaboratorios { get; init; }
    public decimal SueldosDocentes { get; init; }
    public decimal TiempoCompletoPhd { get; init; }
    public decimal TiempoCompletoMgs { get; init; }
    public decimal MedioTiempo { get; init; }
    public decimal TiempoParcial { get; init; }
    public decimal OcasionalTipo2TecnicoDocente { get; init; }
    public decimal SeguroEstudiantil { get; init; }
    public decimal CostoSeguroEstudiantil { get; init; }
    public decimal BecasInstitucionales { get; init; }
    public decimal Investigacion { get; init; }
    public decimal Vinculacion { get; init; }
    public decimal MaterialesSuministros { get; init; }
    public decimal Depreciacion { get; init; }
    public decimal AdministracionCentral { get; init; }
    public decimal Decano { get; init; }
    public decimal Subdecano { get; init; }
    public decimal DirectorCarrera { get; init; }
    public decimal Secretario { get; init; }
    public decimal AuxiliarSecretaria { get; init; }
    public decimal Coordinador { get; init; }
    public decimal BienestarEstudiantil { get; init; }
    public decimal Bibliotecario { get; init; }
    public decimal AuxiliarServicio { get; init; }
    public decimal Guardia { get; init; }
    public decimal MarketingComunicacion { get; init; }
    public decimal ServiciosBasicos { get; init; }
    public decimal AmortizacionActivosDiferidos { get; init; }
    public decimal Amortizacion { get; init; }
    public decimal ImprevistosRecargo { get; init; }
    public decimal Interes { get; init; }
    public decimal TotalBecasGobierno { get; init; }

    public decimal CostosPorServicios => MantenimientoEdificio
        + CapacitacionDocente
        + Internacionalizacion
        + InsumosPracticasLaboratorios
        + TiempoCompletoPhd
        + TiempoCompletoMgs
        + MedioTiempo
        + TiempoParcial
        + OcasionalTipo2TecnicoDocente
        + CostoSeguroEstudiantil
        + BecasInstitucionales
        + Investigacion
        + Vinculacion
        + MaterialesSuministros
        + Depreciacion;
    public decimal CostosServicios => CostosPorServicios;
    public decimal GastosAdministracion => AdministracionCentral
        + Decano
        + Subdecano
        + DirectorCarrera
        + Secretario
        + AuxiliarSecretaria
        + Coordinador
        + BienestarEstudiantil
        + Bibliotecario
        + AuxiliarServicio
        + Guardia;
    public decimal GastosVentas => MarketingComunicacion;
    public decimal OtrosGastos => ServiciosBasicos + Amortizacion;
    public decimal GastoFinanciero => Interes;
    public decimal TotalCostosGastos => CostosServicios + GastosAdministracion + GastosVentas + OtrosGastos + GastoFinanciero;
    public decimal TotalDescontadoBecasGobierno => TotalCostosGastos - TotalBecasGobierno;
}

public sealed class MatrizCostosGastosDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public IReadOnlyList<PeriodoCostoGastoDto> Periodos { get; init; } = [];
    public IReadOnlyList<CostoGastoPeriodoDto> ValoresPorPeriodo { get; init; } = [];
    public IReadOnlyList<CostoGastoRubroDto> ProyeccionCostosGastos { get; init; } = [];
    public IReadOnlyList<PonderacionCostoGastoDto> Ponderacion { get; init; } = [];
    public IReadOnlyList<CostoGastoRubroDto> DescontadoBecasGobierno { get; init; } = [];
    public string? MensajeAdvertencia { get; init; }
    public IReadOnlyList<string> EtiquetasPeriodos => Periodos.Select(p => p.Etiqueta).ToList();
    public bool TieneDatos => ValoresPorPeriodo.Count > 0;
    public decimal TotalGeneral => ValoresPorPeriodo.Sum(p => p.TotalCostosGastos);
    public decimal TotalGeneralDescontado => ValoresPorPeriodo.Sum(p => p.TotalDescontadoBecasGobierno);
    public string TotalGeneralDisplay => $"$ {TotalGeneral:N2}";
    public string TotalGeneralDescontadoDisplay => $"$ {TotalGeneralDescontado:N2}";
}

public sealed class CostoCarreraPeriodoDto
{
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal TotalCostosGastos { get; init; }
    public decimal NumeroEstudiantes { get; init; }
    public decimal CostoPorEstudiante { get; init; }
}

public sealed class CostoCarreraResultadoDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public IReadOnlyList<CostoCarreraPeriodoDto> Periodos { get; init; } = [];
    public int NumeroSemestresCarrera { get; init; }
    public decimal PorcentajeMatriculaAplicado { get; init; }
    public decimal CostoCarreraCompleta { get; init; }
    public decimal ArancelSugeridoSemestre { get; init; }
    public decimal MatriculaSugerida { get; init; }
    public decimal TotalPorSemestre { get; init; }
    public string? MensajeAdvertencia { get; init; }

    public bool TieneDatos => Periodos.Count > 0 && ArancelSugeridoSemestre > 0m;
    public IReadOnlyList<string> EtiquetasPeriodos => Periodos.Select(p => p.EtiquetaPeriodo).ToList();
    public string CostoCarreraCompletaDisplay => $"$ {CostoCarreraCompleta:N2}";
    public string ArancelSugeridoDisplay => ArancelSugeridoSemestre > 0m ? $"$ {ArancelSugeridoSemestre:N2}" : "Pendiente";
    public string MatriculaSugeridaDisplay => $"$ {MatriculaSugerida:N2}";
    public string TotalPorSemestreDisplay => $"$ {TotalPorSemestre:N2}";
    public string PorcentajeMatriculaDisplay => $"{PorcentajeMatriculaAplicado:0.##}%";
    public string EstadoTexto => string.IsNullOrWhiteSpace(MensajeAdvertencia)
        ? "Costo de carrera calculado."
        : MensajeAdvertencia!;
}

public sealed class ArancelOptimoCarreraDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public decimal? ArancelSugeridoSemestre { get; init; }
    public decimal MatriculaSugerida { get; init; }
    public decimal PorcentajeMatriculaAplicado { get; init; }
    public decimal TotalPorSemestre { get; init; }
    public string FuenteCalculo { get; init; } = "Costo de la Carrera";
    public string? MensajeAdvertencia { get; init; }

    public bool Disponible => ArancelSugeridoSemestre is > 0m;
    public string EstadoTexto => Disponible
        ? "Arancel sugerido disponible."
        : MensajeAdvertencia ?? "Costo de carrera pendiente.";
    public string ArancelSugeridoDisplay => Disponible
        ? $"$ {ArancelSugeridoSemestre!.Value:N2}"
        : "Pendiente";
    public string MatriculaSugeridaDisplay => $"$ {MatriculaSugerida:N2}";
    public string TotalPorSemestreDisplay => $"$ {TotalPorSemestre:N2}";
}
