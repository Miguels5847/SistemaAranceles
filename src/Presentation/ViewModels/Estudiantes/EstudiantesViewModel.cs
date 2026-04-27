using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.Estudiantes;

// ── Opciones para ComboBoxes ───────────────────────────────────────────────────
public sealed class CarreraOpcion
{
    public int    Id          { get; init; }
    public string Descripcion { get; init; } = string.Empty;
}

public sealed class EscenarioOpcion
{
    public int    Id          { get; init; }
    public int    CarreraId   { get; init; }
    public string Descripcion { get; init; } = string.Empty;
}

public sealed class SimulacionOpcion
{
    public int    Id          { get; init; }
    public int    CohorteAnio { get; init; }
    public string Descripcion { get; init; } = string.Empty;
}

// ── Ítem de la lista de proyecciones ─────────────────────────────────────────
public sealed class ProyeccionEstudiantesItemViewModel
{
    public int    Id                 { get; init; }
    public int    CarreraId          { get; init; }
    public int    EscenarioId        { get; init; }
    public int    AnioBase           { get; init; }
    public string CarreraNombre      { get; init; } = string.Empty;
    public string CarreraCodigo      { get; init; } = string.Empty;
    public string EscenarioNombre    { get; init; } = string.Empty;
    public int    SemanasPorSemestre { get; init; }
    public string CreadoEnTexto      { get; init; } = string.Empty;
}

// ── ViewModel principal ───────────────────────────────────────────────────────
public sealed partial class EstudiantesViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual     _sesionActual;

    private IReadOnlyList<EscenarioOpcion> _todosLosEscenarios = [];

    // Guardamos el último contexto de cálculo para poder recalcular sin ir a la BD
    private ProyeccionEstudiantesDto?     _ultimaProyeccion;
    private ConfiguracionRetencionDto?    _ultimaConfig;

    public EstudiantesViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual    = sesionActual;
    }

    // ── Listas ────────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ProyeccionEstudiantesItemViewModel> _proyecciones = [];
    [ObservableProperty] private ProyeccionEstudiantesItemViewModel? _proyeccionSeleccionada;

    [ObservableProperty] private ObservableCollection<CarreraOpcion>    _carreras    = [];
    [ObservableProperty] private CarreraOpcion?                         _carreraSeleccionada;

    [ObservableProperty] private ObservableCollection<EscenarioOpcion>  _escenarios  = [];
    [ObservableProperty] private EscenarioOpcion?                       _escenarioSeleccionado;

    [ObservableProperty] private ObservableCollection<SimulacionOpcion> _simulaciones = [];
    [ObservableProperty] private SimulacionOpcion?                      _simulacionSeleccionada;

    // ── Formulario ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _semanasPorSemestre = "16";

    // ── Parámetros editables J24 / J30 ────────────────────────────────────────
    // Al cambiar cualquiera de los dos se dispara RecalcularConsolidado()
    private decimal _horasDocenteSemana = 18m;
    public decimal HorasDocenteSemana
    {
        get => _horasDocenteSemana;
        set
        {
            if (SetProperty(ref _horasDocenteSemana, value))
                RecalcularConsolidado();
        }
    }

    private decimal _horasTecnicoSemana = 40m;
    public decimal HorasTecnicoSemana
    {
        get => _horasTecnicoSemana;
        set
        {
            if (SetProperty(ref _horasTecnicoSemana, value))
                RecalcularConsolidado();
        }
    }

    // ── Estado ────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _estaCargando;
    [ObservableProperty] private bool   _estaGenerando;
    [ObservableProperty] private bool   _estaEliminando;
    [ObservableProperty] private bool   _estaCargandoDetalle;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;

    // ── Consolidado (tablas) ──────────────────────────────────────────────────
    [ObservableProperty] private ProyeccionConsolidadaDto? _detalleConsolidado;

    public bool TieneDetalleConsolidado => DetalleConsolidado is not null;

    partial void OnDetalleConsolidadoChanged(ProyeccionConsolidadaDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(TieneDetalleConsolidado));
    }

    // ── Permisos ──────────────────────────────────────────────────────────────
    public bool PuedeVer      => _sesionActual.TienePermiso("ES.VER")      || _sesionActual.EsAdministrador;
    public bool PuedeGenerar  => _sesionActual.TienePermiso("ES.CREAR")    || _sesionActual.EsAdministrador;
    public bool PuedeEliminar => _sesionActual.TienePermiso("ES.ELIMINAR") || _sesionActual.EsAdministrador;

    // ── Cascada: Carrera → Escenarios ─────────────────────────────────────────
    partial void OnCarreraSeleccionadaChanged(CarreraOpcion? value)
    {
        EscenarioSeleccionado  = null;
        SimulacionSeleccionada = null;
        Simulaciones.Clear();
        MensajeError = string.Empty;

        Escenarios = value is null
            ? new ObservableCollection<EscenarioOpcion>(_todosLosEscenarios)
            : new ObservableCollection<EscenarioOpcion>(
                _todosLosEscenarios.Where(e => e.CarreraId == value.Id));
    }

    // ── Cascada: Escenario → Simulaciones ─────────────────────────────────────
    partial void OnEscenarioSeleccionadoChanged(EscenarioOpcion? value)
    {
        SimulacionSeleccionada = null;
        Simulaciones.Clear();
        MensajeError = string.Empty;

        if (value is not null && CarreraSeleccionada is not null)
            _ = CargarSimulacionesAsync(CarreraSeleccionada.Id, value.Id);
    }

    // ── Selección de proyección → cargar consolidado ──────────────────────────
    partial void OnProyeccionSeleccionadaChanged(ProyeccionEstudiantesItemViewModel? value)
    {
        DetalleConsolidado = null;
        MensajeError       = string.Empty;
        if (value is not null)
            _ = CargarDetalleConsolidadoAsync(value);
    }

    // ── Recalcular sin ir a la BD (al cambiar J24 o J30) ─────────────────────
    private void RecalcularConsolidado()
    {
        if (_ultimaProyeccion is null || _ultimaConfig is null) return;
        if (_horasDocenteSemana <= 0 || _horasTecnicoSemana <= 0)   return;

        var tasaRet  = _ultimaConfig.MetaRetencionPorcentaje  ?? _ultimaConfig.TasaRetencionPorcentaje;
        var tasaGrad = _ultimaConfig.MetaGraduacionPorcentaje ?? _ultimaConfig.TasaGraduacionPorcentaje;

        DetalleConsolidado = ConsolidadorProyeccionEstudiantes.Calcular(
            _ultimaProyeccion,
            _ultimaConfig.ParalelosPeriodo1,
            _ultimaConfig.ParalelosPeriodo2,
            tasaRet,
            tasaGrad,
            horasDocSemanaOverride: _horasDocenteSemana,
            horasTecSemanaOverride: _horasTecnicoSemana);
    }

    // ── Carga consolidado desde BD ────────────────────────────────────────────
    private async Task CargarDetalleConsolidadoAsync(ProyeccionEstudiantesItemViewModel item)
    {
        EstaCargandoDetalle = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ucObtener   = scope.ServiceProvider.GetRequiredService<ObtenerProyeccionEstudiantesUseCase>();
            var repoConfig  = scope.ServiceProvider.GetRequiredService<IRepositorioConfiguracionRetencion>();

            var proyeccion = await ucObtener.EjecutarAsync(item.Id);
            if (proyeccion is null)
            {
                MensajeError = "No se encontró la proyección seleccionada.";
                return;
            }

            if (proyeccion.Detalles is null || proyeccion.Detalles.Count == 0)
            {
                MensajeError = "La proyección no tiene datos de estudiantes guardados. "
                             + "Elimine y vuelva a generar la proyección.";
                return;
            }

            var configs = await repoConfig.ListarDtoAsync();
            var config  = configs.FirstOrDefault(c =>
                c.CarreraId             == item.CarreraId &&
                c.EscenarioProyeccionId == item.EscenarioId);

            if (config is null)
            {
                MensajeError = $"No existe configuración de retención activa para "
                             + $"'{item.CarreraCodigo} — {item.EscenarioNombre}'. "
                             + "Cree la configuración antes de ver el análisis.";
                return;
            }

            // Guardar contexto para recalcular sin BD
            _ultimaProyeccion = proyeccion;
            _ultimaConfig     = config;

            var tasaRet  = config.MetaRetencionPorcentaje  ?? config.TasaRetencionPorcentaje;
            var tasaGrad = config.MetaGraduacionPorcentaje ?? config.TasaGraduacionPorcentaje;

            DetalleConsolidado = ConsolidadorProyeccionEstudiantes.Calcular(
                proyeccion,
                config.ParalelosPeriodo1,
                config.ParalelosPeriodo2,
                tasaRet,
                tasaGrad,
                horasDocSemanaOverride: _horasDocenteSemana,
                horasTecSemanaOverride: _horasTecnicoSemana);
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar análisis: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaCargandoDetalle = false;
        }
    }

    // ── Comandos ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!PuedeVer)
        {
            MensajeError = "Acceso denegado al módulo de Estudiantes.";
            return;
        }

        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        var idSeleccionadoAntes = ProyeccionSeleccionada?.Id;

        try
        {
            using var scope   = _serviceProvider.CreateScope();
            var repoCarrera   = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var repoEscenario = scope.ServiceProvider.GetRequiredService<IRepositorioEscenarioProyeccion>();
            var ucListar      = scope.ServiceProvider.GetRequiredService<ListarProyeccionesEstudiantesUseCase>();

            var carreras     = await repoCarrera.ListarAsync();
            var escenarios   = await repoEscenario.ListarAsync();
            var proyecciones = await ucListar.EjecutarAsync();

            Carreras = new ObservableCollection<CarreraOpcion>(
                carreras.OrderBy(c => c.Codigo)
                        .Select(c => new CarreraOpcion
                        {
                            Id          = c.Id,
                            Descripcion = $"{c.Codigo} — {c.Nombre}"
                        }));

            _todosLosEscenarios = escenarios
                .OrderBy(e => e.Nombre)
                .Select(e => new EscenarioOpcion
                {
                    Id          = e.Id,
                    CarreraId   = e.CarreraId,
                    Descripcion = e.Nombre
                })
                .ToList();

            Escenarios = new ObservableCollection<EscenarioOpcion>(_todosLosEscenarios);

            Proyecciones = new ObservableCollection<ProyeccionEstudiantesItemViewModel>(
                proyecciones.Select(p => new ProyeccionEstudiantesItemViewModel
                {
                    Id                 = p.Id,
                    CarreraId          = p.CarreraId,
                    EscenarioId        = p.EscenarioProyeccionId,
                    AnioBase           = p.AnioBase,
                    CarreraNombre      = p.CarreraNombre,
                    CarreraCodigo      = p.CarreraCodigo,
                    EscenarioNombre    = p.EscenarioNombre,
                    SemanasPorSemestre = p.SemanasPorSemestre,
                    CreadoEnTexto      = p.CreadoEn.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                }));

            if (idSeleccionadoAntes.HasValue)
                ProyeccionSeleccionada = Proyecciones
                    .FirstOrDefault(p => p.Id == idSeleccionadoAntes.Value);
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar datos: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task GenerarAsync()
    {
        if (!PuedeGenerar) { MensajeError = "No tiene permiso para generar proyecciones."; return; }
        if (EstaGenerando) return;

        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        if (CarreraSeleccionada    is null) { MensajeError = "Seleccione una carrera."; return; }
        if (EscenarioSeleccionado  is null) { MensajeError = "Seleccione un escenario de proyección."; return; }
        if (SimulacionSeleccionada is null) { MensajeError = "Seleccione una simulación de retención base."; return; }

        if (!int.TryParse(SemanasPorSemestre, out var semanas) || semanas < 8 || semanas > 30)
        {
            MensajeError = "Las semanas por semestre deben ser un número entre 8 y 30.";
            return;
        }

        EstaGenerando = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<GenerarProyeccionEstudiantesUseCase>();

            var id = await uc.EjecutarAsync(new GenerarProyeccionEstudiantesDto
            {
                CarreraId             = CarreraSeleccionada.Id,
                EscenarioProyeccionId = EscenarioSeleccionado.Id,
                SimulacionRetencionId = SimulacionSeleccionada.Id,
                SemanasPorSemestre    = semanas
            }, _sesionActual.UsuarioId);

            ProyeccionSeleccionada = new ProyeccionEstudiantesItemViewModel { Id = id };
            await CargarAsync();
            MensajeExito = $"Proyección generada correctamente (Id = {id}).";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al generar proyección: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaGenerando = false;
        }
    }

    [RelayCommand]
    private async Task EliminarAsync()
    {
        if (!PuedeEliminar) { MensajeError = "No tiene permiso para eliminar proyecciones."; return; }
        if (ProyeccionSeleccionada is null) { MensajeError = "Seleccione una proyección para eliminar."; return; }

        var confirmacion = MessageBox.Show(
            $"¿Desea eliminar la proyección de '{ProyeccionSeleccionada.CarreraCodigo} — {ProyeccionSeleccionada.EscenarioNombre}' (año base {ProyeccionSeleccionada.AnioBase})?",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmacion != MessageBoxResult.Yes) return;
        if (EstaEliminando) return;

        EstaEliminando = true;
        MensajeError   = string.Empty;
        MensajeExito   = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<EliminarProyeccionEstudiantesUseCase>();
            await uc.EjecutarAsync(ProyeccionSeleccionada.Id, _sesionActual.UsuarioId);
            DetalleConsolidado = null;
            _ultimaProyeccion  = null;
            _ultimaConfig      = null;
            await CargarAsync();
            MensajeExito = "Proyección eliminada correctamente.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al eliminar proyección: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaEliminando = false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task CargarSimulacionesAsync(int carreraId, int escenarioProyeccionId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepositorioSimulacionRetencion>();

            var lista = await repo.ListarResumenAsync(
                carreraId:             carreraId,
                escenarioProyeccionId: escenarioProyeccionId);

            Simulaciones = new ObservableCollection<SimulacionOpcion>(
                lista.OrderByDescending(s => s.FechaSimulacion)
                     .Select(s => new SimulacionOpcion
                     {
                         Id          = s.Id,
                         CohorteAnio = s.CohorteAnio,
                         Descripcion = $"Cohorte {s.CohorteAnio}  —  {s.FechaSimulacion:dd/MM/yyyy HH:mm}"
                     }));

            if (Simulaciones.Count > 0)
                SimulacionSeleccionada = Simulaciones[0];
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar simulaciones: {ObtenerDetalle(ex)}";
        }
    }

    private static string ObtenerDetalle(Exception ex)
        => ex.InnerException?.Message ?? ex.Message;
}
