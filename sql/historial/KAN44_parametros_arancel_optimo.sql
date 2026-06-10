-- KAN-44: parámetros de la bisección del arancel óptimo (VAN=0) en Datos Institucionales.
-- Aditivo e idempotente. El repositorio también lo auto-aplica en runtime
-- (RepositorioDatosInstitucionales.AsegurarParametrosInversionAsync), así que ejecutarlo
-- a mano es opcional; se incluye para entornos local/prueba y documentación.
-- NO aplicado automáticamente al Supabase real.

ALTER TABLE public.datos_institucionales
    ADD COLUMN IF NOT EXISTS tolerancia_van_arancel          NUMERIC(18,2) NOT NULL DEFAULT 1.00,
    ADD COLUMN IF NOT EXISTS margen_aproximacion_van_arancel NUMERIC(18,2) NOT NULL DEFAULT 2.00,
    ADD COLUMN IF NOT EXISTS arancel_minimo_busqueda         NUMERIC(18,2) NOT NULL DEFAULT 500.00,
    ADD COLUMN IF NOT EXISTS arancel_maximo_busqueda         NUMERIC(18,2) NOT NULL DEFAULT 5000.00,
    ADD COLUMN IF NOT EXISTS max_iteraciones_biseccion       INTEGER       NOT NULL DEFAULT 60;
