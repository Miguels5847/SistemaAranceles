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

    private DatosInstitucionales()
    {
        MesesCapitalTrabajo = MesesCapitalTrabajoPorDefecto;
        PorcentajeImprevistosInversion = PorcentajeImprevistosInversionPorDefecto;
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
        Periodo = GuardiaDominio.Requerido(periodo, "Periodo", 20);
    }

    public void CambiarPoblacion(int estudiantesUniversidad, int docentesUniversidad, int personasPlantaCentral)
    {
        NumeroEstudiantesUniversidad = GuardiaDominio.EnteroPositivo(estudiantesUniversidad, "Numero de estudiantes universidad");
        NumeroDocentesUniversidad = GuardiaDominio.EnteroPositivo(docentesUniversidad, "Numero de docentes universidad");
        NumeroPersonasPlantaCentral = GuardiaDominio.EnteroNoNegativo(personasPlantaCentral, "Numero de personas planta central");
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
        SueldoBasico = GuardiaDominio.DecimalNoNegativo(sueldoBasico, "Sueldo basico", 2);
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
            "Porcentaje imprevistos inversion");
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
