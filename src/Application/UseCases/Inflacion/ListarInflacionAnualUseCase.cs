using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.Inflacion;

public sealed class ListarInflacionAnualUseCase(IRepositorioInflacionAnual repositorioInflacionAnual)
{
    public async Task<IReadOnlyList<InflacionAnualDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var lista = await repositorioInflacionAnual.ListarAsync(cancellationToken);
        return lista
            .Select(x => new InflacionAnualDto
            {
                Id = x.Id,
                Anio = x.Anio,
                PorcentajeInflacion = x.PorcentajeInflacion,
                FuenteNombre = x.FuenteNombre,
                TipoFuente = x.TipoFuente
            })
            .OrderBy(x => x.Anio)
            .ToList();
    }
}
