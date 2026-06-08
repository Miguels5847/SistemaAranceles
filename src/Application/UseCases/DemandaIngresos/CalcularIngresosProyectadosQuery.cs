using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Services.Aranceles;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// KAN-33: Calcula ingresos proyectados por carrera/escenario.
/// Bruto = estudiantes × (arancel + matrícula)
/// Becas = bruto × % becas institucionales
/// Neto  = bruto − becas
/// Salida pivotada: filas = ciclos, columnas = períodos académicos.
/// </summary>
public sealed class CalcularIngresosProyectadosQuery(
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioDatosInstitucionales repositorioDatos,
    ObtenerArancelEfectivoQuery obtenerArancelEfectivoQuery,
    IRepositorioDescuentoArancelCiclo repositorioDescuentos)
{
    public async Task<IngresosProyectadosDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var carreraNombre = carrera?.Nombre ?? string.Empty;

        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;
        var escenarioNombre = escenario?.Nombre ?? "Global";

        var advertencias = new List<string>();

        if (escenarioProyeccionId is null or <= 0)
            advertencias.Add("Selecciona un escenario para ver ingresos por período.");

        var arancel = await obtenerArancelEfectivoQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var arancelValor = arancel.ArancelEfectivo ?? 0m;
        var matriculaValor = arancel.MatriculaEfectiva;

        if (arancelValor <= 0m)
            advertencias.Add(arancel.MensajeAdvertencia ?? "Configura el arancel de la carrera (Tab 1).");

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        var porcentajeBecas = datos?.PorcentajeBecasInstitucionales
            ?? SistemaAranceles.Domain.Entities.DatosInstitucionales.PorcentajeBecasInstitucionalesPorDefecto;

        if (datos is null)
            advertencias.Add("No hay Datos Institucionales vigentes. % becas usa default 10%.");

        IngresosProyectadosDto ConstruirVacio() => new()
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenarioNombre,
            ArancelEfectivo = arancelValor,
            MatriculaEfectiva = matriculaValor,
            PorcentajeBecasAplicado = porcentajeBecas,
            MensajeAdvertencia = advertencias.Count > 0 ? string.Join(" ", advertencias) : null
        };

        if (escenarioProyeccionId is null or <= 0)
            return ConstruirVacio();

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId, escenarioProyeccionId.Value, ct);
        if (proyeccionId is null or <= 0)
        {
            advertencias.Add("No hay proyección de estudiantes para esta carrera/escenario. Genere la proyección primero en Proyección de Estudiantes.");
            return ConstruirVacio();
        }

        var proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
        {
            advertencias.Add("Proyección sin detalles. Genera la proyección de estudiantes.");
            return ConstruirVacio();
        }

        var periodos = proyeccion.Detalles
            .GroupBy(d => d.PeriodoAcademicoId)
            .Select(g =>
            {
                var primero = g.First();
                return new
                {
                    primero.PeriodoAcademicoId,
                    primero.Anio,
                    primero.NumeroPeriodo,
                    primero.EtiquetaPeriodo
                };
            })
            .OrderBy(p => p.Anio)
            .ThenBy(p => p.NumeroPeriodo)
            .ToList();

        var ciclos = proyeccion.Detalles
            .Select(d => d.NumeroCiclo)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        // KAN-44: descuento comercial por ciclo. El arancel base se mantiene; cada ciclo cobra
        // arancelCiclo = arancelBase × (1 − %desc) y la matrícula se calcula sobre el arancel cobrado.
        // Sin descuentos configurados, arancelCiclo = arancelBase y el resultado es idéntico al anterior.
        var descuentos = await repositorioDescuentos.ListarEfectivosPorCarreraEscenarioAsync(
            carreraId, escenarioProyeccionId, ct);
        var porcentajeMatricula = arancel.PorcentajeMatriculaAplicado;

        var filas = ciclos.Select(ciclo =>
        {
            var descuentoCiclo = DescuentoArancelHelper.ResolverPorcentajeDescuentoCiclo(descuentos, ciclo);
            var arancelCiclo = DescuentoArancelHelper.CalcularArancelCiclo(arancelValor, descuentoCiclo);
            var matriculaCiclo = decimal.Round(arancelCiclo * porcentajeMatricula / 100m, 2);
            var precioPorEstudiante = arancelCiclo + matriculaCiclo;

            var celdas = periodos.Select(p =>
            {
                var detalle = proyeccion.Detalles
                    .FirstOrDefault(d => d.NumeroCiclo == ciclo && d.PeriodoAcademicoId == p.PeriodoAcademicoId);
                var estudiantes = detalle?.TotalEstudiantes ?? 0m;
                var bruto = decimal.Round(estudiantes * precioPorEstudiante, 2);
                var becas = decimal.Round(bruto * porcentajeBecas / 100m, 2);
                var neto = bruto - becas;

                return new IngresoPeriodoCeldaDto
                {
                    NumeroCiclo = ciclo,
                    PeriodoAcademicoId = p.PeriodoAcademicoId,
                    Anio = p.Anio,
                    NumeroPeriodo = p.NumeroPeriodo,
                    EtiquetaPeriodo = p.EtiquetaPeriodo,
                    Estudiantes = estudiantes,
                    IngresoBruto = bruto,
                    Becas = becas,
                    IngresoNeto = neto,
                    ArancelBase = arancelValor,
                    PorcentajeDescuentoCiclo = descuentoCiclo,
                    ArancelCiclo = arancelCiclo,
                    MatriculaCiclo = matriculaCiclo
                };
            }).ToList();

            return new IngresoFilaCicloDto
            {
                NumeroCiclo = ciclo,
                Periodos = celdas
            };
        }).ToList();

        return new IngresosProyectadosDto
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenarioNombre,
            ArancelEfectivo = arancelValor,
            MatriculaEfectiva = matriculaValor,
            PorcentajeBecasAplicado = porcentajeBecas,
            EtiquetasPeriodos = periodos.Select(p => p.EtiquetaPeriodo).ToList(),
            Filas = filas,
            CeldasPlanas = filas.SelectMany(f => f.Periodos)
                .OrderBy(c => c.NumeroCiclo)
                .ThenBy(c => c.Anio)
                .ThenBy(c => c.NumeroPeriodo)
                .ToList(),
            MensajeAdvertencia = advertencias.Count > 0 ? string.Join(" ", advertencias) : null
        };
    }
}
