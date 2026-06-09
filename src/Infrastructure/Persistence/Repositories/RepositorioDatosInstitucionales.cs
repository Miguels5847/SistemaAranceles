using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using DominioDatosInstitucionales = SistemaAranceles.Domain.Entities.DatosInstitucionales;
using InfraDatosInstitucionales = SistemaAranceles.Infrastructure.Persistence.Entidades.DatosInstitucionales;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioDatosInstitucionales(ContextoAplicacion contexto) : IRepositorioDatosInstitucionales
{
    public async Task<DominioDatosInstitucionales?> ObtenerPorPeriodoAsync(string periodo, CancellationToken cancellationToken = default)
    {
        await AsegurarParametrosInversionAsync(cancellationToken);

        var e = await contexto.DatosInstitucionales
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Periodo == periodo, cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<DominioDatosInstitucionales?> ObtenerVigenteAsync(CancellationToken cancellationToken = default)
    {
        await AsegurarParametrosInversionAsync(cancellationToken);

        var e = await contexto.DatosInstitucionales
            .AsNoTracking()
            .OrderByDescending(x => x.FechaActualizacion)
            .FirstOrDefaultAsync(cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<DominioDatosInstitucionales?> ObtenerAnteriorAsync(string periodoActual, CancellationToken cancellationToken = default)
    {
        await AsegurarParametrosInversionAsync(cancellationToken);

        var e = await contexto.DatosInstitucionales
            .AsNoTracking()
            .Where(x => x.Periodo != periodoActual)
            .OrderByDescending(x => x.FechaActualizacion)
            .FirstOrDefaultAsync(cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<IReadOnlyList<DominioDatosInstitucionales>> ListarHistoricoAsync(CancellationToken cancellationToken = default)
    {
        await AsegurarParametrosInversionAsync(cancellationToken);

        var lista = await contexto.DatosInstitucionales
            .AsNoTracking()
            .OrderByDescending(x => x.FechaActualizacion)
            .ToListAsync(cancellationToken);
        return lista.Select(MapearADominio).ToList();
    }

    private static readonly SemaphoreSlim _gateEsquema = new(1, 1);
    private static bool _esquemaListo;

    // El DDL self-healing corre una sola vez por proceso (no en cada lectura): el esquema no cambia
    // bajo una app en ejecución, y un reinicio lo vuelve a aplicar (auto-heal preservado).
    private async Task AsegurarParametrosInversionAsync(CancellationToken cancellationToken)
    {
        if (_esquemaListo)
            return;

        await _gateEsquema.WaitAsync(cancellationToken);
        try
        {
            if (_esquemaListo)
                return;

            await contexto.Database.ExecuteSqlRawAsync("""
            ALTER TABLE public.datos_institucionales
                ADD COLUMN IF NOT EXISTS meses_capital_trabajo INTEGER NOT NULL DEFAULT 2;

            ALTER TABLE public.datos_institucionales
                ADD COLUMN IF NOT EXISTS porcentaje_imprevistos_inversion NUMERIC(7,4) NOT NULL DEFAULT 5.0000;

            ALTER TABLE public.datos_institucionales
                ADD COLUMN IF NOT EXISTS presupuesto_base_universidad NUMERIC(18,2) NOT NULL DEFAULT 0.00,
                ADD COLUMN IF NOT EXISTS presupuesto_gobierno_becas NUMERIC(18,2) NOT NULL DEFAULT 0.00,
                ADD COLUMN IF NOT EXISTS porcentaje_investigacion NUMERIC(7,4) NOT NULL DEFAULT 5.0000,
                ADD COLUMN IF NOT EXISTS porcentaje_vinculacion NUMERIC(7,4) NOT NULL DEFAULT 1.0000,
                ADD COLUMN IF NOT EXISTS porcentaje_becas_estudiantes NUMERIC(7,4) NOT NULL DEFAULT 90.0000,
                ADD COLUMN IF NOT EXISTS porcentaje_becas_docentes NUMERIC(7,4) NOT NULL DEFAULT 10.0000;

            UPDATE public.datos_institucionales
               SET meses_capital_trabajo = 2
             WHERE meses_capital_trabajo IS NULL;

            UPDATE public.datos_institucionales
               SET porcentaje_imprevistos_inversion = 5.0000
             WHERE porcentaje_imprevistos_inversion IS NULL;

            UPDATE public.datos_institucionales
               SET porcentaje_investigacion = 5.0000
             WHERE porcentaje_investigacion IS NULL;

            UPDATE public.datos_institucionales
               SET porcentaje_vinculacion = 1.0000
             WHERE porcentaje_vinculacion IS NULL;

            UPDATE public.datos_institucionales
               SET porcentaje_becas_estudiantes = 90.0000
             WHERE porcentaje_becas_estudiantes IS NULL;

            UPDATE public.datos_institucionales
               SET porcentaje_becas_docentes = 10.0000
             WHERE porcentaje_becas_docentes IS NULL;

            ALTER TABLE public.datos_institucionales
                ALTER COLUMN meses_capital_trabajo SET DEFAULT 2,
                ALTER COLUMN meses_capital_trabajo SET NOT NULL,
                ALTER COLUMN porcentaje_imprevistos_inversion SET DEFAULT 5.0000,
                ALTER COLUMN porcentaje_imprevistos_inversion SET NOT NULL,
                ALTER COLUMN presupuesto_base_universidad SET DEFAULT 0.00,
                ALTER COLUMN presupuesto_base_universidad SET NOT NULL,
                ALTER COLUMN presupuesto_gobierno_becas SET DEFAULT 0.00,
                ALTER COLUMN presupuesto_gobierno_becas SET NOT NULL,
                ALTER COLUMN porcentaje_investigacion SET DEFAULT 5.0000,
                ALTER COLUMN porcentaje_investigacion SET NOT NULL,
                ALTER COLUMN porcentaje_vinculacion SET DEFAULT 1.0000,
                ALTER COLUMN porcentaje_vinculacion SET NOT NULL,
                ALTER COLUMN porcentaje_becas_estudiantes SET DEFAULT 90.0000,
                ALTER COLUMN porcentaje_becas_estudiantes SET NOT NULL,
                ALTER COLUMN porcentaje_becas_docentes SET DEFAULT 10.0000,
                ALTER COLUMN porcentaje_becas_docentes SET NOT NULL;

            ALTER TABLE public.datos_institucionales
                ADD COLUMN IF NOT EXISTS tasa_interes_financiera NUMERIC(7,4) NOT NULL DEFAULT 8.0000,
                ADD COLUMN IF NOT EXISTS premio_riesgo NUMERIC(7,4) NOT NULL DEFAULT 5.0000,
                ADD COLUMN IF NOT EXISTS tmr_manual NUMERIC(7,4) NOT NULL DEFAULT 0.0000,
                ADD COLUMN IF NOT EXISTS usar_tmr_manual BOOLEAN NOT NULL DEFAULT FALSE;

            ALTER TABLE public.datos_institucionales
                ADD COLUMN IF NOT EXISTS tolerancia_van_arancel NUMERIC(18,2) NOT NULL DEFAULT 1.00,
                ADD COLUMN IF NOT EXISTS margen_aproximacion_van_arancel NUMERIC(18,2) NOT NULL DEFAULT 2.00,
                ADD COLUMN IF NOT EXISTS arancel_minimo_busqueda NUMERIC(18,2) NOT NULL DEFAULT 500.00,
                ADD COLUMN IF NOT EXISTS arancel_maximo_busqueda NUMERIC(18,2) NOT NULL DEFAULT 5000.00,
                ADD COLUMN IF NOT EXISTS max_iteraciones_biseccion INTEGER NOT NULL DEFAULT 60;

            ALTER TABLE public.datos_institucionales
                ADD COLUMN IF NOT EXISTS porcentaje_financiado_prestamo NUMERIC(7,4) NOT NULL DEFAULT 0.0000,
                ADD COLUMN IF NOT EXISTS porcentaje_financiado_convenio NUMERIC(7,4) NOT NULL DEFAULT 0.0000,
                ADD COLUMN IF NOT EXISTS nombre_entidad_prestamo VARCHAR(120),
                ADD COLUMN IF NOT EXISTS nombre_entidad_convenio VARCHAR(120),
                ADD COLUMN IF NOT EXISTS tasa_interes_anual_prestamo NUMERIC(7,4) NOT NULL DEFAULT 15.0200,
                ADD COLUMN IF NOT EXISTS plazo_prestamo_meses INTEGER NOT NULL DEFAULT 24;
            """, cancellationToken);
            _esquemaListo = true;
        }
        finally
        {
            _gateEsquema.Release();
        }
    }

    public void Agregar(DominioDatosInstitucionales datos)
        => contexto.DatosInstitucionales.Add(MapearAInfra(datos));

    public void Actualizar(DominioDatosInstitucionales datos)
    {
        var rastreado = contexto.ChangeTracker.Entries<InfraDatosInstitucionales>()
            .FirstOrDefault(e => e.Entity.Id == datos.Id);
        if (rastreado is not null)
        {
            rastreado.State = EntityState.Detached;
        }

        contexto.DatosInstitucionales.Update(MapearAInfra(datos));
    }

    private static DominioDatosInstitucionales MapearADominio(InfraDatosInstitucionales e)
    {
        var dominio = new DominioDatosInstitucionales(
            e.Periodo,
            e.NumeroEstudiantesUniversidad,
            e.NumeroDocentesUniversidad,
            e.NumeroPersonasPlantaCentral,
            e.SueldoBasico,
            e.Funcional,
            e.FondoReserva,
            e.BeneficioXiv,
            e.BeneficioXiii,
            e.AportePatronal,
            e.Varios,
            e.ActualizadoPorUsuarioId,
            e.FuenteNotas,
            e.MesesCapitalTrabajo,
            e.PorcentajeImprevistosInversion);

        dominio.CambiarParametrosDemandaIngresos(
            e.PorcentajeMatriculaDefault,
            e.PorcentajeBecasInstitucionales,
            e.SemestresPorAnio,
            e.MesesOperativosCiclo,
            e.PresupuestoAnualCapacitacion,
            e.PresupuestoAnualInternacionalizacion,
            e.PresupuestoAnualMarketing,
            e.PolizaSeguroEstudiantilAnual,
            e.FuenteInflacion,
            e.AnioBaseProyeccion);

        dominio.CambiarParametrosCostosGastos(
            e.PresupuestoBaseUniversidad,
            e.PresupuestoGobiernoBecas,
            e.PorcentajeInvestigacion,
            e.PorcentajeVinculacion,
            e.PorcentajeBecasEstudiantes,
            e.PorcentajeBecasDocentes);

        dominio.CambiarParametrosAnalisisFinanciero(
            e.TasaInteresFinanciera,
            e.PremioRiesgo,
            e.TmrManual,
            e.UsarTmrManual);

        dominio.CambiarParametrosArancelOptimo(
            e.ToleranciaVanArancel,
            e.MargenAproximacionVanArancel,
            e.ArancelMinimoBusqueda,
            e.ArancelMaximoBusqueda,
            e.MaxIteracionesBiseccion);

        dominio.CambiarParametrosFinanciamiento(
            e.PorcentajeFinanciadoPrestamo,
            e.PorcentajeFinanciadoConvenio,
            e.NombreEntidadPrestamo,
            e.NombreEntidadConvenio,
            e.TasaInteresAnualPrestamo,
            e.PlazoPrestamoMeses);

        dominio.RehidratarId(e.Id);
        dominio.RehidratarFechaActualizacion(e.FechaActualizacion);
        return dominio;
    }

    private static InfraDatosInstitucionales MapearAInfra(DominioDatosInstitucionales d) => new()
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
        PorcentajeMatriculaDefault = d.PorcentajeMatriculaDefault,
        PorcentajeBecasInstitucionales = d.PorcentajeBecasInstitucionales,
        SemestresPorAnio = d.SemestresPorAnio,
        MesesOperativosCiclo = d.MesesOperativosCiclo,
        PresupuestoAnualCapacitacion = d.PresupuestoAnualCapacitacion,
        PresupuestoAnualInternacionalizacion = d.PresupuestoAnualInternacionalizacion,
        PresupuestoAnualMarketing = d.PresupuestoAnualMarketing,
        PolizaSeguroEstudiantilAnual = d.PolizaSeguroEstudiantilAnual,
        FuenteInflacion = d.FuenteInflacion,
        AnioBaseProyeccion = d.AnioBaseProyeccion,
        PresupuestoBaseUniversidad = d.PresupuestoBaseUniversidad,
        PresupuestoGobiernoBecas = d.PresupuestoGobiernoBecas,
        PorcentajeInvestigacion = d.PorcentajeInvestigacion,
        PorcentajeVinculacion = d.PorcentajeVinculacion,
        PorcentajeBecasEstudiantes = d.PorcentajeBecasEstudiantes,
        PorcentajeBecasDocentes = d.PorcentajeBecasDocentes,
        TasaInteresFinanciera = d.TasaInteresFinanciera,
        PremioRiesgo = d.PremioRiesgo,
        TmrManual = d.TmrManual,
        UsarTmrManual = d.UsarTmrManual,
        ToleranciaVanArancel = d.ToleranciaVanArancel,
        MargenAproximacionVanArancel = d.MargenAproximacionVanArancel,
        ArancelMinimoBusqueda = d.ArancelMinimoBusqueda,
        ArancelMaximoBusqueda = d.ArancelMaximoBusqueda,
        MaxIteracionesBiseccion = d.MaxIteracionesBiseccion,
        PorcentajeFinanciadoPrestamo = d.PorcentajeFinanciadoPrestamo,
        PorcentajeFinanciadoConvenio = d.PorcentajeFinanciadoConvenio,
        NombreEntidadPrestamo = d.NombreEntidadPrestamo,
        NombreEntidadConvenio = d.NombreEntidadConvenio,
        TasaInteresAnualPrestamo = d.TasaInteresAnualPrestamo,
        PlazoPrestamoMeses = d.PlazoPrestamoMeses,
        FechaActualizacion = d.FechaActualizacion,
        ActualizadoPorUsuarioId = d.ActualizadoPorUsuarioId,
        FuenteNotas = d.FuenteNotas,
    };
}
