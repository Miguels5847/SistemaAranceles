using System.Globalization;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.DTOs.InversionInicial;

public sealed class CategoriaActivoFijoInversionDto
{
    public CategoriaActivoFijo Categoria { get; init; }
    public string CategoriaNombre { get; init; } = string.Empty;
    public decimal Valor { get; init; }

    public string ValorDisplay => FormatoInversionInicial.Moneda(Valor);
}

public sealed class InversionInicialTotalDto
{
    public decimal ActivosDiferidos { get; init; }
    public IReadOnlyList<CategoriaActivoFijoInversionDto> CategoriasActivosFijos { get; init; } = [];
    public decimal MueblesEnseres { get; init; }
    public decimal LaboratoriosEquipos { get; init; }
    public decimal EquipoComputo { get; init; }
    public decimal EquipoOficina { get; init; }
    public decimal SubtotalActivosFijos { get; init; }
    public decimal CapitalTrabajoDosM { get; init; }
    public int MesesCapitalTrabajo { get; init; } = DatosInstitucionales.MesesCapitalTrabajoPorDefecto;
    public decimal PorcentajeImprevistosInversion { get; init; } = DatosInstitucionales.PorcentajeImprevistosInversionPorDefecto;
    public decimal Imprevistos { get; init; }
    public decimal TotalInversion { get; init; }
    public decimal TotalActivos => ActivosDiferidos + SubtotalActivosFijos;

    public string ActivosDiferidosDisplay => FormatoInversionInicial.Moneda(ActivosDiferidos);
    public string MueblesDisplay => FormatoInversionInicial.Moneda(MueblesEnseres);
    public string LaboratoriosDisplay => FormatoInversionInicial.Moneda(LaboratoriosEquipos);
    public string EquipoComputoDisplay => FormatoInversionInicial.Moneda(EquipoComputo);
    public string EquipoOficinaDisplay => FormatoInversionInicial.Moneda(EquipoOficina);
    public string SubtotalActivosFijosDisplay => FormatoInversionInicial.Moneda(SubtotalActivosFijos);
    public string TotalActivosDisplay => FormatoInversionInicial.Moneda(TotalActivos);
    public string CapitalTrabajoDisplay => FormatoInversionInicial.Moneda(CapitalTrabajoDosM);
    public string ImprevistosDisplay => FormatoInversionInicial.Moneda(Imprevistos);
    public string TotalInversionDisplay => FormatoInversionInicial.Moneda(TotalInversion);
    public string PorcentajeImprevistosInversionDisplay => PorcentajeImprevistosInversion.ToString("0.####", CultureInfo.InvariantCulture) + "%";
    public string MesesCapitalTrabajoDisplay => MesesCapitalTrabajo.ToString(CultureInfo.InvariantCulture);
}

internal static class FormatoInversionInicial
{
    private static readonly CultureInfo Cultura = new("es-EC");

    public static string Moneda(decimal valor)
    {
        if (valor == 0m)
            return "$ -";

        var formato = decimal.Truncate(valor) == valor ? "N0" : "N2";
        return $"$ {valor.ToString(formato, Cultura)}";
    }
}
