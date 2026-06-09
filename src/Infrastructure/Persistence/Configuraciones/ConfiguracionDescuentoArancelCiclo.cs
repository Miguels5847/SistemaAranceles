using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Configuraciones;

internal sealed class DescuentoArancelCicloConfiguracion : IEntityTypeConfiguration<DescuentoArancelCiclo>
{
    public void Configure(EntityTypeBuilder<DescuentoArancelCiclo> builder)
    {
        builder.ToTable("descuento_arancel_ciclo");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.EscenarioProyeccionId).HasColumnName("escenario_proyeccion_id");
        builder.Property(x => x.CicloDesde).HasColumnName("ciclo_desde").IsRequired();
        builder.Property(x => x.CicloHasta).HasColumnName("ciclo_hasta").IsRequired();
        builder.Property(x => x.PorcentajeDescuento)
            .HasColumnName("porcentaje_descuento")
            .HasColumnType("numeric(6,2)")
            .HasDefaultValue(0m)
            .IsRequired();

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

        builder.HasOne(x => x.EscenarioProyeccion)
            .WithMany()
            .HasForeignKey(x => x.EscenarioProyeccionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.CarreraId, x.EscenarioProyeccionId });
        builder.HasIndex(x => new { x.CarreraId, x.EscenarioProyeccionId, x.EstaActivo });
    }
}
