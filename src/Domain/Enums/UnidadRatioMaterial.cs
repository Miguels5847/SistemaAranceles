namespace SistemaAranceles.Domain.Enums;

public enum UnidadRatioMaterial
{
    PorEstudiante = 0,
    PorEstudianteMes = 1
}

public static class UnidadRatioMaterialExtensiones
{
    public const string PorEstudianteText = "por_estudiante";
    public const string PorEstudianteMesText = "por_estudiante_mes";

    public static string ToBdText(this UnidadRatioMaterial unidad) => unidad switch
    {
        UnidadRatioMaterial.PorEstudianteMes => PorEstudianteMesText,
        _ => PorEstudianteText
    };

    public static UnidadRatioMaterial FromBdText(string? texto) => texto switch
    {
        PorEstudianteMesText => UnidadRatioMaterial.PorEstudianteMes,
        _ => UnidadRatioMaterial.PorEstudiante
    };
}
