using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

/// <summary>
/// CU-ES-04 flujo alterno 5a: elimina el override → vuelve al cálculo automático.
/// </summary>
public sealed class RestaurarConsumoPeriodoUseCase(
    IRepositorioOverrideHorasPeriodo repositorio,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoria)
{
    public async Task<bool> EjecutarAsync(
        int proyeccionId, int periodo, int? usuarioId, CancellationToken ct = default)
    {
        var existente = await repositorio.ObtenerPorProyeccionYPeriodoAsync(proyeccionId, periodo, ct);
        if (existente is null) return false;

        repositorio.Eliminar(existente);
        await unidadTrabajo.GuardarCambiosAsync(ct);

        await auditoria.RegistrarAsync(
            "Estudiantes", "OverrideHorasPeriodo",
            $"{proyeccionId}:{periodo}", "ELIMINAR",
            $"Restaurar horas P{periodo} de proyección {proyeccionId} (vuelve a default)",
            usuarioId, null, null, ct);

        return true;
    }
}
