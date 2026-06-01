using SistemaAranceles.Application.DTOs.CostosGastos;

namespace SistemaAranceles.Application.UseCases.CostosGastos;

public sealed class ObtenerArancelOptimoCarreraQuery(ObtenerCostoCarreraQuery obtenerCostoCarreraQuery)
{
    public async Task<ArancelOptimoCarreraDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        decimal factorImprevisto = 1.05m)
    {
        var costoCarrera = await obtenerCostoCarreraQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, factorImprevisto: factorImprevisto);
        if (!costoCarrera.TieneDatos || costoCarrera.ArancelSugeridoSemestre <= 0m)
        {
            return new ArancelOptimoCarreraDto
            {
                CarreraId = costoCarrera.CarreraId,
                CarreraNombre = costoCarrera.CarreraNombre,
                EscenarioProyeccionId = costoCarrera.EscenarioProyeccionId,
                EscenarioNombre = costoCarrera.EscenarioNombre,
                PorcentajeMatriculaAplicado = costoCarrera.PorcentajeMatriculaAplicado,
                MensajeAdvertencia = string.IsNullOrWhiteSpace(costoCarrera.MensajeAdvertencia)
                    ? "Costo de Carrera pendiente o sin datos suficientes."
                    : costoCarrera.MensajeAdvertencia
            };
        }

        return new ArancelOptimoCarreraDto
        {
            CarreraId = costoCarrera.CarreraId,
            CarreraNombre = costoCarrera.CarreraNombre,
            EscenarioProyeccionId = costoCarrera.EscenarioProyeccionId,
            EscenarioNombre = costoCarrera.EscenarioNombre,
            ArancelSugeridoSemestre = costoCarrera.ArancelSugeridoSemestre,
            MatriculaSugerida = costoCarrera.MatriculaSugerida,
            PorcentajeMatriculaAplicado = costoCarrera.PorcentajeMatriculaAplicado,
            TotalPorSemestre = costoCarrera.TotalPorSemestre,
            MensajeAdvertencia = costoCarrera.MensajeAdvertencia
        };
    }
}
