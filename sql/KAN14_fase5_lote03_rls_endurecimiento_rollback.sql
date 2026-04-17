-- =============================================================
-- KAN14 Fase 5 - Lote 03 (RLS Endurecimiento fino) - ROLLBACK
-- Objetivo:
--   1) Retirar políticas finas de Lote 03.
--   2) Restaurar policy amplia de continuidad del Lote 02.
-- =============================================================

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '300s';

DO $$
DECLARE
    v_roles text;
    v_tbl text;
BEGIN
    SELECT string_agg(quote_ident(x.rolname), ', ')
      INTO v_roles
      FROM (
          SELECT DISTINCT r.rolname
          FROM pg_roles r
          WHERE r.rolname IN ('authenticated', 'service_role', 'postgres', current_user)
      ) x;

    IF v_roles IS NULL THEN
        RAISE EXCEPTION 'KAN14 lote03 rollback abortado: no se detectaron roles candidatos.';
    END IF;

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
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote03_read ON public.%I', v_tbl);
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote03_insert ON public.%I', v_tbl);
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote03_update ON public.%I', v_tbl);
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote03_delete ON public.%I', v_tbl);

        EXECUTE format('DROP POLICY IF EXISTS kan14_lote02_app_rw ON public.%I', v_tbl);
        EXECUTE format(
            'CREATE POLICY kan14_lote02_app_rw ON public.%I AS PERMISSIVE FOR ALL TO %s USING (true) WITH CHECK (true)',
            v_tbl,
            v_roles
        );

        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', v_tbl);
    END LOOP;
END;
$$;

COMMIT;
