using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN30_DatosInstitucionalesParametrosInversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE public.datos_institucionales
                    ADD COLUMN IF NOT EXISTS meses_capital_trabajo INTEGER NOT NULL DEFAULT 2;

                ALTER TABLE public.datos_institucionales
                    ADD COLUMN IF NOT EXISTS porcentaje_imprevistos_inversion NUMERIC(7,4) NOT NULL DEFAULT 5.0000;

                UPDATE public.datos_institucionales
                   SET meses_capital_trabajo = 2
                 WHERE meses_capital_trabajo IS NULL;

                UPDATE public.datos_institucionales
                   SET porcentaje_imprevistos_inversion = 5.0000
                 WHERE porcentaje_imprevistos_inversion IS NULL;

                ALTER TABLE public.datos_institucionales
                    ALTER COLUMN meses_capital_trabajo SET DEFAULT 2,
                    ALTER COLUMN meses_capital_trabajo SET NOT NULL,
                    ALTER COLUMN porcentaje_imprevistos_inversion SET DEFAULT 5.0000,
                    ALTER COLUMN porcentaje_imprevistos_inversion SET NOT NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                          FROM pg_constraint
                         WHERE conname = 'CK_datos_institucionales_meses_capital_trabajo'
                    ) THEN
                        ALTER TABLE public.datos_institucionales
                            ADD CONSTRAINT "CK_datos_institucionales_meses_capital_trabajo"
                            CHECK (meses_capital_trabajo > 0);
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1
                          FROM pg_constraint
                         WHERE conname = 'CK_datos_institucionales_porcentaje_imprevistos_inversion'
                    ) THEN
                        ALTER TABLE public.datos_institucionales
                            ADD CONSTRAINT "CK_datos_institucionales_porcentaje_imprevistos_inversion"
                            CHECK (porcentaje_imprevistos_inversion >= 0 AND porcentaje_imprevistos_inversion <= 100);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE public.datos_institucionales
                    DROP CONSTRAINT IF EXISTS "CK_datos_institucionales_porcentaje_imprevistos_inversion";

                ALTER TABLE public.datos_institucionales
                    DROP CONSTRAINT IF EXISTS "CK_datos_institucionales_meses_capital_trabajo";

                ALTER TABLE public.datos_institucionales
                    DROP COLUMN IF EXISTS porcentaje_imprevistos_inversion;

                ALTER TABLE public.datos_institucionales
                    DROP COLUMN IF EXISTS meses_capital_trabajo;
                """);
        }
    }
}
