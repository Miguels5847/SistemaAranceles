-- =============================================================
-- KAN14 Fase 5 - Lote 02 (RLS) - POSTCHECK
-- Objetivo:
--   1) Verificar RLS habilitado en tablas criticas.
--   2) Verificar existencia de politica kan14_lote02_app_rw.
-- =============================================================

SET lock_timeout = '5s';
SET statement_timeout = '120s';

WITH tablas(tabla) AS (
    VALUES
        ('usuario'),
        ('rol'),
        ('permiso'),
        ('rol_permiso'),
        ('usuario_permiso_override'),
        ('sesion_usuario'),
        ('auditoria_log')
)
SELECT
    t.tabla,
    c.relrowsecurity AS rls_habilitado,
    EXISTS (
        SELECT 1
        FROM pg_policies p
        WHERE p.schemaname = 'public'
          AND p.tablename = t.tabla
          AND p.policyname = 'kan14_lote02_app_rw'
    ) AS politica_app_rw
FROM tablas t
JOIN pg_class c ON c.relname = t.tabla
JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = 'public'
ORDER BY t.tabla;

-- Gate postcheck (debe retornar 0)
WITH tablas(tabla) AS (
    VALUES
        ('usuario'),
        ('rol'),
        ('permiso'),
        ('rol_permiso'),
        ('usuario_permiso_override'),
        ('sesion_usuario'),
        ('auditoria_log')
)
SELECT COUNT(*) AS total_errores_lote02_rls
FROM tablas t
JOIN pg_class c ON c.relname = t.tabla
JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = 'public'
WHERE c.relrowsecurity IS DISTINCT FROM TRUE
   OR NOT EXISTS (
       SELECT 1
       FROM pg_policies p
       WHERE p.schemaname = 'public'
         AND p.tablename = t.tabla
         AND p.policyname = 'kan14_lote02_app_rw'
   );
