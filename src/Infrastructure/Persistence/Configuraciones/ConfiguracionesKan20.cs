using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Configuraciones;

internal sealed class CargoFacultadConfiguracion : IEntityTypeConfiguration<CargoFacultad>
{
    public void Configure(EntityTypeBuilder<CargoFacultad> builder)
    {
        builder.ToTable("cargo_facultad");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.CarreraId)
            .HasColumnName("carrera_id")
            .IsRequired();

        builder.Property(x => x.NombreCargo)
            .HasColumnName("nombre_cargo")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.TipoCargo)
            .HasColumnName("tipo_cargo")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.SueldoBaseMensual)
            .HasColumnName("sueldo_base_mensual")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.EsCargoDocente)
            .HasColumnName("es_cargo_docente")
            .IsRequired();

        builder.HasOne(x => x.Carrera)
            .WithMany(x => x.CargosFacultad)
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<CargoFacultad> builder)
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(x => x.CreadoPorUsuarioId).HasColumnName("creado_por_usuario_id");
        builder.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
        builder.Property(x => x.ActualizadoPorUsuarioId).HasColumnName("actualizado_por_usuario_id");
        builder.Property(x => x.EstaActivo).HasColumnName("esta_activo").IsRequired();
        builder.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        builder.Property(x => x.EliminadoPorUsuarioId).HasColumnName("eliminado_por_usuario_id");
    }
}

internal sealed class ProyeccionCargoFacultadConfiguracion : IEntityTypeConfiguration<ProyeccionCargoFacultad>
{
    public void Configure(EntityTypeBuilder<ProyeccionCargoFacultad> builder)
    {
        builder.ToTable("proyeccion_cargo_facultad");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.CargoFacultadId)
            .HasColumnName("cargo_facultad_id")
            .IsRequired();

        builder.Property(x => x.PeriodoAcademicoId)
            .HasColumnName("periodo_academico_id")
            .IsRequired();

        builder.Property(x => x.CantidadPersonas)
            .HasColumnName("cantidad_personas")
            .HasPrecision(10, 4)
            .IsRequired();

        builder.Property(x => x.FactorPonderacion)
            .HasColumnName("factor_ponderacion")
            .HasPrecision(10, 4)
            .IsRequired();

        builder.Property(x => x.FactorInflacion)
            .HasColumnName("factor_inflacion")
            .HasPrecision(10, 6)
            .IsRequired();

        builder.Property(x => x.CostoTotalSemestre)
            .HasColumnName("costo_total_semestre")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne(x => x.CargoFacultad)
            .WithMany(x => x.ProyeccionesCargoFacultad)
            .HasForeignKey(x => x.CargoFacultadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PeriodoAcademico)
            .WithMany()
            .HasForeignKey(x => x.PeriodoAcademicoId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<ProyeccionCargoFacultad> builder)
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(x => x.CreadoPorUsuarioId).HasColumnName("creado_por_usuario_id");
        builder.Property(x => x.ActualizadoEn).HasColumnName("actualizado_en");
        builder.Property(x => x.ActualizadoPorUsuarioId).HasColumnName("actualizado_por_usuario_id");
        builder.Property(x => x.EstaActivo).HasColumnName("esta_activo").IsRequired();
        builder.Property(x => x.EliminadoEn).HasColumnName("eliminado_en");
        builder.Property(x => x.EliminadoPorUsuarioId).HasColumnName("eliminado_por_usuario_id");
    }
}
