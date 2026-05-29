using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

public sealed class EliminarRatioMaterialDemandaCommand(
    IRepositorioRatioMaterialDemanda repositorio,
    IAuditoriaServicio auditoria)
{
    public async Task EjecutarAsync(int id, int? usuarioId = null, CancellationToken ct = default)
    {
        if (id <= 0)
            throw new ArgumentException("Id inválido.", nameof(id));

        await repositorio.EliminarAsync(id, usuarioId, ct);

        try
        {
            await auditoria.RegistrarAsync(
                moduloNombre: "DemandaIngresos",
                entidadNombre: "RatioMaterialDemanda",
                entidadId: id.ToString(),
                accionNombre: "ELIMINAR",
                resumenTexto: $"Ratio id={id} eliminado (soft).",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: ct);
        }
        catch
        {
        }
    }
}
