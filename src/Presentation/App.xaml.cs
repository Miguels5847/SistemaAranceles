using System.IO;
using System.Diagnostics;
using System.Globalization;
using Npgsql;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using SistemaAranceles.Infrastructure.DI;
using SistemaAranceles.Infrastructure.Persistence;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Application.Options;
using SistemaAranceles.Application.UseCases.Auditoria;
using SistemaAranceles.Application.UseCases.Autenticacion;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.UseCases.Inflacion;
using SistemaAranceles.Application.UseCases.TasaRetencion;
using SistemaAranceles.Application.UseCases.TasaRetencion.Validadores;
using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Application.UseCases.Estudiantes.Validadores;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Presentation.ViewModels.CargosFacultad;
using SistemaAranceles.Presentation.ViewModels.Estudiantes;
using SistemaAranceles.Application.UseCases.Usuarios;
using SistemaAranceles.Application.UseCases.Permisos;
using SistemaAranceles.Presentation.Services;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Presentation.State;
using SistemaAranceles.Presentation.ViewModels;
using SistemaAranceles.Presentation.ViewModels.Auditoria;
using SistemaAranceles.Presentation.ViewModels.Carreras;
using SistemaAranceles.Presentation.ViewModels.Inflacion;
using SistemaAranceles.Presentation.ViewModels.TasaRetencion;
using SistemaAranceles.Presentation.ViewModels.Usuarios;
using SistemaAranceles.Presentation.Views;

namespace SistemaAranceles.Presentation;

public partial class App
{
    private ServiceProvider? _proveedor;
    public static IServiceProvider? ServiceProvider { get; private set; }
    private TextWriterTraceListener? _traceListener;
    private ServicioInactividad? _servicioInactividad;
    private DateTime _lastWindowDeactivated = DateTime.MinValue;
    private bool _ignorarPrimerInputTrasActivacion;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        ConfigurarTrazas();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] App startup iniciado.");

        var config = ConstruirConfiguracion();
        var cadenaConexion = ObtenerCadenaConexion(config);
        var origen = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION") is null ? "appsettings" : "env";
        var endpoint = ExtraerEndpoint(cadenaConexion);
        Trace.WriteLine($"[{DateTime.UtcNow:O}] Cadena de conexión cargada. Origen={origen}. Endpoint={endpoint}");

        var servicios = new ServiceCollection();
        ConfigurarServicios(servicios, cadenaConexion, config);
        _proveedor = servicios.BuildServiceProvider();
        ServiceProvider = _proveedor;

        _servicioInactividad = _proveedor.GetRequiredService<ServicioInactividad>();

        RegistrarMensajes();

        var loginView = _proveedor.GetRequiredService<LoginView>();
        loginView.Show();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginView mostrada.");
    }

    private void ConfigurarTrazas()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SistemaAranceles",
            "logs");

        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, $"app-{DateTime.Now:yyyyMMdd}.log");

        _traceListener = new TextWriterTraceListener(logPath);
        Trace.Listeners.Add(_traceListener);
        Trace.AutoFlush = true;
    }

    private static void ConfigurarServicios(IServiceCollection servicios, string cadenaConexion, IConfiguration config)
    {
        // Infrastructure (DbContext + repos + servicios)
        servicios.AddInfrastructure(cadenaConexion);

        // Configuración de sesión
        var timeoutMinutes = int.TryParse(config["Session:TimeoutMinutes"], out var t) ? t : 30;
        servicios.Configure<SesionOpciones>(opts => opts.TimeoutMinutes = timeoutMinutes);
        servicios.Configure<InflacionOpciones>(opts =>
        {
            opts.MetodoProyeccion = config["Inflacion:MetodoProyeccion"] ?? "promedio-suave";

            if (TryParseDecimalInvariant(config["Inflacion:PisoMinimoProyeccion"], out var piso))
                opts.PisoMinimoProyeccion = piso;

            if (TryParseDecimalInvariant(config["Inflacion:TechoEstableSinHistorial"], out var techo))
                opts.TechoEstableSinHistorial = techo;

            if (int.TryParse(config["Inflacion:VentanaAniosRecientes"], out var ventana))
                opts.VentanaAniosRecientes = ventana;

            if (TryParseDecimalInvariant(config["Inflacion:ValorObjetivoConvergencia"], out var objetivo))
                opts.ValorObjetivoConvergencia = objetivo;

            if (TryParseDecimalInvariant(config["Inflacion:FactorConvergenciaAnual"], out var factor))
                opts.FactorConvergenciaAnual = factor;

            if (TryParseDecimalInvariant(config["Inflacion:TechoMaximoProyeccion"], out var techoMaximo))
                opts.TechoMaximoProyeccion = techoMaximo;
        });

        // Estado de sesión (singleton)
        servicios.AddSingleton<SesionActual>();

        // Servicio de inactividad (singleton — contiene el timer)
        servicios.AddSingleton<ServicioInactividad>();

        // Use Cases
        servicios.AddTransient<LoginUseCase>();
        servicios.AddTransient<CerrarSesionUseCase>();
        servicios.AddTransient<ListarUsuariosUseCase>();
        servicios.AddTransient<ObtenerUsuarioUseCase>();
        servicios.AddTransient<CrearUsuarioUseCase>();
        servicios.AddTransient<ActualizarUsuarioUseCase>();
        servicios.AddTransient<EliminarUsuarioUseCase>();
        servicios.AddTransient<ObtenerPermisosEfectivosUsuarioUseCase>();
        servicios.AddTransient<ActualizarPermisosUsuarioUseCase>();
        servicios.AddTransient<ConsultarAuditoriaUseCase>();
        servicios.AddTransient<ObtenerCargosFacultadPorCarreraQuery>();
        servicios.AddTransient<AgregarCargoFacultadCommand>();
        servicios.AddTransient<ActualizarCargoFacultadCommand>();
        servicios.AddTransient<EliminarCargoFacultadCommand>();
        servicios.AddTransient<ListarProyeccionesCargoFacultadPorCarreraQuery>();
        servicios.AddTransient<GuardarProyeccionCargoFacultadCommand>();
        servicios.AddTransient<EliminarProyeccionCargoFacultadCommand>();
        servicios.AddTransient<ListarCargoPlantaCentralQuery>();
        servicios.AddTransient<GuardarCargoPlantaCentralCommand>();
        servicios.AddTransient<EliminarCargoPlantaCentralCommand>();
        servicios.AddTransient<CalcularProyeccionesCargoPlantaCentralCommand>();
        servicios.AddTransient<ListarProyeccionesCargoPlantaCentralQuery>();
        servicios.AddTransient<ObtenerConsolidadoSueldosPeriodoQuery>();
        servicios.AddTransient<GenerarTablaSueldosPeriodoQuery>();
        servicios.AddTransient<GenerarResumenSueldosQuery>();
        servicios.AddTransient<ListarCarrerasConProyeccionQuery>();
        servicios.AddTransient<ListarEscenariosConProyeccionPorCarreraQuery>();
        servicios.AddTransient<ListarPeriodosDeProyeccionEstudiantesQuery>();
        servicios.AddTransient<ListarPeriodosAcademicosQuery>();
        servicios.AddTransient<ListarOpcionesInflacionPorAnioQuery>();
        servicios.AddTransient<ObtenerInflacionPorAnioQuery>();
        servicios.AddTransient<ListarInflacionAnualUseCase>();
        servicios.AddTransient<CrearInflacionAnualUseCase>();
        servicios.AddTransient<ActualizarInflacionAnualUseCase>();
        servicios.AddTransient<EliminarInflacionAnualUseCase>();
        servicios.AddTransient<ImportarInflacionUseCase>();
        servicios.AddTransient<LimpiarInflacionUseCase>();
        servicios.AddTransient<ImportarInflacionBceArchivoUseCase>();
        servicios.AddTransient<ProyectarInflacionUseCase>();
        servicios.AddTransient<ObtenerInflacionProyectadaParaDependientesUseCase>();
        servicios.AddTransient<SemillaCapitalTrabajoService>();

        // KAN-13: Tasa de Retención — Configuración
        servicios.AddTransient<IValidator<CrearConfiguracionRetencionDto>, CrearConfiguracionRetencionDtoValidador>();
        servicios.AddTransient<IValidator<ActualizarConfiguracionRetencionDto>, ActualizarConfiguracionRetencionDtoValidador>();
        servicios.AddTransient<IValidator<GuardarCriterioReferenciaRetencionDto>, GuardarCriterioReferenciaRetencionDtoValidador>();
        servicios.AddTransient<IValidator<CrearSimulacionRetencionDto>, CrearSimulacionRetencionDtoValidador>();
        servicios.AddTransient<IValidator<ActualizarSimulacionRetencionDto>, ActualizarSimulacionRetencionDtoValidador>();
        servicios.AddTransient<ListarConfiguracionesRetencionUseCase>();
        servicios.AddTransient<ObtenerConfiguracionRetencionUseCase>();
        servicios.AddTransient<CrearConfiguracionRetencionUseCase>();
        servicios.AddTransient<ActualizarConfiguracionRetencionUseCase>();
        servicios.AddTransient<EliminarConfiguracionRetencionUseCase>();
        servicios.AddTransient<CrearCriterioReferenciaRetencionUseCase>();
        servicios.AddTransient<ActualizarCriterioReferenciaRetencionUseCase>();
        servicios.AddTransient<CrearSimulacionRetencionUseCase>();
        servicios.AddTransient<ActualizarSimulacionRetencionUseCase>();
        servicios.AddTransient<ListarSimulacionesRetencionUseCase>();
        servicios.AddTransient<ObtenerSimulacionRetencionUseCase>();
        servicios.AddTransient<EliminarSimulacionRetencionUseCase>();
        servicios.AddTransient<LimpiarSimulacionesRetencionUseCase>();
        servicios.AddScoped<ObtenerValoresSugeridosParaEscenarioUseCase>();

        // Catálogos: vistas y viewmodels
        servicios.AddTransient<SistemaAranceles.Presentation.ViewModels.Catalogos.CatalogoCargosViewModel>();
        servicios.AddTransient<SistemaAranceles.Presentation.ViewModels.Catalogos.CatalogoMaterialesViewModel>();
        servicios.AddTransient<SistemaAranceles.Presentation.ViewModels.CapitalTrabajo.CapitalTrabajoViewModel>();

        // Épica 5 — Estudiantes (KAN-17)
        servicios.AddTransient<IValidator<GenerarProyeccionEstudiantesDto>, GenerarProyeccionEstudiantesDtoValidador>();
        servicios.AddTransient<GenerarProyeccionEstudiantesUseCase>();
        servicios.AddTransient<ObtenerProyeccionEstudiantesUseCase>();
        servicios.AddTransient<ListarProyeccionesEstudiantesUseCase>();
        servicios.AddTransient<EliminarProyeccionEstudiantesUseCase>();
        servicios.AddTransient<EditarConsumoPeriodoUseCase>();
        servicios.AddTransient<RestaurarConsumoPeriodoUseCase>();
        servicios.AddTransient<ListarOverridesHorasPeriodoUseCase>();
        servicios.AddSingleton<ConsolidadoEstudiantesActualState>();
        servicios.AddTransient<CargosFacultadViewModel>();
        servicios.AddTransient<EstudiantesViewModel>();

        // ViewModels
        servicios.AddTransient<LoginViewModel>();
        servicios.AddTransient<SistemaAranceles.Presentation.ViewModels.Catalogos.CatalogoCargosViewModel>();
        servicios.AddTransient<SistemaAranceles.Presentation.ViewModels.Catalogos.CatalogoMaterialesViewModel>();
        servicios.AddTransient<SistemaAranceles.Presentation.ViewModels.CapitalTrabajo.CapitalTrabajoViewModel>();
        servicios.AddTransient<UsuariosViewModel>();
        servicios.AddTransient<AuditoriaViewModel>();
        servicios.AddTransient<CarrerasViewModel>();
        servicios.AddTransient<InflacionViewModel>();
        servicios.AddSingleton<ConfiguracionRetencionViewModel>();
        servicios.AddSingleton<SimulacionRetencionViewModel>();
        servicios.AddTransient<EditarUsuarioViewModel>();
        servicios.AddTransient<Func<EditarUsuarioViewModel>>(sp =>
            () => sp.GetRequiredService<EditarUsuarioViewModel>());
        servicios.AddTransient<MainViewModel>();

        // Views
        servicios.AddTransient<LoginView>();
        servicios.AddTransient<SistemaAranceles.Presentation.Views.Catalogos.CatalogoCargosView>();
        servicios.AddTransient<SistemaAranceles.Presentation.Views.Catalogos.CatalogoMaterialesView>();
        servicios.AddTransient<SistemaAranceles.Presentation.Views.CapitalTrabajo.CapitalTrabajoView>();
        servicios.AddTransient<MainWindow>();
    }

    private static bool TryParseDecimalInvariant(string? value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private void RegistrarMensajes()
    {
        WeakReferenceMessenger.Default.Register<LoginExitosoMensaje>(this, (_, _) =>
        {
            var loginView = Current.Windows.OfType<LoginView>().FirstOrDefault();

            var mainWindow = _proveedor!.GetRequiredService<MainWindow>();

            // Rastrear pérdida de foco para evitar resetear el timer durante la transición
            mainWindow.Deactivated += (_, _) =>
            {
                _lastWindowDeactivated = DateTime.UtcNow;
                Trace.TraceInformation($"[{DateTime.UtcNow:O}] App: MainWindow perdió foco.");
            };

            mainWindow.Activated += (_, _) =>
            {
                // El primer click/tecla tras recuperar foco suele ser sólo para activar ventana.
                _ignorarPrimerInputTrasActivacion = true;
                Trace.TraceInformation($"[{DateTime.UtcNow:O}] App: MainWindow recuperó foco. Se ignorará el primer input para no resetear timer.");
            };

            void RegistrarActividadSiCorresponde()
            {
                if (!mainWindow.IsActive)
                    return;

                if (_ignorarPrimerInputTrasActivacion)
                {
                    _ignorarPrimerInputTrasActivacion = false;
                    return;
                }

                _servicioInactividad?.ResetarActividad();
            }

            // Resetear actividad solo en interacciones intencionales después de que la ventana esté enfocada
            // Si la ventana perdió el foco hace menos de 300ms, no resetear (es solo la transición de foco)
            mainWindow.PreviewMouseDown += (_, _) =>
            {
                RegistrarActividadSiCorresponde();
            };

            mainWindow.PreviewMouseWheel += (_, _) =>
            {
                RegistrarActividadSiCorresponde();
            };

            mainWindow.PreviewKeyDown += (_, _) =>
            {
                RegistrarActividadSiCorresponde();
            };

            mainWindow.Show();
            loginView?.Close();

            _servicioInactividad?.Iniciar();
            Trace.WriteLine($"[{DateTime.UtcNow:O}] App: timer de inactividad iniciado tras login exitoso.");
        });

        WeakReferenceMessenger.Default.Register<CerrarSesionMensaje>(this, (_, msg) =>
        {
            _servicioInactividad?.Detener();

            var mainWindow = Current.Windows.OfType<MainWindow>().FirstOrDefault();

            var loginView = _proveedor!.GetRequiredService<LoginView>();

            if (msg.PorInactividad && loginView.DataContext is LoginViewModel vm)
            {
                vm.MensajeError = "Sesión cerrada por inactividad.";
                Trace.WriteLine($"[{DateTime.UtcNow:O}] App: mensaje de inactividad establecido en LoginView.");
            }

            loginView.Show();
            mainWindow?.Close();
        });
    }

    private static IConfiguration ConstruirConfiguracion()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .Build();
    }

    private static string ExtraerEndpoint(string cadena)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(cadena);
            return $"{builder.Host}:{builder.Port}";
        }
        catch { return "(desconocido)"; }
    }

    private static string ObtenerCadenaConexion(IConfiguration config)
    {
        var cadenaPorVariable = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION");
        if (!string.IsNullOrWhiteSpace(cadenaPorVariable))
            return cadenaPorVariable;

        var conexion = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conexion))
            throw new InvalidOperationException(
                "No se encontró cadena de conexión. Configure SUPABASE_DB_CONNECTION o appsettings.Local.json.");

        return SupabaseConnectionStringHelper.Normalizar(conexion);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Trace.WriteLine($"[{DateTime.UtcNow:O}] App exit.");
        _servicioInactividad?.Detener();
        _traceListener?.Flush();
        _traceListener?.Close();

        _proveedor?.Dispose();
        base.OnExit(e);
    }
}
