using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.TasaRetencion;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.TasaRetencion;

public sealed class OpcionCarrera
{
    public int Id { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public int TotalCiclos { get; init; }
}

public sealed class OpcionEscenario
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
}

public sealed partial class ConfiguracionRetencionViewModel : ObservableObject
{
    private const string FmtDecimal = "0.####";

    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private List<OpcionEscenario> _catalogoEscenarios = new List<OpcionEscenario>();

    public SimulacionRetencionViewModel SimulacionVm { get; }

    public ConfiguracionRetencionViewModel(IServiceProvider serviceProvider, SesionActual sesionActual, SimulacionRetencionViewModel simulacionVm)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
        SimulacionVm = simulacionVm;
    }

    private int _tabInternoIndice;
    public int TabInternoIndice
    {
        get => _tabInternoIndice;
        set
        {
            if (SetProperty(ref _tabInternoIndice, value) && value == 1)
                _ = SimulacionVm.CargarCommand.ExecuteAsync(null);
        }
    }

    [ObservableProperty] private ObservableCollection<ConfiguracionRetencionDto> _configuraciones = [];
    [ObservableProperty] private ConfiguracionRetencionDto? _configuracionSeleccionada;

    [ObservableProperty] private ObservableCollection<OpcionCarrera> _carreras = [];
    [ObservableProperty] private ObservableCollection<OpcionEscenario> _escenarios = [];

    [ObservableProperty] private OpcionCarrera? _carreraSeleccionada;
    [ObservableProperty] private OpcionEscenario? _escenarioSeleccionado;

    [ObservableProperty] private string _totalCiclos = "9";
    [ObservableProperty] private string _tasaRetencion = "0";
    [ObservableProperty] private string _tasaGraduacion = "0";
    [ObservableProperty] private string _estudiantesPeriodo1 = "0";
    [ObservableProperty] private string _estudiantesPeriodo2 = "0";
    [ObservableProperty] private string _paralelosPeriodo1 = "0";
    [ObservableProperty] private string _paralelosPeriodo2 = "0";

    [ObservableProperty] private string _metaRetencion = "0";
    [ObservableProperty] private string _metaGraduacion = "0";

    // Tasas POR CICLO derivadas de las metas (solo lectura: alimentan el cálculo). Se recalculan al
    // cambiar la meta o el nº de ciclos, para que el usuario vea qué % por ciclo produce su meta.
    public string TasaRetencionDerivadaTexto => DerivarTasaTexto(MetaRetencion, esRetencion: true);
    public string TasaGraduacionDerivadaTexto => DerivarTasaTexto(MetaGraduacion, esRetencion: false);

    partial void OnMetaRetencionChanged(string value) => OnPropertyChanged(nameof(TasaRetencionDerivadaTexto));
    partial void OnMetaGraduacionChanged(string value) => OnPropertyChanged(nameof(TasaGraduacionDerivadaTexto));
    partial void OnTotalCiclosChanged(string value)
    {
        OnPropertyChanged(nameof(TasaRetencionDerivadaTexto));
        OnPropertyChanged(nameof(TasaGraduacionDerivadaTexto));
    }

    private string DerivarTasaTexto(string metaTexto, bool esRetencion)
    {
        if (!TryDecimal(metaTexto, out var meta) || meta <= 0m) return "-";
        if (!int.TryParse(TotalCiclos, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ciclos) || ciclos < 2)
            return "-";
        var pasos = esRetencion ? ConfiguracionRetencion.PasosRetencion(ciclos) : ConfiguracionRetencion.PasosGraduacion(ciclos);
        var tasa = ConfiguracionRetencion.TasaPorCicloDesdeMeta(meta, pasos);
        return $"{tasa.ToString("0.##", CultureInfo.InvariantCulture)} %";
    }

    [ObservableProperty] private bool _estaEditando;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;

    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private string _textoInformativoEscenario = string.Empty;

    public bool PuedeVer => _sesionActual.TienePermiso("TRE.VER") || _sesionActual.EsAdministrador;
    public bool PuedeEditar => _sesionActual.TienePermiso("TRE.EDITAR") || _sesionActual.TienePermiso("TRE.CREAR") || _sesionActual.EsAdministrador;
    public bool PuedeEliminar => _sesionActual.TienePermiso("TRE.ELIMINAR") || _sesionActual.EsAdministrador;

    public string TituloFormulario => EstaEditando ? "Editar configuración de retención y graduación" : "Nueva configuración de retención y graduación";

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!PuedeVer)
        {
            MensajeError = "Acceso denegado al módulo de Tasa de Retención.";
            return;
        }

        if (EstaCargando) return;
        EstaCargando = true;
        MensajeExito = string.Empty;
        MensajeError = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var listar = scope.ServiceProvider.GetRequiredService<ListarConfiguracionesRetencionUseCase>();
            var repoCarrera = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var repoEscenario = scope.ServiceProvider.GetRequiredService<IRepositorioEscenarioProyeccion>();

            var carreras = await repoCarrera.ListarAsync();
            var escenarios = await repoEscenario.ListarAsync();
            var lista = await listar.EjecutarAsync();

            Carreras = new ObservableCollection<OpcionCarrera>(
                carreras.Select(c => new OpcionCarrera { Id = c.Id, Descripcion = $"{c.Codigo} - {c.Nombre}", TotalCiclos = c.TotalCiclos }));
            _catalogoEscenarios = escenarios
                .Select(e => new OpcionEscenario
                {
                    Id = e.Id,
                    CarreraId = e.CarreraId,
                    Descripcion = e.Nombre
                })
                .ToList();

            ActualizarEscenariosPorCarrera(CarreraSeleccionada?.Id);
            Configuraciones = new ObservableCollection<ConfiguracionRetencionDto>(lista);

            await SimulacionVm.CargarCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar configuraciones: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private void Nuevo()
    {
        LimpiarFormulario();
    }

    [RelayCommand]
    private void SeleccionarParaEditar()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para editar configuraciones.";
            return;
        }

        if (ConfiguracionSeleccionada is null)
        {
            MensajeError = "Seleccione una configuración para editar.";
            return;
        }

        var c = ConfiguracionSeleccionada;
        EstaEditando = true;
        CarreraSeleccionada = Carreras.FirstOrDefault(x => x.Id == c.CarreraId);
        EscenarioSeleccionado = Escenarios.FirstOrDefault(x => x.Id == c.EscenarioProyeccionId);
        TotalCiclos = c.TotalCiclos.ToString(CultureInfo.InvariantCulture);
        TasaRetencion = c.TasaRetencionPorcentaje.ToString(FmtDecimal, CultureInfo.InvariantCulture);
        TasaGraduacion = c.TasaGraduacionPorcentaje.ToString(FmtDecimal, CultureInfo.InvariantCulture);
        EstudiantesPeriodo1 = c.EstudiantesPeriodo1.ToString(FmtDecimal, CultureInfo.InvariantCulture);
        EstudiantesPeriodo2 = c.EstudiantesPeriodo2.ToString(FmtDecimal, CultureInfo.InvariantCulture);
        ParalelosPeriodo1 = c.ParalelosPeriodo1.ToString(CultureInfo.InvariantCulture);
        ParalelosPeriodo2 = c.ParalelosPeriodo2.ToString(CultureInfo.InvariantCulture);
        MetaRetencion = (c.MetaRetencionPorcentaje ?? 0m).ToString(FmtDecimal, CultureInfo.InvariantCulture);
        MetaGraduacion = (c.MetaGraduacionPorcentaje ?? 0m).ToString(FmtDecimal, CultureInfo.InvariantCulture);
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    partial void OnCarreraSeleccionadaChanged(OpcionCarrera? value)
    {
        ActualizarEscenariosPorCarrera(value?.Id);

        if (value is null)
        {
            EscenarioSeleccionado = null;
            TextoInformativoEscenario = string.Empty;
            return;
        }

        if (EscenarioSeleccionado is not null && EscenarioSeleccionado.CarreraId != value.Id)
        {
            EscenarioSeleccionado = null;
            TextoInformativoEscenario = string.Empty;
        }

        // Propaga el nº de ciclos de la carrera al formulario (en modo Nuevo). Al editar se conserva
        // el valor guardado de la configuración; para Optimista/Pesimista la precarga desde el
        // Histórico lo reajusta si difiere.
        if (!EstaEditando)
            TotalCiclos = value.TotalCiclos.ToString(CultureInfo.InvariantCulture);

        // Carrera sin escenarios (creada antes de la siembra automática): se generan los 3
        // estándar al vuelo para que el desplegable no quede vacío. No afecta a las demás carreras.
        if (Escenarios.Count == 0)
            _ = AsegurarEscenariosAsync(value.Id);
    }

    private async Task AsegurarEscenariosAsync(int carreraId)
    {
        if (EstaEditando || !PuedeEditar)
            return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var sembrar = scope.ServiceProvider.GetRequiredService<SembrarEscenariosProyeccionCarreraCommand>();
            var creados = await sembrar.EjecutarAsync(carreraId, _sesionActual.UsuarioId);
            if (creados <= 0)
                return;

            var repoEscenario = scope.ServiceProvider.GetRequiredService<IRepositorioEscenarioProyeccion>();
            var escenarios = await repoEscenario.ListarAsync();
            _catalogoEscenarios = escenarios
                .Select(e => new OpcionEscenario { Id = e.Id, CarreraId = e.CarreraId, Descripcion = e.Nombre })
                .ToList();

            if (CarreraSeleccionada?.Id == carreraId)
                ActualizarEscenariosPorCarrera(carreraId);
        }
        catch
        {
            // Silencioso: si la siembra falla, el desplegable queda vacío como antes (no se rompe nada).
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para guardar configuraciones.";
            return;
        }

        if (EstaGuardando) return;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        if (!ValidarFormulario(out var datos))
            return;

        EstaGuardando = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            await PersistirConfiguracionAsync(scope, datos!);

            var fueActualizacion = EstaEditando && ConfiguracionSeleccionada is not null;
            await CargarAsync();
            LimpiarFormulario();
            MensajeExito = fueActualizacion
                ? "Configuración actualizada correctamente."
                : "Configuración creada correctamente.";
        }
        catch (Exception ex)
        {
            MensajeExito = string.Empty;
            MensajeError = $"Error al guardar configuración: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    private sealed record DatosFormulario(
        int Ciclos, decimal TasaRet, decimal TasaGrad, decimal MetaRet, decimal MetaGrad,
        decimal Est1, decimal Est2, int Par1, int Par2);

    private bool ValidarFormulario(out DatosFormulario? datos)
    {
        datos = null;

        if (CarreraSeleccionada is null) { MensajeError = "Seleccione una carrera."; return false; }
        if (EscenarioSeleccionado is null) { MensajeError = "Seleccione un escenario."; return false; }
        if (!int.TryParse(TotalCiclos, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ciclos))
        { MensajeError = "Total de ciclos invalido."; return false; }
        if (!TryDecimal(MetaRetencion, out var metaRet) || metaRet < 0m || metaRet > 100m)
        { MensajeError = "Meta de retención inválida (0% a 100%)."; return false; }
        if (!TryDecimal(MetaGraduacion, out var metaGrad) || metaGrad < 0m || metaGrad > 100m)
        { MensajeError = "Meta de graduación inválida (0% a 100%)."; return false; }
        if (!TryDecimal(EstudiantesPeriodo1, out var est1)) { MensajeError = "Estudiantes período 1 inválido."; return false; }
        if (!TryDecimal(EstudiantesPeriodo2, out var est2)) { MensajeError = "Estudiantes período 2 inválido."; return false; }
        if (!int.TryParse(ParalelosPeriodo1, NumberStyles.Integer, CultureInfo.InvariantCulture, out var par1))
        { MensajeError = "Paralelos período 1 inválido."; return false; }
        if (!int.TryParse(ParalelosPeriodo2, NumberStyles.Integer, CultureInfo.InvariantCulture, out var par2))
        { MensajeError = "Paralelos período 2 inválido."; return false; }

        // La tasa por ciclo (lo que alimenta el cálculo) se deriva de la meta acumulada.
        var tasaRet = ConfiguracionRetencion.TasaPorCicloDesdeMeta(metaRet, ConfiguracionRetencion.PasosRetencion(ciclos));
        var tasaGrad = ConfiguracionRetencion.TasaPorCicloDesdeMeta(metaGrad, ConfiguracionRetencion.PasosGraduacion(ciclos));

        datos = new DatosFormulario(ciclos, tasaRet, tasaGrad, metaRet, metaGrad, est1, est2, par1, par2);
        return true;
    }

    private async Task<int> PersistirConfiguracionAsync(IServiceScope scope, DatosFormulario d)
    {
        if (EstaEditando && ConfiguracionSeleccionada is not null)
        {
            var uc = scope.ServiceProvider.GetRequiredService<ActualizarConfiguracionRetencionUseCase>();
            await uc.EjecutarAsync(new ActualizarConfiguracionRetencionDto
            {
                Id = ConfiguracionSeleccionada.Id,
                CarreraId = CarreraSeleccionada!.Id,
                EscenarioProyeccionId = EscenarioSeleccionado!.Id,
                TotalCiclos = d.Ciclos,
                TasaRetencionPorcentaje = d.TasaRet,
                TasaGraduacionPorcentaje = d.TasaGrad,
                MetaRetencionPorcentaje = d.MetaRet,
                MetaGraduacionPorcentaje = d.MetaGrad,
                EstudiantesPeriodo1 = d.Est1,
                EstudiantesPeriodo2 = d.Est2,
                ParalelosPeriodo1 = d.Par1,
                ParalelosPeriodo2 = d.Par2
            }, _sesionActual.UsuarioId);
            return ConfiguracionSeleccionada.Id;
        }

        var ucCrear = scope.ServiceProvider.GetRequiredService<CrearConfiguracionRetencionUseCase>();
        return await ucCrear.EjecutarAsync(new CrearConfiguracionRetencionDto
        {
            CarreraId = CarreraSeleccionada!.Id,
            EscenarioProyeccionId = EscenarioSeleccionado!.Id,
            TotalCiclos = d.Ciclos,
            TasaRetencionPorcentaje = d.TasaRet,
            TasaGraduacionPorcentaje = d.TasaGrad,
            MetaRetencionPorcentaje = d.MetaRet,
            MetaGraduacionPorcentaje = d.MetaGrad,
            EstudiantesPeriodo1 = d.Est1,
            EstudiantesPeriodo2 = d.Est2,
            ParalelosPeriodo1 = d.Par1,
            ParalelosPeriodo2 = d.Par2
        }, _sesionActual.UsuarioId);
    }

    [RelayCommand]
    private async Task EliminarSeleccionadoAsync()
    {
        if (!PuedeEliminar)
        {
            MensajeError = "No tiene permiso para eliminar configuraciones.";
            return;
        }

        if (ConfiguracionSeleccionada is null)
        {
            MensajeError = "Seleccione una configuración para eliminar.";
            return;
        }

        var respuesta = MessageBox.Show(
            $"Eliminar la configuración de '{ConfiguracionSeleccionada.CarreraNombre}' / '{ConfiguracionSeleccionada.EscenarioNombre}'?",
            "Confirmar eliminacion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (respuesta != MessageBoxResult.Yes)
            return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<EliminarConfiguracionRetencionUseCase>();
            await uc.EjecutarAsync(ConfiguracionSeleccionada.Id, _sesionActual.UsuarioId);
            MensajeExito = "Configuración eliminada correctamente.";
            await CargarAsync();
            LimpiarFormulario();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al eliminar configuración: {ObtenerDetalle(ex)}";
        }
    }

    partial void OnEscenarioSeleccionadoChanged(OpcionEscenario? value)
    {
        TextoInformativoEscenario = value?.Descripcion switch
        {
            "Historico" => "Escenario base. Ingrese los valores reales de la carrera tomados del Excel institucional. Este escenario sirve como referencia para los demas.",
            "Optimista" => "Escenario derivado del histórico. Sube la retención 5% y la graduación 8%, y aumenta los estudiantes un 20%. Proyecta mayores ingresos por mejor permanencia y titulación.",
            "Pesimista" => "Escenario derivado del histórico. Baja la retención 10% y la graduación 15%, y reduce los estudiantes un 20%. Proyecta menor ingreso y exige mayor provisión presupuestaria.",
            _ => string.Empty
        };

        if (!EstaEditando)
            _ = PrecargarValoresSugeridosAsync();
    }

    private void ActualizarEscenariosPorCarrera(int? carreraId)
    {
        if (!carreraId.HasValue)
        {
            Escenarios = new ObservableCollection<OpcionEscenario>();
            return;
        }

        Escenarios = new ObservableCollection<OpcionEscenario>(
            _catalogoEscenarios
                .Where(e => e.CarreraId == carreraId.Value)
                .OrderByDescending(e => e.Descripcion == "Historico" || e.Descripcion == "Histórico")
                .ThenBy(e => e.Descripcion));
    }

    private async Task PrecargarValoresSugeridosAsync()
    {
        if (EstaEditando) return;
        if (CarreraSeleccionada is null || EscenarioSeleccionado is null) return;
        if (EscenarioSeleccionado.Descripcion == "Historico" || EscenarioSeleccionado.Descripcion == "Histórico") return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var uc = scope.ServiceProvider.GetRequiredService<ObtenerValoresSugeridosParaEscenarioUseCase>();
            var result = await uc.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Descripcion);

            // result.Retencion/Graduacion ahora representan METAS acumuladas (ajustadas desde el histórico).
            MetaRetencion = result.Retencion.ToString(FmtDecimal, CultureInfo.InvariantCulture);
            MetaGraduacion = result.Graduacion.ToString(FmtDecimal, CultureInfo.InvariantCulture);
            EstudiantesPeriodo1 = result.EstP1.ToString(FmtDecimal, CultureInfo.InvariantCulture);
            EstudiantesPeriodo2 = result.EstP2.ToString(FmtDecimal, CultureInfo.InvariantCulture);
            ParalelosPeriodo1 = result.ParP1.ToString(CultureInfo.InvariantCulture);
            ParalelosPeriodo2 = result.ParP2.ToString(CultureInfo.InvariantCulture);
            TotalCiclos = result.Ciclos.ToString(CultureInfo.InvariantCulture);
            MensajeError = string.Empty;
        }
        catch
        {
            MensajeError = "No se puede precargar valores: primero registre el escenario Historico de esta carrera.";
        }
    }

    private void LimpiarFormulario()
    {
        EstaEditando = false;
        ConfiguracionSeleccionada = null;
        CarreraSeleccionada = null;
        EscenarioSeleccionado = null;
        TotalCiclos = "9";
        TasaRetencion = "0";
        TasaGraduacion = "0";
        EstudiantesPeriodo1 = "0";
        EstudiantesPeriodo2 = "0";
        ParalelosPeriodo1 = "0";
        ParalelosPeriodo2 = "0";
        MetaRetencion = "0";
        MetaGraduacion = "0";
        MensajeExito = string.Empty;
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    private static bool TryDecimal(string valor, out decimal resultado)
    {
        valor = (valor ?? string.Empty).Replace(',', '.');
        return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out resultado);
    }

    private static string ObtenerDetalle(Exception ex)
        => ex.InnerException?.Message ?? ex.Message;
}
