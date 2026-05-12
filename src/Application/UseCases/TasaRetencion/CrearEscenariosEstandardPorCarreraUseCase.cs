using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

/// <summary>
/// Crea escenarios estándar (Histórico, Pesimista, Base, Optimista) para una carrera
/// si no existen aún. Usado cuando usuario selecciona nueva carrera en retención/graduación.
/// </summary>
public sealed class CrearEscenariosEstandardPorCarreraUseCase(
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IUnidadTrabajo unidadTrabajo)
{
    private static readonly string[] NombresEstandar =
    [
        "Historico",
        "Pesimista",
        "Base",
        "Optimista"
    ];

    public async Task<List<int>> EjecutarAsync(int carreraId, CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
            throw new ArgumentException($"{nameof(carreraId)} debe ser mayor que 0.", nameof(carreraId));

        // Verificar si carrera ya tiene escenarios
        var escenariosPorCarrera = await repositorioEscenario.ListarPorCarreraAsync(carreraId, cancellationToken);
        if (escenariosPorCarrera.Count > 0)
            return escenariosPorCarrera.Select(e => e.Id).ToList();

        // Crear escenarios estándar
        var idsCreados = new List<int>();
        foreach (var nombre in NombresEstandar)
        {
            var escenario = new EscenarioProyeccion(carreraId, nombre, nombre, esPredeterminado: false);
            await repositorioEscenario.AgregarAsync(escenario, cancellationToken);
            idsCreados.Add(escenario.Id);
        }

        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        return idsCreados;
    }
}
