using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Plantilla institucional global de activo fijo (KAN-24). Solo el administrador la edita.
/// Al crear una carrera se siembra una copia editable como <see cref="ActivoFijo"/>.
/// </summary>
public sealed class CatalogoActivoBase : EntidadDominioBase
{
    private CatalogoActivoBase()
    {
    }

    public CatalogoActivoBase(
        string descripcion,
        CategoriaActivoFijo categoria,
        TipoCalculoCantidad tipoCalculoCantidad,
        decimal cantidadDefault,
        string unidadMedida,
        decimal valorUnitario,
        decimal factorMultiplicador,
        decimal offsetCantidad,
        int vidaUtilAnios,
        decimal porcentajeResidual)
    {
        CambiarDescripcion(descripcion);
        CambiarCategoria(categoria);
        CambiarCalculoCantidad(tipoCalculoCantidad, factorMultiplicador, offsetCantidad);
        CambiarCantidadDefault(cantidadDefault);
        CambiarUnidadMedida(unidadMedida);
        CambiarValorUnitario(valorUnitario);
        CambiarVidaUtil(vidaUtilAnios);
        CambiarPorcentajeResidual(porcentajeResidual);
    }

    public string Descripcion { get; private set; } = string.Empty;
    public CategoriaActivoFijo Categoria { get; private set; }
    public TipoCalculoCantidad TipoCalculoCantidad { get; private set; } = TipoCalculoCantidad.Manual;
    public decimal CantidadDefault { get; private set; }
    public string UnidadMedida { get; private set; } = "UNI";
    public decimal ValorUnitario { get; private set; }
    public decimal FactorMultiplicador { get; private set; } = 1m;
    public decimal OffsetCantidad { get; private set; }
    public int VidaUtilAnios { get; private set; }
    public decimal PorcentajeResidual { get; private set; } = ActivoFijo.PorcentajeResidualPorDefecto;

    public void CambiarDescripcion(string descripcion)
        => Descripcion = GuardiaDominio.Requerido(descripcion, "Descripcion del catalogo", 200);

    public void CambiarCategoria(CategoriaActivoFijo categoria)
    {
        if (!Enum.IsDefined(categoria))
        {
            throw new DominioException("Categoria de activo fijo no valida.");
        }

        Categoria = categoria;
    }

    public void CambiarCalculoCantidad(TipoCalculoCantidad tipo, decimal factorMultiplicador, decimal offsetCantidad)
    {
        if (!Enum.IsDefined(tipo))
        {
            throw new DominioException("Tipo de calculo de cantidad no valido.");
        }

        TipoCalculoCantidad = tipo;
        FactorMultiplicador = GuardiaDominio.DecimalNoNegativo(factorMultiplicador, "Factor multiplicador", 4);
        OffsetCantidad = GuardiaDominio.DecimalNoNegativo(offsetCantidad, "Offset de cantidad", 4);
    }

    public void CambiarCantidadDefault(decimal cantidad)
        => CantidadDefault = GuardiaDominio.DecimalNoNegativo(cantidad, "Cantidad default", 4);

    public void CambiarUnidadMedida(string unidadMedida)
        => UnidadMedida = string.IsNullOrWhiteSpace(unidadMedida)
            ? "UNI"
            : GuardiaDominio.Requerido(unidadMedida, "Unidad de medida", 20);

    public void CambiarValorUnitario(decimal valorUnitario)
        => ValorUnitario = GuardiaDominio.DecimalNoNegativo(valorUnitario, "Valor unitario", 2);

    public void CambiarVidaUtil(int vidaUtilAnios)
        => VidaUtilAnios = GuardiaDominio.EnteroPositivo(vidaUtilAnios, "Vida util (anios)");

    public void CambiarPorcentajeResidual(decimal porcentajeResidual)
    {
        if (porcentajeResidual < 0m || porcentajeResidual >= 1m)
        {
            throw new DominioException("Porcentaje residual debe estar entre 0 y 1 (ej. 0.05 = 5%).");
        }

        PorcentajeResidual = decimal.Round(porcentajeResidual, 4);
    }

    /// <summary>Crea el ActivoFijo editable para una carrera a partir de esta plantilla.</summary>
    public ActivoFijo CrearActivoParaCarrera(int carreraId) => new(
        carreraId,
        Descripcion,
        Categoria,
        CantidadDefault,
        UnidadMedida,
        ValorUnitario,
        VidaUtilAnios,
        PorcentajeResidual,
        fechaAdquisicion: null,
        TipoCalculoCantidad,
        FactorMultiplicador,
        OffsetCantidad);
}
