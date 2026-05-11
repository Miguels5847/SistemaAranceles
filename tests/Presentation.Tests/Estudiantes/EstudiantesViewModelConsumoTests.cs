using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Auditoria;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Presentation.State;
using SistemaAranceles.Presentation.ViewModels.Estudiantes;
using Xunit;

namespace SistemaAranceles.Presentation.Tests.Estudiantes;

public class EstudiantesViewModelConsumoTests
{
    [Fact]
    public async Task GuardarEdicionHorasMalla_ActualizaOverrideYRecalcula()
    {
        var repositorio = new FakeRepositorioOverrideHorasPeriodo();
        var provider = BuildServiceProvider(repositorio);
        var sesion = BuildSesionAdmin();
        var vm = new EstudiantesViewModel(provider, sesion);

        var proyeccion = CrearProyeccionMinima();
        var config = CrearConfiguracionMinima();
        var consolidado = ConsolidadorProyeccionEstudiantes.Calcular(
            proyeccion,
            config.ParalelosPeriodo1,
            config.ParalelosPeriodo2,
            config.TasaRetencionPorcentaje,
            config.TasaGraduacionPorcentaje);

        vm.DetalleConsolidado = consolidado;
        vm.PeriodoConsumoSeleccionado = vm.DetalleConsolidado.TablaPeriodos[0];

        SetPrivateField(vm, "_ultimaProyeccion", proyeccion);
        SetPrivateField(vm, "_ultimaConfig", config);

        vm.AbrirEdicionHorasMallaCommand.Execute(null);
        vm.HorasDocenciaEdicion = "377";
        vm.HorasPracticaEdicion = "188";

        await vm.GuardarEdicionHorasMallaCommand.ExecuteAsync(null);

        Assert.False(vm.EstaEditandoHorasMalla);
        Assert.NotNull(vm.DetalleConsolidado);
        Assert.Equal(377m, vm.DetalleConsolidado!.TablaPeriodos[0].HorasDocencia);
        Assert.Equal(188m, vm.DetalleConsolidado.TablaPeriodos[0].HorasPractica);

        var overrideGuardado = await repositorio.ObtenerPorProyeccionYPeriodoAsync(proyeccion.Id, 1);
        Assert.NotNull(overrideGuardado);
        Assert.Equal(377m, overrideGuardado!.HorasDocencia);
        Assert.Equal(188m, overrideGuardado.HorasPractica);
    }

    [Fact]
    public void CancelarEdicionHorasMalla_CierraPanelSinGuardar()
    {
        var repositorio = new FakeRepositorioOverrideHorasPeriodo();
        var provider = BuildServiceProvider(repositorio);
        var sesion = BuildSesionAdmin();
        var vm = new EstudiantesViewModel(provider, sesion)
        {
            DetalleConsolidado = new ProyeccionConsolidadaDto
            {
                TablaPeriodos = new List<FilaConsumoPeriodicDto>
                {
                    new()
                    {
                        Periodo = 1,
                        Anio = 2026,
                        Semestre = "Abril",
                        Docentes = 1,
                        Tecnicos = 1,
                        HorasDocencia = 288m,
                        HorasPractica = 160m
                    }
                }
            }
        };

        vm.PeriodoConsumoSeleccionado = vm.DetalleConsolidado.TablaPeriodos[0];
        vm.AbrirEdicionHorasMallaCommand.Execute(null);

        Assert.True(vm.EstaEditandoHorasMalla);

        vm.HorasDocenciaEdicion = "500";
        vm.HorasPracticaEdicion = "300";
        vm.CancelarEdicionHorasMallaCommand.Execute(null);

        Assert.False(vm.EstaEditandoHorasMalla);
        Assert.Equal(288m, vm.DetalleConsolidado.TablaPeriodos[0].HorasDocencia);
        Assert.Equal(160m, vm.DetalleConsolidado.TablaPeriodos[0].HorasPractica);
    }

    private static ServiceProvider BuildServiceProvider(FakeRepositorioOverrideHorasPeriodo repositorio)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IRepositorioOverrideHorasPeriodo>(repositorio);
        services.AddSingleton<IUnidadTrabajo, FakeUnidadTrabajo>();
        services.AddSingleton<IAuditoriaServicio, FakeAuditoriaServicio>();

        services.AddScoped<EditarConsumoPeriodoUseCase>();
        services.AddScoped<ListarOverridesHorasPeriodoUseCase>();
        services.AddScoped<RestaurarConsumoPeriodoUseCase>();

        return services.BuildServiceProvider();
    }

    private static SesionActual BuildSesionAdmin()
    {
        var sesion = new SesionActual();
        sesion.IniciarSesion(1, "Admin", "admin@test.local", "Administrador", "token");
        sesion.EstablecerPermisos(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ES.VER", "ES.CREAR", "ES.ELIMINAR", "ES.EDITAR"
        });
        return sesion;
    }

    private static ProyeccionEstudiantesDto CrearProyeccionMinima()
        => new()
        {
            Id = 100,
            CarreraId = 1,
            EscenarioProyeccionId = 1,
            AnioBase = 2026,
            SemanasPorSemestre = 16,
            Detalles = new List<DetalleProyeccionEstudiantesDto>
            {
                new()
                {
                    Id = 1,
                    PeriodoAcademicoId = 1,
                    Anio = 2026,
                    NumeroPeriodo = 1,
                    EtiquetaPeriodo = "P1",
                    NumeroCiclo = 1,
                    CantidadParalelos = 1,
                    TotalEstudiantes = 120m,
                }
            }
        };

    private static ConfiguracionRetencionDto CrearConfiguracionMinima()
        => new()
        {
            Id = 1,
            CarreraId = 1,
            EscenarioProyeccionId = 1,
            ParalelosPeriodo1 = 1,
            ParalelosPeriodo2 = 1,
            TasaRetencionPorcentaje = 70m,
            TasaGraduacionPorcentaje = 70m,
            MetaRetencionPorcentaje = 70m,
            MetaGraduacionPorcentaje = 70m,
        };

    private static void SetPrivateField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private sealed class FakeUnidadTrabajo : IUnidadTrabajo
    {
        public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task IniciarTransaccionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ConfirmarTransaccionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RevertirTransaccionAsync() => Task.CompletedTask;
    }

    private sealed class FakeAuditoriaServicio : IAuditoriaServicio
    {
        public Task RegistrarAsync(string moduloNombre, string entidadNombre, string entidadId, string accionNombre, string resumenTexto, int? ejecutadoPorUsuarioId = null, string? valoresAnterioresJson = null, string? valoresNuevosJson = null, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<AuditoriaConsultaResultadoDto> ConsultarAsync(AuditoriaFiltroDto filtros, CancellationToken cancellationToken = default)
            => Task.FromResult(new AuditoriaConsultaResultadoDto());
    }

    private sealed class FakeRepositorioOverrideHorasPeriodo : IRepositorioOverrideHorasPeriodo
    {
        private readonly List<SistemaAranceles.Domain.Entities.OverrideHorasPeriodo> _items = [];

        public Task<IReadOnlyList<SistemaAranceles.Domain.Entities.OverrideHorasPeriodo>> ListarPorProyeccionAsync(int proyeccionId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SistemaAranceles.Domain.Entities.OverrideHorasPeriodo>>(
                _items.Where(x => x.ProyeccionId == proyeccionId).OrderBy(x => x.Periodo).ToList());

        public Task<SistemaAranceles.Domain.Entities.OverrideHorasPeriodo?> ObtenerPorProyeccionYPeriodoAsync(int proyeccionId, int periodo, CancellationToken ct = default)
            => Task.FromResult(_items.FirstOrDefault(x => x.ProyeccionId == proyeccionId && x.Periodo == periodo));

        public Task AgregarAsync(SistemaAranceles.Domain.Entities.OverrideHorasPeriodo entidad, CancellationToken ct = default)
        {
            _items.Add(entidad);
            return Task.CompletedTask;
        }

        public void Actualizar(SistemaAranceles.Domain.Entities.OverrideHorasPeriodo entidad)
        {
            var existente = _items.FirstOrDefault(x => x.ProyeccionId == entidad.ProyeccionId && x.Periodo == entidad.Periodo);
            if (existente is null)
            {
                _items.Add(entidad);
                return;
            }

            existente.CambiarHorasDocencia(entidad.HorasDocencia);
            existente.CambiarHorasPractica(entidad.HorasPractica);
        }

        public void Eliminar(SistemaAranceles.Domain.Entities.OverrideHorasPeriodo entidad)
        {
            _items.RemoveAll(x => x.ProyeccionId == entidad.ProyeccionId && x.Periodo == entidad.Periodo);
        }
    }
}
