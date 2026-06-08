using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.CostosGastos;

public sealed class EscenarioCostosGastosOpcion
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public bool EsPredeterminado { get; init; }
    public bool TieneProyeccion { get; init; }
    public string NombreDisplay => TieneProyeccion ? $"{Nombre} (con proyección)" : $"{Nombre} (sin proyección)";
}

public sealed class CostoCarreraMatrizFilaView
{
    public string Concepto { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public string FormatoValor { get; init; } = FormatoMatrizCostosGastos.Moneda;
    public bool EsTotal { get; init; }

    public IReadOnlyList<string> PeriodosDisplay => Periodos
        .Select(v => FormatoMatrizCostosGastos.Formatear(v, FormatoValor))
        .ToList();

    public string TotalDisplay => FormatoMatrizCostosGastos.Formatear(Total, FormatoValor);
}

public sealed partial class CostosGastosViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FactorImprevistoCostosGastosState _factorImprevistoState;
    private bool _suprimirCambios;

    public CostosGastosViewModel(
        IServiceProvider serviceProvider,
        FactorImprevistoCostosGastosState factorImprevistoState)
    {
        _serviceProvider = serviceProvider;
        _factorImprevistoState = factorImprevistoState;
        FactorImprevisto = _factorImprevistoState.FactorImprevisto;
    }

    [ObservableProperty] private ObservableCollection<Carrera> _carreras = [];
    [ObservableProperty] private Carrera? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioCostosGastosOpcion> _escenarios = [];
    [ObservableProperty] private EscenarioCostosGastosOpcion? _escenarioSeleccionado;

    [ObservableProperty] private MatrizInvVinBecasDto? _matrizInvVinBecas;
    [ObservableProperty] private MatrizCostosGastosDto? _matrizCostosGastos;
    [ObservableProperty] private CostoCarreraResultadoDto? _resultadoCostoCarrera;
    [ObservableProperty] private ObservableCollection<CostoCarreraMatrizFilaView> _costoCarreraFilas = [];
    [ObservableProperty] private decimal _factorImprevisto = FactorImprevistoCostosGastosState.FactorPorDefecto;

    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private string _mensajeInfo = string.Empty;
    [ObservableProperty] private bool _estaCargando;

    public IReadOnlyList<string> EtiquetasInvVinBecas => MatrizInvVinBecas?.EtiquetasPeriodos ?? [];
    public IReadOnlyList<CostoGastoRubroDto> FilasInvVinBecas => MatrizInvVinBecas?.Filas ?? [];
    public IReadOnlyList<string> EtiquetasCostosGastos => MatrizCostosGastos?.EtiquetasPeriodos ?? [];
    public IReadOnlyList<CostoGastoRubroDto> FilasCostosGastos => MatrizCostosGastos?.ProyeccionCostosGastos ?? [];
    public IReadOnlyList<PonderacionCostoGastoDto> FilasPonderacion => MatrizCostosGastos?.Ponderacion ?? [];
    public IReadOnlyList<CostoGastoRubroDto> FilasDescontadas => MatrizCostosGastos?.DescontadoBecasGobierno ?? [];
    public IReadOnlyList<string> EtiquetasCostoCarrera => ResultadoCostoCarrera?.EtiquetasPeriodos ?? [];
    public bool TieneResultadoCostoCarrera => ResultadoCostoCarrera?.TieneDatos == true;
    public bool PuedeTrabajar => CarreraSeleccionada is not null && !EstaCargando;

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeTrabajar));
        if (_suprimirCambios || EstaCargando)
            return;

        _ = RecargarEscenariosAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioCostosGastosOpcion? value)
    {
        _ = value;
        if (_suprimirCambios || EstaCargando)
            return;

        _ = RefrescarAsync();
    }

    partial void OnEstaCargandoChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeTrabajar));
    }

    partial void OnMatrizInvVinBecasChanged(MatrizInvVinBecasDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(EtiquetasInvVinBecas));
        OnPropertyChanged(nameof(FilasInvVinBecas));
    }

    partial void OnMatrizCostosGastosChanged(MatrizCostosGastosDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(EtiquetasCostosGastos));
        OnPropertyChanged(nameof(FilasCostosGastos));
        OnPropertyChanged(nameof(FilasPonderacion));
        OnPropertyChanged(nameof(FilasDescontadas));
    }

    partial void OnResultadoCostoCarreraChanged(CostoCarreraResultadoDto? value)
    {
        CostoCarreraFilas = ConstruirFilasCostoCarrera(value);
        OnPropertyChanged(nameof(EtiquetasCostoCarrera));
        OnPropertyChanged(nameof(TieneResultadoCostoCarrera));
    }

    partial void OnFactorImprevistoChanged(decimal value)
    {
        if (value > 0m)
            _factorImprevistoState.Establecer(value);

        // Al cambiar el imprevisto se recalcula toda la matriz (Inv/Vin/Becas → Costos y Gastos → Costo Carrera).
        if (_suprimirCambios || EstaCargando || EscenarioSeleccionado is null || value <= 0m)
            return;

        _ = RefrescarAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando)
            return;

        EstaCargando = true;
        MensajeError = string.Empty;
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
                LimpiarMatrices();
                MensajeInfo = Carreras.Count == 0
                    ? "No hay carreras registradas."
                    : "Selecciona una carrera para cargar Costos y Gastos.";
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
            LimpiarMatrices();
            MensajeInfo = "Selecciona una carrera para cargar Costos y Gastos.";
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

            // Solo escenarios con proyección para la carrera: evita seleccionar duplicados sin datos
            // (arancel/becas/costos no se pueden resolver sin proyección de estudiantes).
            var opciones = escenarios
                .Where(e => e.CarreraId == CarreraSeleccionada.Id
                            && escenariosConProyeccion.Contains(e.Id))
                .Select(e => new EscenarioCostosGastosOpcion
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
                Escenarios = new ObservableCollection<EscenarioCostosGastosOpcion>(opciones);
                EscenarioSeleccionado = Escenarios.FirstOrDefault(e => e.Id == escenarioActualId)
                    ?? Escenarios.FirstOrDefault();
            }
            finally
            {
                _suprimirCambios = false;
            }

            if (EscenarioSeleccionado is null)
            {
                LimpiarMatrices();
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
            LimpiarMatrices();
            MensajeError = string.Empty;
            MensajeExito = string.Empty;
            MensajeInfo = "Selecciona una carrera para refrescar la información.";
            return;
        }

        if (EscenarioSeleccionado is null)
        {
            LimpiarMatrices();
            MensajeError = "Selecciona un escenario.";
            return;
        }

        if (FactorImprevisto <= 0m)
        {
            MensajeExito = string.Empty;
            MensajeError = "El factor imprevisto debe ser mayor a 0.";
            return;
        }

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        MensajeInfo = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryDemanda = scope.ServiceProvider.GetRequiredService<ObtenerDemandaProyectadaQuery>();
            var queryInv = scope.ServiceProvider.GetRequiredService<ObtenerMatrizInvVinBecasQuery>();
            var queryCostos = scope.ServiceProvider.GetRequiredService<ObtenerMatrizCostosGastosQuery>();
            var queryCostoCarrera = scope.ServiceProvider.GetRequiredService<ObtenerCostoCarreraQuery>();
            var queryIngresos = scope.ServiceProvider.GetRequiredService<CalcularIngresosProyectadosQuery>();

            var carreraId = CarreraSeleccionada.Id;
            var escenarioId = EscenarioSeleccionado.Id;
            // Encadena resultados ya calculados: Demanda → InvVinBecas → CostosGastos → CostoCarrera
            // para no recomputar las matrices anidadas (ni reconsultar demanda/arancel) varias veces.
            var demanda = await queryDemanda.EjecutarAsync(carreraId, escenarioId);
            // KAN-44: becas reales (de Ingresos, ya con descuentos) para mostrar en Inv. Vin. Becas como
            // dato referencial. No suman como costo (CostosPorServicios/PE las excluyen).
            var ingresos = await queryIngresos.EjecutarAsync(carreraId, escenarioId);
            var becasPorPeriodo = ingresos.CeldasPlanas
                .GroupBy(c => c.PeriodoAcademicoId)
                .ToDictionary(g => g.Key, g => g.Sum(c => c.Becas));
            var invVinBecas = await queryInv.EjecutarAsync(
                carreraId, escenarioId, demandaPrecalculada: demanda, becasInstitucionalesPorPeriodo: becasPorPeriodo);
            MatrizInvVinBecas = invVinBecas;
            var costosGastos = await queryCostos.EjecutarAsync(
                carreraId,
                escenarioId,
                invVinBecasPrecalculado: invVinBecas,
                demandaPrecalculada: demanda,
                factorImprevisto: FactorImprevisto);
            MatrizCostosGastos = costosGastos;
            ResultadoCostoCarrera = await queryCostoCarrera.EjecutarAsync(carreraId, escenarioId, matrizPrecalculada: costosGastos);

            var advertencias = new[]
            {
                MatrizInvVinBecas?.MensajeAdvertencia,
                MatrizCostosGastos?.MensajeAdvertencia,
                ResultadoCostoCarrera?.MensajeAdvertencia
            }
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
            MensajeExito = advertencias.Count == 0
                ? "Costos y Gastos calculado correctamente."
                : string.Empty;
            MensajeError = advertencias.Count > 0 ? string.Join(Environment.NewLine, advertencias) : string.Empty;
        }
        catch (Exception ex)
        {
            LimpiarMatrices();
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void LimpiarMatrices()
    {
        MatrizInvVinBecas = null;
        MatrizCostosGastos = null;
        ResultadoCostoCarrera = null;
        CostoCarreraFilas = [];
    }

    private static ObservableCollection<CostoCarreraMatrizFilaView> ConstruirFilasCostoCarrera(CostoCarreraResultadoDto? resultado)
    {
        if (resultado is null || resultado.Periodos.Count == 0)
            return [];

        var periodos = resultado.Periodos;
        var filas = new List<CostoCarreraMatrizFilaView>
        {
            new()
            {
                Concepto = "Total costos y gastos",
                Periodos = periodos.Select(p => p.TotalCostosGastos).ToList(),
                Total = periodos.Sum(p => p.TotalCostosGastos),
                FormatoValor = FormatoMatrizCostosGastos.Moneda
            },
            new()
            {
                Concepto = "Nº estudiantes",
                Periodos = periodos.Select(p => p.NumeroEstudiantes).ToList(),
                Total = periodos.Sum(p => p.NumeroEstudiantes),
                FormatoValor = FormatoMatrizCostosGastos.Entero
            },
            new()
            {
                Concepto = "Costo por estudiante",
                Periodos = periodos.Select(p => p.CostoPorEstudiante).ToList(),
                Total = resultado.CostoCarreraCompleta,
                FormatoValor = FormatoMatrizCostosGastos.Moneda,
                EsTotal = true
            }
        };

        return new ObservableCollection<CostoCarreraMatrizFilaView>(filas);
    }

    private static string Detalle(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}
