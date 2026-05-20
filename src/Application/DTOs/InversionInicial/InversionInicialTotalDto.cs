namespace SistemaAranceles.Application.DTOs.InversionInicial;

public sealed class InversionInicialTotalDto
{
    public decimal ActivosDiferidos { get; init; }
    public decimal MueblesEnseres { get; init; }
    public decimal LaboratoriosEquipos { get; init; }
    public decimal EquipoComputo { get; init; }
    public decimal EquipoOficina { get; init; }
    public decimal SubtotalActivosFijos { get; init; }
    public decimal CapitalTrabajoDosM { get; init; }
    public decimal Imprevistos { get; init; }
    public decimal TotalInversion { get; init; }

    public string ActivosDiferidosDisplay => Fmt(ActivosDiferidos);
    public string MueblesDisplay => Fmt(MueblesEnseres);
    public string LaboratoriosDisplay => Fmt(LaboratoriosEquipos);
    public string EquipoComputoDisplay => Fmt(EquipoComputo);
    public string EquipoOficinaDisplay => Fmt(EquipoOficina);
    public string SubtotalActivosFijosDisplay => Fmt(SubtotalActivosFijos);
    public string CapitalTrabajoDisplay => Fmt(CapitalTrabajoDosM);
    public string ImprevistosDisplay => Fmt(Imprevistos);
    public string TotalInversionDisplay => TotalInversion == 0m ? "$ -" : TotalInversion.ToString("N2");

    private static string Fmt(decimal v) => v == 0m ? "$ -" : v.ToString("N2");
}
