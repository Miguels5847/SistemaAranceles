using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN03_InflacionRetencion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracion_retencion",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    carrera_id = table.Column<int>(type: "INTEGER", nullable: false),
                    escenario_proyeccion_id = table.Column<int>(type: "INTEGER", nullable: false),
                    total_ciclos = table.Column<int>(type: "INTEGER", nullable: false),
                    tasa_retencion_porcentaje = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    tasa_graduacion_porcentaje = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    estudiantes_periodo_1 = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    estudiantes_periodo_2 = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    paralelos_periodo_1 = table.Column<int>(type: "INTEGER", nullable: false),
                    paralelos_periodo_2 = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_configuracion_retencion", x => x.id);
                    table.ForeignKey(
                        name: "FK_configuracion_retencion_carrera_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_configuracion_retencion_escenario_proyeccion_escenario_proyeccion_id",
                        column: x => x.escenario_proyeccion_id,
                        principalTable: "escenario_proyeccion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inflacion_anual",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    anio = table.Column<int>(type: "INTEGER", nullable: false),
                    porcentaje_inflacion = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    fuente_nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    tipo_fuente = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
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
                    table.PrimaryKey("PK_inflacion_anual", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inflacion_proyectada",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    escenario_proyeccion_id = table.Column<int>(type: "INTEGER", nullable: false),
                    anio = table.Column<int>(type: "INTEGER", nullable: false),
                    porcentaje_inflacion = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    metodo_proyeccion = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    es_ajuste_manual = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_inflacion_proyectada", x => x.id);
                    table.ForeignKey(
                        name: "FK_inflacion_proyectada_escenario_proyeccion_escenario_proyeccion_id",
                        column: x => x.escenario_proyeccion_id,
                        principalTable: "escenario_proyeccion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "criterio_referencia_retencion",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    configuracion_retencion_id = table.Column<int>(type: "INTEGER", nullable: false),
                    meta_retencion_porcentaje = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    meta_graduacion_porcentaje = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
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
                    table.PrimaryKey("PK_criterio_referencia_retencion", x => x.id);
                    table.ForeignKey(
                        name: "FK_criterio_referencia_retencion_configuracion_retencion_configuracion_retencion_id",
                        column: x => x.configuracion_retencion_id,
                        principalTable: "configuracion_retencion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "simulacion_retencion",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    configuracion_retencion_id = table.Column<int>(type: "INTEGER", nullable: false),
                    ejecutado_en = table.Column<DateTime>(type: "TEXT", nullable: false),
                    notas = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_simulacion_retencion", x => x.id);
                    table.ForeignKey(
                        name: "FK_simulacion_retencion_configuracion_retencion_configuracion_retencion_id",
                        column: x => x.configuracion_retencion_id,
                        principalTable: "configuracion_retencion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "detalle_simulacion_retencion",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    simulacion_retencion_id = table.Column<int>(type: "INTEGER", nullable: false),
                    numero_ciclo = table.Column<int>(type: "INTEGER", nullable: false),
                    numero_periodo = table.Column<int>(type: "INTEGER", nullable: false),
                    valor_estudiantes = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    tasa_aplicada_porcentaje = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    tipo_zona = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_detalle_simulacion_retencion", x => x.id);
                    table.ForeignKey(
                        name: "FK_detalle_simulacion_retencion_simulacion_retencion_simulacion_retencion_id",
                        column: x => x.simulacion_retencion_id,
                        principalTable: "simulacion_retencion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_retencion_carrera_id_escenario_proyeccion_id",
                table: "configuracion_retencion",
                columns: new[] { "carrera_id", "escenario_proyeccion_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_retencion_escenario_proyeccion_id",
                table: "configuracion_retencion",
                column: "escenario_proyeccion_id");

            migrationBuilder.CreateIndex(
                name: "IX_criterio_referencia_retencion_configuracion_retencion_id",
                table: "criterio_referencia_retencion",
                column: "configuracion_retencion_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detalle_simulacion_retencion_simulacion_retencion_id_numero_ciclo_numero_periodo",
                table: "detalle_simulacion_retencion",
                columns: new[] { "simulacion_retencion_id", "numero_ciclo", "numero_periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inflacion_anual_anio",
                table: "inflacion_anual",
                column: "anio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inflacion_proyectada_escenario_proyeccion_id_anio",
                table: "inflacion_proyectada",
                columns: new[] { "escenario_proyeccion_id", "anio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_simulacion_retencion_configuracion_retencion_id_ejecutado_en",
                table: "simulacion_retencion",
                columns: new[] { "configuracion_retencion_id", "ejecutado_en" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "criterio_referencia_retencion");

            migrationBuilder.DropTable(
                name: "detalle_simulacion_retencion");

            migrationBuilder.DropTable(
                name: "inflacion_anual");

            migrationBuilder.DropTable(
                name: "inflacion_proyectada");

            migrationBuilder.DropTable(
                name: "simulacion_retencion");

            migrationBuilder.DropTable(
                name: "configuracion_retencion");
        }
    }
}
