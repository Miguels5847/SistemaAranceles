namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class EscenarioDemandaIngresosDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public bool EsPredeterminado { get; init; }
    public bool TieneProyeccion { get; init; }
    public int? ProyeccionEstudiantesId { get; init; }

    public string EstadoProyeccion => TieneProyeccion ? "con proyeccion" : "sin proyeccion";
    public string NombreDisplay => $"{Nombre} ({EstadoProyeccion})";
}

public sealed class ItemMaterialRatioOpcionDto
{
    public int? Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Categoria { get; init; } = string.Empty;
    public decimal PrecioUnitario { get; init; }
    public bool EsSinItem => Id is null;

    public string PrecioDisplay => $"$ {PrecioUnitario:N2}";
    public string NombreDisplay => EsSinItem
        ? "Sin item vinculado (precio 0)"
        : $"{Nombre} | {Categoria} | {PrecioDisplay}";
}
