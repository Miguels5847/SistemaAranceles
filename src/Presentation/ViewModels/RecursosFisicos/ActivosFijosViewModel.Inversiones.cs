using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

namespace SistemaAranceles.Presentation.ViewModels.RecursosFisicos;

public sealed class PeriodoInversionVm
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}

public sealed partial class CeldaInversionVm : ObservableObject
{
    [ObservableProperty] private string _cantidadTexto = "0";

    public int ActivoFijoId { get; init; }
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public decimal ValorUnitario { get; init; }
    public decimal FactorInflacion { get; init; }
    public decimal Monto { get; init; }
    public bool EsEditable { get; init; }
    public bool EsSoloLectura => !EsEditable;
    public bool EsPeriodoInicial { get; init; }
    public string OrigenCantidad { get; init; } = string.Empty;
    public string MontoDisplay => Monto.ToString("C2");
    public string FactorInflacionDisplay => FactorInflacion.ToString("0.######", CultureInfo.InvariantCulture);
}

public sealed class FilaInversionVm
{
    public int ActivoFijoId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string CategoriaNombre { get; init; } = string.Empty;
    public string TipoCalculoNombre { get; init; } = string.Empty;
    public decimal ValorUnitario { get; init; }
    public string ValorUnitarioDisplay => ValorUnitario.ToString("C2");
    public ObservableCollection<CeldaInversionVm> Celdas { get; init; } = [];
}

public sealed partial class TotalPeriodoInversionVm
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string TotalDisplay => Total.ToString("C2");
}

public sealed partial class ActivosFijosViewModel
{
    [ObservableProperty] private ObservableCollection<PeriodoInversionVm> _periodosInversion = [];
    [ObservableProperty] private ObservableCollection<FilaInversionVm> _filasInversion = [];
    [ObservableProperty] private ObservableCollection<TotalPeriodoInversionVm> _totalesInversion = [];
    [ObservableProperty] private bool _estaCargandoInversiones;
    [ObservableProperty] private bool _estaGuardandoInversiones;
    [ObservableProperty] private string _mensajeInversiones = string.Empty;

    public bool TieneMatrizInversiones => FilasInversion.Count > 0;

    partial void OnFilasInversionChanged(ObservableCollection<FilaInversionVm> value)
    {
        _ = value;
        OnPropertyChanged(nameof(TieneMatrizInversiones));
    }

    [RelayCommand]
    private async Task CargarMatrizInversionesAsync()
    {
        MensajeInversiones = string.Empty;

        if (CarreraSeleccionada is null)
        {
            LimpiarMatrizInversiones();
            MensajeInversiones = "Seleccione una carrera para cargar la proyección.";
            return;
        }

        if (EscenarioSeleccionado is null)
        {
            LimpiarMatrizInversiones();
            MensajeInversiones = "Seleccione un escenario con proyección para cargar la matriz.";
            return;
        }

        if (EstaCargandoInversiones) return;
        EstaCargandoInversiones = true;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ObtenerMatrizInversionesQuery>();
            var matriz = await query.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);
            CargarMatrizEnPantalla(matriz);
            MensajeInversiones = matriz.Filas.Count == 0
                ? "No existen activos fijos para construir la matriz de inversiones."
                : "Matriz de inversiones cargada correctamente.";
        }
        catch (Exception ex)
        {
            LimpiarMatrizInversiones();
            MensajeInversiones = $"Error al cargar inversiones futuras: {Detalle(ex)}";
        }
        finally
        {
            EstaCargandoInversiones = false;
        }
    }

    [RelayCommand]
    private async Task GuardarInversionesFuturasAsync()
    {
        if (!PuedeEditar && !PuedeCrear)
        {
            MensajeInversiones = "No tiene permiso para guardar inversiones futuras.";
            return;
        }

        if (CarreraSeleccionada is null || EscenarioSeleccionado is null)
        {
            MensajeInversiones = "Seleccione carrera y escenario antes de guardar.";
            return;
        }

        if (EstaGuardandoInversiones) return;
        EstaGuardandoInversiones = true;
        MensajeInversiones = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var guardar = scope.ServiceProvider.GetRequiredService<GuardarInversionFuturaCommand>();
            var totalGuardado = 0;

            foreach (var celda in FilasInversion.SelectMany(f => f.Celdas).Where(c => c.EsEditable))
            {
                if (!TryDecimalFlexible(celda.CantidadTexto, out var cantidad) || cantidad < 0)
                {
                    MensajeInversiones = $"Cantidad inválida en {celda.Anio} semestre {celda.Semestre}.";
                    return;
                }

                await guardar.EjecutarAsync(new GuardarInversionFuturaDto
                {
                    ActivoFijoId = celda.ActivoFijoId,
                    Anio = celda.Anio,
                    Semestre = celda.Semestre,
                    CantidadProyectada = cantidad
                });
                totalGuardado++;
            }

            await CargarMatrizInversionesAsync();
            MensajeInversiones = $"Inversiones futuras guardadas correctamente. Celdas manuales procesadas: {totalGuardado}.";
        }
        catch (Exception ex)
        {
            MensajeInversiones = $"Error al guardar inversiones futuras: {Detalle(ex)}";
        }
        finally
        {
            EstaGuardandoInversiones = false;
        }
    }

    private void CargarMatrizEnPantalla(MatrizInversionesDto matriz)
    {
        PeriodosInversion = new ObservableCollection<PeriodoInversionVm>(
            matriz.Periodos.Select(p => new PeriodoInversionVm
            {
                Anio = p.Anio,
                Semestre = p.Semestre,
                NumeroPeriodo = p.NumeroPeriodo,
                Etiqueta = p.Etiqueta
            }));

        FilasInversion = new ObservableCollection<FilaInversionVm>(
            matriz.Filas.Select(f => new FilaInversionVm
            {
                ActivoFijoId = f.ActivoFijoId,
                Descripcion = f.Descripcion,
                CategoriaNombre = f.CategoriaNombre,
                TipoCalculoNombre = f.TipoCalculoNombre,
                ValorUnitario = f.ValorUnitario,
                Celdas = new ObservableCollection<CeldaInversionVm>(
                    f.Celdas.Select(c => new CeldaInversionVm
                    {
                        ActivoFijoId = c.ActivoFijoId,
                        Anio = c.Anio,
                        Semestre = c.Semestre,
                        NumeroPeriodo = c.NumeroPeriodo,
                        CantidadTexto = c.Cantidad.ToString("0.####", CultureInfo.InvariantCulture),
                        ValorUnitario = c.ValorUnitario,
                        FactorInflacion = c.FactorInflacion,
                        Monto = c.Monto,
                        EsEditable = c.EsEditable,
                        EsPeriodoInicial = c.EsPeriodoInicial,
                        OrigenCantidad = c.OrigenCantidad
                    }))
            }));

        TotalesInversion = new ObservableCollection<TotalPeriodoInversionVm>(
            matriz.TotalesPorPeriodo.Select(t => new TotalPeriodoInversionVm
            {
                Anio = t.Anio,
                Semestre = t.Semestre,
                NumeroPeriodo = t.NumeroPeriodo,
                Etiqueta = t.Etiqueta,
                Total = t.Total
            }));
    }

    private void LimpiarMatrizInversiones()
    {
        PeriodosInversion = [];
        FilasInversion = [];
        TotalesInversion = [];
        MensajeInversiones = string.Empty;
    }

    private static bool TryDecimalFlexible(string? value, out decimal result)
    {
        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            return true;

        var normalizado = value?.Replace(',', '.');
        return decimal.TryParse(normalizado, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
