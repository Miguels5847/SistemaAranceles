using SistemaAranceles.Application.DTOs.ActivoDiferido;
using SistemaAranceles.Application.Interfaces.Persistencia;
using DominioActivoDiferido = SistemaAranceles.Domain.Entities.ActivoDiferido;

namespace SistemaAranceles.Application.UseCases.ActivoDiferido;

public sealed class CrearActivoDiferidoCommand(IRepositorioActivoDiferido repositorio, IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(CrearActivoDiferidoDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var activo = new DominioActivoDiferido(dto.CarreraId, dto.NombreRubro, dto.Valor, dto.TasaAmortizacionAnual);
        await repositorio.AgregarAsync(activo, ct);
        await unidadTrabajo.GuardarCambiosAsync(ct);
    }
}
