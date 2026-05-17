using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;

public sealed class ActivoFijoDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public CategoriaActivoFijo Categoria { get; init; }
    public string CategoriaNombre { get; init; } = string.Empty;
    public decimal Cantidad { get; init; }
    public decimal CantidadBase { get; init; }
    public string UnidadMedida { get; init; } = "UNI";
    public decimal ValorUnitario { get; init; }
    public decimal ValorTotal { get; init; }
    public int VidaUtilAnios { get; init; }
    public decimal PorcentajeResidual { get; init; }
    public DateTimeOffset FechaAdquisicion { get; init; }
    public TipoCalculoCantidad TipoCalculoCantidad { get; init; }
    public string TipoCalculoNombre { get; init; } = string.Empty;
    public decimal FactorMultiplicador { get; init; } = 1m;
    public decimal OffsetCantidad { get; init; }
    public bool UsaCantidadCalculada { get; init; }

    public string ValorUnitarioDisplay => ValorUnitario.ToString("C2");
    public string ValorTotalDisplay => ValorTotal.ToString("C2");
    public string PorcentajeResidualDisplay => (PorcentajeResidual * 100m).ToString("0.##") + "%";
}

public sealed class CrearActivoFijoDto
{
    public int CarreraId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public CategoriaActivoFijo Categoria { get; init; }
    public decimal Cantidad { get; init; }
    public string UnidadMedida { get; init; } = "UNI";
    public decimal ValorUnitario { get; init; }
    public int? VidaUtilAnios { get; init; }
    public decimal? PorcentajeResidual { get; init; }
    public DateTimeOffset? FechaAdquisicion { get; init; }
    public TipoCalculoCantidad TipoCalculoCantidad { get; init; } = TipoCalculoCantidad.Manual;
    public decimal FactorMultiplicador { get; init; } = 1m;
    public decimal OffsetCantidad { get; init; }
}

public sealed class ActualizarActivoFijoDto
{
    public int Id { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public CategoriaActivoFijo Categoria { get; init; }
    public decimal Cantidad { get; init; }
    public string UnidadMedida { get; init; } = "UNI";
    public decimal ValorUnitario { get; init; }
    public int VidaUtilAnios { get; init; }
    public decimal PorcentajeResidual { get; init; }
    public DateTimeOffset FechaAdquisicion { get; init; }
    public TipoCalculoCantidad TipoCalculoCantidad { get; init; } = TipoCalculoCantidad.Manual;
    public decimal FactorMultiplicador { get; init; } = 1m;
    public decimal OffsetCantidad { get; init; }
}

/// <summary>Replica la fila TOTAL del bloque 1 de "3 Recursos fisicos" (Excel E62).</summary>
public sealed class TotalCategoriaActivosDto
{
    public CategoriaActivoFijo Categoria { get; init; }
    public string CategoriaNombre { get; init; } = string.Empty;
    public int CantidadItems { get; init; }
    public decimal SubtotalValorTotal { get; init; }
}

public sealed class TotalesActivosFijosDto
{
    public IReadOnlyList<TotalCategoriaActivosDto> PorCategoria { get; init; } = [];
    public decimal TotalGeneral { get; init; }
}
