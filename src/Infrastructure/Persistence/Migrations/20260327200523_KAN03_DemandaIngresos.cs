using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN03_DemandaIngresos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuracion_arancel",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    carrera_id = table.Column<int>(type: "INTEGER", nullable: false),
                    escenario_proyeccion_id = table.Column<int>(type: "INTEGER", nullable: false),
                    valor_arancel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    valor_matricula = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    tipo_origen = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
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
                    table.PrimaryKey("PK_configuracion_arancel", x => x.id);
                    table.ForeignKey(
                        name: "FK_configuracion_arancel_carrera_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_configuracion_arancel_escenario_proyeccion_escenario_proyeccion_id",
                        column: x => x.escenario_proyeccion_id,
                        principalTable: "escenario_proyeccion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "item_material_insumo",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    carrera_id = table.Column<int>(type: "INTEGER", nullable: false),
                    nombre_item = table.Column<string>(type: "TEXT", maxLength: 140, nullable: false),
                    categoria_nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    unidad_nombre = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    cantidad_base = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    precio_unitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    es_cantidad_fija = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_item_material_insumo", x => x.id);
                    table.ForeignKey(
                        name: "FK_item_material_insumo_carrera_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carrera",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "presupuesto_institucional",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    tipo_presupuesto = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    valor_anual_base = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ajustable_por_inflacion = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_presupuesto_institucional", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "proyeccion_material_insumo",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    item_material_insumo_id = table.Column<int>(type: "INTEGER", nullable: false),
                    periodo_academico_id = table.Column<int>(type: "INTEGER", nullable: false),
                    cantidad_proyectada = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    factor_inflacion = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    costo_total_proyectado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_proyeccion_material_insumo", x => x.id);
                    table.ForeignKey(
                        name: "FK_proyeccion_material_insumo_item_material_insumo_item_material_insumo_id",
                        column: x => x.item_material_insumo_id,
                        principalTable: "item_material_insumo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proyeccion_material_insumo_periodo_academico_periodo_academico_id",
                        column: x => x.periodo_academico_id,
                        principalTable: "periodo_academico",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_arancel_carrera_id_escenario_proyeccion_id",
                table: "configuracion_arancel",
                columns: new[] { "carrera_id", "escenario_proyeccion_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_configuracion_arancel_escenario_proyeccion_id",
                table: "configuracion_arancel",
                column: "escenario_proyeccion_id");

            migrationBuilder.CreateIndex(
                name: "IX_item_material_insumo_carrera_id_nombre_item",
                table: "item_material_insumo",
                columns: new[] { "carrera_id", "nombre_item" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_presupuesto_institucional_tipo_presupuesto",
                table: "presupuesto_institucional",
                column: "tipo_presupuesto",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_material_insumo_item_material_insumo_id_periodo_academico_id",
                table: "proyeccion_material_insumo",
                columns: new[] { "item_material_insumo_id", "periodo_academico_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proyeccion_material_insumo_periodo_academico_id",
                table: "proyeccion_material_insumo",
                column: "periodo_academico_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuracion_arancel");

            migrationBuilder.DropTable(
                name: "presupuesto_institucional");

            migrationBuilder.DropTable(
                name: "proyeccion_material_insumo");

            migrationBuilder.DropTable(
                name: "item_material_insumo");
        }
    }
}
