using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SistemaAranceles.Application.Options;
using SistemaAranceles.Application.UseCases.Autenticacion;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.Services;

public sealed class ServicioInactividad : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private readonly TimeSpan _timeout;
    private readonly System.Timers.Timer _timerCountdown;
    private DateTime _ultimaActividad;
    private int _cerrandoPorInactividad;

    public event EventHandler<TimeSpan>? TiempoRestanteActualizado;

    public TimeSpan TiempoRestante
    {
        get
        {
            var tiempoRestante = _ultimaActividad.Add(_timeout) - DateTime.UtcNow;
            return tiempoRestante > TimeSpan.Zero ? tiempoRestante : TimeSpan.Zero;
        }
    }

    public ServicioInactividad(
        IServiceProvider serviceProvider,
        SesionActual sesionActual,
        IOptions<SesionOpciones> opciones)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
        _timeout = TimeSpan.FromMinutes(opciones.Value.TimeoutMinutes);
        _ultimaActividad = DateTime.UtcNow;

        // Timer en ThreadPool (no DispatcherTimer): el tick sigue corriendo aunque
        // el hilo UI esté bloqueado por consultas pesadas (el countdown no se congela).
        _timerCountdown = new System.Timers.Timer(1000) { AutoReset = true };
        _timerCountdown.Elapsed += OnElapsedCountdown;
    }

    public void Iniciar()
    {
        _ultimaActividad = DateTime.UtcNow;
        Interlocked.Exchange(ref _cerrandoPorInactividad, 0);
        _timerCountdown.Stop();
        _timerCountdown.Start();
        TiempoRestanteActualizado?.Invoke(this, TiempoRestante);
        Trace.WriteLine($"[{DateTime.UtcNow:O}] ServicioInactividad: timer iniciado. Timeout={_timeout.TotalMinutes}min.");
    }

    public void Detener()
    {
        _timerCountdown.Stop();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] ServicioInactividad: timer detenido.");
    }

    public void ResetarActividad()
    {
        _ultimaActividad = DateTime.UtcNow;
        TiempoRestanteActualizado?.Invoke(this, TiempoRestante);
    }

    private void OnElapsedCountdown(object? sender, ElapsedEventArgs e)
    {
        if (!_sesionActual.EstaAutenticado)
        {
            _timerCountdown.Stop();
            return;
        }

        var ahora = DateTime.UtcNow;
        var expiracion = _ultimaActividad.Add(_timeout);
        var tiempoRestante = expiracion - ahora;

        if (tiempoRestante.TotalSeconds <= 0)
        {
            tiempoRestante = TimeSpan.Zero;
            _ = Task.Run(CerrarSesionPorInactividadAsync);
        }

        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            () => TiempoRestanteActualizado?.Invoke(this, tiempoRestante));
    }

    private async Task CerrarSesionPorInactividadAsync()
    {
        if (Interlocked.Exchange(ref _cerrandoPorInactividad, 1) == 1)
            return;

        _timerCountdown.Stop();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] ServicioInactividad: inactividad detectada tras {_timeout.TotalMinutes}min. Iniciando cierre de sesión.");

        if (!_sesionActual.EstaAutenticado)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] ServicioInactividad: sesión ya cerrada, sin acción.");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cerrarSesionUseCase = scope.ServiceProvider.GetRequiredService<CerrarSesionUseCase>();
            await cerrarSesionUseCase.EjecutarAsync(_sesionActual.UsuarioId, _sesionActual.TokenSesion);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] ServicioInactividad: error al revocar sesión -> {ex.Message}.");
        }
        finally
        {
            _sesionActual.CerrarSesion();
            _timerCountdown.Stop();
            WeakReferenceMessenger.Default.Send(new CerrarSesionMensaje(PorInactividad: true));
            Trace.WriteLine($"[{DateTime.UtcNow:O}] ServicioInactividad: CerrarSesionMensaje(PorInactividad=true) enviado.");
        }
    }

    public void Dispose()
    {
        _timerCountdown.Stop();
        _timerCountdown.Elapsed -= OnElapsedCountdown;
        _timerCountdown.Dispose();
    }
}
