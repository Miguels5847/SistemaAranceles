using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN_OverrideHorasPeriodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS public.override_horas_periodo (
                    id                          SERIAL PRIMARY KEY,
                    proyeccion_id               INTEGER NOT NULL REFERENCES public.proyeccion_estudiantes(id) ON DELETE CASCADE,
                    periodo                     SMALLINT NOT NULL CHECK (periodo BETWEEN 1 AND 20),
                    horas_docencia              NUMERIC(10,2),
                    horas_practica              NUMERIC(10,2),
                    creado_en                   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    creado_por_usuario_id       INTEGER,
                    actualizado_en              TIMESTAMPTZ,
                    actualizado_por_usuario_id  INTEGER,
                    esta_activo                 BOOLEAN NOT NULL DEFAULT TRUE,
                    eliminado_en                TIMESTAMPTZ,
                    eliminado_por_usuario_id    INTEGER
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_override_horas_periodo_proyeccion_id_periodo""
                    ON public.override_horas_periodo (proyeccion_id, periodo);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.override_horas_periodo;");
        }
    }
}
