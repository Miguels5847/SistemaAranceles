namespace SistemaAranceles.Application.DTOs.Reportes;

/// <summary>
/// KAN-47: destinatario del reporte por dirección. Cada dirección imprime un
/// subconjunto de secciones del informe original "Informe de Costos de la Carrera";
/// Completo imprime todas en el orden del informe.
/// </summary>
public enum DireccionReporte
{
    Financiera,
    GestionDocente,
    Administrativa,
    TalentoHumano,
    EstrategiaComercial,
    GeneralCes,
    Completo
}

public enum SeccionReporte
{
    ArancelMatricula,
    DemandaTabla,
    GraficoMatricula,
    RetencionSimulacion,
    DocentesTabla,
    GraficoDocentes,
    Ingresos,
    MaterialesUnidades,
    MaterialesMonetario,
    ActivosFijos,
    InversionInicial,
    CapitalTrabajo,
    Depreciacion,
    Sueldos,
    Mantenimiento,
    PlantaCentral,
    InvVinBecas,
    CostosGastos,
    FinanciamientoAmortizacion,
    Indicadores,
    PuntoEquilibrio,
    FlujoFondos,
    EstadoResultados,
    BalanceProyectado,
    Ces
}

public static class SeccionesReporte
{
    public static IReadOnlyList<SeccionReporte> ParaDireccion(DireccionReporte direccion) => direccion switch
    {
        DireccionReporte.Financiera =>
        [
            SeccionReporte.InversionInicial,
            SeccionReporte.CapitalTrabajo,
            SeccionReporte.FinanciamientoAmortizacion,
            SeccionReporte.Indicadores,
            SeccionReporte.PuntoEquilibrio,
            SeccionReporte.CostosGastos,
            SeccionReporte.FlujoFondos,
            SeccionReporte.EstadoResultados,
            SeccionReporte.BalanceProyectado
        ],
        DireccionReporte.GestionDocente =>
        [
            SeccionReporte.DemandaTabla,
            SeccionReporte.GraficoMatricula,
            SeccionReporte.RetencionSimulacion,
            SeccionReporte.DocentesTabla,
            SeccionReporte.GraficoDocentes
        ],
        DireccionReporte.Administrativa =>
        [
            SeccionReporte.MaterialesUnidades,
            SeccionReporte.MaterialesMonetario,
            SeccionReporte.ActivosFijos,
            SeccionReporte.InversionInicial,
            SeccionReporte.Depreciacion,
            SeccionReporte.Mantenimiento
        ],
        DireccionReporte.TalentoHumano =>
        [
            SeccionReporte.DocentesTabla,
            SeccionReporte.GraficoDocentes,
            SeccionReporte.Sueldos,
            SeccionReporte.PlantaCentral
        ],
        DireccionReporte.EstrategiaComercial =>
        [
            SeccionReporte.ArancelMatricula,
            SeccionReporte.DemandaTabla,
            SeccionReporte.GraficoMatricula,
            SeccionReporte.Ingresos
        ],
        DireccionReporte.GeneralCes =>
        [
            SeccionReporte.Ces,
            SeccionReporte.InvVinBecas
        ],
        _ =>
        [
            SeccionReporte.ArancelMatricula,
            SeccionReporte.DemandaTabla,
            SeccionReporte.GraficoMatricula,
            SeccionReporte.RetencionSimulacion,
            SeccionReporte.DocentesTabla,
            SeccionReporte.GraficoDocentes,
            SeccionReporte.Ingresos,
            SeccionReporte.MaterialesUnidades,
            SeccionReporte.MaterialesMonetario,
            SeccionReporte.ActivosFijos,
            SeccionReporte.InversionInicial,
            SeccionReporte.CapitalTrabajo,
            SeccionReporte.Depreciacion,
            SeccionReporte.Sueldos,
            SeccionReporte.Mantenimiento,
            SeccionReporte.PlantaCentral,
            SeccionReporte.InvVinBecas,
            SeccionReporte.CostosGastos,
            SeccionReporte.FinanciamientoAmortizacion,
            SeccionReporte.Indicadores,
            SeccionReporte.PuntoEquilibrio,
            SeccionReporte.FlujoFondos,
            SeccionReporte.EstadoResultados,
            SeccionReporte.BalanceProyectado,
            SeccionReporte.Ces
        ]
    };

    public static string Titulo(DireccionReporte direccion) => direccion switch
    {
        DireccionReporte.Financiera => "Dirección Financiera",
        DireccionReporte.GestionDocente => "Dirección de Gestión Docente",
        DireccionReporte.Administrativa => "Dirección Administrativa",
        DireccionReporte.TalentoHumano => "Dirección de Talento Humano",
        DireccionReporte.EstrategiaComercial => "Dirección de Estrategia Comercial y Comunicacional",
        DireccionReporte.GeneralCes => "Dirección General / CES",
        _ => "Reporte completo"
    };

    public static string SlugArchivo(DireccionReporte direccion) => direccion switch
    {
        DireccionReporte.Financiera => "Financiera",
        DireccionReporte.GestionDocente => "Gestion_Docente",
        DireccionReporte.Administrativa => "Administrativa",
        DireccionReporte.TalentoHumano => "Talento_Humano",
        DireccionReporte.EstrategiaComercial => "Estrategia_Comercial",
        DireccionReporte.GeneralCes => "CES",
        _ => "Completo"
    };
}
