using SistemaAranceles.Application.Comun;
using Xunit;

namespace SistemaAranceles.Application.Tests.Comun;

public class CategoriaMaterialDisplayTests
{
    [Theory]
    [InlineData("MATERIALES_SUMINISTROS", "Materiales y Suministros")]
    [InlineData("ASEO_LIMPIEZA", "Suministros de Aseo y Limpieza")]
    [InlineData("ACCESORIOS_MATERIALES", "Accesorios y Materiales")]
    [InlineData("OTRO", "Otros")]
    [InlineData("otro", "Otros")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void Formatear_devuelve_nombre_legible(string? categoria, string esperado)
        => Assert.Equal(esperado, CategoriaMaterialDisplay.Formatear(categoria));

    [Fact]
    public void Formatear_categoria_desconocida_capitaliza_sin_guiones()
        => Assert.Equal("Equipos Laboratorio", CategoriaMaterialDisplay.Formatear("EQUIPOS_LABORATORIO"));

    [Fact]
    public void Orden_respeta_la_secuencia_de_pantalla()
    {
        Assert.True(CategoriaMaterialDisplay.Orden("MATERIALES_SUMINISTROS")
                    < CategoriaMaterialDisplay.Orden("ASEO_LIMPIEZA"));
        Assert.True(CategoriaMaterialDisplay.Orden("ASEO_LIMPIEZA")
                    < CategoriaMaterialDisplay.Orden("ACCESORIOS_MATERIALES"));
        Assert.True(CategoriaMaterialDisplay.Orden("ACCESORIOS_MATERIALES")
                    < CategoriaMaterialDisplay.Orden("OTRO"));
        Assert.True(CategoriaMaterialDisplay.Orden("DESCONOCIDA") < CategoriaMaterialDisplay.Orden("OTRO"));
    }
}
