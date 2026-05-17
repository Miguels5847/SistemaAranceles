using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Configuraciones;

internal sealed class ConfiguracionActivoFijo : IEntityTypeConfiguration<ActivoFijo>
{
    public void Configure(EntityTypeBuilder<ActivoFijo> builder)
    {
        builder.ToTable("recurso_activo_fijo");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Categoria).HasColumnName("categoria").HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Cantidad).HasColumnName("cantidad").HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.UnidadMedida).HasColumnName("unidad_medida").HasMaxLength(20).IsRequired();
        builder.Property(x => x.ValorUnitario).HasColumnName("valor_unitario").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.VidaUtilAnios).HasColumnName("vida_util_anios").IsRequired();
        builder.Property(x => x.PorcentajeResidual).HasColumnName("porcentaje_residual").HasColumnType("numeric(6,4)").IsRequired();
        builder.Property(x => x.FechaAdquisicion).HasColumnName("fecha_adquisicion").IsRequired();

        builder.Property(x => x.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(x => x.CreadoPorUsuarioId).HasColumnName("creado_por_usuario_id");
        builder.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
        builder.Property(x => x.ActualizadoPorUsuarioId).HasColumnName("actualizado_por_usuario_id");
        builder.Property(x => x.EstaActivo).HasColumnName("esta_activo").IsRequired();
        builder.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        builder.Property(x => x.EliminadoPorUsuarioId).HasColumnName("eliminado_por_usuario_id");

        builder.HasIndex(x => new { x.CarreraId, x.Categoria });

        builder.HasOne(x => x.Carrera)
            .WithMany()
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
