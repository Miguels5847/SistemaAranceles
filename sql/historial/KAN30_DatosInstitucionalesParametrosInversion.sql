-- =============================================================================
-- KAN-30: Parametros de inversion en Datos Institucionales
-- Corrige bases donde KAN-22 ya fue aplicado antes de agregar:
-- - meses_capital_trabajo
-- - porcentaje_imprevistos_inversion
-- Idempotente: puede ejecutarse varias veces sin romper datos existentes.
-- =============================================================================

BEGIN;

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

INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260523234517_KAN30_DatosInstitucionalesParametrosInversion', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

COMMIT;

SELECT column_name, data_type, column_default
  FROM information_schema.columns
 WHERE table_schema = 'public'
   AND table_name = 'datos_institucionales'
   AND column_name IN ('meses_capital_trabajo', 'porcentaje_imprevistos_inversion')
 ORDER BY column_name;
