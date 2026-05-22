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
    public MantenimientoViewModel Mantenimiento { get; } = mantenimiento;
    public ActivoDiferidoViewModel ActivoDiferido { get; } = activoDiferido;
    public InversionInicialViewModel InversionInicial { get; } = inversionInicial;

    [ObservableProperty] private string _mensaje = string.Empty;

    public async Task CargarAsync()
    {
        Mensaje = string.Empty;
        await Mantenimiento.CargarCommand.ExecuteAsync(null);

        var carreraId = Mantenimiento.CarreraSeleccionada?.Id;
        if (carreraId is null or <= 0)
        {
            await ActivoDiferido.CargarCommand.ExecuteAsync(null);
            await InversionInicial.CargarCommand.ExecuteAsync(null);
            return;
        }

        await ActivoDiferido.CargarCommand.ExecuteAsync(carreraId);
        await InversionInicial.CargarCommand.ExecuteAsync(carreraId);
    }
}
