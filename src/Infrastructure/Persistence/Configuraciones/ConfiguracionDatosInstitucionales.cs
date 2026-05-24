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
