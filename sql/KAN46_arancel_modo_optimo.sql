-- =====================================================================
-- KAN-46 SQL-A: permitir el tercer modo de arancel 'OptimoFinanciero'.
-- Ejecutar PRIMERO (antes de probar "Usar arancel óptimo" en la app).
-- Idempotente: sí (re-ejecutable).
-- =====================================================================
BEGIN;

ALTER TABLE public.configuracion_arancel_carrera
    DROP CONSTRAINT IF EXISTS "CK_configuracion_arancel_carrera_modo";

ALTER TABLE public.configuracion_arancel_carrera
    ADD CONSTRAINT "CK_configuracion_arancel_carrera_modo"
    CHECK (modo_calculo_arancel IN ('Manual', 'AutomaticoCostoCarrera', 'OptimoFinanciero'));

COMMIT;

-- Verificación:
SELECT conname, pg_get_constraintdef(oid)
FROM pg_constraint
WHERE conrelid = 'public.configuracion_arancel_carrera'::regclass;
