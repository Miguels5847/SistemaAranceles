using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Domain.Entities;
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

public sealed class TipoCalculoOpcion
{
    public TipoCalculoCantidad Valor { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}

public sealed partial class ActivosFijosViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private bool _suprimirRecargaAutomatica;

    public ActivosFijosViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;

        Categorias = new ObservableCollection<CategoriaOpcion>(
            Enum.GetValues<CategoriaActivoFijo>()
                .Select(c => new CategoriaOpcion { Valor = c, Etiqueta = MapeoActivoFijoUi.Nombre(c) }));
        CategoriaForm = Categorias.First();

        TiposCalculo = new ObservableCollection<TipoCalculoOpcion>(
            Enum.GetValues<TipoCalculoCantidad>()
                .Select(t => new TipoCalculoOpcion { Valor = t, Etiqueta = MapeoActivoFijoUi.NombreTipo(t) }));
        TipoCalculoForm = TiposCalculo.First();
    }

    [ObservableProperty] private ObservableCollection<CarreraOpcion> _carreras = [];
    [ObservableProperty] private CarreraOpcion? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioProyeccion> _escenarios = [];
    [ObservableProperty] private EscenarioProyeccion? _escenarioSeleccionado;
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
    [ObservableProperty] private ObservableCollection<TipoCalculoOpcion> _tiposCalculo = [];
    [ObservableProperty] private TipoCalculoOpcion? _tipoCalculoForm;
    [ObservableProperty] private string _factorMultiplicador = "1";
    [ObservableProperty] private string _offsetCantidad = "0";

    public bool CantidadEsManual =>
        TipoCalculoForm is null
        || TipoCalculoForm.Valor is TipoCalculoCantidad.Manual or TipoCalculoCantidad.PorHito;

    // Factor y offset se mantienen internamente por compatibilidad con BD/modelo,
    // pero no se exponen como campos editables en la pantalla.
    public bool UsaFactor => false;
    public bool UsaOffset => false;

    partial void OnTipoCalculoFormChanged(TipoCalculoOpcion? value)
    {
        OnPropertyChanged(nameof(CantidadEsManual));
        OnPropertyChanged(nameof(UsaFactor));
        OnPropertyChanged(nameof(UsaOffset));
    }

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
        _ = value;
        if (_suprimirRecargaAutomatica || EstaCargando)
            return;

        _ = CargarEscenariosAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioProyeccion? value)
    {
        _ = value;
        if (_suprimirRecargaAutomatica || EstaCargando)
            return;

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
            var carreraIdActual = CarreraSeleccionada?.Id;
            var escenarioIdActual = EscenarioSeleccionado?.Id;

            using var scope = _serviceProvider.CreateScope();
            var repoCarrera = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var lista = await repoCarrera.ListarAsync();

            _suprimirRecargaAutomatica = true;
            try
            {
                Carreras = new ObservableCollection<CarreraOpcion>(
                    lista.OrderBy(x => x.Codigo)
                        .Select(x => new CarreraOpcion { Id = x.Id, Etiqueta = $"{x.Codigo} - {x.Nombre}" }));

                CarreraSeleccionada = Carreras.FirstOrDefault(x => x.Id == carreraIdActual) ?? Carreras.FirstOrDefault();
            }
            finally
            {
                _suprimirRecargaAutomatica = false;
            }

            if (CarreraSeleccionada is null)
            {
                Escenarios = [];
                EscenarioSeleccionado = null;
                LimpiarActivos();
                MensajeError = "No hay carreras registradas. Cree una carrera antes de registrar activos.";
                return;
            }

            await CargarEscenariosAsync(escenarioIdActual);
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

    private async Task CargarEscenariosAsync(int? escenarioIdPreferido = null)
    {
        if (CarreraSeleccionada is null)
        {
            _suprimirRecargaAutomatica = true;
            try
            {
                EscenarioSeleccionado = null;
                Escenarios = [];
            }
            finally
            {
                _suprimirRecargaAutomatica = false;
            }

            await RecargarActivosAsync();
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<SistemaAranceles.Application.UseCases.CargosFacultad.ListarEscenariosConProyeccionPorCarreraQuery>();
            var lista = await query.EjecutarAsync(CarreraSeleccionada.Id);

            _suprimirRecargaAutomatica = true;
            try
            {
                Escenarios = new ObservableCollection<EscenarioProyeccion>(lista);
                EscenarioSeleccionado = Escenarios.FirstOrDefault(x => x.Id == escenarioIdPreferido)
                    ?? Escenarios.FirstOrDefault();
            }
            finally
            {
                _suprimirRecargaAutomatica = false;
            }
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar escenarios: {Detalle(ex)}";
            _suprimirRecargaAutomatica = true;
            try
            {
                EscenarioSeleccionado = null;
                Escenarios = [];
            }
            finally
            {
                _suprimirRecargaAutomatica = false;
            }
        }

        await RecargarActivosAsync();
    }

    private async Task RecargarActivosAsync()
    {
        if (CarreraSeleccionada is null)
        {
            LimpiarActivos();
            return;
        }

        EstaCargando = true;
        MensajeError = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var sembrar = scope.ServiceProvider.GetRequiredService<SembrarActivosFijosDesdeCatalogoCommand>();
            var listar = scope.ServiceProvider.GetRequiredService<ListarActivosFijosQuery>();
            var escenarioProyeccionId = EscenarioSeleccionado?.Id;

            await sembrar.EjecutarAsync(CarreraSeleccionada.Id);

            var activos = await listar.EjecutarAsync(CarreraSeleccionada.Id, escenarioProyeccionId);
            var activosNormalizados = activos.Select(NormalizarActivoParaPantalla).ToList();

            Activos = new ObservableCollection<ActivoFijoDto>(activosNormalizados);
            RecalcularTotalesDesdePantalla(activosNormalizados);
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar activos: {Detalle(ex)}";
            LimpiarActivos();
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void LimpiarActivos()
    {
        Activos = [];
        ActivoSeleccionado = null;
        TotalesPorCategoria = [];
        TotalGeneral = 0m;
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
        TipoCalculoForm = TiposCalculo.First();
        FactorMultiplicador = "1";
        OffsetCantidad = "0";
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
        Cantidad = ActivoSeleccionado.CantidadBase.ToString(CultureInfo.InvariantCulture);
        UnidadMedida = ActivoSeleccionado.UnidadMedida;
        ValorUnitario = ActivoSeleccionado.ValorUnitario.ToString(CultureInfo.InvariantCulture);
        VidaUtilAnios = ActivoSeleccionado.VidaUtilAnios.ToString(CultureInfo.InvariantCulture);
        PorcentajeResidual = (ActivoSeleccionado.PorcentajeResidual * 100m).ToString(CultureInfo.InvariantCulture);
        TipoCalculoForm = TiposCalculo.FirstOrDefault(t => t.Valor == ActivoSeleccionado.TipoCalculoCantidad) ?? TiposCalculo.First();
        FactorMultiplicador = "1";
        OffsetCantidad = "0";
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

        var tipoCalculo = TipoCalculoForm?.Valor ?? TipoCalculoCantidad.Manual;
        var cantidadPersistida = tipoCalculo is TipoCalculoCantidad.PorEstudiante or TipoCalculoCantidad.PorDocente
            ? 0m
            : cantidad;

        // Los derivados se calculan desde el escenario seleccionado: estudiantes o docentes requeridos.
        // No se permite que el usuario altere factor/offset desde la pantalla.
        var factor = tipoCalculo is TipoCalculoCantidad.PorEstudiante or TipoCalculoCantidad.PorDocente ? 1m : 1m;
        var offset = 0m;

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
                    Cantidad = cantidadPersistida,
                    UnidadMedida = UnidadMedida,
                    ValorUnitario = valorUnitario,
                    VidaUtilAnios = vidaUtil,
                    PorcentajeResidual = residualPct / 100m,
                    FechaAdquisicion = ActivoSeleccionado.FechaAdquisicion,
                    TipoCalculoCantidad = tipoCalculo,
                    FactorMultiplicador = factor,
                    OffsetCantidad = offset
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
                    Cantidad = cantidadPersistida,
                    UnidadMedida = UnidadMedida,
                    ValorUnitario = valorUnitario,
                    VidaUtilAnios = vidaUtil,
                    PorcentajeResidual = residualPct / 100m,
                    TipoCalculoCantidad = tipoCalculo,
                    FactorMultiplicador = factor,
                    OffsetCantidad = offset
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

    private static ActivoFijoDto NormalizarActivoParaPantalla(ActivoFijoDto activo)
    {
        var cantidad = activo.Cantidad;

        if (activo.TipoCalculoCantidad == TipoCalculoCantidad.PorDocente)
        {
            cantidad = ResolverDerivadoSinFactorNiOffset(
                activo.Cantidad,
                activo.FactorMultiplicador,
                activo.OffsetCantidad);
        }
        else if (activo.TipoCalculoCantidad == TipoCalculoCantidad.PorEstudiante)
        {
            cantidad = ResolverDerivadoSinFactorNiOffset(
                activo.Cantidad,
                activo.FactorMultiplicador,
                offsetCantidad: 0m);
        }

        cantidad = QuitarCerosDecimales(cantidad);

        return new ActivoFijoDto
        {
            Id = activo.Id,
            CarreraId = activo.CarreraId,
            Descripcion = activo.Descripcion,
            Categoria = activo.Categoria,
            CategoriaNombre = activo.CategoriaNombre,
            Cantidad = cantidad,
            CantidadBase = activo.TipoCalculoCantidad is TipoCalculoCantidad.PorEstudiante or TipoCalculoCantidad.PorDocente
                ? 0m
                : QuitarCerosDecimales(activo.CantidadBase),
            UnidadMedida = activo.UnidadMedida,
            ValorUnitario = activo.ValorUnitario,
            ValorTotal = decimal.Round(cantidad * activo.ValorUnitario, 2),
            VidaUtilAnios = activo.VidaUtilAnios,
            PorcentajeResidual = activo.PorcentajeResidual,
            FechaAdquisicion = activo.FechaAdquisicion,
            TipoCalculoCantidad = activo.TipoCalculoCantidad,
            TipoCalculoNombre = activo.TipoCalculoNombre,
            FactorMultiplicador = 1m,
            OffsetCantidad = 0m,
            UsaCantidadCalculada = activo.UsaCantidadCalculada
        };
    }

    private static decimal ResolverDerivadoSinFactorNiOffset(decimal cantidadCalculada, decimal factorMultiplicador, decimal offsetCantidad)
    {
        var cantidadSinOffset = cantidadCalculada - offsetCantidad;
        if (cantidadSinOffset < 0m)
        {
            cantidadSinOffset = 0m;
        }

        if (factorMultiplicador > 0m && factorMultiplicador != 1m)
        {
            cantidadSinOffset /= factorMultiplicador;
        }

        return cantidadSinOffset;
    }

    private void RecalcularTotalesDesdePantalla(IReadOnlyList<ActivoFijoDto> activos)
    {
        var totales = activos
            .GroupBy(x => x.Categoria)
            .Select(g => new TotalCategoriaActivosDto
            {
                Categoria = g.Key,
                CategoriaNombre = g.First().CategoriaNombre,
                CantidadItems = g.Count(),
                SubtotalValorTotal = g.Sum(x => x.ValorTotal)
            })
            .OrderBy(x => x.Categoria)
            .ToList();

        TotalesPorCategoria = new ObservableCollection<TotalCategoriaActivosDto>(totales);
        TotalGeneral = totales.Sum(x => x.SubtotalValorTotal);
    }

    private static decimal QuitarCerosDecimales(decimal valor)
        => decimal.Parse(valor.ToString("0.####", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
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

    public static string NombreTipo(TipoCalculoCantidad t) => t switch
    {
        TipoCalculoCantidad.Manual => "Manual",
        TipoCalculoCantidad.PorEstudiante => "Por estudiante",
        TipoCalculoCantidad.PorDocente => "Por docente",
        TipoCalculoCantidad.PorHito => "Por hito",
        _ => t.ToString()
    };
}
