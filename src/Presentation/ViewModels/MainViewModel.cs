using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.UseCases.Autenticacion;
using SistemaAranceles.Presentation.ViewModels.CargosFacultad;
using SistemaAranceles.Presentation.ViewModels.DatosInstitucionales;
using SistemaAranceles.Presentation.ViewModels.DemandaIngresos;
using SistemaAranceles.Presentation.ViewModels.PlantaCentral;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.Services;
using SistemaAranceles.Presentation.State;
using SistemaAranceles.Presentation.ViewModels.Auditoria;
using SistemaAranceles.Presentation.ViewModels.AnalisisFinanciero;
using SistemaAranceles.Presentation.ViewModels.Carreras;
using SistemaAranceles.Presentation.ViewModels.CostosGastos;
using SistemaAranceles.Presentation.ViewModels.Estudiantes;
using SistemaAranceles.Presentation.ViewModels.Inflacion;
using SistemaAranceles.Presentation.ViewModels.MantenimientoInversion;
using SistemaAranceles.Presentation.ViewModels.TasaRetencion;
using SistemaAranceles.Presentation.ViewModels.Usuarios;
using System.Diagnostics;

namespace SistemaAranceles.Presentation.ViewModels;

public sealed partial class ItemMenu : ObservableObject
{
    public string Titulo { get; init; } = string.Empty;
    public string Icono { get; init; } = string.Empty;
    public System.Windows.Input.ICommand? Comando { get; init; }
    public bool EsVisible { get; init; } = true;

    /// <summary>Encabezado de grupo del menú: no navega, solo guía el flujo de trabajo.</summary>
    public bool EsEncabezado { get; init; }

    /// <summary>Tooltip que explica qué se hace en el módulo y qué habilita después.</summary>
    public string Descripcion { get; init; } = string.Empty;

    [ObservableProperty] private bool _estaSeleccionado;
}

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private readonly ServicioInactividad _servicioInactividad;
    private readonly UsuariosViewModel _usuariosViewModel;
    private readonly AuditoriaViewModel _auditoriaViewModel;
    private readonly CargosFacultadViewModel _cargosFacultadViewModel;
    private readonly CarrerasViewModel _carrerasViewModel;
    private readonly SistemaAranceles.Presentation.ViewModels.CapitalTrabajo.CapitalTrabajoViewModel _capitalTrabajoViewModel;
    private readonly EstudiantesViewModel _estudiantesViewModel;
    private readonly InflacionViewModel _inflacionViewModel;
    private readonly ConfiguracionRetencionViewModel _configuracionRetencionViewModel;
    private readonly DatosInstitucionalesViewModel _datosInstitucionalesViewModel;
    private readonly AportePlantaCentralViewModel _aportePlantaCentralViewModel;
    private readonly SistemaAranceles.Presentation.ViewModels.RecursosFisicos.ActivosFijosViewModel _activosFijosViewModel;
    private readonly Func<EditarUsuarioViewModel> _editarUsuarioViewModelFactory;
    private int _cerrandoSesion;
    private int _cargandoUsuarios;
    private int _navegando;

    public MainViewModel(
        IServiceProvider serviceProvider,
        SesionActual sesionActual,
        ServicioInactividad servicioInactividad,
        UsuariosViewModel usuariosViewModel,
        AuditoriaViewModel auditoriaViewModel,
        CargosFacultadViewModel cargosFacultadViewModel,
        SistemaAranceles.Presentation.ViewModels.CapitalTrabajo.CapitalTrabajoViewModel capitalTrabajoViewModel,
        CarrerasViewModel carrerasViewModel,
        EstudiantesViewModel estudiantesViewModel,
        InflacionViewModel inflacionViewModel,
        ConfiguracionRetencionViewModel configuracionRetencionViewModel,
        DatosInstitucionalesViewModel datosInstitucionalesViewModel,
        AportePlantaCentralViewModel aportePlantaCentralViewModel,
        SistemaAranceles.Presentation.ViewModels.RecursosFisicos.ActivosFijosViewModel activosFijosViewModel,
        Func<EditarUsuarioViewModel> editarUsuarioViewModelFactory)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
        _servicioInactividad = servicioInactividad;
        _usuariosViewModel = usuariosViewModel;
        _auditoriaViewModel = auditoriaViewModel;
        _cargosFacultadViewModel = cargosFacultadViewModel;
        _capitalTrabajoViewModel = capitalTrabajoViewModel;
        _carrerasViewModel = carrerasViewModel;
        _estudiantesViewModel = estudiantesViewModel;
        _inflacionViewModel = inflacionViewModel;
        _configuracionRetencionViewModel = configuracionRetencionViewModel;
        _datosInstitucionalesViewModel = datosInstitucionalesViewModel;
        _aportePlantaCentralViewModel = aportePlantaCentralViewModel;
        _activosFijosViewModel = activosFijosViewModel;
        _editarUsuarioViewModelFactory = editarUsuarioViewModelFactory;

        _servicioInactividad.TiempoRestanteActualizado += (_, tiempoRestante) =>
        {
            TiempoRestanteSesion = FormatearTiempoRestante(tiempoRestante);
        };
        TiempoRestanteSesion = FormatearTiempoRestante(_servicioInactividad.TiempoRestante);

        WeakReferenceMessenger.Default.Register<NavegarAMensaje>(this, (_, msg) =>
        {
            if (msg.DestinoPagina == "Usuarios")
                LanzarSinEsperar(MostrarUsuariosAsync(msg.MensajeExito));
        });

        WeakReferenceMessenger.Default.Register<EditarUsuarioMensaje>(this, (_, msg) =>
        {
            LanzarSinEsperar(MostrarEditarUsuarioAsync(msg.Usuario));
        });

        // Atajo 4 → 5 del flujo: Análisis Financiero pide abrir Reportes ya preseleccionado.
        WeakReferenceMessenger.Default.Register<AbrirReportesMensaje>(this, (_, msg) =>
        {
            LanzarSinEsperar(MostrarReportesAsync(msg.CarreraId, msg.EscenarioId));
        });

        ConstruirMenu();
        if (_sesionActual.TienePermiso("US.VER"))
        {
            PaginaActual = _usuariosViewModel;
            _ = MostrarUsuariosAsync();
        }
        else if (_sesionActual.TienePermiso("INF.VER"))
        {
            PaginaActual = _inflacionViewModel;
            _ = MostrarInflacionAsync();
        }
        else if (_sesionActual.TienePermiso("SC.VER") || _sesionActual.EsAdministrador)
        {
            PaginaActual = _cargosFacultadViewModel;
            _ = MostrarCargosFacultadAsync();
        }
        else
        {
            PaginaActual = null;
            MensajePagina = MenuItems.Count > 1
                ? "Selecciona un módulo del menú lateral."
                : "Sin módulos disponibles para tu perfil. Contacta al administrador.";
        }
    }

    [ObservableProperty] private string _bienvenida = string.Empty;
    [ObservableProperty] private ObservableObject? _paginaActual;
    [ObservableProperty] private string _mensajePagina = string.Empty;
    [ObservableProperty] private bool _estaCerrandoSesion;
    [ObservableProperty] private string _mensajeCierreSesion = "Cerrando sesión...";
    [ObservableProperty] private string _tiempoRestanteSesion = "45:00";

    public ObservableCollection<ItemMenu> MenuItems { get; } = [];

    public string NombreUsuario => _sesionActual.NombreCompleto;
    public string RolUsuario => _sesionActual.RolNombre;

    private static string FormatearTiempoRestante(TimeSpan tiempo)
    {
        var t = tiempo < TimeSpan.Zero ? TimeSpan.Zero : tiempo;
        // TotalMinutes (no Minutes) para soportar timeouts > 59 min.
        return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
    }

    private void SeleccionarMenu(string titulo)
    {
        foreach (var item in MenuItems)
            item.EstaSeleccionado = item.Titulo == titulo;
    }

    private void ConstruirMenu()
    {
        MenuItems.Clear();

        // El orden replica el flujo de trabajo: primero los datos base, luego la proyección,
        // después los costos, y al final el análisis y los reportes. Los encabezados numerados
        // guían al usuario no técnico sin bloquear la navegación libre.
        AgregarGrupo("Administración",
        [
            Modulo("Usuarios", "AccountGroup", _sesionActual.TienePermiso("US.VER"),
                "Crea y administra los usuarios y sus permisos.",
                () => MostrarUsuariosAsync()),
            Modulo("Auditoría", "History", _sesionActual.EsAdministrador && _sesionActual.TienePermiso("AUD.VER"),
                "Consulta quién hizo qué y cuándo dentro del sistema.",
                () => MostrarAuditoriaAsync())
        ]);

        AgregarGrupo("1 · Configuración base",
        [
            Modulo("Carreras", "School", _sesionActual.TienePermiso("CA.VER"),
                "Punto de partida: registra la carrera que vas a proyectar.",
                () => MostrarCarrerasAsync()),
            Modulo("Inflación", "TrendingUp", _sesionActual.TienePermiso("INF.VER"),
                "Tasas de inflación anuales que ajustan los costos futuros.",
                () => MostrarInflacionAsync()),
            Modulo("Tasa de Retención y Graduación", "AccountConvert", _sesionActual.TienePermiso("TRE.VER") || _sesionActual.EsAdministrador,
                "Define cuántos estudiantes continúan de un ciclo al siguiente.",
                () => MostrarTasaRetencionAsync()),
            Modulo("Datos Institucionales", "Domain", _sesionActual.TienePermiso("DI.VER") || _sesionActual.EsAdministrador,
                "Parámetros generales: % matrícula, becas, meses de capital de trabajo, etc.",
                () => MostrarDatosInstitucionalesAsync())
        ]);

        AgregarGrupo("2 · Proyección académica",
        [
            Modulo("Proyección de Estudiantes", "ChartLine", _sesionActual.TienePermiso("ES.VER") || _sesionActual.EsAdministrador,
                "Crea el escenario y proyecta la matrícula por períodos.",
                () => MostrarEstudiantesAsync()),
            Modulo("Demanda e Ingresos", "CashMultiple", _sesionActual.TienePermiso("DI_NG.VER") || _sesionActual.EsAdministrador,
                "Arancel, descuentos por ciclo, materiales e ingresos del escenario.",
                () => MostrarDemandaIngresosAsync())
        ]);

        AgregarGrupo("3 · Costos y recursos",
        [
            Modulo("Sueldos Carrera", "AccountCash", _sesionActual.TienePermiso("SC.VER") || _sesionActual.EsAdministrador,
                "Cargos docentes y administrativos de la carrera y su proyección.",
                () => MostrarCargosFacultadAsync()),
            Modulo("Aporte Planta Central", "OfficeBuilding", _sesionActual.TienePermiso("PC.VER") || _sesionActual.EsAdministrador,
                "Prorratea el costo de la administración central a la carrera.",
                () => MostrarAportePlantaCentralAsync()),
            Modulo("Recursos y Depreciación", "DesktopClassic", _sesionActual.TienePermiso("RD.VER") || _sesionActual.EsAdministrador,
                "Activos fijos y diferidos con su depreciación y amortización.",
                () => MostrarActivosFijosAsync()),
            Modulo("Mantenimiento e Inversión", "Wrench", _sesionActual.EsAdministrador || _sesionActual.TienePermiso("MI.VER"),
                "Servicios básicos, mantenimiento e inversiones futuras.",
                () => MostrarMantenimientoInversionAsync()),
            Modulo("Capital de Trabajo", "Briefcase", _sesionActual.TienePermiso("CT.VER") || _sesionActual.EsAdministrador,
                "Efectivo necesario para operar los primeros meses.",
                () => MostrarCapitalTrabajoAsync()),
            Modulo("Costos y Gastos", "Calculator", _sesionActual.TienePermiso("CG.VER") || _sesionActual.EsAdministrador,
                "Matriz consolidada de todos los costos del escenario.",
                () => MostrarCostosGastosAsync())
        ]);

        AgregarGrupo("4 · Financiamiento y análisis",
        [
            Modulo("Amortización", "Bank", _sesionActual.TienePermiso("AMO.VER") || _sesionActual.EsAdministrador,
                "Fuentes de financiamiento y tabla de amortización del préstamo.",
                () => MostrarAmortizacionAsync()),
            Modulo("Análisis Financiero", "Finance", _sesionActual.TienePermiso("AF.VER") || _sesionActual.EsAdministrador,
                "P&G, flujo, balance, VAN/TIR, punto de equilibrio y arancel óptimo.",
                () => MostrarAnalisisFinancieroAsync())
        ]);

        AgregarGrupo("5 · Resultados",
        [
            Modulo("Reportes", "FileChart", _sesionActual.TienePermiso("REP.VER") || _sesionActual.EsAdministrador,
                "Exporta el informe por dirección destinataria en PDF o Excel.",
                () => MostrarReportesAsync())
        ]);

        MenuItems.Add(new ItemMenu { Titulo = "Cerrar Sesión", Icono = "Logout", Comando = new RelayCommand(() => _ = CerrarSesionAsync()) });

        Bienvenida = $"Bienvenido, {_sesionActual.NombreCompleto}  |  Rol: {_sesionActual.RolNombre}";
    }

    private static ItemMenu? Modulo(string titulo, string icono, bool visible, string descripcion, Func<Task> mostrar)
        => visible
            ? new ItemMenu { Titulo = titulo, Icono = icono, Descripcion = descripcion, Comando = new AsyncRelayCommand(mostrar) }
            : null;

    private void AgregarGrupo(string encabezado, ItemMenu?[] modulos)
    {
        var visibles = modulos.OfType<ItemMenu>().ToList();
        if (visibles.Count == 0)
            return;

        MenuItems.Add(new ItemMenu { Titulo = encabezado, EsEncabezado = true });
        foreach (var modulo in visibles)
            MenuItems.Add(modulo);
    }

    private void MostrarModuloEnDesarrollo(string modulo, string epica)
    {
        SeleccionarMenu(modulo);
        PaginaActual = null;
        MensajePagina = $"Módulo {modulo} — en desarrollo ({epica}). Se habilitó menú por permisos para pruebas de acceso por rol.";
    }

    [RelayCommand] private Task MostrarUsuarios() => MostrarUsuariosAsync();
    [RelayCommand] private Task MostrarAuditoria() => MostrarAuditoriaAsync();
    [RelayCommand] private Task MostrarInflacion() => MostrarInflacionAsync();
    [RelayCommand] private Task MostrarCarreras() => MostrarCarrerasAsync();
    [RelayCommand] private Task MostrarTasaRetencion() => MostrarTasaRetencionAsync();

    private async Task MostrarEstudiantesAsync()
    {
        SeleccionarMenu("Proyección de Estudiantes");
        if (!(_sesionActual.TienePermiso("ES.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo de Proyección de Estudiantes."; return; }
        MensajePagina = string.Empty;
        PaginaActual = _estudiantesViewModel;
        await _estudiantesViewModel.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarCarrerasAsync()
    {
        SeleccionarMenu("Carreras");
        if (!(_sesionActual.TienePermiso("CA.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo de Carreras."; return; }
        MensajePagina = string.Empty;
        PaginaActual = _carrerasViewModel;
        await _carrerasViewModel.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarTasaRetencionAsync()
    {
        SeleccionarMenu("Tasa de Retención y Graduación");
        var puede = _sesionActual.TienePermiso("TRE.VER") || _sesionActual.EsAdministrador;
        if (!puede) { MensajePagina = "Acceso denegado al módulo de Tasa de Retención."; return; }
        MensajePagina = string.Empty;
        PaginaActual = _configuracionRetencionViewModel;
        await _configuracionRetencionViewModel.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarUsuariosAsync(string? mensajeExito = null)
    {
        SeleccionarMenu("Usuarios");
        if (!_sesionActual.TienePermiso("US.VER")) { MensajePagina = "Acceso denegado a Gestión de Usuarios."; return; }
        if (Interlocked.Exchange(ref _cargandoUsuarios, 1) == 1) { Trace.TraceInformation($"[{DateTime.UtcNow:O}] MainViewModel: MostrarUsuariosAsync ignorado por carga en curso."); return; }
        MensajePagina = string.Empty;
        try
        {
            PaginaActual = _usuariosViewModel;
            await _usuariosViewModel.CargarCommand.ExecuteAsync(null);
            if (!string.IsNullOrWhiteSpace(mensajeExito)) _usuariosViewModel.MensajeExito = mensajeExito;
        }
        finally { Interlocked.Exchange(ref _cargandoUsuarios, 0); }
    }

    private Task MostrarAuditoriaAsync()
    {
        SeleccionarMenu("Auditoría");
        if (!(_sesionActual.EsAdministrador && _sesionActual.TienePermiso("AUD.VER"))) { MensajePagina = "Acceso denegado a Auditoría."; return Task.CompletedTask; }
        MensajePagina = string.Empty;
        PaginaActual = _auditoriaViewModel;
        return Task.CompletedTask;
    }

    private async Task MostrarInflacionAsync()
    {
        SeleccionarMenu("Inflación");
        if (!_sesionActual.TienePermiso("INF.VER")) { MensajePagina = "Acceso denegado al módulo de Inflación."; return; }
        MensajePagina = string.Empty;
        PaginaActual = _inflacionViewModel;
        await _inflacionViewModel.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarDatosInstitucionalesAsync()
    {
        SeleccionarMenu("Datos Institucionales");
        if (!(_sesionActual.TienePermiso("DI.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al modulo Datos Institucionales."; return; }
        MensajePagina = string.Empty;
        PaginaActual = _datosInstitucionalesViewModel;
        await _datosInstitucionalesViewModel.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarAportePlantaCentralAsync()
    {
        SeleccionarMenu("Aporte Planta Central");
        if (!(_sesionActual.TienePermiso("PC.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al modulo Aporte Planta Central."; return; }
        MensajePagina = string.Empty;
        PaginaActual = _aportePlantaCentralViewModel;
        await _aportePlantaCentralViewModel.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarActivosFijosAsync()
    {
        SeleccionarMenu("Recursos y Depreciación");
        if (!(_sesionActual.TienePermiso("RD.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo de Recursos y Depreciación."; return; }
        MensajePagina = string.Empty;
        PaginaActual = _activosFijosViewModel;
        await _activosFijosViewModel.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarCargosFacultadAsync()
    {
        SeleccionarMenu("Sueldos Carrera");
        if (!(_sesionActual.TienePermiso("SC.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo de Sueldos."; return; }
        MensajePagina = string.Empty;
        PaginaActual = _cargosFacultadViewModel;
        await _cargosFacultadViewModel.CargarCommand.ExecuteAsync(null);
    }

    private Task MostrarCatalogoCargosAsync() => MostrarCapitalTrabajoAsync();

    private async Task MostrarCapitalTrabajoAsync()
    {
        SeleccionarMenu("Capital de Trabajo");
        if (!(_sesionActual.TienePermiso("CT.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo Capital de Trabajo."; return; }
        MensajePagina = string.Empty;
        PaginaActual = _capitalTrabajoViewModel;
        await _capitalTrabajoViewModel.CargarCommand.ExecuteAsync(null);
    }

    private Task MostrarCatalogoMaterialesAsync() => MostrarCapitalTrabajoAsync();

    private async Task MostrarDemandaIngresosAsync()
    {
        SeleccionarMenu("Demanda e Ingresos");
        if (!(_sesionActual.TienePermiso("DI_NG.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo Demanda e Ingresos."; return; }
        if (Interlocked.Exchange(ref _navegando, 1) == 1) return;
        MensajePagina = string.Empty;
        try
        {
            // Instancia fresca por navegación (como Costos/Análisis): evita que una recarga en vuelo
            // (cambio de escenario) deje el módulo pegado y sin poder reseleccionarse.
            var vm = _serviceProvider.GetRequiredService<DemandaIngresosViewModel>();
            PaginaActual = vm;
            await vm.CargarCommand.ExecuteAsync(null);
        }
        finally { Interlocked.Exchange(ref _navegando, 0); }
    }

    private async Task MostrarCostosGastosAsync()
    {
        SeleccionarMenu("Costos y Gastos");
        if (!(_sesionActual.TienePermiso("CG.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al modulo Costos y Gastos."; return; }
        MensajePagina = string.Empty;
        var vm = _serviceProvider.GetRequiredService<CostosGastosViewModel>();
        PaginaActual = vm;
        await vm.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarAnalisisFinancieroAsync()
    {
        SeleccionarMenu("Análisis Financiero");
        if (!(_sesionActual.TienePermiso("AF.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo Análisis Financiero."; return; }
        MensajePagina = string.Empty;
        var vm = _serviceProvider.GetRequiredService<AnalisisFinancieroViewModel>();
        PaginaActual = vm;
        await vm.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarReportesAsync(int? carreraId = null, int? escenarioId = null)
    {
        SeleccionarMenu("Reportes");
        if (!(_sesionActual.TienePermiso("REP.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo Reportes."; return; }
        MensajePagina = string.Empty;
        var vm = _serviceProvider.GetRequiredService<ViewModels.Reportes.ReportesViewModel>();
        if (carreraId is > 0 && escenarioId is > 0)
            vm.PrepararPreseleccion(carreraId.Value, escenarioId.Value);
        PaginaActual = vm;
        await vm.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarAmortizacionAsync()
    {
        SeleccionarMenu("Amortización");
        if (!(_sesionActual.TienePermiso("AMO.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo Amortización."; return; }
        MensajePagina = string.Empty;
        var vm = _serviceProvider.GetRequiredService<ViewModels.Amortizacion.AmortizacionViewModel>();
        PaginaActual = vm;
        await vm.CargarCommand.ExecuteAsync(null);
    }

    private async Task MostrarMantenimientoInversionAsync()
    {
        SeleccionarMenu("Mantenimiento e Inversión");
        if (!(_sesionActual.EsAdministrador || _sesionActual.TienePermiso("MI.VER"))) { MensajePagina = "Acceso denegado al módulo de Mantenimiento e Inversión."; return; }
        MensajePagina = string.Empty;
        var vm = _serviceProvider.GetRequiredService<MantenimientoInversionViewModel>();
        PaginaActual = vm;
        await vm.CargarAsync();
    }

    [RelayCommand]
    private async Task MostrarNuevoUsuarioAsync()
    {
        SeleccionarMenu("Usuarios");
        if (!_sesionActual.TienePermiso("US.CREAR")) { MensajePagina = "Acceso denegado. No tiene permiso para crear usuarios."; return; }
        var vm = _editarUsuarioViewModelFactory();
        PaginaActual = vm;
        MensajePagina = string.Empty;
        try { await vm.InicializarAsync(); }
        catch (Exception ex)
        {
            Trace.TraceError($"[{DateTime.UtcNow:O}] MainViewModel: error al inicializar nuevo usuario -> {ex.Message}.");
            vm.MensajeError = "No se pudo inicializar el formulario de usuario. Intente nuevamente.";
        }
    }

    private async Task MostrarEditarUsuarioAsync(UsuarioDto usuario)
    {
        SeleccionarMenu("Usuarios");
        if (!_sesionActual.TienePermiso("US.EDITAR")) { MensajePagina = "Acceso denegado. No tiene permiso para editar usuarios."; return; }
        var vm = _editarUsuarioViewModelFactory();
        PaginaActual = vm;
        MensajePagina = string.Empty;
        try { await vm.InicializarAsync(usuario); }
        catch (Exception ex)
        {
            Trace.TraceError($"[{DateTime.UtcNow:O}] MainViewModel: error al inicializar edición de usuario -> {ex.Message}.");
            vm.MensajeError = "No se pudo cargar los datos del usuario. Intente nuevamente.";
        }
    }

    private async Task CerrarSesionAsync()
    {
        if (Interlocked.Exchange(ref _cerrandoSesion, 1) == 1) { Trace.TraceInformation($"[{DateTime.UtcNow:O}] MainViewModel: CerrarSesionAsync ignorado por ejecución en curso."); return; }
        EstaCerrandoSesion = true;
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] MainViewModel: inicio CerrarSesionAsync.");
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cerrarSesionUseCase = scope.ServiceProvider.GetRequiredService<CerrarSesionUseCase>();
            await cerrarSesionUseCase.EjecutarAsync(_sesionActual.UsuarioId, _sesionActual.TokenSesion);
        }
        catch (Exception ex) { Trace.TraceError($"[{DateTime.UtcNow:O}] MainViewModel: error en CerrarSesionAsync -> {ex.Message}."); }
        finally
        {
            _sesionActual.CerrarSesion();
            WeakReferenceMessenger.Default.UnregisterAll(this);
            WeakReferenceMessenger.Default.Send(new CerrarSesionMensaje());
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] MainViewModel: fin CerrarSesionAsync.");
            EstaCerrandoSesion = false;
            Interlocked.Exchange(ref _cerrandoSesion, 0);
        }
    }

    private static void LanzarSinEsperar(Task tarea)
    {
        _ = tarea.ContinueWith(
            antecedente => Trace.TraceError($"[{DateTime.UtcNow:O}] MainViewModel: tarea en segundo plano falló -> {antecedente.Exception?.GetBaseException().Message}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }
}
