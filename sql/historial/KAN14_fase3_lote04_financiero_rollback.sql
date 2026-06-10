-- =============================================================
-- KAN14 Fase 3 - Lote 04 (Financiero) - ROLLBACK
-- Objetivo: reversar lote 04 en caso de falla critica funcional.
-- Nota: se revierte a esquema anterior (TEXT y timestamp sin zona).
-- =============================================================

BEGIN;

SET LOCAL lock_timeout = '10s';
SET LOCAL statement_timeout = '300s';

DO $$
DECLARE
    r record;
    v_type text;
    v_default text;
BEGIN
    FOR r IN
        SELECT *
        FROM (VALUES
            ('presupuesto_institucional', 'creado_en'),
            ('presupuesto_institucional', 'actualizado_en'),
            ('presupuesto_institucional', 'eliminado_en'),
            ('proyeccion_estudiantes', 'creado_en'),
            ('proyeccion_estudiantes', 'actualizado_en'),
            ('proyeccion_estudiantes', 'eliminado_en'),
            ('resumen_proyeccion_financiera', 'creado_en'),
            ('resumen_proyeccion_financiera', 'actualizado_en'),
            ('resumen_proyeccion_financiera', 'eliminado_en'),
            ('configuracion_arancel', 'creado_en'),
            ('configuracion_arancel', 'actualizado_en'),
            ('configuracion_arancel', 'eliminado_en')
        ) AS t(tabla, columna)
    LOOP
        SELECT c.data_type
          INTO v_type
          FROM information_schema.columns c
         WHERE c.table_schema = 'public'
           AND c.table_name = r.tabla
           AND c.column_name = r.columna;

        IF v_type IS NULL THEN
            RAISE NOTICE 'KAN14 rollback lote04: %.% no existe; se omite.', r.tabla, r.columna;
            CONTINUE;
        END IF;

        IF v_type = 'timestamp with time zone' THEN
            SELECT pg_get_expr(ad.adbin, ad.adrelid)
              INTO v_default
              FROM pg_attribute a
              LEFT JOIN pg_attrdef ad
                ON ad.adrelid = a.attrelid
               AND ad.adnum = a.attnum
             WHERE a.attrelid = format('public.%I', r.tabla)::regclass
               AND a.attname = r.columna;

            IF v_default IS NOT NULL THEN
                EXECUTE format(
                    'ALTER TABLE public.%I ALTER COLUMN %I DROP DEFAULT',
                    r.tabla, r.columna
                );
            END IF;

            EXECUTE format(
                'ALTER TABLE public.%I
                   ALTER COLUMN %I TYPE text
                   USING CASE
                            WHEN %I IS NULL THEN NULL
                            ELSE to_char((%I AT TIME ZONE ''UTC''), ''YYYY-MM-DD"T"HH24:MI:SS.MS"Z"'')
                         END',
                r.tabla, r.columna, r.columna, r.columna
            );

            IF v_default IS NOT NULL THEN
                IF v_default IN ('now()', 'CURRENT_TIMESTAMP', 'current_timestamp') THEN
                    EXECUTE format(
                        'ALTER TABLE public.%I ALTER COLUMN %I SET DEFAULT now()::text',
                        r.tabla, r.columna
                    );
                ELSE
                    EXECUTE format(
                        'ALTER TABLE public.%I ALTER COLUMN %I SET DEFAULT %s',
                        r.tabla, r.columna, v_default
                    );
                END IF;
            END IF;

            RAISE NOTICE 'KAN14 rollback lote04: %.% revertido a text.', r.tabla, r.columna;
        ELSE
            RAISE NOTICE 'KAN14 rollback lote04: %.% no esta en timestamptz (tipo=%), sin cambios.', r.tabla, r.columna, v_type;
        END IF;
    END LOOP;
END;
$$;

COMMIT;