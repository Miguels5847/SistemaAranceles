-- =============================================================
-- KAN14 Fase 3 - Lote 03 (Costos/Operativo) - APPLY
-- Objetivo: migrar columnas de fecha a timestamptz de forma segura.
-- Requisito: ejecutar precheck y confirmar total_invalidos_lote03 = 0.
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
BEGIN
    FOR r IN
        SELECT *
        FROM (VALUES
            ('cargo_facultad', 'creado_en'),
            ('cargo_facultad', 'actualizado_en'),
            ('cargo_facultad', 'eliminado_en'),
            ('cargo_planta_central', 'creado_en'),
            ('cargo_planta_central', 'actualizado_en'),
            ('cargo_planta_central', 'eliminado_en'),
            ('configuracion_retencion', 'creado_en'),
            ('configuracion_retencion', 'actualizado_en'),
            ('configuracion_retencion', 'eliminado_en'),
            ('criterio_referencia_retencion', 'creado_en'),
            ('criterio_referencia_retencion', 'actualizado_en'),
            ('criterio_referencia_retencion', 'eliminado_en'),
            ('item_material_insumo', 'creado_en'),
            ('item_material_insumo', 'actualizado_en'),
            ('item_material_insumo', 'eliminado_en'),
            ('proyeccion_cargo_facultad', 'creado_en'),
            ('proyeccion_cargo_facultad', 'actualizado_en'),
            ('proyeccion_cargo_facultad', 'eliminado_en'),
            ('proyeccion_cargo_planta_central', 'creado_en'),
            ('proyeccion_cargo_planta_central', 'actualizado_en'),
            ('proyeccion_cargo_planta_central', 'eliminado_en'),
            ('proyeccion_material_insumo', 'creado_en'),
            ('proyeccion_material_insumo', 'actualizado_en'),
            ('proyeccion_material_insumo', 'eliminado_en'),
            ('proyeccion_requerimiento_docente', 'creado_en'),
            ('proyeccion_requerimiento_docente', 'actualizado_en'),
            ('proyeccion_requerimiento_docente', 'eliminado_en')
        ) AS t(tabla, columna)
    LOOP
        SELECT c.data_type
          INTO v_type
          FROM information_schema.columns c
         WHERE c.table_schema = 'public'
           AND c.table_name = r.tabla
           AND c.column_name = r.columna;

        IF v_type IS NULL THEN
            RAISE NOTICE 'KAN14 lote03: %.% no existe; se omite.', r.tabla, r.columna;
            CONTINUE;
        END IF;

        IF v_type IN ('text', 'character varying') THEN
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
                'SELECT COUNT(*)
                   FROM public.%I
                  WHERE NULLIF(btrim(%I), '''') IS NOT NULL
                    AND (
                        CASE
                            WHEN NULLIF(btrim(%I), '''') IS NULL THEN NULL
                            ELSE NULLIF(btrim(%I), '''')::timestamptz
                        END
                    ) IS NULL',
                r.tabla, r.columna, r.columna, r.columna
            ) INTO v_invalid;

            IF COALESCE(v_invalid, 0) > 0 THEN
                RAISE EXCEPTION 'KAN14 lote03 abortado: %.% tiene % valores no convertibles.', r.tabla, r.columna, v_invalid;
            END IF;

            EXECUTE format(
                'ALTER TABLE public.%I
                   ALTER COLUMN %I TYPE timestamptz
                   USING CASE
                            WHEN NULLIF(btrim(%I), '''') IS NULL THEN NULL
                            ELSE NULLIF(btrim(%I), '''')::timestamptz
                         END',
                r.tabla, r.columna, r.columna, r.columna
            );

            IF v_default IS NOT NULL THEN
                EXECUTE format(
                    'ALTER TABLE public.%I ALTER COLUMN %I SET DEFAULT now()',
                    r.tabla, r.columna
                );
            END IF;

            RAISE NOTICE 'KAN14 lote03: %.% convertido a timestamptz.', r.tabla, r.columna;
        ELSIF v_type = 'timestamp without time zone' THEN
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
                   ALTER COLUMN %I TYPE timestamptz
                   USING %I AT TIME ZONE ''UTC''',
                r.tabla, r.columna, r.columna
            );

            IF v_default IS NOT NULL THEN
                EXECUTE format(
                    'ALTER TABLE public.%I ALTER COLUMN %I SET DEFAULT now()',
                    r.tabla, r.columna
                );
            END IF;

            RAISE NOTICE 'KAN14 lote03: %.% timestamp->timestamptz (UTC) aplicado.', r.tabla, r.columna;
        ELSE
            RAISE NOTICE 'KAN14 lote03: %.% ya no es text/varchar/timestamp sin zona (tipo=%), se omite.', r.tabla, r.columna, v_type;
        END IF;
    END LOOP;
END;
$$;

COMMIT;