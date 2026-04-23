using FluentValidation;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Retencion.Politicas;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class ObtenerValoresSugeridosParaEscenarioUseCase(
    IRepositorioConfiguracionRetencion repositorioConfiguracion)
{
    public async Task<ValoresSugeridosDto> EjecutarAsync(
        int carreraId,
        string escenario,
        CancellationToken cancellationToken = default)
    {
        if (escenario == "Histórico")
            return new ValoresSugeridosDto();

        var historico = await repositorioConfiguracion.ObtenerActivoPorCarreraYEscenarioNombreAsync(carreraId, "Histórico", cancellationToken)
            ?? throw new ValidationException("No existe configuración histórica para la carrera seleccionada.");

        return AjustesPorEscenario.CalcularDesdeHistorico(historico, escenario);
    }
}
