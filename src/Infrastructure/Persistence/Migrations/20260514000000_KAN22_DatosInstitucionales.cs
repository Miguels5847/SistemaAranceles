using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN22_DatosInstitucionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS public.datos_institucionales (
                    id                          SERIAL PRIMARY KEY,
                    periodo                     VARCHAR(20) NOT NULL,
                    n_estudiantes_universidad   INTEGER NOT NULL,
                    n_docentes_universidad      INTEGER NOT NULL,
                    n_personas_planta_central   INTEGER NOT NULL,
                    sueldo_basico               NUMERIC(18,2) NOT NULL,
                    funcional                   NUMERIC(18,2) NOT NULL,
                    fondo_reserva               NUMERIC(18,2) NOT NULL,
                    beneficio_xiv               NUMERIC(18,2) NOT NULL,
                    beneficio_xiii              NUMERIC(18,2) NOT NULL,
                    aporte_patronal             NUMERIC(18,2) NOT NULL,
                    varios                      NUMERIC(18,2) NOT NULL,
                    meses_capital_trabajo       INTEGER NOT NULL DEFAULT 2,
                    porcentaje_imprevistos_inversion NUMERIC(7,4) NOT NULL DEFAULT 5.0000,
                    fecha_actualizacion         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    actualizado_por_usuario_id  INTEGER NOT NULL REFERENCES public.usuario(id) ON DELETE RESTRICT,
                    fuente_notas                VARCHAR(500)
                );

                ALTER TABLE public.datos_institucionales
                    ADD COLUMN IF NOT EXISTS meses_capital_trabajo INTEGER NOT NULL DEFAULT 2;

                ALTER TABLE public.datos_institucionales
                    ADD COLUMN IF NOT EXISTS porcentaje_imprevistos_inversion NUMERIC(7,4) NOT NULL DEFAULT 5.0000;

                ALTER TABLE public.datos_institucionales
                    ALTER COLUMN meses_capital_trabajo SET DEFAULT 2,
                    ALTER COLUMN meses_capital_trabajo SET NOT NULL,
                    ALTER COLUMN porcentaje_imprevistos_inversion SET DEFAULT 5.0000,
                    ALTER COLUMN porcentaje_imprevistos_inversion SET NOT NULL;

                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_datos_institucionales_periodo""
                    ON public.datos_institucionales (periodo);

                CREATE INDEX IF NOT EXISTS ""IX_datos_institucionales_actualizado_por_usuario_id""
                    ON public.datos_institucionales (actualizado_por_usuario_id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS public.datos_institucionales;");
        }
    }
}
