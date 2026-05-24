using SistemaAranceles.Application.DTOs.ActivoDiferido;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.ActivoDiferido;

/// <summary>
/// Genera la tabla de amortización (KAN-31).
/// Los activos diferidos se amortizan a tasa fija (default 20%) durante 5 años.
/// </summary>
public sealed class ObtenerTablaAmortizacionQuery(IRepositorioActivoDiferido repositorio)
{
    private const int AniosAmortizacion = 5;

    public async Task<TablaAmortizacionDto> EjecutarAsync(
        int carreraId,
        int anioBase,
        CancellationToken ct = default)
    {
        var activos = await repositorio.ListarPorCarreraAsync(carreraId, ct);
        if (activos.Count == 0)
            return new TablaAmortizacionDto();

        var anios = Enumerable.Range(anioBase, AniosAmortizacion).ToList();
        var filas = new List<FilaAmortizacionDto>();

        foreach (var a in activos)
        {
            var cuotaAnual = a.CuotaAnual();
            var cuotas = anios.Select(_ => cuotaAnual).ToList();
            filas.Add(new FilaAmortizacionDto
            {
                ActivoDiferidoId = a.Id,
                NombreRubro = a.NombreRubro,
                ValorTotal = a.Valor,
                TasaAmortizacion = a.TasaAmortizacionAnual,
                CuotasPorAnio = cuotas,
            });
        }

        var totalesPorAnio = anios
            .Select((_, i) => filas.Sum(f => f.CuotasPorAnio[i]))
            .ToList();

        return new TablaAmortizacionDto
        {
            Anios = anios,
            Filas = filas,
            TotalesPorAnio = totalesPorAnio,
            TotalGeneral = totalesPorAnio.Sum(),
        };
    }
}
