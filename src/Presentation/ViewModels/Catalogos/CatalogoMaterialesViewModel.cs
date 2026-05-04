using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Infrastructure.Persistence;
using SistemaAranceles.Infrastructure.Persistence.Entidades;
using System.Linq;

namespace SistemaAranceles.Presentation.ViewModels.Catalogos;

public sealed partial class CatalogoMaterialesViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    public CatalogoMaterialesViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [ObservableProperty]
    private ObservableCollection<ItemMaterialInsumo> _items = [];

    [ObservableProperty]
    private ItemMaterialInsumo? _seleccionado;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [RelayCommand]
    private async Task CargarAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ContextoAplicacion>();
            var datos = await ctx.ItemsMaterialInsumo.OrderBy(x => x.NombreItem).ToListAsync();
            Items = new ObservableCollection<ItemMaterialInsumo>(datos);
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
    }
}
