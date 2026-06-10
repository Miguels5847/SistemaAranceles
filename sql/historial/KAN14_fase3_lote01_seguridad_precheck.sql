-- =============================================================
-- KAN14 Fase 3 - Lote 01 (Seguridad/Core) - PRECHECK
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

DROP TABLE IF EXISTS tmp_kan14_lote01_precheck;
CREATE TEMP TABLE tmp_kan14_lote01_precheck
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
            ('usuario', 'ultimo_acceso_en'),
            ('usuario', 'creado_en'),
            ('usuario', 'actualizado_en'),
            ('usuario', 'eliminado_en'),
            ('sesion_usuario', 'emitido_en'),
            ('sesion_usuario', 'expira_en'),
            ('sesion_usuario', 'revocado_en'),
            ('rol', 'creado_en'),
            ('rol', 'actualizado_en'),
            ('rol', 'eliminado_en'),
            ('permiso', 'creado_en'),
            ('permiso', 'actualizado_en'),
            ('permiso', 'eliminado_en'),
            ('usuario_permiso_override', 'creado_en')
        ) AS t(tabla, columna)
    LOOP
        SELECT c.data_type
          INTO v_type
          FROM information_schema.columns c
         WHERE c.table_schema = 'public'
           AND c.table_name = r.tabla
           AND c.column_name = r.columna;

        IF v_type IS NULL THEN
            INSERT INTO tmp_kan14_lote01_precheck
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

        INSERT INTO tmp_kan14_lote01_precheck
        VALUES (r.tabla, r.columna, v_type, v_total, v_invalid, v_sample);
    END LOOP;
END;
$$;

-- Resultado principal de convertibilidad por columna
SELECT *
FROM tmp_kan14_lote01_precheck
ORDER BY tabla, columna;

-- Conteo base por tabla para comparar antes/despues del lote
SELECT 'usuario' AS tabla, COUNT(*) AS filas FROM public.usuario
UNION ALL
SELECT 'sesion_usuario', COUNT(*) FROM public.sesion_usuario
UNION ALL
SELECT 'rol', COUNT(*) FROM public.rol
UNION ALL
SELECT 'permiso', COUNT(*) FROM public.permiso
UNION ALL
SELECT 'usuario_permiso_override', COUNT(*) FROM public.usuario_permiso_override
UNION ALL
SELECT 'auditoria_log', COUNT(*) FROM public.auditoria_log
ORDER BY tabla;

-- Estado actual de auditoria_log.evento_en
SELECT
    c.data_type,
    c.udt_name
FROM information_schema.columns c
WHERE c.table_schema = 'public'
  AND c.table_name = 'auditoria_log'
  AND c.column_name = 'evento_en';

-- Gate precheck: debe devolver 0 para continuar con APPLY
SELECT COALESCE(SUM(invalidos), 0) AS total_invalidos_lote01
FROM tmp_kan14_lote01_precheck;
