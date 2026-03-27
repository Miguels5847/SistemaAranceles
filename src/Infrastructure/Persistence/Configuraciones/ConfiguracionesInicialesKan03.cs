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

internal sealed class InflacionAnualConfiguracion : IEntityTypeConfiguration<InflacionAnual>
{
    public void Configure(EntityTypeBuilder<InflacionAnual> builder)
    {
        builder.ToTable("inflacion_anual");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.Anio).HasColumnName("anio").IsRequired();
        builder.Property(x => x.PorcentajeInflacion).HasColumnName("porcentaje_inflacion").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.FuenteNombre).HasColumnName("fuente_nombre").HasMaxLength(100).IsRequired();
        builder.Property(x => x.TipoFuente).HasColumnName("tipo_fuente").HasMaxLength(60).IsRequired();

        builder.HasIndex(x => x.Anio).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<InflacionAnual> builder)
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

internal sealed class InflacionProyectadaConfiguracion : IEntityTypeConfiguration<InflacionProyectada>
{
    public void Configure(EntityTypeBuilder<InflacionProyectada> builder)
    {
        builder.ToTable("inflacion_proyectada");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.EscenarioProyeccionId).HasColumnName("escenario_proyeccion_id").IsRequired();
        builder.Property(x => x.Anio).HasColumnName("anio").IsRequired();
        builder.Property(x => x.PorcentajeInflacion).HasColumnName("porcentaje_inflacion").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.MetodoProyeccion).HasColumnName("metodo_proyeccion").HasMaxLength(80).IsRequired();
        builder.Property(x => x.EsAjusteManual).HasColumnName("es_ajuste_manual").IsRequired();

        builder.HasOne(x => x.EscenarioProyeccion)
            .WithMany(x => x.InflacionesProyectadas)
            .HasForeignKey(x => x.EscenarioProyeccionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.EscenarioProyeccionId, x.Anio }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<InflacionProyectada> builder)
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

internal sealed class ConfiguracionRetencionConfiguracion : IEntityTypeConfiguration<ConfiguracionRetencion>
{
    public void Configure(EntityTypeBuilder<ConfiguracionRetencion> builder)
    {
        builder.ToTable("configuracion_retencion");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.EscenarioProyeccionId).HasColumnName("escenario_proyeccion_id").IsRequired();
        builder.Property(x => x.TotalCiclos).HasColumnName("total_ciclos").IsRequired();
        builder.Property(x => x.TasaRetencionPorcentaje).HasColumnName("tasa_retencion_porcentaje").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.TasaGraduacionPorcentaje).HasColumnName("tasa_graduacion_porcentaje").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.EstudiantesPeriodo1).HasColumnName("estudiantes_periodo_1").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.EstudiantesPeriodo2).HasColumnName("estudiantes_periodo_2").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.ParalelosPeriodo1).HasColumnName("paralelos_periodo_1").IsRequired();
        builder.Property(x => x.ParalelosPeriodo2).HasColumnName("paralelos_periodo_2").IsRequired();

        builder.HasOne(x => x.Carrera)
            .WithMany()
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EscenarioProyeccion)
            .WithMany(x => x.ConfiguracionesRetencion)
            .HasForeignKey(x => x.EscenarioProyeccionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CarreraId, x.EscenarioProyeccionId }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<ConfiguracionRetencion> builder)
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

internal sealed class CriterioReferenciaRetencionConfiguracion : IEntityTypeConfiguration<CriterioReferenciaRetencion>
{
    public void Configure(EntityTypeBuilder<CriterioReferenciaRetencion> builder)
    {
        builder.ToTable("criterio_referencia_retencion");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.ConfiguracionRetencionId).HasColumnName("configuracion_retencion_id").IsRequired();
        builder.Property(x => x.MetaRetencionPorcentaje).HasColumnName("meta_retencion_porcentaje").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.MetaGraduacionPorcentaje).HasColumnName("meta_graduacion_porcentaje").HasColumnType("decimal(9,4)").IsRequired();

        builder.HasOne(x => x.ConfiguracionRetencion)
            .WithOne(x => x.CriterioReferenciaRetencion)
            .HasForeignKey<CriterioReferenciaRetencion>(x => x.ConfiguracionRetencionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ConfiguracionRetencionId).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<CriterioReferenciaRetencion> builder)
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

internal sealed class SimulacionRetencionConfiguracion : IEntityTypeConfiguration<SimulacionRetencion>
{
    public void Configure(EntityTypeBuilder<SimulacionRetencion> builder)
    {
        builder.ToTable("simulacion_retencion");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.ConfiguracionRetencionId).HasColumnName("configuracion_retencion_id").IsRequired();
        builder.Property(x => x.EjecutadoEn).HasColumnName("ejecutado_en").IsRequired();
        builder.Property(x => x.Notas).HasColumnName("notas").HasMaxLength(500);

        builder.HasOne(x => x.ConfiguracionRetencion)
            .WithMany(x => x.SimulacionesRetencion)
            .HasForeignKey(x => x.ConfiguracionRetencionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ConfiguracionRetencionId, x.EjecutadoEn });
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<SimulacionRetencion> builder)
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

internal sealed class DetalleSimulacionRetencionConfiguracion : IEntityTypeConfiguration<DetalleSimulacionRetencion>
{
    public void Configure(EntityTypeBuilder<DetalleSimulacionRetencion> builder)
    {
        builder.ToTable("detalle_simulacion_retencion");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.SimulacionRetencionId).HasColumnName("simulacion_retencion_id").IsRequired();
        builder.Property(x => x.NumeroCiclo).HasColumnName("numero_ciclo").IsRequired();
        builder.Property(x => x.NumeroPeriodo).HasColumnName("numero_periodo").IsRequired();
        builder.Property(x => x.ValorEstudiantes).HasColumnName("valor_estudiantes").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.TasaAplicadaPorcentaje).HasColumnName("tasa_aplicada_porcentaje").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.TipoZona).HasColumnName("tipo_zona").HasMaxLength(30).IsRequired();

        builder.HasOne(x => x.SimulacionRetencion)
            .WithMany(x => x.DetallesSimulacionRetencion)
            .HasForeignKey(x => x.SimulacionRetencionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.SimulacionRetencionId, x.NumeroCiclo, x.NumeroPeriodo }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<DetalleSimulacionRetencion> builder)
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
