using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;
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

public sealed class ArancelCicloResumenView
{
    public string CicloDisplay { get; init; } = string.Empty;
    public string ArancelBaseDisplay { get; init; } = string.Empty;
    public string DescuentoDisplay { get; init; } = string.Empty;
    public string ArancelCicloDisplay { get; init; } = string.Empty;
    public string MatriculaCicloDisplay { get; init; } = string.Empty;
}

// Fila editable de la tabla de descuentos por ciclo.
public sealed partial class DescuentoCicloEditableView : ObservableObject
{
    public int NumeroCiclo { get; init; }
    public string CicloDisplay => $"Ciclo {NumeroCiclo}";
    [ObservableProperty] private decimal _porcentaje;
}

public sealed class MaterialCantidadMatrizFilaView
{
    public string Categoria { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public bool EsEncabezadoCategoria { get; init; }
    public bool EsTotal { get; init; }
    public string TotalDisplay => EsEncabezadoCategoria ? string.Empty : Total.ToString("N2");
}

public sealed class MaterialMonetarioMatrizFilaView
{
    public string Categoria { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public bool EsEncabezadoCategoria { get; init; }
    public bool EsTotal { get; init; }
    public string TotalDisplay => EsEncabezadoCategoria ? string.Empty : $"$ {Total:N2}";
}

public sealed class PresupuestoPeriodoMatrizFilaView
{
    public string Grupo { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public bool EsEncabezadoGrupo { get; init; }
    public bool EsTotal { get; init; }
    public string TotalDisplay { get; init; } = string.Empty;
}

public sealed class UnidadConsumoOpcion
{
    public string Valor { get; init; } = string.Empty;
    public string Display { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel composite para la vista "Demanda e Ingresos".
/// </summary>
public sealed partial class DemandaIngresosViewModel : ObservableObject
{
    private const string ModoManual = "Manual";
    private const string ModoAutomatico = "AutomaticoCostoCarrera";
    private const string ModoOptimoFinanciero = "OptimoFinanciero";

    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private bool _suprimirCambios;
    private int _refrescoArancelVersion;
    private Dictionary<int, decimal> _descuentosGuardadosPorCiclo = [];

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
    [ObservableProperty] private ArancelOptimoCarreraDto? _arancelSugeridoCostoCarrera;
    [ObservableProperty] private DemandaProyectadaDto? _demandaProyectada;
    [ObservableProperty] private ObservableCollection<DemandaMatrizFilaView> _demandaMatrizFilas = [];
    [ObservableProperty] private PresupuestosCarreraDto? _presupuestos;
    [ObservableProperty] private IReadOnlyList<string> _presupuestosEtiquetasPeriodos = [];
    [ObservableProperty] private ObservableCollection<PresupuestoPeriodoMatrizFilaView> _presupuestosBaseMatrizFilas = [];
    [ObservableProperty] private ObservableCollection<PresupuestoPeriodoMatrizFilaView> _presupuestosCarreraMatrizFilas = [];
    [ObservableProperty] private ObservableCollection<PresupuestoPeriodoMatrizFilaView> _seguroBecasMatrizFilas = [];
    [ObservableProperty] private string _presupuestosTotalAsignadoCarreraDisplay = "$ 0.00";
    [ObservableProperty] private string _presupuestosTotalSeguroDisplay = "$ 0.00";
    [ObservableProperty] private string _presupuestosTotalBecasDisplay = "$ 0.00";
    [ObservableProperty] private IngresosProyectadosDto? _ingresos;
    [ObservableProperty] private ObservableCollection<IngresosMatrizFilaView> _ingresosMatrizFilas = [];
    [ObservableProperty] private ObservableCollection<ArancelCicloResumenView> _arancelesPorCiclo = [];
    [ObservableProperty] private MaterialesProyectadosDto? _materiales;
    [ObservableProperty] private ObservableCollection<MaterialCantidadMatrizFilaView> _materialesCantidadMatrizFilas = [];
    [ObservableProperty] private ObservableCollection<MaterialMonetarioMatrizFilaView> _materialesMonetariosMatrizFilas = [];
    [ObservableProperty] private IReadOnlyList<string> _materialesEtiquetasPeriodos = [];
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
    [ObservableProperty] private string _ratioFormUnidad = UnidadRatioMaterialExtensiones.PorEstudianteText;
    [ObservableProperty] private string _ratioFormMeses = "6";
    [ObservableProperty] private string _ratioFormAdicionalFijo = "0";
    [ObservableProperty] private bool _ratioFormAplicaInflacion = true;
    [ObservableProperty] private int? _ratioFormItemMaterialId;

    // Descuentos de arancel por ciclo. Tabla autogenerada con una fila por ciclo (1..N de la
    // carrera); el usuario edita solo el % y se guarda en bloque.
    [ObservableProperty] private ObservableCollection<DescuentoCicloEditableView> _descuentosPorCiclo = [];
    [ObservableProperty] private bool _descuentosEditables;

    [ObservableProperty] private bool _formVisible;
    [ObservableProperty] private bool _formEsEdicion;
    [ObservableProperty] private int _formId;
    [ObservableProperty] private string _formModoCalculo = ModoManual;
    [ObservableProperty] private string _formArancelManual = "0";
    [ObservableProperty] private bool _formUsaPorcentajeInstitucional = true;
    [ObservableProperty] private string _formPorcentajeMatricula = "10";
    [ObservableProperty] private bool _formEscenarioGlobal = true;
    [ObservableProperty] private int? _formEscenarioProyeccionId;

    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private string _mensajeInfo = string.Empty;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;

    public IReadOnlyList<string> ModosCalculo { get; } = [ModoManual, ModoAutomatico, ModoOptimoFinanciero];
    public IReadOnlyList<string> CategoriasRatio { get; } = ["MATERIALES_SUMINISTROS", "ASEO_LIMPIEZA", "ACCESORIOS_MATERIALES", "OTRO"];
    public IReadOnlyList<UnidadConsumoOpcion> UnidadesRatio { get; } =
    [
        new() { Valor = UnidadRatioMaterialExtensiones.PorEstudianteText, Display = "Por estudiante" },
        new() { Valor = UnidadRatioMaterialExtensiones.PorEstudianteMesText, Display = "Por estudiante / mes" },
        new() { Valor = UnidadRatioMaterialExtensiones.FijoPeriodoText, Display = "Fijo por período" },
        new() { Valor = UnidadRatioMaterialExtensiones.PorDocenteText, Display = "Por docente" }
    ];
    public string TituloRatioFormulario => RatioFormEsEdicion ? "Editar consumo" : "Nuevo consumo";
    public bool RatioFormMesesEditable => RatioFormUnidad is not (UnidadRatioMaterialExtensiones.FijoPeriodoText
        or UnidadRatioMaterialExtensiones.PorDocenteText);
    public bool RatioFormAdicionalFijoEditable => RatioFormUnidad == UnidadRatioMaterialExtensiones.PorDocenteText;
    public string RatioFormConsumoEtiqueta => RatioFormUnidad switch
    {
        UnidadRatioMaterialExtensiones.FijoPeriodoText => "Cantidad fija por período",
        UnidadRatioMaterialExtensiones.PorDocenteText => "Consumo por docente",
        _ => "Consumo por estudiante"
    };
    public string RatioFormItemAdvertencia
    {
        get
        {
            if (RatioFormItemSeleccionado is null || RatioFormItemSeleccionado.EsSinItem)
                return "Sin item vinculado: Materiales Monetarios calculará costo 0.";

            return RatioFormItemSeleccionado.PrecioUnitario <= 0m
                ? "El item seleccionado tiene precio unitario 0; Materiales Monetarios calculará costo 0."
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

    partial void OnRatioFormUnidadChanged(string value)
    {
        if (value is UnidadRatioMaterialExtensiones.FijoPeriodoText
            or UnidadRatioMaterialExtensiones.PorDocenteText)
            RatioFormMeses = "1";
        if (value != UnidadRatioMaterialExtensiones.PorDocenteText)
            RatioFormAdicionalFijo = "0";

        OnPropertyChanged(nameof(RatioFormMesesEditable));
        OnPropertyChanged(nameof(RatioFormAdicionalFijoEditable));
        OnPropertyChanged(nameof(RatioFormConsumoEtiqueta));
    }

    partial void OnDemandaProyectadaChanged(DemandaProyectadaDto? value)
    {
        DemandaMatrizFilas = ConstruirFilasDemanda(value);
        ReconstruirMatricesPresupuestosYSeguro();
    }

    partial void OnPresupuestosChanged(PresupuestosCarreraDto? value)
    {
        _ = value;
        ReconstruirMatricesPresupuestosYSeguro();
    }

    partial void OnIngresosChanged(IngresosProyectadosDto? value)
    {
        IngresosMatrizFilas = ConstruirFilasIngresos(value);
        ArancelesPorCiclo = ConstruirArancelesPorCiclo(value);
        ReconstruirMatricesPresupuestosYSeguro();
    }

    // Resumen por ciclo del arancel cobrado tras el descuento comercial.
    private static ObservableCollection<ArancelCicloResumenView> ConstruirArancelesPorCiclo(IngresosProyectadosDto? dto)
    {
        if (dto is null || dto.Filas.Count == 0)
            return [];

        var filas = dto.Filas
            .Where(f => f.Periodos.Count > 0)
            .Select(f =>
            {
                var celda = f.Periodos[0];
                return new ArancelCicloResumenView
                {
                    CicloDisplay = f.CicloDisplay,
                    ArancelBaseDisplay = celda.ArancelBaseDisplay,
                    DescuentoDisplay = celda.DescuentoCicloDisplay,
                    ArancelCicloDisplay = celda.ArancelCicloDisplay,
                    MatriculaCicloDisplay = celda.MatriculaCicloDisplay
                };
            });
        return new ObservableCollection<ArancelCicloResumenView>(filas);
    }

    partial void OnArancelSugeridoCostoCarreraChanged(ArancelOptimoCarreraDto? value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeUsarArancelSugerido));
        NotificarComparativoAranceles();
    }

    partial void OnConfiguracionesChanged(ObservableCollection<ConfiguracionArancelCarreraDto> value)
    {
        _ = value;
        NotificarComparativoAranceles();
    }

    // KAN-46: panel comparativo de los 3 aranceles (sin ejecutar la bisección).
    private ConfiguracionArancelCarreraDto? ConfiguracionVigente()
    {
        var escenarioId = EscenarioSeleccionado?.Id;
        return Configuraciones.FirstOrDefault(c => c.EscenarioProyeccionId == escenarioId)
            ?? Configuraciones.FirstOrDefault(c => c.EscenarioProyeccionId == null);
    }

    public string ArancelDeseadoComparativoDisplay
    {
        get
        {
            var config = ConfiguracionVigente();
            return config is not null
                && string.Equals(config.ModoCalculoArancel, ModoManual, StringComparison.OrdinalIgnoreCase)
                && config.ArancelManual is > 0m
                ? $"$ {config.ArancelManual.Value:N2}"
                : "—";
        }
    }

    public string ArancelReferencialComparativoDisplay
        => ArancelSugeridoCostoCarrera?.Disponible == true && ArancelSugeridoCostoCarrera.ArancelSugeridoSemestre > 0m
            ? $"$ {ArancelSugeridoCostoCarrera.ArancelSugeridoSemestre:N2}"
            : "Costo de carrera pendiente";

    public string ArancelOptimoComparativoDisplay
    {
        get
        {
            var config = ConfiguracionVigente();
            return config is not null
                && string.Equals(config.ModoCalculoArancel, ModoOptimoFinanciero, StringComparison.OrdinalIgnoreCase)
                && config.ArancelManual is > 0m
                ? $"$ {config.ArancelManual.Value:N2}"
                : "No aplicado";
        }
    }

    private void NotificarComparativoAranceles()
    {
        OnPropertyChanged(nameof(ArancelDeseadoComparativoDisplay));
        OnPropertyChanged(nameof(ArancelReferencialComparativoDisplay));
        OnPropertyChanged(nameof(ArancelOptimoComparativoDisplay));
    }

    partial void OnMaterialesChanged(MaterialesProyectadosDto? value)
    {
        MaterialesEtiquetasPeriodos = ObtenerEtiquetasMateriales(value);
        MaterialesCantidadMatrizFilas = ConstruirFilasMaterialesCantidades(value, MaterialesEtiquetasPeriodos);
        MaterialesMonetariosMatrizFilas = ConstruirFilasMaterialesMonetarios(value, MaterialesEtiquetasPeriodos);
    }
    public string TituloFormulario => FormEsEdicion ? "Editar configuración" : "Nueva configuración";
    public bool FormEsModoManual => EsModoValorFijo(FormModoCalculo);

    private static bool EsModoValorFijo(string? modo)
        => string.Equals(modo, ModoManual, StringComparison.OrdinalIgnoreCase)
        || string.Equals(modo, ModoOptimoFinanciero, StringComparison.OrdinalIgnoreCase);
    public bool FormPorcentajeMatriculaEditable => !FormUsaPorcentajeInstitucional;
    public bool NoEstaGuardando => !EstaGuardando;
    public bool PuedeTrabajar => CarreraSeleccionada is not null && !EstaCargando;
    public bool PuedeEditarDescuentos => PuedeEditar
                                         && PuedeTrabajar
                                         && !EstaCargando
                                         && !EstaGuardando
                                         && !DescuentosEditables
                                         && DescuentosPorCiclo.Count > 0;
    public bool PuedeGuardarDescuentos => PuedeEditar
                                          && PuedeTrabajar
                                          && !EstaCargando
                                          && !EstaGuardando
                                          && DescuentosEditables;
    public bool PuedeCancelarDescuentos => DescuentosEditables && !EstaGuardando;
    public bool PuedeUsarArancelSugerido => PuedeEditar
                                            && !EstaGuardando
                                            && ArancelSugeridoCostoCarrera?.Disponible == true
                                            && CarreraSeleccionada is not null
                                            && EscenarioSeleccionado is not null;

    public bool PuedeEditar => _sesionActual.EsAdministrador
                            || _sesionActual.TienePermiso("DI_NG.EDITAR");

    partial void OnCarreraSeleccionadaChanged(Carrera? value)
    {
        _ = value;
        DescuentosEditables = false;
        OnPropertyChanged(nameof(PuedeTrabajar));
        OnPropertyChanged(nameof(PuedeUsarArancelSugerido));
        NotificarEstadoDescuentos();
        if (_suprimirCambios || EstaCargando) return;
        _ = RecargarPorCarreraAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioDemandaIngresosDto? value)
    {
        _ = value;
        DescuentosEditables = false;
        OnPropertyChanged(nameof(PuedeUsarArancelSugerido));
        NotificarComparativoAranceles();
        NotificarEstadoDescuentos();
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

    partial void OnFormEscenarioGlobalChanged(bool value)
    {
        if (!value && FormEscenarioProyeccionId is null)
            FormEscenarioProyeccionId = EscenarioSeleccionado?.Id;
    }

    partial void OnEstaGuardandoChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(NoEstaGuardando));
        OnPropertyChanged(nameof(PuedeUsarArancelSugerido));
        NotificarEstadoDescuentos();
    }

    partial void OnEstaCargandoChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeTrabajar));
        OnPropertyChanged(nameof(PuedeUsarArancelSugerido));
        NotificarEstadoDescuentos();
    }

    partial void OnDescuentosEditablesChanged(bool value)
    {
        _ = value;
        NotificarEstadoDescuentos();
    }

    partial void OnDescuentosPorCicloChanged(ObservableCollection<DescuentoCicloEditableView> value)
    {
        _ = value;
        NotificarEstadoDescuentos();
    }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        MensajeInfo = string.Empty;
        try
        {
            var teniaCarrerasCargadas = Carreras.Count > 0;
            using var scope = _serviceProvider.CreateScope();
            var repoCarreras = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var carreras = await repoCarreras.ListarAsync();

            var carreraActualId = CarreraSeleccionada?.Id;
            _suprimirCambios = true;
            try
            {
                Carreras = new ObservableCollection<Carrera>(carreras);
                CarreraSeleccionada = carreraActualId is > 0
                    ? Carreras.FirstOrDefault(c => c.Id == carreraActualId.Value)
                    : null;
            }
            finally { _suprimirCambios = false; }

            if (CarreraSeleccionada is null)
            {
                LimpiarTodo();
                MensajeInfo = Carreras.Count == 0
                    ? "No hay carreras registradas. Crea una carrera primero."
                    : teniaCarrerasCargadas
                        ? "Selecciona una carrera para refrescar la información."
                        : "Selecciona una carrera para cargar Demanda e Ingresos.";
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
            MensajeInfo = "Selecciona una carrera para cargar Demanda e Ingresos.";
            return;
        }

        try
        {
            MensajeInfo = string.Empty;
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
            _refrescoArancelVersion++;
            LimpiarTodo();
            MensajeInfo = "Selecciona una carrera para refrescar la información.";
            return;
        }

        if (EscenarioSeleccionado is null)
        {
            _refrescoArancelVersion++;
            ArancelEfectivo = null;
            ArancelSugeridoCostoCarrera = null;
            DemandaProyectada = null;
            Presupuestos = null;
            Ingresos = null;
            Materiales = null;
            Ratios = [];
            DescuentosPorCiclo = [];
            ActualizarResumen();
            MensajeError = "Selecciona un escenario para calcular Demanda e Ingresos.";
            return;
        }

        var version = ++_refrescoArancelVersion;
        var carreraId = CarreraSeleccionada.Id;
        var escenarioId = EscenarioSeleccionado.Id;

        EstaCargando = true;
        try
        {
            MensajeError = string.Empty;
            MensajeInfo = string.Empty;
            ArancelEfectivo = null;
            ArancelSugeridoCostoCarrera = null;
            DemandaProyectada = null;
            Presupuestos = null;
            Ingresos = null;
            Materiales = null;

            using var scope = _serviceProvider.CreateScope();
            var queryArancel = scope.ServiceProvider.GetRequiredService<ObtenerArancelEfectivoQuery>();
            var arancel = await queryArancel.EjecutarAsync(carreraId, escenarioId);

            var queryArancelSugerido = scope.ServiceProvider.GetRequiredService<ObtenerArancelOptimoCarreraQuery>();
            var arancelSugerido = await queryArancelSugerido.EjecutarAsync(carreraId, escenarioId);

            var queryDemanda = scope.ServiceProvider.GetRequiredService<ObtenerDemandaProyectadaQuery>();
            var demanda = await queryDemanda.EjecutarAsync(carreraId, escenarioId);

            var queryPresup = scope.ServiceProvider.GetRequiredService<ObtenerPresupuestosCarreraQuery>();
            var presupuestos = await queryPresup.EjecutarAsync(carreraId, escenarioId);

            var queryIngresos = scope.ServiceProvider.GetRequiredService<CalcularIngresosProyectadosQuery>();
            var ingresos = await queryIngresos.EjecutarAsync(carreraId, escenarioId);

            var queryMateriales = scope.ServiceProvider.GetRequiredService<CalcularMaterialesPorPeriodoQuery>();
            var materiales = await queryMateriales.EjecutarAsync(carreraId, escenarioId);

            var listarRatios = scope.ServiceProvider.GetRequiredService<ListarRatiosMaterialDemandaQuery>();
            var ratios = await listarRatios.EjecutarAsync(carreraId);

            var obtenerDescuentos = scope.ServiceProvider.GetRequiredService<ObtenerDescuentosArancelCicloQuery>();
            var descuentos = await obtenerDescuentos.EjecutarAsync(carreraId, escenarioId);

            var listarConfigs = scope.ServiceProvider.GetRequiredService<ListarConfiguracionesArancelCarreraQuery>();
            var configs = await listarConfigs.EjecutarAsync(carreraId);

            if (!EsRefrescoVigente(version, carreraId, escenarioId))
                return;

            ArancelEfectivo = arancel;
            ArancelSugeridoCostoCarrera = arancelSugerido;
            DemandaProyectada = demanda;
            Presupuestos = presupuestos;
            Ingresos = ingresos;
            Materiales = materiales;
            Configuraciones = new ObservableCollection<ConfiguracionArancelCarreraDto>(configs);
            Ratios = new ObservableCollection<RatioMaterialDemandaDto>(ratios);
            ConstruirDescuentosPorCiclo(descuentos);

            ActualizarResumen();
        }
        catch (Exception ex)
        {
            if (!EsRefrescoVigente(version, carreraId, escenarioId))
                return;

            MensajeError = $"Error al calcular arancel/presupuestos/ingresos/materiales: {Detalle(ex)}";
            ArancelEfectivo = null;
            ArancelSugeridoCostoCarrera = null;
            DemandaProyectada = null;
            Presupuestos = null;
            Ingresos = null;
            Materiales = null;
            Ratios = [];
            DescuentosPorCiclo = [];
            ActualizarResumen();
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private bool EsRefrescoVigente(int version, int carreraId, int escenarioId)
        => version == _refrescoArancelVersion
           && CarreraSeleccionada?.Id == carreraId
           && EscenarioSeleccionado?.Id == escenarioId;

    [RelayCommand]
    private void AbrirNuevoRatio()
    {
        if (!PuedeEditar) { MensajeError = "No tienes permiso para editar."; return; }
        if (CarreraSeleccionada is null) { MensajeError = "Selecciona una carrera."; return; }
        RatioFormId = 0;
        RatioFormCategoria = "MATERIALES_SUMINISTROS";
        RatioFormConcepto = string.Empty;
        RatioFormRatio = "0";
        RatioFormUnidad = UnidadRatioMaterialExtensiones.PorEstudianteText;
        RatioFormMeses = "6";
        RatioFormAdicionalFijo = "0";
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
        RatioFormAdicionalFijo = dto.CantidadFijaAdicional.ToString("0.####", CultureInfo.InvariantCulture);
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
        var unidad = RatioFormUnidad;
        var meses = 1;
        if (unidad is UnidadRatioMaterialExtensiones.FijoPeriodoText
            or UnidadRatioMaterialExtensiones.PorDocenteText)
        {
            RatioFormMeses = "1";
        }
        else if (!int.TryParse(RatioFormMeses, NumberStyles.Integer, CultureInfo.InvariantCulture, out meses)
            || meses <= 0 || meses > 12)
        {
            MensajeError = "Meses operativos entre 1 y 12.";
            return;
        }
        var adicionalFijo = 0m;
        if (unidad == UnidadRatioMaterialExtensiones.PorDocenteText
            && (!TryDecimal(RatioFormAdicionalFijo, out adicionalFijo) || adicionalFijo < 0m))
        {
            MensajeError = "Adicional fijo invalido (>= 0).";
            return;
        }
        if (unidad != UnidadRatioMaterialExtensiones.PorDocenteText)
            RatioFormAdicionalFijo = "0";

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
                UnidadRatio = unidad,
                MesesOperativos = meses,
                CantidadFijaAdicional = adicionalFijo,
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
    private async Task GenerarConsumosPorDefectoAsync()
    {
        if (!PuedeEditar) { MensajeError = "No tienes permiso para editar."; return; }
        if (CarreraSeleccionada is null) { MensajeError = "Selecciona una carrera."; return; }

        EstaGuardando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<GenerarRatiosPorDefectoCarreraCommand>();
            var insertados = await command.EjecutarAsync(CarreraSeleccionada.Id);
            MensajeExito = insertados > 0
                ? $"Se generaron {insertados} consumos por defecto."
                : "La carrera ya tiene los consumos por defecto.";
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

    private void ConstruirDescuentosPorCiclo(MatrizDescuentosArancelDto matriz)
    {
        DescuentosEditables = false;
        var totalCiclos = (CarreraSeleccionada?.TotalCiclos ?? 0) > 0 ? CarreraSeleccionada!.TotalCiclos : 8;

        // Cada ciclo cubierto por un descuento activo (rango o por-ciclo) hereda su %; el resto queda en 0.
        var porCiclo = new Dictionary<int, decimal>();
        foreach (var d in matriz.Descuentos.Where(x => x.EstaActivo))
            for (var c = d.CicloDesde; c <= d.CicloHasta; c++)
                porCiclo[c] = d.PorcentajeDescuento;

        var filas = Enumerable.Range(1, totalCiclos)
            .Select(c => new DescuentoCicloEditableView
            {
                NumeroCiclo = c,
                Porcentaje = porCiclo.TryGetValue(c, out var p) ? p : 0m
            })
            .ToList();
        _descuentosGuardadosPorCiclo = filas.ToDictionary(f => f.NumeroCiclo, f => f.Porcentaje);
        DescuentosPorCiclo = new ObservableCollection<DescuentoCicloEditableView>(filas);
    }

    [RelayCommand]
    private void EditarDescuentos()
    {
        if (!PuedeEditar) { MensajeError = "No tienes permiso para editar."; return; }
        if (CarreraSeleccionada is null) { MensajeError = "Selecciona una carrera."; return; }
        if (EscenarioSeleccionado is null) { MensajeError = "Selecciona un escenario."; return; }
        if (DescuentosPorCiclo.Count == 0) { MensajeError = "No hay descuentos por ciclo cargados."; return; }

        DescuentosEditables = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        MensajeInfo = string.Empty;
    }

    [RelayCommand]
    private void CancelarEdicionDescuentos()
    {
        RestaurarDescuentosGuardados();
        DescuentosEditables = false;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        MensajeInfo = "Edición de descuentos cancelada.";
    }

    [RelayCommand]
    private async Task GuardarDescuentosAsync()
    {
        if (!PuedeEditar) { MensajeError = "No tienes permiso para editar."; return; }
        if (CarreraSeleccionada is null) { MensajeError = "Selecciona una carrera."; return; }
        if (EscenarioSeleccionado is null) { MensajeError = "Selecciona un escenario."; return; }
        if (!DescuentosEditables) { MensajeError = "Presiona Editar descuentos antes de guardar."; return; }

        EstaGuardando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        MensajeInfo = string.Empty;
        try
        {
            var filas = DescuentosPorCiclo
                .Select(d => (d.NumeroCiclo, d.Porcentaje))
                .ToList();
            if (filas.Any(d => d.Porcentaje < 0m || d.Porcentaje > 100m))
            {
                MensajeError = "El descuento debe estar entre 0% y 100%.";
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var command = scope.ServiceProvider.GetRequiredService<GuardarMatrizDescuentosArancelCommand>();
            await command.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id, filas, _sesionActual.UsuarioId);
            DescuentosEditables = false;
            MensajeExito = "Descuentos por ciclo guardados.";
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

    private void RestaurarDescuentosGuardados()
    {
        foreach (var fila in DescuentosPorCiclo)
            fila.Porcentaje = _descuentosGuardadosPorCiclo.TryGetValue(fila.NumeroCiclo, out var valor) ? valor : 0m;
    }

    private void NotificarEstadoDescuentos()
    {
        OnPropertyChanged(nameof(PuedeEditarDescuentos));
        OnPropertyChanged(nameof(PuedeGuardarDescuentos));
        OnPropertyChanged(nameof(PuedeCancelarDescuentos));
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
        FormEscenarioProyeccionId = EscenarioSeleccionado?.Id;
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
        FormEscenarioProyeccionId = dto.EscenarioProyeccionId;
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
        if (EsModoValorFijo(FormModoCalculo)
            && (!TryDecimal(FormArancelManual, out var arancel) || arancel <= 0m))
        {
            MensajeError = "Arancel inválido (debe ser > 0).";
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
                EscenarioProyeccionId = FormEscenarioGlobal ? null : FormEscenarioProyeccionId ?? EscenarioSeleccionado?.Id,
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
    private async Task UsarArancelSugeridoAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tienes permiso para editar.";
            return;
        }
        if (CarreraSeleccionada is null || EscenarioSeleccionado is null)
        {
            MensajeError = "Selecciona carrera y escenario.";
            return;
        }

        EstaGuardando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryOptimo = scope.ServiceProvider.GetRequiredService<ObtenerArancelOptimoCarreraQuery>();
            var optimo = ArancelSugeridoCostoCarrera?.Disponible == true
                ? ArancelSugeridoCostoCarrera
                : await queryOptimo.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);

            if (!optimo.Disponible || optimo.ArancelSugeridoSemestre is not > 0m)
            {
                MensajeError = optimo.MensajeAdvertencia ?? "Costo de Carrera pendiente; no hay arancel sugerido para copiar.";
                return;
            }

            var listarConfigs = scope.ServiceProvider.GetRequiredService<ListarConfiguracionesArancelCarreraQuery>();
            var configs = await listarConfigs.EjecutarAsync(CarreraSeleccionada.Id);
            var configExacta = configs.FirstOrDefault(c => c.EscenarioProyeccionId == EscenarioSeleccionado.Id);
            var usaPorcentajeInstitucional = configExacta?.UsaPorcentajeMatriculaInstitucional ?? true;

            var dto = new GuardarConfiguracionArancelCarreraDto
            {
                Id = configExacta?.Id,
                CarreraId = CarreraSeleccionada.Id,
                EscenarioProyeccionId = EscenarioSeleccionado.Id,
                ModoCalculoArancel = ModoManual,
                ArancelManual = optimo.ArancelSugeridoSemestre,
                UsaPorcentajeMatriculaInstitucional = usaPorcentajeInstitucional,
                PorcentajeMatricula = usaPorcentajeInstitucional
                    ? null
                    : configExacta?.PorcentajeMatricula ?? optimo.PorcentajeMatriculaAplicado
            };

            var command = scope.ServiceProvider.GetRequiredService<GuardarConfiguracionArancelCarreraCommand>();
            await command.EjecutarAsync(dto, _sesionActual.UsuarioId);

            MensajeExito = "Arancel sugerido copiado como configuración manual del escenario.";
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
        DescuentosEditables = false;
        _descuentosGuardadosPorCiclo = [];
        Configuraciones = [];
        Escenarios = [];
        EscenarioSeleccionado = null;
        ArancelEfectivo = null;
        ArancelSugeridoCostoCarrera = null;
        DemandaProyectada = null;
        Presupuestos = null;
        Ingresos = null;
        Materiales = null;
        MaterialesCantidadMatrizFilas = [];
        MaterialesMonetariosMatrizFilas = [];
        MaterialesEtiquetasPeriodos = [];
        Ratios = [];
        DescuentosPorCiclo = [];
        ItemsCapitalTrabajo = [];
        RatioFormItemSeleccionado = null;
        ActualizarResumen();
    }

    private void ReconstruirMatricesPresupuestosYSeguro()
    {
        var etiquetas = DemandaProyectada?.EtiquetasPeriodos?.ToList() ?? [];
        PresupuestosEtiquetasPeriodos = etiquetas;

        if (Presupuestos is null || DemandaProyectada is null || etiquetas.Count == 0)
        {
            PresupuestosBaseMatrizFilas = [];
            PresupuestosCarreraMatrizFilas = [];
            SeguroBecasMatrizFilas = [];
            PresupuestosTotalAsignadoCarreraDisplay = Moneda(0m);
            PresupuestosTotalSeguroDisplay = Moneda(0m);
            PresupuestosTotalBecasDisplay = Moneda(0m);
            return;
        }

        var semestres = Presupuestos.SemestresPorAnio <= 0 ? 2 : Presupuestos.SemestresPorAnio;
        var factores = CompletarFactores(Presupuestos.FactoresInflacionPeriodos, etiquetas.Count);
        var estudiantes = CompletarValores(DemandaProyectada.TotalesPorPeriodo, etiquetas.Count);
        var docentesRequeridos = ObtenerDocentesRequeridosPorPeriodo(DemandaProyectada, etiquetas.Count);

        var presupuestoCapacitacion = ConstruirBaseInstitucional(Presupuestos.PresupuestoAnualCapacitacion, semestres, factores);
        var presupuestoInternacionalizacion = ConstruirBaseInstitucional(Presupuestos.PresupuestoAnualInternacionalizacion, semestres, factores);
        var presupuestoMarketing = ConstruirBaseInstitucional(Presupuestos.PresupuestoAnualMarketing, semestres, factores);

        PresupuestosBaseMatrizFilas = new ObservableCollection<PresupuestoPeriodoMatrizFilaView>
        {
            CrearFilaMonetaria("Presupuestos base institucionales", "Presupuesto Capacitación", presupuestoCapacitacion),
            CrearFilaMonetaria("Presupuestos base institucionales", "Presupuesto Internacionalización", presupuestoInternacionalizacion),
            CrearFilaMonetaria("Presupuestos base institucionales", "Presupuesto Marketing", presupuestoMarketing)
        };

        var capacitacionCarrera = ConstruirAsignadoPorDocentes(
            presupuestoCapacitacion,
            Presupuestos.DocentesUniversidad,
            docentesRequeridos);
        var internacionalizacionCarrera = ConstruirAsignadoPorEstudiantes(
            presupuestoInternacionalizacion,
            Presupuestos.EstudiantesUniversidad,
            estudiantes);
        var marketingCarrera = ConstruirAsignadoPorEstudiantes(
            presupuestoMarketing,
            Presupuestos.EstudiantesUniversidad,
            estudiantes);

        PresupuestosCarreraMatrizFilas = new ObservableCollection<PresupuestoPeriodoMatrizFilaView>
        {
            CrearFilaMonetaria("Montos asignados a la carrera", "Capacitación Docente", capacitacionCarrera),
            CrearFilaMonetaria("Montos asignados a la carrera", "Internacionalización", internacionalizacionCarrera),
            CrearFilaMonetaria("Montos asignados a la carrera", "Marketing y Comunicación", marketingCarrera)
        };

        var seguroPorEstudianteBase = Presupuestos.EstudiantesUniversidad > 0
            ? Presupuestos.PolizaSeguroEstudiantilAnual / semestres / Presupuestos.EstudiantesUniversidad
            : 0m;
        var seguroPorEstudiante = factores
            .Select(f => decimal.Round(seguroPorEstudianteBase * f, 2))
            .ToList();
        var costoSeguro = seguroPorEstudiante
            .Select((valor, i) => decimal.Round(valor * estudiantes[i], 2))
            .ToList();

        var matricula = Ingresos?.MatriculaEfectiva ?? 0m;
        var arancel = Ingresos?.ArancelEfectivo ?? 0m;
        var porcentajeBecas = Ingresos?.PorcentajeBecasAplicado ?? Presupuestos.PorcentajeBecasInstitucionales;
        var becas = estudiantes
            .Select(total => decimal.Round(total * (arancel + matricula) * porcentajeBecas / 100m, 2))
            .ToList();
        var matriculas = Enumerable.Repeat(decimal.Round(matricula, 2), etiquetas.Count).ToList();
        var aranceles = Enumerable.Repeat(decimal.Round(arancel, 2), etiquetas.Count).ToList();

        SeguroBecasMatrizFilas = new ObservableCollection<PresupuestoPeriodoMatrizFilaView>
        {
            CrearFilaMonetaria("Seguro, becas y valores por estudiante", "Seguro Estudiantil", seguroPorEstudiante, mostrarTotal: false),
            CrearFilaMonetaria("Seguro, becas y valores por estudiante", "Costo del Seguro Estudiantil", costoSeguro),
            CrearFilaMonetaria("Seguro, becas y valores por estudiante", "Becas Institucionales", becas),
            CrearFilaMonetaria("Seguro, becas y valores por estudiante", "Matrícula", matriculas, mostrarTotal: false),
            CrearFilaMonetaria("Seguro, becas y valores por estudiante", "Valor ciclo por estudiante", aranceles, mostrarTotal: false)
        };

        PresupuestosTotalAsignadoCarreraDisplay = Moneda(
            capacitacionCarrera.Sum() + internacionalizacionCarrera.Sum() + marketingCarrera.Sum());
        PresupuestosTotalSeguroDisplay = Moneda(costoSeguro.Sum());
        PresupuestosTotalBecasDisplay = Moneda(becas.Sum());
    }

    private static IReadOnlyList<decimal> ConstruirBaseInstitucional(
        decimal presupuestoAnual,
        int semestresPorAnio,
        IReadOnlyList<decimal> factores)
    {
        var basePeriodo = semestresPorAnio > 0 ? presupuestoAnual / semestresPorAnio : 0m;
        return factores.Select(f => decimal.Round(basePeriodo * f, 2)).ToList();
    }

    private static IReadOnlyList<decimal> ConstruirAsignadoPorDocentes(
        IReadOnlyList<decimal> presupuestoPeriodo,
        int docentesUniversidad,
        IReadOnlyList<decimal> docentesRequeridos)
    {
        return presupuestoPeriodo
            .Select((monto, i) => docentesUniversidad > 0
                ? decimal.Round(monto / docentesUniversidad * docentesRequeridos[i], 2)
                : 0m)
            .ToList();
    }

    private static IReadOnlyList<decimal> ConstruirAsignadoPorEstudiantes(
        IReadOnlyList<decimal> presupuestoPeriodo,
        int estudiantesUniversidad,
        IReadOnlyList<decimal> estudiantesCarrera)
    {
        return presupuestoPeriodo
            .Select((monto, i) => estudiantesUniversidad > 0
                ? decimal.Round(monto / estudiantesUniversidad * estudiantesCarrera[i], 2)
                : 0m)
            .ToList();
    }

    private static IReadOnlyList<decimal> ObtenerDocentesRequeridosPorPeriodo(
        DemandaProyectadaDto demanda,
        int cantidadPeriodos)
    {
        var fila = demanda.DocentesPorPeriodo.FirstOrDefault(f =>
            string.Equals(f.Tipo, "Docentes Requeridos", StringComparison.OrdinalIgnoreCase));

        return CompletarValores(fila?.Periodos ?? [], cantidadPeriodos);
    }

    private static IReadOnlyList<decimal> CompletarFactores(
        IReadOnlyList<decimal> factores,
        int cantidadPeriodos)
    {
        var valores = new List<decimal>(cantidadPeriodos);
        for (var i = 0; i < cantidadPeriodos; i++)
        {
            var factor = i < factores.Count && factores[i] > 0m ? factores[i] : 1m;
            valores.Add(factor);
        }

        return valores;
    }

    private static IReadOnlyList<decimal> CompletarValores(
        IReadOnlyList<decimal> valoresOrigen,
        int cantidadPeriodos)
    {
        var valores = new List<decimal>(cantidadPeriodos);
        for (var i = 0; i < cantidadPeriodos; i++)
            valores.Add(i < valoresOrigen.Count ? valoresOrigen[i] : 0m);

        return valores;
    }

    private static PresupuestoPeriodoMatrizFilaView CrearFilaMonetaria(
        string grupo,
        string concepto,
        IReadOnlyList<decimal> valores,
        bool mostrarTotal = true)
    {
        var total = decimal.Round(valores.Sum(), 2);
        return new PresupuestoPeriodoMatrizFilaView
        {
            Grupo = grupo,
            Concepto = concepto,
            Periodos = valores.Select(v => decimal.Round(v, 2)).ToList(),
            Total = total,
            TotalDisplay = mostrarTotal ? Moneda(total) : string.Empty
        };
    }

    private static string Moneda(decimal valor) => $"$ {valor:N2}";

    private static IReadOnlyList<string> ObtenerEtiquetasMateriales(MaterialesProyectadosDto? materiales)
    {
        if (materiales is null || materiales.Cantidades.Count == 0)
            return [];

        return materiales.Cantidades
            .Select(c => new { c.Anio, c.NumeroPeriodo, c.EtiquetaPeriodo })
            .Distinct()
            .OrderBy(p => p.Anio).ThenBy(p => p.NumeroPeriodo)
            .Select(p => p.EtiquetaPeriodo)
            .ToList();
    }

    private static string FormatearCategoria(string categoria) => categoria?.ToUpperInvariant() switch
    {
        "MATERIALES_SUMINISTROS" => "MATERIALES Y SUMINISTROS",
        "ASEO_LIMPIEZA" => "SUMINISTROS DE ASEO Y LIMPIEZA",
        "ACCESORIOS_MATERIALES" => "ACCESORIOS Y MATERIALES",
        "OTRO" => "OTRO",
        _ => categoria ?? string.Empty
    };

    private static ObservableCollection<MaterialCantidadMatrizFilaView> ConstruirFilasMaterialesCantidades(
        MaterialesProyectadosDto? materiales,
        IReadOnlyList<string> etiquetas)
    {
        if (materiales is null || materiales.Cantidades.Count == 0 || etiquetas.Count == 0)
            return [];

        var filas = new List<MaterialCantidadMatrizFilaView>();
        var totalesGenerales = new decimal[etiquetas.Count];

        var grupos = materiales.Cantidades
            .GroupBy(c => c.Categoria)
            .OrderBy(g => OrdenCategoria(g.Key))
            .ThenBy(g => g.Key);

        foreach (var grupo in grupos)
        {
            filas.Add(new MaterialCantidadMatrizFilaView
            {
                Categoria = FormatearCategoria(grupo.Key),
                Concepto = string.Empty,
                EsEncabezadoCategoria = true
            });

            var conceptos = grupo
                .GroupBy(c => c.Concepto)
                .OrderBy(g => g.Key);

            foreach (var conceptoGrp in conceptos)
            {
                var porEtiqueta = conceptoGrp
                    .GroupBy(c => c.EtiquetaPeriodo)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Cantidad));

                var valores = new List<decimal>(etiquetas.Count);
                for (var i = 0; i < etiquetas.Count; i++)
                {
                    var v = porEtiqueta.TryGetValue(etiquetas[i], out var x) ? x : 0m;
                    valores.Add(v);
                    totalesGenerales[i] += v;
                }

                filas.Add(new MaterialCantidadMatrizFilaView
                {
                    Categoria = FormatearCategoria(grupo.Key),
                    Concepto = conceptoGrp.Key,
                    Periodos = valores,
                    Total = valores.Sum()
                });
            }
        }

        filas.Add(new MaterialCantidadMatrizFilaView
        {
            Categoria = string.Empty,
            Concepto = "TOTAL",
            Periodos = totalesGenerales,
            Total = totalesGenerales.Sum(),
            EsTotal = true
        });

        return new ObservableCollection<MaterialCantidadMatrizFilaView>(filas);
    }

    private static ObservableCollection<MaterialMonetarioMatrizFilaView> ConstruirFilasMaterialesMonetarios(
        MaterialesProyectadosDto? materiales,
        IReadOnlyList<string> etiquetas)
    {
        if (materiales is null || materiales.Monetarios.Count == 0 || etiquetas.Count == 0)
            return [];

        var filas = new List<MaterialMonetarioMatrizFilaView>();
        var totalesGenerales = new decimal[etiquetas.Count];

        var grupos = materiales.Monetarios
            .GroupBy(c => c.Categoria)
            .OrderBy(g => OrdenCategoria(g.Key))
            .ThenBy(g => g.Key);

        foreach (var grupo in grupos)
        {
            filas.Add(new MaterialMonetarioMatrizFilaView
            {
                Categoria = FormatearCategoria(grupo.Key),
                Concepto = string.Empty,
                EsEncabezadoCategoria = true
            });

            var conceptos = grupo
                .GroupBy(c => c.Concepto)
                .OrderBy(g => g.Key);

            foreach (var conceptoGrp in conceptos)
            {
                var porEtiqueta = conceptoGrp
                    .GroupBy(c => c.EtiquetaPeriodo)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Costo));

                var valores = new List<decimal>(etiquetas.Count);
                for (var i = 0; i < etiquetas.Count; i++)
                {
                    var v = porEtiqueta.TryGetValue(etiquetas[i], out var x) ? x : 0m;
                    valores.Add(v);
                    totalesGenerales[i] += v;
                }

                filas.Add(new MaterialMonetarioMatrizFilaView
                {
                    Categoria = FormatearCategoria(grupo.Key),
                    Concepto = conceptoGrp.Key,
                    Periodos = valores,
                    Total = valores.Sum()
                });
            }
        }

        filas.Add(new MaterialMonetarioMatrizFilaView
        {
            Categoria = string.Empty,
            Concepto = "TOTAL",
            Periodos = totalesGenerales,
            Total = totalesGenerales.Sum(),
            EsTotal = true
        });

        return new ObservableCollection<MaterialMonetarioMatrizFilaView>(filas);
    }

    private static int OrdenCategoria(string categoria) => categoria?.ToUpperInvariant() switch
    {
        "MATERIALES_SUMINISTROS" => 1,
        "ASEO_LIMPIEZA" => 2,
        "ACCESORIOS_MATERIALES" => 3,
        "OTRO" => 99,
        _ => 50
    };

    private static ObservableCollection<IngresosMatrizFilaView> ConstruirFilasIngresos(IngresosProyectadosDto? ingresos)
    {
        if (ingresos is null || ingresos.Filas.Count == 0 || ingresos.EtiquetasPeriodos.Count == 0)
            return [];

        var etiquetas = ingresos.EtiquetasPeriodos;
        var filas = ingresos.Filas
            .OrderBy(f => f.NumeroCiclo)
            .Select(f =>
            {
                // Asegurar una celda por etiqueta de período, en el orden de EtiquetasPeriodos
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
        AgregarAdvertencia(advertencias, ArancelSugeridoCostoCarrera?.MensajeAdvertencia);
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
