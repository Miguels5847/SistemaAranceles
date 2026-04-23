namespace SistemaAranceles.Application.Interfaces.Servicios;

public interface IServicioProyeccion
{
    IReadOnlyList<(int anio, decimal porcentaje)> ProyectarRegresionLineal(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        int anioDesde,
        int anioHasta,
        decimal pisoMinimoProyeccion,
        decimal techoMaximoProyeccion);

    IReadOnlyList<(int anio, decimal porcentaje)> ProyectarPromedioSuave(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        int anioDesde,
        int anioHasta,
        decimal pisoMinimoProyeccion,
        int ventanaAniosRecientes,
        decimal valorObjetivoConvergencia,
        decimal factorConvergenciaAnual,
        decimal techoMaximoProyeccion);

    decimal ObtenerInflacionFallback(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        decimal porcentajeProyectado);
}
