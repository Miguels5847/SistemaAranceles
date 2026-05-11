using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class OverrideHorasPeriodo : EntidadDominioBase
{
    private OverrideHorasPeriodo()
    {
    }

    public OverrideHorasPeriodo(int proyeccionId, int periodo, decimal? horasDocencia, decimal? horasPractica)
    {
        CambiarProyeccion(proyeccionId);
        CambiarPeriodo(periodo);
        CambiarHorasDocencia(horasDocencia);
        CambiarHorasPractica(horasPractica);
    }

    public int ProyeccionId { get; private set; }
    public int Periodo { get; private set; }
    public decimal? HorasDocencia { get; private set; }
    public decimal? HorasPractica { get; private set; }

    public void CambiarProyeccion(int proyeccionId)
    {
        ProyeccionId = GuardiaDominio.EnteroPositivo(proyeccionId, "Proyeccion");
    }

    public void CambiarPeriodo(int periodo)
    {
        if (periodo is < 1 or > 20)
            throw new DominioException("Periodo del override debe estar entre 1 y 20.");
        Periodo = periodo;
    }

    public void CambiarHorasDocencia(decimal? horas)
    {
        if (horas is < 0)
            throw new DominioException("HorasDocencia no puede ser negativa.");
        HorasDocencia = horas is null ? null : decimal.Round(horas.Value, 2);
    }

    public void CambiarHorasPractica(decimal? horas)
    {
        if (horas is < 0)
            throw new DominioException("HorasPractica no puede ser negativa.");
        HorasPractica = horas is null ? null : decimal.Round(horas.Value, 2);
    }
}
