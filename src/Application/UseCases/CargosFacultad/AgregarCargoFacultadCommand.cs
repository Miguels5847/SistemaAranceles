using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public sealed class AgregarCargoFacultadCommand(
    IRepositorioCargoFacultad repositorioCargoFacultad,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(CrearCargoFacultadDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var cargo = new CargoFacultad(
            dto.CarreraId,
            dto.NombreCargo,
            dto.TipoCargo,
            dto.SueldoBaseMensual,
            dto.EsCargoDocente,
            dto.CantidadDefault);

        await repositorioCargoFacultad.AgregarAsync(cargo, cancellationToken);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }
}