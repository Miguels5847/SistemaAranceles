using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using Microsoft.Extensions.DependencyInjection;

namespace SistemaAranceles.Presentation.ViewModels.CargosFacultad;

/// <summary>
/// Extensión parcial del ViewModel para manejar inflación en cargos.
/// </summary>
public sealed partial class CargosFacultadViewModel
{
    [ObservableProperty]
    private ObservableCollection<OpcionesInflacionPorAnioDto> _opcionesInflacion = [];

    [ObservableProperty]
    private OpcionesInflacionPorAnioDto? _inflacionSeleccionada;

    [ObservableProperty]
    private bool _usarInflacionManual;

    [ObservableProperty]
    private string _inflacionManualValor = "1.0";

    [ObservableProperty]
    private bool _estaCargandoInflacion;

    public decimal ObtenerFactorInflacionActual()
    {
        if (UsarInflacionManual)
        {
            return decimal.TryParse(InflacionManualValor, out var valor) ? Math.Max(valor, 1m) : 1m;
        }

        if (InflacionSeleccionada?.InflacionProyectada.HasValue == true)
        {
            var factor = 1m + (InflacionSeleccionada.InflacionProyectada.Value / 100m);
            return Math.Max(factor, 1m);
        }

        return 1m;
    }

    [RelayCommand]
    private async Task CargarOpcionesInflacionAsync()
    {
        EstaCargandoInflacion = true;
        try
        {
            var query = _serviceProvider.GetRequiredService<ListarOpcionesInflacionPorAnioQuery>();
            // Cargar todos los años disponibles (rango amplio)
            var anioDesde = 1900;
            var anioHasta = 2100;
            var anioActual = DateTime.Now.Year;

            var opciones = await query.EjecutarAsync(anioDesde, anioHasta);
            OpcionesInflacion = new ObservableCollection<OpcionesInflacionPorAnioDto>(opciones);

            // Seleccionar el año actual por defecto
            InflacionSeleccionada = opciones.FirstOrDefault(x => x.Anio == anioActual);
            // Actualizar factor mostrado
            FactorInflacion = ObtenerFactorInflacionActual().ToString("F4");
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar opciones de inflación: {ex.Message}";
        }
        finally
        {
            EstaCargandoInflacion = false;
        }
    }

    [RelayCommand]
    private void AlternarInflacionManual()
    {
        UsarInflacionManual = !UsarInflacionManual;
        FactorInflacion = ObtenerFactorInflacionActual().ToString("F4");
    }

    partial void OnInflacionSeleccionadaChanged(OpcionesInflacionPorAnioDto? value)
    {
        if (UsarInflacionManual)
            return;

        FactorInflacion = ObtenerFactorInflacionActual().ToString("F4");
    }

    partial void OnUsarInflacionManualChanged(bool value)
    {
        // When switching modes, refresh displayed factor
        FactorInflacion = ObtenerFactorInflacionActual().ToString("F4");
    }

    partial void OnInflacionManualValorChanged(string value)
    {
        if (!UsarInflacionManual)
            return;

        FactorInflacion = ObtenerFactorInflacionActual().ToString("F4");
    }
}
