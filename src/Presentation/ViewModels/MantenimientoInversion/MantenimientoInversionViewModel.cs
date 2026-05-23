using CommunityToolkit.Mvvm.ComponentModel;
using SistemaAranceles.Presentation.ViewModels.ActivoDiferido;
using SistemaAranceles.Presentation.ViewModels.InversionInicial;
using SistemaAranceles.Presentation.ViewModels.Mantenimiento;

namespace SistemaAranceles.Presentation.ViewModels.MantenimientoInversion;

public sealed partial class MantenimientoInversionViewModel(
    MantenimientoViewModel mantenimiento,
    ActivoDiferidoViewModel activoDiferido,
    InversionInicialViewModel inversionInicial) : ObservableObject
{
    private bool _cargandoModulo;
    private bool _recargandoPestana;

    public MantenimientoViewModel Mantenimiento { get; } = mantenimiento;
    public ActivoDiferidoViewModel ActivoDiferido { get; } = activoDiferido;
    public InversionInicialViewModel InversionInicial { get; } = inversionInicial;

    [ObservableProperty] private string _mensaje = string.Empty;
    [ObservableProperty] private int _pestanaSeleccionada;

    partial void OnPestanaSeleccionadaChanged(int value)
    {
        if (_cargandoModulo || value == 0)
            return;

        _ = RecargarPestanaActualAsync();
    }

    public async Task CargarAsync()
    {
        if (_cargandoModulo)
            return;

        _cargandoModulo = true;
        Mensaje = string.Empty;
        try
        {
            await Mantenimiento.CargarCommand.ExecuteAsync(null);
            await RecargarActivosDiferidosAsync();
            await RecargarInversionInicialAsync();
        }
        finally
        {
            _cargandoModulo = false;
        }
    }

    private async Task RecargarPestanaActualAsync()
    {
        if (_recargandoPestana)
            return;

        _recargandoPestana = true;
        try
        {
            if (PestanaSeleccionada == 1)
                await RecargarActivosDiferidosAsync();
            else if (PestanaSeleccionada == 2)
                await RecargarInversionInicialAsync();
        }
        finally
        {
            _recargandoPestana = false;
        }
    }

    private Task RecargarActivosDiferidosAsync()
    {
        var carreraId = Mantenimiento.CarreraSeleccionada?.Id;
        if (carreraId is null or <= 0)
            return ActivoDiferido.CargarCommand.ExecuteAsync(null);

        return ActivoDiferido.CargarCommand.ExecuteAsync(carreraId);
    }

    private Task RecargarInversionInicialAsync()
    {
        var carreraId = Mantenimiento.CarreraSeleccionada?.Id;
        var escenarioId = Mantenimiento.EscenarioSeleccionado?.Id;

        if (carreraId is null or <= 0)
            return InversionInicial.CargarParaContextoAsync(null, null);

        return InversionInicial.CargarParaContextoAsync(carreraId, escenarioId);
    }
}
