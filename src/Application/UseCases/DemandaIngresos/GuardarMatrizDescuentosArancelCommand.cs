using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// KAN-44: guarda la matriz de descuentos por ciclo (una fila por ciclo). Reconcilia contra lo
/// existente: actualiza/crea los ciclos con % &gt; 0 y desactiva los que quedan en 0 o los rangos
/// antiguos (desde ≠ hasta) que ya no encajan en el modelo por ciclo.
/// </summary>
public sealed class GuardarMatrizDescuentosArancelCommand(IRepositorioDescuentoArancelCiclo repositorio)
{
    public async Task EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        IReadOnlyList<(int Ciclo, decimal Porcentaje)> filas,
        int? usuarioId = null,
        CancellationToken ct = default)
    {
        if (carreraId <= 0)
            throw new ArgumentException("Carrera es obligatoria.", nameof(carreraId));

        var existentes = await repositorio.ListarPorCarreraEscenarioAsync(carreraId, escenarioProyeccionId, ct);

        var porCiclo = existentes
            .Where(d => d.CicloDesde == d.CicloHasta)
            .GroupBy(d => d.CicloDesde)
            .ToDictionary(g => g.Key, g => g.First());

        // Rangos antiguos (desde ≠ hasta) se desactivan: el modelo ahora es una fila por ciclo.
        foreach (var rango in existentes.Where(d => d.CicloDesde != d.CicloHasta))
            await repositorio.DesactivarAsync(rango.Id, usuarioId, ct);

        foreach (var (ciclo, porcentaje) in filas)
        {
            if (ciclo < 1)
                continue;
            if (porcentaje < 0m || porcentaje > 100m)
                throw new ArgumentException($"Descuento del ciclo {ciclo} inválido (0 a 100).");

            porCiclo.TryGetValue(ciclo, out var existente);

            if (porcentaje > 0m)
            {
                await repositorio.GuardarAsync(new GuardarDescuentoArancelCicloDto
                {
                    Id = existente?.Id,
                    CarreraId = carreraId,
                    EscenarioProyeccionId = escenarioProyeccionId,
                    CicloDesde = ciclo,
                    CicloHasta = ciclo,
                    PorcentajeDescuento = porcentaje
                }, usuarioId, ct);
            }
            else if (existente is not null)
            {
                await repositorio.DesactivarAsync(existente.Id, usuarioId, ct);
            }
        }
    }
}
