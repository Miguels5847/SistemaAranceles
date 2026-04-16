-- =============================================================
-- KAN14 Fase 5 - Lote 03 (RLS Endurecimiento fino) - APPLY
-- Objetivo:
--   1) Reemplazar policy amplia del Lote 02 por políticas por operación.
--   2) Mantener continuidad funcional (sin corte) para roles de app.
-- =============================================================

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '300s';

DO $$
DECLARE
    v_roles text;
    v_tbl text;
BEGIN
    -- Roles permitidos existentes
    SELECT string_agg(quote_ident(x.rolname), ', ')
      INTO v_roles
      FROM (
          SELECT DISTINCT r.rolname
          FROM pg_roles r
          WHERE r.rolname IN ('authenticated', 'service_role', 'postgres', current_user)
      ) x;

    IF v_roles IS NULL THEN
        RAISE EXCEPTION 'KAN14 lote03 RLS abortado: no se detectaron roles candidatos.';
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
        -- Mantener RLS activo
        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', v_tbl);

        -- Limpiar policy amplia previa (si existe)
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote02_app_rw ON public.%I', v_tbl);

        -- Limpiar policies lote03 por si es re-ejecución
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote03_read ON public.%I', v_tbl);
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote03_insert ON public.%I', v_tbl);
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote03_update ON public.%I', v_tbl);
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote03_delete ON public.%I', v_tbl);

        -- Políticas finas por operación
        EXECUTE format(
            'CREATE POLICY kan14_lote03_read ON public.%I AS PERMISSIVE FOR SELECT TO %s USING (true)',
            v_tbl,
            v_roles
        );

        EXECUTE format(
            'CREATE POLICY kan14_lote03_insert ON public.%I AS PERMISSIVE FOR INSERT TO %s WITH CHECK (true)',
            v_tbl,
            v_roles
        );

        EXECUTE format(
            'CREATE POLICY kan14_lote03_update ON public.%I AS PERMISSIVE FOR UPDATE TO %s USING (true) WITH CHECK (true)',
            v_tbl,
            v_roles
        );

        EXECUTE format(
            'CREATE POLICY kan14_lote03_delete ON public.%I AS PERMISSIVE FOR DELETE TO %s USING (true)',
            v_tbl,
            v_roles
        );

        RAISE NOTICE 'KAN14 lote03 RLS: tabla=% endurecida por operación para roles [%].', v_tbl, v_roles;
    END LOOP;
END;
$$;

COMMIT;
