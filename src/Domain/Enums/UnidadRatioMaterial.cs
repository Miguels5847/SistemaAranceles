namespace SistemaAranceles.Domain.Enums;

public enum UnidadRatioMaterial
{
    PorEstudiante = 0,
    PorEstudianteMes = 1,
    FijoPeriodo = 2,
    PorDocente = 3
}

public static class UnidadRatioMaterialExtensiones
{
    public const string PorEstudianteText = "por_estudiante";
    public const string PorEstudianteMesText = "por_estudiante_mes";
    public const string FijoPeriodoText = "fijo_periodo";
    public const string PorDocenteText = "por_docente";

    public static string ToBdText(this UnidadRatioMaterial unidad) => unidad switch
    {
        UnidadRatioMaterial.PorEstudianteMes => PorEstudianteMesText,
        UnidadRatioMaterial.FijoPeriodo => FijoPeriodoText,
        UnidadRatioMaterial.PorDocente => PorDocenteText,
        _ => PorEstudianteText
    };

    public static UnidadRatioMaterial FromBdText(string? texto) => texto switch
    {
        PorEstudianteMesText => UnidadRatioMaterial.PorEstudianteMes,
        FijoPeriodoText => UnidadRatioMaterial.FijoPeriodo,
        PorDocenteText => UnidadRatioMaterial.PorDocente,
        _ => UnidadRatioMaterial.PorEstudiante
    };
}
