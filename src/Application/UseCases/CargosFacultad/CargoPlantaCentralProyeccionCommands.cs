using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Calcula los costos de planta central atribuidos a una carrera por período.
/// Usa la fórmula RN-84: Costo = TotalMensualTodosCargos × 6 × EstCarreraPeriodo / EstUniversidad
/// </summary>
public static class CalculoCargoPlantaCentral
{
    public static decimal CalcularProporcionAsignacion(decimal estudiantesCarrera, decimal estudiantesUniversidad)
    {
        if (estudiantesUniversidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(estudiantesUniversidad), "Total de estudiantes debe ser mayor a 0");

        return estudiantesCarrera / estudiantesUniversidad;
    }

    public static decimal CalcularCostoSemestralAtribuido(
        decimal sueldoMensualTotalCargo,
        decimal proporcionAsignacion,
        bool aplicarInflacion = false,
        decimal factorInflacion = 1m)
    {
        var costoSemestral = sueldoMensualTotalCargo * 6 * proporcionAsignacion;

        if (aplicarInflacion)
            costoSemestral *= Math.Max(factorInflacion, 1m);

        return costoSemestral;
    }
}

public sealed class CalcularProyeccionesCargoPlantaCentralCommand(
    IRepositorioCargoPlantaCentral repositorioCargos,
    IRepositorioProyeccionCargoPlantaCentral repositorioProyecciones,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(
        int carreraId,
        int periodoAcademicoId,
        decimal estudiantesCarrera,
        decimal estudiantesUniversidad,
        bool aplicarInflacion = false,
        decimal factorInflacion = 1m,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
            throw new ArgumentOutOfRangeException(nameof(carreraId), "Carrera ID debe ser mayor a 0");
        if (periodoAcademicoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(periodoAcademicoId), "Período ID debe ser mayor a 0");
        if (estudiantesUniversidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(estudiantesUniversidad), "Total de estudiantes debe ser mayor a 0");

        // Obtener todos los cargos de planta central
        var cargos = await repositorioCargos.ObtenerTodosAsync(cancellationToken);

        foreach (var cargo in cargos)
        {
            var proporcion = CalculoCargoPlantaCentral.CalcularProporcionAsignacion(
                estudiantesCarrera,
                estudiantesUniversidad);

            var costoAtribuido = CalculoCargoPlantaCentral.CalcularCostoSemestralAtribuido(
                cargo.SueldoMensualTotal,
                proporcion,
                aplicarInflacion,
                factorInflacion);

            // Buscar si existe proyección y actualizar o crear
            var proyeccionExistente = await repositorioProyecciones.ObtenerPorCargoYCarreraYPeriodoAsync(
                cargo.Id,
                carreraId,
                periodoAcademicoId,
                cancellationToken);

            if (proyeccionExistente != null)
            {
                proyeccionExistente.CambiarProporcionAsignacion(proporcion);
                proyeccionExistente.RecalcularCosto(costoAtribuido);
                repositorioProyecciones.Actualizar(proyeccionExistente);
            }
            else
            {
                var nuevaProyeccion = new ProyeccionCargoPlantaCentral(
                    cargo.Id,
                    carreraId,
                    periodoAcademicoId,
                    proporcion,
                    costoAtribuido);
                repositorioProyecciones.Agregar(nuevaProyeccion);
            }
        }

        await unidadTrabajo.GuardarCambiosAsync();
    }
}

public sealed class ListarProyeccionesCargoPlantaCentralQuery(
    IRepositorioCargoPlantaCentral repositorioCargos,
    IRepositorioProyeccionCargoPlantaCentral repositorioProyecciones)
{
    public async Task<IReadOnlyList<ProyeccionCargoPlantaCentralDto>> EjecutarAsync(
        int carreraId,
        int periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0 || periodoAcademicoId <= 0)
            return [];

        var proyecciones = await repositorioProyecciones.ObtenerPorCarreraYPeriodoAsync(
            carreraId,
            periodoAcademicoId,
            cancellationToken);

        var cargos = await repositorioCargos.ObtenerTodosAsync(cancellationToken);
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
