using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public sealed class GuardarProyeccionCargoFacultadCommand(
    IRepositorioCargoFacultad repositorioCargoFacultad,
    IRepositorioProyeccionCargoFacultad repositorioProyeccionCargoFacultad,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(GuardarProyeccionCargoFacultadDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.CargoFacultadId <= 0)
            throw new ArgumentOutOfRangeException(nameof(dto));

        if (dto.PeriodoAcademicoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(dto));

        if (dto.CantidadPersonas <= 0m)
            throw new ArgumentOutOfRangeException(nameof(dto));

        if (dto.EstudiantesCarreraPeriodo < 0m)
            throw new ArgumentOutOfRangeException(nameof(dto));

        if (dto.EstudiantesUnidadAcademica < 0m)
            throw new ArgumentOutOfRangeException(nameof(dto));

        if (dto.FactorInflacion <= 0m)
            throw new ArgumentOutOfRangeException(nameof(dto));

        if (dto.ValorBaseDecimoCuartoSemestral < 0m)
            throw new ArgumentOutOfRangeException(nameof(dto));

        var cargo = await repositorioCargoFacultad.ObtenerPorIdAsync(dto.CargoFacultadId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontro el cargo con Id {dto.CargoFacultadId}.");

        var peso = CalculoCargosFacultad.CalcularPeso(cargo, dto.EstudiantesCarreraPeriodo, dto.EstudiantesUnidadAcademica);
        var fondoReserva = CalculoCargosFacultad.CalcularFondoReservaMensual(cargo.SueldoBaseMensual, 0.0833m);
        var aportePatronal = CalculoCargosFacultad.CalcularAportePatronalMensual(cargo.SueldoBaseMensual, 0.1115m);
        var decimoTercero = CalculoCargosFacultad.CalcularDecimoTerceroSemestral(cargo.SueldoBaseMensual);
        var decimoCuarto = CalculoCargosFacultad.CalcularDecimoCuartoSemestral(dto.ValorBaseDecimoCuartoSemestral);
        var vacaciones = CalculoCargosFacultad.CalcularVacacionesSemestral(cargo.SueldoBaseMensual);
        var costoBaseSemestral = CalculoCargosFacultad.CalcularCostoBaseSemestral(
            cargo.SueldoBaseMensual,
            fondoReserva,
            aportePatronal,
            decimoTercero,
            decimoCuarto,
            vacaciones);
        var factorInflacion = CalculoCargosFacultad.NormalizarFactorInflacion(dto.FactorInflacion);
        var costoTotalSemestral = CalculoCargosFacultad.CalcularCostoTotalSemestral(
            costoBaseSemestral,
            peso,
            dto.CantidadPersonas,
            factorInflacion);

        ProyeccionCargoFacultad? proyeccionExistente = null;
        if (dto.Id is > 0)
        {
            proyeccionExistente = await repositorioProyeccionCargoFacultad.ObtenerPorIdAsync(dto.Id.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"No se encontro la proyeccion con Id {dto.Id.Value}.");
        }
        else
        {
            var existentes = await repositorioProyeccionCargoFacultad.ListarPorCargoAsync(dto.CargoFacultadId, cancellationToken);
            proyeccionExistente = existentes.FirstOrDefault(x => x.PeriodoAcademicoId == dto.PeriodoAcademicoId);
        }

        if (proyeccionExistente is null)
        {
            var nueva = new ProyeccionCargoFacultad(
                dto.CargoFacultadId,
                dto.PeriodoAcademicoId,
                dto.CantidadPersonas,
                peso,
                factorInflacion,
                costoTotalSemestral);

            await repositorioProyeccionCargoFacultad.AgregarAsync(nueva, cancellationToken);
        }
        else
        {
            proyeccionExistente.CambiarCargoFacultad(dto.CargoFacultadId);
            proyeccionExistente.CambiarPeriodoAcademico(dto.PeriodoAcademicoId);
            proyeccionExistente.CambiarCantidadPersonas(dto.CantidadPersonas);
            proyeccionExistente.CambiarFactorPonderacion(peso);
            proyeccionExistente.CambiarFactorInflacion(factorInflacion);
            proyeccionExistente.RecalcularCosto(costoTotalSemestral);

            repositorioProyeccionCargoFacultad.Actualizar(proyeccionExistente);
        }

        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }
}