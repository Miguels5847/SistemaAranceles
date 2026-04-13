using System.IO;
using System.Diagnostics;
using Npgsql;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.Options;
using SistemaAranceles.Application.UseCases.Auditoria;
using SistemaAranceles.Application.UseCases.Autenticacion;
using SistemaAranceles.Application.UseCases.Inflacion;
using SistemaAranceles.Application.UseCases.Usuarios;
using SistemaAranceles.Application.UseCases.Permisos;
using SistemaAranceles.Infrastructure.DI;
using SistemaAranceles.Infrastructure.Persistence;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.Services;
using SistemaAranceles.Presentation.State;
using SistemaAranceles.Presentation.ViewModels;
using SistemaAranceles.Presentation.ViewModels.Auditoria;
using SistemaAranceles.Presentation.ViewModels.Inflacion;
using SistemaAranceles.Presentation.ViewModels.Usuarios;
using SistemaAranceles.Presentation.Views;

namespace SistemaAranceles.Presentation;

public partial class App
{
    private ServiceProvider? _proveedor;
    private TextWriterTraceListener? _traceListener;
    private ServicioInactividad? _servicioInactividad;

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
        servicios.AddTransient<ListarInflacionAnualUseCase>();
        servicios.AddTransient<CrearInflacionAnualUseCase>();
        servicios.AddTransient<ActualizarInflacionAnualUseCase>();
        servicios.AddTransient<EliminarInflacionAnualUseCase>();
        servicios.AddTransient<ImportarInflacionUseCase>();
        servicios.AddTransient<LimpiarInflacionUseCase>();
        servicios.AddTransient<ImportarInflacionBceArchivoUseCase>();
        servicios.AddTransient<ImportarInflacionBceUseCase>();
        servicios.AddTransient<ProyectarInflacionUseCase>();

        // ViewModels
        servicios.AddTransient<LoginViewModel>();
        servicios.AddTransient<UsuariosViewModel>();
        servicios.AddTransient<AuditoriaViewModel>();
        servicios.AddTransient<InflacionViewModel>();
        servicios.AddTransient<EditarUsuarioViewModel>();
        servicios.AddTransient<Func<EditarUsuarioViewModel>>(sp =>
            () => sp.GetRequiredService<EditarUsuarioViewModel>());
        servicios.AddTransient<MainViewModel>();

        // Views
        servicios.AddTransient<LoginView>();
        servicios.AddTransient<MainWindow>();
    }

    private void RegistrarMensajes()
    {
        WeakReferenceMessenger.Default.Register<LoginExitosoMensaje>(this, (_, _) =>
        {
            var loginView = Current.Windows.OfType<LoginView>().FirstOrDefault();

            var mainWindow = _proveedor!.GetRequiredService<MainWindow>();

            // Resetear actividad ante cualquier movimiento o tecla en la ventana principal
            mainWindow.PreviewMouseMove += (_, _) => _servicioInactividad?.ResetarActividad();
            mainWindow.PreviewKeyDown += (_, _) => _servicioInactividad?.ResetarActividad();

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
