using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Amortizacion;
using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Amortizacion;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.UseCases.SueldosPlantaCentral;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;
using SistemaAranceles.Presentation.ViewModels.Mantenimiento;
using DatosInstitucionalesDominio = SistemaAranceles.Domain.Entities.DatosInstitucionales;

namespace SistemaAranceles.Presentation.ViewModels.Amortizacion;

/// <summary>
/// KAN-44B: financiamiento de la inversión inicial en 3 fuentes (Recursos Propios,
/// Préstamo Bancario, Convenio Institucional) + tabla de amortización francesa del préstamo.
/// </summary>
public sealed partial class AmortizacionViewModel : ObservableObject
{
    private readonly IServiceProvider _sp;
    private readonly SesionActual _sesion;
    private bool _suprimirRecarga;
    private decimal? _totalInversion;
    private CancellationTokenSource? _ctsRecalculo;

    public AmortizacionViewModel(IServiceProvider sp, SesionActual sesion)
    {
        _sp = sp;
        _sesion = sesion;
    }

    [ObservableProperty] private ObservableCollection<CarreraMantenimientoOpcion> _carreras = [];
    [ObservableProperty] private CarreraMantenimientoOpcion? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioProyeccion> _escenarios = [];
    [ObservableProperty] private EscenarioProyeccion? _escenarioSeleccionado;

    [ObservableProperty] private ResumenFinanciamientoDto? _resumen;

    // Parámetros editables (se persisten en DatosInstitucionales)
    [ObservableProperty] private decimal _porcentajePrestamo;
    [ObservableProperty] private decimal _porcentajeConvenio;
    [ObservableProperty] private string _nombreEntidadPrestamo = string.Empty;
    [ObservableProperty] private string _nombreEntidadConvenio = string.Empty;
    [ObservableProperty] private decimal _tasaInteresAnual = DatosInstitucionalesDominio.TasaInteresAnualPrestamoPorDefecto;
    [ObservableProperty] private int _plazoMeses = DatosInstitucionalesDominio.PlazoPrestamoMesesPorDefecto;

    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;
    [ObservableProperty] private bool _estaRecalculando;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private string _mensajeInfo = string.Empty;

    public bool PuedeEditar => _sesion.EsAdministrador || _sesion.TienePermiso("DI.EDITAR");
    public bool EstaProcesando => EstaCargando || EstaGuardando;
    public decimal PorcentajePropio => 100m - PorcentajePrestamo - PorcentajeConvenio;
    public string PorcentajePropioDisplay => $"{PorcentajePropio:0.####}%";

    partial void OnPorcentajePrestamoChanged(decimal value)
    {
        _ = value;
        OnPropertyChanged(nameof(PorcentajePropio));
        OnPropertyChanged(nameof(PorcentajePropioDisplay));
        ProgramarRecalculo();
    }

    partial void OnPorcentajeConvenioChanged(decimal value)
    {
        _ = value;
        OnPropertyChanged(nameof(PorcentajePropio));
        OnPropertyChanged(nameof(PorcentajePropioDisplay));
        ProgramarRecalculo();
    }

    partial void OnTasaInteresAnualChanged(decimal value) { _ = value; ProgramarRecalculo(); }
    partial void OnPlazoMesesChanged(int value) { _ = value; ProgramarRecalculo(); }

    // Debounce: regenerar la tabla en cada tecla traba la UI; se espera a que el
    // usuario deje de escribir (300 ms) antes de recalcular.
    private void ProgramarRecalculo()
    {
        if (_totalInversion is null)
            return;

        _ctsRecalculo?.Cancel();
        var cts = new CancellationTokenSource();
        _ctsRecalculo = cts;
        EstaRecalculando = true;
        _ = RecalcularConRetrasoAsync(cts.Token);
    }

    private async Task RecalcularConRetrasoAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(300, ct);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (ct.IsCancellationRequested)
            return;

        RecalcularLocal();
        EstaRecalculando = false;
    }

    /// <summary>Recalcula montos y tabla en memoria con los % actuales (como el Excel), sin tocar BD.</summary>
    private void RecalcularLocal()
    {
        if (_totalInversion is not decimal total)
            return;

        if (PorcentajePrestamo < 0m || PorcentajeConvenio < 0m || PorcentajePrestamo + PorcentajeConvenio > 100m)
        {
            MensajeError = "La suma de % préstamo y % convenio no puede superar 100%.";
            return;
        }

        if (TasaInteresAnual < 0m || PlazoMeses <= 0)
            return;

        MensajeError = string.Empty;
        Resumen = ObtenerResumenAmortizacionQuery.ConstruirResumen(
            total,
            PorcentajePrestamo,
            PorcentajeConvenio,
            string.IsNullOrWhiteSpace(NombreEntidadPrestamo) ? null : NombreEntidadPrestamo.Trim(),
            string.IsNullOrWhiteSpace(NombreEntidadConvenio) ? null : NombreEntidadConvenio.Trim(),
            TasaInteresAnual,
            PlazoMeses);
    }

    partial void OnEstaCargandoChanged(bool value) { _ = value; OnPropertyChanged(nameof(EstaProcesando)); }
    partial void OnEstaGuardandoChanged(bool value) { _ = value; OnPropertyChanged(nameof(EstaProcesando)); }

    partial void OnCarreraSeleccionadaChanged(CarreraMantenimientoOpcion? value)
    {
        _ = value;
        if (_suprimirRecarga || EstaCargando)
            return;
        _ = CargarEscenariosAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioProyeccion? value)
    {
        _ = value;
        if (_suprimirRecarga || EstaCargando)
            return;
        _ = RecargarResumenAsync();
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

            var queryDatos = scope.ServiceProvider.GetRequiredService<ObtenerDatosInstitucionalesVigentesQuery>();
            var datos = await queryDatos.EjecutarAsync();
            if (datos is not null)
                HidratarParametros(datos);

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
                Resumen = null;
                MensajeInfo = Carreras.Count == 0
                    ? "No hay carreras registradas para calcular el financiamiento."
                    : "Selecciona una carrera para calcular la amortización.";
                return;
            }

            await CargarEscenariosAsync(escenarioIdActual);
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
            Resumen = null;
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
            Resumen = null;
            MensajeInfo = "Selecciona una carrera para calcular la amortización.";
            return;
        }

        try
        {
            MensajeInfo = string.Empty;
            using var scope = _sp.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ListarEscenariosConProyeccionPorCarreraQuery>();
            var lista = await query.EjecutarAsync(CarreraSeleccionada.Id);

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

        await RecargarResumenAsync();
    }

    private async Task RecargarResumenAsync()
    {
        if (CarreraSeleccionada is null)
        {
            Resumen = null;
            _totalInversion = null;
            return;
        }

        var carreraId = CarreraSeleccionada.Id;
        var escenarioId = EscenarioSeleccionado?.Id;

        try
        {
            MensajeError = string.Empty;
            using var scope = _sp.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ObtenerResumenAmortizacionQuery>();
            var resumen = await query.EjecutarAsync(carreraId, escenarioId);

            // Anti-stale: descartar si la selección cambió durante el await
            if (CarreraSeleccionada?.Id != carreraId || EscenarioSeleccionado?.Id != escenarioId)
                return;

            // La BD solo aporta la inversión total; los montos/tabla se arman
            // con los parámetros en pantalla (pueden diferir de los guardados).
            _totalInversion = resumen.TotalInversion;
            Resumen = resumen;
            RecalcularLocal();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
            Resumen = null;
            _totalInversion = null;
        }
    }

    [RelayCommand]
    private async Task GuardarParametrosAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para editar los parámetros de financiamiento.";
            return;
        }

        if (EstaGuardando) return;
        EstaGuardando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            if (PorcentajePrestamo + PorcentajeConvenio > 100m)
            {
                MensajeError = "La suma de % préstamo y % convenio no puede superar 100%.";
                return;
            }

            using var scope = _sp.CreateScope();
            var queryDatos = scope.ServiceProvider.GetRequiredService<ObtenerDatosInstitucionalesVigentesQuery>();
            var vigente = await queryDatos.EjecutarAsync();

            if (vigente is null)
            {
                MensajeError = "Primero configure los Datos Institucionales (módulo Datos Institucionales).";
                return;
            }

            var cmd = scope.ServiceProvider.GetRequiredService<ConfigurarDatosInstitucionalesCommand>();
            var dto = await cmd.EjecutarAsync(ConstruirDtoGuardado(vigente), _sesion.UsuarioId);

            HidratarParametros(dto);
            MensajeExito = "Parámetros de financiamiento guardados correctamente.";
            await RecargarResumenAsync();
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

    private void HidratarParametros(DatosInstitucionalesDto dto)
    {
        PorcentajePrestamo = dto.PorcentajeFinanciadoPrestamo;
        PorcentajeConvenio = dto.PorcentajeFinanciadoConvenio;
        NombreEntidadPrestamo = dto.NombreEntidadPrestamo ?? string.Empty;
        NombreEntidadConvenio = dto.NombreEntidadConvenio ?? string.Empty;
        TasaInteresAnual = dto.TasaInteresAnualPrestamo;
        PlazoMeses = dto.PlazoPrestamoMeses;
    }

    private GuardarDatosInstitucionalesDto ConstruirDtoGuardado(DatosInstitucionalesDto v) => new()
    {
        Periodo = v.Periodo,
        NumeroEstudiantesUniversidad = v.NumeroEstudiantesUniversidad,
        NumeroDocentesUniversidad = v.NumeroDocentesUniversidad,
        NumeroPersonasPlantaCentral = v.NumeroPersonasPlantaCentral,
        SueldoBasico = v.SueldoBasico,
        Funcional = v.Funcional,
        FondoReserva = v.FondoReserva,
        BeneficioXiv = v.BeneficioXiv,
        BeneficioXiii = v.BeneficioXiii,
        AportePatronal = v.AportePatronal,
        Varios = v.Varios,
        MesesCapitalTrabajo = v.MesesCapitalTrabajo,
        PorcentajeImprevistosInversion = v.PorcentajeImprevistosInversion,
        PorcentajeMatriculaDefault = v.PorcentajeMatriculaDefault,
        PorcentajeBecasInstitucionales = v.PorcentajeBecasInstitucionales,
        SemestresPorAnio = v.SemestresPorAnio,
        MesesOperativosCiclo = v.MesesOperativosCiclo,
        PresupuestoAnualCapacitacion = v.PresupuestoAnualCapacitacion,
        PresupuestoAnualInternacionalizacion = v.PresupuestoAnualInternacionalizacion,
        PresupuestoAnualMarketing = v.PresupuestoAnualMarketing,
        PolizaSeguroEstudiantilAnual = v.PolizaSeguroEstudiantilAnual,
        FuenteInflacion = v.FuenteInflacion,
        AnioBaseProyeccion = v.AnioBaseProyeccion,
        PresupuestoBaseUniversidad = v.PresupuestoBaseUniversidad,
        PresupuestoGobiernoBecas = v.PresupuestoGobiernoBecas,
        PorcentajeInvestigacion = v.PorcentajeInvestigacion,
        PorcentajeVinculacion = v.PorcentajeVinculacion,
        PorcentajeBecasEstudiantes = v.PorcentajeBecasEstudiantes,
        PorcentajeBecasDocentes = v.PorcentajeBecasDocentes,
        TasaInteresFinanciera = v.TasaInteresFinanciera,
        PremioRiesgo = v.PremioRiesgo,
        TmrManual = v.TmrManual,
        UsarTmrManual = v.UsarTmrManual,
        ToleranciaVanArancel = v.ToleranciaVanArancel,
        MargenAproximacionVanArancel = v.MargenAproximacionVanArancel,
        ArancelMinimoBusqueda = v.ArancelMinimoBusqueda,
        ArancelMaximoBusqueda = v.ArancelMaximoBusqueda,
        MaxIteracionesBiseccion = v.MaxIteracionesBiseccion,
        PorcentajeFinanciadoPrestamo = PorcentajePrestamo,
        PorcentajeFinanciadoConvenio = PorcentajeConvenio,
        NombreEntidadPrestamo = string.IsNullOrWhiteSpace(NombreEntidadPrestamo) ? null : NombreEntidadPrestamo.Trim(),
        NombreEntidadConvenio = string.IsNullOrWhiteSpace(NombreEntidadConvenio) ? null : NombreEntidadConvenio.Trim(),
        TasaInteresAnualPrestamo = TasaInteresAnual,
        PlazoPrestamoMeses = PlazoMeses,
        FuenteNotas = v.FuenteNotas,
    };

    private static string Detalle(Exception ex)
        => ex.InnerException is null ? ex.Message : $"{ex.Message} ({ex.InnerException.Message})";
}
