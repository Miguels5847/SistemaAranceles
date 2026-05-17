using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

/// <summary>
/// CU-ES-04: edita el override de horas de docencia/práctica para un período de una proyección.
/// Upsert idempotente. Auditoría fire-and-forget.
/// </summary>
public sealed class EditarConsumoPeriodoUseCase(
    IRepositorioOverrideHorasPeriodo repositorio,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoria)
{
    public async Task EjecutarAsync(
        int proyeccionId,
        int periodo,
        decimal? horasDocencia,
        decimal? horasPractica,
        int? usuarioId,
        CancellationToken ct = default)
    {
        var existente = await repositorio.ObtenerPorProyeccionYPeriodoAsync(proyeccionId, periodo, ct);

        var anteriorJson = existente is null
            ? null
            : $"{{\"HorasDocencia\":{Fmt(existente.HorasDocencia)},\"HorasPractica\":{Fmt(existente.HorasPractica)}}}";

        if (existente is null)
        {
            var nuevo = new OverrideHorasPeriodo(proyeccionId, periodo, horasDocencia, horasPractica);
            await repositorio.AgregarAsync(nuevo, ct);
        }
        else
        {
            existente.CambiarHorasDocencia(horasDocencia);
            existente.CambiarHorasPractica(horasPractica);
            repositorio.Actualizar(existente);
        }

        await unidadTrabajo.GuardarCambiosAsync(ct);

        var nuevoJson = $"{{\"HorasDocencia\":{Fmt(horasDocencia)},\"HorasPractica\":{Fmt(horasPractica)}}}";
        await auditoria.RegistrarAsync(
            "Estudiantes", "OverrideHorasPeriodo",
            $"{proyeccionId}:{periodo}", "EDITAR",
            $"Override horas P{periodo} de proyección {proyeccionId}",
            usuarioId, anteriorJson, nuevoJson, ct);
    }

    private static string Fmt(decimal? v) => v is null ? "null" : v.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
