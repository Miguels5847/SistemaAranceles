using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.UseCases.SueldosPlantaCentral;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.PlantaCentral;

public sealed partial class AportePlantaCentralViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private bool _suspendiendoAutoCarga;

    public AportePlantaCentralViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty] private ObservableCollection<Carrera> _carreras = [];
    [ObservableProperty] private Carrera? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioProyeccion> _escenarios = [];
    [ObservableProperty] private EscenarioProyeccion? _escenarioSeleccionado;
    [ObservableProperty] private ObservableCollection<AportePeriodoDto> _periodos = [];
    [ObservableProperty] private ObservableCollection<string> _encabezadosPeriodos = [];
    [ObservableProperty] private ObservableCollection<FilaAportePlantaCentralTablaViewModel> _tablaResumenAporte = [];

    [ObservableProperty] private string _periodoInstitucionalVigente = string.Empty;
    [ObservableProperty] private decimal _totalMensualPlantaCentral;
    [ObservableProperty] private decimal _totalAnualPlantaCentral;
    [ObservableProperty] private decimal _estudiantesUniversidad;

    [ObservableProperty] private decimal _aporteAcumulado;
    [ObservableProperty] private decimal _aporteMensualPromedio;
    [ObservableProperty] private decimal _aportePromedioAnual;
    [ObservableProperty] private decimal _porcentajePromedio;

    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _textoAyudaCalculo = string.Empty;
    [ObservableProperty] private string _textoInterpretativo = string.Empty;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _tieneResultado;

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        if (_suspendiendoAutoCarga)
            return;

        _ = CargarEscenariosAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioProyeccion? value)
    {
        if (_suspendiendoAutoCarga || value is null)
            return;

        _ = CalcularAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        TieneResultado = false;
        _suspendiendoAutoCarga = true;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryCarreras = scope.ServiceProvider.GetRequiredService<ListarCarrerasConProyeccionQuery>();
            var lista = await queryCarreras.EjecutarAsync();
            Carreras = new ObservableCollection<Carrera>(lista);
            CarreraSeleccionada = Carreras.FirstOrDefault();

            if (CarreraSeleccionada is not null)
            {
                await CargarEscenariosAsync();
                if (EscenarioSeleccionado is not null)
                {
                    await CalcularAsync();
                }
            }
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
        finally
        {
            _suspendiendoAutoCarga = false;
            EstaCargando = false;
        }
    }

    private async Task CargarEscenariosAsync()
    {
        Escenarios = [];
        EscenarioSeleccionado = null;
        if (CarreraSeleccionada is null) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ListarEscenariosConProyeccionPorCarreraQuery>();
            var lista = await query.EjecutarAsync(CarreraSeleccionada.Id);
            Escenarios = new ObservableCollection<EscenarioProyeccion>(lista);
            EscenarioSeleccionado = Escenarios.FirstOrDefault();

            if (!_suspendiendoAutoCarga && EscenarioSeleccionado is not null)
            {
                await CalcularAsync();
            }
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CalcularAsync()
    {
        MensajeError = string.Empty;
        TieneResultado = false;
        EncabezadosPeriodos = [];
        TablaResumenAporte = [];

        if (CarreraSeleccionada is null || EscenarioSeleccionado is null)
        {
            MensajeError = "Seleccione carrera y escenario.";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<CalcularAportePlantaCentralCarreraQuery>();
            var dto = await query.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);

            PeriodoInstitucionalVigente = dto.PeriodoInstitucionalVigente;
            TotalMensualPlantaCentral = dto.TotalMensualPlantaCentral;
            TotalAnualPlantaCentral = dto.TotalAnualPlantaCentral;
            EstudiantesUniversidad = dto.EstudiantesUniversidad;
            AporteAcumulado = dto.AporteAcumulado;
            AporteMensualPromedio = CalcularPromedioMensual(dto);
            AportePromedioAnual = dto.AportePromedioAnual;
            PorcentajePromedio = dto.PorcentajePromedioSobreTotalAnual;
            Periodos = new ObservableCollection<AportePeriodoDto>(dto.Periodos);
            ConstruirTablaResumen(dto.Periodos);

            TextoAyudaCalculo = "Fuente: Datos Institucionales registra totales mensuales. "
                + "Aporte mensual carrera = total mensual planta central / estudiantes universidad x No. alumnos. "
                + "Aporte periodo = aporte mensual x 6 meses; aporte anual = ABR + SEP del mismo año.";
            TextoInterpretativo = $"La carrera \"{dto.CarreraNombre}\" aporta en promedio {AportePromedioAnual:C2} anuales a Planta Central, equivalente al {PorcentajePromedio:P2} del costo administrativo institucional.";
            TieneResultado = true;
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
    }

    private static decimal CalcularPromedioMensual(AportePlantaCentralCarreraDto dto)
    {
        var mesesCubiertos = dto.Periodos.Count * 6m;
        return mesesCubiertos <= 0m
            ? 0m
            : Math.Round(dto.AporteAcumulado / mesesCubiertos, 2);
    }

    private void ConstruirTablaResumen(IReadOnlyList<AportePeriodoDto> periodos)
    {
        EncabezadosPeriodos = new ObservableCollection<string>(
            periodos.Select(p => $"{p.Anio}{Environment.NewLine}{ObtenerNombrePeriodo(p)}"));

        var alumnos = periodos.Select(p => p.AlumnosCarrera).ToArray();
        var aportesMensuales = periodos.Select(p => Math.Round(p.AporteSemestral / 6m, 2)).ToArray();
        var aportes = periodos.Select(p => p.AporteSemestral).ToArray();

        TablaResumenAporte = new ObservableCollection<FilaAportePlantaCentralTablaViewModel>
        {
            new()
            {
                Concepto = "No. Alumnos",
                Valores = alumnos.Select(FormatearNumero).ToArray(),
                EsFilaPrincipal = true,
            },
            new()
            {
                Concepto = "Aporte mensual carrera",
                Valores = aportesMensuales.Select(FormatearMoneda).ToArray(),
            },
            new()
            {
                Concepto = "Aporte Planta Central",
                Valores = aportes.Select(FormatearMoneda).ToArray(),
                EsFilaAporte = true,
            },
            new()
            {
                Concepto = "TOTAL periodo (6 meses)",
                Valores = aportes.Select(FormatearMoneda).ToArray(),
                EsFilaTotal = true,
            },
        };
    }

    private static string ObtenerNombrePeriodo(AportePeriodoDto periodo)
    {
        if (!string.IsNullOrWhiteSpace(periodo.Etiqueta))
        {
            var etiqueta = periodo.Etiqueta.Trim();
            if (etiqueta.Contains("abr", StringComparison.OrdinalIgnoreCase))
            {
                return "ABR";
            }

            if (etiqueta.Contains("sep", StringComparison.OrdinalIgnoreCase))
            {
                return "SEP";
            }
        }

        return periodo.NumeroPeriodoEnAnio % 2 == 1 ? "ABR" : "SEP";
    }

    private static string FormatearNumero(decimal valor)
        => valor.ToString("N0", CultureInfo.CurrentCulture);

    private static string FormatearMoneda(decimal valor)
        => valor.ToString("C2", CultureInfo.CurrentCulture);
}

public sealed class FilaAportePlantaCentralTablaViewModel
{
    public string Concepto { get; init; } = string.Empty;
    public string[] Valores { get; init; } = [];
    public bool EsFilaPrincipal { get; init; }
    public bool EsFilaAporte { get; init; }
    public bool EsFilaTotal { get; init; }
}
