using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.ActivoDiferido;
using SistemaAranceles.Application.UseCases.ActivoDiferido;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.ActivoDiferido;

public sealed partial class ActivoDiferidoViewModel : ObservableObject
{
    private readonly IServiceProvider _sp;
    private readonly SesionActual _sesion;
    private int _carreraId = 1;
    private int _anioBase = DateTime.UtcNow.Year;

    public ActivoDiferidoViewModel(IServiceProvider sp, SesionActual sesion)
    {
        _sp = sp;
        _sesion = sesion;
    }

    [ObservableProperty] private ObservableCollection<ActivoDiferidoDto> _activos = [];
    [ObservableProperty] private TablaAmortizacionDto? _tablaAmortizacion;

    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;

    // Formulario
    [ObservableProperty] private bool _formVisible;
    [ObservableProperty] private bool _formEsEdicion;
    [ObservableProperty] private int _formId;
    [ObservableProperty] private string _formNombre = string.Empty;
    [ObservableProperty] private string _formValor = "0";
    [ObservableProperty] private string _formTasa = "20";

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
            await RefrescarAsync();
        }
        catch (Exception ex) { MensajeError = ex.Message; }
        finally { EstaCargando = false; }
    }

    private async Task RefrescarAsync()
    {
        using var scope = _sp.CreateScope();
        var listar = scope.ServiceProvider.GetRequiredService<ListarActivosDiferidosQuery>();
        var tabla = scope.ServiceProvider.GetRequiredService<ObtenerTablaAmortizacionQuery>();

        Activos = new ObservableCollection<ActivoDiferidoDto>(await listar.EjecutarAsync(_carreraId));
        TablaAmortizacion = await tabla.EjecutarAsync(_carreraId, _anioBase);
    }

    [RelayCommand]
    private void AbrirNuevo()
    {
        FormId = 0;
        FormNombre = string.Empty;
        FormValor = "0";
        FormTasa = "20";
        FormEsEdicion = false;
        FormVisible = true;
    }

    [RelayCommand]
    private void AbrirEditar(ActivoDiferidoDto item)
    {
        FormId = item.Id;
        FormNombre = item.NombreRubro;
        FormValor = item.Valor.ToString("F2");
        FormTasa = (item.TasaAmortizacionAnual * 100m).ToString("F0");
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
        if (!decimal.TryParse(FormValor.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var valor))
        {
            MensajeError = "Valor inválido.";
            return;
        }
        if (!decimal.TryParse(FormTasa.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var tasaPct) || tasaPct < 0 || tasaPct > 100)
        {
            MensajeError = "Tasa inválida (ingrese 0-100).";
            return;
        }
        var tasa = tasaPct / 100m;
        EstaGuardando = true;
        try
        {
            using var scope = _sp.CreateScope();
            if (!FormEsEdicion)
            {
                var cmd = scope.ServiceProvider.GetRequiredService<CrearActivoDiferidoCommand>();
                await cmd.EjecutarAsync(new CrearActivoDiferidoDto
                {
                    CarreraId = _carreraId,
                    NombreRubro = FormNombre,
                    Valor = valor,
                    TasaAmortizacionAnual = tasa,
                });
            }
            else
            {
                var cmd = scope.ServiceProvider.GetRequiredService<ActualizarActivoDiferidoCommand>();
                await cmd.EjecutarAsync(new ActualizarActivoDiferidoDto
                {
                    Id = FormId,
                    NombreRubro = FormNombre,
                    Valor = valor,
                    TasaAmortizacionAnual = tasa,
                });
            }
            FormVisible = false;
            MensajeExito = FormEsEdicion ? "Activo diferido actualizado." : "Activo diferido creado.";
            await RefrescarAsync();
        }
        catch (Exception ex) { MensajeError = ex.Message; }
        finally { EstaGuardando = false; }
    }

    [RelayCommand]
    private async Task EliminarAsync(ActivoDiferidoDto item)
    {
        MensajeError = string.Empty;
        EstaGuardando = true;
        try
        {
            using var scope = _sp.CreateScope();
            var cmd = scope.ServiceProvider.GetRequiredService<EliminarActivoDiferidoCommand>();
            await cmd.EjecutarAsync(item.Id, _sesion.UsuarioId);
            MensajeExito = "Activo diferido eliminado.";
            await RefrescarAsync();
        }
        catch (Exception ex) { MensajeError = ex.Message; }
        finally { EstaGuardando = false; }
    }
}
