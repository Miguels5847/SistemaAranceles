using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Mantenimiento;
using SistemaAranceles.Application.UseCases.Mantenimiento;
using SistemaAranceles.Domain.Enums;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.Mantenimiento;

public sealed class TipoRubroOpcion
{
    public TipoRubroMantenimiento Valor { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}

public sealed partial class MantenimientoViewModel : ObservableObject
{
    private readonly IServiceProvider _sp;
    private readonly SesionActual _sesion;
    private int _carreraId = 1;

    public MantenimientoViewModel(IServiceProvider sp, SesionActual sesion)
    {
        _sp = sp;
        _sesion = sesion;

        TiposRubro = new ObservableCollection<TipoRubroOpcion>([
            new TipoRubroOpcion { Valor = TipoRubroMantenimiento.ServicioBasico, Etiqueta = "Servicio Básico" },
            new TipoRubroOpcion { Valor = TipoRubroMantenimiento.Mantenimiento,  Etiqueta = "Mantenimiento"  },
        ]);
        TipoRubroForm = TiposRubro[0];
    }

    // --- Listas ---
    [ObservableProperty] private ObservableCollection<ServicioMantenimientoDto> _serviciosBasicos = [];
    [ObservableProperty] private ObservableCollection<ServicioMantenimientoDto> _itemsMantenimiento = [];
    [ObservableProperty] private ServicioMantenimientoDto? _seleccionado;

    // --- Resumen / proyección ---
    [ObservableProperty] private ResumenMantenimientoDto? _resumen;

    // --- Estado ---
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;

    // --- Formulario ---
    [ObservableProperty] private bool _formVisible;
    [ObservableProperty] private bool _formEsEdicion;
    [ObservableProperty] private int _formId;
    [ObservableProperty] private string _formNombre = string.Empty;
    [ObservableProperty] private string _formCosto = "0";
    [ObservableProperty] private ObservableCollection<TipoRubroOpcion> _tiposRubro = [];
    [ObservableProperty] private TipoRubroOpcion? _tipoRubroForm;

    public bool PuedeCrear => _sesion.EsAdministrador || _sesion.TienePermiso("MI.CREAR");
    public bool PuedeEditar => _sesion.EsAdministrador || _sesion.TienePermiso("MI.EDITAR");
    public bool PuedeEliminar => _sesion.EsAdministrador || _sesion.TienePermiso("MI.ELIMINAR");

    [RelayCommand]
    private async Task CargarAsync(int? carreraId = null)
    {
        _carreraId = carreraId ?? _carreraId;
        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            await RefrescarListasAsync();
            await CargarResumenAsync();
        }
        catch (Exception ex) { MensajeError = ex.Message; }
        finally { EstaCargando = false; }
    }

    private async Task RefrescarListasAsync()
    {
        using var scope = _sp.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ListarServiciosMantenimientoQuery>();
        var todos = await query.EjecutarAsync(_carreraId);
        ServiciosBasicos = new ObservableCollection<ServicioMantenimientoDto>(
            todos.Where(x => x.TipoRubro == TipoRubroMantenimiento.ServicioBasico));
        ItemsMantenimiento = new ObservableCollection<ServicioMantenimientoDto>(
            todos.Where(x => x.TipoRubro == TipoRubroMantenimiento.Mantenimiento));
    }

    private async Task CargarResumenAsync()
    {
        using var scope = _sp.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ObtenerResumenMantenimientoQuery>();
        Resumen = await query.EjecutarAsync(_carreraId, escenarioProyeccionId: 1);
    }

    [RelayCommand]
    private void AbrirNuevo(string tipoStr)
    {
        var tipo = tipoStr == "Mantenimiento"
            ? TipoRubroMantenimiento.Mantenimiento
            : TipoRubroMantenimiento.ServicioBasico;
        FormId = 0;
        FormNombre = string.Empty;
        FormCosto = "0";
        TipoRubroForm = TiposRubro.First(t => t.Valor == tipo);
        FormEsEdicion = false;
        FormVisible = true;
    }

    [RelayCommand]
    private void AbrirEditar(ServicioMantenimientoDto item)
    {
        FormId = item.Id;
        FormNombre = item.NombreRubro;
        FormCosto = item.CostoAnualUniversidad.ToString("F2");
        TipoRubroForm = TiposRubro.First(t => t.Valor == item.TipoRubro);
        FormEsEdicion = true;
        FormVisible = true;
    }

    [RelayCommand]
    private void CancelarForm()
    {
        FormVisible = false;
        MensajeError = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        MensajeError = string.Empty;
        if (!decimal.TryParse(FormCosto.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var costo))
        {
            MensajeError = "Costo inválido.";
            return;
        }
        EstaGuardando = true;
        try
        {
            using var scope = _sp.CreateScope();
            if (!FormEsEdicion)
            {
                var cmd = scope.ServiceProvider.GetRequiredService<CrearServicioMantenimientoCommand>();
                await cmd.EjecutarAsync(new CrearServicioMantenimientoDto
                {
                    CarreraId = _carreraId,
                    TipoRubro = TipoRubroForm!.Valor,
                    NombreRubro = FormNombre,
                    CostoAnualUniversidad = costo,
                });
            }
            else
            {
                var cmd = scope.ServiceProvider.GetRequiredService<ActualizarServicioMantenimientoCommand>();
                await cmd.EjecutarAsync(new ActualizarServicioMantenimientoDto
                {
                    Id = FormId,
                    TipoRubro = TipoRubroForm!.Valor,
                    NombreRubro = FormNombre,
                    CostoAnualUniversidad = costo,
                });
            }
            FormVisible = false;
            MensajeExito = FormEsEdicion ? "Rubro actualizado." : "Rubro creado.";
            await RefrescarListasAsync();
            await CargarResumenAsync();
        }
        catch (Exception ex) { MensajeError = ex.Message; }
        finally { EstaGuardando = false; }
    }

    [RelayCommand]
    private async Task EliminarAsync(ServicioMantenimientoDto item)
    {
        MensajeError = string.Empty;
        EstaGuardando = true;
        try
        {
            using var scope = _sp.CreateScope();
            var cmd = scope.ServiceProvider.GetRequiredService<EliminarServicioMantenimientoCommand>();
            await cmd.EjecutarAsync(item.Id, _sesion.UsuarioId);
            MensajeExito = "Rubro eliminado.";
            await RefrescarListasAsync();
            await CargarResumenAsync();
        }
        catch (Exception ex) { MensajeError = ex.Message; }
        finally { EstaGuardando = false; }
    }
}
