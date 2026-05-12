using SistemaAranceles.Application.DTOs.Estudiantes;

namespace SistemaAranceles.Presentation.State;

public sealed class ConsolidadoEstudiantesActualState
{
    public int CarreraId { get; private set; }
    public int EscenarioId { get; private set; }
    public ProyeccionConsolidadaDto? Detalle { get; private set; }

    public void Establecer(int carreraId, int escenarioId, ProyeccionConsolidadaDto? detalle)
    {
        CarreraId = carreraId;
        EscenarioId = escenarioId;
        Detalle = detalle;
    }

    public bool CoincideCon(int carreraId, int escenarioId)
        => Detalle is not null && CarreraId == carreraId && EscenarioId == escenarioId;
}