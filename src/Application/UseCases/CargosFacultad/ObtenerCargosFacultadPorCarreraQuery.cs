using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public sealed class ObtenerCargosFacultadPorCarreraQuery(IRepositorioCargoFacultad repositorioCargoFacultad)
{
    public async Task<IReadOnlyList<CargoFacultadCalculadoDto>> EjecutarAsync(
        int carreraId,
        ParametrosCalculoCargoFacultadDto parametros,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
            throw new ArgumentOutOfRangeException(nameof(carreraId), "La carrera debe ser mayor a cero.");

        ArgumentNullException.ThrowIfNull(parametros);

        if (parametros.EstudiantesCarreraPeriodo < 0m)
            throw new ArgumentOutOfRangeException(nameof(parametros));

        if (parametros.EstudiantesUnidadAcademica < 0m)
            throw new ArgumentOutOfRangeException(nameof(parametros));

        if (parametros.FactorInflacion <= 0m)
            throw new ArgumentOutOfRangeException(nameof(parametros));

        if (parametros.ValorBaseDecimoCuartoSemestral < 0m)
            throw new ArgumentOutOfRangeException(nameof(parametros));

        var cargos = await repositorioCargoFacultad.ListarPorCarreraAsync(carreraId, cancellationToken);
        return cargos
            .OrderBy(x => x.NombreCargo)
            .Select(cargo => Mapear(cargo, parametros))
            .ToList();
    }

    private static CargoFacultadCalculadoDto Mapear(CargoFacultad cargo, ParametrosCalculoCargoFacultadDto parametros)
    {
        var peso = CalculoCargosFacultad.CalcularPeso(cargo, parametros.EstudiantesCarreraPeriodo, parametros.EstudiantesUnidadAcademica);
        var decimoTercero = CalculoCargosFacultad.CalcularDecimoTerceroSemestral(cargo.SueldoBaseMensual);
        var decimoCuarto = CalculoCargosFacultad.CalcularDecimoCuartoSemestral(parametros.ValorBaseDecimoCuartoSemestral);
        var vacaciones = CalculoCargosFacultad.CalcularVacacionesSemestral(cargo.SueldoBaseMensual);
        var fondoReserva = CalculoCargosFacultad.CalcularFondoReservaMensual(cargo.SueldoBaseMensual, parametros.TasaFondoReserva);
        var aportePatronal = CalculoCargosFacultad.CalcularAportePatronalMensual(cargo.SueldoBaseMensual, parametros.TasaAportePatronal);
        var costoBaseSemestral = CalculoCargosFacultad.CalcularCostoBaseSemestral(
            cargo.SueldoBaseMensual,
            fondoReserva,
            aportePatronal,
            decimoTercero,
            decimoCuarto,
            vacaciones);
        var costoSemestralPonderado = CalculoCargosFacultad.CalcularCostoTotalSemestral(
            costoBaseSemestral,
            peso,
            1m,
            parametros.FactorInflacion);

        return new CargoFacultadCalculadoDto
        {
            Id = cargo.Id,
            CarreraId = cargo.CarreraId,
            NombreCargo = cargo.NombreCargo,
            TipoCargo = cargo.TipoCargo,
            SueldoBaseMensual = cargo.SueldoBaseMensual,
            EsCargoDocente = cargo.EsCargoDocente,
            CantidadDefault = cargo.CantidadDefault,
            PesoProporcional = peso,
            DecimoTerceroSemestral = decimoTercero,
            DecimoCuartoSemestral = decimoCuarto,
            VacacionesSemestral = vacaciones,
            FondoReservaMensual = fondoReserva,
            AportePatronalMensual = aportePatronal,
            CostoBaseSemestral = costoBaseSemestral,
            CostoSemestralPonderado = costoSemestralPonderado,
        };
    }

}