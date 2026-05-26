using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.DemandaIngresos;

public sealed class DemandaMatrizFilaView
{
    public string CicloDisplay { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public bool EsTotal { get; init; }
    public string TotalDisplay => Total.ToString("N0");
}

public sealed class IngresosMatrizFilaView
{
    public string CicloDisplay { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public bool EsTotal { get; init; }
    public string TotalDisplay => $"$ {Total:N2}";
}

/// <summary>
/// ViewModel composite para la vista "Demanda e Ingresos" (Épica 9).
/// KAN-32 implementa la pestaña de Configuración de Arancel.
/// Las demás pestañas quedan como placeholder hasta KAN-33/34/35.
/// </summary>
public sealed partial class DemandaIngresosViewModel : ObservableObject
{
    private const string ModoManual = "Manual";
    private const string ModoAutomatico = "AutomaticoCostoCarrera";

    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private bool _suprimirCambios;

    public DemandaIngresosViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty] private ObservableCollection<Carrera> _carreras = [];
    [ObservableProperty] private Carrera? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioDemandaIngresosDto> _escenarios = [];
    [ObservableProperty] private EscenarioDemandaIngresosDto? _escenarioSeleccionado;

    [ObservableProperty] private ObservableCollection<ConfiguracionArancelCarreraDto> _configuraciones = [];
    [ObservableProperty] private ConfiguracionArancelCarreraDto? _configuracionSeleccionada;
    [ObservableProperty] private ArancelEfectivoDto? _arancelEfectivo;
    [ObservableProperty] private DemandaProyectadaDto? _demandaProyectada;
    [ObservableProperty] private ObservableCollection<DemandaMatrizFilaView> _demandaMatrizFilas = [];
    [ObservableProperty] private PresupuestosCarreraDto? _presupuestos;
    [ObservableProperty] private IngresosProyectadosDto? _ingresos;
    [ObservableProperty] private ObservableCollection<IngresosMatrizFilaView> _ingresosMatrizFilas = [];
    [ObservableProperty] private MaterialesProyectadosDto? _materiales;
    [ObservableProperty] private ResumenDemandaIngresosDto _resumen = new();
    [ObservableProperty] private ObservableCollection<RatioMaterialDemandaDto> _ratios = [];
    [ObservableProperty] private RatioMaterialDemandaDto? _ratioSeleccionado;
    [ObservableProperty] private ObservableCollection<ItemMaterialRatioOpcionDto> _itemsCapitalTrabajo = [];
    [ObservableProperty] private ItemMaterialRatioOpcionDto? _ratioFormItemSeleccionado;

    [ObservableProperty] private bool _ratioFormVisible;
    [ObservableProperty] private bool _ratioFormEsEdicion;
    [ObservableProperty] private int _ratioFormId;
    [ObservableProperty] private string _ratioFormCategoria = "MATERIALES_SUMINISTROS";
    [ObservableProperty] private string _ratioFormConcepto = string.Empty;
    [ObservableProperty] private string _ratioFormRatio = "0";
    [ObservableProperty] private string _ratioFormUnidad = "por_estudiante";
    [ObservableProperty] private string _ratioFormMeses = "6";
    [ObservableProperty] private bool _ratioFormAplicaInflacion = true;
    [ObservableProperty] private int? _ratioFormItemMaterialId;

    [ObservableProperty] private bool _formVisible;
    [ObservableProperty] private bool _formEsEdicion;
    [ObservableProperty] private int _formId;
    [ObservableProperty] private string _formModoCalculo = ModoManual;
    [ObservableProperty] private string _formArancelManual = "0";
    [ObservableProperty] private bool _formUsaPorcentajeInstitucional = true;
    [ObservableProperty] private string _formPorcentajeMatricula = "10";
    [ObservableProperty] private bool _formEscenarioGlobal = true;

    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;

    public IReadOnlyList<string> ModosCalculo { get; } = [ModoManual, ModoAutomatico];
    public IReadOnlyList<string> CategoriasRatio { get; } = ["MATERIALES_SUMINISTROS", "ASEO_LIMPIEZA", "ACCESORIOS_MATERIALES", "OTRO"];
    public IReadOnlyList<string> UnidadesRatio { get; } = ["por_estudiante", "por_estudiante_mes"];
    public string TituloRatioFormulario => RatioFormEsEdicion ? "Editar ratio" : "Nuevo ratio";
    public string RatioFormItemAdvertencia
    {
        get
        {
            if (RatioFormItemSeleccionado is null || RatioFormItemSeleccionado.EsSinItem)
                return "Sin item vinculado: Materiales Monetarios calculara costo 0.";

            return RatioFormItemSeleccionado.PrecioUnitario <= 0m
                ? "El item seleccionado tiene precio unitario 0; Materiales Monetarios calculara costo 0."
                : string.Empty;
        }
    }

    partial void OnRatioFormEsEdicionChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(TituloRatioFormulario));
    }

    partial void OnRatioFormItemSeleccionadoChanged(ItemMaterialRatioOpcionDto? value)
    {
        RatioFormItemMaterialId = value?.Id;
        OnPropertyChanged(nameof(RatioFormItemAdvertencia));
    }

    partial void OnDemandaProyectadaChanged(DemandaProyectadaDto? value)
    {
        DemandaMatrizFilas = ConstruirFilasDemanda(value);
    }

    partial void OnIngresosChanged(IngresosProyectadosDto? value)
    {
        IngresosMatrizFilas = ConstruirFilasIngresos(value);
    }
    public string TituloFormulario => FormEsEdicion ? "Editar configuración" : "Nueva configuración";
    public bool FormEsModoManual => string.Equals(FormModoCalculo, ModoManual, StringComparison.OrdinalIgnoreCase);
    public bool FormPorcentajeMatriculaEditable => !FormUsaPorcentajeInstitucional;
    public bool NoEstaGuardando => !EstaGuardando;

    public bool PuedeEditar => _sesionActual.EsAdministrador
                            || _sesionActual.TienePermiso("DI_NG.EDITAR");

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        _ = value;
        if (_suprimirCambios || EstaCargando) return;
        _ = RecargarPorCarreraAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioDemandaIngresosDto? value)
    {
        _ = value;
        if (_suprimirCambios || EstaCargando) return;
        _ = RefrescarArancelEfectivoAsync();
    }

    partial void OnFormEsEdicionChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    partial void OnFormModoCalculoChanged(string value)
    {
        _ = value;
        OnPropertyChanged(nameof(FormEsModoManual));
    }

    partial void OnFormUsaPorcentajeInstitucionalChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(FormPorcentajeMatriculaEditable));
    }

    partial void OnEstaGuardandoChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(NoEstaGuardando));
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repoCarreras = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var carreras = await repoCarreras.ListarAsync();

            var carreraActualId = CarreraSeleccionada?.Id;
            _suprimirCambios = true;
            try
            {
                Carreras = new ObservableCollection<Carrera>(carreras);
                CarreraSeleccionada = Carreras.FirstOrDefault(c => c.Id == carreraActualId)
                    ?? Carreras.FirstOrDefault();
            }
            finally { _suprimirCambios = false; }

            if (CarreraSeleccionada is null)
            {
                MensajeError = "No hay carreras registradas. Crea una carrera primero.";
                LimpiarTodo();
                return;
            }

            await RecargarPorCarreraAsync();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private async Task RecargarPorCarreraAsync()
    {
        if (CarreraSeleccionada is null)
        {
            LimpiarTodo();
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repoEscenario = scope.ServiceProvider.GetRequiredService<IRepositorioEscenarioProyeccion>();
            var repoProyeccion = scope.ServiceProvider.GetRequiredService<IRepositorioProyeccionEstudiantes>();
            var repoItems = scope.ServiceProvider.GetRequiredService<IRepositorioItemMaterialInsumo>();
            var todosEscenarios = await repoEscenario.ListarAsync();
            var proyecciones = await repoProyeccion.ListarResumenAsync(CarreraSeleccionada.Id);
            var proyeccionPorEscenario = proyecciones
                .GroupBy(p => p.EscenarioProyeccionId)
                .ToDictionary(g => g.Key, g => g.First().Id);

            var escenarios = todosEscenarios
                .Where(e => e.CarreraId == CarreraSeleccionada.Id)
                .Select(e => new EscenarioDemandaIngresosDto
                {
                    Id = e.Id,
                    CarreraId = e.CarreraId,
                    Nombre = e.Nombre,
                    Descripcion = e.Descripcion,
                    EsPredeterminado = e.EsPredeterminado,
                    TieneProyeccion = proyeccionPorEscenario.ContainsKey(e.Id),
                    ProyeccionEstudiantesId = proyeccionPorEscenario.TryGetValue(e.Id, out var proyId) ? proyId : null
                })
                .OrderByDescending(e => e.TieneProyeccion)
                .ThenByDescending(e => e.EsPredeterminado)
                .ThenBy(e => e.Nombre)
                .ToList();

            var escenarioActualId = EscenarioSeleccionado?.Id;
            _suprimirCambios = true;
            try
            {
                Escenarios = new ObservableCollection<EscenarioDemandaIngresosDto>(escenarios);
                EscenarioSeleccionado = Escenarios.FirstOrDefault(e => e.Id == escenarioActualId)
                    ?? Escenarios.FirstOrDefault(e => e.TieneProyeccion)
                    ?? Escenarios.FirstOrDefault();
            }
            finally { _suprimirCambios = false; }

            var items = await repoItems.ListarPorCarreraAsync(CarreraSeleccionada.Id);
            ItemsCapitalTrabajo = ConstruirOpcionesItems(items);
            SeleccionarItemRatio(RatioFormItemMaterialId);

            var listarQuery = scope.ServiceProvider.GetRequiredService<ListarConfiguracionesArancelCarreraQuery>();
            var configs = await listarQuery.EjecutarAsync(CarreraSeleccionada.Id);
            Configuraciones = new ObservableCollection<ConfiguracionArancelCarreraDto>(configs);

            await RefrescarArancelEfectivoAsync();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar configuraciones: {Detalle(ex)}";
        }
    }

    private async Task RefrescarArancelEfectivoAsync()
    {
        if (CarreraSeleccionada is null)
        {
            LimpiarTodo();
            return;
        }

        if (EscenarioSeleccionado is null)
        {
            ArancelEfectivo = null;
            DemandaProyectada = null;
            Presupuestos = null;
            Ingresos = null;
            Materiales = null;
            Ratios = [];
            ActualizarResumen();
            MensajeError = "Selecciona un escenario para calcular Demanda e Ingresos.";
            return;
        }

        try
        {
            MensajeError = string.Empty;
            using var scope = _serviceProvider.CreateScope();
            var queryArancel = scope.ServiceProvider.GetRequiredService<ObtenerArancelEfectivoQuery>();
            ArancelEfectivo = await queryArancel.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);

            var queryDemanda = scope.ServiceProvider.GetRequiredService<ObtenerDemandaProyectadaQuery>();
            DemandaProyectada = await queryDemanda.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);

            var queryPresup = scope.ServiceProvider.GetRequiredService<ObtenerPresupuestosCarreraQuery>();
            Presupuestos = await queryPresup.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);

            var queryIngresos = scope.ServiceProvider.GetRequiredService<CalcularIngresosProyectadosQuery>();
            Ingresos = await queryIngresos.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);

            var queryMateriales = scope.ServiceProvider.GetRequiredService<CalcularMaterialesPorPeriodoQuery>();
            Materiales = await queryMateriales.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);

            var listarRatios = scope.ServiceProvider.GetRequiredService<ListarRatiosMaterialDemandaQuery>();
            var ratios = await listarRatios.EjecutarAsync(CarreraSeleccionada.Id);
            Ratios = new ObservableCollection<RatioMaterialDemandaDto>(ratios);

            ActualizarResumen();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al calcular arancel/presupuestos/ingresos/materiales: {Detalle(ex)}";
            ArancelEfectivo = null;
            DemandaProyectada = null;
            Presupuestos = null;
            Ingresos = null;
            Materiales = null;
            Ratios = [];
            ActualizarResumen();
        }
    }

    [RelayCommand]
    private void AbrirNuevoRatio()
    {
        if (!PuedeEditar) { MensajeError = "No tienes permiso para editar."; return; }
        if (CarreraSeleccionada is null) { MensajeError = "Selecciona una carrera."; return; }
        RatioFormId = 0;
        RatioFormCategoria = "MATERIALES_SUMINISTROS";
        RatioFormConcepto = string.Empty;
        RatioFormRatio = "0";
        RatioFormUnidad = "por_estudiante";
        RatioFormMeses = "6";
        RatioFormAplicaInflacion = true;
        RatioFormItemMaterialId = null;
        SeleccionarItemRatio(null);
        RatioFormEsEdicion = false;
        RatioFormVisible = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }

    [RelayCommand]
    private void AbrirEditarRatio(RatioMaterialDemandaDto? dto)
    {
        if (dto is null) return;
        if (!PuedeEditar) { MensajeError = "No tienes permiso para editar."; return; }
        RatioFormId = dto.Id;
        RatioFormCategoria = dto.Categoria;
        RatioFormConcepto = dto.Concepto;
        RatioFormRatio = dto.RatioConsumo.ToString("0.######", CultureInfo.InvariantCulture);
        RatioFormUnidad = dto.UnidadRatio;
        RatioFormMeses = dto.MesesOperativos.ToString(CultureInfo.InvariantCulture);
        RatioFormAplicaInflacion = dto.AplicaInflacion;
        RatioFormItemMaterialId = dto.ItemMaterialInsumoId;
        SeleccionarItemRatio(dto.ItemMaterialInsumoId);
        RatioFormEsEdicion = true;
        RatioFormVisible = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }

    [RelayCommand]
    private void CancelarRatio()
    {
        RatioFormVisible = false;
        MensajeError = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarRatioAsync()
    {
        if (CarreraSeleccionada is null) { MensajeError = "Selecciona una carrera."; return; }
        if (string.IsNullOrWhiteSpace(RatioFormConcepto)) { MensajeError = "Concepto es obligatorio."; return; }
        if (!TryDecimal(RatioFormRatio, out var ratio) || ratio < 0m)
        { MensajeError = "Ratio inválido (≥ 0)."; return; }
        if (!int.TryParse(RatioFormMeses, NumberStyles.Integer, CultureInfo.InvariantCulture, out var meses)
            || meses <= 0 || meses > 12)
        { MensajeError = "Meses operativos entre 1 y 12."; return; }

        EstaGuardando = true;
        MensajeError = string.Empty;
        try
        {
            var dto = new GuardarRatioMaterialDemandaDto
            {
                Id = RatioFormEsEdicion ? RatioFormId : null,
                CarreraId = CarreraSeleccionada.Id,
                Categoria = RatioFormCategoria,
                Concepto = RatioFormConcepto.Trim(),
                ItemMaterialInsumoId = RatioFormItemSeleccionado?.Id,
                RatioConsumo = ratio,
                UnidadRatio = RatioFormUnidad,
                MesesOperativos = meses,
                AplicaInflacion = RatioFormAplicaInflacion
            };

            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<GuardarRatioMaterialDemandaCommand>();
            await command.EjecutarAsync(dto, _sesionActual.UsuarioId);

            RatioFormVisible = false;
            MensajeExito = RatioFormEsEdicion ? "Ratio actualizado." : "Ratio creado.";
            await RefrescarArancelEfectivoAsync();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    [RelayCommand]
    private async Task EliminarRatioAsync(RatioMaterialDemandaDto? dto)
    {
        if (dto is null) return;
        if (!PuedeEditar) { MensajeError = "No tienes permiso para eliminar."; return; }

        EstaGuardando = true;
        MensajeError = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<EliminarRatioMaterialDemandaCommand>();
            await command.EjecutarAsync(dto.Id, _sesionActual.UsuarioId);
            MensajeExito = "Ratio eliminado.";
            await RefrescarArancelEfectivoAsync();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    [RelayCommand]
    private void AbrirNuevaConfiguracion()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tienes permiso para editar.";
            return;
        }
        if (CarreraSeleccionada is null)
        {
            MensajeError = "Selecciona una carrera.";
            return;
        }

        FormId = 0;
        FormModoCalculo = ModoManual;
        FormArancelManual = "0";
        FormUsaPorcentajeInstitucional = true;
        FormPorcentajeMatricula = "10";
        FormEscenarioGlobal = EscenarioSeleccionado is null;
        FormEsEdicion = false;
        FormVisible = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }

    [RelayCommand]
    private void AbrirEditarConfiguracion(ConfiguracionArancelCarreraDto? dto)
    {
        if (dto is null) return;
        if (!PuedeEditar)
        {
            MensajeError = "No tienes permiso para editar.";
            return;
        }

        FormId = dto.Id;
        FormModoCalculo = dto.ModoCalculoArancel;
        FormArancelManual = dto.ArancelManual?.ToString("0.##", CultureInfo.InvariantCulture) ?? "0";
        FormUsaPorcentajeInstitucional = dto.UsaPorcentajeMatriculaInstitucional;
        FormPorcentajeMatricula = (dto.PorcentajeMatricula ?? 10m).ToString("0.##", CultureInfo.InvariantCulture);
        FormEscenarioGlobal = dto.EscenarioProyeccionId is null;
        FormEsEdicion = true;
        FormVisible = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }

    [RelayCommand]
    private void CancelarFormulario()
    {
        FormVisible = false;
        MensajeError = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarConfiguracionAsync()
    {
        if (CarreraSeleccionada is null)
        {
            MensajeError = "Selecciona una carrera.";
            return;
        }
        if (string.Equals(FormModoCalculo, ModoManual, StringComparison.OrdinalIgnoreCase)
            && (!TryDecimal(FormArancelManual, out var arancel) || arancel <= 0m))
        {
            MensajeError = "Arancel manual inválido (debe ser > 0).";
            return;
        }

        decimal? porcMatricula = null;
        if (!FormUsaPorcentajeInstitucional)
        {
            if (!TryDecimal(FormPorcentajeMatricula, out var p) || p < 0m || p > 100m)
            {
                MensajeError = "Porcentaje matrícula entre 0 y 100.";
                return;
            }
            porcMatricula = p;
        }

        EstaGuardando = true;
        MensajeError = string.Empty;
        try
        {
            var dto = new GuardarConfiguracionArancelCarreraDto
            {
                Id = FormEsEdicion ? FormId : null,
                CarreraId = CarreraSeleccionada.Id,
                EscenarioProyeccionId = FormEscenarioGlobal ? null : EscenarioSeleccionado?.Id,
                ModoCalculoArancel = FormModoCalculo,
                ArancelManual = TryDecimal(FormArancelManual, out var a) ? a : (decimal?)null,
                PorcentajeMatricula = porcMatricula,
                UsaPorcentajeMatriculaInstitucional = FormUsaPorcentajeInstitucional
            };

            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<GuardarConfiguracionArancelCarreraCommand>();
            await command.EjecutarAsync(dto, _sesionActual.UsuarioId);

            FormVisible = false;
            MensajeExito = FormEsEdicion ? "Configuración actualizada." : "Configuración creada.";
            await RecargarPorCarreraAsync();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    [RelayCommand]
    private async Task EliminarConfiguracionAsync(ConfiguracionArancelCarreraDto? dto)
    {
        if (dto is null) return;
        if (!PuedeEditar)
        {
            MensajeError = "No tienes permiso para eliminar.";
            return;
        }

        EstaGuardando = true;
        MensajeError = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<EliminarConfiguracionArancelCarreraCommand>();
            await command.EjecutarAsync(dto.Id, _sesionActual.UsuarioId);
            MensajeExito = "Configuración eliminada.";
            await RecargarPorCarreraAsync();
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    private void LimpiarTodo()
    {
        Configuraciones = [];
        Escenarios = [];
        EscenarioSeleccionado = null;
        ArancelEfectivo = null;
        DemandaProyectada = null;
        Presupuestos = null;
        Ingresos = null;
        Materiales = null;
        Ratios = [];
        ItemsCapitalTrabajo = [];
        RatioFormItemSeleccionado = null;
        ActualizarResumen();
    }

    private static ObservableCollection<IngresosMatrizFilaView> ConstruirFilasIngresos(IngresosProyectadosDto? ingresos)
    {
        if (ingresos is null || ingresos.Filas.Count == 0 || ingresos.EtiquetasPeriodos.Count == 0)
            return [];

        var etiquetas = ingresos.EtiquetasPeriodos;
        var filas = ingresos.Filas
            .OrderBy(f => f.NumeroCiclo)
            .Select(f =>
            {
                // Asegurar una celda por etiqueta de periodo, en el orden de EtiquetasPeriodos
                var celdasPorEtiqueta = f.Periodos.ToDictionary(p => p.EtiquetaPeriodo, p => p.IngresoNeto);
                var valores = etiquetas
                    .Select(et => celdasPorEtiqueta.TryGetValue(et, out var v) ? v : 0m)
                    .ToList();
                return new IngresosMatrizFilaView
                {
                    CicloDisplay = f.CicloDisplay,
                    Periodos = valores,
                    Total = f.TotalNeto
                };
            })
            .ToList();

        var totales = new List<decimal>(etiquetas.Count);
        for (var i = 0; i < etiquetas.Count; i++)
            totales.Add(filas.Sum(f => f.Periodos[i]));

        filas.Add(new IngresosMatrizFilaView
        {
            CicloDisplay = "TOTAL INGRESOS",
            Periodos = totales,
            Total = totales.Sum(),
            EsTotal = true
        });

        return new ObservableCollection<IngresosMatrizFilaView>(filas);
    }

    private static ObservableCollection<DemandaMatrizFilaView> ConstruirFilasDemanda(DemandaProyectadaDto? demanda)
    {
        if (demanda is null || !demanda.TieneDatos)
            return [];

        var filas = demanda.Filas
            .Select(f => new DemandaMatrizFilaView
            {
                CicloDisplay = f.CicloDisplay,
                Periodos = f.Periodos,
                Total = f.Total
            })
            .ToList();

        filas.Add(new DemandaMatrizFilaView
        {
            CicloDisplay = "TOTAL",
            Periodos = demanda.TotalesPorPeriodo,
            Total = demanda.TotalGeneral,
            EsTotal = true
        });

        return new ObservableCollection<DemandaMatrizFilaView>(filas);
    }

    private static ObservableCollection<ItemMaterialRatioOpcionDto> ConstruirOpcionesItems(
        IReadOnlyList<ItemCapitalTrabajoDto> items)
    {
        var opciones = new List<ItemMaterialRatioOpcionDto>
        {
            new()
            {
                Id = null,
                Nombre = "Sin item vinculado",
                Categoria = string.Empty,
                PrecioUnitario = 0m
            }
        };

        opciones.AddRange(items.Select(i => new ItemMaterialRatioOpcionDto
        {
            Id = i.Id,
            Nombre = i.Concepto,
            Categoria = i.CategoriaNombre,
            PrecioUnitario = i.ValorUnitario
        }));

        return new ObservableCollection<ItemMaterialRatioOpcionDto>(opciones);
    }

    private void SeleccionarItemRatio(int? itemId)
    {
        RatioFormItemSeleccionado = ItemsCapitalTrabajo.FirstOrDefault(i => i.Id == itemId)
            ?? ItemsCapitalTrabajo.FirstOrDefault(i => i.EsSinItem);
    }

    private void ActualizarResumen()
    {
        var advertencias = new List<string>();
        AgregarAdvertencia(advertencias, ArancelEfectivo?.MensajeAdvertencia);
        AgregarAdvertencia(advertencias, DemandaProyectada?.MensajeAdvertencia);
        AgregarAdvertencia(advertencias, Presupuestos?.MensajeAdvertencia);
        AgregarAdvertencia(advertencias, Ingresos?.MensajeAdvertencia);
        AgregarAdvertencia(advertencias, Materiales?.MensajeAdvertencia);

        Resumen = new ResumenDemandaIngresosDto
        {
            TotalIngresoBruto = Ingresos?.TotalGeneralBruto ?? 0m,
            TotalBecas = Ingresos?.TotalGeneralBecas ?? 0m,
            TotalIngresoNeto = Ingresos?.TotalGeneralNeto ?? 0m,
            TotalPresupuestosAnualProrrateado = Presupuestos?.TotalAnualProrrateado ?? 0m,
            TotalPresupuestosPorSemestre = Presupuestos?.TotalPorSemestre ?? 0m,
            TotalMaterialesMonetarios = Materiales?.TotalCosto ?? 0m,
            TotalCantidadMateriales = Materiales?.TotalCantidad ?? 0m,
            Advertencias = advertencias.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    private static void AgregarAdvertencia(List<string> advertencias, string? mensaje)
    {
        if (!string.IsNullOrWhiteSpace(mensaje))
            advertencias.Add(mensaje.Trim());
    }

    private static bool TryDecimal(string? value, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var limpio = value.Trim()
            .Replace("$", string.Empty)
            .Replace(" ", string.Empty);

        if (string.IsNullOrWhiteSpace(limpio)) return false;

        var ultimoPunto = limpio.LastIndexOf('.');
        var ultimaComa = limpio.LastIndexOf(',');

        if (ultimoPunto >= 0 && ultimaComa >= 0)
        {
            limpio = ultimaComa > ultimoPunto
                ? limpio.Replace(".", string.Empty).Replace(',', '.')
                : limpio.Replace(",", string.Empty);
        }
        else if (ultimaComa >= 0)
        {
            limpio = limpio.Replace(',', '.');
        }
        else if (ultimoPunto >= 0)
        {
            var decimales = limpio.Length - ultimoPunto - 1;
            if (decimales == 3)
                limpio = limpio.Replace(".", string.Empty);
        }

        return decimal.TryParse(limpio, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private static string Detalle(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}
