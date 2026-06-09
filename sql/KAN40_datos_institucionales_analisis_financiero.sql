-- KAN-40: Parámetros Análisis Financiero (Épica 11) en datos_institucionales.
-- Idempotente: se puede ejecutar varias veces sin romper datos existentes.
-- La app también auto-crea estas columnas (RepositorioDatosInstitucionales), este script
-- permite aplicarlas manualmente en Supabase.

ALTER TABLE public.datos_institucionales
    ADD COLUMN IF NOT EXISTS tasa_interes_financiera NUMERIC(7,4) NOT NULL DEFAULT 8.0000,
    ADD COLUMN IF NOT EXISTS premio_riesgo           NUMERIC(7,4) NOT NULL DEFAULT 5.0000,
    ADD COLUMN IF NOT EXISTS tmr_manual              NUMERIC(7,4) NOT NULL DEFAULT 0.0000,
    ADD COLUMN IF NOT EXISTS usar_tmr_manual         BOOLEAN      NOT NULL DEFAULT FALSE;

-- Asegura defaults/NOT NULL en caso de que las columnas existieran sin restricción.
ALTER TABLE public.datos_institucionales
    ALTER COLUMN tasa_interes_financiera SET DEFAULT 8.0000,
    ALTER COLUMN tasa_interes_financiera SET NOT NULL,
    ALTER COLUMN premio_riesgo SET DEFAULT 5.0000,
    ALTER COLUMN premio_riesgo SET NOT NULL,
    ALTER COLUMN tmr_manual SET DEFAULT 0.0000,
    ALTER COLUMN tmr_manual SET NOT NULL,
    ALTER COLUMN usar_tmr_manual SET DEFAULT FALSE,
    ALTER COLUMN usar_tmr_manual SET NOT NULL;
