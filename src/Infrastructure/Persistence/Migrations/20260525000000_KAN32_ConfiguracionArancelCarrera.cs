using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaAranceles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KAN32_ConfiguracionArancelCarrera : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS public.configuracion_arancel_carrera (
                    id                                       SERIAL PRIMARY KEY,
                    carrera_id                               INTEGER NOT NULL REFERENCES public.carrera(id) ON DELETE CASCADE,
                    escenario_proyeccion_id                  INTEGER NULL REFERENCES public.escenario_proyeccion(id) ON DELETE SET NULL,
                    modo_calculo_arancel                     VARCHAR(30) NOT NULL DEFAULT 'Manual',
                    arancel_manual                           NUMERIC(18,2) NULL,
                    porcentaje_matricula                     NUMERIC(7,4) NULL,
                    usa_porcentaje_matricula_institucional   BOOLEAN NOT NULL DEFAULT TRUE,
                    creado_en                                TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    creado_por_usuario_id                    INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL,
                    actualizado_en                           TIMESTAMPTZ NULL,
                    actualizado_por_usuario_id               INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL,
                    esta_activo                              BOOLEAN NOT NULL DEFAULT TRUE,
                    eliminado_en                             TIMESTAMPTZ NULL,
                    eliminado_por_usuario_id                 INTEGER NULL REFERENCES public.usuario(id) ON DELETE SET NULL
                );

                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'CK_configuracion_arancel_carrera_modo'
                    ) THEN
                        ALTER TABLE public.configuracion_arancel_carrera
                            ADD CONSTRAINT "CK_configuracion_arancel_carrera_modo"
                            CHECK (modo_calculo_arancel IN ('Manual', 'AutomaticoCostoCarrera'));
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'CK_configuracion_arancel_carrera_porc_matricula'
                    ) THEN
                        ALTER TABLE public.configuracion_arancel_carrera
                            ADD CONSTRAINT "CK_configuracion_arancel_carrera_porc_matricula"
                            CHECK (porcentaje_matricula IS NULL OR (porcentaje_matricula >= 0 AND porcentaje_matricula <= 100));
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint WHERE conname = 'CK_configuracion_arancel_carrera_arancel_manual'
                    ) THEN
                        ALTER TABLE public.configuracion_arancel_carrera
                            ADD CONSTRAINT "CK_configuracion_arancel_carrera_arancel_manual"
                            CHECK (arancel_manual IS NULL OR arancel_manual >= 0);
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_configuracion_arancel_carrera_carrera_escenario"
                    ON public.configuracion_arancel_carrera (carrera_id, COALESCE(escenario_proyeccion_id, 0))
                    WHERE esta_activo = TRUE;

                CREATE INDEX IF NOT EXISTS "IX_configuracion_arancel_carrera_escenario"
                    ON public.configuracion_arancel_carrera (escenario_proyeccion_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS public.configuracion_arancel_carrera;");
        }
    }
}
