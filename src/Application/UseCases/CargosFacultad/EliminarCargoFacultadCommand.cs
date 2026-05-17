using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public sealed class EliminarCargoFacultadCommand(
    IRepositorioCargoFacultad repositorioCargoFacultad,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "El identificador debe ser mayor a cero.");

        var cargo = await repositorioCargoFacultad.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontro el cargo con Id {id}.");

        repositorioCargoFacultad.Eliminar(cargo);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }
}