using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

internal static class MapeoActivoFijo
{
    public static string NombreCategoria(CategoriaActivoFijo categoria) => categoria switch
    {
        CategoriaActivoFijo.MueblesEnseres => "Muebles y enseres",
        CategoriaActivoFijo.LaboratoriosEquipos => "Laboratorios y equipos",
        CategoriaActivoFijo.EquipoComputo => "Equipo de computo",
        CategoriaActivoFijo.EquipoOficina => "Equipo de oficina",
        _ => categoria.ToString()
    };

    public static ActivoFijoDto ADto(ActivoFijo a) => new()
    {
        Id = a.Id,
        CarreraId = a.CarreraId,
        Descripcion = a.Descripcion,
        Categoria = a.Categoria,
        CategoriaNombre = NombreCategoria(a.Categoria),
        Cantidad = a.Cantidad,
        UnidadMedida = a.UnidadMedida,
        ValorUnitario = a.ValorUnitario,
        ValorTotal = a.ValorTotal,
        VidaUtilAnios = a.VidaUtilAnios,
        PorcentajeResidual = a.PorcentajeResidual,
        FechaAdquisicion = a.FechaAdquisicion
    };
}
