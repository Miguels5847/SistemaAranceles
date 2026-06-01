using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;
using SistemaAranceles.Presentation.Views.CargosFacultad;
using System.Windows;

namespace SistemaAranceles.Presentation.ViewModels.CargosFacultad;

public sealed partial class CargosFacultadViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private readonly ConsolidadoEstudiantesActualState _consolidadoActualState;
    private bool _suprimirRecargaAutomatica;

    public CargosFacultadViewModel(
        IServiceProvider serviceProvider,
        SesionActual sesionActual,
        ConsolidadoEstudiantesActualState consolidadoActualState)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
        _consolidadoActualState = consolidadoActualState;
    }

    [ObservableProperty]
    private ObservableCollection<Carrera> _carreras = [];

    [ObservableProperty]
    private Carrera? _carreraSeleccionada;

    [ObservableProperty]
    private ObservableCollection<EscenarioProyeccion> _escenarios = [];

    [ObservableProperty]
    private EscenarioProyeccion? _escenarioSeleccionado;

    [ObservableProperty]
    private ObservableCollection<PeriodoDisponibleSueldosDto> _periodos = [];

    [ObservableProperty]
    private PeriodoDisponibleSueldosDto? _periodoSeleccionado;

    [ObservableProperty]
    private ObservableCollection<FilaSueldoPeriodoDto> _filas = [];

    [ObservableProperty]
    private string _estudiantesUAInput = ConfiguracionSueldosCarrera.EstudiantesUnidadAcademicaPorDefecto.ToString(CultureInfo.InvariantCulture);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextoEstudiantesCarrera))]
    private decimal _estudiantesCarrera;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TextoInflacionPeriodo))]
    private decimal _inflacionPeriodoPorcentaje;

    [ObservableProperty]
    private decimal _totalNumeroPersonas;

    [ObservableProperty]
    private decimal _totalSueldoMensual;

    [ObservableProperty]
    private decimal _totalDecimoTercero;

    [ObservableProperty]
    private decimal _totalDecimoCuarto;

    [ObservableProperty]
    private decimal _totalVacaciones;

    [ObservableProperty]
    private decimal _totalFondoReserva;

    [ObservableProperty]
    private decimal _totalAportePatronal;

    [ObservableProperty]
    private decimal _totalSemestrePeriodo;

    [ObservableProperty]
    private bool _estaCargando;

    [ObservableProperty]
    private bool _estaGenerando;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    public bool PuedeVer => _sesionActual.TienePermiso("AF.VER") || _sesionActual.EsAdministrador;

    public static string TituloModulo => "Sueldos";

    public string TextoInflacionPeriodo => string.Format(CultureInfo.CurrentCulture, "{0:N2}%", InflacionPeriodoPorcentaje);

    public string TextoEstudiantesCarrera => string.Format(CultureInfo.CurrentCulture, "{0:N0}", EstudiantesCarrera);

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!PuedeVer)
        {
            MensajeError = "Acceso denegado al módulo de Sueldos.";
            return;
        }

        if (EstaCargando)
            return;

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryCarreras = scope.ServiceProvider.GetRequiredService<ListarCarrerasConProyeccionQuery>();
            var carreras = await queryCarreras.EjecutarAsync();

            _suprimirRecargaAutomatica = true;
            try
            {
                Carreras = new ObservableCollection<Carrera>(carreras);
                if (Carreras.Count > 0 && (CarreraSeleccionada is null || Carreras.All(c => c.Id != CarreraSeleccionada.Id)))
                    CarreraSeleccionada = Carreras[0];
                else if (Carreras.Count == 0)
                    CarreraSeleccionada = null;
            }
            finally
            {
                _suprimirRecargaAutomatica = false;
            }

            if (CarreraSeleccionada is not null)
                await CargarEscenariosAsync();
            else
                LimpiarVista();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar el módulo de Sueldos: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private async Task CargarEscenariosAsync()
    {
        if (CarreraSeleccionada is null)
        {
            _suprimirRecargaAutomatica = true;
            try
            {
                EscenarioSeleccionado = null;
                Escenarios = [];
                PeriodoSeleccionado = null;
                Periodos = [];
            }
            finally { _suprimirRecargaAutomatica = false; }
            LimpiarVista();
            return;
        }

        IReadOnlyList<EscenarioProyeccion> lista;
        using (var scope = _serviceProvider.CreateScope())
        {
            var query = scope.ServiceProvider.GetRequiredService<ListarEscenariosConProyeccionPorCarreraQuery>();
            lista = await query.EjecutarAsync(CarreraSeleccionada.Id);
        }

        _suprimirRecargaAutomatica = true;
        try
        {
            EscenarioSeleccionado = null;
            Escenarios = new ObservableCollection<EscenarioProyeccion>(lista);
            EscenarioSeleccionado = Escenarios.Count > 0 ? Escenarios[0] : null;
        }
        finally { _suprimirRecargaAutomatica = false; }

        if (EscenarioSeleccionado is not null)
            await CargarPeriodosAsync();
        else
        {
            _suprimirRecargaAutomatica = true;
            try
            {
                PeriodoSeleccionado = null;
                Periodos = [];
            }
            finally { _suprimirRecargaAutomatica = false; }
            LimpiarVista();
        }
    }

    private async Task CargarPeriodosAsync()
    {
        if (CarreraSeleccionada is null || EscenarioSeleccionado is null)
        {
            _suprimirRecargaAutomatica = true;
            try
            {
                PeriodoSeleccionado = null;
                Periodos = [];
            }
            finally { _suprimirRecargaAutomatica = false; }
            LimpiarVista();
            return;
        }

        IReadOnlyList<PeriodoDisponibleSueldosDto> lista;
        using (var scope = _serviceProvider.CreateScope())
        {
            var query = scope.ServiceProvider.GetRequiredService<ListarPeriodosDeProyeccionEstudiantesQuery>();
            lista = await query.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);
        }

        _suprimirRecargaAutomatica = true;
        try
        {
            PeriodoSeleccionado = null;
            Periodos = new ObservableCollection<PeriodoDisponibleSueldosDto>(lista);
            PeriodoSeleccionado = Periodos.Count > 0 ? Periodos[0] : null;
        }
        finally { _suprimirRecargaAutomatica = false; }

        if (PeriodoSeleccionado is not null)
            await GenerarAsync();
        else
            LimpiarVista();
    }

    [RelayCommand]
    private async Task GenerarAsync()
    {
        if (!PuedeVer)
        {
            MensajeError = "Acceso denegado al módulo de Sueldos.";
            return;
        }

        if (EstaGenerando)
            return;

        if (CarreraSeleccionada is null)
        {
            MensajeError = "Seleccione una carrera con proyección de estudiantes.";
            LimpiarVista();
            return;
        }

        if (EscenarioSeleccionado is null)
        {
            MensajeError = "Seleccione un escenario.";
            LimpiarVista();
            return;
        }

        if (PeriodoSeleccionado is null)
        {
            MensajeError = "Seleccione un período.";
            LimpiarVista();
            return;
        }

        if (!TryParseDecimalFlexible(EstudiantesUAInput, out var estudiantesUA) || estudiantesUA < 0m)
        {
            MensajeError = "Ingrese un valor válido para Estudiantes UA.";
            return;
        }

        EstaGenerando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<GenerarTablaSueldosPeriodoQuery>();
            var consolidadoActual = await ObtenerConsolidadoActualAsync();
            var resultado = await query.EjecutarAsync(
                CarreraSeleccionada.Id,
                EscenarioSeleccionado.Id,
                PeriodoSeleccionado.PeriodoAcademicoId,
                estudiantesUA,
                consolidadoActual);

            Filas = new ObservableCollection<FilaSueldoPeriodoDto>(resultado.Filas);
            EstudiantesCarrera = resultado.EstudiantesCarrera;
            InflacionPeriodoPorcentaje = resultado.InflacionPeriodoPorcentaje;

            TotalNumeroPersonas = resultado.TotalNumeroPersonas;
            TotalSueldoMensual = resultado.TotalSueldoMensual;
            TotalDecimoTercero = resultado.TotalDecimoTercero;
            TotalDecimoCuarto = resultado.TotalDecimoCuarto;
            TotalVacaciones = resultado.TotalVacaciones;
            TotalFondoReserva = resultado.TotalFondoReserva;
            TotalAportePatronal = resultado.TotalAportePatronal;
            TotalSemestrePeriodo = resultado.TotalSemestrePeriodo;

            OnPropertyChanged(nameof(TextoInflacionPeriodo));
            OnPropertyChanged(nameof(TextoEstudiantesCarrera));
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al generar la tabla de sueldos: {ObtenerDetalle(ex)}";
            LimpiarVista();
        }
        finally
        {
            EstaGenerando = false;
        }
    }

    [RelayCommand]
    private async Task MostrarResumenSueldosAsync()
    {
        if (!PuedeVer)
        {
            MensajeError = "Acceso denegado al módulo de Sueldos.";
            return;
        }

        if (CarreraSeleccionada is null || EscenarioSeleccionado is null)
        {
            MensajeError = "Seleccione carrera y escenario antes de generar el resumen.";
            return;
        }

        if (!TryParseDecimalFlexible(EstudiantesUAInput, out var estudiantesUA) || estudiantesUA < 0m)
        {
            MensajeError = "Ingrese un valor válido para Estudiantes UA.";
            return;
        }

        try
        {
            ResumenSueldosVistaDto resumen;
            var consolidadoActual = await ObtenerConsolidadoActualAsync();
            using (var scope = _serviceProvider.CreateScope())
            {
                var query = scope.ServiceProvider.GetRequiredService<GenerarResumenSueldosQuery>();
                resumen = await query.EjecutarAsync(
                    CarreraSeleccionada.Id,
                    EscenarioSeleccionado.Id,
                    estudiantesUA,
                    consolidadoActual);
            }

            var ventana = new ResumenSueldosWindow();
            var ownerCandidato = System.Windows.Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive && !ReferenceEquals(w, ventana));
            if (ownerCandidato is not null)
                ventana.Owner = ownerCandidato;

            ventana.CargarResumen(resumen);
            ventana.ShowDialog();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al generar el resumen de sueldos: {ObtenerDetalle(ex)}";
        }
    }

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        _ = value;
        if (_suprimirRecargaAutomatica || EstaCargando)
            return;
        _ = CargarEscenariosAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioProyeccion? value)
    {
        _ = value;
        if (_suprimirRecargaAutomatica || EstaCargando)
            return;
        _ = CargarPeriodosAsync();
    }

    partial void OnPeriodoSeleccionadoChanged(PeriodoDisponibleSueldosDto? value)
    {
        _ = value;
        if (_suprimirRecargaAutomatica || EstaCargando)
            return;
        _ = GenerarAsync();
    }

    private void LimpiarVista()
    {
        Filas = [];
        EstudiantesCarrera = 0m;
        InflacionPeriodoPorcentaje = 0m;
        TotalNumeroPersonas = 0m;
        TotalSueldoMensual = 0m;
        TotalDecimoTercero = 0m;
        TotalDecimoCuarto = 0m;
        TotalVacaciones = 0m;
        TotalFondoReserva = 0m;
        TotalAportePatronal = 0m;
        TotalSemestrePeriodo = 0m;
        OnPropertyChanged(nameof(TextoInflacionPeriodo));
        OnPropertyChanged(nameof(TextoEstudiantesCarrera));
    }

    private static bool TryParseDecimalFlexible(string? value, out decimal result)
    {
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            return true;

        var normalizado = value?.Replace(',', '.');
        return decimal.TryParse(normalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
    }

    private static string ObtenerDetalle(Exception ex)
        => ex.InnerException?.Message ?? ex.Message;

    private async Task<ProyeccionConsolidadaDto?> ObtenerConsolidadoActualAsync()
    {
        if (CarreraSeleccionada is null || EscenarioSeleccionado is null)
            return null;

        if (_consolidadoActualState.CoincideCon(CarreraSeleccionada.Id, EscenarioSeleccionado.Id))
            return _consolidadoActualState.Detalle;

        using var scope = _serviceProvider.CreateScope();
        var repoProyeccion = scope.ServiceProvider.GetRequiredService<IRepositorioProyeccionEstudiantes>();
        var repoConfiguracion = scope.ServiceProvider.GetRequiredService<IRepositorioConfiguracionRetencion>();
        var ucObtenerProyeccion = scope.ServiceProvider.GetRequiredService<ObtenerProyeccionEstudiantesUseCase>();
        var ucOverrides = scope.ServiceProvider.GetRequiredService<ListarOverridesHorasPeriodoUseCase>();

        var proyeccionId = await repoProyeccion.ObtenerIdPorCarreraYEscenarioAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);
        if (proyeccionId is null or 0)
            return null;

        var proyeccion = await ucObtenerProyeccion.EjecutarAsync(proyeccionId.Value);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
            return null;

        var config = (await repoConfiguracion.ListarDtoAsync())
            .FirstOrDefault(c => c.CarreraId == CarreraSeleccionada.Id && c.EscenarioProyeccionId == EscenarioSeleccionado.Id);
        if (config is null)
            return null;

        var overrides = await ucOverrides.EjecutarAsync(proyeccion.Id);
        var (docOverride, tecOverride) = ConstruirArreglosOverride(overrides, proyeccion);

        var consolidado = ConsolidadorProyeccionEstudiantes.Calcular(
            proyeccion,
            config.ParalelosPeriodo1,
            config.ParalelosPeriodo2,
            config.TasaRetencionPorcentaje,
            config.TasaGraduacionPorcentaje,
            horasDocSemestralesOverride: docOverride,
            horasTecSemestralesOverride: tecOverride,
            horasDocSemanaOverride: 18m,
            horasTecSemanaOverride: 40m);

        _consolidadoActualState.Establecer(CarreraSeleccionada.Id, EscenarioSeleccionado.Id, consolidado);
        return consolidado;
    }

    private static (decimal[]? doc, decimal[]? prac) ConstruirArreglosOverride(
        IReadOnlyList<SistemaAranceles.Domain.Entities.OverrideHorasPeriodo> overrides,
        ProyeccionEstudiantesDto proyeccion)
    {
        if (overrides.Count == 0) return (null, null);

        var totalPeriodos = proyeccion.Detalles.Select(d => d.NumeroPeriodo).DefaultIfEmpty(0).Max();
        if (totalPeriodos <= 0) return (null, null);

        decimal[] doc = new decimal[totalPeriodos];
        decimal[] prac = new decimal[totalPeriodos];
        bool hayDoc = false;
        bool hayPrac = false;

        foreach (var o in overrides)
        {
            var idx = o.Periodo - 1;
            if (idx < 0 || idx >= totalPeriodos) continue;
            if (o.HorasDocencia is { } hd) { doc[idx] = hd; hayDoc = true; }
            if (o.HorasPractica is { } hp) { prac[idx] = hp; hayPrac = true; }
        }

        return (hayDoc ? doc : null, hayPrac ? prac : null);
    }
}
