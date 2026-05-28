namespace SistemaAranceles.Domain.Enums;

public enum UnidadRatioMaterial
{
    PorEstudiante = 0,
    PorEstudianteMes = 1,
    FijoPeriodo = 2
}

public static class UnidadRatioMaterialExtensiones
{
    public const string PorEstudianteText = "por_estudiante";
    public const string PorEstudianteMesText = "por_estudiante_mes";
    public const string FijoPeriodoText = "fijo_periodo";

    public static string ToBdText(this UnidadRatioMaterial unidad) => unidad switch
    {
        UnidadRatioMaterial.PorEstudianteMes => PorEstudianteMesText,
        UnidadRatioMaterial.FijoPeriodo => FijoPeriodoText,
        _ => PorEstudianteText
    };

    public static UnidadRatioMaterial FromBdText(string? texto) => texto switch
    {
        PorEstudianteMesText => UnidadRatioMaterial.PorEstudianteMes,
        FijoPeriodoText => UnidadRatioMaterial.FijoPeriodo,
        _ => UnidadRatioMaterial.PorEstudiante
    };
}
