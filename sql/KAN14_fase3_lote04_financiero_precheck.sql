-- =============================================================
-- KAN14 Fase 3 - Lote 04 (Financiero) - PRECHECK
-- Objetivo: validar convertibilidad de columnas de fecha antes
-- de migrar TEXT/timestamp->timestamptz.
-- Ejecutar primero en pgAdmin (entorno local).
-- =============================================================

SET lock_timeout = '5s';
SET statement_timeout = '120s';

CREATE OR REPLACE FUNCTION pg_temp.try_parse_timestamptz(p_value text)
RETURNS timestamptz
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_value IS NULL OR btrim(p_value) = '' THEN
        RETURN NULL;
    END IF;

    BEGIN
        RETURN btrim(p_value)::timestamptz;
    EXCEPTION WHEN others THEN
        RETURN NULL;
    END;
END;
$$;

DROP TABLE IF EXISTS tmp_kan14_lote04_precheck;
CREATE TEMP TABLE tmp_kan14_lote04_precheck
(
    tabla text,
    columna text,
    tipo_actual text,
    total_no_vacios bigint,
    invalidos bigint,
    ejemplo_invalido text
);

DO $$
DECLARE
    r record;
    v_type text;
    v_total bigint;
    v_invalid bigint;
    v_sample text;
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
            INSERT INTO tmp_kan14_lote04_precheck
            VALUES (r.tabla, r.columna, 'NO_EXISTE', 0, 0, NULL);
            CONTINUE;
        END IF;

        IF v_type IN ('text', 'character varying') THEN
            EXECUTE format(
                'SELECT COUNT(*) FROM public.%I WHERE NULLIF(btrim(%I), '''') IS NOT NULL',
                r.tabla, r.columna
            ) INTO v_total;

            EXECUTE format(
                'SELECT COUNT(*) FROM public.%I WHERE NULLIF(btrim(%I), '''') IS NOT NULL AND pg_temp.try_parse_timestamptz(%I) IS NULL',
                r.tabla, r.columna, r.columna
            ) INTO v_invalid;

            EXECUTE format(
                'SELECT %1$I FROM public.%2$I WHERE NULLIF(btrim(%1$I), '''') IS NOT NULL AND pg_temp.try_parse_timestamptz(%1$I) IS NULL LIMIT 1',
                r.columna, r.tabla
            ) INTO v_sample;
        ELSE
            v_total := 0;
            v_invalid := 0;
            v_sample := NULL;
        END IF;

        INSERT INTO tmp_kan14_lote04_precheck
        VALUES (r.tabla, r.columna, v_type, v_total, v_invalid, v_sample);
    END LOOP;
END;
$$;

-- Resultado principal de convertibilidad por columna
SELECT *
FROM tmp_kan14_lote04_precheck
ORDER BY tabla, columna;

-- Conteo base por tabla para comparar antes/despues del lote
SELECT 'presupuesto_institucional' AS tabla, COUNT(*) AS filas FROM public.presupuesto_institucional
UNION ALL
SELECT 'proyeccion_estudiantes', COUNT(*) FROM public.proyeccion_estudiantes
UNION ALL
SELECT 'resumen_proyeccion_financiera', COUNT(*) FROM public.resumen_proyeccion_financiera
UNION ALL
SELECT 'configuracion_arancel', COUNT(*) FROM public.configuracion_arancel
ORDER BY tabla;

-- Gate precheck: debe devolver 0 para continuar con APPLY
SELECT COALESCE(SUM(invalidos), 0) AS total_invalidos_lote04
FROM tmp_kan14_lote04_precheck;