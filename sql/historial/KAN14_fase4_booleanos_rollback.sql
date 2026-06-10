-- =============================================================
-- KAN14 Fase 4 (Booleanos) - ROLLBACK
-- Objetivo: reversar boolean->integer en columnas objetivo.
-- Uso: solo si el postcheck o smoke funcional falla.
-- =============================================================

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '300s';

DO $$
DECLARE
    r record;
    v_type text;
    v_default text;
    v_default_int text;
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
            RAISE NOTICE 'KAN14 fase4 rollback: %.% no existe; se omite.', r.tabla, r.columna;
            CONTINUE;
        END IF;

        IF v_type <> 'boolean' THEN
            RAISE NOTICE 'KAN14 fase4 rollback: %.% no esta en boolean (tipo=%), se omite.', r.tabla, r.columna, v_type;
            CONTINUE;
        END IF;

        SELECT pg_get_expr(ad.adbin, ad.adrelid)
          INTO v_default
          FROM pg_attribute a
          LEFT JOIN pg_attrdef ad
            ON ad.adrelid = a.attrelid
           AND ad.adnum = a.attnum
         WHERE a.attrelid = format('public.%I', r.tabla)::regclass
           AND a.attname = r.columna;

        v_default_int := NULL;
        IF v_default IS NOT NULL THEN
            IF lower(v_default) LIKE '%true%' THEN
                v_default_int := '1';
            ELSIF lower(v_default) LIKE '%false%' THEN
                v_default_int := '0';
            END IF;

            EXECUTE format(
                'ALTER TABLE public.%I ALTER COLUMN %I DROP DEFAULT',
                r.tabla, r.columna
            );
        END IF;

        EXECUTE format(
            'ALTER TABLE public.%I
               ALTER COLUMN %I TYPE integer
               USING CASE WHEN %I THEN 1 ELSE 0 END',
            r.tabla, r.columna, r.columna
        );

        IF v_default_int IS NOT NULL THEN
            EXECUTE format(
                'ALTER TABLE public.%I ALTER COLUMN %I SET DEFAULT %s',
                r.tabla, r.columna, v_default_int
            );
        END IF;

        RAISE NOTICE 'KAN14 fase4 rollback: %.% convertido a integer.', r.tabla, r.columna;
    END LOOP;
END;
$$;

COMMIT;
