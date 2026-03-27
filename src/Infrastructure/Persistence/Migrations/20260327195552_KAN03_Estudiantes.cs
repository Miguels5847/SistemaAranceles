using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN03_Estudiantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "proyeccion_estudiantes",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    carrera_id = table.Column<int>(type: "INTEGER", nullable: false),
                    escenario_proyeccion_id = table.Column<int>(type: "INTEGER", nullable: false),
                    anio_base = table.Column<int>(type: "INTEGER", nullable: false),
                    semanas_por_semestre = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_proyeccion_estudiantes", x => x.id);
                    table.ForeignKey(
                        name: "FK_proyeccion_estudiantes_carrera_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proyeccion_estudiantes_escenario_proyeccion_escenario_proyeccion_id",
                        column: x => x.escenario_proyeccion_id,
                        principalTable: "escenario_proyeccion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "configuracion_carga_docente",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    proyeccion_estudiantes_id = table.Column<int>(type: "INTEGER", nullable: false),
                    horas_docencia_estandar = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    horas_tecnico_estandar = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    proporcion_phd_porcentaje = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    proporcion_mgs_porcentaje = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
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
                    table.PrimaryKey("PK_configuracion_carga_docente", x => x.id);
                    table.ForeignKey(
                        name: "FK_configuracion_carga_docente_proyeccion_estudiantes_proyeccion_estudiantes_id",
                        column: x => x.proyeccion_estudiantes_id,
                        principalTable: "proyeccion_estudiantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detalle_proyeccion_estudiantes",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    proyeccion_estudiantes_id = table.Column<int>(type: "INTEGER", nullable: false),
                    periodo_academico_id = table.Column<int>(type: "INTEGER", nullable: false),
                    numero_ciclo = table.Column<int>(type: "INTEGER", nullable: false),
                    cantidad_paralelos = table.Column<int>(type: "INTEGER", nullable: false),
                    total_estudiantes = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
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
                    table.PrimaryKey("PK_detalle_proyeccion_estudiantes", x => x.id);
                    table.ForeignKey(
                        name: "FK_detalle_proyeccion_estudiantes_periodo_academico_periodo_academico_id",
                        column: x => x.periodo_academico_id,
                        principalTable: "periodo_academico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_detalle_proyeccion_estudiantes_proyeccion_estudiantes_proyeccion_estudiantes_id",
                        column: x => x.proyeccion_estudiantes_id,
                        principalTable: "proyeccion_estudiantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "proyeccion_requerimiento_docente",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    configuracion_carga_docente_id = table.Column<int>(type: "INTEGER", nullable: false),
                    periodo_academico_id = table.Column<int>(type: "INTEGER", nullable: false),
                    total_docentes = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    docentes_phd = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    docentes_mgs = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    docentes_parcial = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    docentes_tecnico = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
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
                    table.PrimaryKey("PK_proyeccion_requerimiento_docente", x => x.id);
                    table.ForeignKey(
                        name: "FK_proyeccion_requerimiento_docente_configuracion_carga_docente_configuracion_carga_docente_id",
                        column: x => x.configuracion_carga_docente_id,
                        principalTable: "configuracion_carga_docente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proyeccion_requerimiento_docente_periodo_academico_periodo_academico_id",
                        column: x => x.periodo_academico_id,
                        principalTable: "periodo_academico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_carga_docente_proyeccion_estudiantes_id",
                table: "configuracion_carga_docente",
                column: "proyeccion_estudiantes_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_proyeccion_estudiantes_periodo_academico_id",
                table: "detalle_proyeccion_estudiantes",
                column: "periodo_academico_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_proyeccion_estudiantes_proyeccion_estudiantes_id_periodo_academico_id_numero_ciclo",
                table: "detalle_proyeccion_estudiantes",
                columns: new[] { "proyeccion_estudiantes_id", "periodo_academico_id", "numero_ciclo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_estudiantes_carrera_id_escenario_proyeccion_id",
                table: "proyeccion_estudiantes",
                columns: new[] { "carrera_id", "escenario_proyeccion_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_estudiantes_escenario_proyeccion_id",
                table: "proyeccion_estudiantes",
                column: "escenario_proyeccion_id");

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_requerimiento_docente_configuracion_carga_docente_id_periodo_academico_id",
                table: "proyeccion_requerimiento_docente",
                columns: new[] { "configuracion_carga_docente_id", "periodo_academico_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_requerimiento_docente_periodo_academico_id",
                table: "proyeccion_requerimiento_docente",
                column: "periodo_academico_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detalle_proyeccion_estudiantes");

            migrationBuilder.DropTable(
                name: "proyeccion_requerimiento_docente");

            migrationBuilder.DropTable(
                name: "configuracion_carga_docente");

            migrationBuilder.DropTable(
                name: "proyeccion_estudiantes");
        }
    }
}
