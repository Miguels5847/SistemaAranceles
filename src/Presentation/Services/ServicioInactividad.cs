using System.Diagnostics;
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
    private readonly DispatcherTimer _timer;

    public ServicioInactividad(
        IServiceProvider serviceProvider,
        SesionActual sesionActual,
        IOptions<SesionOpciones> opciones)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
        _timeout = TimeSpan.FromMinutes(opciones.Value.TimeoutMinutes);
        _timer = new DispatcherTimer { Interval = _timeout };
        _timer.Tick += OnTick;
    }

    public void Iniciar()
    {
        _timer.Stop();
        _timer.Start();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] ServicioInactividad: timer iniciado. Timeout={_timeout.TotalMinutes}min.");
    }

    public void Detener()
    {
        _timer.Stop();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] ServicioInactividad: timer detenido.");
    }

    public void ResetarActividad()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
            _timer.Start();
        }
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
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
            WeakReferenceMessenger.Default.Send(new CerrarSesionMensaje(PorInactividad: true));
            Trace.WriteLine($"[{DateTime.UtcNow:O}] ServicioInactividad: CerrarSesionMensaje(PorInactividad=true) enviado.");
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }
}
