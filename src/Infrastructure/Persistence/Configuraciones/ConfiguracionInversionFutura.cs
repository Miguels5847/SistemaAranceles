using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Configuraciones;

internal sealed class ConfiguracionInversionFutura : IEntityTypeConfiguration<InversionFutura>
{
    public void Configure(EntityTypeBuilder<InversionFutura> builder)
    {
        builder.ToTable("inversion_futura");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ActivoFijoId).HasColumnName("activo_fijo_id").IsRequired();
        builder.Property(x => x.Anio).HasColumnName("anio").IsRequired();
        builder.Property(x => x.Semestre).HasColumnName("semestre").IsRequired();
        builder.Property(x => x.CantidadProyectada).HasColumnName("cantidad_proyectada").HasColumnType("numeric(18,4)").IsRequired();

        builder.Property(x => x.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(x => x.CreadoPorUsuarioId).HasColumnName("creado_por_usuario_id");
        builder.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
        builder.Property(x => x.ActualizadoPorUsuarioId).HasColumnName("actualizado_por_usuario_id");
        builder.Property(x => x.EstaActivo).HasColumnName("esta_activo").IsRequired();
        builder.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        builder.Property(x => x.EliminadoPorUsuarioId).HasColumnName("eliminado_por_usuario_id");

        builder.HasIndex(x => new { x.ActivoFijoId, x.Anio, x.Semestre })
            .IsUnique();

        builder.HasOne(x => x.ActivoFijo)
            .WithMany(x => x.InversionesFuturas)
            .HasForeignKey(x => x.ActivoFijoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
