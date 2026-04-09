using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Roles;
using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Usuarios;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.State;
using System.Diagnostics;

namespace SistemaAranceles.Presentation.ViewModels.Usuarios;

public sealed partial class EditarUsuarioViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;

    private int _usuarioId;

    public EditarUsuarioViewModel(
        IServiceProvider serviceProvider,
        SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty] private string _titulo = "Nuevo Usuario";
    [ObservableProperty] private string _nombreCompleto = string.Empty;
    [ObservableProperty] private string _correoInstitucional = string.Empty;
    [ObservableProperty] private string _contrasena = string.Empty;
    [ObservableProperty] private string _rolSeleccionado = string.Empty;
    [ObservableProperty] private string _estadoSeleccionado = "Activo";
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private bool _isCargandoDatos; // "Cargando datos del usuario..."
    [ObservableProperty] private bool _estaGuardando;
    [ObservableProperty] private bool _esNuevo = true;

    public ObservableCollection<RolDto> Roles { get; } = [];
    public ObservableCollection<string> Estados { get; } = ["Activo", "Suspendido", "Inactivo"];

    public async Task InicializarAsync(UsuarioDto? usuarioExistente = null)
    {
        IsCargandoDatos = true;
        try
        {
            await CargarRolesAsync();

            if (usuarioExistente is not null)
            {
                _usuarioId = usuarioExistente.Id;
                EsNuevo = false;
                Titulo = "Editar Usuario";
                NombreCompleto = usuarioExistente.NombreCompleto;
                CorreoInstitucional = usuarioExistente.CorreoInstitucional;
                EstadoSeleccionado = usuarioExistente.Estado;
                RolSeleccionado = usuarioExistente.Roles.FirstOrDefault() ?? string.Empty;
            }
            else
            {
                EsNuevo = true;
                Titulo = "Nuevo Usuario";
                RolSeleccionado = Roles.FirstOrDefault()?.Nombre ?? string.Empty;
            }
        }
        finally
        {
            IsCargandoDatos = false;
        }
    }

    private async Task CargarRolesAsync()
    {
        Roles.Clear();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repositorioRol = scope.ServiceProvider.GetRequiredService<IRepositorioRol>();

            var listarRolesTask = repositorioRol.ListarAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(12));
            var completed = await Task.WhenAny(listarRolesTask, timeoutTask);

            if (completed != listarRolesTask)
            {
                throw new TimeoutException("Timeout al cargar catálogo de roles.");
            }

            var roles = await listarRolesTask;

            foreach (var r in roles)
                Roles.Add(new RolDto { Id = r.Id, Nombre = r.Nombre, Descripcion = r.Descripcion });
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] EditarUsuarioViewModel: no se pudieron cargar roles desde BD -> {ex.Message}. Se usará catálogo local.");
            Roles.Add(new RolDto { Id = 1, Nombre = "Administrador", Descripcion = "Acceso total al sistema" });
            Roles.Add(new RolDto { Id = 2, Nombre = "Analista", Descripcion = "Acceso a módulos financieros" });
            Roles.Add(new RolDto { Id = 3, Nombre = "Visualizador", Descripcion = "Acceso de solo lectura" });
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        if (string.IsNullOrWhiteSpace(NombreCompleto) || string.IsNullOrWhiteSpace(CorreoInstitucional))
        {
            MensajeError = "Nombre y correo son obligatorios.";
            return;
        }

        EstaGuardando = true;
        try
        {
            using var ctsGuardar = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            var guardadoExitoso = false;
            for (var intento = 1; intento <= 3; intento++)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    if (EsNuevo)
                    {
                        if (string.IsNullOrWhiteSpace(Contrasena) || Contrasena.Length < 8)
                        {
                            MensajeError = "La contraseña debe tener al menos 8 caracteres.";
                            return;
                        }

                        var crearUseCase = scope.ServiceProvider.GetRequiredService<CrearUsuarioUseCase>();
                        await crearUseCase.EjecutarAsync(new CrearUsuarioDto
                        {
                            NombreCompleto = NombreCompleto,
                            CorreoInstitucional = CorreoInstitucional,
                            Contrasena = Contrasena,
                            RolId = ObtenerRolIdSeleccionado(),
                            RolNombre = RolSeleccionado
                        }, _sesionActual.UsuarioId, ctsGuardar.Token);

                        MensajeExito = "Usuario creado correctamente.";
                    }
                    else
                    {
                        var actualizarUseCase = scope.ServiceProvider.GetRequiredService<ActualizarUsuarioUseCase>();
                        await actualizarUseCase.EjecutarAsync(new ActualizarUsuarioDto
                        {
                            Id = _usuarioId,
                            NombreCompleto = NombreCompleto,
                            CorreoInstitucional = CorreoInstitucional,
                            NuevaContrasena = string.IsNullOrWhiteSpace(Contrasena) ? null : Contrasena,
                            RolNombre = RolSeleccionado,
                            Estado = EstadoSeleccionado
                        }, _sesionActual.UsuarioId, ctsGuardar.Token);

                        MensajeExito = "Usuario actualizado correctamente.";
                    }

                    guardadoExitoso = true;
                    break;
                }
                catch (Exception ex) when (intento < 3 && EsErrorTransitorio(ex))
                {
                    Trace.WriteLine($"[{DateTime.UtcNow:O}] EditarUsuarioViewModel: error transitorio en GuardarAsync (intento {intento}) -> {ex.Message}. Reintentando.");
                    await Task.Delay(400, ctsGuardar.Token);
                }
            }

            if (!guardadoExitoso)
            {
                throw new InvalidOperationException("No se pudo guardar por una falla transitoria de conexión. Intente nuevamente.");
            }

            WeakReferenceMessenger.Default.Send(new NavegarAMensaje("Usuarios", MensajeExito));
        }
        catch (OperationCanceledException)
        {
            MensajeError = "La operación tardó demasiado. Verifique conexión a Supabase e intente nuevamente.";
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] EditarUsuarioViewModel: error en GuardarAsync -> {ex}");
            MensajeError = ex.Message;
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    private int ObtenerRolIdSeleccionado()
    {
        var rol = Roles.FirstOrDefault(r => string.Equals(r.Nombre, RolSeleccionado, StringComparison.OrdinalIgnoreCase));
        return rol?.Id ?? 0;
    }

    private static bool EsErrorTransitorio(Exception ex)
    {
        if (ex is TimeoutException || ex is OperationCanceledException)
            return true;

        // Revisar mensaje de la excepción actual
        var mensaje = ex.Message.ToLowerInvariant();
        if (mensaje.Contains("stream", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("transient", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("likely due to a transient failure", StringComparison.OrdinalIgnoreCase))
            return true;

        // Revisar InnerException (EF wrappea errors)
        if (ex.InnerException != null)
        {
            var innerMensaje = ex.InnerException.Message.ToLowerInvariant();
            if (innerMensaje.Contains("stream", StringComparison.OrdinalIgnoreCase)
                || innerMensaje.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                || innerMensaje.Contains("transient", StringComparison.OrdinalIgnoreCase)
                || innerMensaje.Contains("pooling", StringComparison.OrdinalIgnoreCase)
                || innerMensaje.Contains("connection", StringComparison.OrdinalIgnoreCase)
                || ex.InnerException is TimeoutException
                || ex.InnerException is OperationCanceledException)
                return true;
        }

        return false;
    }
}
