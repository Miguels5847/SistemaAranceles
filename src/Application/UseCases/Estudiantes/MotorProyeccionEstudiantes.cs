using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.DTOs.TasaRetencion;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

internal static class MotorProyeccionEstudiantes
{
    /// <summary>
    /// Genera la matriz de proyección (período × ciclo) a partir de la simulación guardada.
    /// Reglas Excel «1 Estudiantes»:
    ///   - Para período p (1-based): paralelos = paralelosPeriodo1 si p es impar, paralelosPeriodo2 si p es par.
    ///   - Para cada ciclo c activo en período p (c ≤ p): total = ValorEstudiantes[c] × paralelos[p].
    ///   - Períodos = totalCiclos (8 semestres = 4 años).
    /// </summary>
    public static IReadOnlyList<CeldaProyeccionEstudiantesDto> Ejecutar(
        int totalCiclos,
        int paralelosPeriodo1,
        int paralelosPeriodo2,
        IReadOnlyList<DetalleSimulacionRetencionDto> detallesSimulacion)
    {
        var valoresPorCiclo = detallesSimulacion
            .OrderBy(d => d.Ciclo)
            .Select(d => d.EstudiantesInicio)
            .ToArray();

        var resultado = new List<CeldaProyeccionEstudiantesDto>(totalCiclos * (totalCiclos + 1) / 2);

        for (var p = 1; p <= totalCiclos; p++)
        {
            var paralelos = p % 2 == 1 ? paralelosPeriodo1 : paralelosPeriodo2;
            var ciclosActivos = Math.Min(p, totalCiclos);

            for (var c = 1; c <= ciclosActivos; c++)
            {
                var estudiantesBase = c <= valoresPorCiclo.Length ? valoresPorCiclo[c - 1] : 0m;
                resultado.Add(new CeldaProyeccionEstudiantesDto(
                    NumeroPeriodo: p,
                    NumeroCiclo: c,
                    CantidadParalelos: paralelos,
                    TotalEstudiantes: decimal.Round(estudiantesBase * paralelos, 4)));
            }
        }

        return resultado;
    }
}
