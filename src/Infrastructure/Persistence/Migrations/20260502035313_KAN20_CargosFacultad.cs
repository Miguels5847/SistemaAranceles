using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN20_CargosFacultad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ejecutado_en",
                table: "simulacion_retencion",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "numero_ciclo",
                table: "detalle_simulacion_retencion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "numero_periodo",
                table: "detalle_simulacion_retencion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "tasa_aplicada_porcentaje",
                table: "detalle_simulacion_retencion",
                type: "numeric(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "tipo_zona",
                table: "detalle_simulacion_retencion",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "valor_estudiantes",
                table: "detalle_simulacion_retencion",
                type: "numeric(9,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ejecutado_en",
                table: "simulacion_retencion");

            migrationBuilder.DropColumn(
                name: "numero_ciclo",
                table: "detalle_simulacion_retencion");

            migrationBuilder.DropColumn(
                name: "numero_periodo",
                table: "detalle_simulacion_retencion");

            migrationBuilder.DropColumn(
                name: "tasa_aplicada_porcentaje",
                table: "detalle_simulacion_retencion");

            migrationBuilder.DropColumn(
                name: "tipo_zona",
                table: "detalle_simulacion_retencion");

            migrationBuilder.DropColumn(
                name: "valor_estudiantes",
                table: "detalle_simulacion_retencion");
        }
    }
}
