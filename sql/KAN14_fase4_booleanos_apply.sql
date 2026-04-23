-- =============================================================
-- KAN14 Fase 4 (Booleanos) - APPLY
-- Objetivo: migrar columnas enteras de estado a boolean,
--           y corregir consistencia estado/esta_activo de usuario.
-- Requisito: precheck con total_invalidos_fase4 = 0.
-- =============================================================

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '300s';

DO $$
DECLARE
    r record;
    v_type text;
    v_invalid bigint;
    v_default text;
    v_default_bool text;
BEGIN
    FOR r IN
        WITH columnas_esta_activo AS (
            SELECT c.table_name AS tabla, c.column_name AS columna
            FROM information_schema.columns c
            WHERE c.table_schema = 'public'
              AND c.column_name = 'esta_activo'
        ),
        columnas_extra AS (
            SELECT 'balance_proyectado'::text AS tabla, 'cuadra_balance'::text AS columna
        )
        SELECT tabla, columna FROM columnas_esta_activo
        UNION
        SELECT tabla, columna FROM columnas_extra
    LOOP
        SELECT c.data_type
          INTO v_type
          FROM information_schema.columns c
         WHERE c.table_schema = 'public'
           AND c.table_name = r.tabla
           AND c.column_name = r.columna;

        IF v_type IS NULL THEN
            RAISE NOTICE 'KAN14 fase4: %.% no existe; se omite.', r.tabla, r.columna;
            CONTINUE;
        END IF;

        IF v_type = 'boolean' THEN
            RAISE NOTICE 'KAN14 fase4: %.% ya es boolean; se omite.', r.tabla, r.columna;
            CONTINUE;
        END IF;

        IF v_type NOT IN ('integer', 'smallint', 'bigint') THEN
            RAISE EXCEPTION 'KAN14 fase4 abortado: %.% tiene tipo no soportado (%).', r.tabla, r.columna, v_type;
        END IF;

        EXECUTE format(
            'SELECT COUNT(*) FROM public.%I WHERE %I IS NOT NULL AND %I NOT IN (0, 1)',
            r.tabla, r.columna, r.columna
        ) INTO v_invalid;

        IF COALESCE(v_invalid, 0) > 0 THEN
            RAISE EXCEPTION 'KAN14 fase4 abortado: %.% tiene % valores no convertibles (esperado 0/1).', r.tabla, r.columna, v_invalid;
        END IF;

        SELECT pg_get_expr(ad.adbin, ad.adrelid)
          INTO v_default
          FROM pg_attribute a
          LEFT JOIN pg_attrdef ad
            ON ad.adrelid = a.attrelid
           AND ad.adnum = a.attnum
         WHERE a.attrelid = format('public.%I', r.tabla)::regclass
           AND a.attname = r.columna;

        v_default_bool := NULL;
        IF v_default IS NOT NULL THEN
            IF lower(v_default) LIKE '%true%' OR lower(v_default) LIKE '%1%' THEN
                v_default_bool := 'TRUE';
            ELSIF lower(v_default) LIKE '%false%' OR lower(v_default) LIKE '%0%' THEN
                v_default_bool := 'FALSE';
            END IF;

            EXECUTE format(
                'ALTER TABLE public.%I ALTER COLUMN %I DROP DEFAULT',
                r.tabla, r.columna
            );
        END IF;

        EXECUTE format(
            'ALTER TABLE public.%I
               ALTER COLUMN %I TYPE boolean
               USING CASE
                        WHEN %I IS NULL THEN NULL
                        WHEN %I = 1 THEN TRUE
                        ELSE FALSE
                     END',
            r.tabla, r.columna, r.columna, r.columna
        );

        IF v_default_bool IS NOT NULL THEN
            EXECUTE format(
                'ALTER TABLE public.%I ALTER COLUMN %I SET DEFAULT %s',
                r.tabla, r.columna, v_default_bool
            );
        END IF;

        RAISE NOTICE 'KAN14 fase4: %.% convertido a boolean.', r.tabla, r.columna;
    END LOOP;
END;
$$;

-- Normalizacion de estado y consistencia en usuario
UPDATE public.usuario
SET estado = CASE
    WHEN lower(coalesce(estado, '')) = 'activo' THEN 'Activo'
    WHEN lower(coalesce(estado, '')) = 'suspendido' THEN 'Suspendido'
    WHEN lower(coalesce(estado, '')) = 'inactivo' THEN 'Inactivo'
    ELSE 'Inactivo'
END
WHERE estado IS NULL
   OR btrim(estado) = ''
   OR lower(estado) NOT IN ('activo', 'suspendido', 'inactivo')
   OR estado NOT IN ('Activo', 'Suspendido', 'Inactivo');

UPDATE public.usuario
SET esta_activo = CASE
    WHEN estado = 'Activo' THEN TRUE
    ELSE FALSE
END
WHERE esta_activo IS DISTINCT FROM (estado = 'Activo');

COMMIT;
