using System.Collections.ObjectModel;
using System.Globalization;
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

// ── Opciones para ComboBoxes ────────────────────────────────────────────
public sealed class CarreraOpcion
{
    public int Id { get; init; }
    public string Descripcion { get; init; } = string.Empty;
}

public sealed class EscenarioOpcion
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
}

public sealed class SimulacionOpcion
{
    public int Id { get; init; }
    public int CohorteAnio { get; init; }
    public string Descripcion { get; init; } = string.Empty;
}

// ── Ítem de la lista de proyecciones ───────────────────────────────────
public sealed class ProyeccionEstudiantesItemViewModel
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public int EscenarioId { get; init; }
    public int AnioBase { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public string CarreraCodigo { get; init; } = string.Empty;
    public string EscenarioNombre { get; init; } = string.Empty;
    public int SemanasPorSemestre { get; init; }
    public string CreadoEnTexto { get; init; } = string.Empty;
}

// ── ViewModel principal ─────────────────────────────────────────────
public sealed partial class EstudiantesViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;

    private IReadOnlyList<EscenarioOpcion> _todosLosEscenarios = [];

    // Guardamos el último contexto de cálculo para poder recalcular sin ir a la BD
    private ProyeccionEstudiantesDto? _ultimaProyeccion;
    private ConfiguracionRetencionDto? _ultimaConfig;
    private decimal[]? _overrideHorasDocencia;
    private decimal[]? _overrideHorasPractica;

    public EstudiantesViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    // ── Listas ──────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ProyeccionEstudiantesItemViewModel> _proyecciones = [];
    [ObservableProperty] private ProyeccionEstudiantesItemViewModel? _proyeccionSeleccionada;

    [ObservableProperty] private ObservableCollection<CarreraOpcion> _carreras = [];
    [ObservableProperty] private CarreraOpcion? _carreraSeleccionada;

    [ObservableProperty] private ObservableCollection<EscenarioOpcion> _escenarios = [];
    [ObservableProperty] private EscenarioOpcion? _escenarioSeleccionado;

    [ObservableProperty] private ObservableCollection<SimulacionOpcion> _simulaciones = [];
    [ObservableProperty] private SimulacionOpcion? _simulacionSeleccionada;

    // ── Formulario ───────────────────────────────────────────────
    [ObservableProperty] private string _semanasPorSemestre = "16";

    // ── Parámetros editables J24 / J30 ──────────────────────────────────
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

    // ── Estado ──────────────────────────────────────────────────
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGenerando;
    [ObservableProperty] private bool _estaEliminando;
    [ObservableProperty] private bool _estaCargandoDetalle;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;

    // ── Consolidado (tablas) ──────────────────────────────────────────
    [ObservableProperty] private ProyeccionConsolidadaDto? _detalleConsolidado;
    [ObservableProperty] private FilaConsumoPeriodicDto? _periodoConsumoSeleccionado;

    [ObservableProperty] private bool _estaEditandoHorasMalla;
    [ObservableProperty] private string _horasDocenciaEdicion = string.Empty;
    [ObservableProperty] private string _horasPracticaEdicion = string.Empty;
    [ObservableProperty] private string _resumenPeriodoEdicion = string.Empty;

    public bool TieneDetalleConsolidado => DetalleConsolidado is not null;
    public bool PuedeEditarConsumo =>
        _sesionActual.EsAdministrador
        || _sesionActual.TienePermiso("ES.EDITAR")
        || _sesionActual.TienePermiso("ES.CREAR");
    public bool PuedeAbrirEdicionHorasMalla => PuedeEditarConsumo && PeriodoConsumoSeleccionado is not null;

    public decimal TotalHorasDocenciaConsumo =>
        DetalleConsolidado?.TablaPeriodos.Sum(x => x.HorasDocencia) ?? 0m;
    public decimal TotalHorasPracticaConsumo =>
        DetalleConsolidado?.TablaPeriodos.Sum(x => x.HorasPractica) ?? 0m;
    public decimal TotalHorasCombinadasConsumo => TotalHorasDocenciaConsumo + TotalHorasPracticaConsumo;

    partial void OnDetalleConsolidadoChanged(ProyeccionConsolidadaDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(TieneDetalleConsolidado));
        OnPropertyChanged(nameof(TotalHorasDocenciaConsumo));
        OnPropertyChanged(nameof(TotalHorasPracticaConsumo));
        OnPropertyChanged(nameof(TotalHorasCombinadasConsumo));
        ReaplicarSeleccionConsumo();
    }

    partial void OnPeriodoConsumoSeleccionadoChanged(FilaConsumoPeriodicDto? value)
    {
        if (!EstaEditandoHorasMalla)
        {
            ResumenPeriodoEdicion = value is null
                ? string.Empty
                : $"Período {value.Periodo} · {value.Anio} · {value.Semestre}";
        }
        OnPropertyChanged(nameof(PuedeAbrirEdicionHorasMalla));
    }

    partial void OnEstaEditandoHorasMallaChanged(bool value)
    {
        if (!value)
        {
            HorasDocenciaEdicion = string.Empty;
            HorasPracticaEdicion = string.Empty;
        }
    }

    // ── Permisos ────────────────────────────────────────────────
    public bool PuedeVer => _sesionActual.TienePermiso("ES.VER") || _sesionActual.EsAdministrador;
    public bool PuedeGenerar => _sesionActual.TienePermiso("ES.CREAR") || _sesionActual.EsAdministrador;
    public bool PuedeEliminar => _sesionActual.TienePermiso("ES.ELIMINAR") || _sesionActual.EsAdministrador;

    // ── Cascada: Carrera → Escenarios ───────────────────────────────────
    partial void OnCarreraSeleccionadaChanged(CarreraOpcion? value)
    {
        EscenarioSeleccionado = null;
        SimulacionSeleccionada = null;
        Simulaciones.Clear();
        MensajeError = string.Empty;

        Escenarios = value is null
            ? new ObservableCollection<EscenarioOpcion>(_todosLosEscenarios)
            : new ObservableCollection<EscenarioOpcion>(
                _todosLosEscenarios.Where(e => e.CarreraId == value.Id));
    }

    // ── Cascada: Escenario → Simulaciones ───────────────────────────────
    partial void OnEscenarioSeleccionadoChanged(EscenarioOpcion? value)
    {
        SimulacionSeleccionada = null;
        Simulaciones.Clear();
        MensajeError = string.Empty;

        if (value is not null && CarreraSeleccionada is not null)
            _ = CargarSimulacionesAsync(CarreraSeleccionada.Id, value.Id);
    }

    // ── Selección de proyección → cargar consolidado ──────────────────────
    partial void OnProyeccionSeleccionadaChanged(ProyeccionEstudiantesItemViewModel? value)
    {
        DetalleConsolidado = null;
        MensajeError = string.Empty;
        // Solo cargar detalle si el ítem tiene datos completos (CarreraId > 0).
        // Evita disparar la búsqueda de config con un ítem fantasma (solo Id).
        if (value is not null && value.CarreraId > 0)
            _ = CargarDetalleConsolidadoAsync(value);
    }

    // ── Recalcular sin ir a la BD (al cambiar J24 o J30) ───────────────────
    private void RecalcularConsolidado()
    {
        if (_ultimaProyeccion is null || _ultimaConfig is null) return;
        if (_horasDocenteSemana <= 0 || _horasTecnicoSemana <= 0) return;

        var tasaRet = _ultimaConfig.MetaRetencionPorcentaje ?? _ultimaConfig.TasaRetencionPorcentaje;
        var tasaGrad = _ultimaConfig.MetaGraduacionPorcentaje ?? _ultimaConfig.TasaGraduacionPorcentaje;

        DetalleConsolidado = ConsolidadorProyeccionEstudiantes.Calcular(
            _ultimaProyeccion,
            _ultimaConfig.ParalelosPeriodo1,
            _ultimaConfig.ParalelosPeriodo2,
            tasaRet,
            tasaGrad,
            horasDocSemestralesOverride: _overrideHorasDocencia,
            horasTecSemestralesOverride: _overrideHorasPractica,
            horasDocSemanaOverride: _horasDocenteSemana,
            horasTecSemanaOverride: _horasTecnicoSemana);

        ReaplicarSeleccionConsumo();
    }

    // ── Carga consolidado desde BD ───────────────────────────────────────
    private async Task CargarDetalleConsolidadoAsync(ProyeccionEstudiantesItemViewModel item)
    {
        EstaCargandoDetalle = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ucObtener = scope.ServiceProvider.GetRequiredService<ObtenerProyeccionEstudiantesUseCase>();
            var ucOverrides = scope.ServiceProvider.GetRequiredService<ListarOverridesHorasPeriodoUseCase>();
            var repoConfig = scope.ServiceProvider.GetRequiredService<IRepositorioConfiguracionRetencion>();

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
            var config = configs.FirstOrDefault(c =>
                c.CarreraId == item.CarreraId &&
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
            _ultimaConfig = config;

            var overrides = await ucOverrides.EjecutarAsync(item.Id);
            (_overrideHorasDocencia, _overrideHorasPractica) = ConstruirArreglosOverride(overrides, proyeccion);

            var tasaRet = config.MetaRetencionPorcentaje ?? config.TasaRetencionPorcentaje;
            var tasaGrad = config.MetaGraduacionPorcentaje ?? config.TasaGraduacionPorcentaje;

            DetalleConsolidado = ConsolidadorProyeccionEstudiantes.Calcular(
                proyeccion,
                config.ParalelosPeriodo1,
                config.ParalelosPeriodo2,
                tasaRet,
                tasaGrad,
                horasDocSemestralesOverride: _overrideHorasDocencia,
                horasTecSemestralesOverride: _overrideHorasPractica,
                horasDocSemanaOverride: _horasDocenteSemana,
                horasTecSemanaOverride: _horasTecnicoSemana);

            ReaplicarSeleccionConsumo();
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

    // ── Comandos ──────────────────────────────────────────────────
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
            using var scope = _serviceProvider.CreateScope();
            var repoCarrera = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var repoEscenario = scope.ServiceProvider.GetRequiredService<IRepositorioEscenarioProyeccion>();
            var ucListar = scope.ServiceProvider.GetRequiredService<ListarProyeccionesEstudiantesUseCase>();

            var carreras = await repoCarrera.ListarAsync();
            var escenarios = await repoEscenario.ListarAsync();
            var proyecciones = await ucListar.EjecutarAsync();

            Carreras = new ObservableCollection<CarreraOpcion>(
                carreras.OrderBy(c => c.Codigo)
                        .Select(c => new CarreraOpcion
                        {
                            Id = c.Id,
                            Descripcion = $"{c.Codigo} — {c.Nombre}"
                        }));

            _todosLosEscenarios = escenarios
                .OrderBy(e => e.Nombre)
                .Select(e => new EscenarioOpcion
                {
                    Id = e.Id,
                    CarreraId = e.CarreraId,
                    Descripcion = e.Nombre
                })
                .ToList();

            Escenarios = new ObservableCollection<EscenarioOpcion>(_todosLosEscenarios);

            Proyecciones = new ObservableCollection<ProyeccionEstudiantesItemViewModel>(
                proyecciones.Select(p => new ProyeccionEstudiantesItemViewModel
                {
                    Id = p.Id,
                    CarreraId = p.CarreraId,
                    EscenarioId = p.EscenarioProyeccionId,
                    AnioBase = p.AnioBase,
                    CarreraNombre = p.CarreraNombre,
                    CarreraCodigo = p.CarreraCodigo,
                    EscenarioNombre = p.EscenarioNombre,
                    SemanasPorSemestre = p.SemanasPorSemestre,
                    CreadoEnTexto = p.CreadoEn.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
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

        if (CarreraSeleccionada is null) { MensajeError = "Seleccione una carrera."; return; }
        if (EscenarioSeleccionado is null) { MensajeError = "Seleccione un escenario de proyección."; return; }
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
                CarreraId = CarreraSeleccionada.Id,
                EscenarioProyeccionId = EscenarioSeleccionado.Id,
                SimulacionRetencionId = SimulacionSeleccionada.Id,
                SemanasPorSemestre = semanas
            }, _sesionActual.UsuarioId);

            // FIX: primero recargar la lista completa desde BD,
            // luego buscar el ítem ya poblado por su Id.
            // Así OnProyeccionSeleccionadaChanged recibe un objeto completo
            // (CarreraId > 0) y no dispara el error de config fantasma.
            await CargarAsync();
            ProyeccionSeleccionada = Proyecciones.FirstOrDefault(p => p.Id == id);
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
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<EliminarProyeccionEstudiantesUseCase>();
            await uc.EjecutarAsync(ProyeccionSeleccionada.Id, _sesionActual.UsuarioId);
            DetalleConsolidado = null;
            _ultimaProyeccion = null;
            _ultimaConfig = null;
            _overrideHorasDocencia = null;
            _overrideHorasPractica = null;
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

    // ── Helpers ──────────────────────────────────────────────────
    private async Task CargarSimulacionesAsync(int carreraId, int escenarioProyeccionId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepositorioSimulacionRetencion>();

            var lista = await repo.ListarResumenAsync(
                carreraId: carreraId,
                escenarioProyeccionId: escenarioProyeccionId);

            Simulaciones = new ObservableCollection<SimulacionOpcion>(
                lista.OrderByDescending(s => s.FechaSimulacion)
                     .Select(s => new SimulacionOpcion
                     {
                         Id = s.Id,
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

    private void ReaplicarSeleccionConsumo(int? periodoPreferido = null)
    {
        if (DetalleConsolidado is null || DetalleConsolidado.TablaPeriodos.Count == 0)
        {
            PeriodoConsumoSeleccionado = null;
            return;
        }

        var periodoObjetivo = periodoPreferido ?? PeriodoConsumoSeleccionado?.Periodo;
        if (periodoObjetivo is null)
        {
            PeriodoConsumoSeleccionado = DetalleConsolidado.TablaPeriodos[0];
            return;
        }

        PeriodoConsumoSeleccionado = DetalleConsolidado.TablaPeriodos
            .FirstOrDefault(x => x.Periodo == periodoObjetivo.Value)
            ?? DetalleConsolidado.TablaPeriodos[0];
    }

    [RelayCommand]
    private void AbrirEdicionHorasMalla()
    {
        if (!PuedeEditarConsumo)
        {
            MensajeError = "No tiene permiso para editar consumo por período.";
            return;
        }

        if (PeriodoConsumoSeleccionado is null)
        {
            MensajeError = "Seleccione un período para editar.";
            return;
        }

        MensajeError = string.Empty;
        ResumenPeriodoEdicion = $"Período {PeriodoConsumoSeleccionado.Periodo} · {PeriodoConsumoSeleccionado.Anio} · {PeriodoConsumoSeleccionado.Semestre}";
        HorasDocenciaEdicion = PeriodoConsumoSeleccionado.HorasDocencia.ToString("N2", CultureInfo.CurrentCulture);
        HorasPracticaEdicion = PeriodoConsumoSeleccionado.HorasPractica.ToString("N2", CultureInfo.CurrentCulture);
        EstaEditandoHorasMalla = true;
    }

    [RelayCommand]
    private void CancelarEdicionHorasMalla()
    {
        EstaEditandoHorasMalla = false;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarEdicionHorasMallaAsync()
    {
        if (PeriodoConsumoSeleccionado is null)
        {
            MensajeError = "Seleccione un período para editar.";
            return;
        }

        if (!decimal.TryParse(HorasDocenciaEdicion, NumberStyles.Number, CultureInfo.CurrentCulture, out var horasDoc)
            && !decimal.TryParse(HorasDocenciaEdicion, NumberStyles.Number, CultureInfo.InvariantCulture, out horasDoc))
        {
            MensajeError = "H. Docencia debe ser un valor numérico.";
            return;
        }

        if (!decimal.TryParse(HorasPracticaEdicion, NumberStyles.Number, CultureInfo.CurrentCulture, out var horasPrac)
            && !decimal.TryParse(HorasPracticaEdicion, NumberStyles.Number, CultureInfo.InvariantCulture, out horasPrac))
        {
            MensajeError = "H. Práctica debe ser un valor numérico.";
            return;
        }

        if (horasDoc <= 0m || horasPrac <= 0m)
        {
            MensajeError = "H. Docencia y H. Práctica deben ser mayores a 0.";
            return;
        }

        var periodo = PeriodoConsumoSeleccionado.Periodo;
        var ok = await EditarConsumoAsync(periodo, horasDoc, horasPrac);
        if (!ok) return;

        ReaplicarSeleccionConsumo(periodo);
        EstaEditandoHorasMalla = false;
        MensajeError = string.Empty;
        MensajeExito = $"Horas malla actualizadas para período {periodo}.";
    }

    // ── CU-ES-04: override de horas por período ───────────────────────────
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

    public async Task<bool> EditarConsumoAsync(int periodo, decimal? horasDocencia, decimal? horasPractica)
    {
        if (_ultimaProyeccion is null) return false;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ucEditar = scope.ServiceProvider.GetRequiredService<EditarConsumoPeriodoUseCase>();
            var ucOverrides = scope.ServiceProvider.GetRequiredService<ListarOverridesHorasPeriodoUseCase>();

            await ucEditar.EjecutarAsync(
                _ultimaProyeccion.Id, periodo, horasDocencia, horasPractica, _sesionActual.UsuarioId);

            var overrides = await ucOverrides.EjecutarAsync(_ultimaProyeccion.Id);
            (_overrideHorasDocencia, _overrideHorasPractica) =
                ConstruirArreglosOverride(overrides, _ultimaProyeccion);

            RecalcularConsolidado();
            ReaplicarSeleccionConsumo(periodo);
            return true;
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al editar consumo: {ObtenerDetalle(ex)}";
            return false;
        }
    }

    public async Task<bool> RestaurarConsumoAsync(int periodo)
    {
        if (_ultimaProyeccion is null) return false;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ucRestaurar = scope.ServiceProvider.GetRequiredService<RestaurarConsumoPeriodoUseCase>();
            var ucOverrides = scope.ServiceProvider.GetRequiredService<ListarOverridesHorasPeriodoUseCase>();

            await ucRestaurar.EjecutarAsync(_ultimaProyeccion.Id, periodo, _sesionActual.UsuarioId);

            var overrides = await ucOverrides.EjecutarAsync(_ultimaProyeccion.Id);
            (_overrideHorasDocencia, _overrideHorasPractica) =
                ConstruirArreglosOverride(overrides, _ultimaProyeccion);

            RecalcularConsolidado();
            ReaplicarSeleccionConsumo(periodo);
            return true;
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al restaurar consumo: {ObtenerDetalle(ex)}";
            return false;
        }
    }
}
