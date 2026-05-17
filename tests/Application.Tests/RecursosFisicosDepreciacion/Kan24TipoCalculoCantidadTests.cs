using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;
using Xunit;

namespace SistemaAranceles.Application.Tests.RecursosFisicosDepreciacion;

public class Kan24TipoCalculoCantidadTests
{
    private static ActivoFijo Construir(
        TipoCalculoCantidad tipo,
        decimal factor = 1m,
        decimal offset = 0m,
        decimal cantidad = 5m)
    {
        return new ActivoFijo(
            carreraId: 1,
            descripcion: "Activo test",
            categoria: CategoriaActivoFijo.LaboratoriosEquipos,
            cantidad: cantidad,
            unidadMedida: "UNI",
            valorUnitario: 10m,
            vidaUtilAnios: 10,
            porcentajeResidual: 0.05m,
            fechaAdquisicion: null,
            tipoCalculoCantidad: tipo,
            factorMultiplicador: factor,
            offsetCantidad: offset);
    }

    [Fact]
    public void Manual_UsaCantidadAlmacenada()
    {
        var a = Construir(TipoCalculoCantidad.Manual, cantidad: 7m);
        Assert.Equal(7m, a.ResolverCantidad(999m, 999m, esSemestreInicial: true));
    }

    [Fact]
    public void PorEstudiante_EsTotalEstudiantesPorFactor()
    {
        var a = Construir(TipoCalculoCantidad.PorEstudiante, factor: 1m);
        Assert.Equal(30m, a.ResolverCantidad(30m, 0m, esSemestreInicial: false));
    }

    [Fact]
    public void PorDocente_AplicaOffsetSoloEnSemestreInicial()
    {
        var a = Construir(TipoCalculoCantidad.PorDocente, factor: 1m, offset: 10m);

        Assert.Equal(20m, a.ResolverCantidad(0m, 10m, esSemestreInicial: true));
        Assert.Equal(10m, a.ResolverCantidad(0m, 10m, esSemestreInicial: false));
    }

    [Fact]
    public void PorHito_UsaCantidadAlmacenada()
    {
        var a = Construir(TipoCalculoCantidad.PorHito, cantidad: 1m);
        Assert.Equal(1m, a.ResolverCantidad(500m, 50m, esSemestreInicial: true));
    }

    [Fact]
    public void CambiarCalculoCantidad_RechazaFactorNegativo()
    {
        var a = Construir(TipoCalculoCantidad.Manual);
        Assert.Throws<DominioException>(() => a.CambiarCalculoCantidad(TipoCalculoCantidad.PorEstudiante, -1m, 0m));
    }

    [Fact]
    public void Catalogo_CrearActivoParaCarrera_CopiaPlantilla()
    {
        var catalogo = new CatalogoActivoBase(
            descripcion: "Operatividad EVEA",
            categoria: CategoriaActivoFijo.LaboratoriosEquipos,
            tipoCalculoCantidad: TipoCalculoCantidad.PorEstudiante,
            cantidadDefault: 0m,
            unidadMedida: "UNI",
            valorUnitario: 7.5m,
            factorMultiplicador: 1m,
            offsetCantidad: 0m,
            vidaUtilAnios: 10,
            porcentajeResidual: 0.05m);

        var activo = catalogo.CrearActivoParaCarrera(42);

        Assert.Equal(42, activo.CarreraId);
        Assert.Equal("Operatividad EVEA", activo.Descripcion);
        Assert.Equal(TipoCalculoCantidad.PorEstudiante, activo.TipoCalculoCantidad);
        Assert.Equal(7.5m, activo.ValorUnitario);
    }
}
