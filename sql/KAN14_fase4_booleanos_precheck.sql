-- =============================================================
-- KAN14 Fase 4 (Booleanos) - PRECHECK
-- Objetivo: validar convertibilidad segura de columnas enteras
--           a boolean y detectar inconsistencias de estado.
-- Ejecutar primero en base de respaldo/local.
-- =============================================================

SET lock_timeout = '5s';
SET statement_timeout = '120s';

DROP TABLE IF EXISTS tmp_kan14_fase4_precheck;
CREATE TEMP TABLE tmp_kan14_fase4_precheck
(
    tabla text,
    columna text,
    tipo_actual text,
    total_no_nulos bigint,
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
            INSERT INTO tmp_kan14_fase4_precheck
            VALUES (r.tabla, r.columna, 'NO_EXISTE', 0, 0, NULL);
            CONTINUE;
        END IF;

        IF v_type IN ('integer', 'smallint', 'bigint') THEN
            EXECUTE format(
                'SELECT COUNT(*) FROM public.%I WHERE %I IS NOT NULL',
                r.tabla, r.columna
            ) INTO v_total;

            EXECUTE format(
                'SELECT COUNT(*) FROM public.%I WHERE %I IS NOT NULL AND %I NOT IN (0, 1)',
                r.tabla, r.columna, r.columna
            ) INTO v_invalid;

            EXECUTE format(
                'SELECT %1$I::text FROM public.%2$I WHERE %1$I IS NOT NULL AND %1$I NOT IN (0, 1) LIMIT 1',
                r.columna, r.tabla
            ) INTO v_sample;
        ELSIF v_type = 'boolean' THEN
            v_total := 0;
            v_invalid := 0;
            v_sample := NULL;
        ELSE
            v_total := 0;
            v_invalid := 1;
            v_sample := 'TIPO_NO_SOPORTADO:' || v_type;
        END IF;

        INSERT INTO tmp_kan14_fase4_precheck
        VALUES (r.tabla, r.columna, v_type, v_total, v_invalid, v_sample);
    END LOOP;
END;
$$;

-- Resultado de convertibilidad por columna objetivo
SELECT *
FROM tmp_kan14_fase4_precheck
ORDER BY tabla, columna;

-- Inconsistencias de negocio en usuario (estado vs esta_activo)
SELECT
    COUNT(*) FILTER (
        WHERE lower(coalesce(estado, '')) NOT IN ('activo', 'suspendido', 'inactivo')
    ) AS estados_no_canonicos,
    COUNT(*) FILTER (
        WHERE lower(coalesce(estado, '')) = 'activo'
          AND lower(coalesce(esta_activo::text, 'false')) NOT IN ('true', 't', '1')
    ) AS activos_marcados_como_inactivos,
    COUNT(*) FILTER (
        WHERE lower(coalesce(estado, '')) IN ('suspendido', 'inactivo')
          AND lower(coalesce(esta_activo::text, 'false')) IN ('true', 't', '1')
    ) AS no_activos_marcados_como_activos
FROM public.usuario;

-- Conteos base de tablas clave para comparar antes/despues
SELECT 'usuario' AS tabla, COUNT(*) AS filas FROM public.usuario
UNION ALL SELECT 'rol', COUNT(*) FROM public.rol
UNION ALL SELECT 'permiso', COUNT(*) FROM public.permiso
UNION ALL SELECT 'balance_proyectado', COUNT(*) FROM public.balance_proyectado
ORDER BY tabla;

-- Gate precheck: debe devolver 0 para continuar con APPLY
SELECT COALESCE(SUM(invalidos), 0) AS total_invalidos_fase4
FROM tmp_kan14_fase4_precheck;
