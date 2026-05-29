using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Domain.Entities;

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
    private bool _suprimirCambios;

    public CostosGastosViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [ObservableProperty] private ObservableCollection<Carrera> _carreras = [];
    [ObservableProperty] private Carrera? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioCostosGastosOpcion> _escenarios = [];
    [ObservableProperty] private EscenarioCostosGastosOpcion? _escenarioSeleccionado;

    [ObservableProperty] private MatrizInvVinBecasDto? _matrizInvVinBecas;
    [ObservableProperty] private MatrizCostosGastosDto? _matrizCostosGastos;
    [ObservableProperty] private CostoCarreraResultadoDto? _resultadoCostoCarrera;
    [ObservableProperty] private ObservableCollection<CostoCarreraMatrizFilaView> _costoCarreraFilas = [];

    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private bool _estaCargando;

    public IReadOnlyList<string> EtiquetasInvVinBecas => MatrizInvVinBecas?.EtiquetasPeriodos ?? [];
    public IReadOnlyList<CostoGastoRubroDto> FilasInvVinBecas => MatrizInvVinBecas?.Filas ?? [];
    public IReadOnlyList<string> EtiquetasCostosGastos => MatrizCostosGastos?.EtiquetasPeriodos ?? [];
    public IReadOnlyList<CostoGastoRubroDto> FilasCostosGastos => MatrizCostosGastos?.ProyeccionCostosGastos ?? [];
    public IReadOnlyList<PonderacionCostoGastoDto> FilasPonderacion => MatrizCostosGastos?.Ponderacion ?? [];
    public IReadOnlyList<CostoGastoRubroDto> FilasDescontadas => MatrizCostosGastos?.DescontadoBecasGobierno ?? [];
    public IReadOnlyList<string> EtiquetasCostoCarrera => ResultadoCostoCarrera?.EtiquetasPeriodos ?? [];
    public bool TieneResultadoCostoCarrera => ResultadoCostoCarrera?.TieneDatos == true;

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        _ = value;
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
                LimpiarMatrices();
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
            LimpiarMatrices();
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
                .Where(e => e.CarreraId == CarreraSeleccionada.Id)
                .Select(e => new EscenarioCostosGastosOpcion
                {
                    Id = e.Id,
                    CarreraId = e.CarreraId,
                    Nombre = e.Nombre,
                    EsPredeterminado = e.EsPredeterminado,
                    TieneProyeccion = escenariosConProyeccion.Contains(e.Id)
                })
                .OrderByDescending(e => e.TieneProyeccion)
                .ThenByDescending(e => e.EsPredeterminado)
                .ThenBy(e => e.Nombre)
                .ToList();

            _suprimirCambios = true;
            try
            {
                Escenarios = new ObservableCollection<EscenarioCostosGastosOpcion>(opciones);
                EscenarioSeleccionado = Escenarios.FirstOrDefault(e => e.Id == escenarioActualId)
                    ?? Escenarios.FirstOrDefault(e => e.TieneProyeccion)
                    ?? Escenarios.FirstOrDefault();
            }
            finally
            {
                _suprimirCambios = false;
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
            MensajeError = "Selecciona una carrera.";
            return;
        }

        if (EscenarioSeleccionado is null)
        {
            LimpiarMatrices();
            MensajeError = "Selecciona un escenario.";
            return;
        }

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryInv = scope.ServiceProvider.GetRequiredService<ObtenerMatrizInvVinBecasQuery>();
            var queryCostos = scope.ServiceProvider.GetRequiredService<ObtenerMatrizCostosGastosQuery>();
            var queryCostoCarrera = scope.ServiceProvider.GetRequiredService<ObtenerCostoCarreraQuery>();

            var carreraId = CarreraSeleccionada.Id;
            var escenarioId = EscenarioSeleccionado.Id;
            MatrizInvVinBecas = await queryInv.EjecutarAsync(carreraId, escenarioId);
            MatrizCostosGastos = await queryCostos.EjecutarAsync(carreraId, escenarioId);
            ResultadoCostoCarrera = await queryCostoCarrera.EjecutarAsync(carreraId, escenarioId);

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
                FormatoValor = FormatoMatrizCostosGastos.Decimal
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
