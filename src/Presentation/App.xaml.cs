using System.IO;
using System.Diagnostics;
using Npgsql;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.UseCases.Autenticacion;
using SistemaAranceles.Application.UseCases.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.DI;
using SistemaAranceles.Infrastructure.Persistence;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.State;
using SistemaAranceles.Presentation.ViewModels;
using SistemaAranceles.Presentation.ViewModels.Usuarios;
using SistemaAranceles.Presentation.Views;

namespace SistemaAranceles.Presentation;

public partial class App
{
    private ServiceProvider? _proveedor;
    private TextWriterTraceListener? _traceListener;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        ConfigurarTrazas();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] App startup iniciado.");

        var cadenaConexion = ObtenerCadenaConexion();
        var origen = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION") is null ? "appsettings" : "env";
        var endpoint = ExtraerEndpoint(cadenaConexion);
        Trace.WriteLine($"[{DateTime.UtcNow:O}] Cadena de conexión cargada. Origen={origen}. Endpoint={endpoint}");

        var servicios = new ServiceCollection();
        ConfigurarServicios(servicios, cadenaConexion);
        _proveedor = servicios.BuildServiceProvider();

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

    private static void ConfigurarServicios(IServiceCollection servicios, string cadenaConexion)
    {
        // Infrastructure (DbContext + repos + servicios)
        servicios.AddInfrastructure(cadenaConexion);

        // Estado de sesión (singleton)
        servicios.AddSingleton<SesionActual>();

        // Use Cases
        servicios.AddTransient<LoginUseCase>();
        servicios.AddTransient<CerrarSesionUseCase>();
        servicios.AddTransient<ListarUsuariosUseCase>();
        servicios.AddTransient<ObtenerUsuarioUseCase>();
        servicios.AddTransient<CrearUsuarioUseCase>();
        servicios.AddTransient<ActualizarUsuarioUseCase>();
        servicios.AddTransient<EliminarUsuarioUseCase>();

        // ViewModels
        servicios.AddTransient<LoginViewModel>();
        servicios.AddTransient<UsuariosViewModel>();
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
            mainWindow.Show();

            loginView?.Close();
        });

        WeakReferenceMessenger.Default.Register<CerrarSesionMensaje>(this, (_, _) =>
        {
            var mainWindow = Current.Windows.OfType<MainWindow>().FirstOrDefault();

            var loginView = _proveedor!.GetRequiredService<LoginView>();
            loginView.Show();

            mainWindow?.Close();
        });
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

    private static string ObtenerCadenaConexion()
    {
        var cadenaPorVariable = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION");
        if (!string.IsNullOrWhiteSpace(cadenaPorVariable))
            return cadenaPorVariable;

        var basePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .Build();

        var conexion = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conexion))
            throw new InvalidOperationException(
                "No se encontró cadena de conexión. Configure SUPABASE_DB_CONNECTION o appsettings.Local.json.");

        return SupabaseConnectionStringHelper.Normalizar(conexion);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Trace.WriteLine($"[{DateTime.UtcNow:O}] App exit.");
        _traceListener?.Flush();
        _traceListener?.Close();

        _proveedor?.Dispose();
        base.OnExit(e);
    }
}
