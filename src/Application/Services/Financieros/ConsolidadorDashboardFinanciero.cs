using SistemaAranceles.Application.DTOs.AnalisisFinanciero;

namespace SistemaAranceles.Application.Services.Financieros;

public static class ConsolidadorDashboardFinanciero
{
    public static DashboardFinancieroDto Construir(
        int carreraId,
        string carreraNombre,
        int? escenarioProyeccionId,
        string escenarioNombre,
        EstadoPerdidasGananciasDto? estado,
        FlujoFondosDto? flujo,
        IndicadoresFinancierosDto? indicadores,
        PeriodoRecuperacionDto? periodoRecuperacion,
        PuntoEquilibrioDto? puntoEquilibrio,
        ArancelOptimoBiseccionDto? arancelOptimo,
        string? mensajeAdvertencia = null)
    {
        var items = new List<DashboardIndicadorFinancieroDto>
        {
            ConstruirTir(indicadores),
            ConstruirVan(indicadores),
            ConstruirPuntoEquilibrio(puntoEquilibrio),
            ConstruirRecuperacion(periodoRecuperacion),
            ConstruirArancelOptimo(arancelOptimo)
        };

        return new DashboardFinancieroDto
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenarioNombre,
            Indicadores = items,
            Recomendaciones = ConstruirRecomendaciones(items, indicadores, puntoEquilibrio, periodoRecuperacion, arancelOptimo),
            MensajeAdvertencia = mensajeAdvertencia,
            TotalIngresos = estado?.TotalIngresos ?? 0m,
            TotalCostosGastos = estado?.TotalCostosYGastos ?? 0m,
            UtilidadPerdida = estado?.TotalUtilidadPerdidaEjercicio ?? 0m,
            FlujoAcumuladoFinal = flujo?.FlujoAcumuladoFinal ?? 0m,
            Van = indicadores?.Van ?? 0m,
            TmrPorcentaje = indicadores?.TmrPorcentaje ?? 0m,
            EsTirCalculable = indicadores?.EsTirCalculable == true,
            TirPorcentaje = indicadores?.EsTirCalculable == true ? indicadores.TirPorcentaje : 0m,
            ArancelOptimo = arancelOptimo?.Disponible == true ? arancelOptimo.ArancelOptimo : 0m
        };
    }

    private static DashboardIndicadorFinancieroDto ConstruirTir(IndicadoresFinancierosDto? indicadores)
    {
        if (indicadores?.TieneDatos != true)
        {
            return new DashboardIndicadorFinancieroDto
            {
                Nombre = "TIR",
                ValorDisplay = "No calculable",
                Detalle = "No hay flujo de fondos suficiente.",
                Estado = "Rojo: TIR no calculable",
                Modulo = "TIR / VAN",
                EsViable = false
            };
        }

        var esNoUnica = indicadores.TirPosibleNoUnica;
        var esViable = indicadores.EsTirCalculable
            && (esNoUnica ? indicadores.Van >= 0m : indicadores.TirPorcentaje >= indicadores.TmrPorcentaje);
        var estado = !indicadores.EsTirCalculable
            ? "Rojo: TIR no calculable"
            : esNoUnica
                ? "Advertencia: TIR puede no ser única; priorizar VAN"
                : esViable
                    ? "Verde: TIR cubre la TMR"
                    : "Rojo: TIR no cubre la TMR";

        return new DashboardIndicadorFinancieroDto
        {
            Nombre = "TIR",
            ValorDisplay = indicadores.TirDisplay,
            Detalle = $"TMR / tasa mínima de rendimiento {indicadores.TmrDisplay}; TIR complementaria.",
            Estado = estado,
            Modulo = "TIR / VAN",
            EsViable = esViable
        };
    }

    private static DashboardIndicadorFinancieroDto ConstruirVan(IndicadoresFinancierosDto? indicadores)
    {
        var disponible = indicadores?.TieneDatos == true;
        var esViable = disponible && indicadores!.Van >= 0m;

        return new DashboardIndicadorFinancieroDto
        {
            Nombre = "VAN",
            ValorDisplay = disponible ? indicadores!.VanDisplay : "$ 0.00",
            Detalle = disponible ? $"Estado {indicadores!.EstadoViabilidad}" : "No hay VAN calculado.",
            Estado = esViable ? "Verde: VAN no negativo" : "Rojo: VAN negativo",
            Modulo = "TIR / VAN",
            EsViable = esViable
        };
    }

    private static DashboardIndicadorFinancieroDto ConstruirPuntoEquilibrio(PuntoEquilibrioDto? puntoEquilibrio)
    {
        var calculables = puntoEquilibrio?.Periodos.Where(p => p.EsCalculable).ToList() ?? [];
        var estudiantesPromedio = calculables.Count > 0
            ? decimal.Round(calculables.Average(p => p.Estudiantes), 2)
            : 0m;
        var pePromedio = puntoEquilibrio?.PuntoEquilibrioEstudiantesPromedio ?? 0m;
        var esViable = calculables.Count > 0 && pePromedio > 0m && pePromedio <= estudiantesPromedio;

        return new DashboardIndicadorFinancieroDto
        {
            Nombre = "Punto de equilibrio",
            ValorDisplay = puntoEquilibrio?.PuntoEquilibrioEstudiantesPromedioDisplay ?? "No calculable",
            Detalle = estudiantesPromedio > 0m
                ? $"Promedio estudiantes {estudiantesPromedio:N2}"
                : "Sin periodos calculables.",
            Estado = esViable ? "Verde: PE cubierto por demanda" : "Rojo: PE sobre demanda o no calculable",
            Modulo = "Punto de Equilibrio",
            EsViable = esViable
        };
    }

    private static DashboardIndicadorFinancieroDto ConstruirRecuperacion(PeriodoRecuperacionDto? periodoRecuperacion)
    {
        var esViable = periodoRecuperacion?.Recuperado == true;

        return new DashboardIndicadorFinancieroDto
        {
            Nombre = "Recuperación",
            ValorDisplay = periodoRecuperacion?.TiempoRecuperacionDisplay ?? "No recuperado",
            Detalle = periodoRecuperacion?.PeriodoRecuperacion ?? "No hay periodo de recuperación calculado.",
            Estado = esViable ? "Verde: inversión recuperada" : "Rojo: inversión no recuperada",
            Modulo = "Periodo de Recuperación",
            EsViable = esViable
        };
    }

    private static DashboardIndicadorFinancieroDto ConstruirArancelOptimo(ArancelOptimoBiseccionDto? arancelOptimo)
    {
        var esViable = arancelOptimo?.Disponible == true;

        return new DashboardIndicadorFinancieroDto
        {
            Nombre = "Arancel óptimo",
            ValorDisplay = arancelOptimo?.ArancelOptimoDisplay ?? "No calculable",
            Detalle = esViable
                ? $"VAN objetivo {arancelOptimo!.VanDisplay}"
                : arancelOptimo?.MensajeAdvertencia ?? "No hay arancel óptimo disponible.",
            Estado = esViable ? "Verde: arancel óptimo disponible" : "Rojo: arancel óptimo no calculable",
            Modulo = "Arancel Óptimo",
            EsViable = esViable
        };
    }

    private static IReadOnlyList<DashboardRecomendacionFinancieraDto> ConstruirRecomendaciones(
        IReadOnlyList<DashboardIndicadorFinancieroDto> indicadoresDashboard,
        IndicadoresFinancierosDto? indicadores,
        PuntoEquilibrioDto? puntoEquilibrio,
        PeriodoRecuperacionDto? periodoRecuperacion,
        ArancelOptimoBiseccionDto? arancelOptimo)
    {
        var recomendaciones = new List<DashboardRecomendacionFinancieraDto>();

        if (indicadores?.Van < 0m)
            recomendaciones.Add(CrearRecomendacion("Alta", "VAN", "El arancel vigente no cubre la sostenibilidad financiera (VAN negativo): revisar costos, ingresos o inversión inicial."));
        if (indicadores?.TirPosibleNoUnica == true)
            recomendaciones.Add(CrearRecomendacion("Media", "TIR", "El flujo presenta múltiples cambios de signo; la TIR puede no ser única. Evalúe la viabilidad principalmente con el VAN."));
        if (indicadores?.TieneDatos == true
            && !indicadores.TirPosibleNoUnica
            && (!indicadores.EsTirCalculable || indicadores.TirPorcentaje < indicadores.TmrPorcentaje))
            recomendaciones.Add(CrearRecomendacion("Alta", "TIR", "La TIR no cubre la TMR; validar precio, demanda y estructura de costos."));
        if (puntoEquilibrio?.TienePeriodosNoCalculables == true)
            recomendaciones.Add(CrearRecomendacion("Media", "Punto de Equilibrio", "Hay periodos sin margen positivo o sin estudiantes; revisar demanda e ingresos."));
        if (periodoRecuperacion?.TieneDatos == true && !periodoRecuperacion.Recuperado)
            recomendaciones.Add(CrearRecomendacion("Media", "Recuperación", "La inversión no se recupera dentro del horizonte proyectado."));
        if (arancelOptimo?.Disponible != true)
            recomendaciones.Add(CrearRecomendacion("Media", "Arancel Óptimo", "No existe arancel óptimo dentro del rango de bisección configurado."));

        if (recomendaciones.Count == 0 && indicadoresDashboard.All(i => i.EsViable))
            recomendaciones.Add(CrearRecomendacion("Baja", "Dashboard", "Indicadores financieros consistentes; mantener monitoreo por escenario."));

        return recomendaciones;
    }

    private static DashboardRecomendacionFinancieraDto CrearRecomendacion(
        string prioridad,
        string origen,
        string mensaje)
        => new()
        {
            Prioridad = prioridad,
            Origen = origen,
            Mensaje = mensaje
        };
}
