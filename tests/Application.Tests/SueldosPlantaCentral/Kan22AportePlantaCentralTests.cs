using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.SueldosPlantaCentral;
using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Entities;
using Xunit;

namespace SistemaAranceles.Application.Tests.SueldosPlantaCentral;

public class Kan22AportePlantaCentralTests
{
    [Fact]
    public async Task Calcular_AporteSemestralCoincideConFormula()
    {
        // TotalMensualPC = 100000, EstUniv = 10000 => costo per capita mensual = 10
        // AlumnosPeriodo = 200 => aporte semestral = 10 * 200 * 6 = 12000
        var datos = NuevoDatos(totalMensual: 100000m, estudiantesUniv: 10000);
        var proyeccion = NuevaProyeccion(
            (anio: 2026, np: 1, alumnos: 200m),
            (anio: 2026, np: 2, alumnos: 200m));

        var query = ConstruirQuery(datos, proyeccion);
        var resultado = await query.EjecutarAsync(carreraId: 1, escenarioProyeccionId: 1);

        Assert.Equal(2, resultado.Periodos.Count);
        Assert.Equal(12000m, resultado.Periodos[0].AporteSemestral);
        Assert.Equal(12000m, resultado.Periodos[1].AporteSemestral);
    }

    [Fact]
    public async Task Calcular_AcumuladoYPromedioAnualCorrectos()
    {
        var datos = NuevoDatos(totalMensual: 100000m, estudiantesUniv: 10000);
        var proyeccion = NuevaProyeccion(
            (2026, 1, 100m),
            (2026, 2, 100m),
            (2027, 1, 200m),
            (2027, 2, 200m));

        var query = ConstruirQuery(datos, proyeccion);
        var resultado = await query.EjecutarAsync(1, 1);

        // P1=6000, P2=6000, P3=12000, P4=12000 => acumulado 36000, 2 anios => promedio anual 18000
        Assert.Equal(36000m, resultado.AporteAcumulado);
        Assert.Equal(18000m, resultado.AportePromedioAnual);
    }

    [Fact]
    public async Task Calcular_AgrupaCiclosDelMismoPeriodoAntesDeCalcular()
    {
        var datos = NuevoDatos(totalMensual: 100000m, estudiantesUniv: 10000);
        var proyeccion = new ProyeccionEstudiantesDto
        {
            Id = 99,
            CarreraId = 1,
            CarreraNombre = "Ingenieria Test",
            CarreraCodigo = "TEST",
            EscenarioProyeccionId = 1,
            EscenarioNombre = "Base",
            AnioBase = 2026,
            SemanasPorSemestre = 16,
            CreadoEn = DateTime.UtcNow,
            Detalles =
            [
                new()
                {
                    Id = 1,
                    PeriodoAcademicoId = 1,
                    Anio = 2026,
                    NumeroPeriodo = 1,
                    EtiquetaPeriodo = "Abr 2026",
                    NumeroCiclo = 1,
                    CantidadParalelos = 1,
                    TotalEstudiantes = 100m,
                },
                new()
                {
                    Id = 2,
                    PeriodoAcademicoId = 1,
                    Anio = 2026,
                    NumeroPeriodo = 1,
                    EtiquetaPeriodo = "Abr 2026",
                    NumeroCiclo = 2,
                    CantidadParalelos = 1,
                    TotalEstudiantes = 50m,
                },
                new()
                {
                    Id = 3,
                    PeriodoAcademicoId = 2,
                    Anio = 2026,
                    NumeroPeriodo = 2,
                    EtiquetaPeriodo = "Sep 2026",
                    NumeroCiclo = 1,
                    CantidadParalelos = 1,
                    TotalEstudiantes = 80m,
                },
            ],
        };

        var query = ConstruirQuery(datos, proyeccion);
        var resultado = await query.EjecutarAsync(1, 1);

        Assert.Equal(2, resultado.Periodos.Count);
        Assert.Equal(150m, resultado.Periodos[0].AlumnosCarrera);
        Assert.Equal(9000m, resultado.Periodos[0].AporteSemestral);
        Assert.Equal(80m, resultado.Periodos[1].AlumnosCarrera);
        Assert.Equal(4800m, resultado.Periodos[1].AporteSemestral);
    }

    [Fact]
    public async Task Calcular_PorcentajeSobreTotalAnualEsAporteSobreTotalAnual()
    {
        var datos = NuevoDatos(totalMensual: 100000m, estudiantesUniv: 10000);
        var proyeccion = NuevaProyeccion((2026, 1, 100m));

        var query = ConstruirQuery(datos, proyeccion);
        var resultado = await query.EjecutarAsync(1, 1);

        // Aporte semestral = 6000; Total anual PC = 1.200.000 => 0.005
        Assert.Equal(0.005m, resultado.Periodos[0].PorcentajeSobreTotalAnual);
    }

    [Fact]
    public async Task Calcular_LanzaSiNoHayDatosInstitucionalesVigentes()
    {
        var proyeccion = NuevaProyeccion((2026, 1, 100m));
        var query = ConstruirQuery(null, proyeccion);

        await Assert.ThrowsAsync<DominioException>(() => query.EjecutarAsync(1, 1));
    }

    [Fact]
    public async Task Calcular_LanzaSiNoExisteProyeccion()
    {
        var datos = NuevoDatos();
        var query = ConstruirQuery(datos, null);

        await Assert.ThrowsAsync<DominioException>(() => query.EjecutarAsync(1, 1));
    }

    private static CalcularAportePlantaCentralCarreraQuery ConstruirQuery(
        DatosInstitucionales? datos,
        ProyeccionEstudiantesDto? proyeccion)
    {
        return new CalcularAportePlantaCentralCarreraQuery(
            new RepositorioDatosInstitucionalesFake(datos),
            new RepositorioProyeccionEstudiantesFake(proyeccion),
            new RepositorioCarreraFake());
    }

    private static DatosInstitucionales NuevoDatos(decimal totalMensual = 100000m, int estudiantesUniv = 10000)
    {
        // Hacemos que la masa salarial mensual = totalMensual asignandolo a SueldoBasico.
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

    private sealed class RepositorioProyeccionEstudiantesFake(ProyeccionEstudiantesDto? proyeccion) : IRepositorioProyeccionEstudiantes
    {
        public Task<IReadOnlyList<ResumenProyeccionEstudiantesDto>> ListarResumenAsync(int? carreraId = null, int? escenarioProyeccionId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ResumenProyeccionEstudiantesDto>>([]);
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
}
