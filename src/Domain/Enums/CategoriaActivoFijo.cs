namespace SistemaAranceles.Domain.Enums;

/// <summary>
/// Categorias de activos fijos del bloque 1 de la hoja Excel "3 Recursos fisicos".
/// La vida util por defecto se alinea con la normativa tributaria ecuatoriana (LORTI).
/// </summary>
public enum CategoriaActivoFijo
{
    MueblesEnseres = 0,
    LaboratoriosEquipos = 1,
    EquipoComputo = 2,
    EquipoOficina = 3
}
