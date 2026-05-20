using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Configuraciones;

internal sealed class ConfiguracionActivoDiferido : IEntityTypeConfiguration<ActivoDiferido>
{
    public void Configure(EntityTypeBuilder<ActivoDiferido> builder)
    {
        builder.ToTable("activo_diferido");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.NombreRubro).HasColumnName("nombre_rubro").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Valor).HasColumnName("valor").HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.TasaAmortizacionAnual).HasColumnName("tasa_amortizacion_anual").HasColumnType("numeric(6,4)").IsRequired();

        builder.Property(x => x.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(x => x.CreadoPorUsuarioId).HasColumnName("creado_por_usuario_id");
        builder.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
        builder.Property(x => x.ActualizadoPorUsuarioId).HasColumnName("actualizado_por_usuario_id");
        builder.Property(x => x.EstaActivo).HasColumnName("esta_activo").IsRequired();
        builder.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        builder.Property(x => x.EliminadoPorUsuarioId).HasColumnName("eliminado_por_usuario_id");

        builder.HasIndex(x => x.CarreraId);

        builder.HasOne(x => x.Carrera)
            .WithMany()
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
