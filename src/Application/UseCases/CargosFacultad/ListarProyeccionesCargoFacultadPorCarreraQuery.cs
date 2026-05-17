using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public sealed class ListarProyeccionesCargoFacultadPorCarreraQuery(
    IRepositorioCargoFacultad repositorioCargoFacultad,
    IRepositorioProyeccionCargoFacultad repositorioProyeccionCargoFacultad)
{
    public async Task<IReadOnlyList<ProyeccionCargoFacultadDto>> EjecutarAsync(
        int carreraId,
        ParametrosCalculoCargoFacultadDto parametros,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
            throw new ArgumentOutOfRangeException(nameof(carreraId), "La carrera debe ser mayor a cero.");

        ArgumentNullException.ThrowIfNull(parametros);

        var cargos = await repositorioCargoFacultad.ListarPorCarreraAsync(carreraId, cancellationToken);
        var resultado = new List<ProyeccionCargoFacultadDto>();

        foreach (var cargo in cargos.OrderBy(x => x.NombreCargo))
        {
            var proyecciones = await repositorioProyeccionCargoFacultad.ListarPorCargoAsync(cargo.Id, cancellationToken);
            foreach (var proyeccion in proyecciones.OrderBy(x => x.PeriodoAcademicoId))
            {
                resultado.Add(Mapear(cargo, proyeccion, parametros));
            }
        }

        return resultado;
    }

    private static ProyeccionCargoFacultadDto Mapear(
        CargoFacultad cargo,
        ProyeccionCargoFacultad proyeccion,
        ParametrosCalculoCargoFacultadDto parametros)
    {
        var peso = proyeccion.FactorPonderacion > 0m
            ? proyeccion.FactorPonderacion
            : CalculoCargosFacultad.CalcularPeso(cargo, parametros.EstudiantesCarreraPeriodo, parametros.EstudiantesUnidadAcademica);

        var fondoReserva = CalculoCargosFacultad.CalcularFondoReservaMensual(cargo.SueldoBaseMensual, parametros.TasaFondoReserva);
        var aportePatronal = CalculoCargosFacultad.CalcularAportePatronalMensual(cargo.SueldoBaseMensual, parametros.TasaAportePatronal);
        var decimoTercero = CalculoCargosFacultad.CalcularDecimoTerceroSemestral(cargo.SueldoBaseMensual);
        var decimoCuarto = CalculoCargosFacultad.CalcularDecimoCuartoSemestral(parametros.ValorBaseDecimoCuartoSemestral);
        var vacaciones = CalculoCargosFacultad.CalcularVacacionesSemestral(cargo.SueldoBaseMensual);
        var costoBaseSemestral = CalculoCargosFacultad.CalcularCostoBaseSemestral(
            cargo.SueldoBaseMensual,
            fondoReserva,
            aportePatronal,
            decimoTercero,
            decimoCuarto,
            vacaciones);

        return new ProyeccionCargoFacultadDto
        {
            Id = proyeccion.Id,
            CarreraId = cargo.CarreraId,
            CargoFacultadId = cargo.Id,
            PeriodoAcademicoId = proyeccion.PeriodoAcademicoId,
            NombreCargo = cargo.NombreCargo,
            TipoCargo = cargo.TipoCargo,
            SueldoBaseMensual = cargo.SueldoBaseMensual,
            CantidadPersonas = proyeccion.CantidadPersonas,
            FactorPonderacion = peso,
            FactorInflacion = CalculoCargosFacultad.NormalizarFactorInflacion(proyeccion.FactorInflacion),
            CostoBaseSemestral = costoBaseSemestral,
            CostoTotalSemestre = proyeccion.CostoTotalSemestre,
        };
    }
}