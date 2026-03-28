using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN03_BaseSeguridadCatalogos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carrera",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    codigo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    facultad_nombre = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    total_ciclos = table.Column<int>(type: "INTEGER", nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    creado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    actualizado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    esta_activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    eliminado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carrera", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "periodo_academico",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    anio = table.Column<int>(type: "INTEGER", nullable: false),
                    numero_periodo = table.Column<int>(type: "INTEGER", nullable: false),
                    etiqueta_periodo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    fecha_fin = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    creado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    actualizado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    esta_activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    eliminado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periodo_academico", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permiso",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    codigo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    modulo_nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    accion_nombre = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    creado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    actualizado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    esta_activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    eliminado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permiso", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    creado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    actualizado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    esta_activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    eliminado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre_completo = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    correo_institucional = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    hash_contrasena = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    estado = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ultimo_acceso_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    creado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    actualizado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    esta_activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    eliminado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "escenario_proyeccion",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    carrera_id = table.Column<int>(type: "INTEGER", nullable: false),
                    nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    es_predeterminado = table.Column<bool>(type: "INTEGER", nullable: false),
                    creado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    creado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    actualizado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    actualizado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true),
                    esta_activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    eliminado_en = table.Column<DateTime>(type: "TEXT", nullable: true),
                    eliminado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_escenario_proyeccion", x => x.id);
                    table.ForeignKey(
                        name: "FK_escenario_proyeccion_carrera_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rol_permiso",
                columns: table => new
                {
                    rol_id = table.Column<int>(type: "INTEGER", nullable: false),
                    permiso_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rol_permiso", x => new { x.rol_id, x.permiso_id });
                    table.ForeignKey(
                        name: "FK_rol_permiso_permiso_permiso_id",
                        column: x => x.permiso_id,
                        principalTable: "permiso",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rol_permiso_rol_rol_id",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auditoria_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    evento_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    modulo_nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    entidad_nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    entidad_id = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    accion_nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    resumen_texto = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    valores_anteriores_json = table.Column<string>(type: "TEXT", nullable: true),
                    valores_nuevos_json = table.Column<string>(type: "TEXT", nullable: true),
                    ejecutado_por_usuario_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditoria_log", x => x.id);
                    table.ForeignKey(
                        name: "FK_auditoria_log_usuario_ejecutado_por_usuario_id",
                        column: x => x.ejecutado_por_usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "sesion_usuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    token_sesion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    emitido_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expira_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    revocado_en = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesion_usuario", x => x.id);
                    table.ForeignKey(
                        name: "FK_sesion_usuario_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "usuario_rol",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "INTEGER", nullable: false),
                    rol_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario_rol", x => new { x.usuario_id, x.rol_id });
                    table.ForeignKey(
                        name: "FK_usuario_rol_rol_rol_id",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_usuario_rol_usuario_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_log_ejecutado_por_usuario_id",
                table: "auditoria_log",
                column: "ejecutado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_auditoria_log_evento_en_modulo_nombre",
                table: "auditoria_log",
                columns: new[] { "evento_en", "modulo_nombre" });

            migrationBuilder.CreateIndex(
                name: "IX_carrera_codigo",
                table: "carrera",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_escenario_proyeccion_carrera_id_nombre",
                table: "escenario_proyeccion",
                columns: new[] { "carrera_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_periodo_academico_anio_numero_periodo",
                table: "periodo_academico",
                columns: new[] { "anio", "numero_periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permiso_codigo",
                table: "permiso",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rol_nombre",
                table: "rol",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rol_permiso_permiso_id",
                table: "rol_permiso",
                column: "permiso_id");

            migrationBuilder.CreateIndex(
                name: "IX_sesion_usuario_token_sesion",
                table: "sesion_usuario",
                column: "token_sesion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sesion_usuario_usuario_id",
                table: "sesion_usuario",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_usuario_correo_institucional",
                table: "usuario",
                column: "correo_institucional",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuario_rol_rol_id",
                table: "usuario_rol",
                column: "rol_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditoria_log");

            migrationBuilder.DropTable(
                name: "escenario_proyeccion");

            migrationBuilder.DropTable(
                name: "periodo_academico");

            migrationBuilder.DropTable(
                name: "rol_permiso");

            migrationBuilder.DropTable(
                name: "sesion_usuario");

            migrationBuilder.DropTable(
                name: "usuario_rol");

            migrationBuilder.DropTable(
                name: "carrera");

            migrationBuilder.DropTable(
                name: "permiso");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
