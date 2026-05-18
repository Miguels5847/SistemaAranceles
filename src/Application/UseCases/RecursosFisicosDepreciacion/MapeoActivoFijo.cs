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
        CategoriaActivoFijo.Edificacion => "Edificación",
        CategoriaActivoFijo.Vehiculos => "Vehículos",
        CategoriaActivoFijo.Nueva => "Nueva",
        _ => categoria.ToString()
    };

    public static string NombreTipoCalculo(TipoCalculoCantidad tipo) => tipo switch
    {
        TipoCalculoCantidad.Manual => "Manual",
        TipoCalculoCantidad.PorEstudiante => "Por estudiante",
        TipoCalculoCantidad.PorDocente => "Por docente",
        TipoCalculoCantidad.PorHito => "Por hito",
        _ => tipo.ToString()
    };

    public static decimal ResolverCantidadMostrada(
        ActivoFijo activo,
        bool usarCantidadCalculada = false,
        decimal totalEstudiantesSemestre = 0m,
        decimal totalDocentesSemestre = 0m)
    {
        var aplicaCalculo = usarCantidadCalculada
            && activo.TipoCalculoCantidad is TipoCalculoCantidad.PorEstudiante or TipoCalculoCantidad.PorDocente;

        return aplicaCalculo
            ? activo.ResolverCantidad(totalEstudiantesSemestre, totalDocentesSemestre, esSemestreInicial: true)
            : activo.Cantidad;
    }

    public static decimal ResolverValorTotalMostrado(
        ActivoFijo activo,
        bool usarCantidadCalculada = false,
        decimal totalEstudiantesSemestre = 0m,
        decimal totalDocentesSemestre = 0m)
        => decimal.Round(
            ResolverCantidadMostrada(
                activo,
                usarCantidadCalculada,
                totalEstudiantesSemestre,
                totalDocentesSemestre) * activo.ValorUnitario,
            2);

    public static ActivoFijoDto ADto(
        ActivoFijo a,
        bool usarCantidadCalculada = false,
        decimal totalEstudiantesSemestre = 0m,
        decimal totalDocentesSemestre = 0m)
    {
        var cantidadMostrada = ResolverCantidadMostrada(
            a,
            usarCantidadCalculada,
            totalEstudiantesSemestre,
            totalDocentesSemestre);

        return new ActivoFijoDto
        {
            Id = a.Id,
            CarreraId = a.CarreraId,
            Descripcion = a.Descripcion,
            Categoria = a.Categoria,
            CategoriaNombre = NombreCategoria(a.Categoria),
            Cantidad = cantidadMostrada,
            CantidadBase = a.Cantidad,
            UnidadMedida = a.UnidadMedida,
            ValorUnitario = a.ValorUnitario,
            ValorTotal = decimal.Round(cantidadMostrada * a.ValorUnitario, 2),
            VidaUtilAnios = a.VidaUtilAnios,
            PorcentajeResidual = a.PorcentajeResidual,
            FechaAdquisicion = a.FechaAdquisicion,
            TipoCalculoCantidad = a.TipoCalculoCantidad,
            TipoCalculoNombre = NombreTipoCalculo(a.TipoCalculoCantidad),
            FactorMultiplicador = a.FactorMultiplicador,
            OffsetCantidad = a.OffsetCantidad,
            UsaCantidadCalculada = usarCantidadCalculada
                && a.TipoCalculoCantidad is TipoCalculoCantidad.PorEstudiante or TipoCalculoCantidad.PorDocente
        };
    }
}
