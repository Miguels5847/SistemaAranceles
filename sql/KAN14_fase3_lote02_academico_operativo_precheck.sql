-- =============================================================
-- KAN14 Fase 3 - Lote 02 (Académico-Operativo) - PRECHECK
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

DROP TABLE IF EXISTS tmp_kan14_lote02_precheck;
CREATE TEMP TABLE tmp_kan14_lote02_precheck
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
            ('carrera', 'creado_en'),
            ('carrera', 'actualizado_en'),
            ('carrera', 'eliminado_en'),
            ('escenario_proyeccion', 'creado_en'),
            ('escenario_proyeccion', 'actualizado_en'),
            ('escenario_proyeccion', 'eliminado_en'),
            ('periodo_academico', 'creado_en'),
            ('periodo_academico', 'actualizado_en'),
            ('periodo_academico', 'eliminado_en'),
            ('configuracion_carga_docente', 'creado_en'),
            ('configuracion_carga_docente', 'actualizado_en'),
            ('configuracion_carga_docente', 'eliminado_en'),
            ('detalle_proyeccion_estudiantes', 'creado_en'),
            ('detalle_proyeccion_estudiantes', 'actualizado_en'),
            ('detalle_proyeccion_estudiantes', 'eliminado_en'),
            ('detalle_simulacion_retencion', 'creado_en'),
            ('detalle_simulacion_retencion', 'actualizado_en'),
            ('detalle_simulacion_retencion', 'eliminado_en'),
            ('simulacion_retencion', 'creado_en'),
            ('simulacion_retencion', 'actualizado_en'),
            ('simulacion_retencion', 'eliminado_en')
        ) AS t(tabla, columna)
    LOOP
        SELECT c.data_type
          INTO v_type
          FROM information_schema.columns c
         WHERE c.table_schema = 'public'
           AND c.table_name = r.tabla
           AND c.column_name = r.columna;

        IF v_type IS NULL THEN
            INSERT INTO tmp_kan14_lote02_precheck
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

        INSERT INTO tmp_kan14_lote02_precheck
        VALUES (r.tabla, r.columna, v_type, v_total, v_invalid, v_sample);
    END LOOP;
END;
$$;

-- Resultado principal de convertibilidad por columna
SELECT *
FROM tmp_kan14_lote02_precheck
ORDER BY tabla, columna;

-- Conteo base por tabla para comparar antes/despues del lote
SELECT 'carrera' AS tabla, COUNT(*) AS filas FROM public.carrera
UNION ALL
SELECT 'escenario_proyeccion', COUNT(*) FROM public.escenario_proyeccion
UNION ALL
SELECT 'periodo_academico', COUNT(*) FROM public.periodo_academico
UNION ALL
SELECT 'configuracion_carga_docente', COUNT(*) FROM public.configuracion_carga_docente
UNION ALL
SELECT 'detalle_proyeccion_estudiantes', COUNT(*) FROM public.detalle_proyeccion_estudiantes
UNION ALL
SELECT 'detalle_simulacion_retencion', COUNT(*) FROM public.detalle_simulacion_retencion
UNION ALL
SELECT 'simulacion_retencion', COUNT(*) FROM public.simulacion_retencion
ORDER BY tabla;

-- Gate precheck: debe devolver 0 para continuar con APPLY
SELECT COALESCE(SUM(invalidos), 0) AS total_invalidos_lote02
FROM tmp_kan14_lote02_precheck;