using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

namespace SistemaAranceles.Presentation.ViewModels.RecursosFisicos;

public sealed class PeriodoDepreciacionVm
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}

public sealed class CeldaDepreciacionVm
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public decimal DepreciacionPeriodo { get; init; }
    public decimal DepreciacionAcumulada { get; init; }
    public string DepreciacionPeriodoDisplay => DepreciacionPeriodo == 0m ? "$ -" : DepreciacionPeriodo.ToString("C2");
    public string DepreciacionAcumuladaDisplay => DepreciacionAcumulada == 0m ? "$ -" : DepreciacionAcumulada.ToString("C2");
}

public sealed class FilaDepreciacionVm
{
    public int ActivoFijoId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string CategoriaNombre { get; init; } = string.Empty;
    public decimal ValorInicial { get; init; }
    public decimal ValorResidual { get; init; }
    public int VidaUtilAnios { get; init; }
    public ObservableCollection<CeldaDepreciacionVm> Celdas { get; init; } = [];
    public string ValorInicialDisplay => ValorInicial == 0m ? "$ -" : ValorInicial.ToString("C2");
    public string ValorResidualDisplay => ValorResidual == 0m ? "$ -" : ValorResidual.ToString("C2");
}

public sealed class TotalPeriodoDepreciacionVm
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public decimal DepreciacionPeriodo { get; init; }
    public decimal DepreciacionAcumulada { get; init; }
    public string DepreciacionPeriodoDisplay => DepreciacionPeriodo == 0m ? "$ -" : DepreciacionPeriodo.ToString("C2");
    public string DepreciacionAcumuladaDisplay => DepreciacionAcumulada == 0m ? "$ -" : DepreciacionAcumulada.ToString("C2");
}

public sealed partial class ActivosFijosViewModel
{
    [ObservableProperty] private ObservableCollection<PeriodoDepreciacionVm> _periodosDepreciacion = [];
    [ObservableProperty] private ObservableCollection<FilaDepreciacionVm> _filasDepreciacion = [];
    [ObservableProperty] private ObservableCollection<TotalPeriodoDepreciacionVm> _totalesDepreciacion = [];
    [ObservableProperty] private bool _estaCargandoDepreciacion;
    [ObservableProperty] private string _mensajeDepreciacion = string.Empty;

    public bool TieneMatrizDepreciacion => FilasDepreciacion.Count > 0;

    partial void OnFilasDepreciacionChanged(ObservableCollection<FilaDepreciacionVm> value)
    {
        _ = value;
        OnPropertyChanged(nameof(TieneMatrizDepreciacion));
    }

    [RelayCommand]
    private async Task CargarMatrizDepreciacionAsync()
    {
        MensajeDepreciacion = string.Empty;

        if (CarreraSeleccionada is null)
        {
            LimpiarMatrizDepreciacion();
            MensajeDepreciacion = "Seleccione una carrera para cargar la depreciación.";
            return;
        }

        if (EscenarioSeleccionado is null)
        {
            LimpiarMatrizDepreciacion();
            MensajeDepreciacion = "Seleccione un escenario con proyección para cargar la depreciación.";
            return;
        }

        if (EstaCargandoDepreciacion) return;
        EstaCargandoDepreciacion = true;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ObtenerMatrizDepreciacionQuery>();
            var matriz = await query.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);
            CargarDepreciacionEnPantalla(matriz);
            MensajeDepreciacion = ConstruirMensajeCargaDepreciacion(matriz);
        }
        catch (Exception ex)
        {
            LimpiarMatrizDepreciacion();
            MensajeDepreciacion = $"Error al cargar depreciación: {Detalle(ex)}";
        }
        finally
        {
            EstaCargandoDepreciacion = false;
        }
    }

    private void CargarDepreciacionEnPantalla(MatrizDepreciacionDto matriz)
    {
        PeriodosDepreciacion = new ObservableCollection<PeriodoDepreciacionVm>(
            matriz.Periodos.Select(p => new PeriodoDepreciacionVm
            {
                Anio = p.Anio,
                Semestre = p.Semestre,
                NumeroPeriodo = p.NumeroPeriodo,
                Etiqueta = p.Etiqueta
            }));

        FilasDepreciacion = new ObservableCollection<FilaDepreciacionVm>(
            matriz.Filas.Select(f => new FilaDepreciacionVm
            {
                ActivoFijoId = f.ActivoFijoId,
                Descripcion = f.Descripcion,
                CategoriaNombre = f.CategoriaNombre,
                ValorInicial = f.ValorInicial,
                ValorResidual = f.ValorResidual,
                VidaUtilAnios = f.VidaUtilAnios,
                Celdas = new ObservableCollection<CeldaDepreciacionVm>(
                    f.Celdas.Select(c => new CeldaDepreciacionVm
                    {
                        Anio = c.Anio,
                        Semestre = c.Semestre,
                        NumeroPeriodo = c.NumeroPeriodo,
                        DepreciacionPeriodo = c.DepreciacionPeriodo,
                        DepreciacionAcumulada = c.DepreciacionAcumulada
                    }))
            }));

        TotalesDepreciacion = new ObservableCollection<TotalPeriodoDepreciacionVm>(
            matriz.TotalesPorPeriodo.Select(t => new TotalPeriodoDepreciacionVm
            {
                Anio = t.Anio,
                Semestre = t.Semestre,
                NumeroPeriodo = t.NumeroPeriodo,
                Etiqueta = t.Etiqueta,
                DepreciacionPeriodo = t.DepreciacionPeriodo,
                DepreciacionAcumulada = t.DepreciacionAcumulada
            }));
    }

    private static string ConstruirMensajeCargaDepreciacion(MatrizDepreciacionDto matriz)
    {
        if (matriz.Periodos.Count == 0)
        {
            return "No se encontraron períodos para depreciación. Genere primero la Proyección de Estudiantes y la matriz de inversiones.";
        }

        if (matriz.Filas.Count == 0)
        {
            return "No existen activos fijos activos para calcular depreciación.";
        }

        return $"Matriz de depreciación cargada correctamente: {matriz.Filas.Count} activos y {matriz.Periodos.Count} períodos.";
    }

    private void LimpiarMatrizDepreciacion()
    {
        PeriodosDepreciacion = [];
        FilasDepreciacion = [];
        TotalesDepreciacion = [];
        MensajeDepreciacion = string.Empty;
    }
}
