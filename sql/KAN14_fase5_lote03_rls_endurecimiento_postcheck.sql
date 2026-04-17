-- =============================================================
-- KAN14 Fase 5 - Lote 03 (RLS Endurecimiento fino) - POSTCHECK
-- Objetivo:
--   1) Verificar RLS activo en tablas criticas.
--   2) Verificar políticas finas de Lote 03 por tabla.
--   3) Verificar retiro de policy amplia de Lote 02.
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
        SELECT 1 FROM pg_policies p
        WHERE p.schemaname = 'public' AND p.tablename = t.tabla AND p.policyname = 'kan14_lote03_read'
    ) AS p_read,
    EXISTS (
        SELECT 1 FROM pg_policies p
        WHERE p.schemaname = 'public' AND p.tablename = t.tabla AND p.policyname = 'kan14_lote03_insert'
    ) AS p_insert,
    EXISTS (
        SELECT 1 FROM pg_policies p
        WHERE p.schemaname = 'public' AND p.tablename = t.tabla AND p.policyname = 'kan14_lote03_update'
    ) AS p_update,
    EXISTS (
        SELECT 1 FROM pg_policies p
        WHERE p.schemaname = 'public' AND p.tablename = t.tabla AND p.policyname = 'kan14_lote03_delete'
    ) AS p_delete,
    EXISTS (
        SELECT 1 FROM pg_policies p
        WHERE p.schemaname = 'public' AND p.tablename = t.tabla AND p.policyname = 'kan14_lote02_app_rw'
    ) AS p_lote02_remanente
FROM tablas t
JOIN pg_class c ON c.relname = t.tabla
JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = 'public'
ORDER BY t.tabla;

-- Gate postcheck: debe retornar 0
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
SELECT COUNT(*) AS total_errores_lote03_rls
FROM tablas t
JOIN pg_class c ON c.relname = t.tabla
JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = 'public'
WHERE c.relrowsecurity IS DISTINCT FROM TRUE
   OR NOT EXISTS (SELECT 1 FROM pg_policies p WHERE p.schemaname = 'public' AND p.tablename = t.tabla AND p.policyname = 'kan14_lote03_read')
   OR NOT EXISTS (SELECT 1 FROM pg_policies p WHERE p.schemaname = 'public' AND p.tablename = t.tabla AND p.policyname = 'kan14_lote03_insert')
   OR NOT EXISTS (SELECT 1 FROM pg_policies p WHERE p.schemaname = 'public' AND p.tablename = t.tabla AND p.policyname = 'kan14_lote03_update')
   OR NOT EXISTS (SELECT 1 FROM pg_policies p WHERE p.schemaname = 'public' AND p.tablename = t.tabla AND p.policyname = 'kan14_lote03_delete')
   OR EXISTS (SELECT 1 FROM pg_policies p WHERE p.schemaname = 'public' AND p.tablename = t.tabla AND p.policyname = 'kan14_lote02_app_rw');
