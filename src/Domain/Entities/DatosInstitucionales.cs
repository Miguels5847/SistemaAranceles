using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Snapshot institucional agregado (KAN-22): poblacion y rubros monetarios de planta central
/// usados como fuente unica para calculo de aporte de carrera y modulos consumidores.
/// </summary>
public sealed class DatosInstitucionales : EntidadDominioBase
{
    public const int MesesCapitalTrabajoPorDefecto = 2;
    public const decimal PorcentajeImprevistosInversionPorDefecto = 5m;
    public const decimal PorcentajeMatriculaDefaultPorDefecto = 10m;
    public const decimal PorcentajeBecasInstitucionalesPorDefecto = 10m;
    public const int SemestresPorAnioPorDefecto = 2;
    public const int MesesOperativosCicloPorDefecto = 6;
    public const decimal PresupuestoBaseUniversidadPorDefecto = 0m;
    public const decimal PresupuestoGobiernoBecasPorDefecto = 0m;
    public const decimal PorcentajeInvestigacionPorDefecto = 5m;
    public const decimal PorcentajeVinculacionPorDefecto = 1m;
    public const decimal PorcentajeBecasEstudiantesPorDefecto = 90m;
    public const decimal PorcentajeBecasDocentesPorDefecto = 10m;

    private DatosInstitucionales()
    {
        MesesCapitalTrabajo = MesesCapitalTrabajoPorDefecto;
        PorcentajeImprevistosInversion = PorcentajeImprevistosInversionPorDefecto;
        PorcentajeMatriculaDefault = PorcentajeMatriculaDefaultPorDefecto;
        PorcentajeBecasInstitucionales = PorcentajeBecasInstitucionalesPorDefecto;
        SemestresPorAnio = SemestresPorAnioPorDefecto;
        MesesOperativosCiclo = MesesOperativosCicloPorDefecto;
        PresupuestoBaseUniversidad = PresupuestoBaseUniversidadPorDefecto;
        PresupuestoGobiernoBecas = PresupuestoGobiernoBecasPorDefecto;
        PorcentajeInvestigacion = PorcentajeInvestigacionPorDefecto;
        PorcentajeVinculacion = PorcentajeVinculacionPorDefecto;
        PorcentajeBecasEstudiantes = PorcentajeBecasEstudiantesPorDefecto;
        PorcentajeBecasDocentes = PorcentajeBecasDocentesPorDefecto;
    }

    public DatosInstitucionales(
        string periodo,
        int numeroEstudiantesUniversidad,
        int numeroDocentesUniversidad,
        int numeroPersonasPlantaCentral,
        decimal sueldoBasico,
        decimal funcional,
        decimal fondoReserva,
        decimal beneficioXiv,
        decimal beneficioXiii,
        decimal aportePatronal,
        decimal varios,
        int actualizadoPorUsuarioId,
        string? fuenteNotas,
        int mesesCapitalTrabajo = MesesCapitalTrabajoPorDefecto,
        decimal porcentajeImprevistosInversion = PorcentajeImprevistosInversionPorDefecto)
    {
        CambiarPeriodo(periodo);
        CambiarPoblacion(numeroEstudiantesUniversidad, numeroDocentesUniversidad, numeroPersonasPlantaCentral);
        CambiarRubros(sueldoBasico, funcional, fondoReserva, beneficioXiv, beneficioXiii, aportePatronal, varios);
        CambiarParametrosInversion(mesesCapitalTrabajo, porcentajeImprevistosInversion);
        RegistrarActualizacion(actualizadoPorUsuarioId, fuenteNotas);
    }

    public string Periodo { get; private set; } = string.Empty;

    public int NumeroEstudiantesUniversidad { get; private set; }
    public int NumeroDocentesUniversidad { get; private set; }
    public int NumeroPersonasPlantaCentral { get; private set; }

    public decimal SueldoBasico { get; private set; }
    public decimal Funcional { get; private set; }
    public decimal FondoReserva { get; private set; }
    public decimal BeneficioXiv { get; private set; }
    public decimal BeneficioXiii { get; private set; }
    public decimal AportePatronal { get; private set; }
    public decimal Varios { get; private set; }
    public int MesesCapitalTrabajo { get; private set; } = MesesCapitalTrabajoPorDefecto;
    public decimal PorcentajeImprevistosInversion { get; private set; } = PorcentajeImprevistosInversionPorDefecto;

    // KAN-35: parámetros Épica 9 (Demanda e Ingresos)
    public decimal PorcentajeMatriculaDefault { get; private set; } = PorcentajeMatriculaDefaultPorDefecto;
    public decimal PorcentajeBecasInstitucionales { get; private set; } = PorcentajeBecasInstitucionalesPorDefecto;
    public int SemestresPorAnio { get; private set; } = SemestresPorAnioPorDefecto;
    public int MesesOperativosCiclo { get; private set; } = MesesOperativosCicloPorDefecto;
    public decimal PresupuestoAnualCapacitacion { get; private set; }
    public decimal PresupuestoAnualInternacionalizacion { get; private set; }
    public decimal PresupuestoAnualMarketing { get; private set; }
    public decimal PolizaSeguroEstudiantilAnual { get; private set; }
    public string? FuenteInflacion { get; private set; }
    public int? AnioBaseProyeccion { get; private set; }

    // KAN-36: parámetros Épica 10 (Costos y Gastos)
    public decimal PresupuestoBaseUniversidad { get; private set; } = PresupuestoBaseUniversidadPorDefecto;
    public decimal PresupuestoGobiernoBecas { get; private set; } = PresupuestoGobiernoBecasPorDefecto;
    public decimal PorcentajeInvestigacion { get; private set; } = PorcentajeInvestigacionPorDefecto;
    public decimal PorcentajeVinculacion { get; private set; } = PorcentajeVinculacionPorDefecto;
    public decimal PorcentajeBecasEstudiantes { get; private set; } = PorcentajeBecasEstudiantesPorDefecto;
    public decimal PorcentajeBecasDocentes { get; private set; } = PorcentajeBecasDocentesPorDefecto;

    public DateTimeOffset FechaActualizacion { get; private set; }
    public int ActualizadoPorUsuarioId { get; private set; }
    public string? FuenteNotas { get; private set; }

    public decimal MasaSalarialMensual => SueldoBasico + Funcional + FondoReserva + BeneficioXiv + BeneficioXiii + AportePatronal + Varios;
    public decimal TotalMensualPlantaCentral => MasaSalarialMensual;
    public decimal TotalAnualPlantaCentral => Math.Round(TotalMensualPlantaCentral * 12m, 2);
    public decimal RatioAdminPorDocente => NumeroDocentesUniversidad <= 0
        ? 0m
        : Math.Round((decimal)NumeroPersonasPlantaCentral / NumeroDocentesUniversidad, 4);
    public decimal CostoPlantaCentralPorEstudianteAnual => NumeroEstudiantesUniversidad <= 0
        ? 0m
        : Math.Round(TotalAnualPlantaCentral / NumeroEstudiantesUniversidad, 4);

    public void CambiarPeriodo(string periodo)
    {
        Periodo = GuardiaDominio.Requerido(periodo, "Período", 20);
    }

    public void CambiarPoblacion(int estudiantesUniversidad, int docentesUniversidad, int personasPlantaCentral)
    {
        NumeroEstudiantesUniversidad = GuardiaDominio.EnteroPositivo(estudiantesUniversidad, "Número de estudiantes universidad");
        NumeroDocentesUniversidad = GuardiaDominio.EnteroPositivo(docentesUniversidad, "Número de docentes universidad");
        NumeroPersonasPlantaCentral = GuardiaDominio.EnteroNoNegativo(personasPlantaCentral, "Número de personas planta central");
    }

    public void CambiarRubros(
        decimal sueldoBasico,
        decimal funcional,
        decimal fondoReserva,
        decimal beneficioXiv,
        decimal beneficioXiii,
        decimal aportePatronal,
        decimal varios)
    {
        SueldoBasico = GuardiaDominio.DecimalNoNegativo(sueldoBasico, "Sueldo básico", 2);
        Funcional = GuardiaDominio.DecimalNoNegativo(funcional, "Funcional", 2);
        FondoReserva = GuardiaDominio.DecimalNoNegativo(fondoReserva, "Fondo de reserva", 2);
        BeneficioXiv = GuardiaDominio.DecimalNoNegativo(beneficioXiv, "Beneficio XIV", 2);
        BeneficioXiii = GuardiaDominio.DecimalNoNegativo(beneficioXiii, "Beneficio XIII", 2);
        AportePatronal = GuardiaDominio.DecimalNoNegativo(aportePatronal, "Aporte patronal", 2);
        Varios = GuardiaDominio.DecimalNoNegativo(varios, "Varios", 2);
    }

    public void CambiarParametrosInversion(int mesesCapitalTrabajo, decimal porcentajeImprevistosInversion)
    {
        MesesCapitalTrabajo = GuardiaDominio.EnteroPositivo(mesesCapitalTrabajo, "Meses capital trabajo");
        PorcentajeImprevistosInversion = GuardiaDominio.Porcentaje(
            porcentajeImprevistosInversion,
            "Porcentaje imprevistos inversión");
    }

    /// <summary>KAN-35: parámetros globales para módulo Demanda e Ingresos.</summary>
    public void CambiarParametrosDemandaIngresos(
        decimal porcentajeMatriculaDefault,
        decimal porcentajeBecasInstitucionales,
        int semestresPorAnio,
        int mesesOperativosCiclo,
        decimal presupuestoAnualCapacitacion,
        decimal presupuestoAnualInternacionalizacion,
        decimal presupuestoAnualMarketing,
        decimal polizaSeguroEstudiantilAnual,
        string? fuenteInflacion,
        int? anioBaseProyeccion)
    {
        PorcentajeMatriculaDefault = GuardiaDominio.Porcentaje(porcentajeMatriculaDefault, "Porcentaje matrícula default");
        PorcentajeBecasInstitucionales = GuardiaDominio.Porcentaje(porcentajeBecasInstitucionales, "Porcentaje becas institucionales");

        if (semestresPorAnio is <= 0 or > 4)
            throw new DominioException("Semestres por año debe estar entre 1 y 4.");
        SemestresPorAnio = semestresPorAnio;

        if (mesesOperativosCiclo is <= 0 or > 12)
            throw new DominioException("Meses operativos por ciclo debe estar entre 1 y 12.");
        MesesOperativosCiclo = mesesOperativosCiclo;

        PresupuestoAnualCapacitacion = GuardiaDominio.DecimalNoNegativo(presupuestoAnualCapacitacion, "Presupuesto capacitación", 2);
        PresupuestoAnualInternacionalizacion = GuardiaDominio.DecimalNoNegativo(presupuestoAnualInternacionalizacion, "Presupuesto internacionalización", 2);
        PresupuestoAnualMarketing = GuardiaDominio.DecimalNoNegativo(presupuestoAnualMarketing, "Presupuesto marketing", 2);
        PolizaSeguroEstudiantilAnual = GuardiaDominio.DecimalNoNegativo(polizaSeguroEstudiantilAnual, "Póliza seguro estudiantil", 2);

        FuenteInflacion = string.IsNullOrWhiteSpace(fuenteInflacion) ? null : fuenteInflacion.Trim();
        if (anioBaseProyeccion is int a && (a < 2010 || a > 2050))
            throw new DominioException("Año base proyección debe estar entre 2010 y 2050.");
        AnioBaseProyeccion = anioBaseProyeccion;
    }

    /// <summary>KAN-36: parámetros globales para módulo Costos y Gastos.</summary>
    public void CambiarParametrosCostosGastos(
        decimal presupuestoBaseUniversidad,
        decimal presupuestoGobiernoBecas,
        decimal porcentajeInvestigacion,
        decimal porcentajeVinculacion,
        decimal porcentajeBecasEstudiantes,
        decimal porcentajeBecasDocentes)
    {
        PresupuestoBaseUniversidad = GuardiaDominio.DecimalNoNegativo(presupuestoBaseUniversidad, "Presupuesto base universidad", 2);
        PresupuestoGobiernoBecas = GuardiaDominio.DecimalNoNegativo(presupuestoGobiernoBecas, "Presupuesto gobierno becas", 2);
        PorcentajeInvestigacion = GuardiaDominio.Porcentaje(porcentajeInvestigacion, "Porcentaje investigación");
        PorcentajeVinculacion = GuardiaDominio.Porcentaje(porcentajeVinculacion, "Porcentaje vinculación");
        PorcentajeBecasEstudiantes = GuardiaDominio.Porcentaje(porcentajeBecasEstudiantes, "Porcentaje becas estudiantes");
        PorcentajeBecasDocentes = GuardiaDominio.Porcentaje(porcentajeBecasDocentes, "Porcentaje becas docentes");
    }

    public void RehidratarFechaActualizacion(DateTimeOffset fecha)
    {
        FechaActualizacion = fecha;
    }

    public void RegistrarActualizacion(int actualizadoPorUsuarioId, string? fuenteNotas)
    {
        if (actualizadoPorUsuarioId <= 0)
        {
            throw new DominioException("Usuario que actualiza es obligatorio.");
        }

        ActualizadoPorUsuarioId = actualizadoPorUsuarioId;
        FechaActualizacion = DateTimeOffset.UtcNow;
        FuenteNotas = string.IsNullOrWhiteSpace(fuenteNotas) ? null : fuenteNotas.Trim();
    }
}
