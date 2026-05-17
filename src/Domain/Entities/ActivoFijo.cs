using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Activo fijo de inversion inicial (KAN-24). Replica el bloque 1 de la hoja Excel
/// "3 Recursos fisicos": DESCRIPCION | CANTIDAD | UNIDAD | VALOR UNITARIO | VALOR TOTAL.
/// ValorTotal siempre se calcula en dominio (RN-95), nunca se acepta del cliente.
/// </summary>
public sealed class ActivoFijo : EntidadDominioBase
{
    private ActivoFijo()
    {
    }

    public ActivoFijo(
        int carreraId,
        string descripcion,
        CategoriaActivoFijo categoria,
        decimal cantidad,
        string unidadMedida,
        decimal valorUnitario,
        int? vidaUtilAnios = null,
        decimal? porcentajeResidual = null,
        DateTimeOffset? fechaAdquisicion = null)
    {
        CambiarCarrera(carreraId);
        CambiarDescripcion(descripcion);
        CambiarCategoria(categoria);
        CambiarCantidad(cantidad);
        CambiarUnidadMedida(unidadMedida);
        CambiarValorUnitario(valorUnitario);
        CambiarVidaUtil(vidaUtilAnios ?? VidaUtilPorDefecto(categoria));
        CambiarPorcentajeResidual(porcentajeResidual ?? PorcentajeResidualPorDefecto);
        FechaAdquisicion = fechaAdquisicion ?? DateTimeOffset.UtcNow;
    }

    public const decimal PorcentajeResidualPorDefecto = 0.05m;

    public int CarreraId { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public CategoriaActivoFijo Categoria { get; private set; }
    public decimal Cantidad { get; private set; }
    public string UnidadMedida { get; private set; } = "UNI";
    public decimal ValorUnitario { get; private set; }
    public int VidaUtilAnios { get; private set; }
    public decimal PorcentajeResidual { get; private set; } = PorcentajeResidualPorDefecto;
    public DateTimeOffset FechaAdquisicion { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>VALOR TOTAL = CANTIDAD x VALOR UNITARIO (Excel columna E, RN-95).</summary>
    public decimal ValorTotal => decimal.Round(Cantidad * ValorUnitario, 2);

    /// <summary>Vida util minima legal (anios) por categoria — LORTI / NIC 16 (RN-98).</summary>
    public static int VidaUtilPorDefecto(CategoriaActivoFijo categoria) => categoria switch
    {
        CategoriaActivoFijo.MueblesEnseres => 10,
        CategoriaActivoFijo.LaboratoriosEquipos => 10,
        CategoriaActivoFijo.EquipoComputo => 3,
        CategoriaActivoFijo.EquipoOficina => 1,
        _ => 10
    };

    public void CambiarCarrera(int carreraId)
    {
        CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");
    }

    public void CambiarDescripcion(string descripcion)
    {
        Descripcion = GuardiaDominio.Requerido(descripcion, "Descripcion del activo", 200);
    }

    public void CambiarCategoria(CategoriaActivoFijo categoria)
    {
        if (!Enum.IsDefined(categoria))
        {
            throw new DominioException("Categoria de activo fijo no valida.");
        }

        Categoria = categoria;
    }

    public void CambiarCantidad(decimal cantidad)
    {
        Cantidad = GuardiaDominio.DecimalNoNegativo(cantidad, "Cantidad", 4);
    }

    public void CambiarUnidadMedida(string unidadMedida)
    {
        UnidadMedida = string.IsNullOrWhiteSpace(unidadMedida)
            ? "UNI"
            : GuardiaDominio.Requerido(unidadMedida, "Unidad de medida", 20);
    }

    public void CambiarValorUnitario(decimal valorUnitario)
    {
        ValorUnitario = GuardiaDominio.DecimalNoNegativo(valorUnitario, "Valor unitario", 2);
    }

    public void CambiarVidaUtil(int vidaUtilAnios)
    {
        VidaUtilAnios = GuardiaDominio.EnteroPositivo(vidaUtilAnios, "Vida util (anios)");
    }

    public void CambiarPorcentajeResidual(decimal porcentajeResidual)
    {
        if (porcentajeResidual < 0m || porcentajeResidual >= 1m)
        {
            throw new DominioException("Porcentaje residual debe estar entre 0 y 1 (ej. 0.05 = 5%).");
        }

        PorcentajeResidual = decimal.Round(porcentajeResidual, 4);
    }

    public void CambiarFechaAdquisicion(DateTimeOffset fecha)
    {
        FechaAdquisicion = fecha;
    }

    public void RehidratarFechaAdquisicion(DateTimeOffset fecha)
    {
        FechaAdquisicion = fecha;
    }
}
