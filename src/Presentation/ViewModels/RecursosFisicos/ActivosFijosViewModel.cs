using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Domain.Enums;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.RecursosFisicos;

public sealed class CarreraOpcion
{
    public int Id { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}

public sealed class CategoriaOpcion
{
    public CategoriaActivoFijo Valor { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}

public sealed partial class ActivosFijosViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;

    public ActivosFijosViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;

        Categorias = new ObservableCollection<CategoriaOpcion>(
            Enum.GetValues<CategoriaActivoFijo>()
                .Select(c => new CategoriaOpcion { Valor = c, Etiqueta = MapeoActivoFijoUi.Nombre(c) }));
        CategoriaForm = Categorias.First();
    }

    [ObservableProperty] private ObservableCollection<CarreraOpcion> _carreras = [];
    [ObservableProperty] private CarreraOpcion? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<CategoriaOpcion> _categorias = [];
    [ObservableProperty] private ObservableCollection<ActivoFijoDto> _activos = [];
    [ObservableProperty] private ActivoFijoDto? _activoSeleccionado;
    [ObservableProperty] private ObservableCollection<TotalCategoriaActivosDto> _totalesPorCategoria = [];
    [ObservableProperty] private decimal _totalGeneral;

    [ObservableProperty] private string _descripcion = string.Empty;
    [ObservableProperty] private CategoriaOpcion? _categoriaForm;
    [ObservableProperty] private string _cantidad = "1";
    [ObservableProperty] private string _unidadMedida = "UNI";
    [ObservableProperty] private string _valorUnitario = "0";
    [ObservableProperty] private string _vidaUtilAnios = "10";
    [ObservableProperty] private string _porcentajeResidual = "5";

    [ObservableProperty] private bool _estaEditando;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;
    [ObservableProperty] private bool _estaEliminando;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;

    public bool PuedeVer => _sesionActual.TienePermiso("RD.VER") || _sesionActual.EsAdministrador;
    public bool PuedeCrear => _sesionActual.TienePermiso("RD.CREAR") || _sesionActual.EsAdministrador;
    public bool PuedeEditar => _sesionActual.TienePermiso("RD.EDITAR") || _sesionActual.EsAdministrador;
    public bool PuedeEliminar => _sesionActual.TienePermiso("RD.ELIMINAR") || _sesionActual.EsAdministrador;

    public string TituloFormulario => EstaEditando ? "Editar activo fijo" : "Nuevo activo fijo";
    public string TotalGeneralDisplay => TotalGeneral.ToString("C2");

    partial void OnTotalGeneralChanged(decimal value) => OnPropertyChanged(nameof(TotalGeneralDisplay));

    partial void OnCarreraSeleccionadaChanged(CarreraOpcion? value)
    {
        if (value is not null)
            _ = RecargarActivosAsync();
    }

    partial void OnCategoriaFormChanged(CategoriaOpcion? value)
    {
        if (value is not null && !EstaEditando)
            VidaUtilAnios = Domain.Entities.ActivoFijo.VidaUtilPorDefecto(value.Valor)
                .ToString(CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!PuedeVer)
        {
            MensajeError = "Acceso denegado al modulo de Recursos y Depreciacion.";
            return;
        }

        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repoCarrera = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var lista = await repoCarrera.ListarAsync();

            Carreras = new ObservableCollection<CarreraOpcion>(
                lista.OrderBy(x => x.Codigo)
                    .Select(x => new CarreraOpcion { Id = x.Id, Etiqueta = $"{x.Codigo} - {x.Nombre}" }));

            CarreraSeleccionada = Carreras.FirstOrDefault();
            if (CarreraSeleccionada is null)
                MensajeError = "No hay carreras registradas. Cree una carrera antes de registrar activos.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar carreras: {Detalle(ex)}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private async Task RecargarActivosAsync()
    {
        if (CarreraSeleccionada is null) return;

        EstaCargando = true;
        MensajeError = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var listar = scope.ServiceProvider.GetRequiredService<ListarActivosFijosQuery>();
            var totalesQuery = scope.ServiceProvider.GetRequiredService<ObtenerTotalesActivosQuery>();

            var activos = await listar.EjecutarAsync(CarreraSeleccionada.Id);
            Activos = new ObservableCollection<ActivoFijoDto>(activos);

            var totales = await totalesQuery.EjecutarAsync(CarreraSeleccionada.Id);
            TotalesPorCategoria = new ObservableCollection<TotalCategoriaActivosDto>(totales.PorCategoria);
            TotalGeneral = totales.TotalGeneral;
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar activos: {Detalle(ex)}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private void Nuevo()
    {
        ActivoSeleccionado = null;
        EstaEditando = false;
        Descripcion = string.Empty;
        CategoriaForm = Categorias.First();
        Cantidad = "1";
        UnidadMedida = "UNI";
        ValorUnitario = "0";
        VidaUtilAnios = Domain.Entities.ActivoFijo.VidaUtilPorDefecto(CategoriaForm!.Valor)
            .ToString(CultureInfo.InvariantCulture);
        PorcentajeResidual = "5";
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    [RelayCommand]
    private void SeleccionarParaEditar()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para editar activos.";
            return;
        }

        if (ActivoSeleccionado is null)
        {
            MensajeError = "Seleccione un activo para editar.";
            return;
        }

        EstaEditando = true;
        Descripcion = ActivoSeleccionado.Descripcion;
        CategoriaForm = Categorias.FirstOrDefault(c => c.Valor == ActivoSeleccionado.Categoria) ?? Categorias.First();
        Cantidad = ActivoSeleccionado.Cantidad.ToString(CultureInfo.InvariantCulture);
        UnidadMedida = ActivoSeleccionado.UnidadMedida;
        ValorUnitario = ActivoSeleccionado.ValorUnitario.ToString(CultureInfo.InvariantCulture);
        VidaUtilAnios = ActivoSeleccionado.VidaUtilAnios.ToString(CultureInfo.InvariantCulture);
        PorcentajeResidual = (ActivoSeleccionado.PorcentajeResidual * 100m).ToString(CultureInfo.InvariantCulture);
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (EstaEditando ? !PuedeEditar : !PuedeCrear)
        {
            MensajeError = "No tiene permiso para guardar activos.";
            return;
        }

        if (EstaGuardando) return;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        if (CarreraSeleccionada is null)
        {
            MensajeError = "Seleccione una carrera.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Descripcion))
        {
            MensajeError = "Ingrese la descripcion del activo.";
            return;
        }

        if (CategoriaForm is null)
        {
            MensajeError = "Seleccione una categoria.";
            return;
        }

        if (!TryDecimal(Cantidad, out var cantidad) || cantidad < 0)
        {
            MensajeError = "Cantidad invalida.";
            return;
        }

        if (!TryDecimal(ValorUnitario, out var valorUnitario) || valorUnitario < 0)
        {
            MensajeError = "Valor unitario invalido.";
            return;
        }

        if (!int.TryParse(VidaUtilAnios, NumberStyles.Integer, CultureInfo.InvariantCulture, out var vidaUtil) || vidaUtil <= 0)
        {
            MensajeError = "Vida util invalida.";
            return;
        }

        if (!TryDecimal(PorcentajeResidual, out var residualPct) || residualPct < 0 || residualPct >= 100)
        {
            MensajeError = "Porcentaje residual invalido (0 a 99).";
            return;
        }

        EstaGuardando = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();

            if (EstaEditando && ActivoSeleccionado is not null)
            {
                var cmd = scope.ServiceProvider.GetRequiredService<ActualizarActivoFijoCommand>();
                await cmd.EjecutarAsync(new ActualizarActivoFijoDto
                {
                    Id = ActivoSeleccionado.Id,
                    Descripcion = Descripcion,
                    Categoria = CategoriaForm.Valor,
                    Cantidad = cantidad,
                    UnidadMedida = UnidadMedida,
                    ValorUnitario = valorUnitario,
                    VidaUtilAnios = vidaUtil,
                    PorcentajeResidual = residualPct / 100m,
                    FechaAdquisicion = ActivoSeleccionado.FechaAdquisicion
                });
                MensajeExito = "Activo actualizado correctamente.";
            }
            else
            {
                var cmd = scope.ServiceProvider.GetRequiredService<CrearActivoFijoCommand>();
                await cmd.EjecutarAsync(new CrearActivoFijoDto
                {
                    CarreraId = CarreraSeleccionada.Id,
                    Descripcion = Descripcion,
                    Categoria = CategoriaForm.Valor,
                    Cantidad = cantidad,
                    UnidadMedida = UnidadMedida,
                    ValorUnitario = valorUnitario,
                    VidaUtilAnios = vidaUtil,
                    PorcentajeResidual = residualPct / 100m
                });
                MensajeExito = "Activo creado correctamente.";
            }

            await RecargarActivosAsync();
            Nuevo();
            OnPropertyChanged(nameof(TituloFormulario));
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al guardar activo: {Detalle(ex)}";
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    [RelayCommand]
    private async Task EliminarSeleccionadoAsync()
    {
        if (!PuedeEliminar)
        {
            MensajeError = "No tiene permiso para eliminar activos.";
            return;
        }

        if (ActivoSeleccionado is null)
        {
            MensajeError = "Seleccione un activo para eliminar.";
            return;
        }

        var confirmacion = MessageBox.Show(
            $"¿Desea eliminar el activo '{ActivoSeleccionado.Descripcion}'?",
            "Confirmar eliminacion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmacion != MessageBoxResult.Yes) return;
        if (EstaEliminando) return;

        EstaEliminando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cmd = scope.ServiceProvider.GetRequiredService<EliminarActivoFijoCommand>();
            var usuarioId = _sesionActual.UsuarioId > 0 ? _sesionActual.UsuarioId : (int?)null;
            await cmd.EjecutarAsync(ActivoSeleccionado.Id, usuarioId);

            await RecargarActivosAsync();
            Nuevo();
            MensajeExito = "Activo eliminado correctamente.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al eliminar activo: {Detalle(ex)}";
        }
        finally
        {
            EstaEliminando = false;
        }
    }

    private static bool TryDecimal(string? value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    private static string Detalle(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}

internal static class MapeoActivoFijoUi
{
    public static string Nombre(CategoriaActivoFijo c) => c switch
    {
        CategoriaActivoFijo.MueblesEnseres => "Muebles y enseres",
        CategoriaActivoFijo.LaboratoriosEquipos => "Laboratorios y equipos",
        CategoriaActivoFijo.EquipoComputo => "Equipo de computo",
        CategoriaActivoFijo.EquipoOficina => "Equipo de oficina",
        _ => c.ToString()
    };
}
