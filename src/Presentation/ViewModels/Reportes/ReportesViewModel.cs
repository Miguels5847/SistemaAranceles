using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SistemaAranceles.Application.DTOs.Amortizacion;
using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.DTOs.InversionInicial;
using SistemaAranceles.Application.DTOs.Reportes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Application.UseCases.Amortizacion;
using SistemaAranceles.Application.UseCases.AnalisisFinanciero;
using SistemaAranceles.Application.UseCases.CapitalTrabajo;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.UseCases.InversionInicial;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;
using SistemaAranceles.Presentation.ViewModels.Mantenimiento;

namespace SistemaAranceles.Presentation.ViewModels.Reportes;

/// <summary>
/// KAN-46 Fase 1: módulo Reportes (CES, Financiero, Demanda) con exportación PDF.
/// Reutiliza las queries existentes de cada módulo; no introduce cálculos nuevos.
/// </summary>
public sealed partial class ReportesViewModel : ObservableObject
{
    private readonly IServiceProvider _sp;
    private readonly SesionActual _sesion;
    private bool _suprimirRecarga;
    // Permite cambiar carrera/escenario sin esperar la carga en curso:
    // cada cambio sube la versión y los resultados viejos se descartan.
    private int _versionCarga;

    public ReportesViewModel(IServiceProvider sp, SesionActual sesion)
    {
        _sp = sp;
        _sesion = sesion;
    }

    [ObservableProperty] private ObservableCollection<CarreraMantenimientoOpcion> _carreras = [];
    [ObservableProperty] private CarreraMantenimientoOpcion? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioProyeccion> _escenarios = [];
    [ObservableProperty] private EscenarioProyeccion? _escenarioSeleccionado;
    [ObservableProperty] private int _pestanaSeleccionada;

    // Reporte CES
    [ObservableProperty] private CesDto? _ces;

    // Reporte Financiero
    [ObservableProperty] private InversionInicialTotalDto? _inversion;
    [ObservableProperty] private ResumenCapitalTrabajoDto? _capitalTrabajo;
    [ObservableProperty] private ResumenFinanciamientoDto? _financiamiento;
    [ObservableProperty] private MatrizCostosGastosDto? _costosGastos;
    [ObservableProperty] private FlujoFondosDto? _flujoFondos;
    [ObservableProperty] private IndicadoresFinancierosDto? _indicadores;

    // Reporte Demanda
    [ObservableProperty] private DemandaProyectadaDto? _demanda;
    [ObservableProperty] private ArancelEfectivoDto? _arancelEfectivo;
    [ObservableProperty] private IngresosProyectadosDto? _ingresos;

    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaExportando;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private string _mensajeInfo = string.Empty;

    public bool PuedeExportar => _sesion.EsAdministrador || _sesion.TienePermiso("REP.EXPORTAR");
    public bool EstaProcesando => EstaCargando || EstaExportando;

    partial void OnEstaCargandoChanged(bool value) { _ = value; OnPropertyChanged(nameof(EstaProcesando)); }
    partial void OnEstaExportandoChanged(bool value) { _ = value; OnPropertyChanged(nameof(EstaProcesando)); }

    partial void OnCarreraSeleccionadaChanged(CarreraMantenimientoOpcion? value)
    {
        _ = value;
        if (_suprimirRecarga)
            return;
        _ = CargarEscenariosAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioProyeccion? value)
    {
        _ = value;
        if (_suprimirRecarga)
            return;
        LimpiarReportes();
        _ = CargarReporteActualAsync();
    }

    partial void OnPestanaSeleccionadaChanged(int value)
    {
        _ = value;
        if (_suprimirRecarga)
            return;
        _ = CargarReporteActualAsync();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        MensajeInfo = string.Empty;

        try
        {
            var carreraIdActual = CarreraSeleccionada?.Id;
            var escenarioIdActual = EscenarioSeleccionado?.Id;

            using var scope = _sp.CreateScope();
            var repoCarrera = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var lista = await repoCarrera.ListarAsync();

            _suprimirRecarga = true;
            try
            {
                Carreras = new ObservableCollection<CarreraMantenimientoOpcion>(
                    lista.OrderBy(x => x.Codigo)
                        .Select(x => new CarreraMantenimientoOpcion
                        {
                            Id = x.Id,
                            Etiqueta = $"{x.Codigo} - {x.Nombre}"
                        }));

                CarreraSeleccionada = carreraIdActual is > 0
                    ? Carreras.FirstOrDefault(x => x.Id == carreraIdActual.Value)
                    : null;
            }
            finally
            {
                _suprimirRecarga = false;
            }

            if (CarreraSeleccionada is null)
            {
                LimpiarReportes();
                MensajeInfo = Carreras.Count == 0
                    ? "No hay carreras registradas para generar reportes."
                    : "Selecciona una carrera para generar los reportes.";
                return;
            }

            await CargarEscenariosAsync(escenarioIdActual);
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
            LimpiarReportes();
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private async Task CargarEscenariosAsync(int? escenarioIdPreferido = null)
    {
        if (CarreraSeleccionada is null)
        {
            LimpiarReportes();
            return;
        }

        var carreraId = CarreraSeleccionada.Id;
        try
        {
            MensajeInfo = string.Empty;
            using var scope = _sp.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ListarEscenariosConProyeccionPorCarreraQuery>();
            var lista = await query.EjecutarAsync(carreraId);

            // Anti-stale: la carrera cambió mientras se listaban escenarios
            if (CarreraSeleccionada?.Id != carreraId)
                return;

            _suprimirRecarga = true;
            try
            {
                Escenarios = new ObservableCollection<EscenarioProyeccion>(lista);
                EscenarioSeleccionado = Escenarios.FirstOrDefault(x => x.Id == escenarioIdPreferido)
                    ?? Escenarios.FirstOrDefault();
            }
            finally
            {
                _suprimirRecarga = false;
            }
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar escenarios: {Detalle(ex)}";
            _suprimirRecarga = true;
            try
            {
                Escenarios = [];
                EscenarioSeleccionado = null;
            }
            finally
            {
                _suprimirRecarga = false;
            }
        }

        LimpiarReportes();
        await CargarReporteActualAsync();
    }

    private async Task CargarReporteActualAsync()
    {
        if (CarreraSeleccionada is null)
            return;

        var carreraId = CarreraSeleccionada.Id;
        var escenarioId = EscenarioSeleccionado?.Id;
        var pestana = PestanaSeleccionada;
        var version = ++_versionCarga;

        EstaCargando = true;
        MensajeError = string.Empty;
        try
        {
            switch (pestana)
            {
                case 0 when Ces is null:
                    await CargarCesAsync(carreraId, escenarioId);
                    break;
                case 1 when FlujoFondos is null:
                    await CargarFinancieroAsync(carreraId, escenarioId);
                    break;
                case 2 when Ingresos is null:
                    await CargarDemandaAsync(carreraId, escenarioId);
                    break;
            }
        }
        catch (Exception ex)
        {
            if (version == _versionCarga)
                MensajeError = Detalle(ex);
        }
        finally
        {
            // Solo la carga más reciente apaga el indicador (las viejas se descartan)
            if (version == _versionCarga)
                EstaCargando = false;
        }
    }

    private bool SeleccionVigente(int carreraId, int? escenarioId)
        => CarreraSeleccionada?.Id == carreraId && EscenarioSeleccionado?.Id == escenarioId;

    private async Task CargarCesAsync(int carreraId, int? escenarioId)
    {
        using var scope = _sp.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ObtenerCesQuery>();
        var ces = await query.EjecutarAsync(carreraId, escenarioId);
        if (!SeleccionVigente(carreraId, escenarioId))
            return;
        Ces = ces;
    }

    private async Task CargarFinancieroAsync(int carreraId, int? escenarioId)
    {
        using var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;

        var inversion = await sp.GetRequiredService<ObtenerInversionInicialTotalQuery>()
            .EjecutarAsync(carreraId, escenarioId);
        var capital = await sp.GetRequiredService<ObtenerResumenCapitalTrabajoQuery>()
            .EjecutarAsync(carreraId, escenarioId);
        var financiamiento = await sp.GetRequiredService<ObtenerResumenAmortizacionQuery>()
            .EjecutarAsync(carreraId, escenarioId);
        var costos = await sp.GetRequiredService<ObtenerMatrizCostosGastosQuery>()
            .EjecutarAsync(carreraId, escenarioId);
        var flujo = await sp.GetRequiredService<ObtenerFlujoFondosQuery>()
            .EjecutarAsync(carreraId, escenarioId, capitalTrabajoPrecalculado: capital);
        var indicadores = await sp.GetRequiredService<ObtenerIndicadoresFinancierosQuery>()
            .EjecutarAsync(carreraId, escenarioId, flujoPrecalculado: flujo);

        if (!SeleccionVigente(carreraId, escenarioId))
            return;

        Inversion = inversion;
        CapitalTrabajo = capital;
        Financiamiento = financiamiento;
        CostosGastos = costos;
        FlujoFondos = flujo;
        Indicadores = indicadores;
    }

    private async Task CargarDemandaAsync(int carreraId, int? escenarioId)
    {
        using var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;

        var demanda = await sp.GetRequiredService<ObtenerDemandaProyectadaQuery>()
            .EjecutarAsync(carreraId, escenarioId);
        var arancel = await sp.GetRequiredService<ObtenerArancelEfectivoQuery>()
            .EjecutarAsync(carreraId, escenarioId);
        var ingresos = await sp.GetRequiredService<CalcularIngresosProyectadosQuery>()
            .EjecutarAsync(carreraId, escenarioId, arancelPrecalculado: arancel);

        if (!SeleccionVigente(carreraId, escenarioId))
            return;

        Demanda = demanda;
        ArancelEfectivo = arancel;
        Ingresos = ingresos;
    }

    private void LimpiarReportes()
    {
        Ces = null;
        Inversion = null;
        CapitalTrabajo = null;
        Financiamiento = null;
        CostosGastos = null;
        FlujoFondos = null;
        Indicadores = null;
        Demanda = null;
        ArancelEfectivo = null;
        Ingresos = null;
    }

    [RelayCommand]
    private async Task ExportarPdfAsync(string? tipo)
    {
        if (!PuedeExportar)
        {
            MensajeError = "No tiene permiso para exportar reportes (REP.EXPORTAR).";
            return;
        }

        if (CarreraSeleccionada is null)
        {
            MensajeError = "Selecciona una carrera antes de exportar.";
            return;
        }

        if (EstaExportando) return;
        EstaExportando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        var carreraId = CarreraSeleccionada.Id;
        var escenarioId = EscenarioSeleccionado?.Id;
        var carreraNombre = CarreraSeleccionada.Etiqueta;
        var escenarioNombre = EscenarioSeleccionado?.Nombre ?? "Global";

        try
        {
            byte[] pdf;
            string nombreReporte;

            switch (tipo)
            {
                case "CES":
                    if (Ces is null) await CargarCesAsync(carreraId, escenarioId);
                    if (Ces is null) { MensajeError = "No hay datos CES para exportar."; return; }
                    nombreReporte = "CES";
                    pdf = GenerarPdf(s => s.GenerarReporteCes(
                        new ReporteCesDatos(carreraNombre, escenarioNombre, DateTime.Now, Ces)));
                    break;

                case "Financiero":
                    if (FlujoFondos is null) await CargarFinancieroAsync(carreraId, escenarioId);
                    if (Inversion is null || CapitalTrabajo is null || Financiamiento is null
                        || CostosGastos is null || FlujoFondos is null || Indicadores is null)
                    { MensajeError = "No hay datos financieros completos para exportar."; return; }
                    nombreReporte = "Financiero";
                    pdf = GenerarPdf(s => s.GenerarReporteFinanciero(
                        new ReporteFinancieroDatos(carreraNombre, escenarioNombre, DateTime.Now,
                            Inversion, CapitalTrabajo, Financiamiento, CostosGastos, FlujoFondos, Indicadores)));
                    break;

                case "Demanda":
                    if (Ingresos is null) await CargarDemandaAsync(carreraId, escenarioId);
                    if (Demanda is null || ArancelEfectivo is null || Ingresos is null)
                    { MensajeError = "No hay datos de demanda para exportar."; return; }
                    nombreReporte = "Demanda";
                    pdf = GenerarPdf(s => s.GenerarReporteDemanda(
                        new ReporteDemandaDatos(carreraNombre, escenarioNombre, DateTime.Now,
                            Demanda, ArancelEfectivo, Ingresos)));
                    break;

                default:
                    MensajeError = "Tipo de reporte desconocido.";
                    return;
            }

            var dialogo = new SaveFileDialog
            {
                Filter = "Archivo PDF (*.pdf)|*.pdf",
                FileName = $"Reporte_{nombreReporte}_{Sanear(carreraNombre)}_{Sanear(escenarioNombre)}_{DateTime.Now:yyyyMMdd}.pdf"
            };
            if (dialogo.ShowDialog() != true)
                return;

            await File.WriteAllBytesAsync(dialogo.FileName, pdf);
            MensajeExito = $"Reporte {nombreReporte} exportado: {dialogo.FileName}";
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaExportando = false;
        }
    }

    private byte[] GenerarPdf(Func<IServicioExportacionPdf, byte[]> generar)
    {
        using var scope = _sp.CreateScope();
        var servicio = scope.ServiceProvider.GetRequiredService<IServicioExportacionPdf>();
        return generar(servicio);
    }

    private static string Sanear(string texto)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var limpio = new string(texto.Where(c => !invalidos.Contains(c)).ToArray());
        return limpio.Replace(' ', '_');
    }

    private static string Detalle(Exception ex)
        => ex.InnerException is null ? ex.Message : $"{ex.Message} ({ex.InnerException.Message})";
}
