-- =============================================================
-- KAN14 Fase 5 - Lote 03 (RLS Endurecimiento fino) - PRECHECK
-- Objetivo:
--   1) Verificar base de partida del Lote 02 (RLS activo).
--   2) Identificar políticas actuales por tabla critica.
--   3) Confirmar roles candidatos para aplicar políticas finas.
-- =============================================================

SET lock_timeout = '5s';
SET statement_timeout = '120s';

SELECT current_database() AS db, current_user AS usuario_actual, now() AS ejecutado_en;

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
    CASE WHEN c.oid IS NULL THEN false ELSE true END AS existe,
    c.relrowsecurity AS rls_habilitado,
    c.relforcerowsecurity AS rls_forzado,
    EXISTS (
        SELECT 1
        FROM pg_policies p
        WHERE p.schemaname = 'public'
          AND p.tablename = t.tabla
          AND p.policyname = 'kan14_lote02_app_rw'
    ) AS tiene_policy_lote02
FROM tablas t
LEFT JOIN pg_class c ON c.relname = t.tabla
LEFT JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = 'public'
ORDER BY t.tabla;

-- Políticas actuales por tabla crítica
SELECT
    p.tablename,
    p.policyname,
    p.cmd,
    p.roles,
    p.permissive
FROM pg_policies p
WHERE p.schemaname = 'public'
  AND p.tablename IN ('usuario','rol','permiso','rol_permiso','usuario_permiso_override','sesion_usuario','auditoria_log')
ORDER BY p.tablename, p.policyname, p.cmd;

-- Roles candidatos (existentes)
WITH candidatos(nombre) AS (
    VALUES ('authenticated'), ('service_role'), ('postgres'), (current_user)
)
SELECT DISTINCT c.nombre AS rol_candidato, r.oid IS NOT NULL AS existe_en_bd
FROM candidatos c
LEFT JOIN pg_roles r ON r.rolname = c.nombre
ORDER BY c.nombre;

-- Gate precheck: debe retornar 0 para continuar
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
SELECT COUNT(*) AS total_bloqueantes_lote03_rls
FROM tablas t
LEFT JOIN pg_class c ON c.relname = t.tabla
LEFT JOIN pg_namespace n ON n.oid = c.relnamespace AND n.nspname = 'public'
WHERE c.oid IS NULL
   OR c.relrowsecurity IS DISTINCT FROM TRUE;
