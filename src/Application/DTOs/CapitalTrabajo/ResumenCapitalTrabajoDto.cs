namespace SistemaAranceles.Application.DTOs.CapitalTrabajo;

public sealed class ResumenCapitalTrabajoDto
{
    public decimal SubtotalCargos { get; init; }
    public decimal SubtotalMateriales { get; init; }
    public decimal SubtotalAseo { get; init; }
    public decimal SubtotalAccesorios { get; init; }
    public int MesesCapitalTrabajo { get; init; } = 2;
    public decimal TotalMensual => SubtotalCargos + SubtotalMateriales + SubtotalAseo + SubtotalAccesorios;
    public decimal TotalCapitalTrabajo => decimal.Round(TotalMensual * MesesCapitalTrabajo, 2);
    public decimal TotalDosMeses => TotalCapitalTrabajo;

    public string SubtotalCargosDisplay => FormatoCapitalTrabajo.Moneda(SubtotalCargos);
    public string SubtotalMaterialesDisplay => FormatoCapitalTrabajo.Moneda(SubtotalMateriales);
    public string SubtotalAseoDisplay => FormatoCapitalTrabajo.Moneda(SubtotalAseo);
    public string SubtotalAccesoriosDisplay => FormatoCapitalTrabajo.Moneda(SubtotalAccesorios);
    public string TotalMensualDisplay => FormatoCapitalTrabajo.Moneda(TotalMensual);
    public string TotalCapitalTrabajoDisplay => FormatoCapitalTrabajo.Moneda(TotalCapitalTrabajo);
    public string TotalDosMesesDisplay => TotalCapitalTrabajoDisplay;
}

public sealed class FilaGastoServicioAdministracionDto
{
    public decimal Cantidad { get; init; }
    public string Concepto { get; init; } = string.Empty;
    public decimal ValorUnitario { get; init; }
    public decimal ValorMensual { get; init; }

    public string CantidadDisplay => FormatoCapitalTrabajo.Cantidad(Cantidad);
    public string ValorUnitarioDisplay => FormatoCapitalTrabajo.Moneda(ValorUnitario);
    public string ValorMensualDisplay => FormatoCapitalTrabajo.Moneda(ValorMensual);
}

public sealed class ItemCapitalTrabajoDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string CategoriaNombre { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public decimal Cantidad { get; init; }
    public decimal ValorUnitario { get; init; }
    public decimal ValorMensual => decimal.Round(Cantidad * ValorUnitario, 2);

    public string CantidadDisplay => FormatoCapitalTrabajo.Cantidad(Cantidad);
    public string ValorUnitarioDisplay => FormatoCapitalTrabajo.Moneda(ValorUnitario);
    public string ValorMensualDisplay => FormatoCapitalTrabajo.Moneda(ValorMensual);
}

public sealed class GuardarItemCapitalTrabajoDto
{
    public int? Id { get; init; }
    public int CarreraId { get; init; }
    public string CategoriaNombre { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public decimal Cantidad { get; init; }
    public decimal ValorUnitario { get; init; }
}

public sealed class CapitalTrabajoPorCarreraDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public int? PeriodoBaseId { get; init; }
    public string PeriodoBaseDisplay { get; init; } = "Sin periodo base";
    public int MesesCapitalTrabajo { get; init; } = 2;

    public IReadOnlyList<FilaGastoServicioAdministracionDto> GastosServicioAdministracion { get; init; } = [];
    public IReadOnlyList<ItemCapitalTrabajoDto> MaterialesSuministros { get; init; } = [];
    public IReadOnlyList<ItemCapitalTrabajoDto> AseoLimpieza { get; init; } = [];
    public IReadOnlyList<ItemCapitalTrabajoDto> AccesoriosMateriales { get; init; } = [];

    public decimal SubtotalGastosServicioAdministracion => GastosServicioAdministracion.Sum(x => x.ValorMensual);
    public decimal SubtotalMateriales => MaterialesSuministros.Sum(x => x.ValorMensual);
    public decimal SubtotalAseo => AseoLimpieza.Sum(x => x.ValorMensual);
    public decimal SubtotalAccesorios => AccesoriosMateriales.Sum(x => x.ValorMensual);

    public ResumenCapitalTrabajoDto Resumen => new()
    {
        SubtotalCargos = decimal.Round(SubtotalGastosServicioAdministracion, 2),
        SubtotalMateriales = decimal.Round(SubtotalMateriales, 2),
        SubtotalAseo = decimal.Round(SubtotalAseo, 2),
        SubtotalAccesorios = decimal.Round(SubtotalAccesorios, 2),
        MesesCapitalTrabajo = MesesCapitalTrabajo
    };
}

internal static class FormatoCapitalTrabajo
{
    private static readonly System.Globalization.CultureInfo Cultura = new("es-EC");

    public static string Moneda(decimal valor)
    {
        if (valor == 0m)
            return "$ -";

        var formato = decimal.Truncate(valor) == valor ? "N0" : "N2";
        return $"$ {valor.ToString(formato, Cultura)}";
    }

    public static string Cantidad(decimal valor)
    {
        if (valor == 0m)
            return "-";

        return valor.ToString("0.####", Cultura);
    }
}
