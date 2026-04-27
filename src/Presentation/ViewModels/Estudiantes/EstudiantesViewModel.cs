using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.Estudiantes;

// ── Opciones para ComboBoxes ──────────────────────────────────────────────────
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

// ── Ítem de la lista de proyecciones ────────────────────────────────────────────
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

// ── ViewModel principal ──────────────────────────────────────────────────────────
public sealed partial class EstudiantesViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual     _sesionActual;

    private IReadOnlyList<EscenarioOpcion> _todosLosEscenarios = [];

    public EstudiantesViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual    = sesionActual;
    }

    // ── Listas ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ProyeccionEstudiantesItemViewModel> _proyecciones = [];
    [ObservableProperty] private ProyeccionEstudiantesItemViewModel? _proyeccionSeleccionada;

    [ObservableProperty] private ObservableCollection<CarreraOpcion>   _carreras   = [];
    [ObservableProperty] private CarreraOpcion?                        _carreraSeleccionada;

    [ObservableProperty] private ObservableCollection<EscenarioOpcion> _escenarios = [];
    [ObservableProperty] private EscenarioOpcion?                      _escenarioSeleccionado;

    [ObservableProperty] private ObservableCollection<SimulacionOpcion> _simulaciones = [];
    [ObservableProperty] private SimulacionOpcion?                      _simulacionSeleccionada;

    // ── Formulario ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _semanasPorSemestre = "16";

    // ── Estado ────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _estaCargando;
    [ObservableProperty] private bool   _estaGenerando;
    [ObservableProperty] private bool   _estaEliminando;
    [ObservableProperty] private bool   _estaCargandoDetalle;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;

    // ── Consolidado (5 tablas) ──────────────────────────────────────────────────
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

    // ── Cascada: Carrera → Escenarios ─────────────────────────────────────────────
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

    // ── Cascada: Escenario → Simulaciones ─────────────────────────────────────────
    partial void OnEscenarioSeleccionadoChanged(EscenarioOpcion? value)
    {
        SimulacionSeleccionada = null;
        Simulaciones.Clear();
        MensajeError = string.Empty;

        if (value is not null && CarreraSeleccionada is not null)
            _ = CargarSimulacionesAsync(CarreraSeleccionada.Id, value.Id);
    }

    // ── Selección de proyección → cargar consolidado ───────────────────────────
    partial void OnProyeccionSeleccionadaChanged(ProyeccionEstudiantesItemViewModel? value)
    {
        DetalleConsolidado = null;
        MensajeError       = string.Empty;
        if (value is not null)
            _ = CargarDetalleConsolidadoAsync(value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FIX-1: recibe el ViewModel directamente para tener CarreraId y EscenarioId
    //        sin depender de que ListarDtoAsync devuelva el match correcto.
    // FIX-2: busca config por Id de carrera + Id de escenario (no por nombre).
    // FIX-3: fallback a TasaRetencion/TasaGraduacion cuando no hay CriterioReferencia.
    // ─────────────────────────────────────────────────────────────────────────
    private async Task CargarDetalleConsolidadoAsync(ProyeccionEstudiantesItemViewModel item)
    {
        EstaCargandoDetalle = true;
        try
        {
            using var scope   = _serviceProvider.CreateScope();
            var ucObtener     = scope.ServiceProvider.GetRequiredService<ObtenerProyeccionEstudiantesUseCase>();
            var repoConfig    = scope.ServiceProvider.GetRequiredService<IRepositorioConfiguracionRetencion>();

            // Carga la proyección con sus Detalles (NumeroPeriodo, NumeroCiclo, TotalEstudiantes)
            var proyeccion = await ucObtener.EjecutarAsync(item.Id);
            if (proyeccion is null)
            {
                MensajeError = "No se encontró la proyección seleccionada.";
                return;
            }

            // Diagnóstico: sin detalles no hay nada que mostrar
            if (proyeccion.Detalles is null || proyeccion.Detalles.Count == 0)
            {
                MensajeError = "La proyección no tiene datos de estudiantes guardados. "
                             + "Elimine y vuelva a generar la proyección.";
                return;
            }

            // FIX-1: busca la config usando CarreraId + EscenarioId del item
            //        (el item ya tiene ambos IDs, no depende del DTO de proyección)
            var configs = await repoConfig.ListarDtoAsync();
            var config  = configs.FirstOrDefault(c =>
                c.CarreraId            == item.CarreraId &&
                c.EscenarioProyeccionId == item.EscenarioId);

            if (config is null)
            {
                MensajeError = $"No existe configuración de retención activa para "
                             + $"'{item.CarreraCodigo} — {item.EscenarioNombre}'. "
                             + "Cree la configuración antes de ver el análisis.";
                return;
            }

            // FIX-3: fallback cuando no hay CriterioReferencia
            var tasaRetencion  = config.MetaRetencionPorcentaje  ?? config.TasaRetencionPorcentaje;
            var tasaGraduacion = config.MetaGraduacionPorcentaje ?? config.TasaGraduacionPorcentaje;

            DetalleConsolidado = ConsolidadorProyeccionEstudiantes.Calcular(
                proyeccion,
                config.ParalelosPeriodo1,
                config.ParalelosPeriodo2,
                tasaRetencion,
                tasaGraduacion);
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

        // FIX-4: guarda el Id seleccionado antes de recargar la lista
        var idSeleccionadoAntes = ProyeccionSeleccionada?.Id;

        try
        {
            using var scope    = _serviceProvider.CreateScope();
            var repoCarrera    = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var repoEscenario  = scope.ServiceProvider.GetRequiredService<IRepositorioEscenarioProyeccion>();
            var ucListar       = scope.ServiceProvider.GetRequiredService<ListarProyeccionesEstudiantesUseCase>();

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

            // FIX-4: re-selecciona la misma proyección para que el consolidado
            //        se recargue automáticamente tras Generar o Refrescar
            if (idSeleccionadoAntes.HasValue)
            {
                ProyeccionSeleccionada = Proyecciones
                    .FirstOrDefault(p => p.Id == idSeleccionadoAntes.Value);
            }
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

            // Recarga la lista; FIX-4 se encarga de re-seleccionar la nueva proyección
            // Para auto-seleccionar la recién generada, forzamos su id antes de recargar
            // (CargarAsync guarda idSeleccionadoAntes = ProyeccionSeleccionada?.Id)
            // No necesitamos nada extra: basta con poner un item temporal con el id nuevo
            // y dejar que CargarAsync haga el match.
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
