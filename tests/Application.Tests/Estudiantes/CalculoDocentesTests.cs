using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Domain.Constantes;
using Xunit;

namespace SistemaAranceles.Application.Tests.Estudiantes;

public class CalculoDocentesTests
{
    // CU-ES-03 RN-94: invariante phd*18 + mgs*18 + hMT + hTP == horasAsistidas
    // Datos = los 8 períodos del Excel del docente experto (espec viva).
    [Theory]
    [InlineData(18,   0, 1, 0, 0,  0, 0)]
    [InlineData(60,   1, 2, 0, 1,  0, 6)]
    [InlineData(80,   2, 2, 0, 1,  0, 8)]
    [InlineData(122,  2, 4, 1, 1, 12, 2)]
    [InlineData(143,  3, 4, 1, 1, 12, 5)]
    [InlineData(183,  4, 6, 0, 1,  0, 3)]
    [InlineData(202,  4, 7, 0, 1,  0, 4)]
    [InlineData(234,  5, 8, 0, 0,  0, 0)]
    public void Desglose_CoincideConExcelDelDocente(
        int horas, int phdEsperado, int mgsEsperado, int mtEsperado,
        int tpEsperado, int hMTEsperado, int hTPEsperado)
    {
        var (phd, mgs, mt, tp, hMT, hTP) =
            ConsolidadorProyeccionEstudiantes.DesglosarDocentesPorPeriodo(horas);

        Assert.Equal(phdEsperado, phd);
        Assert.Equal(mgsEsperado, mgs);
        Assert.Equal(mtEsperado,  mt);
        Assert.Equal(tpEsperado,  tp);
        Assert.Equal(hMTEsperado, hMT);
        Assert.Equal(hTPEsperado, hTP);

        var horasCubiertas = phd * ConstantesDocentes.HorasDocenteTC
                           + mgs * ConstantesDocentes.HorasDocenteTC
                           + hMT + hTP;
        Assert.Equal(horas, horasCubiertas);
    }

    [Theory]
    [InlineData(18.13, 18, 0, 1, 0, 0,  0, 0)]
    [InlineData(18.75, 19, 0, 1, 0, 1,  0, 1)]
    [InlineData(21.38, 21, 0, 1, 0, 1,  0, 3)]
    [InlineData(28.50, 29, 0, 1, 0, 1,  0, 11)]
    [InlineData(31.20, 31, 0, 1, 1, 1, 12, 1)]
    public void Desglose_RedondeaHorasAntesDeAsignarDocentes(
        decimal horas, decimal horasRedondeadasEsperadas, int phdEsperado, int mgsEsperado,
        int mtEsperado, int tpEsperado, int hMTEsperado, int hTPEsperado)
    {
        var (phd, mgs, mt, tp, hMT, hTP) =
            ConsolidadorProyeccionEstudiantes.DesglosarDocentesPorPeriodo(horas);

        Assert.Equal(phdEsperado, phd);
        Assert.Equal(mgsEsperado, mgs);
        Assert.Equal(mtEsperado, mt);
        Assert.Equal(tpEsperado, tp);
        Assert.Equal(hMTEsperado, hMT);
        Assert.Equal(hTPEsperado, hTP);
        Assert.Equal(decimal.Truncate(hMT), hMT);
        Assert.Equal(decimal.Truncate(hTP), hTP);

        var horasCubiertas = phd * ConstantesDocentes.HorasDocenteTC
                           + mgs * ConstantesDocentes.HorasDocenteTC
                           + hMT + hTP;
        Assert.Equal(horasRedondeadasEsperadas, horasCubiertas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Desglose_HorasNoPositivas_DevuelveCero(decimal horas)
    {
        var r = ConsolidadorProyeccionEstudiantes.DesglosarDocentesPorPeriodo(horas);
        Assert.Equal((0, 0, 0, 0, 0m, 0m), r);
    }

    // RN-70: CES no aplica por debajo del umbral (P1 = 18h queda 0 PhD).
    [Fact]
    public void Ces_NoAplica_BajoUmbral()
    {
        var (phd, _, _, _, _, _) =
            ConsolidadorProyeccionEstudiantes.DesglosarDocentesPorPeriodo(18m);
        Assert.Equal(0, phd);
    }

    // RN-70: CES sí aplica si horas ≥ umbral, tcEntero ≥ 2 y phd calculado = 0.
    // Caso construido: 36h → tcEntero=2, mgs=round(1.2)=1, phd=1 calculado.
    // No dispara la regla porque phd ya es 1. Verificamos que el resultado siga válido.
    [Fact]
    public void Ces_RespetaInvariante_EnUmbral()
    {
        var (phd, mgs, mt, tp, hMT, hTP) =
            ConsolidadorProyeccionEstudiantes.DesglosarDocentesPorPeriodo(36m);
        Assert.True(phd >= 1);
        Assert.Equal(36m, phd * 18 + mgs * 18 + hMT + hTP);
        Assert.Equal(0, mt);
        Assert.Equal(0, tp);
    }

    // RN-72: residuo entre 0 y 12 → 1 TP, 0 MT.
    [Fact]
    public void Residuo_MenorIgualUmbralMT_GeneraSoloTP()
    {
        var (_, _, mt, tp, hMT, hTP) =
            ConsolidadorProyeccionEstudiantes.DesglosarDocentesPorPeriodo(60m);
        Assert.Equal(0, mt);
        Assert.Equal(0m, hMT);
        Assert.Equal(1, tp);
        Assert.Equal(6m, hTP);
    }

    // RN-72: residuo > 12 → 1 MT con 12h fijas + sobrante < 12 → 1 TP.
    [Fact]
    public void Residuo_MayorUmbralMT_GeneraMTYTP()
    {
        var (_, _, mt, tp, hMT, hTP) =
            ConsolidadorProyeccionEstudiantes.DesglosarDocentesPorPeriodo(122m);
        Assert.Equal(1, mt);
        Assert.Equal(12m, hMT);
        Assert.Equal(1, tp);
        Assert.Equal(2m, hTP);
    }
}
