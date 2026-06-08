using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Mantenimiento;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.UseCases.Mantenimiento;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.Mantenimiento;

public sealed class TipoRubroOpcion
{
    public TipoRubroMantenimiento Valor { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}

public sealed class CarreraMantenimientoOpcion
{
    public int Id { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}

public sealed partial class MantenimientoViewModel : ObservableObject
{
    private readonly IServiceProvider _sp;
    private readonly SesionActual _sesion;
    private bool _suprimirRecarga;

    public MantenimientoViewModel(IServiceProvider sp, SesionActual sesion)
    {
        _sp = sp;
        _sesion = sesion;

        TiposRubro = new ObservableCollection<TipoRubroOpcion>([
            new TipoRubroOpcion { Valor = TipoRubroMantenimiento.ServicioBasico, Etiqueta = "Servicio Básico" },
            new TipoRubroOpcion { Valor = TipoRubroMantenimiento.Mantenimiento,  Etiqueta = "Mantenimiento"  },
        ]);
        TipoRubroForm = TiposRubro[0];
    }

    [ObservableProperty] private ObservableCollection<CarreraMantenimientoOpcion> _carreras = [];
    [ObservableProperty] private CarreraMantenimientoOpcion? _carreraSeleccionada;
    [ObservableProperty] private ObservableCollection<EscenarioProyeccion> _escenarios = [];
    [ObservableProperty] private EscenarioProyeccion? _escenarioSeleccionado;

    [ObservableProperty] private ObservableCollection<ServicioMantenimientoDto> _serviciosBasicos = [];
    [ObservableProperty] private ObservableCollection<ServicioMantenimientoDto> _itemsMantenimiento = [];
    [ObservableProperty] private ServicioMantenimientoDto? _seleccionado;
    [ObservableProperty] private ResumenMantenimientoDto? _resumen;
    [ObservableProperty] private DataView? _proyeccionSemestralVista;

    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private string _mensajeInfo = string.Empty;

    [ObservableProperty] private bool _formVisible;
    [ObservableProperty] private bool _formEsEdicion;
    [ObservableProperty] private int _formId;
    [ObservableProperty] private string _formNombre = string.Empty;
    [ObservableProperty] private string _formCosto = string.Empty;
    [ObservableProperty] private string _formSede = "General";
    [ObservableProperty] private bool _formUsaEscenarioEspecifico;
    [ObservableProperty] private ObservableCollection<TipoRubroOpcion> _tiposRubro = [];
    [ObservableProperty] private TipoRubroOpcion? _tipoRubroForm;

    public bool PuedeCrear => _sesion.EsAdministrador || _sesion.TienePermiso("MI.CREAR");
    public bool PuedeEditar => _sesion.EsAdministrador || _sesion.TienePermiso("MI.EDITAR");
    public bool PuedeEliminar => _sesion.EsAdministrador || _sesion.TienePermiso("MI.ELIMINAR");
    public bool PuedeTrabajar => CarreraSeleccionada is not null && !EstaCargando;

    partial void OnCarreraSeleccionadaChanged(CarreraMantenimientoOpcion? value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeTrabajar));
        if (_suprimirRecarga || EstaCargando)
            return;

        _ = CargarEscenariosAsync();
    }

    partial void OnEscenarioSeleccionadoChanged(EscenarioProyeccion? value)
    {
        _ = value;
        if (_suprimirRecarga || EstaCargando)
            return;

        _ = RecargarDatosAsync();
    }

    partial void OnEstaCargandoChanged(bool value)
    {
        _ = value;
        OnPropertyChanged(nameof(PuedeTrabajar));
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
            var carreraIdActual = CarreraSeleccionada?.Id;
            var escenarioIdActual = EscenarioSeleccionado?.Id;

            using var scope = _sp.CreateScope();
            var repoCarrera = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var lista = await repoCarrera.ListarAsync();

            _suprimirRecarga = true;
            try
            {
                Carreras = new ObservableCollection<CarreraMantenimientoOpcion>(
                    lista.OrderBy(x => x.Codigo)
                        .Select(x => new CarreraMantenimientoOpcion
                        {
                            Id = x.Id,
                            Etiqueta = $"{x.Codigo} - {x.Nombre}"
                        }));

                CarreraSeleccionada = carreraIdActual is > 0
                    ? Carreras.FirstOrDefault(x => x.Id == carreraIdActual.Value)
                    : null;
            }
            finally
            {
                _suprimirRecarga = false;
            }

            if (CarreraSeleccionada is null)
            {
                LimpiarDatos();
                MensajeInfo = Carreras.Count == 0
                    ? "No hay carreras registradas para configurar servicios y mantenimiento."
                    : teniaCarrerasCargadas
                        ? "Selecciona una carrera para refrescar la información."
                        : "Selecciona una carrera para cargar Mantenimiento e Inversión.";
                return;
            }

            await CargarEscenariosAsync(escenarioIdActual);
        }
        catch (Exception ex)
        {
            MensajeError = Detalle(ex);
            LimpiarDatos();
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
            LimpiarDatos();
            MensajeInfo = "Selecciona una carrera para cargar Mantenimiento e Inversión.";
            return;
        }

        try
        {
            MensajeInfo = string.Empty;
            using var scope = _sp.CreateScope();
            var query = scope.ServiceProvider.GetRequiredService<ListarEscenariosConProyeccionPorCarreraQuery>();
            var lista = await query.EjecutarAsync(CarreraSeleccionada.Id);

            _suprimirRecarga = true;
            try
            {
                Escenarios = new ObservableCollection<EscenarioProyeccion>(lista);
                EscenarioSeleccionado = Escenarios.FirstOrDefault(x => x.Id == escenarioIdPreferido)
                    ?? Escenarios.FirstOrDefault();
            }
            finally
            {
                _suprimirRecarga = false;
            }
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar escenarios: {Detalle(ex)}";
            _suprimirRecarga = true;
            try
            {
                Escenarios = [];
                EscenarioSeleccionado = null;
            }
            finally
            {
                _suprimirRecarga = false;
            }
        }

        await RecargarDatosAsync();
    }

    private async Task RecargarDatosAsync()
    {
        if (CarreraSeleccionada is null)
        {
            LimpiarDatos();
            MensajeInfo = "Selecciona una carrera para refrescar la información.";
            return;
        }

        MensajeInfo = string.Empty;
        await RefrescarListasAsync();
        await CargarResumenAsync();
    }

    private async Task RefrescarListasAsync()
    {
        if (CarreraSeleccionada is null)
            return;

        using var scope = _sp.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ListarServiciosMantenimientoQuery>();
        var todos = await query.EjecutarAsync(CarreraSeleccionada.Id, escenarioProyeccionId: EscenarioSeleccionado?.Id);

        ServiciosBasicos = new ObservableCollection<ServicioMantenimientoDto>(
            todos.Where(x => x.TipoRubro == TipoRubroMantenimiento.ServicioBasico));
        ItemsMantenimiento = new ObservableCollection<ServicioMantenimientoDto>(
            todos.Where(x => x.TipoRubro == TipoRubroMantenimiento.Mantenimiento));
    }

    private ServicioMantenimientoDto? BuscarMantenimiento(string nombre)
        => ItemsMantenimiento.FirstOrDefault(x =>
            string.Equals(x.NombreRubro.Trim(), nombre, StringComparison.OrdinalIgnoreCase));

    private async Task CargarResumenAsync()
    {
        if (CarreraSeleccionada is null || EscenarioSeleccionado is null)
        {
            Resumen = null;
            ProyeccionSemestralVista = null;
            MensajeError = EscenarioSeleccionado is null
                ? "Seleccione un escenario con proyección para calcular la proyección semestral."
                : string.Empty;
            return;
        }

        using var scope = _sp.CreateScope();
        var query = scope.ServiceProvider.GetRequiredService<ObtenerResumenMantenimientoQuery>();
        Resumen = await query.EjecutarAsync(CarreraSeleccionada.Id, EscenarioSeleccionado.Id);
        ConstruirMatrizProyeccionSemestral();
    }

    private void ConstruirMatrizProyeccionSemestral()
    {
        if (Resumen is null)
        {
            ProyeccionSemestralVista = null;
            return;
        }

        var tabla = new DataTable();
        tabla.Columns.Add("DESCRIPCIÓN", typeof(string));
        tabla.Columns.Add("VALOR", typeof(string));
        tabla.Columns.Add("UNIDAD", typeof(string));
        tabla.Columns.Add("AÑO", typeof(string));

        var periodos = Resumen.Proyeccion
            .OrderBy(p => p.NumeroPeriodo)
            .Take(8)
            .ToList();

        var filaTotal = tabla.NewRow();
        filaTotal["DESCRIPCIÓN"] = "TOTAL SEMESTRAL";
        filaTotal["VALOR"] = string.Empty;
        filaTotal["UNIDAD"] = string.Empty;
        filaTotal["AÑO"] = "Total";

        foreach (var periodo in periodos)
        {
            var etiquetaSemestre = periodo.Semestre == 1 ? "ABR" : "SEP";
            tabla.Columns.Add($"{periodo.Anio} {etiquetaSemestre}", typeof(string));
        }

        var refacciones = BuscarMantenimiento("Refacciones");
        var garantia = BuscarMantenimiento("Garantía") ?? BuscarMantenimiento("Garantia");
        var refaccionesMensual = refacciones is null ? 0m : decimal.Round(refacciones.CostoAnualUniversidad / 12m, 2);
        var garantiaAnual = garantia?.CostoAnualUniversidad ?? 0m;

        var filaRefacciones = tabla.NewRow();
        filaRefacciones["DESCRIPCIÓN"] = "Refacciones";
        filaRefacciones["VALOR"] = FormatearMonto(refaccionesMensual, mostrarSimbolo: false);
        filaRefacciones["UNIDAD"] = "Mensual";
        filaRefacciones["AÑO"] = "No. Alumnos";

        var filaGarantia = tabla.NewRow();
        filaGarantia["DESCRIPCIÓN"] = "Garantía";
        filaGarantia["VALOR"] = FormatearMonto(garantiaAnual, mostrarSimbolo: false);
        filaGarantia["UNIDAD"] = "Anual";
        filaGarantia["AÑO"] = "Servicios Básicos";

        var filaServicios = tabla.NewRow();
        filaServicios["DESCRIPCIÓN"] = "Servicios Básicos";
        filaServicios["VALOR"] = FormatearMonto(Resumen.ValorMensualServiciosBasicosPorAlumno);
        filaServicios["UNIDAD"] = "Mensual";
        filaServicios["AÑO"] = "Mantenimiento";

        foreach (var periodo in periodos)
        {
            var etiquetaSemestre = periodo.Semestre == 1 ? "ABR" : "SEP";
            var columna = $"{periodo.Anio} {etiquetaSemestre}";

            filaRefacciones[columna] = periodo.DemandaDisplay;
            filaGarantia[columna] = periodo.CostoServiciosBasicosDisplay;
            filaServicios[columna] = periodo.CostoMantenimientoDisplay;
            filaTotal[columna] = periodo.CostoTotalDisplay;
        }

        tabla.Rows.Add(filaRefacciones);
        tabla.Rows.Add(filaGarantia);
        tabla.Rows.Add(filaServicios);
        tabla.Rows.Add(filaTotal);

        ProyeccionSemestralVista = tabla.DefaultView;
    }

    [RelayCommand]
    private void AbrirNuevo(string tipoStr)
    {
        if (!PuedeCrear)
        {
            MensajeError = "No tiene permiso para crear rubros de mantenimiento.";
            return;
        }

        var tipo = tipoStr == "Mantenimiento"
            ? TipoRubroMantenimiento.Mantenimiento
            : TipoRubroMantenimiento.ServicioBasico;
        FormId = 0;
        FormNombre = string.Empty;
        FormCosto = string.Empty;
        FormSede = "General";
        FormUsaEscenarioEspecifico = false;
        TipoRubroForm = TiposRubro.First(t => t.Valor == tipo);
        FormEsEdicion = false;
        FormVisible = true;
    }

    [RelayCommand]
    private void AbrirEditar(ServicioMantenimientoDto item)
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para editar rubros de mantenimiento.";
            return;
        }

        FormId = item.Id;
        FormNombre = item.NombreRubro;
        FormCosto = FormatearMonto(item.CostoAnualUniversidad, mostrarSimbolo: false);
        FormSede = string.IsNullOrWhiteSpace(item.Sede) ? "General" : item.Sede;
        FormUsaEscenarioEspecifico = item.EscenarioProyeccionId.HasValue;
        TipoRubroForm = TiposRubro.First(t => t.Valor == item.TipoRubro);
        FormEsEdicion = true;
        FormVisible = true;
    }

    [RelayCommand]
    private void CancelarForm()
    {
        FormVisible = false;
        MensajeError = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        MensajeError = string.Empty;

        if (!FormEsEdicion && !PuedeCrear)
        {
            MensajeError = "No tiene permiso para crear rubros de mantenimiento.";
            return;
        }

        if (FormEsEdicion && !PuedeEditar)
        {
            MensajeError = "No tiene permiso para editar rubros de mantenimiento.";
            return;
        }

        if (CarreraSeleccionada is null)
        {
            MensajeError = "Seleccione una carrera.";
            return;
        }

        if (!TryDecimal(FormCosto, out var costo) || costo < 0m)
        {
            MensajeError = "Costo inválido. Puede escribir valores como 1000, 1.000 o 1.000,50.";
            return;
        }

        var escenarioId = FormUsaEscenarioEspecifico ? EscenarioSeleccionado?.Id : null;
        EstaGuardando = true;
        try
        {
            using var scope = _sp.CreateScope();
            if (!FormEsEdicion)
            {
                var cmd = scope.ServiceProvider.GetRequiredService<CrearServicioMantenimientoCommand>();
                await cmd.EjecutarAsync(new CrearServicioMantenimientoDto
                {
                    CarreraId = CarreraSeleccionada.Id,
                    EscenarioProyeccionId = escenarioId,
                    Sede = FormSede,
                    TipoRubro = TipoRubroForm!.Valor,
                    NombreRubro = FormNombre,
                    CostoAnualUniversidad = costo,
                });
            }
            else
            {
                var cmd = scope.ServiceProvider.GetRequiredService<ActualizarServicioMantenimientoCommand>();
                await cmd.EjecutarAsync(new ActualizarServicioMantenimientoDto
                {
                    Id = FormId,
                    EscenarioProyeccionId = escenarioId,
                    Sede = FormSede,
                    TipoRubro = TipoRubroForm!.Valor,
                    NombreRubro = FormNombre,
                    CostoAnualUniversidad = costo,
                });
            }
            FormVisible = false;
            MensajeExito = FormEsEdicion ? "Rubro actualizado." : "Rubro creado.";
            await RecargarDatosAsync();
        }
        catch (Exception ex) { MensajeError = Detalle(ex); }
        finally { EstaGuardando = false; }
    }

    [RelayCommand]
    private async Task EliminarAsync(ServicioMantenimientoDto item)
    {
        if (!PuedeEliminar)
        {
            MensajeError = "No tiene permiso para eliminar rubros de mantenimiento.";
            return;
        }

        MensajeError = string.Empty;
        EstaGuardando = true;
        try
        {
            using var scope = _sp.CreateScope();
            var cmd = scope.ServiceProvider.GetRequiredService<EliminarServicioMantenimientoCommand>();
            await cmd.EjecutarAsync(item.Id, _sesion.UsuarioId);
            MensajeExito = "Rubro eliminado.";
            await RecargarDatosAsync();
        }
        catch (Exception ex) { MensajeError = Detalle(ex); }
        finally { EstaGuardando = false; }
    }

    private void LimpiarDatos()
    {
        ServiciosBasicos = [];
        ItemsMantenimiento = [];
        Resumen = null;
        ProyeccionSemestralVista = null;
        Escenarios = [];
        EscenarioSeleccionado = null;
    }

    private static bool TryDecimal(string? value, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var limpio = value.Trim()
            .Replace("$", string.Empty)
            .Replace(" ", string.Empty);

        if (string.IsNullOrWhiteSpace(limpio))
            return false;

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

    private static string FormatearMonto(decimal valor, bool mostrarSimbolo = true)
    {
        if (valor == 0m)
            return mostrarSimbolo ? "$ -" : string.Empty;

        var cultura = new CultureInfo("es-EC");
        var formato = decimal.Truncate(valor) == valor ? "N0" : "N2";
        var texto = valor.ToString(formato, cultura);
        return mostrarSimbolo ? $"$ {texto}" : texto;
    }

    private static string Detalle(Exception ex) => ex.InnerException?.Message ?? ex.Message;
}
