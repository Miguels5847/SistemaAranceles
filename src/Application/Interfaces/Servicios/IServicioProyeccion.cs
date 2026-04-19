namespace SistemaAranceles.Application.Interfaces.Servicios;

public interface IServicioProyeccion
{
    IReadOnlyList<(int anio, decimal porcentaje)> ProyectarRegresionLineal(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        int anioDesde,
        int anioHasta);

    IReadOnlyList<(int anio, decimal porcentaje)> ProyectarPromedioSuave(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        int anioDesde,
        int anioHasta);

    decimal ObtenerInflacionFallback(
        IReadOnlyList<(int anio, decimal porcentaje)> historicos,
        decimal porcentajeProyectado);
}
