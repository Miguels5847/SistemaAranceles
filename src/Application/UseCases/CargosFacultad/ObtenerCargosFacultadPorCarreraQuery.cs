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
            throw new ArgumentOutOfRangeException(nameof(parametros.EstudiantesCarreraPeriodo));

        if (parametros.EstudiantesUnidadAcademica < 0m)
            throw new ArgumentOutOfRangeException(nameof(parametros.EstudiantesUnidadAcademica));

        if (parametros.FactorInflacion <= 0m)
            throw new ArgumentOutOfRangeException(nameof(parametros.FactorInflacion));

        if (parametros.ValorBaseDecimoCuartoSemestral < 0m)
            throw new ArgumentOutOfRangeException(nameof(parametros.ValorBaseDecimoCuartoSemestral));

        var cargos = await repositorioCargoFacultad.ListarPorCarreraAsync(carreraId, cancellationToken);
        return cargos
            .OrderBy(x => x.NombreCargo)
            .Select(cargo => Mapear(cargo, parametros))
            .ToList();
    }

    private static CargoFacultadCalculadoDto Mapear(CargoFacultad cargo, ParametrosCalculoCargoFacultadDto parametros)
    {
        var peso = CalcularPeso(cargo, parametros);
        var decimoTercero = Math.Round(cargo.SueldoBaseMensual / 2m, 2);
        var decimoCuarto = Math.Round(parametros.ValorBaseDecimoCuartoSemestral * parametros.FactorInflacion, 2);
        var vacaciones = Math.Round(cargo.SueldoBaseMensual / 4m, 2);
        var fondoReserva = Math.Round(cargo.SueldoBaseMensual * parametros.TasaFondoReserva, 2);
        var aportePatronal = Math.Round(cargo.SueldoBaseMensual * parametros.TasaAportePatronal, 2);
        var costoBaseSemestral = Math.Round(
            ((cargo.SueldoBaseMensual + fondoReserva + aportePatronal) * 6m)
            + decimoTercero
            + decimoCuarto
            + vacaciones,
            2);
        var costoSemestralPonderado = Math.Round(costoBaseSemestral * peso, 2);

        return new CargoFacultadCalculadoDto
        {
            Id = cargo.Id,
            CarreraId = cargo.CarreraId,
            NombreCargo = cargo.NombreCargo,
            TipoCargo = cargo.TipoCargo,
            SueldoBaseMensual = cargo.SueldoBaseMensual,
            EsCargoDocente = cargo.EsCargoDocente,
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

    private static decimal CalcularPeso(CargoFacultad cargo, ParametrosCalculoCargoFacultadDto parametros)
    {
        if (EsPesoFijo(cargo))
            return 1m;

        var denominador = parametros.EstudiantesUnidadAcademica + parametros.EstudiantesCarreraPeriodo;
        if (denominador <= 0m)
            return 0m;

        return Math.Round(parametros.EstudiantesCarreraPeriodo / denominador, 4);
    }

    private static bool EsPesoFijo(CargoFacultad cargo)
    {
        if (cargo.EsCargoDocente)
            return true;

        if (cargo.NombreCargo.Equals("Director de Carrera", StringComparison.OrdinalIgnoreCase))
            return true;

        return cargo.TipoCargo.Contains("dedicacion exclusiva", StringComparison.OrdinalIgnoreCase);
    }
}