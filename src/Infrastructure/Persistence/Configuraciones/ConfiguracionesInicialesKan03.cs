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
        builder.Property(x => x.EstaActivo)
            .HasColumnName("esta_activo")
            .HasColumnType("integer")
            .HasConversion<int>()
            .IsRequired();
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
        builder.Property(x => x.EstaActivo)
            .HasColumnName("esta_activo")
            .HasColumnType("integer")
            .HasConversion<int>()
            .IsRequired();
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

internal sealed class ProyeccionEstudiantesConfiguracion : IEntityTypeConfiguration<ProyeccionEstudiantes>
{
    public void Configure(EntityTypeBuilder<ProyeccionEstudiantes> builder)
    {
        builder.ToTable("proyeccion_estudiantes");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.EscenarioProyeccionId).HasColumnName("escenario_proyeccion_id").IsRequired();
        builder.Property(x => x.AnioBase).HasColumnName("anio_base").IsRequired();
        builder.Property(x => x.SemanasPorSemestre).HasColumnName("semanas_por_semestre").IsRequired();

        builder.HasOne(x => x.Carrera)
            .WithMany()
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EscenarioProyeccion)
            .WithMany(x => x.ProyeccionesEstudiantes)
            .HasForeignKey(x => x.EscenarioProyeccionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CarreraId, x.EscenarioProyeccionId }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<ProyeccionEstudiantes> builder)
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

internal sealed class DetalleProyeccionEstudiantesConfiguracion : IEntityTypeConfiguration<DetalleProyeccionEstudiantes>
{
    public void Configure(EntityTypeBuilder<DetalleProyeccionEstudiantes> builder)
    {
        builder.ToTable("detalle_proyeccion_estudiantes");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.ProyeccionEstudiantesId).HasColumnName("proyeccion_estudiantes_id").IsRequired();
        builder.Property(x => x.PeriodoAcademicoId).HasColumnName("periodo_academico_id").IsRequired();
        builder.Property(x => x.NumeroCiclo).HasColumnName("numero_ciclo").IsRequired();
        builder.Property(x => x.CantidadParalelos).HasColumnName("cantidad_paralelos").IsRequired();
        builder.Property(x => x.TotalEstudiantes).HasColumnName("total_estudiantes").HasColumnType("decimal(9,4)").IsRequired();

        builder.HasOne(x => x.ProyeccionEstudiantes)
            .WithMany(x => x.DetallesProyeccionEstudiantes)
            .HasForeignKey(x => x.ProyeccionEstudiantesId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PeriodoAcademico)
            .WithMany()
            .HasForeignKey(x => x.PeriodoAcademicoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProyeccionEstudiantesId, x.PeriodoAcademicoId, x.NumeroCiclo }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<DetalleProyeccionEstudiantes> builder)
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

internal sealed class ConfiguracionCargaDocenteConfiguracion : IEntityTypeConfiguration<ConfiguracionCargaDocente>
{
    public void Configure(EntityTypeBuilder<ConfiguracionCargaDocente> builder)
    {
        builder.ToTable("configuracion_carga_docente");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.ProyeccionEstudiantesId).HasColumnName("proyeccion_estudiantes_id").IsRequired();
        builder.Property(x => x.HorasDocenciaEstandar).HasColumnName("horas_docencia_estandar").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.HorasTecnicoEstandar).HasColumnName("horas_tecnico_estandar").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.ProporcionPhdPorcentaje).HasColumnName("proporcion_phd_porcentaje").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.ProporcionMgsPorcentaje).HasColumnName("proporcion_mgs_porcentaje").HasColumnType("decimal(9,4)").IsRequired();

        builder.HasOne(x => x.ProyeccionEstudiantes)
            .WithMany(x => x.ConfiguracionesCargaDocente)
            .HasForeignKey(x => x.ProyeccionEstudiantesId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<ConfiguracionCargaDocente> builder)
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

internal sealed class ProyeccionRequerimientoDocenteConfiguracion : IEntityTypeConfiguration<ProyeccionRequerimientoDocente>
{
    public void Configure(EntityTypeBuilder<ProyeccionRequerimientoDocente> builder)
    {
        builder.ToTable("proyeccion_requerimiento_docente");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.ConfiguracionCargaDocenteId).HasColumnName("configuracion_carga_docente_id").IsRequired();
        builder.Property(x => x.PeriodoAcademicoId).HasColumnName("periodo_academico_id").IsRequired();
        builder.Property(x => x.TotalDocentes).HasColumnName("total_docentes").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.DocentesPhd).HasColumnName("docentes_phd").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.DocentesMgs).HasColumnName("docentes_mgs").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.DocentesParcial).HasColumnName("docentes_parcial").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.DocentesTecnico).HasColumnName("docentes_tecnico").HasColumnType("decimal(9,4)").IsRequired();

        builder.HasOne(x => x.ConfiguracionCargaDocente)
            .WithMany(x => x.ProyeccionesRequerimientoDocente)
            .HasForeignKey(x => x.ConfiguracionCargaDocenteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PeriodoAcademico)
            .WithMany()
            .HasForeignKey(x => x.PeriodoAcademicoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ConfiguracionCargaDocenteId, x.PeriodoAcademicoId }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<ProyeccionRequerimientoDocente> builder)
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

internal sealed class CargoFacultadConfiguracion : IEntityTypeConfiguration<CargoFacultad>
{
    public void Configure(EntityTypeBuilder<CargoFacultad> builder)
    {
        builder.ToTable("cargo_facultad");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.NombreCargo).HasColumnName("nombre_cargo").HasMaxLength(120).IsRequired();
        builder.Property(x => x.TipoCargo).HasColumnName("tipo_cargo").HasMaxLength(60).IsRequired();
        builder.Property(x => x.SueldoBaseMensual).HasColumnName("sueldo_base_mensual").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.EsCargoDocente).HasColumnName("es_cargo_docente").IsRequired();

        builder.HasOne(x => x.Carrera)
            .WithMany(x => x.CargosFacultad)
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CarreraId, x.NombreCargo }).IsUnique();
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

        builder.Property(x => x.CargoFacultadId).HasColumnName("cargo_facultad_id").IsRequired();
        builder.Property(x => x.PeriodoAcademicoId).HasColumnName("periodo_academico_id").IsRequired();
        builder.Property(x => x.CantidadPersonas).HasColumnName("cantidad_personas").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.FactorPonderacion).HasColumnName("factor_ponderacion").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.FactorInflacion).HasColumnName("factor_inflacion").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.CostoTotalSemestre).HasColumnName("costo_total_semestre").HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(x => x.CargoFacultad)
            .WithMany(x => x.ProyeccionesCargoFacultad)
            .HasForeignKey(x => x.CargoFacultadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PeriodoAcademico)
            .WithMany()
            .HasForeignKey(x => x.PeriodoAcademicoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CargoFacultadId, x.PeriodoAcademicoId }).IsUnique();
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

internal sealed class CargoPlantaCentralConfiguracion : IEntityTypeConfiguration<CargoPlantaCentral>
{
    public void Configure(EntityTypeBuilder<CargoPlantaCentral> builder)
    {
        builder.ToTable("cargo_planta_central");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.NombreCargo).HasColumnName("nombre_cargo").HasMaxLength(120).IsRequired();
        builder.Property(x => x.SueldoMensualTotal).HasColumnName("sueldo_mensual_total").HasColumnType("decimal(18,2)").IsRequired();

        builder.HasIndex(x => x.NombreCargo).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<CargoPlantaCentral> builder)
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

internal sealed class ProyeccionCargoPlantaCentralConfiguracion : IEntityTypeConfiguration<ProyeccionCargoPlantaCentral>
{
    public void Configure(EntityTypeBuilder<ProyeccionCargoPlantaCentral> builder)
    {
        builder.ToTable("proyeccion_cargo_planta_central");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.CargoPlantaCentralId).HasColumnName("cargo_planta_central_id").IsRequired();
        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.PeriodoAcademicoId).HasColumnName("periodo_academico_id").IsRequired();
        builder.Property(x => x.ProporcionAsignacion).HasColumnName("proporcion_asignacion").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.CostoTotalSemestre).HasColumnName("costo_total_semestre").HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(x => x.CargoPlantaCentral)
            .WithMany(x => x.ProyeccionesCargoPlantaCentral)
            .HasForeignKey(x => x.CargoPlantaCentralId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Carrera)
            .WithMany()
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PeriodoAcademico)
            .WithMany()
            .HasForeignKey(x => x.PeriodoAcademicoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CargoPlantaCentralId, x.CarreraId, x.PeriodoAcademicoId }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<ProyeccionCargoPlantaCentral> builder)
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

internal sealed class ConfiguracionArancelConfiguracion : IEntityTypeConfiguration<ConfiguracionArancel>
{
    public void Configure(EntityTypeBuilder<ConfiguracionArancel> builder)
    {
        builder.ToTable("configuracion_arancel");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.EscenarioProyeccionId).HasColumnName("escenario_proyeccion_id").IsRequired();
        builder.Property(x => x.ValorArancel).HasColumnName("valor_arancel").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ValorMatricula).HasColumnName("valor_matricula").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TipoOrigen).HasColumnName("tipo_origen").HasMaxLength(60).IsRequired();

        builder.HasOne(x => x.Carrera)
            .WithMany(x => x.ConfiguracionesArancel)
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EscenarioProyeccion)
            .WithMany(x => x.ConfiguracionesArancel)
            .HasForeignKey(x => x.EscenarioProyeccionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CarreraId, x.EscenarioProyeccionId }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<ConfiguracionArancel> builder)
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

internal sealed class PresupuestoInstitucionalConfiguracion : IEntityTypeConfiguration<PresupuestoInstitucional>
{
    public void Configure(EntityTypeBuilder<PresupuestoInstitucional> builder)
    {
        builder.ToTable("presupuesto_institucional");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.TipoPresupuesto).HasColumnName("tipo_presupuesto").HasMaxLength(80).IsRequired();
        builder.Property(x => x.ValorAnualBase).HasColumnName("valor_anual_base").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.AjustablePorInflacion).HasColumnName("ajustable_por_inflacion").IsRequired();

        builder.HasIndex(x => x.TipoPresupuesto).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<PresupuestoInstitucional> builder)
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

internal sealed class ItemMaterialInsumoConfiguracion : IEntityTypeConfiguration<ItemMaterialInsumo>
{
    public void Configure(EntityTypeBuilder<ItemMaterialInsumo> builder)
    {
        builder.ToTable("item_material_insumo");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.NombreItem).HasColumnName("nombre_item").HasMaxLength(140).IsRequired();
        builder.Property(x => x.CategoriaNombre).HasColumnName("categoria_nombre").HasMaxLength(100).IsRequired();
        builder.Property(x => x.UnidadNombre).HasColumnName("unidad_nombre").HasMaxLength(60).IsRequired();
        builder.Property(x => x.CantidadBase).HasColumnName("cantidad_base").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.PrecioUnitario).HasColumnName("precio_unitario").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.EsCantidadFija).HasColumnName("es_cantidad_fija").IsRequired();

        builder.HasOne(x => x.Carrera)
            .WithMany(x => x.ItemsMaterialInsumo)
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CarreraId, x.NombreItem }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<ItemMaterialInsumo> builder)
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

internal sealed class ProyeccionMaterialInsumoConfiguracion : IEntityTypeConfiguration<ProyeccionMaterialInsumo>
{
    public void Configure(EntityTypeBuilder<ProyeccionMaterialInsumo> builder)
    {
        builder.ToTable("proyeccion_material_insumo");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.ItemMaterialInsumoId).HasColumnName("item_material_insumo_id").IsRequired();
        builder.Property(x => x.PeriodoAcademicoId).HasColumnName("periodo_academico_id").IsRequired();
        builder.Property(x => x.CantidadProyectada).HasColumnName("cantidad_proyectada").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.FactorInflacion).HasColumnName("factor_inflacion").HasColumnType("decimal(9,4)").IsRequired();
        builder.Property(x => x.CostoTotalProyectado).HasColumnName("costo_total_proyectado").HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(x => x.ItemMaterialInsumo)
            .WithMany(x => x.ProyeccionesMaterialInsumo)
            .HasForeignKey(x => x.ItemMaterialInsumoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PeriodoAcademico)
            .WithMany(x => x.ProyeccionesMaterialInsumo)
            .HasForeignKey(x => x.PeriodoAcademicoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ItemMaterialInsumoId, x.PeriodoAcademicoId }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<ProyeccionMaterialInsumo> builder)
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

internal sealed class ResumenProyeccionFinancieraConfiguracion : IEntityTypeConfiguration<ResumenProyeccionFinanciera>
{
    public void Configure(EntityTypeBuilder<ResumenProyeccionFinanciera> builder)
    {
        builder.ToTable("resumen_proyeccion_financiera");
        ConfigurarCamposAuditoria(builder);

        builder.Property(x => x.CarreraId).HasColumnName("carrera_id").IsRequired();
        builder.Property(x => x.EscenarioProyeccionId).HasColumnName("escenario_proyeccion_id").IsRequired();
        builder.Property(x => x.PeriodoAcademicoId).HasColumnName("periodo_academico_id").IsRequired();
        builder.Property(x => x.IngresoTotal).HasColumnName("ingreso_total").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CostoServiciosTotal).HasColumnName("costo_servicios_total").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.GastoAdministrativoTotal).HasColumnName("gasto_administrativo_total").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.GastoVentasTotal).HasColumnName("gasto_ventas_total").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.OtrosGastosTotal).HasColumnName("otros_gastos_total").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.GastoFinancieroTotal).HasColumnName("gasto_financiero_total").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ResultadoNetoTotal).HasColumnName("resultado_neto_total").HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(x => x.Carrera)
            .WithMany(x => x.ResumenesProyeccionFinanciera)
            .HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.EscenarioProyeccion)
            .WithMany(x => x.ResumenesProyeccionFinanciera)
            .HasForeignKey(x => x.EscenarioProyeccionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PeriodoAcademico)
            .WithMany(x => x.ResumenesProyeccionFinanciera)
            .HasForeignKey(x => x.PeriodoAcademicoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CarreraId, x.EscenarioProyeccionId, x.PeriodoAcademicoId }).IsUnique();
    }

    private static void ConfigurarCamposAuditoria(EntityTypeBuilder<ResumenProyeccionFinanciera> builder)
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

internal sealed class UsuarioPermisoOverrideConfiguracion : IEntityTypeConfiguration<UsuarioPermisoOverride>
{
    public void Configure(EntityTypeBuilder<UsuarioPermisoOverride> builder)
    {
        builder.ToTable("usuario_permiso_override");
        builder.HasKey(x => new { x.UsuarioId, x.PermisoId });

        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        builder.Property(x => x.PermisoId).HasColumnName("permiso_id");
        builder.Property(x => x.Concedido).HasColumnName("concedido").IsRequired();
        builder.Property(x => x.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(x => x.CreadoPorUsuarioId).HasColumnName("creado_por_usuario_id");

        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Permiso)
            .WithMany()
            .HasForeignKey(x => x.PermisoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
