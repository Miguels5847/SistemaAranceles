using SistemaAranceles.Domain.Entities;
using Xunit;

namespace SistemaAranceles.Application.Tests.TasaRetencion;

public sealed class DerivacionMetaTasaTests
{
    [Fact]
    public void TasaPorCicloDesdeMeta_ReproduceElExcel()
    {
        // Malla de 8 → pasos retención = 3. El Excel usa 0.97/ciclo y muestra meta = 0.97³ = 91.27%.
        var pasos = ConfiguracionRetencion.PasosRetencion(8);
        Assert.Equal(3, pasos);

        var tasaPorCiclo = ConfiguracionRetencion.TasaPorCicloDesdeMeta(91.27m, pasos);
        Assert.Equal(97.00m, tasaPorCiclo, precision: 1);
    }

    [Fact]
    public void MetaYTasa_SonInversas()
    {
        var pasos = ConfiguracionRetencion.PasosGraduacion(8); // 8 - 4 - 1 = 3
        Assert.Equal(3, pasos);

        var tasa = ConfiguracionRetencion.TasaPorCicloDesdeMeta(65m, pasos);
        var metaVuelta = ConfiguracionRetencion.MetaDesdeTasaPorCiclo(tasa, pasos);
        Assert.Equal(65m, metaVuelta, precision: 1);
    }

    [Fact]
    public void DefinirMetas_DerivaTasasPorCiclo()
    {
        var config = new ConfiguracionRetencion(carreraId: 1, escenarioProyeccionId: 1, totalCiclos: 8,
            tasaRetencionPorcentaje: 0m, tasaGraduacionPorcentaje: 0m);

        config.DefinirMetas(metaRetencionPorcentaje: 65m, metaGraduacionPorcentaje: 100m);

        Assert.Equal(65m, config.MetaRetencionPorcentaje);
        Assert.Equal(100m, config.MetaGraduacionPorcentaje);
        // 65%^(1/3) ≈ 86.62% por ciclo; meta 100% ⇒ 100% por ciclo (sin decaimiento).
        Assert.Equal(86.62m, config.TasaRetencionPorcentaje, precision: 1);
        Assert.Equal(100m, config.TasaGraduacionPorcentaje, precision: 1);
    }
}
