using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public sealed class EliminarProyeccionCargoFacultadCommand(
    IRepositorioProyeccionCargoFacultad repositorioProyeccionCargoFacultad,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "El identificador debe ser mayor a cero.");

        var proyeccion = await repositorioProyeccionCargoFacultad.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontro la proyeccion con Id {id}.");

        repositorioProyeccionCargoFacultad.Eliminar(proyeccion);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }
}