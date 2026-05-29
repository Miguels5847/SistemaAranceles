using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

public sealed class GuardarConfiguracionArancelCarreraCommand(
    IRepositorioConfiguracionArancelCarrera repositorio,
    IAuditoriaServicio auditoria)
{
    public async Task<int> EjecutarAsync(
        GuardarConfiguracionArancelCarreraDto dto,
        int? usuarioId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.CarreraId <= 0)
            throw new ArgumentException("Carrera es obligatoria.", nameof(dto.CarreraId));

        if (string.Equals(dto.ModoCalculoArancel, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            if (dto.ArancelManual is null or <= 0m)
                throw new ArgumentException("Arancel manual obligatorio en modo Manual.", nameof(dto.ArancelManual));
        }
        else if (!string.Equals(dto.ModoCalculoArancel, "AutomaticoCostoCarrera", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Modo de cálculo no válido.", nameof(dto.ModoCalculoArancel));
        }

        if (!dto.UsaPorcentajeMatriculaInstitucional)
        {
            if (dto.PorcentajeMatricula is null or < 0m or > 100m)
                throw new ArgumentException("Porcentaje matrícula entre 0 y 100.", nameof(dto.PorcentajeMatricula));
        }

        var id = await repositorio.GuardarAsync(dto, usuarioId, ct);

        try
        {
            await auditoria.RegistrarAsync(
                moduloNombre: "DemandaIngresos",
                entidadNombre: "ConfiguracionArancelCarrera",
                entidadId: id.ToString(),
                accionNombre: dto.Id is null or 0 ? "CREAR" : "ACTUALIZAR",
                resumenTexto: $"Carrera={dto.CarreraId}, Escenario={dto.EscenarioProyeccionId?.ToString() ?? "GLOBAL"}, Modo={dto.ModoCalculoArancel}",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: ct);
        }
        catch
        {
            // Auditoría no bloquea.
        }

        return id;
    }
}
