using FluentValidation;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

public sealed class GenerarProyeccionEstudiantesUseCase(
    IRepositorioConfiguracionRetencion repositorioConfiguracion,
    IRepositorioSimulacionRetencion repositorioSimulacion,
    IRepositorioDetalleSimulacionRetencion repositorioDetalle,
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IValidator<GenerarProyeccionEstudiantesDto> validador,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<int> EjecutarAsync(
        GenerarProyeccionEstudiantesDto dto,
        int? ejecutadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await validador.ValidateAndThrowAsync(dto, cancellationToken);

        var configuracionId = await ResolverConfiguracionIdAsync(dto, cancellationToken);
        var configuracion = await repositorioConfiguracion.ObtenerDominioPorIdAsync(configuracionId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No existe configuración de retención para carrera={dto.CarreraId}, escenario={dto.EscenarioProyeccionId}.");

        var simulacion = await repositorioSimulacion.ObtenerDominioPorIdAsync(dto.SimulacionRetencionId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"La simulación {dto.SimulacionRetencionId} no existe o está inactiva.");

        if (simulacion.ConfiguracionRetencionId != configuracion.Id)
            throw new InvalidOperationException(
                "La simulación indicada no pertenece a la configuración de retención de la carrera y escenario seleccionados.");

        var detallesSimulacion = await repositorioDetalle.ListarDtoPorSimulacionAsync(
            dto.SimulacionRetencionId, cancellationToken);

        if (detallesSimulacion.Count == 0)
            throw new InvalidOperationException(
                "La simulación no tiene detalles calculados. Ejecute la simulación antes de generar la proyección.");

        var celdas = MotorProyeccionEstudiantes.Ejecutar(
            configuracion.TotalCiclos,
            configuracion.ParalelosPeriodo1,
            configuracion.ParalelosPeriodo2,
            detallesSimulacion);

        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);

        await unidadTrabajo.IniciarTransaccionAsync(cancellationToken);
        int proyeccionId;
        try
        {
            proyeccionId = await repositorioProyeccion.GuardarAsync(
                dto.CarreraId,
                dto.EscenarioProyeccionId,
                simulacion.CohorteAnio,
                dto.SemanasPorSemestre,
                celdas,
                usuarioId,
                cancellationToken);

            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
            await unidadTrabajo.ConfirmarTransaccionAsync(cancellationToken);
        }
        catch
        {
            await unidadTrabajo.RevertirTransaccionAsync();
            throw;
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Estudiantes",
                entidadNombre: "ProyeccionEstudiantes",
                entidadId: proyeccionId.ToString(),
                accionNombre: "GENERAR_PROYECCION",
                resumenTexto: $"Proyección generada. Carrera={dto.CarreraId}, Escenario={dto.EscenarioProyeccionId}, AnioBase={simulacion.CohorteAnio}, Celdas={celdas.Count}.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Auditoría no bloquea la operación principal.
        }

        return proyeccionId;
    }

    private async Task<int> ResolverConfiguracionIdAsync(
        GenerarProyeccionEstudiantesDto dto,
        CancellationToken cancellationToken)
    {
        var todas = await repositorioConfiguracion.ListarDtoAsync(cancellationToken);
        var config = todas.FirstOrDefault(c =>
            c.CarreraId == dto.CarreraId &&
            c.EscenarioProyeccionId == dto.EscenarioProyeccionId)
            ?? throw new InvalidOperationException(
                $"No existe configuración de retención para carrera={dto.CarreraId}, escenario={dto.EscenarioProyeccionId}.");

        return config.Id;
    }

    private static int? NormalizarUsuarioId(int? usuarioId)
        => usuarioId.HasValue && usuarioId.Value > 0 ? usuarioId.Value : null;
}
