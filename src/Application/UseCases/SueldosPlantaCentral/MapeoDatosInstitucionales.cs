using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.SueldosPlantaCentral;

internal static class MapeoDatosInstitucionales
{
    public static DatosInstitucionalesDto AMapeo(DatosInstitucionales d, string? usuarioNombre) => new()
    {
        Id = d.Id,
        Periodo = d.Periodo,
        NumeroEstudiantesUniversidad = d.NumeroEstudiantesUniversidad,
        NumeroDocentesUniversidad = d.NumeroDocentesUniversidad,
        NumeroPersonasPlantaCentral = d.NumeroPersonasPlantaCentral,
        SueldoBasico = d.SueldoBasico,
        Funcional = d.Funcional,
        FondoReserva = d.FondoReserva,
        BeneficioXiv = d.BeneficioXiv,
        BeneficioXiii = d.BeneficioXiii,
        AportePatronal = d.AportePatronal,
        Varios = d.Varios,
        MesesCapitalTrabajo = d.MesesCapitalTrabajo,
        PorcentajeImprevistosInversion = d.PorcentajeImprevistosInversion,
        MasaSalarialMensual = d.MasaSalarialMensual,
        TotalMensualPlantaCentral = d.TotalMensualPlantaCentral,
        TotalAnualPlantaCentral = d.TotalAnualPlantaCentral,
        RatioAdminPorDocente = d.RatioAdminPorDocente,
        CostoPlantaCentralPorEstudianteAnual = d.CostoPlantaCentralPorEstudianteAnual,
        FechaActualizacion = d.FechaActualizacion,
        ActualizadoPorUsuarioId = d.ActualizadoPorUsuarioId,
        ActualizadoPorUsuarioNombre = usuarioNombre,
        FuenteNotas = d.FuenteNotas,
    };
}
