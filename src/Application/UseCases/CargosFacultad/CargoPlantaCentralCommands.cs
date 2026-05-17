using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public sealed class ListarCargoPlantaCentralQuery(IRepositorioCargoPlantaCentral repositorio)
{
    public async Task<IReadOnlyList<CargoPlantaCentralDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var cargos = await repositorio.ObtenerTodosAsync(cancellationToken);
        return cargos
            .Select(c => new CargoPlantaCentralDto
            {
                Id = c.Id,
                NombreCargo = c.NombreCargo,
                SueldoMensualTotal = c.SueldoMensualTotal,
            })
            .ToList();
    }
}

public sealed class GuardarCargoPlantaCentralCommand(
    IRepositorioCargoPlantaCentral repositorio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task<CargoPlantaCentralDto> EjecutarAsync(GuardarCargoPlantaCentralDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dto.NombreCargo, nameof(dto.NombreCargo));
        if (dto.SueldoMensualTotal <= 0)
            throw new ArgumentOutOfRangeException(nameof(dto.SueldoMensualTotal), "El sueldo mensual debe ser mayor a 0");

        CargoPlantaCentral cargo;

        if (dto.Id.HasValue)
        {
            cargo = await repositorio.ObtenerPorIdAsync(dto.Id.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Cargo de planta central con ID {dto.Id} no encontrado");

            cargo.CambiarNombre(dto.NombreCargo);
            cargo.CambiarSueldoMensual(dto.SueldoMensualTotal);
            repositorio.Actualizar(cargo);
        }
        else
        {
            cargo = new CargoPlantaCentral(dto.NombreCargo, dto.SueldoMensualTotal);
            repositorio.Agregar(cargo);
        }

        await unidadTrabajo.GuardarCambiosAsync();

        return new CargoPlantaCentralDto
        {
            Id = cargo.Id,
            NombreCargo = cargo.NombreCargo,
            SueldoMensualTotal = cargo.SueldoMensualTotal,
        };
    }
}

public sealed class EliminarCargoPlantaCentralCommand(
    IRepositorioCargoPlantaCentral repositorio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "ID debe ser mayor a 0");

        var cargo = await repositorio.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Cargo de planta central con ID {id} no encontrado");

        repositorio.Eliminar(cargo);
        await unidadTrabajo.GuardarCambiosAsync();
    }
}
