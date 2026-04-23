using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using DetalleDominio = SistemaAranceles.Domain.Entities.DetalleSimulacionRetencion;
using DetallePersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.DetalleSimulacionRetencion;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioDetalleSimulacionRetencion(ContextoAplicacion contextoAplicacion) : IRepositorioDetalleSimulacionRetencion
{
    public async Task<IReadOnlyList<DetalleSimulacionRetencionDto>> ListarDtoPorSimulacionAsync(int simulacionRetencionId, CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.DetallesSimulacionRetencion
            .AsNoTracking()
            .Where(x => x.SimulacionRetencionId == simulacionRetencionId && x.EstaActivo)
            .OrderBy(x => x.Ciclo)
            .Select(x => new DetalleSimulacionRetencionDto
            {
                Id = x.Id,
                Ciclo = x.Ciclo,
                EstudiantesInicio = x.EstudiantesInicio,
                EstudiantesRetenidos = x.EstudiantesRetenidos,
                EstudiantesReprobados = x.EstudiantesReprobados,
                EstudiantesGraduados = x.EstudiantesGraduados,
                AnioAcademico = x.AnioAcademico,
                TasaRetencionCicloPorcentaje = x.EstudiantesInicio <= 0m ? 0m : decimal.Round((x.EstudiantesRetenidos / x.EstudiantesInicio) * 100m, 4),
                TasaGraduacionCicloPorcentaje = x.EstudiantesInicio <= 0m ? 0m : decimal.Round((x.EstudiantesGraduados / x.EstudiantesInicio) * 100m, 4)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task ReemplazarPorSimulacionAsync(
        int simulacionRetencionId,
        IReadOnlyList<DetalleDominio> detalles,
        int? usuarioId = null,
        CancellationToken cancellationToken = default)
    {
        await contextoAplicacion.DetallesSimulacionRetencion
            .Where(x => x.SimulacionRetencionId == simulacionRetencionId)
            .ExecuteDeleteAsync(cancellationToken);

        if (detalles.Count == 0)
            return;

        var ahora = DateTime.UtcNow;
        var entidades = detalles.Select(d => new DetallePersistencia
        {
            SimulacionRetencionId = simulacionRetencionId,
            NumeroCiclo = d.Ciclo,
            NumeroPeriodo = d.Ciclo,
            ValorEstudiantes = d.EstudiantesInicio,
            TasaAplicadaPorcentaje = d.EstudiantesInicio <= 0m ? 0m : decimal.Round((d.EstudiantesRetenidos / d.EstudiantesInicio) * 100m, 4),
            TipoZona = "GENERAL",
            Ciclo = d.Ciclo,
            EstudiantesInicio = d.EstudiantesInicio,
            EstudiantesRetenidos = d.EstudiantesRetenidos,
            EstudiantesReprobados = d.EstudiantesReprobados,
            EstudiantesGraduados = d.EstudiantesGraduados,
            CostoMatriculaProyectado = d.CostoMatriculaProyectado,
            AnioAcademico = d.AnioAcademico,
            CreadoEn = ahora,
            CreadoPorUsuarioId = usuarioId,
            EstaActivo = true
        });

        await contextoAplicacion.DetallesSimulacionRetencion.AddRangeAsync(entidades, cancellationToken);
    }

    public async Task<int> EliminarPorSimulacionAsync(int simulacionRetencionId, CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.DetallesSimulacionRetencion
            .Where(x => x.SimulacionRetencionId == simulacionRetencionId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> EliminarPorConfiguracionAsync(int configuracionRetencionId, CancellationToken cancellationToken = default)
    {
        var simulacionIds = contextoAplicacion.SimulacionesRetencion
            .Where(x => x.ConfiguracionRetencionId == configuracionRetencionId)
            .Select(x => x.Id);

        return await contextoAplicacion.DetallesSimulacionRetencion
            .Where(x => simulacionIds.Contains(x.SimulacionRetencionId))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
