using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.AnalisisFinanciero;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.AnalisisFinanciero;

public sealed class EscenarioAnalisisFinancieroOpcion
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public bool EsPredeterminado { get; init; }
    public bool TieneProyeccion { get; init; }
    public string NombreDisplay => TieneProyeccion ? $"{Nombre} (con proyección)" : $"{Nombre} (sin proyección)";
}

public sealed partial class AnalisisFinancieroViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private bool _suprimirCambios;

    public AnalisisFinancieroViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty] private ObservableCollection<Carrera> _carreras = [];
    [ObservableProperty] private Carrera? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioAnalisisFinancieroOpcion> _escenarios = [];
    [ObservableProperty] private EscenarioAnalisisFinancieroOpcion? _escenarioSeleccionado;
    [ObservableProperty] private EstadoPerdidasGananciasDto? _estadoPerdidasGanancias;
    [ObservableProperty] private FlujoFondosDto? _flujoFondos;
    [ObservableProperty] private IndicadoresFinancierosDto? _indicadoresFinancieros;
    [ObservableProperty] private PeriodoRecuperacionDto? _periodoRecuperacion;
    [ObservableProperty] private PuntoEquilibrioDto? _puntoEquilibrio;
    [ObservableProperty] private ArancelOptimoBiseccionDto? _arancelOptimoBiseccion;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private bool _estaCargando;

    public IReadOnlyList<string> EtiquetasPerdidasGanancias => EstadoPerdidasGanancias?.EtiquetasPeriodos ?? [];
    public IReadOnlyList<EstadoPerdidasGananciasRubroDto> FilasPerdidasGanancias => EstadoPerdidasGanancias?.Filas ?? [];
    public bool TieneEstadoPerdidasGanancias => EstadoPerdidasGanancias?.TieneDatos == true;
    public IReadOnlyList<string> EtiquetasFlujoFondos => FlujoFondos?.EtiquetasPeriodos ?? [];
    public IReadOnlyList<FlujoFondosRubroDto> FilasFlujoFondos => FlujoFondos?.Filas ?? [];
    public bool TieneFlujoFondos => FlujoFondos?.TieneDatos == true;
    public IReadOnlyList<IndicadorVanPeriodoDto> DetalleVanFinanciero => IndicadoresFinancieros?.DetalleVan ?? [];
    public bool TieneIndicadoresFinancieros => IndicadoresFinancieros?.TieneDatos == true;
    public IReadOnlyList<PeriodoRecuperacionDetalleDto> DetallePeriodoRecuperacion => PeriodoRecuperacion?.Detalle ?? [];
    public bool TienePeriodoRecuperacion => PeriodoRecuperacion?.TieneDatos == true;
    public IReadOnlyList<PuntoEquilibrioPeriodoDto> DetallePuntoEquilibrio => PuntoEquilibrio?.Periodos ?? [];
    public bool TienePuntoEquilibrio => PuntoEquilibrio?.TieneDatos == true;
    public IReadOnlyList<ArancelOptimoBiseccionIteracionDto> IteracionesArancelOptimo => ArancelOptimoBiseccion?.Iteraciones ?? [];
    public IReadOnlyList<ArancelOptimoBiseccionPeriodoDto> DetalleArancelOptimo => ArancelOptimoBiseccion?.Periodos ?? [];
    public bool TieneArancelOptimoBiseccion => ArancelOptimoBiseccion?.TieneDatos == true;
    public bool PuedeUsarArancelOptimo => PuedeEditar
                                           && !EstaCargando
                                           && ArancelOptimoBiseccion?.Disponible == true
                                           && CarreraSeleccionada is not null
                                           && EscenarioSeleccionado is not null;
    public bool PuedeEditar => _sesionActual.EsAdministrador || _sesionActual.TienePermiso("DI_NG.EDITAR");

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        _ = value;
        if (_suprimirCambios || EstaCargando)
            return;

        _ = RecargarEscenariosAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioAnalisisFinancieroOpcion? value)
    {
        _ = value;
        if (_suprimirCambios || EstaCargando)
            return;

        _ = RefrescarAsync();
    }

    partial void OnEstadoPerdidasGananciasChanged(EstadoPerdidasGananciasDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(EtiquetasPerdidasGanancias));
        OnPropertyChanged(nameof(FilasPerdidasGanancias));
        OnPropertyChanged(nameof(TieneEstadoPerdidasGanancias));
    }

    partial void OnFlujoFondosChanged(FlujoFondosDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(EtiquetasFlujoFondos));
        OnPropertyChanged(nameof(FilasFlujoFondos));
        OnPropertyChanged(nameof(TieneFlujoFondos));
    }

    partial void OnIndicadoresFinancierosChanged(IndicadoresFinancierosDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(DetalleVanFinanciero));
        OnPropertyChanged(nameof(TieneIndicadoresFinancieros));
    }

    partial void OnPeriodoRecuperacionChanged(PeriodoRecuperacionDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(DetallePeriodoRecuperacion));
        OnPropertyChanged(nameof(TienePeriodoRecuperacion));
    }

    partial void OnPuntoEquilibrioChanged(PuntoEquilibrioDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(DetallePuntoEquilibrio));
        OnPropertyChanged(nameof(TienePuntoEquilibrio));
    }

    partial void OnArancelOptimoBiseccionChanged(ArancelOptimoBiseccionDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(IteracionesArancelOptimo));
        OnPropertyChanged(nameof(DetalleArancelOptimo));
        OnPropertyChanged(nameof(TieneArancelOptimoBiseccion));
        OnPropertyChanged(nameof(PuedeUsarArancelOptimo));
    }

    partial void OnEstaCargandoChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeUsarArancelOptimo));
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando)
            return;

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repoCarreras = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var carreras = await repoCarreras.ListarAsync();
            var carreraActualId = CarreraSeleccionada?.Id;

            _suprimirCambios = true;
            try
            {
                Carreras = new ObservableCollection<Carrera>(carreras);
                CarreraSeleccionada = Carreras.FirstOrDefault(c => c.Id == carreraActualId)
                    ?? Carreras.FirstOrDefault();
            }
            finally
            {
                _suprimirCambios = false;
            }

            if (CarreraSeleccionada is null)
            {
                LimpiarResultados();
                MensajeError = "No hay carreras registradas.";
                return;
            }

            await RecargarEscenariosAsync();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private async Task RecargarEscenariosAsync()
    {
        if (CarreraSeleccionada is null)
        {
            Escenarios = [];
            EscenarioSeleccionado = null;
            LimpiarResultados();
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repoEscenario = scope.ServiceProvider.GetRequiredService<IRepositorioEscenarioProyeccion>();
            var repoProyeccion = scope.ServiceProvider.GetRequiredService<IRepositorioProyeccionEstudiantes>();
            var escenarios = await repoEscenario.ListarAsync();
            var proyecciones = await repoProyeccion.ListarResumenAsync(CarreraSeleccionada.Id);
            var escenariosConProyeccion = proyecciones.Select(p => p.EscenarioProyeccionId).ToHashSet();
            var escenarioActualId = EscenarioSeleccionado?.Id;

            var opciones = escenarios
                .Where(e => e.CarreraId == CarreraSeleccionada.Id && escenariosConProyeccion.Contains(e.Id))
                .Select(e => new EscenarioAnalisisFinancieroOpcion
                {
                    Id = e.Id,
                    CarreraId = e.CarreraId,
                    Nombre = e.Nombre,
                    EsPredeterminado = e.EsPredeterminado,
                    TieneProyeccion = true
                })
                .OrderByDescending(e => e.EsPredeterminado)
                .ThenBy(e => e.Nombre)
                .ToList();

            _suprimirCambios = true;
            try
            {
                Escenarios = new ObservableCollection<EscenarioAnalisisFinancieroOpcion>(opciones);
                EscenarioSeleccionado = Escenarios.FirstOrDefault(e => e.Id == escenarioActualId)
                    ?? Escenarios.FirstOrDefault();
            }
            finally
            {
                _suprimirCambios = false;
            }

            if (EscenarioSeleccionado is null)
            {
                LimpiarResultados();
                MensajeError = "La carrera no tiene escenarios con proyección de estudiantes. Genera la proyección en Proyección de Estudiantes.";
                return;
            }

            await RefrescarAsync();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
    }

    [RelayCommand]
    private async Task RefrescarAsync()
    {
        if (CarreraSeleccionada is null)
        {
            LimpiarResultados();
            MensajeError = "Selecciona una carrera.";
            return;
        }

        if (EscenarioSeleccionado is null)
        {
            LimpiarResultados();
            MensajeError = "Selecciona un escenario.";
            return;
        }

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryEstado = scope.ServiceProvider.GetRequiredService<ObtenerEstadoPerdidasGananciasQuery>();
            var queryFlujo = scope.ServiceProvider.GetRequiredService<ObtenerFlujoFondosQuery>();
            var queryIndicadores = scope.ServiceProvider.GetRequiredService<ObtenerIndicadoresFinancierosQuery>();
            var queryPeriodoRecuperacion = scope.ServiceProvider.GetRequiredService<ObtenerPeriodoRecuperacionQuery>();
            var queryPuntoEquilibrio = scope.ServiceProvider.GetRequiredService<ObtenerPuntoEquilibrioQuery>();
            var queryArancelOptimo = scope.ServiceProvider.GetRequiredService<ObtenerArancelOptimoBiseccionQuery>();

            EstadoPerdidasGanancias = await queryEstado.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);
            FlujoFondos = await queryFlujo.EjecutarAsync(
                CarreraSeleccionada.Id,
                EscenarioSeleccionado.Id,
                estadoPrecalculado: EstadoPerdidasGanancias);
            IndicadoresFinancieros = await queryIndicadores.EjecutarAsync(
                CarreraSeleccionada.Id,
                EscenarioSeleccionado.Id,
                flujoPrecalculado: FlujoFondos);
            PeriodoRecuperacion = await queryPeriodoRecuperacion.EjecutarAsync(
                CarreraSeleccionada.Id,
                EscenarioSeleccionado.Id,
                flujoPrecalculado: FlujoFondos);
            PuntoEquilibrio = await queryPuntoEquilibrio.EjecutarAsync(
                CarreraSeleccionada.Id,
                EscenarioSeleccionado.Id,
                estadoPrecalculado: EstadoPerdidasGanancias);
            ArancelOptimoBiseccion = await queryArancelOptimo.EjecutarAsync(
                CarreraSeleccionada.Id,
                EscenarioSeleccionado.Id);

            var advertencias = new[]
            {
                EstadoPerdidasGanancias.MensajeAdvertencia,
                FlujoFondos.MensajeAdvertencia,
                IndicadoresFinancieros.MensajeAdvertencia,
                PeriodoRecuperacion.MensajeAdvertencia,
                PuntoEquilibrio.MensajeAdvertencia,
                ArancelOptimoBiseccion.MensajeAdvertencia
            }
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

            if (advertencias.Count == 0)
            {
                MensajeExito = "Análisis financiero calculado correctamente.";
            }
            else
            {
                MensajeError = string.Join(Environment.NewLine, advertencias);
            }
        }
        catch (Exception ex)
        {
            LimpiarResultados();
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task UsarArancelOptimoAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tienes permiso para editar.";
            return;
        }

        if (CarreraSeleccionada is null || EscenarioSeleccionado is null)
        {
            MensajeError = "Selecciona carrera y escenario.";
            return;
        }

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryArancelOptimo = scope.ServiceProvider.GetRequiredService<ObtenerArancelOptimoBiseccionQuery>();
            var optimo = ArancelOptimoBiseccion?.Disponible == true
                ? ArancelOptimoBiseccion
                : await queryArancelOptimo.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);

            if (!optimo.Disponible || optimo.ArancelOptimo <= 0m)
            {
                MensajeError = optimo.MensajeAdvertencia ?? "No hay arancel óptimo disponible para aplicar.";
                return;
            }

            var listarConfigs = scope.ServiceProvider.GetRequiredService<ListarConfiguracionesArancelCarreraQuery>();
            var configuraciones = await listarConfigs.EjecutarAsync(CarreraSeleccionada.Id);
            var configExacta = configuraciones.FirstOrDefault(c => c.EscenarioProyeccionId == EscenarioSeleccionado.Id);
            var usaPorcentajeInstitucional = configExacta?.UsaPorcentajeMatriculaInstitucional ?? true;

            var dto = new GuardarConfiguracionArancelCarreraDto
            {
                Id = configExacta?.Id,
                CarreraId = CarreraSeleccionada.Id,
                EscenarioProyeccionId = EscenarioSeleccionado.Id,
                ModoCalculoArancel = "Manual",
                ArancelManual = optimo.ArancelOptimo,
                UsaPorcentajeMatriculaInstitucional = usaPorcentajeInstitucional,
                PorcentajeMatricula = usaPorcentajeInstitucional
                    ? null
                    : configExacta?.PorcentajeMatricula
            };

            var command = scope.ServiceProvider.GetRequiredService<GuardarConfiguracionArancelCarreraCommand>();
            await command.EjecutarAsync(dto, _sesionActual.UsuarioId);

            await RefrescarAsync();
            MensajeExito = "Arancel óptimo aplicado como configuración manual del escenario.";
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void LimpiarResultados()
    {
        EstadoPerdidasGanancias = null;
        FlujoFondos = null;
        IndicadoresFinancieros = null;
        PeriodoRecuperacion = null;
        PuntoEquilibrio = null;
        ArancelOptimoBiseccion = null;
    }

    private static string Detalle(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}
