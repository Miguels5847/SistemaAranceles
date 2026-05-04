using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Obtiene el consolidado de sueldos por período, combinando facultad y planta central.
/// </summary>
public sealed class ObtenerConsolidadoSueldosPeriodoQuery(
    IRepositorioCargoFacultad repositorioCargoFacultad,
    IRepositorioProyeccionCargoFacultad repositorioProyeccionFacultad,
    IRepositorioCargoPlantaCentral repositorioCargoPlantaCentral,
    IRepositorioProyeccionCargoPlantaCentral repositorioProyeccionPlantaCentral)
{
    public async Task<ConsolidadoSueldosPeriodoDto> EjecutarAsync(
        int carreraId,
        int periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0 || periodoAcademicoId <= 0)
            return new ConsolidadoSueldosPeriodoDto { PeriodoAcademicoId = periodoAcademicoId };

        var proyeccionesFacultad = await ObtenerProyeccionesFacultadAsync(carreraId, periodoAcademicoId, cancellationToken);
        var proyeccionesPlantaCentral = await ObtenerProyeccionesPlantaCentralAsync(carreraId, periodoAcademicoId, cancellationToken);

        return new ConsolidadoSueldosPeriodoDto
        {
            PeriodoAcademicoId = periodoAcademicoId,
            EtiquetaPeriodo = $"Período {periodoAcademicoId}",
            CargosFacultad = proyeccionesFacultad,
            CargoPlantaCentral = proyeccionesPlantaCentral,
        };
    }

    private async Task<IReadOnlyList<ProyeccionCargoFacultadDto>> ObtenerProyeccionesFacultadAsync(
        int carreraId,
        int periodoAcademicoId,
        CancellationToken cancellationToken)
    {
        var proyecciones = await repositorioProyeccionFacultad.ObtenerPorCarreraYPeriodoAsync(
            carreraId,
            periodoAcademicoId,
            cancellationToken);

        if (proyecciones.Count == 0)
            return [];

        var cargos = await repositorioCargoFacultad.ListarPorCarreraAsync(carreraId, cancellationToken);
        var cargoDict = cargos.ToDictionary(c => c.Id);

        var parametros = new ParametrosCalculoCargoFacultadDto();

        return proyecciones
            .Select(p =>
            {
                if (!cargoDict.TryGetValue(p.CargoFacultadId, out var cargo))
                    return null;

                var fondoReserva = CalculoCargosFacultad.CalcularFondoReservaMensual(
                    cargo.SueldoBaseMensual,
                    parametros.TasaFondoReserva);

                var aportePatronal = CalculoCargosFacultad.CalcularAportePatronalMensual(
                    cargo.SueldoBaseMensual,
                    parametros.TasaAportePatronal);

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
                    Id = p.Id,
                    CarreraId = carreraId,
                    CargoFacultadId = p.CargoFacultadId,
                    PeriodoAcademicoId = p.PeriodoAcademicoId,
                    NombreCargo = cargo.NombreCargo,
                    TipoCargo = cargo.TipoCargo,
                    SueldoBaseMensual = cargo.SueldoBaseMensual,
                    CantidadPersonas = p.CantidadPersonas,
                    FactorPonderacion = p.FactorPonderacion,
                    FactorInflacion = p.FactorInflacion,
                    CostoBaseSemestral = costoBaseSemestral,
                    CostoTotalSemestre = p.CostoTotalSemestre,
                };
            })
            .Where(x => x != null)
            .Cast<ProyeccionCargoFacultadDto>()
            .ToList();
    }

    private async Task<IReadOnlyList<ProyeccionCargoPlantaCentralDto>> ObtenerProyeccionesPlantaCentralAsync(
        int carreraId,
        int periodoAcademicoId,
        CancellationToken cancellationToken)
    {
        var proyecciones = await repositorioProyeccionPlantaCentral.ObtenerPorCarreraYPeriodoAsync(
            carreraId,
            periodoAcademicoId,
            cancellationToken);

        if (proyecciones.Count == 0)
            return [];

        var cargos = await repositorioCargoPlantaCentral.ObtenerTodosAsync(cancellationToken);
        var cargoDict = cargos.ToDictionary(c => c.Id);

        return proyecciones
            .Select(p =>
            {
                cargoDict.TryGetValue(p.CargoPlantaCentralId, out var cargo);
                return new ProyeccionCargoPlantaCentralDto
                {
                    Id = p.Id,
                    CargoPlantaCentralId = p.CargoPlantaCentralId,
                    CarreraId = p.CarreraId,
                    PeriodoAcademicoId = p.PeriodoAcademicoId,
                    NombreCargo = cargo?.NombreCargo ?? "Desconocido",
                    SueldoMensualTotal = cargo?.SueldoMensualTotal ?? 0,
                    ProporcionAsignacion = p.ProporcionAsignacion,
                    CostoTotalSemestre = p.CostoTotalSemestre,
                };
            })
            .ToList();
    }
}
