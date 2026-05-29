using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Configuraciones;

internal sealed class ConfiguracionArancelCarreraConfiguracion : IEntityTypeConfiguration<ConfiguracionArancelCarrera>
{
    public void Configure(EntityTypeBuilder<ConfiguracionArancelCarrera> builder)
    {
        builder.ToTable("configuracion_arancel_carrera");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.EscenarioProyeccionId).HasColumnName("escenario_proyeccion_id");
        builder.Property(x => x.ModoCalculoArancel)
            .HasColumnName("modo_calculo_arancel")
            .HasMaxLength(30)
            .HasDefaultValue("Manual")
            .IsRequired();
        builder.Property(x => x.ArancelManual)
            .HasColumnName("arancel_manual")
            .HasColumnType("decimal(18,2)");
        builder.Property(x => x.PorcentajeMatricula)
            .HasColumnName("porcentaje_matricula")
            .HasColumnType("decimal(7,4)");
        builder.Property(x => x.UsaPorcentajeMatriculaInstitucional)
            .HasColumnName("usa_porcentaje_matricula_institucional")
            .HasDefaultValue(true)
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

        builder.HasIndex(x => x.EscenarioProyeccionId);
        builder.HasIndex(x => new { x.CarreraId, x.EscenarioProyeccionId });
    }
}
