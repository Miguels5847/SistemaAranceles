using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.SueldosPlantaCentral;

public sealed class ConfigurarDatosInstitucionalesCommand(
    IRepositorioDatosInstitucionales repositorio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task<DatosInstitucionalesDto> EjecutarAsync(
        GuardarDatosInstitucionalesDto dto,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (usuarioId <= 0)
        {
            throw new DominioException("Usuario que actualiza es obligatorio.");
        }

        var existente = await repositorio.ObtenerPorPeriodoAsync(dto.Periodo, cancellationToken);

        DatosInstitucionales datos;
        if (existente is null)
        {
            datos = new DatosInstitucionales(
                dto.Periodo,
                dto.NumeroEstudiantesUniversidad,
                dto.NumeroDocentesUniversidad,
                dto.NumeroPersonasPlantaCentral,
                dto.SueldoBasico,
                dto.Funcional,
                dto.FondoReserva,
                dto.BeneficioXiv,
                dto.BeneficioXiii,
                dto.AportePatronal,
                dto.Varios,
                usuarioId,
                dto.FuenteNotas,
                dto.MesesCapitalTrabajo,
                dto.PorcentajeImprevistosInversion);
            datos.CambiarParametrosDemandaIngresos(
                dto.PorcentajeMatriculaDefault,
                dto.PorcentajeBecasInstitucionales,
                dto.SemestresPorAnio,
                dto.MesesOperativosCiclo,
                dto.PresupuestoAnualCapacitacion,
                dto.PresupuestoAnualInternacionalizacion,
                dto.PresupuestoAnualMarketing,
                dto.PolizaSeguroEstudiantilAnual,
                dto.FuenteInflacion,
                dto.AnioBaseProyeccion);
            datos.CambiarParametrosCostosGastos(
                dto.PresupuestoBaseUniversidad,
                dto.PresupuestoGobiernoBecas,
                dto.PorcentajeInvestigacion,
                dto.PorcentajeVinculacion,
                dto.PorcentajeBecasEstudiantes,
                dto.PorcentajeBecasDocentes);
            repositorio.Agregar(datos);
        }
        else
        {
            existente.CambiarPoblacion(
                dto.NumeroEstudiantesUniversidad,
                dto.NumeroDocentesUniversidad,
                dto.NumeroPersonasPlantaCentral);
            existente.CambiarRubros(
                dto.SueldoBasico,
                dto.Funcional,
                dto.FondoReserva,
                dto.BeneficioXiv,
                dto.BeneficioXiii,
                dto.AportePatronal,
                dto.Varios);
            existente.CambiarParametrosInversion(
                dto.MesesCapitalTrabajo,
                dto.PorcentajeImprevistosInversion);
            existente.CambiarParametrosDemandaIngresos(
                dto.PorcentajeMatriculaDefault,
                dto.PorcentajeBecasInstitucionales,
                dto.SemestresPorAnio,
                dto.MesesOperativosCiclo,
                dto.PresupuestoAnualCapacitacion,
                dto.PresupuestoAnualInternacionalizacion,
                dto.PresupuestoAnualMarketing,
                dto.PolizaSeguroEstudiantilAnual,
                dto.FuenteInflacion,
                dto.AnioBaseProyeccion);
            existente.CambiarParametrosCostosGastos(
                dto.PresupuestoBaseUniversidad,
                dto.PresupuestoGobiernoBecas,
                dto.PorcentajeInvestigacion,
                dto.PorcentajeVinculacion,
                dto.PorcentajeBecasEstudiantes,
                dto.PorcentajeBecasDocentes);
            existente.RegistrarActualizacion(usuarioId, dto.FuenteNotas);
            repositorio.Actualizar(existente);
            datos = existente;
        }

        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        return MapeoDatosInstitucionales.AMapeo(datos, null);
    }
}
