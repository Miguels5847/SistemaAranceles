using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.UseCases.SueldosPlantaCentral;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;
using SistemaAranceles.Application.DTOs.Estudiantes;
using Xunit;

namespace SistemaAranceles.Application.Tests.CargosFacultad;

public class GenerarResumenSueldosPlantaCentralIntegrationTests
{
    [Fact]
    public async Task Ejecutar_IncluyeAportePlantaCentralYTotaCombinado()
    {
        var datos = NuevoDatos(totalMensual: 100000m, estudiantesUniv: 10000);
        var proyeccion = NuevaProyeccion((2026, 1, 100m), (2026, 2, 100m));

        var cargo = new CargoFacultad(1, "Tiempo Parcial", "Tiempo Parcial", 0m, true);
        cargo.CambiarTipoContrato(TipoContrato.TiempoParcial);
        cargo.CambiarTarifaHora(10m);

        var query = new GenerarResumenSueldosQuery(
            new RepositorioCargoFacultadFake(new[] { cargo }),
            new RepositorioInflacionAnualFake(),
            new RepositorioProyeccionEstudiantesFake(proyeccion),
            new RepositorioCarreraFake(),
            new RepositorioDatosInstitucionalesFake(datos),
            new RepositorioConfiguracionRetencionFake(),
            new RepositorioOverrideHorasPeriodoFake());

        var resultado = await query.EjecutarAsync(carreraId: 1, escenarioProyeccionId: 1, estudiantesUA: 0m);

        Assert.NotNull(resultado.PlantaCentralDistribucion);
        var aporte = resultado.PlantaCentralDistribucion!;
        Assert.Equal(resultado.TotalSueldosMasPlantaCentral, Math.Round(resultado.GranTotal + aporte.AporteAcumulado, 2));
    }

    [Fact]
    public async Task Ejecutar_UsaElMismoTotalPeriodoQueTablaNormalParaOcasionalTipo2()
    {
        var datos = NuevoDatos(totalMensual: 100000m, estudiantesUniv: 10000);
        var proyeccion = NuevaProyeccion((2023, 2, 100m));
        var cargo = new CargoFacultad(1, "Ocasional Tipo 2 (Técnico Docente)", "Docente", 1350m, true);
        cargo.CambiarTipoContrato(TipoContrato.Tecnico);

        var repoCargo = new RepositorioCargoFacultadFake([cargo]);
        var repoInflacion = new RepositorioInflacionAnualFake();
        var repoProyeccion = new RepositorioProyeccionEstudiantesFake(proyeccion);
        var repoCarrera = new RepositorioCarreraFake();

        var consolidado = new ProyeccionConsolidadaDto
        {
            HorasTecnicoSemana = 40m,
            TablaHoras =
            [
                new FilaHorasDto
                {
                    Etiqueta = "Nº Horas Clase Aplicac. Práctica x semana Acumuladas",
                    Valores = [0m, 10m]
                }
            ],
            DocentesPorPeriodo =
            [
                new FilaDocentePeriodoDto
                {
                    Tipo = "Ocasional Tipo 2 (Técnico)",
                    Periodos = [7, 1],
                    Total = 1
                }
            ]
        };

        var tablaNormalQuery = new GenerarTablaSueldosPeriodoQuery(
            repoCargo,
            repoInflacion,
            repoProyeccion,
            repoCarrera);
        var tablaNormal = await tablaNormalQuery.EjecutarAsync(
            carreraId: 1,
            escenarioProyeccionId: 1,
            periodoAcademicoId: proyeccion.Detalles[0].PeriodoAcademicoId,
            estudiantesUA: 0m,
            consolidado);

        var resumenQuery = new GenerarResumenSueldosQuery(
            repoCargo,
            repoInflacion,
            repoProyeccion,
            repoCarrera,
            new RepositorioDatosInstitucionalesFake(datos),
            new RepositorioConfiguracionRetencionFake(),
            new RepositorioOverrideHorasPeriodoFake());
        var resumen = await resumenQuery.EjecutarAsync(
            carreraId: 1,
            escenarioProyeccionId: 1,
            estudiantesUA: 0m,
            consolidado);

        var esperado = tablaNormal.Filas.Single(f => f.CargoId == cargo.Id);
        var filaResumen = resumen.Filas.Single(f => f.CargoId == cargo.Id);

        Assert.Equal(esperado.TotalSemestre, filaResumen.ValoresPorPeriodo[0]);
        Assert.Equal(esperado.NumeroPersonas, filaResumen.NumeroPersonas);
        Assert.Equal(0.25m, filaResumen.NumeroPersonas);
    }

    private static DatosInstitucionales NuevoDatos(decimal totalMensual = 100000m, int estudiantesUniv = 10000)
    {
        return new DatosInstitucionales(
            periodo: "2026",
            numeroEstudiantesUniversidad: estudiantesUniv,
            numeroDocentesUniversidad: 500,
            numeroPersonasPlantaCentral: 200,
            sueldoBasico: totalMensual,
            funcional: 0m,
            fondoReserva: 0m,
            beneficioXiv: 0m,
            beneficioXiii: 0m,
            aportePatronal: 0m,
            varios: 0m,
            actualizadoPorUsuarioId: 1,
            fuenteNotas: null);
    }

    private static ProyeccionEstudiantesDto NuevaProyeccion(params (int anio, int np, decimal alumnos)[] periodos)
    {
        var detalles = periodos.Select((p, idx) => new DetalleProyeccionEstudiantesDto
        {
            Id = idx + 1,
            PeriodoAcademicoId = idx + 1,
            Anio = p.anio,
            NumeroPeriodo = p.np,
            EtiquetaPeriodo = $"{p.anio}-P{p.np}",
            NumeroCiclo = 1,
            CantidadParalelos = 1,
            TotalEstudiantes = p.alumnos,
        }).ToList();

        return new ProyeccionEstudiantesDto
        {
            Id = 99,
            CarreraId = 1,
            CarreraNombre = "Ingenieria Test",
            CarreraCodigo = "TEST",
            EscenarioProyeccionId = 1,
            EscenarioNombre = "Base",
            AnioBase = periodos.Min(p => p.anio),
            SemanasPorSemestre = 16,
            CreadoEn = DateTime.UtcNow,
            Detalles = detalles,
        };
    }

    private sealed class RepositorioCargoFacultadFake(IReadOnlyList<CargoFacultad> cargos) : IRepositorioCargoFacultad
    {
        public Task<CargoFacultad?> ObtenerPorIdAsync(int id, CancellationToken ct = default) => Task.FromResult<CargoFacultad?>(cargos.FirstOrDefault());
        public Task<IReadOnlyList<CargoFacultad>> ListarPorCarreraAsync(int carreraId, CancellationToken ct = default)
            => Task.FromResult(cargos);
        public Task AgregarAsync(CargoFacultad cargo, CancellationToken ct = default) => Task.CompletedTask;
        public void Actualizar(CargoFacultad cargo) { }
        public void Eliminar(CargoFacultad cargo) { }
    }

    private sealed class RepositorioInflacionAnualFake : IRepositorioInflacionAnual
    {
        public Task<IReadOnlyList<InflacionAnual>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InflacionAnual>>(Array.Empty<InflacionAnual>());

        public Task<IReadOnlyList<InflacionAnual>> ListarPorRangoAsync(int anioDesde, int anioHasta, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InflacionAnual>>(Array.Empty<InflacionAnual>());

        public Task<InflacionAnual?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<InflacionAnual?>(null);
        public Task<InflacionAnual?> ObtenerPorAnioAsync(int anio, CancellationToken cancellationToken = default) => Task.FromResult<InflacionAnual?>(null);
        public Task<bool> ExisteAnioAsync(int anio, int? excluirId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AgregarAsync(InflacionAnual inflacionAnual, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ActualizarAsync(InflacionAnual inflacionAnual, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EliminarPorIdAsync(int id, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<(int registrosAnualesEliminados, int registrosProyectadosEliminados)> LimpiarTodoAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((0, 0));
    }

    private sealed class RepositorioProyeccionEstudiantesFake(ProyeccionEstudiantesDto? proyeccion) : IRepositorioProyeccionEstudiantes
    {
        public Task<IReadOnlyList<ResumenProyeccionEstudiantesDto>> ListarResumenAsync(int? carreraId = null, int? escenarioProyeccionId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ResumenProyeccionEstudiantesDto>>(Array.Empty<ResumenProyeccionEstudiantesDto>());
        public Task<ProyeccionEstudiantesDto?> ObtenerDtoPorIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(proyeccion);
        public Task<int?> ObtenerIdPorCarreraYEscenarioAsync(int carreraId, int escenarioProyeccionId, CancellationToken cancellationToken = default)
            => Task.FromResult(proyeccion is null ? (int?)null : proyeccion.Id);
        public Task<int> GuardarAsync(int carreraId, int escenarioProyeccionId, int anioBase, int semanasPorSemestre, IReadOnlyList<CeldaProyeccionEstudiantesDto> celdas, int? usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
        public Task EliminarPorIdAsync(int id, int? usuarioId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RepositorioCarreraFake : IRepositorioCarrera
    {
        public Task<IReadOnlyList<Carrera>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Carrera>>([]);
        public Task<Carrera?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var carrera = new Carrera("TEST", "Ingenieria Test", "Facultad Test", 8);
            carrera.RehidratarId(id);
            return Task.FromResult<Carrera?>(carrera);
        }
        public Task<Carrera?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancellationToken = default) => Task.FromResult<Carrera?>(null);
        public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AgregarAsync(Carrera carrera, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ActualizarAsync(Carrera carrera, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EliminarPorIdAsync(int id, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RepositorioDatosInstitucionalesFake(DatosInstitucionales? vigente) : IRepositorioDatosInstitucionales
    {
        public Task<DatosInstitucionales?> ObtenerPorPeriodoAsync(string periodo, CancellationToken cancellationToken = default) => Task.FromResult(vigente);
        public Task<DatosInstitucionales?> ObtenerVigenteAsync(CancellationToken cancellationToken = default) => Task.FromResult(vigente);
        public Task<DatosInstitucionales?> ObtenerAnteriorAsync(string periodoActual, CancellationToken cancellationToken = default) => Task.FromResult<DatosInstitucionales?>(null);
        public Task<IReadOnlyList<DatosInstitucionales>> ListarHistoricoAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DatosInstitucionales>>(vigente is null ? [] : [vigente]);
        public void Agregar(DatosInstitucionales datos) { }
        public void Actualizar(DatosInstitucionales datos) { }
    }

    // Sin configuración de retención el consolidado interno queda null: mismo comportamiento
    // que antes de KAN-49 para estos tests (docentes sin consolidado explícito).
    private sealed class RepositorioConfiguracionRetencionFake : IRepositorioConfiguracionRetencion
    {
        public Task<IReadOnlyList<SistemaAranceles.Application.DTOs.TasaRetencion.ConfiguracionRetencionDto>> ListarDtoAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SistemaAranceles.Application.DTOs.TasaRetencion.ConfiguracionRetencionDto>>([]);
        public Task<SistemaAranceles.Application.DTOs.TasaRetencion.ConfiguracionRetencionDto?> ObtenerDtoPorIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<SistemaAranceles.Application.DTOs.TasaRetencion.ConfiguracionRetencionDto?>(null);
        public Task<ConfiguracionRetencion?> ObtenerDominioPorIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<ConfiguracionRetencion?>(null);
        public Task<ConfiguracionRetencion?> ObtenerActivoPorCarreraYEscenarioNombreAsync(int carreraId, string escenarioNombre, CancellationToken cancellationToken = default)
            => Task.FromResult<ConfiguracionRetencion?>(null);
        public Task<bool> ExisteCombinacionAsync(int carreraId, int escenarioProyeccionId, int? excluirId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(false);
        public Task<int?> ObtenerIdCualquierEstadoPorCombinacionAsync(int carreraId, int escenarioProyeccionId, CancellationToken cancellationToken = default)
            => Task.FromResult<int?>(null);
        public Task AgregarAsync(ConfiguracionRetencion configuracion, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ActualizarAsync(ConfiguracionRetencion configuracion, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EliminarPorIdAsync(int id, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RepositorioOverrideHorasPeriodoFake : IRepositorioOverrideHorasPeriodo
    {
        public Task<IReadOnlyList<OverrideHorasPeriodo>> ListarPorProyeccionAsync(int proyeccionId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OverrideHorasPeriodo>>([]);
        public Task<OverrideHorasPeriodo?> ObtenerPorProyeccionYPeriodoAsync(int proyeccionId, int periodo, CancellationToken ct = default)
            => Task.FromResult<OverrideHorasPeriodo?>(null);
        public Task AgregarAsync(OverrideHorasPeriodo entidad, CancellationToken ct = default) => Task.CompletedTask;
        public void Actualizar(OverrideHorasPeriodo entidad) { }
        public void Eliminar(OverrideHorasPeriodo entidad) { }
    }
}
