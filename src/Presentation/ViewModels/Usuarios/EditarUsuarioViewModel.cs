using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Permisos;
using SistemaAranceles.Application.DTOs.Roles;
using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Permisos;
using SistemaAranceles.Application.UseCases.Usuarios;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.State;
using System.Diagnostics;

namespace SistemaAranceles.Presentation.ViewModels.Usuarios;

// ---------------------------------------------------------------------------
// Clases auxiliares para la UI de permisos (dentro del mismo namespace)
// ---------------------------------------------------------------------------

/// <summary>Item de checkbox para un permiso individual en la UI.</summary>
public sealed partial class PermisoCheckboxItem : ObservableObject
{
    public int PermisoId { get; init; }
    public string AccionNombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>True si este permiso proviene del rol base del usuario (sin override).</summary>
    public bool EsDeRolBase { get; init; }

    /// <summary>Estado efectivo que el admin desea para este usuario.</summary>
    [ObservableProperty]
    private bool _tieneAcceso;
}

/// <summary>Agrupa los permisos de un módulo para el ItemsControl de la UI.</summary>
public sealed class ModuloPermisosVm
{
    public string ModuloNombre { get; init; } = string.Empty;
    public ObservableCollection<PermisoCheckboxItem> Permisos { get; } = [];
}

// ---------------------------------------------------------------------------
// ViewModel principal
// ---------------------------------------------------------------------------

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
    [ObservableProperty] private bool _isCargandoDatos;
    [ObservableProperty] private bool _isCargandoPermisos;
    [ObservableProperty] private bool _estaGuardando;
    [ObservableProperty] private bool _esNuevo = true;

    public ObservableCollection<RolDto> Roles { get; } = [];
    public ObservableCollection<string> Estados { get; } = ["Activo", "Suspendido", "Inactivo"];

    /// <summary>Módulos con sus permisos en checkboxes. Visible solo al editar usuario existente.</summary>
    public ObservableCollection<ModuloPermisosVm> ModulosPermisos { get; } = [];

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

        // Carga de permisos separada del overlay principal (no bloquea el formulario)
        if (!EsNuevo)
            await CargarPermisosAsync(_usuarioId);
    }

    private async Task CargarRolesAsync()
    {
        Roles.Clear();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repositorioRol = scope.ServiceProvider.GetRequiredService<IRepositorioRol>();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var roles = await repositorioRol.ListarAsync(cts.Token);
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

    private async Task CargarPermisosAsync(int usuarioId)
    {
        ModulosPermisos.Clear();
        IsCargandoPermisos = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ObtenerPermisosEfectivosUsuarioUseCase>();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var permisos = await useCase.EjecutarAsync(usuarioId, cts.Token);

            if (permisos.Count == 0)
                return;

            var grupos = permisos.GroupBy(p => p.ModuloNombre);
            foreach (var grupo in grupos.OrderBy(g => g.Key))
            {
                var modulo = new ModuloPermisosVm { ModuloNombre = grupo.Key };
                foreach (var p in grupo.OrderBy(x => x.AccionNombre))
                {
                    modulo.Permisos.Add(new PermisoCheckboxItem
                    {
                        PermisoId = p.Id,
                        AccionNombre = p.AccionNombre,
                        Descripcion = p.Descripcion,
                        EsDeRolBase = p.EsDeRolBase,
                        TieneAcceso = p.TieneAcceso
                    });
                }
                ModulosPermisos.Add(modulo);
            }

            Trace.WriteLine($"[{DateTime.UtcNow:O}] EditarUsuarioViewModel: permisos cargados para usuarioId={usuarioId}. Módulos={ModulosPermisos.Count}.");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] EditarUsuarioViewModel: error cargando permisos -> {ex.Message}.");
        }
        finally
        {
            IsCargandoPermisos = false;
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
                throw new InvalidOperationException("No se pudo guardar por una falla transitoria de conexión. Intente nuevamente.");

            // Guardar overrides de permisos (solo en edición, no en creación)
            if (!EsNuevo && ModulosPermisos.Count > 0)
                await GuardarOverridesAsync(ctsGuardar.Token);

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

    private async Task GuardarOverridesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Solo se envían los permisos donde el estado deseado difiere del rol base
            var overrides = ModulosPermisos
                .SelectMany(m => m.Permisos)
                .Where(p => p.TieneAcceso != p.EsDeRolBase)
                .Select(p => new PermisoOverrideDto { PermisoId = p.PermisoId, Concedido = p.TieneAcceso })
                .ToList();

            using var scope = _serviceProvider.CreateScope();
            var actualizarPermisosUseCase = scope.ServiceProvider.GetRequiredService<ActualizarPermisosUsuarioUseCase>();
            await actualizarPermisosUseCase.EjecutarAsync(_usuarioId, overrides, _sesionActual.UsuarioId, cancellationToken);

            Trace.WriteLine($"[{DateTime.UtcNow:O}] EditarUsuarioViewModel: overrides guardados para usuarioId={_usuarioId}. Total={overrides.Count}.");
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] EditarUsuarioViewModel: error guardando overrides -> {ex.Message}. El usuario fue actualizado pero los permisos no.");
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

        var mensaje = ex.Message.ToLowerInvariant();
        if (mensaje.Contains("stream", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("transient", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("likely due to a transient failure", StringComparison.OrdinalIgnoreCase))
            return true;

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
