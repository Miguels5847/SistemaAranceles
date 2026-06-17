using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

/// <summary>
/// Crea los 3 escenarios estándar (Histórico, Optimista, Pesimista) de una carrera, vacíos de
/// datos: solo el escenario, para que el usuario registre luego la configuración de retención.
/// Los nombres son exactamente los que el resto del módulo exige ("Histórico" con tilde para el
/// prerequisito de Optimista/Pesimista). Idempotente: agrega solo los que falten, así puede usarse
/// tanto al crear una carrera nueva como para reparar una existente sin escenarios.
/// </summary>
public sealed class SembrarEscenariosProyeccionCarreraCommand(
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IUnidadTrabajo unidadTrabajo)
{
    private static readonly (string Nombre, string Descripcion, bool Predeterminado)[] Estandar =
    [
        ("Histórico", "Escenario base: valores reales de la carrera.", true),
        ("Optimista", "Mayor retención y graduación que el escenario histórico.", false),
        ("Pesimista", "Menor retención y graduación que el escenario histórico.", false)
    ];

    public async Task<int> EjecutarAsync(int carreraId, int? creadoPorUsuarioId = null, CancellationToken ct = default)
    {
        if (carreraId <= 0)
            throw new DominioException("Carrera es obligatoria para sembrar escenarios.");

        var existentes = await repositorioEscenario.ListarNombresPorCarreraAsync(carreraId, ct);
        var creados = 0;

        foreach (var (nombre, descripcion, predeterminado) in Estandar)
        {
            if (existentes.Contains(nombre, StringComparer.OrdinalIgnoreCase))
                continue;

            await repositorioEscenario.AgregarAsync(
                new EscenarioProyeccion(carreraId, nombre, descripcion, predeterminado),
                creadoPorUsuarioId,
                ct);
            creados++;
        }

        if (creados > 0)
            await unidadTrabajo.GuardarCambiosAsync(ct);

        return creados;
    }
}
