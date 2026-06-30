using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.UseCases.TasaRetencion;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.TasaRetencion;

public sealed class OpcionConfiguracionRetencion
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public int EscenarioProyeccionId { get; init; }
    public int TotalCiclos { get; init; }
    public decimal TasaRetencionPorcentaje { get; init; }
    public decimal TasaGraduacionPorcentaje { get; init; }
    public decimal MetaRetencionPorcentaje { get; init; }
    public decimal MetaGraduacionPorcentaje { get; init; }
    public decimal EstudiantesPeriodo1 { get; init; }
    public decimal EstudiantesPeriodo2 { get; init; }
    public int ParalelosPeriodo1 { get; init; }
    public int ParalelosPeriodo2 { get; init; }
    public string Descripcion { get; init; } = string.Empty;
}

public sealed class ColumnaComportamientoRetencion
{
    public string TituloCiclo { get; init; } = string.Empty;
    public string AlumnosTexto { get; init; } = string.Empty;
    public string FondoAlumnos { get; init; } = "#C5E1A5";
}

public sealed partial class SimulacionRetencionViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;

    public SimulacionRetencionViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty] private ObservableCollection<OpcionConfiguracionRetencion> _configuraciones = [];
    [ObservableProperty] private OpcionConfiguracionRetencion? _configuracionSeleccionada;

    [ObservableProperty] private string _cohorteAnio = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture);

    [ObservableProperty] private ObservableCollection<ResumenSimulacionRetencionDto> _simulaciones = [];
    [ObservableProperty] private ResumenSimulacionRetencionDto? _simulacionSeleccionada;
    [ObservableProperty] private int? _selectedSimulationId;
    [ObservableProperty] private ObservableCollection<DetalleSimulacionRetencionDto> _detallesSimulacionSeleccionada = [];
    [ObservableProperty] private ObservableCollection<ColumnaComportamientoRetencion> _tablaComportamiento = [];

    [ObservableProperty] private int _columnasComportamiento = 1;
    [ObservableProperty] private string _paraleloAbr = "1";
    [ObservableProperty] private string _paraleloSep = "2";
    [ObservableProperty] private string _retencionResumenTexto = "0.0%";
    [ObservableProperty] private string _graduacionResumenTexto = "0.0%";
    [ObservableProperty] private string _resumenCarrera = "-";
    [ObservableProperty] private string _resumenEscenario = "-";
    [ObservableProperty] private string _resumenAnioIngreso = "-";
    [ObservableProperty] private string _resumenRetencion = "-";
    [ObservableProperty] private string _resumenGraduacion = "-";

    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaSimulando;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;

    public bool PuedeVer => _sesionActual.TienePermiso("TRE.VER") || _sesionActual.EsAdministrador;
    public bool PuedeEditar => _sesionActual.TienePermiso("TRE.EDITAR") || _sesionActual.TienePermiso("TRE.CREAR") || _sesionActual.EsAdministrador;
    public bool PuedeEliminar => _sesionActual.TienePermiso("TRE.ELIMINAR") || _sesionActual.EsAdministrador;

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!PuedeVer)
        {
            MensajeError = "Acceso denegado al módulo de Simulación de Retención.";
            return;
        }

        if (EstaCargando) return;
        EstaCargando = true;
        MensajeExito = string.Empty;
        MensajeError = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ucConfig = scope.ServiceProvider.GetRequiredService<ListarConfiguracionesRetencionUseCase>();
            var configs = await ucConfig.EjecutarAsync();

            Configuraciones = new ObservableCollection<OpcionConfiguracionRetencion>(
                configs.Select(c => new OpcionConfiguracionRetencion
                {
                    Id = c.Id,
                    CarreraId = c.CarreraId,
                    EscenarioProyeccionId = c.EscenarioProyeccionId,
                    TotalCiclos = c.TotalCiclos,
                    TasaRetencionPorcentaje = c.TasaRetencionPorcentaje,
                    TasaGraduacionPorcentaje = c.TasaGraduacionPorcentaje,
                    MetaRetencionPorcentaje = c.MetaRetencionPorcentaje ?? 0m,
                    MetaGraduacionPorcentaje = c.MetaGraduacionPorcentaje ?? 0m,
                    EstudiantesPeriodo1 = c.EstudiantesPeriodo1,
                    EstudiantesPeriodo2 = c.EstudiantesPeriodo2,
                    ParalelosPeriodo1 = c.ParalelosPeriodo1,
                    ParalelosPeriodo2 = c.ParalelosPeriodo2,
                    Descripcion = $"{c.CarreraCodigo} - {c.EscenarioNombre} (Ciclos: {c.TotalCiclos})"
                }));

            if (ConfiguracionSeleccionada is null && Configuraciones.Count > 0)
                ConfiguracionSeleccionada = Configuraciones[0];

            await CargarSimulacionesAsync();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar simulación: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    partial void OnConfiguracionSeleccionadaChanged(OpcionConfiguracionRetencion? value)
    {
        ActualizarResumenCabecera();
        _ = CargarSimulacionesAsync();
    }

    partial void OnSimulacionSeleccionadaChanged(ResumenSimulacionRetencionDto? value)
    {
        SelectedSimulationId = value?.Id;
        ActualizarResumenCabecera();
    }

    partial void OnSelectedSimulationIdChanged(int? value)
    {
        if (value is null)
        {
            SimulacionSeleccionada = null;
            return;
        }

        if (SimulacionSeleccionada?.Id == value.Value)
            return;

        SimulacionSeleccionada = Simulaciones.FirstOrDefault(x => x.Id == value.Value);
    }

    partial void OnCohorteAnioChanged(string value)
    {
        ActualizarResumenCabecera();
    }

    [RelayCommand]
    private async Task CargarSimulacionesAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<ListarSimulacionesRetencionUseCase>();

            var lista = await uc.EjecutarAsync(carreraId: null, escenarioProyeccionId: null, cohorteAnio: null);
            Simulaciones = new ObservableCollection<ResumenSimulacionRetencionDto>(lista);

            if (SelectedSimulationId is not null)
                SimulacionSeleccionada = Simulaciones.FirstOrDefault(x => x.Id == SelectedSimulationId.Value);

            if (SimulacionSeleccionada is null)
            {
                SelectedSimulationId = null;
                DetallesSimulacionSeleccionada = [];
                TablaComportamiento = [];
                ColumnasComportamiento = 1;
            }

            ActualizarResumenCabecera();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar simulaciones: {ObtenerDetalle(ex)}";
        }
    }

    [RelayCommand]
    private async Task EjecutarSimulacionAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para ejecutar simulaciones.";
            return;
        }

        if (ConfiguracionSeleccionada is null)
        {
            MensajeError = "Seleccione una configuración para simular.";
            return;
        }

        if (!int.TryParse(CohorteAnio, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cohorte))
        {
            MensajeError = "Grupo de ingreso invalido.";
            return;
        }

        EstaSimulando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<CrearSimulacionRetencionUseCase>();
            var id = await uc.EjecutarAsync(new CrearSimulacionRetencionDto
            {
                ConfiguracionRetencionId = ConfiguracionSeleccionada.Id,
                CohorteAnio = cohorte
            }, _sesionActual.UsuarioId);

            await CargarSimulacionesAsync();
            SimulacionSeleccionada = Simulaciones.FirstOrDefault(x => x.Id == id);
            SelectedSimulationId = SimulacionSeleccionada?.Id;
            await VerDetalleSimulacionAsync();
            MensajeExito = "Simulación ejecutada correctamente.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al ejecutar simulación: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaSimulando = false;
        }
    }

    [RelayCommand]
    private async Task VerDetalleSimulacionAsync()
    {
        if (SimulacionSeleccionada is null)
        {
            DetallesSimulacionSeleccionada = [];
            TablaComportamiento = [];
            ColumnasComportamiento = 1;
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<ObtenerSimulacionRetencionUseCase>();
            var simulacion = await uc.EjecutarAsync(SimulacionSeleccionada.Id);
            DetallesSimulacionSeleccionada = new ObservableCollection<DetalleSimulacionRetencionDto>(simulacion.Detalles);
            ConstruirTablaComportamiento(simulacion);
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar detalle de simulación: {ObtenerDetalle(ex)}";
        }
    }

    [RelayCommand]
    private async Task EliminarSimulacionSeleccionadaAsync()
    {
        if (!PuedeEliminar)
        {
            MensajeError = "No tiene permiso para eliminar simulaciones.";
            return;
        }

        if (SelectedSimulationId is null || SelectedSimulationId <= 0)
        {
            MensajeError = "Seleccione una simulación";
            return;
        }

        var simulacionObjetivo = Simulaciones.FirstOrDefault(x => x.Id == SelectedSimulationId.Value);
        if (simulacionObjetivo is null)
        {
            MensajeError = "Seleccione una simulación";
            return;
        }

        var respuesta = MessageBox.Show(
            $"¿Eliminar la simulacion del grupo de ingreso {simulacionObjetivo.CohorteAnio}?",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (respuesta != MessageBoxResult.Yes)
            return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<EliminarSimulacionRetencionUseCase>();
            await uc.EjecutarAsync(simulacionObjetivo.Id, _sesionActual.UsuarioId);
            await CargarSimulacionesAsync();
            SelectedSimulationId = null;
            SimulacionSeleccionada = null;
            DetallesSimulacionSeleccionada = [];
            MensajeExito = "Simulación eliminada correctamente.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al eliminar simulación: {ObtenerDetalle(ex)}";
        }
    }

    [RelayCommand]
    private async Task LimpiarSimulacionesConfiguracionAsync()
    {
        if (!PuedeEliminar)
        {
            MensajeError = "No tiene permiso para limpiar simulaciones.";
            return;
        }

        if (ConfiguracionSeleccionada is null)
        {
            MensajeError = "Seleccione una configuración para limpiar simulaciones.";
            return;
        }

        var respuesta = MessageBox.Show(
            "¿Eliminar todas las simulaciones de la configuración seleccionada?",
            "Confirmar limpieza",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (respuesta != MessageBoxResult.Yes)
            return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<LimpiarSimulacionesRetencionUseCase>();
            var total = await uc.EjecutarAsync(ConfiguracionSeleccionada.Id, _sesionActual.UsuarioId);
            await CargarSimulacionesAsync();
            DetallesSimulacionSeleccionada = [];
            MensajeExito = $"Se limpiaron {total} simulaciones de la configuración seleccionada.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al limpiar simulaciones: {ObtenerDetalle(ex)}";
        }
    }

    private static string ObtenerDetalle(Exception ex)
        => ex.InnerException?.Message ?? ex.Message;

    private void ConstruirTablaComportamiento(SimulacionRetencionDto simulacion)
    {
        var configuracion = Configuraciones.FirstOrDefault(x => x.Id == simulacion.ConfiguracionRetencionId);
        if (configuracion is null)
        {
            TablaComportamiento = [];
            ColumnasComportamiento = 1;
            return;
        }

        ParaleloAbr = configuracion.ParalelosPeriodo1.ToString(CultureInfo.InvariantCulture);
        ParaleloSep = configuracion.ParalelosPeriodo2.ToString(CultureInfo.InvariantCulture);

        var totalCiclos = Math.Max(2, configuracion.TotalCiclos);
        var mitad = totalCiclos / 2;
        var tasaRetencionFactor = configuracion.TasaRetencionPorcentaje / 100m;
        var tasaGraduacionFactor = configuracion.TasaGraduacionPorcentaje / 100m;

        var valorPeriodo1 = configuracion.EstudiantesPeriodo1;
        var valorPeriodo2 = configuracion.EstudiantesPeriodo2;
        var columnas = new List<ColumnaComportamientoRetencion>(totalCiclos * 2);

        // Tasas REALIZADAS (acumuladas): último/primer estudiante de cada mitad, igual que el Excel.
        // valInicio = ciclo 1 (valor previo al bucle); valFinal = valorPeriodo1 al terminar (ciclo final).
        var valInicio = valorPeriodo1;
        decimal valMitad = valorPeriodo1, valMitadMas1 = valorPeriodo1;

        for (var ciclo = 1; ciclo <= totalCiclos; ciclo++)
        {
            if (ciclo == mitad) valMitad = valorPeriodo1;
            if (ciclo == mitad + 1) valMitadMas1 = valorPeriodo1;

            columnas.Add(CrearColumna(ciclo, valorPeriodo1, esPeriodoSeptiembre: false));

            if (ciclo < totalCiclos)
                columnas.Add(CrearColumna(ciclo, valorPeriodo2, esPeriodoSeptiembre: true));

            if (ciclo >= totalCiclos)
                continue;

            var tasa = ciclo <= mitad ? tasaRetencionFactor : tasaGraduacionFactor;
            valorPeriodo1 = decimal.Round(valorPeriodo1 * tasa, 4);
            valorPeriodo2 = decimal.Round(valorPeriodo2 * tasa, 4);
        }

        // Se muestra la META (lo que el usuario fijó); si no hay meta, la tasa realizada (último/primer).
        RetencionResumenTexto = FormatearPorcentaje(MetaOTasa(configuracion.MetaRetencionPorcentaje, RatioPorcentaje(valMitad, valInicio)));
        GraduacionResumenTexto = FormatearPorcentaje(MetaOTasa(configuracion.MetaGraduacionPorcentaje, RatioPorcentaje(valorPeriodo1, valMitadMas1)));

        TablaComportamiento = new ObservableCollection<ColumnaComportamientoRetencion>(columnas);
        ColumnasComportamiento = Math.Max(1, columnas.Count);
    }

    private static ColumnaComportamientoRetencion CrearColumna(int ciclo, decimal valorAlumnos, bool esPeriodoSeptiembre)
    {
        var valorRedondeado = decimal.Round(valorAlumnos, 0, MidpointRounding.AwayFromZero);
        return new ColumnaComportamientoRetencion
        {
            TituloCiclo = $"{ciclo}°",
            AlumnosTexto = valorRedondeado.ToString("0", CultureInfo.InvariantCulture),
            FondoAlumnos = esPeriodoSeptiembre ? "#00A651" : "#C5E1A5"
        };
    }

    private static string FormatearPorcentaje(decimal valor)
        => $"{valor.ToString("0.0", CultureInfo.GetCultureInfo("es-EC"))}%";

    private static decimal RatioPorcentaje(decimal parte, decimal baseValor)
        => baseValor <= 0m ? 0m : decimal.Round(parte / baseValor * 100m, 1);

    private static decimal MetaOTasa(decimal meta, decimal tasaFallback)
        => meta > 0m ? meta : tasaFallback;

    private void ActualizarResumenCabecera()
    {
        var simulacion = SimulacionSeleccionada;
        var configuracion = ConfiguracionSeleccionada;

        // La cabecera muestra la META acumuladada (objetivo) cuando existe; si no, la tasa por ciclo.
        if (simulacion is not null)
        {
            ResumenCarrera = string.IsNullOrWhiteSpace(simulacion.CarreraCodigo) ? "-" : simulacion.CarreraCodigo;
            ResumenEscenario = string.IsNullOrWhiteSpace(simulacion.EscenarioNombre) ? "-" : simulacion.EscenarioNombre;
            ResumenAnioIngreso = simulacion.CohorteAnio.ToString(CultureInfo.InvariantCulture);
            ResumenRetencion = FormatearPorcentaje(MetaOTasa(configuracion?.MetaRetencionPorcentaje ?? 0m, simulacion.TasaRetencionConfigurada));
            ResumenGraduacion = FormatearPorcentaje(MetaOTasa(configuracion?.MetaGraduacionPorcentaje ?? 0m, simulacion.TasaGraduacionConfigurada));
            return;
        }

        if (configuracion is not null)
        {
            var partes = configuracion.Descripcion.Split(" - ", 2, StringSplitOptions.TrimEntries);
            ResumenCarrera = partes.Length > 0 && !string.IsNullOrWhiteSpace(partes[0]) ? partes[0] : "-";
            ResumenEscenario = partes.Length > 1 && !string.IsNullOrWhiteSpace(partes[1]) ? partes[1] : "-";
            ResumenAnioIngreso = int.TryParse(CohorteAnio, out var cohorte)
                ? cohorte.ToString(CultureInfo.InvariantCulture)
                : "-";
            ResumenRetencion = FormatearPorcentaje(MetaOTasa(configuracion.MetaRetencionPorcentaje, configuracion.TasaRetencionPorcentaje));
            ResumenGraduacion = FormatearPorcentaje(MetaOTasa(configuracion.MetaGraduacionPorcentaje, configuracion.TasaGraduacionPorcentaje));
            return;
        }

        ResumenCarrera = "-";
        ResumenEscenario = "-";
        ResumenAnioIngreso = "-";
        ResumenRetencion = "-";
        ResumenGraduacion = "-";
    }
}
