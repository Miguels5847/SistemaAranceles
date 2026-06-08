using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.AnalisisFinanciero;
using SistemaAranceles.Application.UseCases.CapitalTrabajo;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;
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
    // Simulación de deserción editable (defaults = constantes del cálculo). Solo recalcula las dos
    // filas de estudiantes/ciclo con deserción; no re-ejecuta el pipeline ni afecta otros valores.
    [ObservableProperty] private decimal _tasaDesercionAltaPe = 35m;
    [ObservableProperty] private decimal _tasaDesercionMediaPe = 17.5m;
    [ObservableProperty] private ArancelOptimoBiseccionDto? _arancelOptimoBiseccion;
    [ObservableProperty] private DashboardFinancieroDto? _dashboardFinanciero;
    [ObservableProperty] private CesDto? _ces;
    [ObservableProperty] private string _costoCarrerasSimilares = string.Empty;
    [ObservableProperty] private decimal _factorImprevisto = FactorImprevistoCostosGastosState.FactorPorDefecto;
    [ObservableProperty] private ArancelEfectivoDto? _arancelVigente;
    [ObservableProperty] private CostoCarreraResultadoDto? _arancelReferencial;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeAdvertencia = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private string _mensajeInfo = string.Empty;
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
    public string PeriodoBasePuntoEquilibrio => PuntoEquilibrio?.PeriodoBaseEtiqueta ?? string.Empty;
    public decimal EstudiantesPorCicloBasePe => PuntoEquilibrio?.EstudiantesPorCicloBase ?? 0m;
    public bool TieneSimulacionDesercion => TienePuntoEquilibrio && EstudiantesPorCicloBasePe > 0m;
    public string EstudiantesPorCicloBaseDisplay => EstudiantesPorCicloBasePe.ToString("N0");
    public string EstudiantesDesercionAltaDisplay => decimal.Round(EstudiantesPorCicloBasePe * (1m + TasaDesercionAltaPe / 100m), 2).ToString("N0");
    public string EstudiantesDesercionMediaDisplay => decimal.Round(EstudiantesPorCicloBasePe * (1m + TasaDesercionMediaPe / 100m), 2).ToString("N0");
    public IReadOnlyList<ArancelOptimoBiseccionIteracionDto> IteracionesArancelOptimo => ArancelOptimoBiseccion?.Iteraciones ?? [];
    public IReadOnlyList<ArancelOptimoBiseccionPeriodoDto> DetalleArancelOptimo => ArancelOptimoBiseccion?.Periodos ?? [];
    public bool TieneArancelOptimoBiseccion => ArancelOptimoBiseccion?.TieneDatos == true;
    public IReadOnlyList<DashboardIndicadorFinancieroDto> IndicadoresDashboard => DashboardFinanciero?.Indicadores ?? [];
    public IReadOnlyList<DashboardRecomendacionFinancieraDto> RecomendacionesDashboard => DashboardFinanciero?.Recomendaciones ?? [];
    public bool TieneDashboardFinanciero => DashboardFinanciero?.TieneDatos == true;
    public IReadOnlyList<CesInfFilaDto> InfCes => Ces?.InfCes ?? [];
    public IReadOnlyList<CesParametroFilaDto> ParametrosCes => Ces?.Parametros ?? [];
    public IReadOnlyList<CesDistribucionFilaDto> DistribucionCes => Ces?.Distribucion ?? [];
    public bool TieneCes => Ces?.TieneDatos == true;
    // Si el arancel vigente ya coincide con el óptimo (dentro de tolerancia), no tiene sentido
    // re-aplicar: evita la sensación de "iterar una y otra vez" cuando ya se está en el óptimo.
    private const decimal ToleranciaAplicacionArancel = 2m;
    public bool ArancelOptimoYaAplicado => DiferenciaArancel(out var dif) && Math.Abs(dif) <= ToleranciaAplicacionArancel;
    public string TextoBotonArancelOptimo => ArancelOptimoYaAplicado
        ? "Arancel óptimo ya aplicado"
        : "Usar arancel óptimo";

    public bool PuedeUsarArancelOptimo => PuedeEditar
                                           && !EstaCargando
                                           && ArancelOptimoBiseccion?.Disponible == true
                                           && CarreraSeleccionada is not null
                                           && EscenarioSeleccionado is not null
                                           && !ArancelOptimoYaAplicado;
    public bool PuedeTrabajar => CarreraSeleccionada is not null && !EstaCargando;
    public bool PuedeEditar => _sesionActual.EsAdministrador || _sesionActual.TienePermiso("DI_NG.EDITAR");

    // Comparativa arancel vigente vs propuesto (Fase 6).
    public bool TieneComparativaArancel => ArancelVigente is not null
                                           || ArancelReferencial is not null
                                           || ArancelOptimoBiseccion?.Disponible == true;
    public string ArancelVigenteDisplay => ArancelVigente?.ArancelDisplay ?? "—";
    public string MatriculaVigenteDisplay => ArancelVigente?.MatriculaDisplay ?? "—";
    public string TotalVigenteDisplay => ArancelVigente is { ArancelEfectivo: > 0m } v
        ? FormatoMatrizAnalisisFinanciero.FormatearMoneda((v.ArancelEfectivo ?? 0m) + v.MatriculaEfectiva)
        : "—";
    public string ArancelReferencialDisplay => ArancelReferencial?.TieneDatos == true
        ? ArancelReferencial.ArancelSugeridoDisplay
        : "—";
    public string TotalReferencialDisplay => ArancelReferencial?.TieneDatos == true
        ? ArancelReferencial.TotalPorSemestreDisplay
        : "—";
    public string ArancelOptimoComparativaDisplay => ArancelOptimoBiseccion?.Disponible == true
        ? ArancelOptimoBiseccion.ArancelOptimoDisplay
        : "—";
    public string TotalOptimoComparativaDisplay => ArancelOptimoBiseccion?.Disponible == true
        ? FormatoMatrizAnalisisFinanciero.FormatearMoneda(ArancelOptimoBiseccion.TotalPorSemestre)
        : "—";
    public string DiferenciaArancelDisplay => DiferenciaArancel(out var dif)
        ? FormatoMatrizAnalisisFinanciero.FormatearMoneda(dif)
        : "—";
    public string DiferenciaPorcentajeDisplay
    {
        get
        {
            var vigente = ArancelVigente?.ArancelEfectivo ?? 0m;
            return DiferenciaArancel(out var dif) && vigente > 0m
                ? $"{decimal.Round(dif / vigente * 100m, 1):N1}%"
                : "—";
        }
    }

    private bool DiferenciaArancel(out decimal diferencia)
    {
        diferencia = 0m;
        var vigente = ArancelVigente?.ArancelEfectivo ?? 0m;
        var optimo = ArancelOptimoBiseccion?.Disponible == true ? ArancelOptimoBiseccion.ArancelOptimo : 0m;
        if (vigente <= 0m || optimo <= 0m)
            return false;
        diferencia = decimal.Round(optimo - vigente, 2);
        return true;
    }

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeTrabajar));
        OnPropertyChanged(nameof(PuedeUsarArancelOptimo));
        if (_suprimirCambios || EstaCargando)
            return;

        _ = RecargarEscenariosAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioAnalisisFinancieroOpcion? value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeUsarArancelOptimo));
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
        OnPropertyChanged(nameof(PeriodoBasePuntoEquilibrio));
        OnPropertyChanged(nameof(EstudiantesPorCicloBasePe));
        OnPropertyChanged(nameof(TieneSimulacionDesercion));
        OnPropertyChanged(nameof(EstudiantesPorCicloBaseDisplay));
        OnPropertyChanged(nameof(EstudiantesDesercionAltaDisplay));
        OnPropertyChanged(nameof(EstudiantesDesercionMediaDisplay));
    }

    partial void OnTasaDesercionAltaPeChanged(decimal value)
    {
        _ = value;
        OnPropertyChanged(nameof(EstudiantesDesercionAltaDisplay));
    }

    partial void OnTasaDesercionMediaPeChanged(decimal value)
    {
        _ = value;
        OnPropertyChanged(nameof(EstudiantesDesercionMediaDisplay));
    }

    partial void OnArancelOptimoBiseccionChanged(ArancelOptimoBiseccionDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(IteracionesArancelOptimo));
        OnPropertyChanged(nameof(DetalleArancelOptimo));
        OnPropertyChanged(nameof(TieneArancelOptimoBiseccion));
        OnPropertyChanged(nameof(PuedeUsarArancelOptimo));
        NotificarComparativaArancel();
    }

    partial void OnArancelVigenteChanged(ArancelEfectivoDto? value)
    {
        _ = value;
        NotificarComparativaArancel();
    }

    partial void OnArancelReferencialChanged(CostoCarreraResultadoDto? value)
    {
        _ = value;
        NotificarComparativaArancel();
    }

    private void NotificarComparativaArancel()
    {
        OnPropertyChanged(nameof(TieneComparativaArancel));
        OnPropertyChanged(nameof(ArancelVigenteDisplay));
        OnPropertyChanged(nameof(MatriculaVigenteDisplay));
        OnPropertyChanged(nameof(TotalVigenteDisplay));
        OnPropertyChanged(nameof(ArancelReferencialDisplay));
        OnPropertyChanged(nameof(TotalReferencialDisplay));
        OnPropertyChanged(nameof(ArancelOptimoComparativaDisplay));
        OnPropertyChanged(nameof(TotalOptimoComparativaDisplay));
        OnPropertyChanged(nameof(DiferenciaArancelDisplay));
        OnPropertyChanged(nameof(DiferenciaPorcentajeDisplay));
        OnPropertyChanged(nameof(ArancelOptimoYaAplicado));
        OnPropertyChanged(nameof(TextoBotonArancelOptimo));
        OnPropertyChanged(nameof(PuedeUsarArancelOptimo));
    }

    partial void OnDashboardFinancieroChanged(DashboardFinancieroDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(IndicadoresDashboard));
        OnPropertyChanged(nameof(RecomendacionesDashboard));
        OnPropertyChanged(nameof(TieneDashboardFinanciero));
    }

    partial void OnCesChanged(CesDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(InfCes));
        OnPropertyChanged(nameof(ParametrosCes));
        OnPropertyChanged(nameof(DistribucionCes));
        OnPropertyChanged(nameof(TieneCes));
    }

    partial void OnFactorImprevistoChanged(decimal value)
    {
        if (value > 0m)
            _factorImprevistoState.Establecer(value);
    }

    partial void OnEstaCargandoChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeTrabajar));
        OnPropertyChanged(nameof(PuedeUsarArancelOptimo));
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
        MensajeInfo = string.Empty;
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
                CarreraSeleccionada = carreraActualId is > 0
                    ? Carreras.FirstOrDefault(c => c.Id == carreraActualId.Value)
                    : null;
            }
            finally
            {
                _suprimirCambios = false;
            }

            if (CarreraSeleccionada is null)
            {
                LimpiarResultados();
                MensajeInfo = Carreras.Count == 0
                    ? "No hay carreras registradas."
                    : "Selecciona una carrera para cargar el Análisis Financiero.";
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
            MensajeInfo = "Selecciona una carrera para cargar el Análisis Financiero.";
            return;
        }

        try
        {
            MensajeInfo = string.Empty;
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
            MensajeError = string.Empty;
            MensajeExito = string.Empty;
            MensajeInfo = "Selecciona una carrera para refrescar la información.";
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
        MensajeInfo = string.Empty;
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
            var queryCes = scope.ServiceProvider.GetRequiredService<ObtenerCesQuery>();
            var queryInversiones = scope.ServiceProvider.GetRequiredService<ObtenerMatrizInversionesQuery>();
            var queryCapitalTrabajo = scope.ServiceProvider.GetRequiredService<ObtenerResumenCapitalTrabajoQuery>();
            var queryArancelEfectivo = scope.ServiceProvider.GetRequiredService<ObtenerArancelEfectivoQuery>();
            var queryCostoCarrera = scope.ServiceProvider.GetRequiredService<ObtenerCostoCarreraQuery>();
            var queryIngresos = scope.ServiceProvider.GetRequiredService<CalcularIngresosProyectadosQuery>();
            var repoProyeccion = scope.ServiceProvider.GetRequiredService<IRepositorioProyeccionEstudiantes>();

            var carreraId = CarreraSeleccionada.Id;
            var escenarioId = EscenarioSeleccionado.Id;
            var factorImprevisto = FactorImprevisto;
            _factorImprevistoState.Establecer(factorImprevisto);

            // Medición de tiempos por query (ventana Output del depurador). Confirma el efecto de las
            // optimizaciones; no afecta resultados.
            var swTotal = Stopwatch.StartNew();
            async Task<T> Medir<T>(string etiqueta, Func<Task<T>> factory)
            {
                var sw = Stopwatch.StartNew();
                var resultado = await factory();
                Debug.WriteLine($"AF: {etiqueta}={sw.ElapsedMilliseconds}ms");
                return resultado;
            }

            // La proyección de estudiantes (DTO pesado con todos los detalles) se cargaba en demanda,
            // matriz e ingresos. Se carga una sola vez y se propaga.
            var proyeccionId = await repoProyeccion.ObtenerIdPorCarreraYEscenarioAsync(carreraId, escenarioId);
            var proyeccion = proyeccionId is > 0
                ? await repoProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value)
                : null;

            // Calcula una sola vez la demanda y la matriz de Costos y Gastos (lo más pesado) y las
            // propaga a cada query que las acepta, evitando recomputarlas por refresco.
            var demanda = await Medir("demanda", () => queryDemanda.EjecutarAsync(
                carreraId,
                escenarioId,
                proyeccionPrecalculada: proyeccion));
            var matriz = await Medir("matriz", () => queryMatriz.EjecutarAsync(
                carreraId,
                escenarioId,
                demandaPrecalculada: demanda,
                factorImprevisto: factorImprevisto,
                proyeccionPrecalculada: proyeccion));

            // Inversiones y capital de trabajo se recalculaban ~5x y ~4x por refresco (Flujo,
            // ArancelOptimo y sus inversión inicial/depreciación). Se calculan una vez y se propagan.
            var hayMatriz = matriz.TieneDatos && matriz.ValoresPorPeriodo.Count > 0;
            var inversiones = hayMatriz ? await Medir("inversiones", () => queryInversiones.EjecutarAsync(carreraId, escenarioId, null)) : null;
            var capitalTrabajo = hayMatriz ? await Medir("capitalTrabajo", () => queryCapitalTrabajo.EjecutarAsync(carreraId, escenarioId)) : null;

            // Arancel vigente e Ingresos se calculan una vez y se propagan a P&G (antes P&G recomputaba
            // todo Ingresos, recargando proyección + arancel + descuentos).
            ArancelVigente = await Medir("arancelVigente", () => queryArancelEfectivo.EjecutarAsync(carreraId, escenarioId));
            var ingresos = await Medir("ingresos", () => queryIngresos.EjecutarAsync(
                carreraId,
                escenarioId,
                arancelPrecalculado: ArancelVigente,
                proyeccionPrecalculada: proyeccion));

            EstadoPerdidasGanancias = await Medir("estado", () => queryEstado.EjecutarAsync(
                carreraId,
                escenarioId,
                costosPrecalculados: matriz,
                ingresosPrecalculados: ingresos));
            FlujoFondos = await Medir("flujo", () => queryFlujo.EjecutarAsync(
                carreraId,
                escenarioId,
                estadoPrecalculado: EstadoPerdidasGanancias,
                inversionesPrecalculada: inversiones,
                capitalTrabajoPrecalculado: capitalTrabajo));
            IndicadoresFinancieros = await Medir("indicadores", () => queryIndicadores.EjecutarAsync(
                carreraId,
                escenarioId,
                flujoPrecalculado: FlujoFondos,
                factorImprevisto: factorImprevisto));
            PeriodoRecuperacion = await Medir("periodoRecuperacion", () => queryPeriodoRecuperacion.EjecutarAsync(
                carreraId,
                escenarioId,
                flujoPrecalculado: FlujoFondos,
                factorImprevisto: factorImprevisto));
            PuntoEquilibrio = await Medir("puntoEquilibrio", () => queryPuntoEquilibrio.EjecutarAsync(
                carreraId,
                escenarioId,
                estadoPrecalculado: EstadoPerdidasGanancias,
                costosPrecalculados: matriz,
                demandaPrecalculada: demanda,
                factorImprevisto: factorImprevisto));
            ArancelOptimoBiseccion = await Medir("arancelOptimo", () => queryArancelOptimo.EjecutarAsync(
                carreraId,
                escenarioId,
                costosPrecalculados: matriz,
                demandaPrecalculada: demanda,
                inversionesPrecalculada: inversiones,
                capitalTrabajoPrecalculado: capitalTrabajo,
                factorImprevisto: factorImprevisto));
            ArancelReferencial = await Medir("arancelReferencial", () => queryCostoCarrera.EjecutarAsync(
                carreraId,
                escenarioId,
                matrizPrecalculada: matriz,
                factorImprevisto: factorImprevisto));
            DashboardFinanciero = await Medir("dashboard", () => queryDashboard.EjecutarAsync(
                carreraId,
                escenarioId,
                estadoPrecalculado: EstadoPerdidasGanancias,
                flujoPrecalculado: FlujoFondos,
                indicadoresPrecalculados: IndicadoresFinancieros,
                periodoRecuperacionPrecalculado: PeriodoRecuperacion,
                puntoEquilibrioPrecalculado: PuntoEquilibrio,
                arancelOptimoPrecalculado: ArancelOptimoBiseccion,
                factorImprevisto: factorImprevisto));
            Ces = await Medir("ces", () => queryCes.EjecutarAsync(
                carreraId,
                escenarioId,
                costosPrecalculados: matriz,
                demandaPrecalculada: demanda,
                inversionesPrecalculada: inversiones,
                costoCarreraPrecalculado: ArancelReferencial,
                arancelVigentePrecalculado: ArancelVigente,
                factorImprevisto: factorImprevisto));
            Debug.WriteLine($"AF: TOTAL={swTotal.ElapsedMilliseconds}ms");

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
            MensajeExito = "Se aplicó el arancel óptimo financiero como arancel base vigente. Los descuentos por ciclo se mantienen y los ingresos fueron recalculados.";
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

    [RelayCommand]
    private void CerrarMensaje(string? cual)
    {
        switch (cual)
        {
            case "exito": MensajeExito = string.Empty; break;
            case "info": MensajeInfo = string.Empty; break;
            case "advertencia": MensajeAdvertencia = string.Empty; break;
            case "error": MensajeError = string.Empty; break;
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
        ArancelVigente = null;
        ArancelReferencial = null;
        Ces = null;
    }

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

    private static string Detalle(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}
