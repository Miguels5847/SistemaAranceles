using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.DTOs.TasaRetencion;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

public static class MotorProyeccionEstudiantes
{
    /// <summary>
    /// Genera la matriz de proyección (período × ciclo) por cohortes.
    /// Reglas:
    ///   - matriz[1, p] = estudiantesPorParalelo × paralelos(p)  (nuevos ingresos a ciclo 1)
    ///   - matriz[c, p] = matriz[c-1, p-1] × tasa(c)             (cohorte que avanza)
    /// La tasa(c) se deriva de detallesSimulacion como detalle[c].EstudiantesInicio /
    /// detalle[c-1].EstudiantesInicio, respetando la zona retención/graduación que ya
    /// calculó MotorSimulacionRetencion. Los paralelos del período afectan únicamente
    /// a ciclo 1; los ciclos superiores nunca se multiplican por paralelos.
    /// </summary>
    public static IReadOnlyList<CeldaProyeccionEstudiantesDto> Ejecutar(
        int totalCiclos,
        int paralelosPeriodo1,
        int paralelosPeriodo2,
        IReadOnlyList<DetalleSimulacionRetencionDto> detallesSimulacion)
    {
        var detalles = detallesSimulacion.OrderBy(d => d.Ciclo).ToArray();
        var estudiantesPorParalelo = detalles.Length > 0 ? detalles[0].EstudiantesInicio : 0m;

        var matriz = new decimal[totalCiclos + 1, totalCiclos + 1];
        var resultado = new List<CeldaProyeccionEstudiantesDto>(totalCiclos * (totalCiclos + 1) / 2);

        for (var p = 1; p <= totalCiclos; p++)
        {
            var paralelos = Paralelos(p, paralelosPeriodo1, paralelosPeriodo2);
            matriz[1, p] = estudiantesPorParalelo * paralelos;

            var ciclosActivos = Math.Min(p, totalCiclos);
            for (var c = 2; c <= ciclosActivos; c++)
            {
                var tasa = ObtenerTasaParaLlegarACiclo(c, detalles);
                matriz[c, p] = matriz[c - 1, p - 1] * tasa;
            }

            for (var c = 1; c <= ciclosActivos; c++)
            {
                resultado.Add(new CeldaProyeccionEstudiantesDto(
                    NumeroPeriodo: p,
                    NumeroCiclo: c,
                    CantidadParalelos: paralelos,
                    TotalEstudiantes: decimal.Round(matriz[c, p], 4)));
            }
        }

        return resultado;
    }

    private static int Paralelos(int periodo, int paralelosPeriodo1, int paralelosPeriodo2)
        => periodo % 2 == 1 ? paralelosPeriodo1 : paralelosPeriodo2;

    private static decimal ObtenerTasaParaLlegarACiclo(
        int cicloDestino,
        IReadOnlyList<DetalleSimulacionRetencionDto> detalles)
    {
        if (cicloDestino <= 1) return 1m;
        if (cicloDestino > detalles.Count) return 0m;

        var anterior = detalles[cicloDestino - 2];
        var actual = detalles[cicloDestino - 1];
        if (anterior.EstudiantesInicio <= 0m) return 0m;

        return actual.EstudiantesInicio / anterior.EstudiantesInicio;
    }
}
