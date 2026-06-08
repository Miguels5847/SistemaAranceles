using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// Lista los descuentos de arancel por ciclo configurados para la carrera+escenario (KAN-44).
/// Para edición usa la configuración exacta (sin fallback global).
/// </summary>
public sealed class ObtenerDescuentosArancelCicloQuery(
    IRepositorioDescuentoArancelCiclo repositorio,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario)
{
    public async Task<MatrizDescuentosArancelDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;

        var descuentos = await repositorio.ListarPorCarreraEscenarioAsync(carreraId, escenarioProyeccionId, ct);

        return new MatrizDescuentosArancelDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? string.Empty,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? "Global",
            Descuentos = descuentos
        };
    }
}
