using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;
using SistemaAranceles.Application.UseCases.SueldosPlantaCentral;
using SistemaAranceles.Presentation.State;
using DatosInstitucionalesDominio = SistemaAranceles.Domain.Entities.DatosInstitucionales;

namespace SistemaAranceles.Presentation.ViewModels.DatosInstitucionales;

public sealed partial class DatosInstitucionalesViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;

    public DatosInstitucionalesViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty] private string _periodo = string.Empty;
    [ObservableProperty] private int _numeroEstudiantesUniversidad;
    [ObservableProperty] private int _numeroDocentesUniversidad;
    [ObservableProperty] private int _numeroPersonasPlantaCentral;
    [ObservableProperty] private decimal _sueldoBasico;
    [ObservableProperty] private decimal _funcional;
    [ObservableProperty] private decimal _fondoReserva;
    [ObservableProperty] private decimal _beneficioXiv;
    [ObservableProperty] private decimal _beneficioXiii;
    [ObservableProperty] private decimal _aportePatronal;
    [ObservableProperty] private decimal _varios;
    [ObservableProperty] private int _mesesCapitalTrabajo = DatosInstitucionalesDominio.MesesCapitalTrabajoPorDefecto;
    [ObservableProperty] private decimal _porcentajeImprevistosInversion = DatosInstitucionalesDominio.PorcentajeImprevistosInversionPorDefecto;

    // KAN-35
    [ObservableProperty] private decimal _porcentajeMatriculaDefault = DatosInstitucionalesDominio.PorcentajeMatriculaDefaultPorDefecto;
    [ObservableProperty] private decimal _porcentajeBecasInstitucionales = DatosInstitucionalesDominio.PorcentajeBecasInstitucionalesPorDefecto;
    [ObservableProperty] private int _semestresPorAnio = DatosInstitucionalesDominio.SemestresPorAnioPorDefecto;
    [ObservableProperty] private int _mesesOperativosCiclo = DatosInstitucionalesDominio.MesesOperativosCicloPorDefecto;
    [ObservableProperty] private decimal _presupuestoAnualCapacitacion;
    [ObservableProperty] private decimal _presupuestoAnualInternacionalizacion;
    [ObservableProperty] private decimal _presupuestoAnualMarketing;
    [ObservableProperty] private decimal _polizaSeguroEstudiantilAnual;
    [ObservableProperty] private string? _fuenteInflacion;
    [ObservableProperty] private int? _anioBaseProyeccion;

    [ObservableProperty] private string? _fuenteNotas;

    [ObservableProperty] private decimal _masaSalarialMensual;
    [ObservableProperty] private decimal _totalAnualPlantaCentral;
    [ObservableProperty] private decimal _ratioAdminPorDocente;
    [ObservableProperty] private decimal _costoPorEstudianteAnual;

    [ObservableProperty] private string _ultimaActualizacionTexto = string.Empty;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _puedeEditar;
    [ObservableProperty] private bool _edicionActiva;

    public bool CamposEditables => PuedeEditar && EdicionActiva;

    partial void OnPuedeEditarChanged(bool value) => OnPropertyChanged(nameof(CamposEditables));
    partial void OnEdicionActivaChanged(bool value) => OnPropertyChanged(nameof(CamposEditables));

    partial void OnSueldoBasicoChanged(decimal value) => RecalcularMetricas();
    partial void OnFuncionalChanged(decimal value) => RecalcularMetricas();
    partial void OnFondoReservaChanged(decimal value) => RecalcularMetricas();
    partial void OnBeneficioXivChanged(decimal value) => RecalcularMetricas();
    partial void OnBeneficioXiiiChanged(decimal value) => RecalcularMetricas();
    partial void OnAportePatronalChanged(decimal value) => RecalcularMetricas();
    partial void OnVariosChanged(decimal value) => RecalcularMetricas();
    partial void OnNumeroDocentesUniversidadChanged(int value) => RecalcularMetricas();
    partial void OnNumeroPersonasPlantaCentralChanged(int value) => RecalcularMetricas();
    partial void OnNumeroEstudiantesUniversidadChanged(int value) => RecalcularMetricas();

    private void RecalcularMetricas()
    {
        MasaSalarialMensual = SueldoBasico + Funcional + FondoReserva + BeneficioXiv + BeneficioXiii + AportePatronal + Varios;
        TotalAnualPlantaCentral = Math.Round(MasaSalarialMensual * 12m, 2);
        RatioAdminPorDocente = NumeroDocentesUniversidad <= 0
            ? 0m
            : Math.Round((decimal)NumeroPersonasPlantaCentral / NumeroDocentesUniversidad, 4);
        CostoPorEstudianteAnual = NumeroEstudiantesUniversidad <= 0
            ? 0m
            : Math.Round(TotalAnualPlantaCentral / NumeroEstudiantesUniversidad, 4);
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        PuedeEditar = _sesionActual.TienePermiso("DI.EDITAR") || _sesionActual.EsAdministrador;
        EdicionActiva = false;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ObtenerDatosInstitucionalesVigentesQuery>();
            var dto = await query.EjecutarAsync();

            if (dto is null)
            {
                Periodo = DateTime.Now.Year.ToString();
                MesesCapitalTrabajo = DatosInstitucionalesDominio.MesesCapitalTrabajoPorDefecto;
                PorcentajeImprevistosInversion = DatosInstitucionalesDominio.PorcentajeImprevistosInversionPorDefecto;
                UltimaActualizacionTexto = "Sin registros previos. Ingrese los datos iniciales.";
                RecalcularMetricas();
                return;
            }

            HidratarDesdeDto(dto);
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!CamposEditables)
        {
            MensajeError = "No tiene permiso para editar Datos Institucionales.";
            return;
        }

        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cmd = scope.ServiceProvider.GetRequiredService<ConfigurarDatosInstitucionalesCommand>();
            var dto = await cmd.EjecutarAsync(
                new GuardarDatosInstitucionalesDto
                {
                    Periodo = Periodo,
                    NumeroEstudiantesUniversidad = NumeroEstudiantesUniversidad,
                    NumeroDocentesUniversidad = NumeroDocentesUniversidad,
                    NumeroPersonasPlantaCentral = NumeroPersonasPlantaCentral,
                    SueldoBasico = SueldoBasico,
                    Funcional = Funcional,
                    FondoReserva = FondoReserva,
                    BeneficioXiv = BeneficioXiv,
                    BeneficioXiii = BeneficioXiii,
                    AportePatronal = AportePatronal,
                    Varios = Varios,
                    MesesCapitalTrabajo = MesesCapitalTrabajo,
                    PorcentajeImprevistosInversion = PorcentajeImprevistosInversion,
                    PorcentajeMatriculaDefault = PorcentajeMatriculaDefault,
                    PorcentajeBecasInstitucionales = PorcentajeBecasInstitucionales,
                    SemestresPorAnio = SemestresPorAnio,
                    MesesOperativosCiclo = MesesOperativosCiclo,
                    PresupuestoAnualCapacitacion = PresupuestoAnualCapacitacion,
                    PresupuestoAnualInternacionalizacion = PresupuestoAnualInternacionalizacion,
                    PresupuestoAnualMarketing = PresupuestoAnualMarketing,
                    PolizaSeguroEstudiantilAnual = PolizaSeguroEstudiantilAnual,
                    FuenteInflacion = FuenteInflacion,
                    AnioBaseProyeccion = AnioBaseProyeccion,
                    FuenteNotas = FuenteNotas,
                },
                _sesionActual.UsuarioId);

            HidratarDesdeDto(dto);
            EdicionActiva = false;
            MensajeExito = "Datos institucionales guardados correctamente.";
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
    }

    [RelayCommand]
    private void HabilitarEdicion()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para editar Datos Institucionales.";
            return;
        }

        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        EdicionActiva = true;
    }

    [RelayCommand]
    private async Task CancelarEdicionAsync()
    {
        if (!PuedeEditar)
            return;

        EdicionActiva = false;
        await CargarAsync();
    }

    private void HidratarDesdeDto(DatosInstitucionalesDto dto)
    {
        Periodo = dto.Periodo;
        NumeroEstudiantesUniversidad = dto.NumeroEstudiantesUniversidad;
        NumeroDocentesUniversidad = dto.NumeroDocentesUniversidad;
        NumeroPersonasPlantaCentral = dto.NumeroPersonasPlantaCentral;
        SueldoBasico = dto.SueldoBasico;
        Funcional = dto.Funcional;
        FondoReserva = dto.FondoReserva;
        BeneficioXiv = dto.BeneficioXiv;
        BeneficioXiii = dto.BeneficioXiii;
        AportePatronal = dto.AportePatronal;
        Varios = dto.Varios;
        MesesCapitalTrabajo = dto.MesesCapitalTrabajo;
        PorcentajeImprevistosInversion = dto.PorcentajeImprevistosInversion;
        PorcentajeMatriculaDefault = dto.PorcentajeMatriculaDefault;
        PorcentajeBecasInstitucionales = dto.PorcentajeBecasInstitucionales;
        SemestresPorAnio = dto.SemestresPorAnio;
        MesesOperativosCiclo = dto.MesesOperativosCiclo;
        PresupuestoAnualCapacitacion = dto.PresupuestoAnualCapacitacion;
        PresupuestoAnualInternacionalizacion = dto.PresupuestoAnualInternacionalizacion;
        PresupuestoAnualMarketing = dto.PresupuestoAnualMarketing;
        PolizaSeguroEstudiantilAnual = dto.PolizaSeguroEstudiantilAnual;
        FuenteInflacion = dto.FuenteInflacion;
        AnioBaseProyeccion = dto.AnioBaseProyeccion;
        FuenteNotas = dto.FuenteNotas;
        MasaSalarialMensual = dto.MasaSalarialMensual;
        TotalAnualPlantaCentral = dto.TotalAnualPlantaCentral;
        RatioAdminPorDocente = dto.RatioAdminPorDocente;
        CostoPorEstudianteAnual = dto.CostoPlantaCentralPorEstudianteAnual;

        var usuario = string.IsNullOrWhiteSpace(dto.ActualizadoPorUsuarioNombre)
            ? $"usuario id={dto.ActualizadoPorUsuarioId}"
            : dto.ActualizadoPorUsuarioNombre;
        UltimaActualizacionTexto = $"Ultima actualizacion: {dto.FechaActualizacion.LocalDateTime:dd/MM/yyyy HH:mm} por {usuario}";
    }
}
