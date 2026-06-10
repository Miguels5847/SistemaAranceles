using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.UseCases.CapitalTrabajo;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Presentation.ViewModels.CapitalTrabajo;

public sealed partial class CapitalTrabajoViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private bool _suprimirRecargaAutomatica;

    public CapitalTrabajoViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [ObservableProperty] private ObservableCollection<Carrera> _carreras = [];
    [ObservableProperty] private Carrera? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioProyeccion> _escenarios = [];
    [ObservableProperty] private EscenarioProyeccion? _escenarioSeleccionado;
    [ObservableProperty] private string _periodoBaseDisplay = "Sin periodo base";

    [ObservableProperty] private ObservableCollection<FilaGastoServicioAdministracionDto> _gastosServicioAdministracion = [];
    [ObservableProperty] private ObservableCollection<ItemCapitalTrabajoDto> _materialesSuministros = [];
    [ObservableProperty] private ObservableCollection<ItemCapitalTrabajoDto> _aseoLimpieza = [];
    [ObservableProperty] private ObservableCollection<ItemCapitalTrabajoDto> _accesoriosMateriales = [];

    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private string _mensajeInfo = string.Empty;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;
    [ObservableProperty] private ResumenCapitalTrabajoDto? _resumen;

    [ObservableProperty] private bool _formMatVisible;
    [ObservableProperty] private bool _formMatEsEdicion;
    [ObservableProperty] private int _formMatId;
    [ObservableProperty] private string _formMatNombre = string.Empty;
    [ObservableProperty] private string _formMatCantidad = "0";
    [ObservableProperty] private string _formMatPrecio = "0";
    [ObservableProperty] private string _formMatCategoria = string.Empty;

    public string TituloFormularioMaterial => FormMatEsEdicion ? "Editar item" : "Nuevo item";
    public bool FormMaterialesVisible => FormMatVisible && FormMatCategoria == ObtenerCapitalTrabajoPorCarreraQuery.CategoriaMateriales;
    public bool FormAseoVisible => FormMatVisible && FormMatCategoria == ObtenerCapitalTrabajoPorCarreraQuery.CategoriaAseo;
    public bool FormAccesoriosVisible => FormMatVisible && FormMatCategoria == ObtenerCapitalTrabajoPorCarreraQuery.CategoriaAccesorios;
    public bool PuedeTrabajar => CarreraSeleccionada is not null && !EstaCargando;

    partial void OnFormMatEsEdicionChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(TituloFormularioMaterial));
    }

    partial void OnFormMatVisibleChanged(bool value)
    {
        _ = value;
        NotificarVisibilidadFormulario();
    }

    partial void OnFormMatCategoriaChanged(string value)
    {
        _ = value;
        NotificarVisibilidadFormulario();
    }

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeTrabajar));
        if (_suprimirRecargaAutomatica)
            return;

        _ = CargarEscenariosAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioProyeccion? value)
    {
        _ = value;
        if (_suprimirRecargaAutomatica)
            return;

        _ = RefrescarCapitalTrabajoAsync();
    }

    partial void OnEstaCargandoChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeTrabajar));
    }

    [RelayCommand]
    private async Task CargarAsync(int? carreraId = null)
    {
        if (EstaCargando)
            return;

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        MensajeInfo = string.Empty;

        try
        {
            var teniaCarrerasCargadas = Carreras.Count > 0;
            var carreraActualId = carreraId ?? CarreraSeleccionada?.Id;
            using var scope = _serviceProvider.CreateScope();
            var queryCarreras = scope.ServiceProvider.GetRequiredService<ListarCarrerasConProyeccionQuery>();
            var carreras = await queryCarreras.EjecutarAsync();

            _suprimirRecargaAutomatica = true;
            try
            {
                Carreras = new ObservableCollection<Carrera>(carreras);
                CarreraSeleccionada = carreraActualId is > 0
                    ? Carreras.FirstOrDefault(c => c.Id == carreraActualId.Value)
                    : null;
            }
            finally
            {
                _suprimirRecargaAutomatica = false;
            }

            if (CarreraSeleccionada is null)
            {
                LimpiarVista();
                MensajeInfo = Carreras.Count == 0
                    ? "No hay carreras con proyección de estudiantes para calcular Capital de Trabajo."
                    : teniaCarrerasCargadas
                        ? "Selecciona una carrera para refrescar la información."
                        : "Selecciona una carrera para cargar Capital de Trabajo.";
                return;
            }

            await CargarEscenariosAsync();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
            LimpiarVista();
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
            LimpiarVista();
            MensajeInfo = "Selecciona una carrera para cargar Capital de Trabajo.";
            return;
        }

        var carreraId = CarreraSeleccionada.Id;
        var escenarioActualId = EscenarioSeleccionado?.Id;
        try
        {
            MensajeInfo = string.Empty;
            using var scope = _serviceProvider.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ListarEscenariosConProyeccionPorCarreraQuery>();
            var escenarios = await query.EjecutarAsync(carreraId);

            // Anti-stale: la carrera cambió mientras se listaban escenarios
            if (CarreraSeleccionada?.Id != carreraId)
                return;

            _suprimirRecargaAutomatica = true;
            try
            {
                Escenarios = new ObservableCollection<EscenarioProyeccion>(escenarios);
                // KAN-46: sin auto-selección — el usuario elige el escenario y recién ahí se carga
                EscenarioSeleccionado = Escenarios.FirstOrDefault(e => e.Id == escenarioActualId);
            }
            finally
            {
                _suprimirRecargaAutomatica = false;
            }

            if (EscenarioSeleccionado is null && Escenarios.Count > 0)
            {
                LimpiarBloques();
                MensajeInfo = "Selecciona el escenario para cargar Capital de Trabajo.";
                return;
            }

            await RefrescarCapitalTrabajoAsync();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar escenarios: {Detalle(ex)}";
            Escenarios = [];
            EscenarioSeleccionado = null;
            await RefrescarCapitalTrabajoAsync();
        }
    }

    private async Task RefrescarCapitalTrabajoAsync()
    {
        if (CarreraSeleccionada is null)
        {
            LimpiarVista();
            MensajeInfo = "Selecciona una carrera para refrescar la información.";
            return;
        }

        MensajeError = string.Empty;
        MensajeInfo = string.Empty;
        var carreraId = CarreraSeleccionada.Id;
        var escenarioId = EscenarioSeleccionado?.Id;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ObtenerCapitalTrabajoPorCarreraQuery>();
            var capital = await query.EjecutarAsync(carreraId, escenarioId);

            // Anti-stale: la selección cambió durante la consulta
            if (CarreraSeleccionada?.Id != carreraId || EscenarioSeleccionado?.Id != escenarioId)
                return;

            PeriodoBaseDisplay = capital.PeriodoBaseDisplay;
            GastosServicioAdministracion = new ObservableCollection<FilaGastoServicioAdministracionDto>(capital.GastosServicioAdministracion);
            MaterialesSuministros = new ObservableCollection<ItemCapitalTrabajoDto>(capital.MaterialesSuministros);
            AseoLimpieza = new ObservableCollection<ItemCapitalTrabajoDto>(capital.AseoLimpieza);
            AccesoriosMateriales = new ObservableCollection<ItemCapitalTrabajoDto>(capital.AccesoriosMateriales);
            Resumen = capital.Resumen;
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
            LimpiarBloques();
        }
    }

    [RelayCommand]
    private async Task GenerarMaterialesPorDefectoAsync()
    {
        if (CarreraSeleccionada is null)
        {
            MensajeError = "Seleccione una carrera antes de generar materiales.";
            return;
        }

        EstaGuardando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<GenerarMaterialesPorDefectoCarreraCommand>();
            var insertados = await command.EjecutarAsync(CarreraSeleccionada.Id);
            MensajeExito = insertados > 0
                ? $"Se generaron {insertados} materiales por defecto."
                : "La carrera ya tiene los materiales por defecto.";
            await RefrescarCapitalTrabajoAsync();
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
    private void AbrirNuevoMaterial(string categoria)
    {
        if (CarreraSeleccionada is null)
        {
            MensajeError = "Seleccione una carrera antes de crear items.";
            return;
        }

        FormMatId = 0;
        FormMatNombre = string.Empty;
        FormMatCantidad = "0";
        FormMatPrecio = "0";
        FormMatCategoria = categoria;
        FormMatEsEdicion = false;
        FormMatVisible = true;
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(TituloFormularioMaterial));
    }

    [RelayCommand]
    private void AbrirEditarMaterial(ItemCapitalTrabajoDto item)
    {
        FormMatId = item.Id;
        FormMatNombre = item.Concepto;
        FormMatCantidad = item.Cantidad.ToString("0.####", CultureInfo.InvariantCulture);
        FormMatPrecio = item.ValorUnitario.ToString("0.##", CultureInfo.InvariantCulture);
        FormMatCategoria = item.CategoriaNombre;
        FormMatEsEdicion = true;
        FormMatVisible = true;
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(TituloFormularioMaterial));
    }

    [RelayCommand]
    private void CancelarFormMat()
    {
        FormMatVisible = false;
        MensajeError = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarMaterialAsync()
    {
        if (CarreraSeleccionada is null)
        {
            MensajeError = "Seleccione una carrera.";
            return;
        }

        if (!TryDecimalFlexible(FormMatCantidad, out var cantidad) || cantidad < 0m)
        {
            MensajeError = "Cantidad invalida. Puede escribir valores como 10, 10.5 o 10,5.";
            return;
        }

        if (!TryDecimalFlexible(FormMatPrecio, out var precio) || precio < 0m)
        {
            MensajeError = "Valor unitario invalido. Puede escribir valores como 1000, 1.000, 1000.50 o 1.000,50.";
            return;
        }

        EstaGuardando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<GuardarItemCapitalTrabajoCommand>();
            await command.EjecutarAsync(new GuardarItemCapitalTrabajoDto
            {
                Id = FormMatEsEdicion ? FormMatId : null,
                CarreraId = CarreraSeleccionada.Id,
                CategoriaNombre = FormMatCategoria,
                Concepto = FormMatNombre,
                Cantidad = cantidad,
                ValorUnitario = precio
            });

            FormMatVisible = false;
            MensajeExito = FormMatEsEdicion ? "Item actualizado." : "Item creado.";
            await RefrescarCapitalTrabajoAsync();
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
    private async Task EliminarMaterialAsync(ItemCapitalTrabajoDto item)
    {
        EstaGuardando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<EliminarItemCapitalTrabajoCommand>();
            await command.EjecutarAsync(item.Id);
            MensajeExito = "Item eliminado.";
            await RefrescarCapitalTrabajoAsync();
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

    private void LimpiarVista()
    {
        Escenarios = [];
        EscenarioSeleccionado = null;
        PeriodoBaseDisplay = "Sin periodo base";
        LimpiarBloques();
    }

    private void LimpiarBloques()
    {
        GastosServicioAdministracion = [];
        MaterialesSuministros = [];
        AseoLimpieza = [];
        AccesoriosMateriales = [];
        Resumen = null;
    }

    private static bool TryDecimalFlexible(string? value, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var limpio = value.Trim()
            .Replace("$", string.Empty)
            .Replace(" ", string.Empty);

        if (string.IsNullOrWhiteSpace(limpio))
            return false;

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

    private void NotificarVisibilidadFormulario()
    {
        OnPropertyChanged(nameof(FormMaterialesVisible));
        OnPropertyChanged(nameof(FormAseoVisible));
        OnPropertyChanged(nameof(FormAccesoriosVisible));
    }
}
