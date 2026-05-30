using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Configuraciones;

internal sealed class ConfiguracionDatosInstitucionales : IEntityTypeConfiguration<DatosInstitucionales>
{
    public void Configure(EntityTypeBuilder<DatosInstitucionales> builder)
    {
        builder.ToTable("datos_institucionales");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Periodo).HasColumnName("periodo").HasMaxLength(20).IsRequired();

        builder.Property(x => x.NumeroEstudiantesUniversidad).HasColumnName("n_estudiantes_universidad").IsRequired();
        builder.Property(x => x.NumeroDocentesUniversidad).HasColumnName("n_docentes_universidad").IsRequired();
        builder.Property(x => x.NumeroPersonasPlantaCentral).HasColumnName("n_personas_planta_central").IsRequired();

        builder.Property(x => x.SueldoBasico).HasColumnName("sueldo_basico").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Funcional).HasColumnName("funcional").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.FondoReserva).HasColumnName("fondo_reserva").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.BeneficioXiv).HasColumnName("beneficio_xiv").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.BeneficioXiii).HasColumnName("beneficio_xiii").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.AportePatronal).HasColumnName("aporte_patronal").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Varios).HasColumnName("varios").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.MesesCapitalTrabajo).HasColumnName("meses_capital_trabajo").HasDefaultValue(2).IsRequired();
        builder.Property(x => x.PorcentajeImprevistosInversion).HasColumnName("porcentaje_imprevistos_inversion").HasColumnType("numeric(7,4)").HasDefaultValue(5m).IsRequired();

        // KAN-35
        builder.Property(x => x.PorcentajeMatriculaDefault).HasColumnName("porcentaje_matricula_default").HasColumnType("numeric(7,4)").HasDefaultValue(10m).IsRequired();
        builder.Property(x => x.PorcentajeBecasInstitucionales).HasColumnName("porcentaje_becas_institucionales").HasColumnType("numeric(7,4)").HasDefaultValue(10m).IsRequired();
        builder.Property(x => x.SemestresPorAnio).HasColumnName("semestres_por_anio").HasDefaultValue(2).IsRequired();
        builder.Property(x => x.MesesOperativosCiclo).HasColumnName("meses_operativos_ciclo").HasDefaultValue(6).IsRequired();
        builder.Property(x => x.PresupuestoAnualCapacitacion).HasColumnName("presupuesto_anual_capacitacion").HasColumnType("numeric(18,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.PresupuestoAnualInternacionalizacion).HasColumnName("presupuesto_anual_internacionalizacion").HasColumnType("numeric(18,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.PresupuestoAnualMarketing).HasColumnName("presupuesto_anual_marketing").HasColumnType("numeric(18,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.PolizaSeguroEstudiantilAnual).HasColumnName("poliza_seguro_estudiantil_anual").HasColumnType("numeric(18,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.FuenteInflacion).HasColumnName("fuente_inflacion").HasMaxLength(80);
        builder.Property(x => x.AnioBaseProyeccion).HasColumnName("anio_base_proyeccion");

        // KAN-36
        builder.Property(x => x.PresupuestoBaseUniversidad).HasColumnName("presupuesto_base_universidad").HasColumnType("numeric(18,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.PresupuestoGobiernoBecas).HasColumnName("presupuesto_gobierno_becas").HasColumnType("numeric(18,2)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.PorcentajeInvestigacion).HasColumnName("porcentaje_investigacion").HasColumnType("numeric(7,4)").HasDefaultValue(5m).IsRequired();
        builder.Property(x => x.PorcentajeVinculacion).HasColumnName("porcentaje_vinculacion").HasColumnType("numeric(7,4)").HasDefaultValue(1m).IsRequired();
        builder.Property(x => x.PorcentajeBecasEstudiantes).HasColumnName("porcentaje_becas_estudiantes").HasColumnType("numeric(7,4)").HasDefaultValue(90m).IsRequired();
        builder.Property(x => x.PorcentajeBecasDocentes).HasColumnName("porcentaje_becas_docentes").HasColumnType("numeric(7,4)").HasDefaultValue(10m).IsRequired();

        // KAN-40
        builder.Property(x => x.TasaInteresFinanciera).HasColumnName("tasa_interes_financiera").HasColumnType("numeric(7,4)").HasDefaultValue(8m).IsRequired();
        builder.Property(x => x.PremioRiesgo).HasColumnName("premio_riesgo").HasColumnType("numeric(7,4)").HasDefaultValue(5m).IsRequired();
        builder.Property(x => x.TmrManual).HasColumnName("tmr_manual").HasColumnType("numeric(7,4)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.UsarTmrManual).HasColumnName("usar_tmr_manual").HasDefaultValue(false).IsRequired();

        builder.Property(x => x.FechaActualizacion).HasColumnName("fecha_actualizacion").IsRequired();
        builder.Property(x => x.ActualizadoPorUsuarioId).HasColumnName("actualizado_por_usuario_id").IsRequired();
        builder.Property(x => x.FuenteNotas).HasColumnName("fuente_notas").HasMaxLength(500);

        builder.HasIndex(x => x.Periodo).IsUnique();

        builder.HasOne(x => x.ActualizadoPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.ActualizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
