using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Infrastructure.Servicios;

public sealed class ServicioProyeccion : IServicioProyeccion
{
    public IReadOnlyList<(int anio, decimal porcentaje)> ProyectarRegresionLineal(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        int anioDesde,
        int anioHasta,
        decimal pisoMinimoProyeccion,
        decimal techoMaximoProyeccion)
    {
        if (historicos.Count < 2)
            return [];

        var n = historicos.Count;
        var sumX = historicos.Sum(x => (decimal)x.anio);
        var sumY = historicos.Sum(x => x.porcentaje);
        var sumXY = historicos.Sum(x => x.anio * x.porcentaje);
        var sumX2 = historicos.Sum(x => (decimal)x.anio * x.anio);

        var denominador = n * sumX2 - (sumX * sumX);
        decimal pendiente;
        decimal interseccion;

        if (Math.Abs((double)denominador) < 0.0000001d)
        {
            pendiente = 0m;
            interseccion = sumY / n;
        }
        else
        {
            pendiente = ((n * sumXY) - (sumX * sumY)) / denominador;
            interseccion = (sumY - (pendiente * sumX)) / n;
        }

        var resultado = new List<(int anio, decimal porcentaje)>();
        for (var anio = anioDesde; anio <= anioHasta; anio++)
        {
            var estimado = interseccion + (pendiente * anio);
            var normalizado = decimal.Round(Math.Clamp(estimado, pisoMinimoProyeccion, techoMaximoProyeccion), 4);
            resultado.Add((anio, normalizado));
        }

        return resultado;
    }

    public IReadOnlyList<(int anio, decimal porcentaje)> ProyectarPromedioSuave(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        int anioDesde,
        int anioHasta,
        decimal pisoMinimoProyeccion,
        int ventanaAniosRecientes,
        decimal valorObjetivoConvergencia,
        decimal factorConvergenciaAnual,
        decimal techoMaximoProyeccion)
    {
        if (historicos.Count < 3)
            return [];

        var tamanioVentana = Math.Clamp(ventanaAniosRecientes, 3, 5);
        var recientes = historicos
            .OrderByDescending(x => x.anio)
            .Take(tamanioVentana)
            .OrderBy(x => x.anio)
            .ToList();

        var primero = recientes.First().porcentaje;
        var ultimo = recientes.Last().porcentaje;
        var pendienteReciente = (ultimo - primero) / Math.Max(1, recientes.Count - 1);

        var factor = Math.Clamp(factorConvergenciaAnual, 0.30m, 0.50m);
        var objetivo = Math.Max(valorObjetivoConvergencia, pisoMinimoProyeccion);

        var resultado = new List<(int anio, decimal porcentaje)>();
        // La proyección siempre parte del último dato histórico observado.
        var valorActual = ultimo;
        for (var anio = anioDesde; anio <= anioHasta; anio++)
        {
            // Convergencia parcial al objetivo + tendencia reciente amortiguada.
            var ajusteConvergencia = factor * (objetivo - valorActual);
            var ajusteTendencia = pendienteReciente * 0.35m;
            valorActual += ajusteConvergencia + ajusteTendencia;
            var normalizado = decimal.Round(Math.Clamp(valorActual, pisoMinimoProyeccion, techoMaximoProyeccion), 4);
            resultado.Add((anio, normalizado));
        }

        return resultado;
    }

    public decimal ObtenerInflacionFallback(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        decimal porcentajeProyectado)
    {
        // Si proyección es válida (≥ 0%), retornar como está
        if (porcentajeProyectado >= 0m)
            return porcentajeProyectado;

        // Buscar histórico válido (≥ 0%) ordenado descendentemente por año (más reciente primero)
        var historicoValido = historicos
            .Where(x => x.porcentaje >= 0m)
            .OrderByDescending(x => x.anio)
            .FirstOrDefault();

        if (historicoValido != default)
            return historicoValido.porcentaje;

        // Fallback: 0.5% si no hay histórico válido
        return 0.50m;
    }
}
