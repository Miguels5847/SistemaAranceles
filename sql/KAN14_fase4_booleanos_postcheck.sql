-- =============================================================
-- KAN14 Fase 4 (Booleanos) - POSTCHECK
-- Objetivo: verificar tipos finales, consistencia de estado
--           y ausencia de deuda int/bool en columnas objetivo.
-- =============================================================

SET lock_timeout = '5s';
SET statement_timeout = '120s';

-- Tipos finales por columna objetivo
WITH objetivos AS (
    SELECT c.table_name AS tabla, c.column_name AS columna
    FROM information_schema.columns c
    WHERE c.table_schema = 'public'
      AND c.column_name = 'esta_activo'
    UNION
    SELECT 'balance_proyectado'::text AS tabla, 'cuadra_balance'::text AS columna
)
SELECT
    o.tabla,
    o.columna,
    c.data_type AS tipo_actual
FROM objetivos o
LEFT JOIN information_schema.columns c
  ON c.table_schema = 'public'
 AND c.table_name = o.tabla
 AND c.column_name = o.columna
ORDER BY o.tabla, o.columna;

-- Conteos de tablas clave (control de filas)
SELECT 'usuario' AS tabla, COUNT(*) AS filas FROM public.usuario
UNION ALL SELECT 'rol', COUNT(*) FROM public.rol
UNION ALL SELECT 'permiso', COUNT(*) FROM public.permiso
UNION ALL SELECT 'balance_proyectado', COUNT(*) FROM public.balance_proyectado
ORDER BY tabla;

-- Validacion de consistencia estado vs esta_activo
SELECT
    COUNT(*) FILTER (
        WHERE estado = 'Activo'
          AND esta_activo IS DISTINCT FROM TRUE
    ) AS activos_inconsistentes,
    COUNT(*) FILTER (
        WHERE estado IN ('Suspendido', 'Inactivo')
          AND esta_activo IS DISTINCT FROM FALSE
    ) AS no_activos_inconsistentes,
    COUNT(*) FILTER (
        WHERE estado NOT IN ('Activo', 'Suspendido', 'Inactivo')
    ) AS estados_fuera_catalogo
FROM public.usuario;

-- Gate postcheck: debe devolver 0 para considerar cierre tecnico
WITH objetivos AS (
    SELECT c.table_name AS tabla, c.column_name AS columna
    FROM information_schema.columns c
    WHERE c.table_schema = 'public'
      AND c.column_name = 'esta_activo'
    UNION
    SELECT 'balance_proyectado'::text AS tabla, 'cuadra_balance'::text AS columna
)
SELECT COUNT(*) AS columnas_no_boolean
FROM objetivos o
LEFT JOIN information_schema.columns c
  ON c.table_schema = 'public'
 AND c.table_name = o.tabla
 AND c.column_name = o.columna
WHERE c.data_type IS DISTINCT FROM 'boolean';
