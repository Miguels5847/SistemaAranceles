using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Entities;
using Xunit;

namespace SistemaAranceles.Application.Tests.SueldosPlantaCentral;

public class Kan22DatosInstitucionalesTests
{
    private static DatosInstitucionales Construir(
        int estudiantes = 10000,
        int docentes = 500,
        int planta = 200,
        decimal sueldoBasico = 100000m,
        decimal funcional = 20000m,
        decimal fondoReserva = 10000m,
        decimal xiv = 5000m,
        decimal xiii = 5000m,
        decimal aportePatronal = 12000m,
        decimal varios = 3000m)
    {
        return new DatosInstitucionales(
            periodo: "2026",
            numeroEstudiantesUniversidad: estudiantes,
            numeroDocentesUniversidad: docentes,
            numeroPersonasPlantaCentral: planta,
            sueldoBasico: sueldoBasico,
            funcional: funcional,
            fondoReserva: fondoReserva,
            beneficioXiv: xiv,
            beneficioXiii: xiii,
            aportePatronal: aportePatronal,
            varios: varios,
            actualizadoPorUsuarioId: 1,
            fuenteNotas: null);
    }

    [Fact]
    public void MasaSalarialMensual_EsSumaDeRubros()
    {
        var datos = Construir();
        Assert.Equal(155000m, datos.MasaSalarialMensual);
    }

    [Fact]
    public void TotalAnualPlantaCentral_EsMasaMensualPorDoce()
    {
        var datos = Construir();
        Assert.Equal(1_860_000m, datos.TotalAnualPlantaCentral);
    }

    [Fact]
    public void RatioAdminPorDocente_EsPlantaSobreDocentes()
    {
        var datos = Construir(docentes: 500, planta: 200);
        Assert.Equal(0.4m, datos.RatioAdminPorDocente);
    }

    [Fact]
    public void CostoPlantaCentralPorEstudianteAnual_EsTotalAnualSobreEstudiantes()
    {
        var datos = Construir(estudiantes: 10000);
        Assert.Equal(186m, datos.CostoPlantaCentralPorEstudianteAnual);
    }

    [Fact]
    public void Constructor_LanzaSiEstudiantesUniversidadCero()
    {
        Assert.Throws<DominioException>(() => Construir(estudiantes: 0));
    }

    [Fact]
    public void Constructor_LanzaSiDocentesUniversidadCero()
    {
        Assert.Throws<DominioException>(() => Construir(docentes: 0));
    }

    [Fact]
    public void Constructor_LanzaSiUsuarioActualizaInvalido()
    {
        Assert.Throws<DominioException>(() => new DatosInstitucionales(
            "2026", 10000, 500, 200, 100000m, 0m, 0m, 0m, 0m, 0m, 0m, 0, null));
    }

    [Fact]
    public void Constructor_LanzaSiRubroNegativo()
    {
        Assert.Throws<DominioException>(() => Construir(varios: -1m));
    }

    [Fact]
    public void RegistrarActualizacion_TocaFechaYUsuario()
    {
        var datos = Construir();
        var fechaAntes = datos.FechaActualizacion;

        Thread.Sleep(10);
        datos.RegistrarActualizacion(7, "documento oficial");

        Assert.Equal(7, datos.ActualizadoPorUsuarioId);
        Assert.Equal("documento oficial", datos.FuenteNotas);
        Assert.True(datos.FechaActualizacion >= fechaAntes);
    }
}
