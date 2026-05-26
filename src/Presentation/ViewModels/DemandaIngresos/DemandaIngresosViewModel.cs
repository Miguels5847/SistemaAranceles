using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.DemandaIngresos;

/// <summary>
/// ViewModel composite para la vista "Demanda e Ingresos" (Épica 9).
/// KAN-32 implementa la pestaña de Configuración de Arancel.
/// Las demás pestañas quedan como placeholder hasta KAN-33/34/35.
/// </summary>
public sealed partial class DemandaIngresosViewModel : ObservableObject
{
    private const string ModoManual = "Manual";
    private const string ModoAutomatico = "AutomaticoCostoCarrera";

    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private bool _suprimirCambios;

    public DemandaIngresosViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty] private ObservableCollection<Carrera> _carreras = [];
    [ObservableProperty] private Carrera? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioProyeccion> _escenarios = [];
    [ObservableProperty] private EscenarioProyeccion? _escenarioSeleccionado;

    [ObservableProperty] private ObservableCollection<ConfiguracionArancelCarreraDto> _configuraciones = [];
    [ObservableProperty] private ConfiguracionArancelCarreraDto? _configuracionSeleccionada;
    [ObservableProperty] private ArancelEfectivoDto? _arancelEfectivo;
    [ObservableProperty] private PresupuestosCarreraDto? _presupuestos;
    [ObservableProperty] private IngresosProyectadosDto? _ingresos;

    [ObservableProperty] private bool _formVisible;
    [ObservableProperty] private bool _formEsEdicion;
    [ObservableProperty] private int _formId;
    [ObservableProperty] private string _formModoCalculo = ModoManual;
    [ObservableProperty] private string _formArancelManual = "0";
    [ObservableProperty] private bool _formUsaPorcentajeInstitucional = true;
    [ObservableProperty] private string _formPorcentajeMatricula = "10";
    [ObservableProperty] private bool _formEscenarioGlobal = true;

    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;

    public IReadOnlyList<string> ModosCalculo { get; } = [ModoManual, ModoAutomatico];
    public string TituloFormulario => FormEsEdicion ? "Editar configuración" : "Nueva configuración";
    public bool FormEsModoManual => string.Equals(FormModoCalculo, ModoManual, StringComparison.OrdinalIgnoreCase);
    public bool FormPorcentajeMatriculaEditable => !FormUsaPorcentajeInstitucional;
    public bool NoEstaGuardando => !EstaGuardando;

    public bool PuedeEditar => _sesionActual.EsAdministrador
                            || _sesionActual.TienePermiso("DI_NG.EDITAR");

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        _ = value;
        if (_suprimirCambios || EstaCargando) return;
        _ = RecargarPorCarreraAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioProyeccion? value)
    {
        _ = value;
        if (_suprimirCambios || EstaCargando) return;
        _ = RefrescarArancelEfectivoAsync();
    }

    partial void OnFormEsEdicionChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    partial void OnFormModoCalculoChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(FormEsModoManual));
    }

    partial void OnFormUsaPorcentajeInstitucionalChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(FormPorcentajeMatriculaEditable));
    }

    partial void OnEstaGuardandoChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(NoEstaGuardando));
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando) return;
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
            finally { _suprimirCambios = false; }

            if (CarreraSeleccionada is null)
            {
                MensajeError = "No hay carreras registradas. Crea una carrera primero.";
                Configuraciones = [];
                ArancelEfectivo = null;
                return;
            }

            await RecargarPorCarreraAsync();
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

    private async Task RecargarPorCarreraAsync()
    {
        if (CarreraSeleccionada is null)
        {
            Configuraciones = [];
            Escenarios = [];
            EscenarioSeleccionado = null;
            ArancelEfectivo = null;
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repoEscenario = scope.ServiceProvider.GetRequiredService<IRepositorioEscenarioProyeccion>();
            var todosEscenarios = await repoEscenario.ListarAsync();
            var escenarios = todosEscenarios.Where(e => e.CarreraId == CarreraSeleccionada.Id).ToList();

            var escenarioActualId = EscenarioSeleccionado?.Id;
            _suprimirCambios = true;
            try
            {
                Escenarios = new ObservableCollection<EscenarioProyeccion>(escenarios);
                EscenarioSeleccionado = Escenarios.FirstOrDefault(e => e.Id == escenarioActualId)
                    ?? Escenarios.FirstOrDefault();
            }
            finally { _suprimirCambios = false; }

            var listarQuery = scope.ServiceProvider.GetRequiredService<ListarConfiguracionesArancelCarreraQuery>();
            var configs = await listarQuery.EjecutarAsync(CarreraSeleccionada.Id);
            Configuraciones = new ObservableCollection<ConfiguracionArancelCarreraDto>(configs);

            await RefrescarArancelEfectivoAsync();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar configuraciones: {Detalle(ex)}";
        }
    }

    private async Task RefrescarArancelEfectivoAsync()
    {
        if (CarreraSeleccionada is null)
        {
            ArancelEfectivo = null;
            Presupuestos = null;
            Ingresos = null;
            return;
        }
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryArancel = scope.ServiceProvider.GetRequiredService<ObtenerArancelEfectivoQuery>();
            ArancelEfectivo = await queryArancel.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado?.Id);

            var queryPresup = scope.ServiceProvider.GetRequiredService<ObtenerPresupuestosCarreraQuery>();
            Presupuestos = await queryPresup.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado?.Id);

            var queryIngresos = scope.ServiceProvider.GetRequiredService<CalcularIngresosProyectadosQuery>();
            Ingresos = await queryIngresos.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado?.Id);
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al calcular arancel/presupuestos/ingresos: {Detalle(ex)}";
            ArancelEfectivo = null;
            Presupuestos = null;
            Ingresos = null;
        }
    }

    [RelayCommand]
    private void AbrirNuevaConfiguracion()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tienes permiso para editar.";
            return;
        }
        if (CarreraSeleccionada is null)
        {
            MensajeError = "Selecciona una carrera.";
            return;
        }

        FormId = 0;
        FormModoCalculo = ModoManual;
        FormArancelManual = "0";
        FormUsaPorcentajeInstitucional = true;
        FormPorcentajeMatricula = "10";
        FormEscenarioGlobal = EscenarioSeleccionado is null;
        FormEsEdicion = false;
        FormVisible = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }

    [RelayCommand]
    private void AbrirEditarConfiguracion(ConfiguracionArancelCarreraDto? dto)
    {
        if (dto is null) return;
        if (!PuedeEditar)
        {
            MensajeError = "No tienes permiso para editar.";
            return;
        }

        FormId = dto.Id;
        FormModoCalculo = dto.ModoCalculoArancel;
        FormArancelManual = dto.ArancelManual?.ToString("0.##", CultureInfo.InvariantCulture) ?? "0";
        FormUsaPorcentajeInstitucional = dto.UsaPorcentajeMatriculaInstitucional;
        FormPorcentajeMatricula = (dto.PorcentajeMatricula ?? 10m).ToString("0.##", CultureInfo.InvariantCulture);
        FormEscenarioGlobal = dto.EscenarioProyeccionId is null;
        FormEsEdicion = true;
        FormVisible = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }

    [RelayCommand]
    private void CancelarFormulario()
    {
        FormVisible = false;
        MensajeError = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarConfiguracionAsync()
    {
        if (CarreraSeleccionada is null)
        {
            MensajeError = "Selecciona una carrera.";
            return;
        }
        if (string.Equals(FormModoCalculo, ModoManual, StringComparison.OrdinalIgnoreCase)
            && (!TryDecimal(FormArancelManual, out var arancel) || arancel <= 0m))
        {
            MensajeError = "Arancel manual inválido (debe ser > 0).";
            return;
        }

        decimal? porcMatricula = null;
        if (!FormUsaPorcentajeInstitucional)
        {
            if (!TryDecimal(FormPorcentajeMatricula, out var p) || p < 0m || p > 100m)
            {
                MensajeError = "Porcentaje matrícula entre 0 y 100.";
                return;
            }
            porcMatricula = p;
        }

        EstaGuardando = true;
        MensajeError = string.Empty;
        try
        {
            var dto = new GuardarConfiguracionArancelCarreraDto
            {
                Id = FormEsEdicion ? FormId : null,
                CarreraId = CarreraSeleccionada.Id,
                EscenarioProyeccionId = FormEscenarioGlobal ? null : EscenarioSeleccionado?.Id,
                ModoCalculoArancel = FormModoCalculo,
                ArancelManual = TryDecimal(FormArancelManual, out var a) ? a : (decimal?)null,
                PorcentajeMatricula = porcMatricula,
                UsaPorcentajeMatriculaInstitucional = FormUsaPorcentajeInstitucional
            };

            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<GuardarConfiguracionArancelCarreraCommand>();
            await command.EjecutarAsync(dto, _sesionActual.UsuarioId);

            FormVisible = false;
            MensajeExito = FormEsEdicion ? "Configuración actualizada." : "Configuración creada.";
            await RecargarPorCarreraAsync();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    [RelayCommand]
    private async Task EliminarConfiguracionAsync(ConfiguracionArancelCarreraDto? dto)
    {
        if (dto is null) return;
        if (!PuedeEditar)
        {
            MensajeError = "No tienes permiso para eliminar.";
            return;
        }

        EstaGuardando = true;
        MensajeError = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<EliminarConfiguracionArancelCarreraCommand>();
            await command.EjecutarAsync(dto.Id, _sesionActual.UsuarioId);
            MensajeExito = "Configuración eliminada.";
            await RecargarPorCarreraAsync();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    private static bool TryDecimal(string? value, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var limpio = value.Trim()
            .Replace("$", string.Empty)
            .Replace(" ", string.Empty);

        if (string.IsNullOrWhiteSpace(limpio)) return false;

        var ultimoPunto = limpio.LastIndexOf('.');
        var ultimaComa = limpio.LastIndexOf(',');

        if (ultimoPunto >= 0 && ultimaComa >= 0)
        {
            limpio = ultimaComa > ultimoPunto
                ? limpio.Replace(".", string.Empty).Replace(',', '.')
                : limpio.Replace(",", string.Empty);
        }
        else if (ultimaComa >= 0)
        {
            limpio = limpio.Replace(',', '.');
        }
        else if (ultimoPunto >= 0)
        {
            var decimales = limpio.Length - ultimoPunto - 1;
            if (decimales == 3)
                limpio = limpio.Replace(".", string.Empty);
        }

        return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private static string Detalle(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}
