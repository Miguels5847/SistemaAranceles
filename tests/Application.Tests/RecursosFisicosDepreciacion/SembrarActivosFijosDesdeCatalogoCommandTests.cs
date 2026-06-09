using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;
using Xunit;

namespace SistemaAranceles.Application.Tests.RecursosFisicosDepreciacion;

public class SembrarActivosFijosDesdeCatalogoCommandTests
{
    [Fact]
    public async Task EjecutarAsync_CompletaSoloLosActivosFaltantes_DeUnaCarreraExistente()
    {
        var repoCatalogo = new RepositorioCatalogoActivoBaseFalso(
        [
            CrearPlantilla("Operatividad EVEA", TipoCalculoCantidad.PorEstudiante, 7.5m, 0m),
            CrearPlantilla("Licencias Zoom", TipoCalculoCantidad.PorDocente, 13.5m, 10m)
        ]);
        var repoActivos = new RepositorioActivoFijoFalso(["Archivador de madera"]);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var command = new SembrarActivosFijosDesdeCatalogoCommand(repoCatalogo, repoActivos, unidadTrabajo);

        var insertados = await command.EjecutarAsync(25);

        Assert.Equal(2, insertados);
        Assert.Equal(2, repoActivos.Agregados.Count);
        Assert.Contains(repoActivos.Agregados, x => x.Descripcion == "Operatividad EVEA");
        Assert.Contains(repoActivos.Agregados, x => x.Descripcion == "Licencias Zoom");
        Assert.All(repoActivos.Agregados, x => Assert.Equal(CategoriaActivoFijo.LaboratoriosEquipos, x.Categoria));
        Assert.Equal(1, unidadTrabajo.VecesGuardado);
    }

    [Fact]
    public async Task EjecutarAsync_NoReinserta_DescripcionesQueYaExistieronEnLaCarrera()
    {
        var repoCatalogo = new RepositorioCatalogoActivoBaseFalso(
        [
            CrearPlantilla("Operatividad EVEA", TipoCalculoCantidad.PorEstudiante, 7.5m, 0m),
            CrearPlantilla("Licencias Zoom", TipoCalculoCantidad.PorDocente, 13.5m, 10m)
        ]);
        var repoActivos = new RepositorioActivoFijoFalso(["Operatividad EVEA", "Licencias Zoom"]);
        var unidadTrabajo = new UnidadTrabajoFalsa();
        var command = new SembrarActivosFijosDesdeCatalogoCommand(repoCatalogo, repoActivos, unidadTrabajo);

        var insertados = await command.EjecutarAsync(25);

        Assert.Equal(0, insertados);
        Assert.Empty(repoActivos.Agregados);
        Assert.Equal(0, unidadTrabajo.VecesGuardado);
    }

    private static CatalogoActivoBase CrearPlantilla(
        string descripcion,
        TipoCalculoCantidad tipoCalculoCantidad,
        decimal valorUnitario,
        decimal offsetCantidad)
    {
        return new CatalogoActivoBase(
            descripcion: descripcion,
            categoria: CategoriaActivoFijo.LaboratoriosEquipos,
            tipoCalculoCantidad: tipoCalculoCantidad,
            cantidadDefault: 0m,
            unidadMedida: "UNI",
            valorUnitario: valorUnitario,
            factorMultiplicador: 1m,
            offsetCantidad: offsetCantidad,
            vidaUtilAnios: 10,
            porcentajeResidual: 0.05m);
    }

    private sealed class RepositorioCatalogoActivoBaseFalso(IReadOnlyList<CatalogoActivoBase> plantillas)
        : IRepositorioCatalogoActivoBase
    {
        public Task<IReadOnlyList<CatalogoActivoBase>> ListarActivosAsync(CancellationToken ct = default)
            => Task.FromResult(plantillas);

        public Task<CatalogoActivoBase?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
            => Task.FromResult<CatalogoActivoBase?>(null);

        public Task AgregarAsync(CatalogoActivoBase item, CancellationToken ct = default)
            => Task.CompletedTask;

        public void Actualizar(CatalogoActivoBase item)
        {
        }

        public Task<int> SembrarPorDefectoAsync(
            IReadOnlyList<SistemaAranceles.Domain.Constantes.ActivoBasePorDefecto> items,
            CancellationToken ct = default)
            => Task.FromResult(0);
    }

    private sealed class RepositorioActivoFijoFalso(IEnumerable<string> descripcionesRegistradas)
        : IRepositorioActivoFijo
    {
        private readonly IReadOnlyCollection<string> _descripcionesRegistradas = descripcionesRegistradas.ToArray();

        public List<ActivoFijo> Agregados { get; } = [];

        public Task<ActivoFijo?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
            => Task.FromResult<ActivoFijo?>(null);

        public Task<IReadOnlyList<ActivoFijo>> ListarPorCarreraAsync(
            int carreraId,
            CategoriaActivoFijo? categoria = null,
            CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ActivoFijo>>([]);

        public Task<IReadOnlyCollection<string>> ListarDescripcionesRegistradasPorCarreraAsync(
            int carreraId,
            CancellationToken ct = default)
            => Task.FromResult(_descripcionesRegistradas);

        public Task AgregarAsync(ActivoFijo activo, CancellationToken ct = default)
        {
            Agregados.Add(activo);
            return Task.CompletedTask;
        }

        public void Actualizar(ActivoFijo activo)
        {
        }

        public void EliminarLogico(ActivoFijo activo, int? eliminadoPorUsuarioId)
        {
        }
    }

    private sealed class UnidadTrabajoFalsa : IUnidadTrabajo
    {
        public int VecesGuardado { get; private set; }

        public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            VecesGuardado++;
            return Task.FromResult(1);
        }

        public Task IniciarTransaccionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ConfirmarTransaccionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RevertirTransaccionAsync()
            => Task.CompletedTask;
    }
}
