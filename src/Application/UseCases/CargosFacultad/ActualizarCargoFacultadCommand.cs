using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public sealed class ActualizarCargoFacultadCommand(
    IRepositorioCargoFacultad repositorioCargoFacultad,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(ActualizarCargoFacultadDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var cargo = await repositorioCargoFacultad.ObtenerPorIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontro el cargo con Id {dto.Id}.");

        cargo.CambiarCarrera(dto.CarreraId);
        cargo.CambiarNombreCargo(dto.NombreCargo);
        cargo.CambiarTipoCargo(dto.TipoCargo);
        cargo.CambiarSueldoBase(dto.SueldoBaseMensual);
        cargo.CambiarEsCargoDocente(dto.EsCargoDocente);

        repositorioCargoFacultad.Actualizar(cargo);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }
}