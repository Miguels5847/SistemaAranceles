using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN03_SueldosPlantaCentral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cargo_facultad",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    carrera_id = table.Column<int>(type: "INTEGER", nullable: false),
                    nombre_cargo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    tipo_cargo = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    sueldo_base_mensual = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    es_cargo_docente = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_cargo_facultad", x => x.id);
                    table.ForeignKey(
                        name: "FK_cargo_facultad_carrera_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cargo_planta_central",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    nombre_cargo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    sueldo_mensual_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_cargo_planta_central", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "proyeccion_cargo_facultad",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    cargo_facultad_id = table.Column<int>(type: "INTEGER", nullable: false),
                    periodo_academico_id = table.Column<int>(type: "INTEGER", nullable: false),
                    cantidad_personas = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    factor_ponderacion = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    factor_inflacion = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    costo_total_semestre = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_proyeccion_cargo_facultad", x => x.id);
                    table.ForeignKey(
                        name: "FK_proyeccion_cargo_facultad_cargo_facultad_cargo_facultad_id",
                        column: x => x.cargo_facultad_id,
                        principalTable: "cargo_facultad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proyeccion_cargo_facultad_periodo_academico_periodo_academico_id",
                        column: x => x.periodo_academico_id,
                        principalTable: "periodo_academico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proyeccion_cargo_planta_central",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    cargo_planta_central_id = table.Column<int>(type: "INTEGER", nullable: false),
                    carrera_id = table.Column<int>(type: "INTEGER", nullable: false),
                    periodo_academico_id = table.Column<int>(type: "INTEGER", nullable: false),
                    proporcion_asignacion = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    costo_total_semestre = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_proyeccion_cargo_planta_central", x => x.id);
                    table.ForeignKey(
                        name: "FK_proyeccion_cargo_planta_central_cargo_planta_central_cargo_planta_central_id",
                        column: x => x.cargo_planta_central_id,
                        principalTable: "cargo_planta_central",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proyeccion_cargo_planta_central_carrera_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proyeccion_cargo_planta_central_periodo_academico_periodo_academico_id",
                        column: x => x.periodo_academico_id,
                        principalTable: "periodo_academico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cargo_facultad_carrera_id_nombre_cargo",
                table: "cargo_facultad",
                columns: new[] { "carrera_id", "nombre_cargo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cargo_planta_central_nombre_cargo",
                table: "cargo_planta_central",
                column: "nombre_cargo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_cargo_facultad_cargo_facultad_id_periodo_academico_id",
                table: "proyeccion_cargo_facultad",
                columns: new[] { "cargo_facultad_id", "periodo_academico_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_cargo_facultad_periodo_academico_id",
                table: "proyeccion_cargo_facultad",
                column: "periodo_academico_id");

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_cargo_planta_central_cargo_planta_central_id_carrera_id_periodo_academico_id",
                table: "proyeccion_cargo_planta_central",
                columns: new[] { "cargo_planta_central_id", "carrera_id", "periodo_academico_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_cargo_planta_central_carrera_id",
                table: "proyeccion_cargo_planta_central",
                column: "carrera_id");

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_cargo_planta_central_periodo_academico_id",
                table: "proyeccion_cargo_planta_central",
                column: "periodo_academico_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "proyeccion_cargo_facultad");

            migrationBuilder.DropTable(
                name: "proyeccion_cargo_planta_central");

            migrationBuilder.DropTable(
                name: "cargo_facultad");

            migrationBuilder.DropTable(
                name: "cargo_planta_central");
        }
    }
}
