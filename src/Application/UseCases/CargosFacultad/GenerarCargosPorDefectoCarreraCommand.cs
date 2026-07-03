using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Copia la plantilla compartida de cargos (Decano, Secretario, docentes...) como cargos PROPIOS
/// de la carrera, para que deje de depender de otra carrera (borrar la carrera "plantilla" dejaba
/// a las demás sin sueldos administrativos). Idempotente: agrega solo los cargos que falten por
/// nombre; sirve al crear la carrera y para reparar una existente. Devuelve cuántos se crearon.
/// </summary>
public sealed class GenerarCargosPorDefectoCarreraCommand(
    IRepositorioCargoFacultad repositorio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task<int> EjecutarAsync(int carreraId, CancellationToken ct = default)
    {
        if (carreraId <= 0)
            throw new ArgumentException("Carrera es obligatoria.", nameof(carreraId));

        var existentes = await repositorio.ListarPorCarreraAsync(carreraId, ct);
        var nombres = existentes.Select(c => c.NombreCargo).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var plantilla = await repositorio.ListarPlantillaCompartidaAsync(carreraId, ct);

        var creados = 0;
        foreach (var origen in plantilla)
        {
            if (nombres.Contains(origen.NombreCargo))
                continue;

            var copia = new CargoFacultad(
                carreraId,
                origen.NombreCargo,
                origen.TipoCargo,
                origen.SueldoBaseMensual,
                origen.EsCargoDocente,
                origen.CantidadDefault);
            copia.CambiarTipoContrato(origen.TipoContrato);
            copia.CambiarTarifaHora(origen.TarifaHora);

            await repositorio.AgregarAsync(copia, ct);
            creados++;
        }

        if (creados > 0)
            await unidadTrabajo.GuardarCambiosAsync(ct);

        return creados;
    }
}
