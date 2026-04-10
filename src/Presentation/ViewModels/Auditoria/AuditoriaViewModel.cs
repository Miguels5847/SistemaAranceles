using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Auditoria;
using SistemaAranceles.Application.UseCases.Auditoria;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.Auditoria;

public sealed partial class AuditoriaViewModel(
    IServiceProvider serviceProvider,
    SesionActual sesionActual) : ObservableObject
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly SesionActual _sesionActual = sesionActual;

    public ObservableCollection<AuditoriaLogItemDto> Registros { get; } = [];

    [ObservableProperty]
    private string _usuarioFiltro = string.Empty;

    [ObservableProperty]
    private DateTime? _fechaDesde;

    [ObservableProperty]
    private DateTime? _fechaHasta;

    [ObservableProperty]
    private string _moduloFiltro = "Todos";

    [ObservableProperty]
    private string _accionFiltro = "Todos";

    [ObservableProperty]
    private int _numeroPagina = 1;

    [ObservableProperty]
    private int _tamanoPagina = 25;

    [ObservableProperty]
    private int _totalPaginas;

    [ObservableProperty]
    private int _totalRegistros;

    [ObservableProperty]
    private bool _estaCargando;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    public IReadOnlyList<string> ModulosDisponibles { get; } =
    [
        "Todos",
        "Autenticacion",
        "Usuarios",
        "Auditoria"
    ];

    public IReadOnlyList<string> AccionesDisponibles { get; } =
    [
        "Todos",
        "LOGIN_EXITOSO",
        "LOGOUT",
        "CREAR",
        "ACTUALIZAR",
        "ELIMINAR",
        "CAMBIO_ROL",
        "CAMBIO_PERMISOS",
        "VER"
    ];

    public bool PuedeConsultarAuditoria => _sesionActual.EsAdministrador && _sesionActual.TienePermiso("AUD.VER");

    public string ResumenPaginacion => TotalRegistros == 0
        ? "Sin resultados"
        : $"Página {NumeroPagina} de {TotalPaginas} - {TotalRegistros} registros";

    public bool PuedeIrPaginaAnterior => NumeroPagina > 1 && !EstaCargando;

    public bool PuedeIrPaginaSiguiente => NumeroPagina < TotalPaginas && !EstaCargando;

    [RelayCommand]
    private async Task BuscarAsync()
    {
        if (EstaCargando)
            return;

        var swTotal = Stopwatch.StartNew();
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] AuditoriaViewModel: inicio BuscarAsync. UsuarioFiltro='{UsuarioFiltro}', FechaDesde='{FechaDesde:O}', FechaHasta='{FechaHasta:O}', ModuloFiltro='{ModuloFiltro}', AccionFiltro='{AccionFiltro}', Pagina={NumeroPagina}, Tamano={TamanoPagina}.");

        MensajeError = string.Empty;

        if (!PuedeConsultarAuditoria)
        {
            MensajeError = "Acceso denegado. Solo el administrador puede consultar la auditoría.";
            Registros.Clear();
            TotalRegistros = 0;
            TotalPaginas = 0;
            OnPropiedadesPaginacionCambian();
            return;
        }

        int? usuarioId = null;
        if (!string.IsNullOrWhiteSpace(UsuarioFiltro))
        {
            if (!int.TryParse(UsuarioFiltro.Trim(), out var parsedUsuarioId) || parsedUsuarioId <= 0)
            {
                MensajeError = "El filtro de usuario debe ser un ID numérico válido.";
                return;
            }

            usuarioId = parsedUsuarioId;
        }

        if (FechaDesde.HasValue && FechaHasta.HasValue && FechaDesde.Value.Date > FechaHasta.Value.Date)
        {
            MensajeError = "La fecha desde no puede ser mayor a la fecha hasta.";
            return;
        }

        EstaCargando = true;
        try
        {
            var swUseCase = Stopwatch.StartNew();
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ConsultarAuditoriaUseCase>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            Trace.TraceInformation($"[{DateTime.UtcNow:O}] AuditoriaViewModel: invocando ConsultarAuditoriaUseCase.");
            var resultado = await useCase.EjecutarAsync(new AuditoriaFiltroDto
            {
                UsuarioId = usuarioId,
                FechaDesdeUtc = FechaDesde?.Date,
                FechaHastaUtc = FechaHasta?.Date.AddDays(1).AddTicks(-1),
                ModuloNombre = ModuloFiltro == "Todos" ? string.Empty : ModuloFiltro,
                AccionNombre = AccionFiltro == "Todos" ? string.Empty : AccionFiltro,
                NumeroPagina = NumeroPagina,
                TamanoPagina = TamanoPagina
            }, cts.Token);
            swUseCase.Stop();
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] AuditoriaViewModel: ConsultarAuditoriaUseCase finalizado. total_ms={swUseCase.ElapsedMilliseconds}, total_registros={resultado.TotalRegistros}, items={resultado.Items.Count}, total_paginas={resultado.TotalPaginas}.");

            Registros.Clear();
            foreach (var item in resultado.Items)
            {
                Registros.Add(item);
            }

            TotalRegistros = resultado.TotalRegistros;
            TotalPaginas = resultado.TotalPaginas;

            if (TotalPaginas == 0)
            {
                NumeroPagina = 1;
            }
            else if (NumeroPagina > TotalPaginas)
            {
                NumeroPagina = TotalPaginas;
            }

            OnPropiedadesPaginacionCambian();
            swTotal.Stop();
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] AuditoriaViewModel: fin BuscarAsync exitoso. total_ms={swTotal.ElapsedMilliseconds}.");
        }
        catch (OperationCanceledException)
        {
            Trace.TraceError($"[{DateTime.UtcNow:O}] AuditoriaViewModel: timeout en BuscarAsync (60s).");
            MensajeError = "La consulta de auditoría tardó demasiado (timeout de 60s).";
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[{DateTime.UtcNow:O}] AuditoriaViewModel: error en BuscarAsync -> {ex.GetBaseException().Message}.");
            MensajeError = $"Error al consultar auditoría: {ex.Message}";
        }
        finally
        {
            EstaCargando = false;
            OnPropiedadesPaginacionCambian();
        }
    }

    [RelayCommand]
    private async Task PaginaSiguienteAsync()
    {
        if (!PuedeIrPaginaSiguiente)
            return;

        NumeroPagina++;
        await BuscarAsync();
    }

    [RelayCommand]
    private async Task PaginaAnteriorAsync()
    {
        if (!PuedeIrPaginaAnterior)
            return;

        NumeroPagina--;
        await BuscarAsync();
    }

    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        UsuarioFiltro = string.Empty;
        FechaDesde = null;
        FechaHasta = null;
        ModuloFiltro = "Todos";
        AccionFiltro = "Todos";
        NumeroPagina = 1;
        await BuscarAsync();
    }

    partial void OnNumeroPaginaChanged(int value) => OnPropiedadesPaginacionCambian();
    partial void OnTotalPaginasChanged(int value) => OnPropiedadesPaginacionCambian();
    partial void OnTotalRegistrosChanged(int value) => OnPropiedadesPaginacionCambian();
    partial void OnEstaCargandoChanged(bool value) => OnPropiedadesPaginacionCambian();

    private void OnPropiedadesPaginacionCambian()
    {
        OnPropertyChanged(nameof(PuedeIrPaginaAnterior));
        OnPropertyChanged(nameof(PuedeIrPaginaSiguiente));
        OnPropertyChanged(nameof(ResumenPaginacion));
    }
}
