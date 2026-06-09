using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using Xunit;

namespace SistemaAranceles.Application.Tests.AnalisisFinanciero;

public sealed class FormatoMatrizAnalisisFinancieroTests
{
    [Fact]
    public void Formatear_MonedaNegativa_UsaParentesisContables()
    {
        var display = FormatoMatrizAnalisisFinanciero.Formatear(
            -18548.68m,
            FormatoMatrizAnalisisFinanciero.Moneda);

        Assert.StartsWith("$ (", display);
        Assert.EndsWith(")", display);
        Assert.DoesNotContain("-", display);
    }

    [Fact]
    public void FormatearPorcentajeValor_NoAgregaEspacioAntesDelSimbolo()
    {
        var display = FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(9.54m);

        Assert.EndsWith("%", display);
        Assert.DoesNotContain(" %", display);
        Assert.DoesNotContain("\u00A0%", display);
    }
}
