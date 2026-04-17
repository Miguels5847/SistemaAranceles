using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Infrastructure.Servicios;

public sealed class ServicioProyeccion : IServicioProyeccion
{
    public IReadOnlyList<(int anio, decimal porcentaje)> ProyectarRegresionLineal(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        int anioDesde,
        int anioHasta)
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
            var normalizado = decimal.Round(Math.Clamp(estimado, -10m, 20m), 4);
            resultado.Add((anio, normalizado));
        }

        return resultado;
    }

    public IReadOnlyList<(int anio, decimal porcentaje)> ProyectarPromedioSuave(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        int anioDesde,
        int anioHasta)
    {
        if (historicos.Count < 3)
            return [];

        var ultimosTres = historicos
            .OrderByDescending(x => x.anio)
            .Take(3)
            .OrderBy(x => x.anio)
            .ToList();

        var promedio = decimal.Round(ultimosTres.Average(x => x.porcentaje), 4);
        var resultado = new List<(int anio, decimal porcentaje)>();

        var paso = 0;
        for (var anio = anioDesde; anio <= anioHasta; anio++)
        {
            var estimado = promedio - (0.10m * paso);
            var normalizado = decimal.Round(Math.Max(estimado, 0.50m), 4);
            resultado.Add((anio, normalizado));
            paso++;
        }

        return resultado;
    }
}
