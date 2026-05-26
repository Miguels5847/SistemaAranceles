namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class ResumenDemandaIngresosDto
{
    public decimal TotalIngresoBruto { get; init; }
    public decimal TotalBecas { get; init; }
    public decimal TotalIngresoNeto { get; init; }
    public decimal TotalPresupuestosAnualProrrateado { get; init; }
    public decimal TotalPresupuestosPorSemestre { get; init; }
    public decimal TotalMaterialesMonetarios { get; init; }
    public decimal TotalCantidadMateriales { get; init; }
    public IReadOnlyList<string> Advertencias { get; init; } = [];

    public bool TieneAdvertencias => Advertencias.Count > 0;
    public string TotalIngresoBrutoDisplay => Moneda(TotalIngresoBruto);
    public string TotalBecasDisplay => Moneda(TotalBecas);
    public string TotalIngresoNetoDisplay => Moneda(TotalIngresoNeto);
    public string TotalPresupuestosAnualDisplay => Moneda(TotalPresupuestosAnualProrrateado);
    public string TotalPresupuestosSemestreDisplay => Moneda(TotalPresupuestosPorSemestre);
    public string TotalMaterialesMonetariosDisplay => Moneda(TotalMaterialesMonetarios);
    public string TotalCantidadMaterialesDisplay => TotalCantidadMateriales.ToString("N2");
    public string AdvertenciasTexto => TieneAdvertencias
        ? string.Join(Environment.NewLine, Advertencias)
        : "Sin advertencias activas.";

    private static string Moneda(decimal valor) => $"$ {valor:N2}";
}
