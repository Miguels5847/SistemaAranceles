-- =============================================================
-- KAN14 Fase 5 - Lote 02 (RLS) - ROLLBACK
-- Objetivo: retirar politica de continuidad y deshabilitar RLS
--           en tablas criticas del lote.
-- =============================================================

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '300s';

DO $$
DECLARE
    v_tbl text;
BEGIN
    FOR v_tbl IN
        SELECT unnest(ARRAY[
            'usuario',
            'rol',
            'permiso',
            'rol_permiso',
            'usuario_permiso_override',
            'sesion_usuario',
            'auditoria_log'
        ])
    LOOP
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote02_app_rw ON public.%I', v_tbl);
        EXECUTE format('ALTER TABLE public.%I DISABLE ROW LEVEL SECURITY', v_tbl);
    END LOOP;
END;
$$;

COMMIT;
