-- =============================================================
-- KAN14 Fase 3 - Lote 03 (Costos/Operativo) - PRECHECK
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

DROP TABLE IF EXISTS tmp_kan14_lote03_precheck;
CREATE TEMP TABLE tmp_kan14_lote03_precheck
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
            INSERT INTO tmp_kan14_lote03_precheck
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

        INSERT INTO tmp_kan14_lote03_precheck
        VALUES (r.tabla, r.columna, v_type, v_total, v_invalid, v_sample);
    END LOOP;
END;
$$;

-- Resultado principal de convertibilidad por columna
SELECT *
FROM tmp_kan14_lote03_precheck
ORDER BY tabla, columna;

-- Conteo base por tabla para comparar antes/despues del lote
SELECT 'cargo_facultad' AS tabla, COUNT(*) AS filas FROM public.cargo_facultad
UNION ALL
SELECT 'cargo_planta_central', COUNT(*) FROM public.cargo_planta_central
UNION ALL
SELECT 'configuracion_retencion', COUNT(*) FROM public.configuracion_retencion
UNION ALL
SELECT 'criterio_referencia_retencion', COUNT(*) FROM public.criterio_referencia_retencion
UNION ALL
SELECT 'item_material_insumo', COUNT(*) FROM public.item_material_insumo
UNION ALL
SELECT 'proyeccion_cargo_facultad', COUNT(*) FROM public.proyeccion_cargo_facultad
UNION ALL
SELECT 'proyeccion_cargo_planta_central', COUNT(*) FROM public.proyeccion_cargo_planta_central
UNION ALL
SELECT 'proyeccion_material_insumo', COUNT(*) FROM public.proyeccion_material_insumo
UNION ALL
SELECT 'proyeccion_requerimiento_docente', COUNT(*) FROM public.proyeccion_requerimiento_docente
ORDER BY tabla;

-- Gate precheck: debe devolver 0 para continuar con APPLY
SELECT COALESCE(SUM(invalidos), 0) AS total_invalidos_lote03
FROM tmp_kan14_lote03_precheck;