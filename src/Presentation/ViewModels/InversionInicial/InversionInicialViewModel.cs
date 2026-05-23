using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.InversionInicial;
using SistemaAranceles.Application.UseCases.InversionInicial;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.InversionInicial;

public sealed partial class InversionInicialViewModel : ObservableObject
{
    private readonly IServiceProvider _sp;
    private readonly SesionActual _sesion;
    private int _carreraId = 1;
    private int? _escenarioProyeccionId;

    public InversionInicialViewModel(IServiceProvider sp, SesionActual sesion)
    {
        _sp = sp;
        _sesion = sesion;
    }

    [ObservableProperty] private InversionInicialTotalDto? _inversion;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private string _mensajeError = string.Empty;

    [RelayCommand]
    private Task CargarAsync(int? carreraId = null)
        => CargarParaContextoAsync(carreraId, _escenarioProyeccionId);

    public async Task CargarParaContextoAsync(int? carreraId = null, int? escenarioProyeccionId = null)
    {
        _carreraId = carreraId ?? _carreraId;
        _escenarioProyeccionId = escenarioProyeccionId;
        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        try
        {
            using var scope = _sp.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ObtenerInversionInicialTotalQuery>();
            Inversion = await query.EjecutarAsync(_carreraId, _escenarioProyeccionId);
        }
        catch (Exception ex) { MensajeError = ex.Message; }
        finally { EstaCargando = false; }
    }
}
