using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Obtiene las opciones de inflación disponibles para cada año.
/// </summary>
public sealed class ListarOpcionesInflacionPorAnioQuery(IRepositorioInflacionAnual repositorioInflacion)
{
    public async Task<IReadOnlyList<OpcionesInflacionPorAnioDto>> EjecutarAsync(
        int anioDesde,
        int anioHasta,
        CancellationToken cancellationToken = default)
    {
        if (anioDesde <= 0 || anioHasta <= 0)
            throw new ArgumentException("El rango de años debe ser mayor a cero.");

        if (anioDesde > anioHasta)
            throw new ArgumentException("El año inicial no puede ser mayor al año final.");

        var registros = await repositorioInflacion.ListarPorRangoAsync(
            anioDesde,
            anioHasta,
            cancellationToken);

        var opcionesDict = new Dictionary<int, OpcionesInflacionPorAnioDto>();

        foreach (var registro in registros)
        {
            if (!opcionesDict.ContainsKey(registro.Anio))
            {
                opcionesDict[registro.Anio] = new OpcionesInflacionPorAnioDto
                {
                    Anio = registro.Anio,
                    InflacionProyectada = null,
                    TieneInflacionImportada = false,
                };
            }

            // Acumular inflación proyectada (tomar la más reciente)
            if (registro.TipoFuente.Equals("estimacion", StringComparison.OrdinalIgnoreCase))
            {
                var opcion = opcionesDict[registro.Anio];
                opcionesDict[registro.Anio] = new OpcionesInflacionPorAnioDto
                {
                    Anio = opcion.Anio,
                    InflacionProyectada = registro.PorcentajeInflacion,
                    TieneInflacionImportada = opcion.TieneInflacionImportada,
                };
            }
            else
            {
                var opcion = opcionesDict[registro.Anio];
                opcionesDict[registro.Anio] = new OpcionesInflacionPorAnioDto
                {
                    Anio = opcion.Anio,
                    InflacionProyectada = opcion.InflacionProyectada,
                    TieneInflacionImportada = true,
                };
            }
        }

        return opcionesDict.Values
            .OrderBy(x => x.Anio)
            .ToList();
    }
}

/// <summary>
/// Obtiene la inflación específica a aplicar (proyectada o importada) para un año.
/// </summary>
public sealed class ObtenerInflacionPorAnioQuery(IRepositorioInflacionAnual repositorioInflacion)
{
    public async Task<SeleccionInflacionDto?> EjecutarAsync(
        int anio,
        bool usarProyectada = true,
        CancellationToken cancellationToken = default)
    {
        if (anio <= 0)
            throw new ArgumentException("El año debe ser mayor a cero.");

        var registros = await repositorioInflacion.ListarPorRangoAsync(anio, anio, cancellationToken);

        if (registros.Count == 0)
            return null;

        var registro = usarProyectada
            ? registros.FirstOrDefault(x => x.TipoFuente.Equals("estimacion", StringComparison.OrdinalIgnoreCase))
            : registros.FirstOrDefault(x => !x.TipoFuente.Equals("estimacion", StringComparison.OrdinalIgnoreCase));

        if (registro == null)
            return null;

        // Convertir porcentaje a factor (100% = 1.0, 105% = 1.05)
        var factor = 1m + (registro.PorcentajeInflacion / 100m);

        return new SeleccionInflacionDto
        {
            Anio = anio,
            FactorInflacion = Math.Max(factor, 1m), // Protección anti-deflación
            EsValorManual = false,
        };
    }
}
