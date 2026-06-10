-- =============================================================================
-- KAN-22: Datos Institucionales (snapshot agregado de planta central)
-- Ejecutar en Supabase SQL Editor.
-- Idempotente: usa IF NOT EXISTS y ON CONFLICT.
-- =============================================================================

BEGIN;

-- 1. Tabla datos_institucionales
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
        SELECT 1 FROM pg_constraint
         WHERE conname = 'CK_datos_institucionales_meses_capital_trabajo'
    ) THEN
        ALTER TABLE public.datos_institucionales
            ADD CONSTRAINT "CK_datos_institucionales_meses_capital_trabajo"
            CHECK (meses_capital_trabajo > 0);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
         WHERE conname = 'CK_datos_institucionales_porcentaje_imprevistos_inversion'
    ) THEN
        ALTER TABLE public.datos_institucionales
            ADD CONSTRAINT "CK_datos_institucionales_porcentaje_imprevistos_inversion"
            CHECK (porcentaje_imprevistos_inversion >= 0 AND porcentaje_imprevistos_inversion <= 100);
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_datos_institucionales_periodo"
    ON public.datos_institucionales (periodo);

CREATE INDEX IF NOT EXISTS "IX_datos_institucionales_actualizado_por_usuario_id"
    ON public.datos_institucionales (actualizado_por_usuario_id);

-- 2. Marcar la migracion EF como aplicada para evitar re-aplicacion
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260514000000_KAN22_DatosInstitucionales', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

-- Verificacion
SELECT to_regclass('public.datos_institucionales') AS tabla_creada;
SELECT COUNT(*) AS migracion_registrada
  FROM public."__EFMigrationsHistory"
 WHERE "MigrationId" = '20260514000000_KAN22_DatosInstitucionales';
