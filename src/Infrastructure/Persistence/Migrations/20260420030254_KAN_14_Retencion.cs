using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN_14_Retencion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cohorte_anio",
                table: "simulacion_retencion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "costo_matricula_promedio",
                table: "simulacion_retencion",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "estudiantes_graduados",
                table: "simulacion_retencion",
                type: "numeric(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "estudiantes_retenidos",
                table: "simulacion_retencion",
                type: "numeric(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "estudiantes_totales_inicio",
                table: "simulacion_retencion",
                type: "numeric(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_simulacion",
                table: "simulacion_retencion",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<decimal>(
                name: "graduacion_porcentaje_final",
                table: "simulacion_retencion",
                type: "numeric(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "retencion_porcentaje_final",
                table: "simulacion_retencion",
                type: "numeric(9,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "anio_academico",
                table: "detalle_simulacion_retencion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ciclo",
                table: "detalle_simulacion_retencion",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "costo_matricula_proyectado",
                table: "detalle_simulacion_retencion",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "estudiantes_graduados",
                table: "detalle_simulacion_retencion",
                type: "numeric(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "estudiantes_inicio",
                table: "detalle_simulacion_retencion",
                type: "numeric(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "estudiantes_reprobados",
                table: "detalle_simulacion_retencion",
                type: "numeric(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "estudiantes_retenidos",
                table: "detalle_simulacion_retencion",
                type: "numeric(14,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
                UPDATE detalle_simulacion_retencion
                SET ciclo = COALESCE(numero_ciclo, numero_periodo, 0),
                    estudiantes_inicio = COALESCE(valor_estudiantes, 0),
                    estudiantes_retenidos = COALESCE(valor_estudiantes, 0),
                    estudiantes_reprobados = 0,
                    estudiantes_graduados = 0,
                    costo_matricula_proyectado = 0,
                    anio_academico = EXTRACT(YEAR FROM CURRENT_TIMESTAMP)::integer
                WHERE ciclo = 0;
            ");

            migrationBuilder.Sql(@"
                UPDATE simulacion_retencion
                SET fecha_simulacion = CURRENT_TIMESTAMP,
                    cohorte_anio = EXTRACT(YEAR FROM CURRENT_TIMESTAMP)::integer
                WHERE cohorte_anio = 0;
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_simulacion_retencion_configuracion_retencion_id_cohorte_anio""
                ON simulacion_retencion(configuracion_retencion_id, cohorte_anio);
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_detalle_simulacion_retencion_simulacion_retencion_id_ciclo""
                ON detalle_simulacion_retencion(simulacion_retencion_id, ciclo);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_simulacion_retencion_configuracion_retencion_id_cohorte_anio\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_detalle_simulacion_retencion_simulacion_retencion_id_ciclo\";");

            migrationBuilder.DropColumn(name: "cohorte_anio", table: "simulacion_retencion");
            migrationBuilder.DropColumn(name: "costo_matricula_promedio", table: "simulacion_retencion");
            migrationBuilder.DropColumn(name: "estudiantes_graduados", table: "simulacion_retencion");
            migrationBuilder.DropColumn(name: "estudiantes_retenidos", table: "simulacion_retencion");
            migrationBuilder.DropColumn(name: "estudiantes_totales_inicio", table: "simulacion_retencion");
            migrationBuilder.DropColumn(name: "fecha_simulacion", table: "simulacion_retencion");
            migrationBuilder.DropColumn(name: "graduacion_porcentaje_final", table: "simulacion_retencion");
            migrationBuilder.DropColumn(name: "retencion_porcentaje_final", table: "simulacion_retencion");

            migrationBuilder.DropColumn(name: "anio_academico", table: "detalle_simulacion_retencion");
            migrationBuilder.DropColumn(name: "ciclo", table: "detalle_simulacion_retencion");
            migrationBuilder.DropColumn(name: "costo_matricula_proyectado", table: "detalle_simulacion_retencion");
            migrationBuilder.DropColumn(name: "estudiantes_graduados", table: "detalle_simulacion_retencion");
            migrationBuilder.DropColumn(name: "estudiantes_inicio", table: "detalle_simulacion_retencion");
            migrationBuilder.DropColumn(name: "estudiantes_reprobados", table: "detalle_simulacion_retencion");
            migrationBuilder.DropColumn(name: "estudiantes_retenidos", table: "detalle_simulacion_retencion");
        }
    }
}
