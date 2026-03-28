using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN03_ResumenFinanciero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "resumen_proyeccion_financiera",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    carrera_id = table.Column<int>(type: "INTEGER", nullable: false),
                    escenario_proyeccion_id = table.Column<int>(type: "INTEGER", nullable: false),
                    periodo_academico_id = table.Column<int>(type: "INTEGER", nullable: false),
                    ingreso_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    costo_servicios_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    gasto_administrativo_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    gasto_ventas_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    otros_gastos_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    gasto_financiero_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    resultado_neto_total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_resumen_proyeccion_financiera", x => x.id);
                    table.ForeignKey(
                        name: "FK_resumen_proyeccion_financiera_carrera_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_resumen_proyeccion_financiera_escenario_proyeccion_escenario_proyeccion_id",
                        column: x => x.escenario_proyeccion_id,
                        principalTable: "escenario_proyeccion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_resumen_proyeccion_financiera_periodo_academico_periodo_academico_id",
                        column: x => x.periodo_academico_id,
                        principalTable: "periodo_academico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_resumen_proyeccion_financiera_carrera_id_escenario_proyeccion_id_periodo_academico_id",
                table: "resumen_proyeccion_financiera",
                columns: new[] { "carrera_id", "escenario_proyeccion_id", "periodo_academico_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resumen_proyeccion_financiera_escenario_proyeccion_id",
                table: "resumen_proyeccion_financiera",
                column: "escenario_proyeccion_id");

            migrationBuilder.CreateIndex(
                name: "IX_resumen_proyeccion_financiera_periodo_academico_id",
                table: "resumen_proyeccion_financiera",
                column: "periodo_academico_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resumen_proyeccion_financiera");
        }
    }
}
