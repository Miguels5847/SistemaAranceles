using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;
using Xunit;

namespace SistemaAranceles.Application.Tests.DemandaIngresos;

public class MaterialesYArancelTests
{
    [Fact]
    public void RatioMaterial_FijoPeriodo_NoMultiplicaPorEstudiantesNiMeses()
    {
        var ratio = new RatioMaterialDemanda(
            carreraId: 1,
            categoria: "ASEO_LIMPIEZA",
            concepto: "Escoba",
            itemMaterialInsumoId: null,
            ratioConsumo: 2m,
            unidadRatio: UnidadRatioMaterial.FijoPeriodo,
            mesesOperativos: 6,
            aplicaInflacion: true);

        Assert.Equal(2m, ratio.CalcularCantidad(30m));
        Assert.Equal(2m, ratio.CalcularCantidad(0m));
    }

    [Fact]
    public void RatioMaterial_PorDocente_UsaDocentesMasAdicionalFijo()
    {
        var ratio = new RatioMaterialDemanda(
            carreraId: 1,
            categoria: "ACCESORIOS_MATERIALES",
            concepto: "Grapadora",
            itemMaterialInsumoId: null,
            ratioConsumo: 1m,
            unidadRatio: UnidadRatioMaterial.PorDocente,
            mesesOperativos: 1,
            aplicaInflacion: true,
            cantidadFijaAdicional: 4m);

        Assert.Equal(9m, ratio.CalcularCantidad(estudiantes: 30m, docentes: 5m));
        Assert.Equal(4m, ratio.CalcularCantidad(estudiantes: 30m, docentes: 0m));
    }

    [Theory]
    [InlineData(UnidadRatioMaterial.PorEstudiante, 30, 2, 6, 60)]
    [InlineData(UnidadRatioMaterial.PorEstudianteMes, 30, 0.13, 6, 23.4)]
    public void RatioMaterial_UnidadesExistentes_MantienenFormula(
        UnidadRatioMaterial unidad,
        decimal estudiantes,
        decimal consumo,
        int meses,
        decimal esperado)
    {
        var ratio = new RatioMaterialDemanda(
            carreraId: 1,
            categoria: "MATERIALES_SUMINISTROS",
            concepto: "Consumo",
            itemMaterialInsumoId: null,
            ratioConsumo: consumo,
            unidadRatio: unidad,
            mesesOperativos: meses,
            aplicaInflacion: true);

        Assert.Equal(esperado, ratio.CalcularCantidad(estudiantes));
    }

    [Fact]
    public async Task ArancelEfectivo_UsaConfiguracionGlobal_CuandoNoExisteEspecifica()
    {
        var repoArancel = new RepositorioArancelExactoFake([
            Configuracion(carreraId: 1, escenarioId: null, arancel: 1000m)
        ]);
        var query = new ObtenerArancelEfectivoQuery(
            repoArancel,
            new RepositorioCarreraFake(),
            new RepositorioDatosInstitucionalesFake());

        var resultado = await query.EjecutarAsync(carreraId: 1, escenarioProyeccionId: 7);

        Assert.Equal(1000m, resultado.ArancelEfectivo);
        Assert.Equal(100m, resultado.MatriculaEfectiva);
        Assert.Null(resultado.MensajeAdvertencia);
        Assert.Contains(repoArancel.Consultas, c => c == (1, 7));
        Assert.Contains(repoArancel.Consultas, c => c == (1, null));
    }

    [Fact]
    public async Task ArancelEfectivo_PriorizaConfiguracionEspecifica_SobreGlobal()
    {
        var repoArancel = new RepositorioArancelExactoFake([
            Configuracion(carreraId: 1, escenarioId: null, arancel: 1000m),
            Configuracion(carreraId: 1, escenarioId: 7, arancel: 1500m)
        ]);
        var query = new ObtenerArancelEfectivoQuery(
            repoArancel,
            new RepositorioCarreraFake(),
            new RepositorioDatosInstitucionalesFake());

        var resultado = await query.EjecutarAsync(carreraId: 1, escenarioProyeccionId: 7);

        Assert.Equal(1500m, resultado.ArancelEfectivo);
        Assert.Equal(150m, resultado.MatriculaEfectiva);
        Assert.Null(resultado.MensajeAdvertencia);
        Assert.DoesNotContain(repoArancel.Consultas, c => c == (1, null));
    }

    private static ConfiguracionArancelCarreraDto Configuracion(int carreraId, int? escenarioId, decimal arancel)
        => new()
        {
            Id = escenarioId ?? 99,
            CarreraId = carreraId,
            CarreraNombre = "Administracion",
            CarreraCodigo = "ADM",
            EscenarioProyeccionId = escenarioId,
            EscenarioNombre = escenarioId is null ? "Global" : $"Escenario {escenarioId}",
            ModoCalculoArancel = "Manual",
            ArancelManual = arancel,
            UsaPorcentajeMatriculaInstitucional = true,
            EstaActivo = true
        };

    private sealed class RepositorioArancelExactoFake(IReadOnlyList<ConfiguracionArancelCarreraDto> configuraciones)
        : IRepositorioConfiguracionArancelCarrera
    {
        public List<(int carreraId, int? escenarioId)> Consultas { get; } = [];

        public Task<IReadOnlyList<ConfiguracionArancelCarreraDto>> ListarAsync(
            int? carreraId = null,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ConfiguracionArancelCarreraDto>>(configuraciones
                .Where(c => carreraId is null || c.CarreraId == carreraId)
                .ToList());

        public Task<ConfiguracionArancelCarreraDto?> ObtenerPorCarreraEscenarioAsync(
            int carreraId,
            int? escenarioProyeccionId,
            CancellationToken ct = default)
        {
            Consultas.Add((carreraId, escenarioProyeccionId));
            return Task.FromResult(configuraciones.FirstOrDefault(c =>
                c.CarreraId == carreraId && c.EscenarioProyeccionId == escenarioProyeccionId));
        }

        public Task<ConfiguracionArancelCarrera?> ObtenerDominioAsync(int id, CancellationToken ct = default)
            => Task.FromResult<ConfiguracionArancelCarrera?>(null);

        public Task<int> GuardarAsync(GuardarConfiguracionArancelCarreraDto dto, int? usuarioId, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task EliminarAsync(int id, int? usuarioId, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class RepositorioCarreraFake : IRepositorioCarrera
    {
        public Task<IReadOnlyList<Carrera>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Carrera>>([]);

        public Task<Carrera?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Carrera?>(new Carrera("ADM", "Administracion", "Facultad", 8));

        public Task<Carrera?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancellationToken = default)
            => Task.FromResult<Carrera?>(null);

        public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AgregarAsync(Carrera carrera, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task ActualizarAsync(Carrera carrera, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task EliminarPorIdAsync(int id, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class RepositorioDatosInstitucionalesFake : IRepositorioDatosInstitucionales
    {
        public Task<DatosInstitucionales?> ObtenerPorPeriodoAsync(string periodo, CancellationToken cancellationToken = default)
            => Task.FromResult<DatosInstitucionales?>(null);

        public Task<DatosInstitucionales?> ObtenerVigenteAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<DatosInstitucionales?>(null);

        public Task<DatosInstitucionales?> ObtenerAnteriorAsync(string periodoActual, CancellationToken cancellationToken = default)
            => Task.FromResult<DatosInstitucionales?>(null);

        public Task<IReadOnlyList<DatosInstitucionales>> ListarHistoricoAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DatosInstitucionales>>([]);

        public void Agregar(DatosInstitucionales datos)
        {
        }

        public void Actualizar(DatosInstitucionales datos)
        {
        }
    }
}
