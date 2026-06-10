-- =============================================================
-- KAN14 Fase 5 - Lote 02 (RLS) - APPLY
-- Objetivo:
--   1) Habilitar RLS de forma progresiva en tablas criticas.
--   2) Crear politicas de continuidad para roles de app conocidos.
--      (sin corte funcional; endurecimiento fino se hace despues).
-- =============================================================

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '300s';

DO $$
DECLARE
    v_roles text;
    v_tbl text;
BEGIN
    -- Roles permitidos: existentes entre authenticated, service_role, postgres y current_user
    SELECT string_agg(quote_ident(x.rolname), ', ')
      INTO v_roles
      FROM (
          SELECT DISTINCT r.rolname
          FROM pg_roles r
          WHERE r.rolname IN ('authenticated', 'service_role', 'postgres', current_user)
      ) x;

    IF v_roles IS NULL THEN
        RAISE EXCEPTION 'KAN14 lote02 RLS abortado: no se detectaron roles candidatos.';
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
        -- Habilitar RLS
        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', v_tbl);

        -- Politica idempotente de continuidad (lectura/escritura) para roles detectados
        EXECUTE format('DROP POLICY IF EXISTS kan14_lote02_app_rw ON public.%I', v_tbl);
        EXECUTE format(
            'CREATE POLICY kan14_lote02_app_rw ON public.%I AS PERMISSIVE FOR ALL TO %s USING (true) WITH CHECK (true)',
            v_tbl,
            v_roles
        );

        RAISE NOTICE 'KAN14 lote02 RLS: tabla=% politicas aplicadas para roles [%].', v_tbl, v_roles;
    END LOOP;
END;
$$;

COMMIT;
