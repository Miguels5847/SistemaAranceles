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
        else if (_sesionActual.TienePermiso("AF.VER") || _sesionActual.EsAdministrador)
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
        // Solo minutos (redondeo hacia arriba): evita el salto visible por reinicios de actividad
        // o por congelamiento del timer de UI durante consultas pesadas.
        var minutos = (int)Math.Ceiling(tiempo.TotalMinutes);
        return $"{minutos} min";
    }

    private void SeleccionarMenu(string titulo)
    {
        foreach (var item in MenuItems)
            item.EstaSeleccionado = item.Titulo == titulo;
    }

    private void ConstruirMenu()
    {
        MenuItems.Clear();

        if (_sesionActual.TienePermiso("US.VER"))
        {
            MenuItems.Add(new ItemMenu { Titulo = "Usuarios", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarUsuariosAsync()) });
        }

        if (_sesionActual.TienePermiso("CA.VER"))
        {
            MenuItems.Add(new ItemMenu { Titulo = "Carreras", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarCarrerasAsync()) });
        }

        if (_sesionActual.TienePermiso("INF.VER"))
        {
            MenuItems.Add(new ItemMenu { Titulo = "Inflación", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarInflacionAsync()) });
        }

        if (_sesionActual.TienePermiso("TRE.VER") || _sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu { Titulo = "Tasa de Retención y Graduación", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarTasaRetencionAsync()) });
        }

        if (_sesionActual.TienePermiso("ES.VER") || _sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu { Titulo = "Proyección de Estudiantes", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarEstudiantesAsync()) });
        }

        if (_sesionActual.TienePermiso("AF.VER") || _sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu { Titulo = "Sueldos Carrera", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarCargosFacultadAsync()) });
        }

        if (_sesionActual.TienePermiso("DI.VER") || _sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu { Titulo = "Datos Institucionales", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarDatosInstitucionalesAsync()) });
        }

        if (_sesionActual.TienePermiso("PC.VER") || _sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu { Titulo = "Aporte Planta Central", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarAportePlantaCentralAsync()) });
        }

        if (_sesionActual.TienePermiso("RD.VER") || _sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu { Titulo = "Recursos y Depreciación", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarActivosFijosAsync()) });
        }

        if (_sesionActual.EsAdministrador || _sesionActual.TienePermiso("MI.VER"))
        {
            MenuItems.Add(new ItemMenu { Titulo = "Mantenimiento e Inversión", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarMantenimientoInversionAsync()) });
        }

        if (_sesionActual.TienePermiso("AF.VER") || _sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu { Titulo = "Capital de Trabajo", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarCapitalTrabajoAsync()) });
        }

        if (_sesionActual.TienePermiso("DI_NG.VER") || _sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu { Titulo = "Demanda e Ingresos", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarDemandaIngresosAsync()) });
        }

        if (_sesionActual.TienePermiso("CG.VER") || _sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu { Titulo = "Costos y Gastos", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarCostosGastosAsync()) });
        }

        if (_sesionActual.TienePermiso("AF.VER") || _sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu { Titulo = "Análisis Financiero", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarAnalisisFinancieroAsync()) });
        }

        if (_sesionActual.TienePermiso("CFG.VER"))
        {
            MenuItems.Add(new ItemMenu { Titulo = "Configuración", Icono = string.Empty, Comando = new RelayCommand(() => MostrarModuloEnDesarrollo("Configuración", "Pendiente")) });
        }

        if (_sesionActual.TienePermiso("REP.VER"))
        {
            MenuItems.Add(new ItemMenu { Titulo = "Reportes", Icono = string.Empty, Comando = new RelayCommand(() => MostrarModuloEnDesarrollo("Reportes", "Pendiente")) });
        }

        if (_sesionActual.EsAdministrador && _sesionActual.TienePermiso("AUD.VER"))
        {
            MenuItems.Add(new ItemMenu { Titulo = "Auditoría", Icono = string.Empty, Comando = new AsyncRelayCommand(() => MostrarAuditoriaAsync()) });
        }

        MenuItems.Add(new ItemMenu { Titulo = "Cerrar Sesión", Icono = string.Empty, Comando = new RelayCommand(() => _ = CerrarSesionAsync()) });

        Bienvenida = $"Bienvenido, {_sesionActual.NombreCompleto}  |  Rol: {_sesionActual.RolNombre}";
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
        if (!(_sesionActual.TienePermiso("AF.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo de Sueldos."; return; }
        MensajePagina = string.Empty;
        PaginaActual = _cargosFacultadViewModel;
        await _cargosFacultadViewModel.CargarCommand.ExecuteAsync(null);
    }

    private Task MostrarCatalogoCargosAsync() => MostrarCapitalTrabajoAsync();

    private async Task MostrarCapitalTrabajoAsync()
    {
        SeleccionarMenu("Capital de Trabajo");
        if (!(_sesionActual.TienePermiso("AF.VER") || _sesionActual.EsAdministrador)) { MensajePagina = "Acceso denegado al módulo Capital de Trabajo."; return; }
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
