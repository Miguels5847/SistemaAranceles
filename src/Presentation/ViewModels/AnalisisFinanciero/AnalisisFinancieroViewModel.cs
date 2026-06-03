using System.IO;
using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.AnalisisFinanciero;
using SistemaAranceles.Application.UseCases.CapitalTrabajo;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
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

public sealed class MatrizAnalisisFinancieroFila
{
    public string Concepto { get; init; } = string.Empty;
    public IReadOnlyList<string> ValoresDisplay { get; init; } = [];
    public string TipoFila { get; init; } = "detalle";
    public bool EsTotal => string.Equals(TipoFila, "total", StringComparison.OrdinalIgnoreCase);
    public bool EsResultado => string.Equals(TipoFila, "resultado", StringComparison.OrdinalIgnoreCase);
}

public sealed class ParametroAnalisisFinancieroFila
{
    public string Concepto { get; init; } = string.Empty;
    public string ValorDisplay { get; init; } = string.Empty;
    public bool EsTotal { get; init; }
}

public sealed partial class AnalisisFinancieroViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private readonly FactorImprevistoCostosGastosState _factorImprevistoState;
    private bool _suprimirCambios;

    public AnalisisFinancieroViewModel(
        IServiceProvider serviceProvider,
        SesionActual sesionActual,
        FactorImprevistoCostosGastosState factorImprevistoState)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
        _factorImprevistoState = factorImprevistoState;
        FactorImprevisto = _factorImprevistoState.FactorImprevisto;
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
    [ObservableProperty] private DashboardFinancieroDto? _dashboardFinanciero;
    [ObservableProperty] private decimal _factorImprevisto = FactorImprevistoCostosGastosState.FactorPorDefecto;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeAdvertencia = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private bool _estaCargando;

    public IReadOnlyList<string> EtiquetasPerdidasGanancias => EstadoPerdidasGanancias?.EtiquetasPeriodos ?? [];
    public IReadOnlyList<EstadoPerdidasGananciasRubroDto> FilasPerdidasGanancias => EstadoPerdidasGanancias?.Filas ?? [];
    public bool TieneEstadoPerdidasGanancias => EstadoPerdidasGanancias?.TieneDatos == true;
    public IReadOnlyList<string> EtiquetasFlujoFondos => FlujoFondos?.EtiquetasPeriodos ?? [];
    public IReadOnlyList<FlujoFondosRubroDto> FilasFlujoFondos => FlujoFondos?.Filas ?? [];
    public bool TieneFlujoFondos => FlujoFondos?.TieneDatos == true;
    public IReadOnlyList<string> EtiquetasTirVan => FlujoFondos?.ValoresPorPeriodo
        .Select(p => p.PeriodoOrden == 0 ? "0" : p.Anio.ToString())
        .ToList() ?? [];
    public IReadOnlyList<MatrizAnalisisFinancieroFila> FilasFlujoTir => ConstruirFilasFlujoTirVan();
    public IReadOnlyList<MatrizAnalisisFinancieroFila> FilasFlujoVan => ConstruirFilasFlujoTirVan();
    public IReadOnlyList<ParametroAnalisisFinancieroFila> FilasTasaMinimaRendimiento => ConstruirFilasTasaMinimaRendimiento();
    public IReadOnlyList<IndicadorVanPeriodoDto> DetalleVanFinanciero => IndicadoresFinancieros?.DetalleVan ?? [];
    public bool TieneIndicadoresFinancieros => IndicadoresFinancieros?.TieneDatos == true;
    public IReadOnlyList<PeriodoRecuperacionDetalleDto> DetallePeriodoRecuperacion => PeriodoRecuperacion?.Detalle ?? [];
    public bool TienePeriodoRecuperacion => PeriodoRecuperacion?.TieneDatos == true;
    public IReadOnlyList<PuntoEquilibrioPeriodoDto> DetallePuntoEquilibrio => PuntoEquilibrio?.Periodos ?? [];
    public IReadOnlyList<PuntoEquilibrioResultadoFilaDto> ProyeccionResultadosPuntoEquilibrio => PuntoEquilibrio?.ProyeccionResultados ?? [];
    public IReadOnlyList<PuntoEquilibrioAnalisisFilaDto> AnalisisPuntoEquilibrio => PuntoEquilibrio?.AnalisisPuntoEquilibrio ?? [];
    public bool TienePuntoEquilibrio => PuntoEquilibrio?.TieneDatos == true;
    public IReadOnlyList<ArancelOptimoBiseccionIteracionDto> IteracionesArancelOptimo => ArancelOptimoBiseccion?.Iteraciones ?? [];
    public IReadOnlyList<ArancelOptimoBiseccionPeriodoDto> DetalleArancelOptimo => ArancelOptimoBiseccion?.Periodos ?? [];
    public bool TieneArancelOptimoBiseccion => ArancelOptimoBiseccion?.TieneDatos == true;
    public IReadOnlyList<DashboardIndicadorFinancieroDto> IndicadoresDashboard => DashboardFinanciero?.Indicadores ?? [];
    public IReadOnlyList<DashboardRecomendacionFinancieraDto> RecomendacionesDashboard => DashboardFinanciero?.Recomendaciones ?? [];
    public bool TieneDashboardFinanciero => DashboardFinanciero?.TieneDatos == true;
    public bool PuedeUsarArancelOptimo => PuedeEditar
                                           && !EstaCargando
                                           && ArancelOptimoBiseccion?.Disponible == true
                                           && CarreraSeleccionada is not null
                                           && EscenarioSeleccionado is not null;
    public bool PuedeVerExportarDashboard => _sesionActual.EsAdministrador;
    public bool PuedeExportarDashboard => PuedeVerExportarDashboard
                                          && !EstaCargando
                                          && DashboardFinanciero?.TieneDatos == true;
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
        OnPropertyChanged(nameof(EtiquetasTirVan));
        OnPropertyChanged(nameof(FilasFlujoTir));
        OnPropertyChanged(nameof(FilasFlujoVan));
    }

    partial void OnIndicadoresFinancierosChanged(IndicadoresFinancierosDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(DetalleVanFinanciero));
        OnPropertyChanged(nameof(TieneIndicadoresFinancieros));
        OnPropertyChanged(nameof(FilasTasaMinimaRendimiento));
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
        OnPropertyChanged(nameof(ProyeccionResultadosPuntoEquilibrio));
        OnPropertyChanged(nameof(AnalisisPuntoEquilibrio));
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

    partial void OnDashboardFinancieroChanged(DashboardFinancieroDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(IndicadoresDashboard));
        OnPropertyChanged(nameof(RecomendacionesDashboard));
        OnPropertyChanged(nameof(TieneDashboardFinanciero));
        OnPropertyChanged(nameof(PuedeExportarDashboard));
    }

    partial void OnFactorImprevistoChanged(decimal value)
    {
        if (value > 0m)
            _factorImprevistoState.Establecer(value);
    }

    partial void OnEstaCargandoChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeUsarArancelOptimo));
        OnPropertyChanged(nameof(PuedeExportarDashboard));
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando)
            return;

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeAdvertencia = string.Empty;
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
            MensajeAdvertencia = string.Empty;
            MensajeError = "Selecciona una carrera.";
            return;
        }

        if (EscenarioSeleccionado is null)
        {
            LimpiarResultados();
            MensajeAdvertencia = string.Empty;
            MensajeError = "Selecciona un escenario.";
            return;
        }

        if (FactorImprevisto <= 0m)
        {
            LimpiarResultados();
            MensajeAdvertencia = string.Empty;
            MensajeExito = string.Empty;
            MensajeError = "El factor imprevisto debe ser mayor a 0.";
            return;
        }

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeAdvertencia = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryDemanda = scope.ServiceProvider.GetRequiredService<ObtenerDemandaProyectadaQuery>();
            var queryMatriz = scope.ServiceProvider.GetRequiredService<ObtenerMatrizCostosGastosQuery>();
            var queryEstado = scope.ServiceProvider.GetRequiredService<ObtenerEstadoPerdidasGananciasQuery>();
            var queryFlujo = scope.ServiceProvider.GetRequiredService<ObtenerFlujoFondosQuery>();
            var queryIndicadores = scope.ServiceProvider.GetRequiredService<ObtenerIndicadoresFinancierosQuery>();
            var queryPeriodoRecuperacion = scope.ServiceProvider.GetRequiredService<ObtenerPeriodoRecuperacionQuery>();
            var queryPuntoEquilibrio = scope.ServiceProvider.GetRequiredService<ObtenerPuntoEquilibrioQuery>();
            var queryArancelOptimo = scope.ServiceProvider.GetRequiredService<ObtenerArancelOptimoBiseccionQuery>();
            var queryDashboard = scope.ServiceProvider.GetRequiredService<ObtenerDashboardFinancieroQuery>();
            var queryInversiones = scope.ServiceProvider.GetRequiredService<ObtenerMatrizInversionesQuery>();
            var queryCapitalTrabajo = scope.ServiceProvider.GetRequiredService<ObtenerResumenCapitalTrabajoQuery>();

            var carreraId = CarreraSeleccionada.Id;
            var escenarioId = EscenarioSeleccionado.Id;
            var factorImprevisto = FactorImprevisto;
            _factorImprevistoState.Establecer(factorImprevisto);

            // Calcula una sola vez la demanda y la matriz de Costos y Gastos (lo más pesado) y las
            // propaga a cada query que las acepta, evitando recomputarlas 3 veces por refresco.
            var demanda = await queryDemanda.EjecutarAsync(carreraId, escenarioId);
            var matriz = await queryMatriz.EjecutarAsync(
                carreraId,
                escenarioId,
                demandaPrecalculada: demanda,
                factorImprevisto: factorImprevisto);

            // Inversiones y capital de trabajo se recalculaban ~5x y ~4x por refresco (Flujo,
            // ArancelOptimo y sus inversión inicial/depreciación). Se calculan una vez y se propagan.
            var hayMatriz = matriz.TieneDatos && matriz.ValoresPorPeriodo.Count > 0;
            var inversiones = hayMatriz ? await queryInversiones.EjecutarAsync(carreraId, escenarioId, null) : null;
            var capitalTrabajo = hayMatriz ? await queryCapitalTrabajo.EjecutarAsync(carreraId, escenarioId) : null;

            EstadoPerdidasGanancias = await queryEstado.EjecutarAsync(
                carreraId,
                escenarioId,
                costosPrecalculados: matriz);
            FlujoFondos = await queryFlujo.EjecutarAsync(
                carreraId,
                escenarioId,
                estadoPrecalculado: EstadoPerdidasGanancias,
                inversionesPrecalculada: inversiones,
                capitalTrabajoPrecalculado: capitalTrabajo);
            IndicadoresFinancieros = await queryIndicadores.EjecutarAsync(
                carreraId,
                escenarioId,
                flujoPrecalculado: FlujoFondos,
                factorImprevisto: factorImprevisto);
            PeriodoRecuperacion = await queryPeriodoRecuperacion.EjecutarAsync(
                carreraId,
                escenarioId,
                flujoPrecalculado: FlujoFondos,
                factorImprevisto: factorImprevisto);
            PuntoEquilibrio = await queryPuntoEquilibrio.EjecutarAsync(
                carreraId,
                escenarioId,
                estadoPrecalculado: EstadoPerdidasGanancias,
                costosPrecalculados: matriz,
                demandaPrecalculada: demanda,
                factorImprevisto: factorImprevisto);
            ArancelOptimoBiseccion = await queryArancelOptimo.EjecutarAsync(
                carreraId,
                escenarioId,
                costosPrecalculados: matriz,
                demandaPrecalculada: demanda,
                inversionesPrecalculada: inversiones,
                capitalTrabajoPrecalculado: capitalTrabajo,
                factorImprevisto: factorImprevisto);
            DashboardFinanciero = await queryDashboard.EjecutarAsync(
                carreraId,
                escenarioId,
                estadoPrecalculado: EstadoPerdidasGanancias,
                flujoPrecalculado: FlujoFondos,
                indicadoresPrecalculados: IndicadoresFinancieros,
                periodoRecuperacionPrecalculado: PeriodoRecuperacion,
                puntoEquilibrioPrecalculado: PuntoEquilibrio,
                arancelOptimoPrecalculado: ArancelOptimoBiseccion,
                factorImprevisto: factorImprevisto);

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
            .Select(m => m!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

            var advertenciasSuaves = advertencias
                .Where(EsAdvertenciaSuave)
                .ToList();
            var erroresCriticos = advertencias
                .Where(m => !EsAdvertenciaSuave(m))
                .ToList();

            if (advertenciasSuaves.Count > 0)
                MensajeAdvertencia = string.Join(Environment.NewLine, advertenciasSuaves);

            if (erroresCriticos.Count == 0)
            {
                MensajeExito = "Análisis financiero calculado correctamente.";
            }
            else
            {
                MensajeError = string.Join(Environment.NewLine, erroresCriticos);
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
    private async Task ExportarDashboardAsync()
    {
        if (!PuedeVerExportarDashboard)
        {
            MensajeAdvertencia = string.Empty;
            MensajeError = "Solo un administrador puede exportar el dashboard.";
            return;
        }

        if (DashboardFinanciero is null || !DashboardFinanciero.TieneDatos)
        {
            MensajeAdvertencia = string.Empty;
            MensajeError = "No hay dashboard financiero para exportar.";
            return;
        }

        var nombreCarrera = NormalizarNombreArchivo(DashboardFinanciero.CarreraNombre);
        var nombreEscenario = NormalizarNombreArchivo(DashboardFinanciero.EscenarioNombre);
        var dialog = new SaveFileDialog
        {
            Title = "Exportar Dashboard Financiero",
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = $"dashboard_financiero_{nombreCarrera}_{nombreEscenario}_{DateTime.Now:yyyyMMddHHmm}.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            await File.WriteAllTextAsync(dialog.FileName, ConstruirCsvDashboard(DashboardFinanciero), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            MensajeError = string.Empty;
            MensajeAdvertencia = string.Empty;
            MensajeExito = "Dashboard financiero exportado correctamente.";
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
    }

    [RelayCommand]
    private async Task UsarArancelOptimoAsync()
    {
        if (!PuedeEditar)
        {
            MensajeAdvertencia = string.Empty;
            MensajeError = "No tienes permiso para editar.";
            return;
        }

        if (CarreraSeleccionada is null || EscenarioSeleccionado is null)
        {
            MensajeAdvertencia = string.Empty;
            MensajeError = "Selecciona carrera y escenario.";
            return;
        }

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeAdvertencia = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryArancelOptimo = scope.ServiceProvider.GetRequiredService<ObtenerArancelOptimoBiseccionQuery>();
            var optimo = ArancelOptimoBiseccion?.Disponible == true
                ? ArancelOptimoBiseccion
                : await queryArancelOptimo.EjecutarAsync(
                    CarreraSeleccionada.Id,
                    EscenarioSeleccionado.Id,
                    factorImprevisto: FactorImprevisto);

            if (!optimo.Disponible || optimo.ArancelOptimo <= 0m)
            {
                MensajeAdvertencia = string.Empty;
                MensajeError = optimo.MensajeAdvertencia ?? "No hay arancel óptimo disponible para aplicar.";
                return;
            }

            var listarConfigs = scope.ServiceProvider.GetRequiredService<ListarConfiguracionesArancelCarreraQuery>();
            var configuraciones = await listarConfigs.EjecutarAsync(CarreraSeleccionada.Id);
            var configExacta = configuraciones.FirstOrDefault(c => c.EscenarioProyeccionId == EscenarioSeleccionado.Id);
            var usaPorcentajeInstitucional = configExacta?.UsaPorcentajeMatriculaInstitucional ?? true;

            // El arancel financiero sugerido (VAN=0) se confirma como arancel vigente. Se persiste
            // como "Manual" porque queda como valor fijo del escenario; el valor fue calculado por
            // bisección y confirmado explícitamente por el usuario con este botón (no es automático).
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
            MensajeExito = "Arancel financiero sugerido aplicado como arancel vigente del escenario.";
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
        DashboardFinanciero = null;
    }

    private static string ConstruirCsvDashboard(DashboardFinancieroDto dashboard)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Seccion;Campo;Valor;Detalle;Estado");
        sb.AppendLine($"Resumen;Carrera;{Csv(dashboard.CarreraNombre)};;");
        sb.AppendLine($"Resumen;Escenario;{Csv(dashboard.EscenarioNombre)};;");
        sb.AppendLine($"Resumen;Estado general;{Csv(dashboard.EstadoGeneral)};{Csv(dashboard.ViabilidadDisplay)};");
        sb.AppendLine($"Resumen;Fecha calculo;{Csv(dashboard.FechaCalculoDisplay)};;");
        sb.AppendLine($"Resumen;Ingresos;{Csv(dashboard.TotalIngresosDisplay)};;");
        sb.AppendLine($"Resumen;Costos y gastos;{Csv(dashboard.TotalCostosGastosDisplay)};;");
        sb.AppendLine($"Resumen;Utilidad o perdida;{Csv(dashboard.UtilidadPerdidaDisplay)};;");
        sb.AppendLine($"Resumen;Flujo acumulado final;{Csv(dashboard.FlujoAcumuladoFinalDisplay)};;");
        sb.AppendLine($"Resumen;VAN;{Csv(dashboard.VanDisplay)};;");
        sb.AppendLine($"Resumen;TIR;{Csv(dashboard.TirDisplay)};;");
        sb.AppendLine($"Resumen;TMR;{Csv(dashboard.TmrDisplay)};;");
        sb.AppendLine($"Resumen;Arancel optimo;{Csv(dashboard.ArancelOptimoDisplay)};;");

        foreach (var indicador in dashboard.Indicadores)
            sb.AppendLine($"Indicador;{Csv(indicador.Nombre)};{Csv(indicador.ValorDisplay)};{Csv(indicador.Detalle)};{Csv(indicador.Estado)}");

        foreach (var recomendacion in dashboard.Recomendaciones)
            sb.AppendLine($"Recomendacion;{Csv(recomendacion.Origen)};{Csv(recomendacion.Prioridad)};{Csv(recomendacion.Mensaje)};");

        return sb.ToString();
    }

    private static string Csv(string? value)
        => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";

    private IReadOnlyList<MatrizAnalisisFinancieroFila> ConstruirFilasFlujoTirVan()
    {
        var valores = FlujoFondos?.ValoresPorPeriodo
            .Select(p => FormatoMatrizAnalisisFinanciero.Formatear(p.FlujoNeto, FormatoMatrizAnalisisFinanciero.Moneda))
            .ToList() ?? [];

        return valores.Count == 0
            ? []
            :
            [
                new MatrizAnalisisFinancieroFila
                {
                    Concepto = "Flujo de Fondos Neto",
                    ValoresDisplay = valores,
                    TipoFila = "resultado"
                }
            ];
    }

    private IReadOnlyList<ParametroAnalisisFinancieroFila> ConstruirFilasTasaMinimaRendimiento()
    {
        if (IndicadoresFinancieros is null)
            return [];

        return
        [
            new ParametroAnalisisFinancieroFila
            {
                Concepto = "Tasa de interés",
                ValorDisplay = IndicadoresFinancieros.TasaInteresFinancieraDisplay
            },
            new ParametroAnalisisFinancieroFila
            {
                Concepto = "Inflación anual / promedio",
                ValorDisplay = IndicadoresFinancieros.InflacionPromedioDisplay
            },
            new ParametroAnalisisFinancieroFila
            {
                Concepto = "Premio al riesgo",
                ValorDisplay = IndicadoresFinancieros.PremioRiesgoDisplay
            },
            new ParametroAnalisisFinancieroFila
            {
                Concepto = "Tasa mínima de rendimiento",
                ValorDisplay = IndicadoresFinancieros.TmrDisplay,
                EsTotal = true
            }
        ];
    }

    private static bool EsAdvertenciaSuave(string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return false;

        var patrones = new[]
        {
            "múltiples cambios de signo",
            "multiples cambios de signo",
            "TIR puede no ser única",
            "TIR puede no ser unica",
            "TIR posiblemente no única",
            "TIR posiblemente no unica",
            "VAN cercano a 0",
            "máximo de iteraciones",
            "maximo de iteraciones",
            "mejor aproximación",
            "mejor aproximacion",
            "redondeo monetario",
            "inversiones futuras",
            "inflación promedio se tomó como 0%",
            "inflacion promedio se tomo como 0%",
            "No recuperado dentro del horizonte proyectado",
            "recuperación estable",
            "recuperacion estable",
            "vuelve a ser negativo",
            "periodos sin margen positivo",
            "periodos no calculables"
        };

        return patrones.Any(p => mensaje.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizarNombreArchivo(string value)
    {
        var invalido = Path.GetInvalidFileNameChars().ToHashSet();
        var normalizado = new string(value.Select(c => invalido.Contains(c) || char.IsWhiteSpace(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(normalizado) ? "sin_nombre" : normalizado;
    }

    private static string Detalle(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}
