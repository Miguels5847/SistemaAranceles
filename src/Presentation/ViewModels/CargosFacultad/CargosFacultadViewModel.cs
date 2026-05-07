using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.CargosFacultad;

public sealed partial class CargosFacultadViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private bool _suprimirRecargaAutomatica;

    public CargosFacultadViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty]
    private ObservableCollection<Carrera> _carreras = [];

    [ObservableProperty]
    private Carrera? _carreraSeleccionada;

    [ObservableProperty]
    private ObservableCollection<PeriodoAcademicoCatalogDto> _periodos = [];

    [ObservableProperty]
    private PeriodoAcademicoCatalogDto? _periodoSeleccionado;

    [ObservableProperty]
    private ObservableCollection<FilaSueldoPeriodoDto> _filas = [];

    [ObservableProperty]
    private decimal _estudiantesUA;

    [ObservableProperty]
    private decimal _estudiantesCarrera;

    [ObservableProperty]
    private decimal _inflacionAcumulada = 1m;

    [ObservableProperty]
    private decimal _inflacionPeriodoPorcentaje;

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

    public string TituloModulo => "Sueldos";

    public string TextoInflacionPeriodo =>
        $"{InflacionPeriodoPorcentaje:N2}%";

    public string TextoInflacionAcumulada =>
        $"x {InflacionAcumulada:N4}";

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

            var repoCarrera = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var carreras = await repoCarrera.ListarAsync();

            var queryPeriodos = scope.ServiceProvider.GetRequiredService<ListarPeriodosAcademicosQuery>();
            var periodos = await queryPeriodos.EjecutarAsync();

            _suprimirRecargaAutomatica = true;
            try
            {
                Carreras = new ObservableCollection<Carrera>(carreras.OrderBy(x => x.Nombre));
                Periodos = new ObservableCollection<PeriodoAcademicoCatalogDto>(
                    periodos.OrderBy(p => p.Anio).ThenBy(p => p.NumeroPeriodo));

                if (Carreras.Count > 0 && (CarreraSeleccionada is null || Carreras.All(c => c.Id != CarreraSeleccionada.Id)))
                    CarreraSeleccionada = Carreras[0];

                if (Periodos.Count > 0 && (PeriodoSeleccionado is null || Periodos.All(p => p.Id != PeriodoSeleccionado.Id)))
                    PeriodoSeleccionado = Periodos[0];
            }
            finally
            {
                _suprimirRecargaAutomatica = false;
            }

            await GenerarAsync();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar el módulo de sueldos: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaCargando = false;
        }
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
            MensajeError = "Seleccione una carrera.";
            LimpiarVista();
            return;
        }

        if (PeriodoSeleccionado is null)
        {
            MensajeError = "Seleccione un período académico.";
            LimpiarVista();
            return;
        }

        EstaGenerando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<GenerarTablaSueldosPeriodoQuery>();
            var resultado = await query.EjecutarAsync(CarreraSeleccionada.Id, PeriodoSeleccionado.Id);

            Filas = new ObservableCollection<FilaSueldoPeriodoDto>(resultado.Filas);
            EstudiantesUA = resultado.EstudiantesUA;
            EstudiantesCarrera = resultado.EstudiantesCarrera;
            InflacionAcumulada = resultado.InflacionAcumulada;
            InflacionPeriodoPorcentaje = resultado.InflacionPeriodoPorcentaje;
            TotalSemestrePeriodo = resultado.TotalSemestrePeriodo;

            OnPropertyChanged(nameof(TextoInflacionPeriodo));
            OnPropertyChanged(nameof(TextoInflacionAcumulada));
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

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        _ = value;
        if (_suprimirRecargaAutomatica || EstaCargando)
            return;
        _ = GenerarAsync();
    }

    partial void OnPeriodoSeleccionadoChanged(PeriodoAcademicoCatalogDto? value)
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
        InflacionAcumulada = 1m;
        InflacionPeriodoPorcentaje = 0m;
        TotalSemestrePeriodo = 0m;
        OnPropertyChanged(nameof(TextoInflacionPeriodo));
        OnPropertyChanged(nameof(TextoInflacionAcumulada));
    }

    private static string ObtenerDetalle(Exception ex)
        => ex.InnerException?.Message ?? ex.Message;
}
