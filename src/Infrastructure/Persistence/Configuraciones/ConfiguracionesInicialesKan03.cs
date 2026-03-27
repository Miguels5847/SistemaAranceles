using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Configuraciones;

internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.NombreCompleto).HasColumnName("nombre_completo").HasMaxLength(150).IsRequired();
        builder.Property(x => x.CorreoInstitucional).HasColumnName("correo_institucional").HasMaxLength(150).IsRequired();
        builder.Property(x => x.HashContrasena).HasColumnName("hash_contrasena").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(30).IsRequired();
        builder.Property(x => x.UltimoAccesoEn).HasColumnName("ultimo_acceso_en");

        builder.HasIndex(x => x.CorreoInstitucional).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<Usuario> builder)
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

internal sealed class RolConfiguracion : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("rol");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300).IsRequired();

        builder.HasIndex(x => x.Nombre).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<Rol> builder)
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

internal sealed class PermisoConfiguracion : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        builder.ToTable("permiso");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(100).IsRequired();
        builder.Property(x => x.ModuloNombre).HasColumnName("modulo_nombre").HasMaxLength(100).IsRequired();
        builder.Property(x => x.AccionNombre).HasColumnName("accion_nombre").HasMaxLength(60).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(300).IsRequired();

        builder.HasIndex(x => x.Codigo).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<Permiso> builder)
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

internal sealed class RolPermisoConfiguracion : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("rol_permiso");
        builder.HasKey(x => new { x.RolId, x.PermisoId });

        builder.Property(x => x.RolId).HasColumnName("rol_id");
        builder.Property(x => x.PermisoId).HasColumnName("permiso_id");

        builder.HasOne(x => x.Rol)
            .WithMany(x => x.RolPermisos)
            .HasForeignKey(x => x.RolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Permiso)
            .WithMany(x => x.RolPermisos)
            .HasForeignKey(x => x.PermisoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UsuarioRolConfiguracion : IEntityTypeConfiguration<UsuarioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioRol> builder)
    {
        builder.ToTable("usuario_rol");
        builder.HasKey(x => new { x.UsuarioId, x.RolId });

        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        builder.Property(x => x.RolId).HasColumnName("rol_id");

        builder.HasOne(x => x.Usuario)
            .WithMany(x => x.UsuarioRoles)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Rol)
            .WithMany(x => x.UsuarioRoles)
            .HasForeignKey(x => x.RolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SesionUsuarioConfiguracion : IEntityTypeConfiguration<SesionUsuario>
{
    public void Configure(EntityTypeBuilder<SesionUsuario> builder)
    {
        builder.ToTable("sesion_usuario");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.TokenSesion).HasColumnName("token_sesion").HasMaxLength(300).IsRequired();
        builder.Property(x => x.EmitidoEn).HasColumnName("emitido_en").IsRequired();
        builder.Property(x => x.ExpiraEn).HasColumnName("expira_en").IsRequired();
        builder.Property(x => x.RevocadoEn).HasColumnName("revocado_en");

        builder.HasIndex(x => x.TokenSesion).IsUnique();

        builder.HasOne(x => x.Usuario)
            .WithMany(x => x.Sesiones)
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AuditoriaLogConfiguracion : IEntityTypeConfiguration<AuditoriaLog>
{
    public void Configure(EntityTypeBuilder<AuditoriaLog> builder)
    {
        builder.ToTable("auditoria_log");

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EventoEn).HasColumnName("evento_en").IsRequired();
        builder.Property(x => x.ModuloNombre).HasColumnName("modulo_nombre").HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntidadNombre).HasColumnName("entidad_nombre").HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntidadId).HasColumnName("entidad_id").HasMaxLength(80).IsRequired();
        builder.Property(x => x.AccionNombre).HasColumnName("accion_nombre").HasMaxLength(50).IsRequired();
        builder.Property(x => x.ResumenTexto).HasColumnName("resumen_texto").HasMaxLength(500).IsRequired();
        builder.Property(x => x.ValoresAnterioresJson).HasColumnName("valores_anteriores_json");
        builder.Property(x => x.ValoresNuevosJson).HasColumnName("valores_nuevos_json");
        builder.Property(x => x.EjecutadoPorUsuarioId).HasColumnName("ejecutado_por_usuario_id");

        builder.HasIndex(x => new { x.EventoEn, x.ModuloNombre });

        builder.HasOne(x => x.EjecutadoPorUsuario)
            .WithMany()
            .HasForeignKey(x => x.EjecutadoPorUsuarioId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class CarreraConfiguracion : IEntityTypeConfiguration<Carrera>
{
    public void Configure(EntityTypeBuilder<Carrera> builder)
    {
        builder.ToTable("carrera");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(180).IsRequired();
        builder.Property(x => x.FacultadNombre).HasColumnName("facultad_nombre").HasMaxLength(180).IsRequired();
        builder.Property(x => x.TotalCiclos).HasColumnName("total_ciclos").IsRequired();

        builder.HasIndex(x => x.Codigo).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<Carrera> builder)
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

internal sealed class PeriodoAcademicoConfiguracion : IEntityTypeConfiguration<PeriodoAcademico>
{
    public void Configure(EntityTypeBuilder<PeriodoAcademico> builder)
    {
        builder.ToTable("periodo_academico");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.Anio).HasColumnName("anio").IsRequired();
        builder.Property(x => x.NumeroPeriodo).HasColumnName("numero_periodo").IsRequired();
        builder.Property(x => x.EtiquetaPeriodo).HasColumnName("etiqueta_periodo").HasMaxLength(40).IsRequired();
        builder.Property(x => x.FechaInicio).HasColumnName("fecha_inicio");
        builder.Property(x => x.FechaFin).HasColumnName("fecha_fin");

        builder.HasIndex(x => new { x.Anio, x.NumeroPeriodo }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<PeriodoAcademico> builder)
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

internal sealed class EscenarioProyeccionConfiguracion : IEntityTypeConfiguration<EscenarioProyeccion>
{
    public void Configure(EntityTypeBuilder<EscenarioProyeccion> builder)
    {
        builder.ToTable("escenario_proyeccion");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(500);
        builder.Property(x => x.EsPredeterminado).HasColumnName("es_predeterminado").IsRequired();

        builder.HasOne(x => x.Carrera)
            .WithMany(x => x.EscenariosProyeccion)
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CarreraId, x.Nombre }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<EscenarioProyeccion> builder)
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
