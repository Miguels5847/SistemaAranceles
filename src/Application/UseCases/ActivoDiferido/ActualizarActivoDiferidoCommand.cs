using SistemaAranceles.Application.DTOs.ActivoDiferido;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Application.UseCases.ActivoDiferido;

public sealed class ActualizarActivoDiferidoCommand(IRepositorioActivoDiferido repositorio, IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(ActualizarActivoDiferidoDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var activo = await repositorio.ObtenerPorIdAsync(dto.Id, ct)
            ?? throw new DominioException($"Activo diferido con Id={dto.Id} no encontrado.");
        activo.CambiarNombreRubro(dto.NombreRubro);
        activo.CambiarValor(dto.Valor);
        activo.CambiarTasa(dto.TasaAmortizacionAnual);
        repositorio.Actualizar(activo);
        await unidadTrabajo.GuardarCambiosAsync(ct);
    }
}
