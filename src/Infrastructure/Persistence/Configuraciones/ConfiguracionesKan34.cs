using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Configuraciones;

internal sealed class RatioMaterialDemandaConfiguracion : IEntityTypeConfiguration<RatioMaterialDemanda>
{
    public void Configure(EntityTypeBuilder<RatioMaterialDemanda> builder)
    {
        builder.ToTable("ratio_material_demanda");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CarreraId).HasColumnName("carrera_id");
        builder.Property(x => x.Categoria).HasColumnName("categoria").HasMaxLength(60).IsRequired();
        builder.Property(x => x.Concepto).HasColumnName("concepto").HasMaxLength(140).IsRequired();
        builder.Property(x => x.ItemMaterialInsumoId).HasColumnName("item_material_insumo_id");
        builder.Property(x => x.RatioConsumo).HasColumnName("ratio_consumo").HasColumnType("numeric(12,6)").HasDefaultValue(0m).IsRequired();
        builder.Property(x => x.UnidadRatio).HasColumnName("unidad_ratio").HasMaxLength(40).HasDefaultValue("por_estudiante").IsRequired();
        builder.Property(x => x.MesesOperativos).HasColumnName("meses_operativos").HasDefaultValue(6).IsRequired();
        builder.Property(x => x.AplicaInflacion).HasColumnName("aplica_inflacion").HasDefaultValue(true).IsRequired();

        builder.Property(x => x.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(x => x.CreadoPorUsuarioId).HasColumnName("creado_por_usuario_id");
        builder.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
        builder.Property(x => x.ActualizadoPorUsuarioId).HasColumnName("actualizado_por_usuario_id");
        builder.Property(x => x.EstaActivo).HasColumnName("esta_activo").IsRequired();
        builder.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        builder.Property(x => x.EliminadoPorUsuarioId).HasColumnName("eliminado_por_usuario_id");

        builder.HasOne(x => x.Carrera)
            .WithMany()
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ItemMaterialInsumo)
            .WithMany()
            .HasForeignKey(x => x.ItemMaterialInsumoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.CarreraId);
        builder.HasIndex(x => x.ItemMaterialInsumoId);
    }
}
