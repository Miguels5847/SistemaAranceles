using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.UseCases.Inflacion;
using SistemaAranceles.Presentation.State;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Windows;

namespace SistemaAranceles.Presentation.ViewModels.Inflacion;

public sealed partial class InflacionViewModel : ObservableObject
{
    private const string FuenteDatoHistorico = "Dato historico";
    private const string FuenteEstimacion = "Est" + "imacion";
    private const string FuenteAjusteManual = "Ajuste manual";
    private static readonly string[] TiposFuenteOpciones = [FuenteDatoHistorico, FuenteEstimacion, FuenteAjusteManual];
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;

    public InflacionViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;

        GraficoInflacion = new PlotModel { Title = "Inflación anual" };
        ResumenImportacion = InstruccionesImportacionExcel;
    }

    public static string InstruccionesImportacionExcel =>
        "El Excel debe tener:\n- Una sola hoja\n- Columna A: Año\n- Columna B: Inflación anual\n- Columna C: Fuente (opcional)";

    [ObservableProperty]
    private ObservableCollection<InflacionAnualDto> _registros = [];

    [ObservableProperty]
    private InflacionAnualDto? _registroSeleccionado;

    [ObservableProperty]
    private string _anio = string.Empty;

    [ObservableProperty]
    private string _porcentajeInflacion = string.Empty;

    [ObservableProperty]
    private string _fuenteNombre = FuenteEstimacion;

    [ObservableProperty]
    private string _tipoFuenteSeleccionado = FuenteEstimacion;

    public static IReadOnlyList<string> TiposFuente => TiposFuenteOpciones;

    [ObservableProperty]
    private string _proyeccionAnioDesde = DateTime.UtcNow.Year.ToString();

    [ObservableProperty]
    private string _proyeccionAnioHasta = (DateTime.UtcNow.Year + 5).ToString();

    [ObservableProperty]
    private string _bceAnioDesde = string.Empty;

    [ObservableProperty]
    private string _bceAnioHasta = string.Empty;

    [ObservableProperty]
    private string _bceTipoSerieSeleccionada = "Annual";

    [ObservableProperty]
    private int _pestaniaSeleccionada = 0;

    [ObservableProperty]
    private bool _estaImportandoManual;

    [ObservableProperty]
    private bool _estaImportandoBce;

    [ObservableProperty]
    private bool _estaProyectando;

    [ObservableProperty]
    private string _resumenImportacion = string.Empty;

    [ObservableProperty]
    private string _resumenImportacionManual = string.Empty;

    [ObservableProperty]
    private string _resumenImportacionBce = string.Empty;

    [ObservableProperty]
    private string _mensajeImportacionManual = string.Empty;

    [ObservableProperty]
    private string _mensajeImportacionBce = string.Empty;

    [ObservableProperty]
    private string _resumenProyeccion = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ImportacionInflacionErrorDto> _erroresImportacionManual = [];

    [ObservableProperty]
    private ObservableCollection<ImportacionInflacionErrorDto> _erroresImportacionBce = [];

    [ObservableProperty]
    private PlotModel _graficoInflacion;

    [ObservableProperty]
    private bool _estaEditando;

    [ObservableProperty]
    private bool _estaCargando;

    [ObservableProperty]
    private bool _estaGuardando;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    public bool PuedeVer => _sesionActual.TienePermiso("INF.VER");
    public bool PuedeEditar => _sesionActual.TienePermiso("INF.ED");

    public string TituloFormulario => EstaEditando
        ? "Editar registro de inflación"
        : "Nuevo registro de inflación";

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!PuedeVer)
        {
            MensajeError = "Acceso denegado al módulo de Inflación.";
            return;
        }

        if (EstaCargando)
            return;

        MensajeError = string.Empty;
        EstaCargando = true;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ListarInflacionAnualUseCase>();
            var lista = await useCase.EjecutarAsync();
            Registros = new ObservableCollection<InflacionAnualDto>(lista);
            ConstruirGrafico();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar inflación anual: {ObtenerMensajeDetalle(ex)}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private void Nuevo()
    {
        LimpiarFormulario();
    }

    [RelayCommand]
    private void SeleccionarParaEditar()
    {
        if (!PuedeEditar)
            return;

        if (RegistroSeleccionado is null)
        {
            MensajeError = "Seleccione un registro para editar.";
            return;
        }

        EstaEditando = true;
        Anio = RegistroSeleccionado.Anio.ToString();
        PorcentajeInflacion = RegistroSeleccionado.PorcentajeInflacion.ToString("0.####");
        FuenteNombre = RegistroSeleccionado.FuenteNombre;
        TipoFuenteSeleccionado = NormalizarTipoFuente(RegistroSeleccionado.TipoFuente);
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    [RelayCommand]
    private async Task EliminarSeleccionadoAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para eliminar inflación.";
            return;
        }

        if (RegistroSeleccionado is null)
        {
            MensajeError = "Seleccione un registro para eliminar.";
            return;
        }

        var respuesta = MessageBox.Show(
            $"¿Eliminar el registro del año {RegistroSeleccionado.Anio}?",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (respuesta != MessageBoxResult.Yes)
            return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<EliminarInflacionAnualUseCase>();
            await useCase.EjecutarAsync(RegistroSeleccionado.Id, _sesionActual.UsuarioId);

            MensajeExito = "Registro eliminado correctamente.";
            await CargarAsync();
            LimpiarFormulario();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al eliminar registro: {ObtenerMensajeDetalle(ex)}";
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para registrar o editar inflación.";
            return;
        }

        if (EstaGuardando)
            return;

        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        if (!int.TryParse(Anio, out var anio))
        {
            MensajeError = "Ingrese un año válido.";
            return;
        }

        if (!TryParseDecimalFlexible(PorcentajeInflacion, out var porcentaje))
        {
            MensajeError = "Ingrese un porcentaje de inflación válido.";
            return;
        }

        EstaGuardando = true;

        try
        {
            using var scope = _serviceProvider.CreateScope();

            if (EstaEditando)
            {
                if (RegistroSeleccionado is null)
                    throw new InvalidOperationException("No existe un registro seleccionado para editar.");

                var useCaseEditar = scope.ServiceProvider.GetRequiredService<ActualizarInflacionAnualUseCase>();
                await useCaseEditar.EjecutarAsync(new ActualizarInflacionAnualDto
                {
                    Id = RegistroSeleccionado.Id,
                    Anio = anio,
                    PorcentajeInflacion = porcentaje,
                    FuenteNombre = FuenteNombre,
                    TipoFuente = TipoFuenteSeleccionado
                }, _sesionActual.UsuarioId);

                MensajeExito = "Registro de inflación actualizado correctamente.";
            }
            else
            {
                var useCaseCrear = scope.ServiceProvider.GetRequiredService<CrearInflacionAnualUseCase>();
                await useCaseCrear.EjecutarAsync(new CrearInflacionAnualDto
                {
                    Anio = anio,
                    PorcentajeInflacion = porcentaje,
                    FuenteNombre = FuenteNombre,
                    TipoFuente = TipoFuenteSeleccionado
                }, _sesionActual.UsuarioId);

                MensajeExito = "Registro de inflación creado correctamente.";
            }

            await CargarAsync();
            SeleccionarRegistroPorAnio(anio);
            LimpiarFormulario();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Ya existe un registro de inflación para el año", StringComparison.OrdinalIgnoreCase))
        {
            await CargarAsync();
            SeleccionarRegistroPorAnio(anio);
            MensajeError = ex.Message + " Se cargó el registro existente para su edición.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al guardar registro de inflación: {ObtenerMensajeDetalle(ex)}";
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    [RelayCommand]
    private async Task ImportarArchivoAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para importar inflación.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar archivo de inflación",
            Filter = "Excel de inflación (*.xlsx)|*.xlsx"
        };

        if (dialog.ShowDialog() != true)
            return;

        EstaImportandoManual = true;
        MensajeImportacionManual = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ImportarInflacionUseCase>();
            var resultado = await useCase.EjecutarAsync(dialog.FileName, _sesionActual.UsuarioId);
            AplicarResultadoImportacionManual(resultado);
            await CargarAsync();

            if (resultado.FilasCorrectas > 0)
                PestaniaSeleccionada = 0;
        }
        catch (Exception ex)
        {
            MensajeImportacionManual = $"Error al importar archivo manual: {ObtenerMensajeDetalle(ex)}";
        }
        finally
        {
            EstaImportandoManual = false;
        }
    }

    [RelayCommand]
    private async Task LimpiarInflacionAsync()
    {
        if (!PuedeEditar)
        {
            MensajeImportacionManual = "No tiene permiso para limpiar inflación.";
            return;
        }

        var respuesta = MessageBox.Show(
            "Esto eliminará todos los registros de inflación anual y proyectada. ¿Desea continuar?",
            "Confirmar limpieza de inflación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (respuesta != MessageBoxResult.Yes)
            return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<LimpiarInflacionUseCase>();
            var resultado = await useCase.EjecutarAsync(_sesionActual.UsuarioId);

            MensajeImportacionManual = $"Inflación limpiada. Anuales eliminados: {resultado.RegistrosAnualesEliminados}, proyectados eliminados: {resultado.RegistrosProyectadosEliminados}.";
            ResumenImportacionManual = string.Empty;
            ResumenImportacionBce = string.Empty;
            ErroresImportacionManual = [];
            ErroresImportacionBce = [];

            await CargarAsync();
            LimpiarFormulario();
        }
        catch (Exception ex)
        {
            MensajeImportacionManual = $"Error al limpiar inflación: {ObtenerMensajeDetalle(ex)}";
        }
    }

    [RelayCommand]
    private async Task ImportarDesdeBceAsync()
    {
        if (!PuedeEditar)
        {
            MensajeImportacionBce = "No tiene permiso para importar inflación BCE.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Seleccionar archivo BCE",
            Filter = "Archivos BCE (*.xlsx;*.xls;*.html)|*.xlsx;*.xls;*.html|Todos los archivos (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        EstaImportandoBce = true;
        MensajeImportacionBce = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ImportarInflacionBceArchivoUseCase>();
            var resultado = await useCase.EjecutarAsync(new ImportacionBceArchivoSolicitudDto
            {
                RutaArchivo = dialog.FileName,
                TipoSerie = TipoSerieInflacionBce.Annual
            }, _sesionActual.UsuarioId);

            AplicarResultadoImportacionBce(resultado);
            await CargarAsync();

            if (resultado.FilasCorrectas > 0)
                PestaniaSeleccionada = 0;
        }
        catch (Exception ex)
        {
            MensajeImportacionBce = $"Error al importar archivo BCE: {ObtenerMensajeDetalle(ex)}";
        }
        finally
        {
            EstaImportandoBce = false;
        }
    }

    [RelayCommand]
    private async Task GenerarProyeccionAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para generar proyección.";
            return;
        }

        if (!int.TryParse(ProyeccionAnioDesde, out var anioDesde)
            || !int.TryParse(ProyeccionAnioHasta, out var anioHasta))
        {
            MensajeError = "Ingrese un rango válido para la proyección.";
            return;
        }

        EstaProyectando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ProyectarInflacionUseCase>();
            var resultado = await useCase.EjecutarAsync(new ProyeccionInflacionSolicitudDto
            {
                AnioDesde = anioDesde,
                AnioHasta = anioHasta
            }, _sesionActual.UsuarioId);

            ResumenProyeccion = $"Proyección generada. Creados: {resultado.RegistrosCreados}, actualizados: {resultado.RegistrosActualizados}, omitidos: {resultado.RegistrosOmitidos}.";

            if (resultado.RegistrosCreados == 0 && resultado.RegistrosActualizados == 0 && resultado.RegistrosOmitidos > 0)
                MensajeError = "Proyección sin cambios: todos los años del rango ya existen como datos históricos o ajustes manuales.";
            else
                MensajeExito = "Proyección calculada correctamente.";

            await CargarAsync();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al generar proyección: {ObtenerMensajeDetalle(ex)}";
        }
        finally
        {
            EstaProyectando = false;
        }
    }

    partial void OnRegistroSeleccionadoChanged(InflacionAnualDto? value)
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
    }

    private void LimpiarFormulario()
    {
        EstaEditando = false;
        RegistroSeleccionado = null;
        Anio = string.Empty;
        PorcentajeInflacion = string.Empty;
        FuenteNombre = FuenteEstimacion;
        TipoFuenteSeleccionado = FuenteEstimacion;
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    private void SeleccionarRegistroPorAnio(int anio)
    {
        var registro = Registros.FirstOrDefault(x => x.Anio == anio);
        if (registro is null)
            return;

        RegistroSeleccionado = registro;
        EstaEditando = true;
        Anio = registro.Anio.ToString();
        PorcentajeInflacion = registro.PorcentajeInflacion.ToString("0.####");
        FuenteNombre = registro.FuenteNombre;
        TipoFuenteSeleccionado = NormalizarTipoFuente(registro.TipoFuente);
        OnPropertyChanged(nameof(TituloFormulario));
    }

    private static string NormalizarTipoFuente(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return FuenteEstimacion;

        return valor.Trim() switch
        {
            var v when v.Equals("Dato historico", StringComparison.OrdinalIgnoreCase) => "Dato historico",
            var v when v.Equals(FuenteEstimacion, StringComparison.OrdinalIgnoreCase) => FuenteEstimacion,
            var v when v.Equals("Ajuste manual", StringComparison.OrdinalIgnoreCase) => "Ajuste manual",
            _ => FuenteEstimacion
        };
    }

    private void AplicarResultadoImportacionManual(ImportacionInflacionResultadoDto resultado)
    {
        ResumenImportacionManual = $"{resultado.FilasCorrectas} filas cargadas correctamente.";
        ErroresImportacionManual = [];

        if (resultado.FilasConError == 0 && resultado.FilasOmitidas == 0)
        {
            MensajeImportacionManual = "Importación manual completada. Redirigiendo a Modificar inflación.";
            return;
        }

        if (resultado.FilasConError == 0 && resultado.FilasOmitidas > 0)
        {
            MensajeImportacionManual = $"Importación manual completada. {resultado.FilasOmitidas} filas omitidas por año existente.";
            return;
        }

        var primerError = resultado.DetalleErrores.FirstOrDefault()?.Mensaje ?? "Revise el archivo.";
        MensajeImportacionManual = $"Importación manual con errores: {resultado.FilasConError}. {primerError}";
    }

    private void AplicarResultadoImportacionBce(ImportacionInflacionResultadoDto resultado)
    {
        ResumenImportacionBce = $"{resultado.FilasCorrectas} filas cargadas correctamente.";
        ErroresImportacionBce = [];

        if (resultado.FilasConError == 0 && resultado.FilasOmitidas == 0)
        {
            MensajeImportacionBce = "Importación BCE completada. Redirigiendo a Modificar inflación.";
            return;
        }

        if (resultado.FilasConError == 0 && resultado.FilasOmitidas > 0)
        {
            MensajeImportacionBce = $"Importación BCE completada. {resultado.FilasOmitidas} filas omitidas por año existente.";
            return;
        }

        var primerError = resultado.DetalleErrores.FirstOrDefault()?.Mensaje ?? "Revise el archivo BCE descargado.";
        MensajeImportacionBce = $"Importación BCE con errores: {resultado.FilasConError}. {primerError}";
    }

    private void ConstruirGrafico()
    {
        var model = new PlotModel { Title = "Histórico vs Proyección" };
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Año", MajorStep = 1, MinorStep = 1 });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Inflación (%)" });

        var seriesHistorica = Registros
            .Where(x => !x.TipoFuente.Equals(FuenteEstimacion, StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => x.Anio)
            .Select(g => g.OrderByDescending(x => x.Id).First())
            .OrderBy(x => x.Anio)
            .ToList();

        var ultimoHistorico = seriesHistorica.Count > 0 ? seriesHistorica.Max(x => x.Anio) : int.MinValue;

        var seriesProyeccion = Registros
            .Where(x => x.TipoFuente.Equals(FuenteEstimacion, StringComparison.OrdinalIgnoreCase) && x.Anio > ultimoHistorico)
            .GroupBy(x => x.Anio)
            .Select(g => g.OrderByDescending(x => x.Id).First())
            .OrderBy(x => x.Anio)
            .ToList();

        var historico = new LineSeries
        {
            Title = "Histórico",
            Color = OxyColors.SteelBlue,
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = OxyColors.SteelBlue,
            MarkerStroke = OxyColors.SteelBlue
        };

        var proyeccion = new LineSeries
        {
            Title = "Proyección",
            Color = OxyColors.OrangeRed,
            StrokeThickness = 2,
            LineStyle = LineStyle.Dash,
            MarkerType = MarkerType.None
        };

        foreach (var item in seriesHistorica)
        {
            historico.Points.Add(new DataPoint(item.Anio, (double)item.PorcentajeInflacion));
        }

        foreach (var item in seriesProyeccion)
        {
            proyeccion.Points.Add(new DataPoint(item.Anio, (double)item.PorcentajeInflacion));
        }

        if (historico.Points.Count > 0) model.Series.Add(historico);
        if (proyeccion.Points.Count > 0) model.Series.Add(proyeccion);

        GraficoInflacion = model;
    }

    private static bool TryParseDecimalFlexible(string valor, out decimal resultado)
    {
        var limpio = valor.Replace("%", string.Empty).Trim();
        return decimal.TryParse(limpio.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out resultado)
               || decimal.TryParse(limpio, NumberStyles.Any, new CultureInfo("es-EC"), out resultado);
    }

    private static string ObtenerMensajeDetalle(Exception ex)
        => ex.InnerException?.Message ?? ex.Message;
}
